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
    public class PVSOctreeNode
    {
        /// <summary>
        /// 
        /// </summary>
        List<PVSOctreeNode> m_ChildList = new List<PVSOctreeNode>();

        /// <summary>
        /// 
        /// </summary>
        PVSVolume m_Volume;

        /// <summary>
        /// 
        /// </summary>
        Quaternion m_VolumeRot;
        Vector3 m_VolumeSize;

        /// <summary>
        /// 
        /// </summary>
        public int leafNodeIdx;

        /// <summary>
        /// 
        /// </summary>
        public bool canSplit;
        public bool manualSplitMinMode;
        public List<Vector3Int> saveManualSplitPosList;

        /// <summary>
        /// 
        /// </summary>
        public PVSGizmosMgr maxDensityAreaGizmosMgr;

        /// <summary>
        /// 
        /// </summary>
        public bool hasNodeData;

        /// <summary>
        /// 
        /// </summary>
        public bool hasRawData;

        /// <summary>
        /// 
        /// </summary>
        public int nodeID;

        /// <summary>
        /// 
        /// </summary>
        public Vector3 samplePointLocalLowerPos;

        /// <summary>
        /// 
        /// </summary>
        public Vector3 samplePointWorldPos;
        public Vector3 samplePointWorldLowerPos;

        /// <summary>
        /// 
        /// </summary>
        public Bounds areaBound;

        /// <summary>
        /// 
        /// </summary>
        public List<uint> rawDataList = new List<uint>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_volumePos"></param>
        /// <param name="_volumeRot"></param>
        /// <param name="_volumeSize"></param>
        /// <param name="_cellIndex"></param>
        /// <param name="_cellSize"></param>
        /// <param name="_isForceSampleIndex"></param>
        public void Init(PVSVolume _volume, Vector3 _volumePos, Quaternion _volumeRot, Vector3 _volumeSize, int _cellIndex, Vector3 _cellSize)
        {
            m_Volume = _volume;
            m_VolumeRot = _volumeRot;
            m_VolumeSize = _volumeSize;

            samplePointLocalLowerPos = PVSVolumeUtils.GetSamplingPositionAt(_volumePos, _volumeRot,
                _volumeSize, _cellIndex, _cellSize, Space.Self, PVSAlignment.LowerLeft, PVSCoordSystem.BottomLeftOrigin);

            samplePointWorldPos = PVSVolumeUtils.GetSamplingPositionAt(_volumePos, _volumeRot, 
                _volumeSize, _cellIndex, _cellSize, Space.World, PVSAlignment.MiddleCenter, PVSCoordSystem.CenterOrigin);
            samplePointWorldLowerPos = PVSVolumeUtils.GetSamplingPositionAt(_volumePos, _volumeRot,
                _volumeSize, _cellIndex, _cellSize, Space.World, PVSAlignment.LowerLeft, PVSCoordSystem.CenterOrigin);

            CalcNodeID(_cellIndex);
            CalcAreaBounds(_cellSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_cellIndex"></param>
        /// <param name="_cellSize"></param>
        /// <param name="_volumeRot"></param>
        /// <param name="_volumeSize"></param>
        /// <param name="_samplePointLocalLowerPos"></param>
        /// <param name="_samplePointWorldLowerPos"></param>
        /// <param name="_isForceSampleIndex"></param>
        public void InitChild(PVSVolume _volume, int _cellIndex, Vector3 _cellSize, 
            Quaternion _volumeRot, Vector3 _volumeSize,
            Vector3 _samplePointLocalLowerPos, Vector3 _samplePointWorldLowerPos)
        {
            m_Volume = _volume;
            m_VolumeRot = _volumeRot;
            m_VolumeSize = _volumeSize;

            var cellNumVec = PVSDefine.s_OctreeChildNumVec;
            samplePointLocalLowerPos = PVSVolumeUtils.GetSamplingChildPositionAt(_samplePointLocalLowerPos,
                _cellIndex, cellNumVec, _cellSize, _volumeRot, Space.Self, PVSAlignment.LowerLeft, out _);

            samplePointWorldPos = PVSVolumeUtils.GetSamplingChildPositionAt(_samplePointWorldLowerPos,
                _cellIndex, cellNumVec, _cellSize, _volumeRot, Space.World, PVSAlignment.MiddleCenter, out _);
            samplePointWorldLowerPos = PVSVolumeUtils.GetSamplingChildPositionAt(_samplePointWorldLowerPos,
                _cellIndex, cellNumVec, _cellSize, _volumeRot, Space.World, PVSAlignment.LowerLeft, out _);

            CalcNodeID(_cellIndex);
            CalcAreaBounds(_cellSize);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SplitForceArea()
        {
            var hasRawData = HasChildRawData();
            if (!hasRawData)
            {
                if (m_ChildList.Count > 0)
                {
                    Logger.LogError("[PVS][PVSSceneOctree] m_ChildList.Count >= 0");
                }

                SetForceAreaData();
            }
        }

        /// <summary>
        /// 没有RawData数据的块合并
        /// </summary>
        public void Merge()
        {
            if (m_ChildList.Count <= 0)
                return;

            var hasRawData = HasChildRawData();
            if (!hasRawData)
            {
                m_ChildList.Clear();
            }
            else
            {
                foreach (var childNode in m_ChildList)
                {
                    if (childNode == null)
                        continue;

                    childNode.Merge();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Split()
        {
            if (!canSplit)
            {
                Split_DisbaleSplit();
            }
            else
            {
                Split_EnableSplit();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_nodeList"></param>
        public void CacheNodeList(List<PVSOctreeNode> _nodeList)
        {
            if (!canSplit)
            {
                CacheNodeList_DisbaleSplit(_nodeList);
            }
            else
            {
                CacheNodeList_EnableSplit(_nodeList);
            }
        }

        /// <summary>
        /// Octree计算过了RawData，后面不计算了
        /// </summary>
        /// <returns></returns>
        public bool IsForceSamplePos()
        {
            var isForceSamplePos = false;

            if (!canSplit)
            {
                isForceSamplePos = hasRawData;
            }
            else
            {
                // Octree计算过了RawData，后面不计算了, 所以全部用强制采样
                isForceSamplePos = true;
            }

            return isForceSamplePos;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chunkIdx"></param>
        /// <param name="dataIdx"></param>
        public void FillRawInfo(ushort chunkIdx, uint dataIdx)
        {
            rawDataList.Clear();
            rawDataList.Add(chunkIdx);
            rawDataList.Add(dataIdx);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetDesc()
        {
            string strDesc = "";
            strDesc += nodeID.ToString();
            strDesc += "    ";
            strDesc += samplePointWorldPos.ToString();
            strDesc += "    ";

            return strDesc;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_saveBigVisIndex"></param>
        /// <param name="dataBytes"></param>
        /// <returns></returns>
        public int SerializeWrite(bool _saveBigVisIndex, RapidList<byte> dataBytes)
        {
            SerializeWrite_RawData(_saveBigVisIndex, dataBytes);

            if (rawDataList.Count <= 0)
            {
                SerializeWrite_ChildExist(dataBytes);

                if (m_ChildList.Count > 0)
                {
                    SerializeWrite_Child(_saveBigVisIndex, dataBytes);
                }
            }

            return dataBytes.count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_nodeList"></param>
        void CacheNodeList_DisbaleSplit(List<PVSOctreeNode> _nodeList)
        {
            _nodeList.Add(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_nodeList"></param>
        void CacheNodeList_EnableSplit(List<PVSOctreeNode> _nodeList)
        {
            if (hasRawData)
            {
                _nodeList.Add(this);
            }
            else
            {
                foreach (var child in m_ChildList)
                {
                    if (child == null)
                        continue;

                    child.CacheNodeList(_nodeList);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_saveBigVisIndex"></param>
        /// <param name="_dataBytes"></param>
        void SerializeWrite_Child(bool _saveBigVisIndex, RapidList<byte> _dataBytes)
        {
            RapidList<int> indexOffArray = new RapidList<int>();
            RapidList<byte> childDataBytes = new RapidList<byte>();

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
                offset = child.SerializeWrite(_saveBigVisIndex, childDataBytes);
            }

            // 2.
            for (int i = 0; i < indexOffArray.count; i++)
            {
                int Offset = indexOffArray[i] + indexOffArray.count * sizeof(int);
                var sizeByte = BitConverter.GetBytes(Offset);
                _dataBytes.AddItems(sizeByte);
            }

            // 3.
            for (int i = 0; i < childDataBytes.count; i++)
            {
                _dataBytes.Add(childDataBytes[i]);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataBytes"></param>
        void SerializeWrite_ChildExist(RapidList<byte> _dataBytes)
        {
            var existByte = m_ChildList.Count > 0 ? PVSDefine.s_OneByte : PVSDefine.s_EmptyByte;
            _dataBytes.Add(existByte);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_saveBigVisIndex"></param>
        /// <param name="_dataBytes"></param>
        void SerializeWrite_RawData(bool _saveBigVisIndex, RapidList<byte> _dataBytes)
        {
            var existByte = rawDataList.Count > 0 ? PVSDefine.s_OneByte : PVSDefine.s_EmptyByte;
            _dataBytes.Add(existByte);

            if (rawDataList.Count > 0)
            {
                if (rawDataList.Count != 2)
                {
                    Logger.LogError("[PVS][PVSOctreeNode] SerializeWrite_RawData rawDataList.Count != 2");
                }

                if (m_ChildList.Count > 0)
                {
                    Logger.LogError("[PVS][PVSOctreeNode] SerializeWrite_RawData m_ChildList.Count > 0");
                }

                // chunkIdx 项目有效值不会超过 64，用byte存储不会溢出 
                var chunkIdx = (byte)rawDataList[0];
                _dataBytes.Add(chunkIdx);

                if (_saveBigVisIndex)
                {
                    var rawUintIdx = rawDataList[1];
                    var rawIdSizeByte = BitConverter.GetBytes(rawUintIdx);
                    _dataBytes.AddItems(rawIdSizeByte);
                }
                else
                {
                    var rawIdx = (ushort)rawDataList[1];
                    GridMath.FlattenUShort2Byte(rawIdx, out byte _highByte, out byte _lowByte);
                    _dataBytes.Add(_highByte);
                    _dataBytes.Add(_lowByte);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void CalcNodeID(int _cellIndex)
        {
            if (!canSplit)
            {
                CalcNodeID_DisableSplit(_cellIndex);
            }
            else
            {
                CalcNodeID_EnableSplit();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void CalcNodeID_EnableSplit()
        {
            var cellSize = PVSDefine.s_OctreeMinLeafSizeVec;
            var cellCount = GridMath.CalculateCellCount(m_VolumeSize, cellSize);
            GridMath.UnflattenToXYZ(samplePointLocalLowerPos, cellSize, out int x, out int y, out int z);
            nodeID = GridMath.FlattenXYZ(x, y, z, cellCount);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_cellIndex"></param>
        void CalcNodeID_DisableSplit(int _cellIndex)
        {
            nodeID = _cellIndex;
        }

        /// <summary>
        /// 
        /// </summary>
        void Split_EnableSplit()
        {
            var isSetData = SplitSetData();
            if (isSetData)
                return;

            var childNum = PVSDefine.s_OctreeChildNum;
            for (int i = 0; i < childNum; i++)
            {
                var childCellSize = CalcChildCellSize();

                PVSOctreeNode childNode = new PVSOctreeNode();
                childNode.leafNodeIdx = leafNodeIdx + 1;
                childNode.canSplit = canSplit;
                childNode.manualSplitMinMode = manualSplitMinMode;
                childNode.saveManualSplitPosList = saveManualSplitPosList;
                childNode.maxDensityAreaGizmosMgr = maxDensityAreaGizmosMgr;
                childNode.InitChild(m_Volume, i, childCellSize, m_VolumeRot, m_VolumeSize, samplePointLocalLowerPos, samplePointWorldLowerPos);
                childNode.Split();

                m_ChildList.Add(childNode);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool SplitSetData()
        {
            var isCollision = PVSOctreeUtils.IsMaxDensityAreaCollision(maxDensityAreaGizmosMgr, areaBound);
            var isMinLeaf = PVSOctreeUtils.IsMinLeaf(areaBound);
            var isAutoMinLeaf = PVSOctreeUtils.IsAutoMinLeaf(areaBound);

            bool isSetData = false;

            if (!isCollision)
            {
                isSetData = true;
            }

            if (manualSplitMinMode)
            {
                if (isAutoMinLeaf)
                {
                    isSetData = true;
                }
            }
            else
            {
                var camMaxDisOffset = m_Volume.CalcCamMaxDisOffset();
                var isContainPos = PVSOctreeUtils.IsManualSplitPointContain(saveManualSplitPosList, 
                    samplePointWorldPos, camMaxDisOffset);
                if (isContainPos)
                {
                    if (isMinLeaf)
                    {
                        isSetData = true;
                    }
                }
                else
                {
                    if (isAutoMinLeaf)
                    {
                        isSetData = true;
                    }
                }
            }

            if (isSetData)
            {
                SetData();
            }

            return isSetData;
        }

        /// <summary>
        /// 
        /// </summary>
        void Split_DisbaleSplit()
        {
            SetData();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Vector3 CalcChildCellSize()
        {
            var childCellSize = areaBound.size * 0.5f;
            return childCellSize;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_cellSize"></param>
        void CalcAreaBounds(Vector3 _cellSize)
        {
            areaBound.center = samplePointWorldPos;
            areaBound.size = _cellSize;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool HasChildRawData()
        {
            bool tempHasRawData = hasRawData;

            foreach (var childNode in m_ChildList)
            {
                if (childNode == null)
                    continue;

                var tempHasData = childNode.HasChildRawData();
                if (tempHasData)
                {
                    tempHasRawData = true;
                    break;
                }
            }

            return tempHasRawData;
        }

        /// <summary>
        /// 
        /// </summary>
        void SetForceAreaData()
        {
            hasNodeData = true;
            hasRawData = true;
        }

        /// <summary>
        /// 
        /// </summary>
        void SetData()
        {
            hasNodeData = true;

            if (canSplit)
            {
                if (m_Volume == null)
                {
                    Logger.LogError($"[PVS][PVSOctreeNode] m_Volume == null");
                    return;
                }

                if (manualSplitMinMode)
                {
                    hasRawData = m_Volume.ManualSplitSamplingProviderIsPosActive(samplePointWorldPos);
                }
                else
                {
                    hasRawData = m_Volume.SamplingProvidersIsPositionActive(samplePointWorldPos);
                }
            }
            else
            {
                hasRawData = false;
            }
        }
    }
}
