
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ST.Core;
using Logger = ST.Core.Logging.Logger;


namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSSceneOctree
    {
        /// <summary>
        /// 
        /// </summary>
        static PVSSceneOctree s_Instance;

        /// <summary>
        /// 
        /// </summary>
        public static PVSSceneOctree S
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new PVSSceneOctree();
                }

                return s_Instance;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        List<PVSOctreeNode> m_ChildList = new List<PVSOctreeNode>();

        /// <summary>
        /// 
        /// </summary>
        List<PVSOctreeNode> m_NodeList = new List<PVSOctreeNode>();
        Dictionary<int, PVSOctreeNode> m_NodeDict = new Dictionary<int, PVSOctreeNode>();

        /// <summary>
        /// 
        /// </summary>
        List<PVSWorldPosInfo> m_NodePosInfoList = new List<PVSWorldPosInfo>();

        /// <summary>
        /// 
        /// </summary>
        public Vector3 cellNumVec;
        public Vector3 cellSize;

        /// <summary>
        /// 
        /// </summary>
        public PVSGizmosMgr maxDensityAreaGizmosMgr;

        /// <summary>
        /// 
        /// </summary>
        public List<PVSOctreeNode> nodeList
        {
            get { return m_NodeList; }
        }

        /// <summary>
        /// 
        /// </summary>
        public List<PVSWorldPosInfo> nodePosInfoList
        {
            get { return m_NodePosInfoList; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_volumePos"></param>
        /// <param name="_volumeRot"></param>
        /// <param name="_volumeSize"></param>
        /// <param name="_bakeCellSize"></param>
        public void SplitAllForceArea(Vector3 _volumePos, Quaternion _volumeRot, Vector3 _volumeSize, Vector3 _bakeCellSize)
        {
            var forceSampleAreaMgr = PVSForceSampleAreaMgr.S;
            var ignorePointAreaMgr = PVSIgnorePointAreaGizmosMgr.S;
            forceSampleAreaMgr.CalcForceSampleIdxDict(true, _volumePos, _volumeRot,
                _volumeSize, cellNumVec, cellSize);

            for (int i = 0; i < m_ChildList.Count; ++i) 
            {
                var childNode = m_ChildList[i];
                if (childNode == null) 
                    continue;

                var pos = childNode.samplePointWorldPos;

                var isForceSample = forceSampleAreaMgr.IsForceSampleIdx(i);
                if (!isForceSample)
                    continue;

                var isIgnorePt = ignorePointAreaMgr.IsPointInGizmos(pos);
                if (isIgnorePt)
                    continue;

                childNode.SplitForceArea();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_volume"></param>
        /// <param name="_commonBakeData"></param>
        /// <param name="_volumePos"></param>
        /// <param name="_volumeRot"></param>
        /// <param name="_volumeSize"></param>
        /// <param name="_bakeCellSize"></param>
        /// <param name="_manualSplitMinMode"></param>
        /// <returns></returns>
        public bool SplitAll(PVSVolume _volume, PVSCommonBakeData _commonBakeData, 
            Vector3 _volumePos, Quaternion _volumeRot, 
            Vector3 _volumeSize, Vector3 _bakeCellSize, bool _manualSplitMinMode)
        {
            m_ChildList.Clear();

            maxDensityAreaGizmosMgr = PVSMaxDensityAreaGizmosMgr.S;
            var isAreaExist = PVSOctreeUtils.IsMaxDensityAreaExist(false);
            var cellVal = PVSDefine.s_OctreeMaxLeafSize;
            var cellSize = new Vector3(cellVal, cellVal, cellVal);
            var cellNum = GridMath.CalculateNumberOfCells(_volumeSize, cellSize);
            var cellNumVec = GridMath.CalculateCellCount(_volumeSize, cellSize);
            if (!isAreaExist)
            {
                cellSize = _bakeCellSize;
                cellNum = GridMath.CalculateNumberOfCells(_volumeSize, _bakeCellSize);
                cellNumVec = GridMath.CalculateCellCount(_volumeSize, _bakeCellSize);
            }

            this.cellNumVec = cellNumVec;
            this.cellSize = cellSize;

            for (int i = 0; i < cellNum; i++)
            {
                var node = CreateNode(_volume, _commonBakeData, isAreaExist, _volumePos, _volumeRot, 
                    _volumeSize, i, cellSize, _manualSplitMinMode);
                if (node == null)
                    continue;

                m_ChildList.Add(node);
            }

            return isAreaExist;
        }

        /// <summary>
        /// 
        /// </summary>
        public void CacheNodeInfo()
        {
            CacheNodeList();
            CacheNodeDict();
            CacheNodePosInfoList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodeIdxArray"></param>
        /// <param name="chunkIdxArray"></param>
        /// <param name="dataIdxArray"></param>
        public void ProcessNodeInfo(uint[] nodeIdxArray, ushort[] chunkIdxArray, uint[] dataIdxArray)
        {
            if (m_NodeList.Count != nodeIdxArray.Length)
            {
                Logger.LogError("[PVS][PVSSceneOctree] m_NodeList.Count != nodeIdxArray.Length");
                return;
            }

            int hasDataNodeNum = 0;

            for (int i = 0; i < m_NodeList.Count; i++)
            {
                var node = m_NodeList[i];
                if (node == null)
                    continue;

                var nodeIdx = nodeIdxArray[i];
                var chunkIdx = chunkIdxArray[i];
                var dataIdx = dataIdxArray[i];
                if (nodeIdx == uint.MaxValue)
                    continue;

                if (dataIdx >= ushort.MaxValue)
                {
                    Logger.LogError($"[PVS][PVSSceneOctree] dataIdx >= ushort.MaxValue" +
                            $"ChunkIdx : {chunkIdx}  dataIdx : {dataIdx}");
                }

                node.FillRawInfo(chunkIdx, dataIdx);
                hasDataNodeNum++;
            }

            Logger.Log($"[PVS][PVSSceneOctree] hasDataNodeNum : {hasDataNodeNum}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_saveBigVisIndex"></param>
        /// <returns></returns>
        public byte[] SerializeWrite(bool _saveBigVisIndex)
        {
            RapidList<byte> saveBytes = new RapidList<byte>();
            RapidList<int> indexOffArray = new RapidList<int>();
            RapidList<byte> dataBytes = new RapidList<byte>();

            // 1.
            int offset = 0;
            foreach (var child in m_ChildList)
            {
                if (child == null)
                {
                    Logger.LogError("[PVS][PVSSceneOctree] child is null");
                    continue;
                }

                indexOffArray.Add(offset);
                offset = child.SerializeWrite(_saveBigVisIndex, dataBytes);
            }

            // 2.
            for (int i = 0; i < indexOffArray.count; i++)
            {
                int Offset = indexOffArray[i] + indexOffArray.count * sizeof(int);
                var sizeByte = BitConverter.GetBytes(Offset);
                saveBytes.AddItems(sizeByte);
            }

            // 3.
            for (int i = 0; i < dataBytes.count; i++)
            {
                saveBytes.Add(dataBytes[i]);
            }

            return saveBytes.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        void CacheNodePosInfoList()
        {
            m_NodePosInfoList.Clear();

            foreach (var node in m_NodeList)
            {
                if (node == null)
                    continue;

                var pos = node.samplePointWorldPos;
                var bounds = node.areaBound;

                PVSWorldPosInfo posInfo = new PVSWorldPosInfo();
                posInfo.pos = pos;
                posInfo.bounds = bounds;
                posInfo.leafNodeIdx = node.leafNodeIdx;

                var isForce = node.IsForceSamplePos();
                posInfo.isForceSamplePos = isForce;

                m_NodePosInfoList.Add(posInfo);
            }

            if (m_NodePosInfoList.Count != m_NodeList.Count)
            {
                Logger.LogError($"[PVS][PVSSceneOctree] m_NodePosList.Count != m_NodeList.Count");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void CacheNodeDict()
        {
            m_NodeDict.Clear();

            foreach (var node in m_NodeList)
            {
                if (node == null)
                    continue;

                var nodeID = node.nodeID;
                if (m_NodeDict.ContainsKey(nodeID))
                {
                    var nodeDesc = node.GetDesc();
                    Logger.LogError($"[PVS][PVSSceneOctree] {nodeDesc}");
                    continue;
                }

                m_NodeDict.Add(nodeID, node);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void CacheNodeList()
        {
            m_NodeList.Clear();

            foreach (var child in m_ChildList)
            {
                if (child == null)
                    continue;

                child.CacheNodeList(m_NodeList);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_volume"></param>
        /// <param name="_commonBakeData"></param>
        /// <param name="_isAreaExist"></param>
        /// <param name="_volumePos"></param>
        /// <param name="_volumeRot"></param>
        /// <param name="_volumeSize"></param>
        /// <param name="_cellIndex"></param>
        /// <param name="_cellSize"></param>
        /// <param name="_manualSplitMinMode"></param>
        /// <returns></returns>
        PVSOctreeNode CreateNode(PVSVolume _volume, PVSCommonBakeData _commonBakeData,
            bool _isAreaExist, Vector3 _volumePos, Quaternion _volumeRot, Vector3 _volumeSize, int _cellIndex, Vector3 _cellSize,
            bool _manualSplitMinMode)
        {
            PVSOctreeNode rootNode = new PVSOctreeNode();
            rootNode.leafNodeIdx = 0;
            rootNode.canSplit = _isAreaExist;
            rootNode.manualSplitMinMode = _manualSplitMinMode;
            rootNode.saveManualSplitPosList = (_commonBakeData != null) ? _commonBakeData.saveManualSplitPosList : null;
            rootNode.maxDensityAreaGizmosMgr = maxDensityAreaGizmosMgr;
            rootNode.Init(_volume, _volumePos, _volumeRot, _volumeSize, _cellIndex, _cellSize);
            rootNode.Split();
            rootNode.Merge();
            return rootNode;
        }
    }

}
