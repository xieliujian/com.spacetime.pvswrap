using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using ST.Core;
using Logger = ST.Core.Logging.Logger;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public partial class PVSVolumeEditor
    {
        /// <summary>
        /// 
        /// </summary>
        void OnSceneGUI()
        {
            PVSVolume cullingVolume = target as PVSVolume;
            if (cullingVolume == null)
                return;

            RebuildSampleOffsetMaskDict(cullingVolume);
            DrawVolumeCube(cullingVolume);

            if (cullingVolume.gizmosType == PVSVolumeGizmosType.Default)
            {
                DrawGizmos_Default(cullingVolume);
            }
            else if (cullingVolume.gizmosType == PVSVolumeGizmosType.ManualSplit)
            {
                DrawGizmos_ManualSplit(cullingVolume);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawGizmos_Default(PVSVolume _cullingVolume)
        {
            var _camTrans = PVSVolumeUtils.GetPVSCamTrans(out PVSCamera _cullCam);
            if (_camTrans == null)
                return;

            var volumePos = _cullingVolume.transform.position;
            var volumeRot = _cullingVolume.transform.rotation;
            Matrix4x4 matrix = Matrix4x4.TRS(volumePos, volumeRot, Vector3.one);
            Handles.matrix = matrix;
            Handles.zTest = CompareFunction.LessEqual;

            var camPos = _camTrans.position;

            var curBakeDataVer = _cullingVolume.volumeBakeData.bakeDataVersion;
            if (curBakeDataVer == (int)PVSBakeDataVer.Ver3)
            {
                DrawGizmos_UnOctree(_cullCam, _cullingVolume, camPos);
            }
            else if (curBakeDataVer >= (int)PVSBakeDataVer.Ver4)
            {
                var isOctreeAreaExist = _cullingVolume.volumeBakeData.octreeAreaExist;
                if (isOctreeAreaExist)
                {
                    DrawGizmos_Octree(_cullCam, _cullingVolume, camPos);
                }
                else
                {
                    DrawGizmos_UnOctree(_cullCam, _cullingVolume, camPos);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawGizmos_UnOctree(PVSCamera _cullCam, PVSVolume _cullingVolume, 
            Vector3 _camPos)
        {
            var _cellCount = _cullingVolume.volumeBakeData.cellCount;
            var _cellSize = _cullingVolume.volumeBakeData.cellSize;
            var _range = PVSDefine.s_GizmosVolumeUnOctreeRange;
            var _volumePos = _cullingVolume.transform.position;
            var _volumeRot = _cullingVolume.transform.rotation;

            Vector3[] _offsetArray = null;
            if (_cullCam != null)
            {
                var camPos = _cullCam.GetCamPos();
                _offsetArray = _cullCam.GetOffsetArray(camPos, out _);
            }

            var camSampleIndex = _cullingVolume.GetIndexForWorldPos(_camPos, _cellSize, out bool _);
            GridMath.UnflattenToXYZ(camSampleIndex, out int x, out int y, out int z, _cellCount);

            for (int xx = -_range; xx <= _range; xx++)
            {
                for (int yy = -_range; yy <= _range; yy++)
                {
                    for (int zz = -_range; zz <= _range; zz++)
                    {
                        var newX = x + xx;
                        var newY = y + yy;
                        var newZ = z + zz;
                        var isIndexValid = PVSVolumeUtils.IsIndexValid(newX, newY, newZ, _cellCount);
                        if (!isIndexValid)
                            continue;

                        var isCamPoint = (newX == x && newY == y && newZ == z);
                        var neighborCellIndex = GridMath.FlattenXYZ(newX, newY, newZ, _cellCount);
                        var localPos = _cullingVolume.GetSamplingPositionAt(neighborCellIndex, _cellSize);

                        RapidList<ushort> indices = new RapidList<ushort>();
                        _cullingVolume.volumeBakeData.SampleAtIndex(neighborCellIndex, indices, _camPos, true, out _, out _, 
                            out uint _samplePosOffsetMask);
                        if (indices.count <= 0)
                            continue;

                        if (_cullingVolume.posOffsetDefaultTypeShow)
                        {
                            DrawGizmos_UnOctree_CamDirOffsetType(localPos, _cellSize, _samplePosOffsetMask);
                        }
                        else
                        {
                            DrawGizmos_UnOctree_DefaultOffsetType(_cullCam, _cullingVolume, _volumePos, _volumeRot, localPos,
                                isCamPoint, _cellSize, _offsetArray, _camPos);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawGizmos_UnOctree_CamDirOffsetType(Vector3 _localPos, Vector3 _cellSize, uint _samplePosOffsetMask)
        {
            var isType = PVSCameraUtils.IsCamSampleOffsetDefaultType(_samplePosOffsetMask);

            Handles.color = isType ? PVSDefine.s_Gizmos_CamSamplePosOffset_Default_Color :
                PVSDefine.s_Gizmos_CamSamplePosOffset_CamDir_Color;
            Handles.DrawWireCube(_localPos, _cellSize);
            UnityEditor.Handles.Button(_localPos, Quaternion.identity, 0.5f, 0.5f, UnityEditor.Handles.SphereHandleCap);
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawGizmos_UnOctree_DefaultOffsetType(PVSCamera _cullCam, PVSVolume _cullingVolume,
            Vector3 _volumePos, Quaternion _volumeRot, Vector3 localPos,
            bool isCamPoint, Vector3 _cellSize, Vector3[] _offsetArray, Vector3 _camPos)
        {
            var defaultColor = PVSDefine.s_GizmosVolumeUnOctreeColor;
            var worldPos = GridMath.CalcWorldPos(_volumePos, _volumeRot, localPos);

            var handleColor = defaultColor;
            if (isCamPoint)
            {
                handleColor = new Color(defaultColor.r, defaultColor.g, defaultColor.b, 1f);
            }

            var isInSampleList = _cullingVolume.IsSampleWorldPosInList(worldPos, out int _findIndex);
            if (isInSampleList)
            {
                handleColor = _findIndex == 0 ? new Color(handleColor.r, handleColor.g, handleColor.b, 1) :
                    PVSDefine.s_GizmosVolumeOctreeNeighborColor;
            }

            Handles.color = handleColor;
            Handles.DrawWireCube(localPos, _cellSize);
            PVSCameraUtils.ShowSamplePosOffsetMaskDesc(_cullingVolume, worldPos, localPos, out uint _samplePosOffsetMask);
            PVSVolumeUtils.DrawGUI_OffsetPosDesc(isInSampleList, _findIndex, _cullCam, _offsetArray, localPos);

            if (UnityEditor.Handles.Button(localPos, Quaternion.identity, 0.5f, 0.5f, UnityEditor.Handles.SphereHandleCap))
            {
                var offsetPos = "";
                if (_camPos != null)
                {
                    if (_findIndex >= 0)
                    {
                        if (_offsetArray != null)
                        {
                            offsetPos = _offsetArray[_findIndex].ToString();
                        }
                    }
                }

                var desc = PVSCameraUtils.GetSamplePosOffsetMaskDesc(_samplePosOffsetMask);
                Logger.Log(string.Format("[PVS] UnOctree Gizmos Pos : {0} {1} {2}", worldPos, offsetPos, desc));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawGizmos_Octree(PVSCamera _cullCam, PVSVolume _cullingVolume, Vector3 _camPos)
        {
            var _cellCount = _cullingVolume.volumeBakeData.cellCount;
            var _cellSize = _cullingVolume.volumeBakeData.cellSize;
            var _volumePos = _cullingVolume.transform.position;
            var _volumeRot = _cullingVolume.transform.rotation;
            var _volumeSize = _cullingVolume.volumeSize;

            var _dataArray = _cullingVolume.volumeBakeData.rawDataIdxArray;
            if (_dataArray == null || _dataArray.Length <= 0)
                return;

            PVSOctreeUtils.DrawGizmosOctree(_cullCam, _cullingVolume, _dataArray, _camPos, _volumePos,
                    _volumeRot, _volumeSize, _cellCount, _cellSize);
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawGizmos_ManualSplit(PVSVolume cullingVolume)
        {
            var camTrans = PVSVolumeUtils.GetPVSCamTrans(out _);
            if (camTrans == null)
                return;

            var volumeTrans = cullingVolume.transform;

            Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            Handles.matrix = matrix;
            Handles.zTest = CompareFunction.LessEqual;

            var camPos = camTrans.position;
            var _cellCount = cullingVolume.volumeBakeData.cellCount;
            var _cellSize = cullingVolume.volumeBakeData.cellSize;
            var _volumePos = volumeTrans.position;
            var _volumeRot = volumeTrans.rotation;
            var _volumeSize = cullingVolume.volumeSize;

            PVSOctreeUtils.DrawGizmos_ManualSplit(cullingVolume, camPos, _volumePos,
                    _volumeRot, _volumeSize);
        }

        /// <summary>
        /// Draws the volume cube in the scene view.
        /// </summary>
        void DrawVolumeCube(PVSVolume cullingVolume)
        {
            if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "FrameSelected")
            {
                Event.current.commandName = "";
                Event.current.Use();
                SceneView.lastActiveSceneView.Frame(cullingVolume.volumeBakeBounds, false);
                return;
            }

            m_handle.DrawHandle(cullingVolume);

            Handles.matrix = cullingVolume.transform.localToWorldMatrix;
            Handles.zTest = CompareFunction.LessEqual;
            Handles.color = Color.blue;
            Handles.DrawWireCube(Vector3.zero, cullingVolume.volumeSize);
        }

        /// <summary>
        /// 
        /// </summary>
        void RebuildSampleOffsetMaskDict(PVSVolume _cullingVolume)
        {
            var commonBakeData = _cullingVolume.commonBakeData;
            if (commonBakeData == null)
                return;

            var isFill = commonBakeData.IsSampleOffsetMaskDictFill();
            if (isFill)
                return;

            commonBakeData.RebuildSampleOffsetMaskDict();
        }
    }
}
