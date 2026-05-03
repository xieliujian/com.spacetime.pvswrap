using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ST.Core;
using Logger = ST.Core.Logging.Logger;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSOctreeUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_maxDensityAreaGizmosMgr"></param>
        /// <returns></returns>
        public static bool IsMaxDensityAreaExist(PVSGizmosMgr _maxDensityAreaGizmosMgr)
        {
            if (PVSDefine.s_CurBakeDataVer < (int)PVSBakeDataVer.Ver4)
                return false;

            if (_maxDensityAreaGizmosMgr == null)
                return false;

            var hasData = _maxDensityAreaGizmosMgr.HasData();
            if (!hasData)
                return false;

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isReCollect"></param>
        /// <returns></returns>
        public static bool IsMaxDensityAreaExist(bool isReCollect)
        {
            var gizmosMgr = PVSMaxDensityAreaGizmosMgr.S;
            if (isReCollect)
            {
                gizmosMgr.ReCollect();
            }

            var isAreaExist = IsMaxDensityAreaExist(gizmosMgr);
            return isAreaExist;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cullingVolume"></param>
        /// <returns></returns>
        public static bool IsMaxDensityAreaExist_Game(PVSVolume cullingVolume)
        {
            var isAreaExist = cullingVolume.volumeBakeData.octreeAreaExist;
            return isAreaExist;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_maxDensityAreaGizmosMgr"></param>
        /// <param name="_nodeBounds"></param>
        /// <returns></returns>
        public static bool IsMaxDensityAreaCollision(PVSGizmosMgr _maxDensityAreaGizmosMgr, Bounds _nodeBounds)
        {
            if (_maxDensityAreaGizmosMgr == null)
                return false;

            var isCollision = _maxDensityAreaGizmosMgr.IsCollision(_nodeBounds);
            return isCollision;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool IsMinLeaf(Bounds _areaBound)
        {
            var isSame = IsMinLeaf(_areaBound, PVSDefine.s_OctreeMinLeafSize);
            return isSame;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_areaBound"></param>
        /// <returns></returns>
        public static bool IsAutoMinLeaf(Bounds _areaBound)
        {
            var isSame = IsMinLeaf(_areaBound, PVSDefine.s_OctreeAutoMinLeafSize);
            return isSame;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void DeserializeRead(bool _saveBigVisIndex, byte[] _dataArray, Vector3 _worldPos, Vector3 _volumePos, 
            Quaternion _volumeRot, Vector3 _volumeSize, Vector3 _cellNumVec, Vector3 _cellSize, 
            out byte _outChunkIdx, out ushort _outRawIdx, out uint _outRawUintIdx, out Vector3 _sampleWorldPos,
            ref int leafNodeIdx)
        {
            leafNodeIdx = 0;
            var cellIndex = GridMath.GetIndexForWorldPos(_worldPos, _volumePos, _volumeRot,
                _volumeSize, _volumeRot, _cellNumVec, _cellSize, out bool _, out Vector3 _outLocalPos);
            var readerOff = cellIndex * sizeof(int);
            var nodeReaderOff = BitConverter.ToInt32(_dataArray, readerOff);
            var localLowerPos = PVSVolumeUtils.GetSamplingPositionAt(_volumePos, _volumeRot, _volumeSize, cellIndex, _cellSize,
                Space.Self, PVSAlignment.LowerLeft, PVSCoordSystem.BottomLeftOrigin);
            var localCenterOriginPos = PVSVolumeUtils.GetSamplingPositionAt(_volumePos, _volumeRot, _volumeSize, cellIndex, _cellSize,
                Space.Self, PVSAlignment.LowerLeft, PVSCoordSystem.CenterOrigin);
            var localCellSize = _cellSize / 2;
            DeserializeNode(_volumePos, _volumeRot, _saveBigVisIndex, ref leafNodeIdx, _dataArray, nodeReaderOff, _outLocalPos, localLowerPos, localCenterOriginPos, localCellSize,
                out _outChunkIdx, out _outRawIdx, out _outRawUintIdx, out _sampleWorldPos);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void DrawGizmosOctree(PVSCamera _cullCam, PVSVolume _cullingVolume, 
            byte[] _dataArray, Vector3 _worldPos, 
            Vector3 _volumePos, Quaternion _volumeRot, Vector3 _volumeSize, 
            Vector3 _cellNumVec, Vector3 _cellSize)
        {
            var _range = PVSDefine.s_GizmosVolumeOctreeRange;

            var curCellIndex = GridMath.GetIndexForWorldPos(_worldPos, _volumePos, _volumeRot,
                            _volumeSize, _volumeRot, _cellNumVec, _cellSize, out bool _, out Vector3 _);

            Vector3[] offsetArray = null;
            if (_cullCam != null)
            {
                var camPos = _cullCam.GetCamPos();
                offsetArray = _cullCam.GetOffsetArray(camPos, out _);
            }

            for (int xx = -_range; xx <= _range; xx++)
            {
                for (int yy = -_range; yy <= _range; yy++)
                {
                    for (int zz = -_range; zz <= _range; zz++)
                    {
                        var neighborPos = _worldPos + new Vector3(xx * _cellSize.x, yy * _cellSize.y, zz * _cellSize.z);

                        var leafNodeIdx = 0;
                        var cellIndex = GridMath.GetIndexForWorldPos(neighborPos, _volumePos, _volumeRot,
                            _volumeSize, _volumeRot, _cellNumVec, _cellSize, out bool _, out Vector3 _outLocalPos);

                        var readerOff = cellIndex * sizeof(int);
                        var nodeReaderOff = BitConverter.ToInt32(_dataArray, readerOff);

                        bool _cellIdxSame = (cellIndex == curCellIndex);
                        var localLowerPos = PVSVolumeUtils.GetSamplingPositionAt(_volumePos, _volumeRot, 
                            _volumeSize, cellIndex, _cellSize, Space.Self, PVSAlignment.LowerLeft, PVSCoordSystem.BottomLeftOrigin);
                        var samplePointLocalPos = PVSVolumeUtils.GetSamplingPositionAt(_volumePos, _volumeRot,
                            _volumeSize, cellIndex, _cellSize, Space.Self, PVSAlignment.MiddleCenter, PVSCoordSystem.CenterOrigin);
                        var samplePointLocalLowerPos = PVSVolumeUtils.GetSamplingPositionAt(_volumePos, _volumeRot,
                            _volumeSize, cellIndex, _cellSize, Space.Self, PVSAlignment.LowerLeft, PVSCoordSystem.CenterOrigin);

                        DrawGizmosOctreeNode(_cullCam, _cullingVolume, offsetArray, _cellIdxSame, _outLocalPos, localLowerPos, _volumePos, _volumeRot, leafNodeIdx, _dataArray,
                            nodeReaderOff, _cellSize, samplePointLocalPos, samplePointLocalLowerPos);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void DrawGizmos_ManualSplit(PVSVolume cullingVolume,
            Vector3 _camPos,  Vector3 _volumePos, Quaternion _volumeRot, Vector3 _volumeSize)
        {
            var manualSplitPosInfoList = cullingVolume.manualSplitPosInfoList;
            if (manualSplitPosInfoList == null || manualSplitPosInfoList.Count <= 0)
                return;

            var volumeBakeData = cullingVolume.volumeBakeData;
            if (volumeBakeData == null)
                return;

            var commonBakeData = cullingVolume.commonBakeData;
            if (commonBakeData == null)
                return;

            foreach (var posInfo in manualSplitPosInfoList)
            {
                if (posInfo == null)
                    continue;
              
                var pos = posInfo.pos;
                var distance = Vector3.Distance(_camPos, pos);
                if (distance >= 50)
                    continue;

                var leafNodeIdx = posInfo.leafNodeIdx;
                var cellSize = PVSVolumeUtils.CalcOctreeCellSize(leafNodeIdx);
                var isNear = Vector3.Distance(pos, _camPos);

                var isSamePt = commonBakeData.IsSameManualSplitPoint(pos);
                var color = isSamePt ? Color.red : Color.green;

#if UNITY_EDITOR
                UnityEditor.Handles.color = color;
                UnityEditor.Handles.DrawWireCube(pos, cellSize);

                var cubeSize = 1f;

                if (UnityEditor.Handles.Button(pos, Quaternion.identity, cubeSize, cubeSize, UnityEditor.Handles.SphereHandleCap))
                {
                    commonBakeData.SwitchManualSplitPoint(pos);
                }
#endif
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void CollectAllManualSplitPoint(PVSVolume _cullingVolume)
        {
            _cullingVolume.CreateCommonBakeDataAsset();

            var volumeNode = _cullingVolume.transform;
            var volumePos = volumeNode.position;
            var volumeRot = volumeNode.rotation;
            var volumeSize = _cullingVolume.volumeSize;
            var orientation = volumeNode.rotation;
            var cellSize = _cullingVolume.bakeCellSize;

            var sceneOctree = PVSSceneOctree.S;
            sceneOctree.SplitAll(_cullingVolume, _cullingVolume.commonBakeData, volumePos, volumeRot, volumeSize, cellSize, true);
            sceneOctree.CacheNodeInfo();

            var nodePosInfoList = sceneOctree.nodePosInfoList;
            _cullingVolume.manualSplitPosInfoList = nodePosInfoList;
        }

        /// <summary>
        /// 
        /// </summary>
        public static Vector3Int CalcManualSplitPoint(Vector3 _pos)
        {
            var leafSize = PVSDefine.s_OctreeAutoMinLeafSize;
            var x = Mathf.FloorToInt(_pos.x / leafSize);
            var y = Mathf.FloorToInt(_pos.y / leafSize);
            var z = Mathf.FloorToInt(_pos.z / leafSize);
            var point = new Vector3Int(x, y, z);
            return point;
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool IsManualSplitPointContain(List<Vector3Int> _saveManualSplitPosList, Vector3 _samplePointWorldPos,
            float _camMaxDisOffset)
        {
            if (_saveManualSplitPosList == null)
                return false;

            float maxDistance = _camMaxDisOffset;
            float maxDistanceInt = maxDistance / PVSDefine.s_OctreeAutoMinLeafSize;
            var calcPos = CalcManualSplitPoint(_samplePointWorldPos);

            foreach(var pos in _saveManualSplitPosList)
            {
                if (pos.x == calcPos.x && pos.z == calcPos.z)
                {
                    var height = (calcPos.y - pos.y);
                    var isSame1 = height <= maxDistance;
                    var isSame2 = height >= PVSDefine.s_GameCamMinDistance;
                    var isSame = isSame1 && isSame2;
                    if (isSame)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        static void DrawGizmosOctreeNode(PVSCamera _cullCam, PVSVolume _cullingVolume, 
            Vector3[] _offsetArray,
            bool _cellIdxSame, Vector3 _localPos, Vector3 _localLowerPos,
            Vector3 _volumePos, Quaternion _volumeRot, 
            int _leafNodeIdx, byte[] _dataArray, int _nodeReaderOff, Vector3 _cellSize,
            Vector3 _samplePointLocalPos, Vector3 _samplePointLocalLowerPos)
        {
            if (_leafNodeIdx >= PVSDefine.s_OctreeMaxLeafNum)
            {
                Logger.LogError($"[PVS][PVSOctreeUtils] _leafNodeIdx Error, this should not happen. leafNodeIdx: {_leafNodeIdx}");
                return;
            }

            var readerOff = _nodeReaderOff;
            byte hasRawData = _dataArray[readerOff];
            readerOff += sizeof(byte);
            if (hasRawData > 0)
            {
                DrawGizmosRawData(_cullCam, _cullingVolume, _offsetArray, _volumePos, _volumeRot, _cellIdxSame, _leafNodeIdx, _samplePointLocalPos, _cellSize);
                return;
            }

            byte hasChild = _dataArray[readerOff];
            readerOff += sizeof(byte);
            if (hasChild <= 0)
                return;

            var deltaPos = _localPos - _localLowerPos;
            var childLocalCellSize = _cellSize / 2;
            var cellNumVec = PVSDefine.s_OctreeChildNumVec;
            var curCellId = GridMath.GetIndexForLocalPos(deltaPos, childLocalCellSize, cellNumVec);

            var cellNum = PVSDefine.s_OctreeChildNum;
            for (int cellId = 0; cellId < cellNum; cellId++)
            {
                var childCellIdxSame = (cellId == curCellId);
                childCellIdxSame &= _cellIdxSame;

                var childLeafNodeIdx = _leafNodeIdx + 1;
                var childLocalReaderOff = readerOff + cellId * sizeof(int);
                var childReaderOff = BitConverter.ToInt32(_dataArray, childLocalReaderOff);
                var childAbsReadOff = readerOff + childReaderOff;

                var childLocalLowerPos = PVSVolumeUtils.GetSamplingChildPositionAt(_localLowerPos,
                    cellId, cellNumVec, childLocalCellSize, Quaternion.identity, Space.Self, PVSAlignment.LowerLeft, out _);

                var childSamplePointLocalPos = PVSVolumeUtils.GetSamplingChildPositionAt(_samplePointLocalLowerPos,
                    cellId, cellNumVec, childLocalCellSize, Quaternion.identity, Space.Self, PVSAlignment.MiddleCenter, out _);
                var childSamplePointLocalLowerPos = PVSVolumeUtils.GetSamplingChildPositionAt(_samplePointLocalLowerPos,
                    cellId, cellNumVec, childLocalCellSize, Quaternion.identity, Space.Self, PVSAlignment.LowerLeft, out _);

                DrawGizmosOctreeNode(_cullCam, _cullingVolume, _offsetArray, childCellIdxSame, _localPos, childLocalLowerPos, 
                    _volumePos, _volumeRot, childLeafNodeIdx, _dataArray, childAbsReadOff, childLocalCellSize,
                    childSamplePointLocalPos, childSamplePointLocalLowerPos);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        static void DeserializeNode(Vector3 _volumePos, Quaternion _volumeRot, bool _saveBigVisIndex, ref int _leafNodeIdx, byte[] _dataArray, int _nodeReaderOff,
            Vector3 _localPos, Vector3 _localLowerPos, Vector3 _localCenterOriginPos, Vector3 _localCellSize, 
            out byte _outChunkIdx, out ushort _outRawIdx, out uint _outRawUintIdx, out Vector3 _sampleWorldPos)
        {
            _outChunkIdx = byte.MaxValue;
            _outRawIdx = ushort.MaxValue;
            _outRawUintIdx = uint.MaxValue;
            _sampleWorldPos = Vector3.zero;

            if (_leafNodeIdx >= PVSDefine.s_OctreeMaxLeafNum)
            {
                Logger.LogError($"[PVS][PVSOctreeUtils] _leafNodeIdx Error, this should not happen. leafNodeIdx: {_leafNodeIdx}");
                return;
            }

            var readerOff = _nodeReaderOff;
            byte hasRawData = _dataArray[readerOff];
            readerOff += sizeof(byte);
            if (hasRawData > 0)
            {
                _sampleWorldPos = _localCenterOriginPos + _localCellSize;
                _sampleWorldPos = GridMath.CalcWorldPos(_volumePos, _volumeRot, _sampleWorldPos);

                DeserializeRawData(_saveBigVisIndex, _dataArray, readerOff, out _outChunkIdx, out _outRawIdx, out _outRawUintIdx);
                return;
            }

            byte hasChild = _dataArray[readerOff];
            readerOff += sizeof(byte);
            if (hasChild <= 0)
                return;

            var deltaPos = _localPos - _localLowerPos;
            var cellNumVec = PVSDefine.s_OctreeChildNumVec;
            var cellId = GridMath.GetIndexForLocalPos(deltaPos, _localCellSize, cellNumVec);
            var childLocalLowerPos = PVSVolumeUtils.GetSamplingChildPositionAt(_localLowerPos,
                    cellId, cellNumVec, _localCellSize, Quaternion.identity, Space.Self, PVSAlignment.LowerLeft, out Vector3 _outGridLocalPos);
            var childLocalCenterOriginPos = _localCenterOriginPos + _outGridLocalPos;
            var childLocalCellSize = _localCellSize / 2;

            _leafNodeIdx++;
            var childLocalReaderOff = readerOff + cellId * sizeof(int);
            var childReaderOff = BitConverter.ToInt32(_dataArray, childLocalReaderOff);
            var childAbsReadOff = readerOff + childReaderOff;
            DeserializeNode(_volumePos, _volumeRot, _saveBigVisIndex, ref _leafNodeIdx, _dataArray, childAbsReadOff, _localPos, childLocalLowerPos, childLocalCenterOriginPos, childLocalCellSize, 
                out _outChunkIdx, out _outRawIdx, out _outRawUintIdx, out _sampleWorldPos);
        }

        /// <summary>
        /// 
        /// </summary>
        static void DrawGizmosRawData(PVSCamera _cullCam, PVSVolume _cullingVolume,
            Vector3[] _offsetArray,
            Vector3 _volumePos, Quaternion _volumeRot, 
            bool _cellIdxSame, int _leafNodeIdx, Vector3 _localPos, Vector3 _cellSize)
        {
            var cubeSize = 0.5f * Mathf.Max(PVSDefine.s_OctreeMaxLeafNum - _leafNodeIdx, 1);
            var worldPos = GridMath.CalcWorldPos(_volumePos, _volumeRot, _localPos);

            if (_cullingVolume.posOffsetDefaultTypeShow)
            {
                DrawGizmosRawData_CamDirOffsetType(_cullCam, _cullingVolume, _offsetArray,
                    _volumePos, _volumeRot, _cellIdxSame, _leafNodeIdx, _localPos, _cellSize, 
                    cubeSize, worldPos);
            }
            else
            {
                DrawGizmosRawData_DefaultOffsetType(_cullCam, _cullingVolume, _offsetArray,
                    _volumePos, _volumeRot, _cellIdxSame, _leafNodeIdx, _localPos, _cellSize, 
                    cubeSize, worldPos);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        static void DrawGizmosRawData_CamDirOffsetType(PVSCamera _cullCam, PVSVolume _cullingVolume,
            Vector3[] _offsetArray,
            Vector3 _volumePos, Quaternion _volumeRot,
            bool _cellIdxSame, int _leafNodeIdx, Vector3 _localPos, Vector3 _cellSize,
            float _cubeSize, Vector3 _worldPos)
        {
#if UNITY_EDITOR
            RapidList<ushort> indices = new RapidList<ushort>();
            _cullingVolume.volumeBakeData.SampleAtIndex(0, indices, _worldPos, true, out _, out _,
                           out uint _samplePosOffsetMask);

            var isType = PVSCameraUtils.IsCamSampleOffsetDefaultType(_samplePosOffsetMask);
            Handles.color = isType ? PVSDefine.s_Gizmos_CamSamplePosOffset_Default_Color :
                PVSDefine.s_Gizmos_CamSamplePosOffset_CamDir_Color;
            Handles.DrawWireCube(_localPos, _cellSize);
            Handles.Button(_localPos, Quaternion.identity, _cubeSize, _cubeSize, UnityEditor.Handles.SphereHandleCap);
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        static void DrawGizmosRawData_DefaultOffsetType(PVSCamera _cullCam, PVSVolume _cullingVolume,
            Vector3[] _offsetArray,
            Vector3 _volumePos, Quaternion _volumeRot,
            bool _cellIdxSame, int _leafNodeIdx, Vector3 _localPos, Vector3 _cellSize,
            float _cubeSize, Vector3 _worldPos)
        {
#if UNITY_EDITOR
            var color = PVSDefine.s_GizmosVolumeOctreeColorArray[_leafNodeIdx];

            if (_cellIdxSame)
            {
                color = new Color(color.r, color.g, color.b, 1);
            }

            var isInSampleList = _cullingVolume.IsSampleWorldPosInList(_worldPos, out int _findIndex);
            if (isInSampleList)
            {
                color = _findIndex == 0 ? new Color(color.r, color.g, color.b, 1) : PVSDefine.s_GizmosVolumeOctreeNeighborColor;
            }

            UnityEditor.Handles.color = color;
            UnityEditor.Handles.DrawWireCube(_localPos, _cellSize);

            PVSCameraUtils.ShowSamplePosOffsetMaskDesc(_cullingVolume, _worldPos, _localPos, out uint _samplePosOffsetMask);
            PVSVolumeUtils.DrawGUI_OffsetPosDesc(isInSampleList, _findIndex, _cullCam, _offsetArray, _localPos);

            if (Handles.Button(_localPos, Quaternion.identity, _cubeSize, _cubeSize, UnityEditor.Handles.SphereHandleCap))
            {
                var maskValid = PVSCameraUtils.IsSamplePosOffsetMaskValid(_samplePosOffsetMask);
                var desc = PVSCameraUtils.GetSamplePosOffsetMaskDesc(_samplePosOffsetMask);
                var format = "[PVS] Octree Gizmos [Pos] {0} {1} [CellSize] {2} ";
                if (maskValid)
                {
                    format += "[Mask] {3} ";
                }

                var offsetPosDesc = "";
                if (_cullCam != null)
                {
                    if (_offsetArray != null && _findIndex != -1)
                    {
                        offsetPosDesc = _offsetArray[_findIndex].ToString();
                    }
                }

                Logger.Log(string.Format(format, _worldPos, offsetPosDesc, _cellSize, desc));
            }
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        static void DeserializeRawData(bool _saveBigVisIndex, byte[] _dataArray, int _readerOff, 
            out byte _outChunkIdx, out ushort _outRawIdx, out uint _outRawUintIdx)
        {
            _outRawIdx = ushort.MaxValue;
            _outRawUintIdx = uint.MaxValue;
            _outChunkIdx = _dataArray[_readerOff];

            if (_saveBigVisIndex)
            {
                var rawUintIdxOff = _readerOff + sizeof(byte);
                _outRawUintIdx = BitConverter.ToUInt32(_dataArray, rawUintIdxOff);
            }
            else
            {
                var rawIdxHigh = _dataArray[_readerOff + 1];
                var rawIdxLower = _dataArray[_readerOff + 2];
                _outRawIdx = GridMath.UnflattenByte2UShort(rawIdxHigh, rawIdxLower);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        static bool IsMinLeaf(Bounds _areaBound, int _leafSize)
        {
            var isXSame = (int)_areaBound.size.x == _leafSize;
            var isYSame = (int)_areaBound.size.y == _leafSize;
            var isZSame = (int)_areaBound.size.z == _leafSize;
            var isSame = isXSame && isYSame && isZSame;
            return isSame;
        }
    }
}
