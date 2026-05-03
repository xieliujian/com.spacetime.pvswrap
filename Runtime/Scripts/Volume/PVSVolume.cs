

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ST.Core;
#if LR_SCENE_MERGE_ENABLED
using LR.Scene.SceneMerge;
#endif
using Logger = ST.Core.Logging.Logger;

namespace ST.PVS
{
    /// <summary>
    /// This is the main volume for Perfect Culling. It contains all the bake groups and is responsible for baking the data.
    /// </summary>
    [ExecuteInEditMode]
    public partial class PVSVolume : PVSBakingBehaviour, CustomHandle.IResizableByHandle
    {
        /// <summary>
        /// 
        /// </summary>
        IActiveSamplingProvider m_ManualSplitSampleProvider = new ManualSplitSamplingProvider();

        [FormerlySerializedAs("VolumeSize")]
        [SerializeField] 
        public Vector3 volumeSize;

        [SerializeField]
        public Bounds volumeBakeBounds
        {
            get => new Bounds(transform.position, volumeSize);

            set
            {
                transform.position = value.center;
                
                volumeSize = new Vector3(
                    Mathf.Max(1, value.size.x), 
                    Mathf.Max(1, value.size.y), 
                    Mathf.Max(1, value.size.z));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public PVSVolumeGizmosType gizmosType = PVSVolumeGizmosType.None;

        public int RenderersCount => bakeGroups.Length;

        public static readonly List<PVSVolume> AllVolumes = new List<PVSVolume>();

        [FormerlySerializedAs("VolumeBakeData")] 
        public PVSVolumeBakeData volumeBakeData;
        public PVSCommonBakeData commonBakeData;

        public override PVSBakeData BakeData => volumeBakeData;

        [FormerlySerializedAs("MergeDownsampleIterations")]
        [Tooltip("After the bake completed for each cell all neighbor cells are merged into a single cell. This will reduce the number of cells without introducing culling issues. This is useful to reduce memory usage.")]
        [Range(0, 8)]
        public int mergeDownsampleIterations = 0;

        public bool bChange = false;

        /// <summary>
        /// 
        /// </summary>
        public bool visCellInfo { get; set; }
        public bool posOffsetDefaultTypeShow { get; set; }

#if LR_SCENE_MERGE_ENABLED
        /// <summary>
        /// 
        /// </summary>
        [SerializeField]
        [HideInInspector]
        public SceneMergeCellExportBlock[] mergeExportBlockArray;
#endif

        /// <summary>
        /// 
        /// </summary>
        [NonSerialized]
        public List<PVSWorldPosInfo> manualSplitPosInfoList;
        [NonSerialized]
        public Vector3[] sampleWorldPosList = new Vector3[PVSDefine.s_CamSamplePosOffsetArray.Length];

        /// <summary>
        /// 
        /// </summary>
        public PVSVolume()
        {
            gizmosType = PVSVolumeGizmosType.None;
            m_CheckSamplePosOffsetMask = true;

            SamplingProviders.Clear();
            AddSamplingProvider(PVSDefine.s_DefaultSamplingProvider);
            AddSamplingProvider(PVSDefine.s_CanMoveSamplingProvider);
        }

        void OnEnable()
        {
            if (volumeBakeData == null || volumeBakeData == null)
                return;

            var isValid = volumeBakeData.IsValid();
            if (!isValid)
                return;

            AllVolumes.Add(this);
        }

        void OnDisable()
        {
            AllVolumes.Remove(this);
            ToggleAllRenderers(true, true);
        }

        void OnDestroy()
        {
            
        }

        void LateUpdate()
        {
            
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            Vector3 nScale = new Vector3((int)volumeSize.x, (int)volumeSize.y, (int)volumeSize.z);

            bakeCellSize = new Vector3(
                Mathf.Min(bakeCellSize.x, nScale.x), 
                Mathf.Min(bakeCellSize.y, nScale.y), 
                Mathf.Min(bakeCellSize.z, nScale.z));
        }

#if LR_SCENE_MERGE_ENABLED
        /// <summary>
        /// 
        /// </summary>
        public void ClearSceneMergeBlockList()
        {
            mergeExportBlockArray = null;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsGameObjExportDB(GameObject _gameObj)
        {
            if (mergeExportBlockArray == null || mergeExportBlockArray.Length <= 0)
                return true;

            foreach(var block in mergeExportBlockArray)
            {
                if (block == null)
                    continue;

                var isExport = block.IsGameObjExportDB(_gameObj);
                if (!isExport)
                    return false;
            }

            return true;
        }
#endif

        /// <summary>
        /// 
        /// </summary>
        public int GetBakeGroupsUseIndex(List<Renderer> _renderList)
        {
            Renderer checkRenderer = _renderList[0];

            for (int i = 0; i < bakeGroups.Length; i++)
            {
                var bakeGroup = bakeGroups[i];
                if (bakeGroup == null)
                    continue;

                if (bakeGroup.HasRenderer(checkRenderer))
                    return i;
            }

            return -1;
        }

        public int GetBakeGroupsUseIndex(GameObject GO)
        {
            List<Renderer> renderers = new List<Renderer>();
            GO.GetComponentsInChildren<Renderer>(true, renderers);

            Renderer[] selfRenderers = GO.GetComponents<Renderer>();
            for (int i = 0; i < selfRenderers.Length; i++)
            {
                renderers.Add(selfRenderers[i]);
            }

            if (renderers.Count == 0)
                return -1;

            var pvsID = GetBakeGroupsUseIndex(renderers);
            return pvsID;
        }
#endif

        /// <summary>
        /// 
        /// </summary>
        public override bool ManualSplitSamplingProviderIsPosActive(Vector3 _pos)
        {
            var isValid = m_ManualSplitSampleProvider.IsSamplingPositionActive(_pos);
            return isValid;
        }

        /// <summary>
        /// 
        /// </summary>
        public override float CalcCamMaxDisOffset()
        {
            float dist = PVSDefine.s_GameCamMaxDistance;
            if (openCamMaxDisOffset)
            {
                dist = camMaxDisOffset;
            }

            return dist;
        }

        /// <summary>
        /// 
        /// </summary>
        public override int GetIndicesForWorldPos(Vector3 worldPos, RapidList<ushort> indices,
            bool isSampleData,
            out Vector3 _samplePos,
            out int _leafNodeIdx, out uint _samplePosOffsetMask)
        { 
            int flat = GetIndexForWorldPos(worldPos, volumeBakeData.cellSize, out bool _);
            GetIndicesForIndex(flat, indices, worldPos, isSampleData, out _samplePos, out _leafNodeIdx, out _samplePosOffsetMask);
            return flat;
        }

        /// <summary>
        /// 
        /// </summary>
        public override List<PVSWorldPosInfo> GetSamplingPosInfoList(Space space,
            bool isSampleNeighbor, Vector3 _volumePos, Quaternion _volumeRot, 
            Vector3 _volumeSize, Vector3 _cellNumVec, Vector3 _cellSize)
        {
            var forceSampleAreaMgr = PVSForceSampleAreaMgr.S;
            forceSampleAreaMgr.CalcForceSampleIdxDict(isSampleNeighbor, _volumePos, _volumeRot,
                _volumeSize, _cellNumVec, _cellSize);

            int cellCount = GridMath.CalculateNumberOfCells(volumeSize, bakeCellSize);
            List<PVSWorldPosInfo> posInfoList = new List<PVSWorldPosInfo>(cellCount);
            
            for (int rawIndexCell = 0; rawIndexCell < cellCount; ++rawIndexCell)
            {
                Vector3 pos = GetSamplingPositionAt(rawIndexCell, bakeCellSize, space);

                PVSWorldPosInfo worldPosInfo = new PVSWorldPosInfo();
                worldPosInfo.pos = pos;

                var bounds = new Bounds(pos, bakeCellSize);
                worldPosInfo.bounds = bounds;

                var isForceSampleIndex = forceSampleAreaMgr.IsForceSampleIdx(rawIndexCell);
                worldPosInfo.isForceSamplePos = isForceSampleIndex;

                posInfoList.Add(worldPosInfo);
            }

            return posInfoList;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void SetSaveBigVisIndex(int _activeSamplingPositionsCount)
        {
            var saveBigVisIndex = PVSBakeDataUtils.IsActivePosNumBeyondLimit(_activeSamplingPositionsCount);
            BakeData.SetSaveBigVisIndex(saveBigVisIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        public override void PreBakeProcessScene()
        {
            var func = PVSWrapBridge.onUnpackAllSectorNodeFunc;
            if (func != null)
            {
                func();
            }
        }

        public override bool PreBake()
        {
            volumeSize = new Vector3(
                (int) volumeSize.x, 
                (int) volumeSize.y,
                (int) volumeSize.z);
            
            volumeBakeData.PreBake(this);

            if ((int)volumeBakeData.cellCount.x == 0 || (int)volumeBakeData.cellCount.y == 0 || (int)volumeBakeData.cellCount.z == 0)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayDialog("Invalid cell size.",
                    "The cell size is invalid. Please double check that the scale of your volume supports at least one cell of the given size.",
                    "OK");
#endif
                
                return false;
            }

            var isValid = PVSVolumeUtils.IsVolumeSizeValid(this, false);
            if (!isValid)
            {
#if UNITY_EDITOR
                var errorDesc = PVSDefine.s_Bake_VolumeSizeError;
                UnityEditor.EditorUtility.DisplayDialog(errorDesc, errorDesc, "OK");
#endif
                return false;
            }

            return true;
        }

        protected override void CullAdditionalOccluders(ref HashSet<Renderer> additionalOccluders)
        {
            if (additionalOccluders == null)
            {
                return;
            }

            Bounds bakeBounds = new Bounds(transform.position, volumeSize);

            HashSet<Renderer> relevantOccluders = new HashSet<Renderer>();

            foreach (Renderer r in additionalOccluders)
            {
                if (!bakeBounds.Intersects(r.bounds))
                {
                    continue;
                }

                relevantOccluders.Add(r);
            }

            additionalOccluders = relevantOccluders;
        }

        public override List<PVSWorldPosInfo> FindWorldPosInfoList()
        {
            List<PVSWorldPosInfo> worldPosInfoList = volumeBakeData.allSamplePosInfoList;
            return worldPosInfoList;
        }

        public override void PostBake()
        {
            for (int i = 0; i < mergeDownsampleIterations; ++i)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayProgressBar(
                    "Performing Merge-Downsample step", $"Performing Merge-Downsample iteration {i + 1}/{mergeDownsampleIterations}",
                    i / (float)mergeDownsampleIterations);
#endif
                volumeBakeData.MergeDownsample();
            }

            PVSBakeDataUtils.CalcUnCompressRawDataSize(volumeBakeData, true);
        }
        
        /// <summary>
        /// 
        /// </summary>
        public override int GetIndexForWorldPos(Vector3 pos, out bool isOutOfBounds)
        {
            return GetIndexForWorldPos(pos, volumeBakeData.cellSize, out isOutOfBounds);
        }

        /// <summary>
        /// 
        /// </summary>
        public override int GetIndexForWorldPos(Vector3 pos, Vector3 cellSize, out bool isOutOfBounds)
        {
            Vector3 cellCount = GridMath.CalculateCellCount(volumeSize, cellSize);
            return GetIndexForWorldPos(pos, cellCount, cellSize, out isOutOfBounds);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool IsMaxDensityAreaExist(bool isReCollect)
        {
            var isAreaExist = PVSOctreeUtils.IsMaxDensityAreaExist(isReCollect);
            return isAreaExist;
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool IsSamplePosValid(Vector3 _samplePos, bool _isMaxDensityAreaExist)
        {
            bool isValid = false;

            if (BakeData.bakeDataVersion == (int)PVSBakeDataVer.Ver3)
            {
                isValid = IsSamplePosValid_UnOctree(_samplePos);
            }
            else if (BakeData.bakeDataVersion >= (int)PVSBakeDataVer.Ver4)
            {
                if (!_isMaxDensityAreaExist)
                {
                    isValid = IsSamplePosValid_UnOctree(_samplePos);
                }
                else
                {
                    isValid = IsSamplePosValid_Octree(_samplePos);
                }
            }

            return isValid;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void CalcSamplePosOffsetMask(List<PVSBakeSettings.SamplingLocation> _samplingLocations)
        {
            if (!checkSamplePosOffsetMask)
                return;

            var ignoreRayCheckDefaultOffsetType = IsIgnoreRayCheckDefaultOffsetType();
            var worldInfoList = FindWorldPosInfoList();
            if (_samplingLocations.Count != worldInfoList.Count)
            {
                Logger.LogError("CalcSamplePosOffsetMask Error: _samplingLocations.Count != worldInfoList.Count");
                return;
            }

            for (int i = 0; i < _samplingLocations.Count; i++)
            {
                var location = _samplingLocations[i];
                var info = worldInfoList[i];
                if (info == null)
                    continue;

                if (!location.Active)
                    continue;

                var pos = location.Position;

                int layer = 1 << PVSConstants.CamBakeLayer | 1 << PVSConstants.CamBakeDisLayer;
                var leafNodeIdx = info.leafNodeIdx;
                var cellSize = BakeData.GetCellSize(leafNodeIdx);

                uint samplePosOffsetMask = 0;
                CalcSamplePosOffsetMask(ref samplePosOffsetMask, PVSSamplePosOffsetMask.UpMask, pos, cellSize, Vector3.up, cellSize.y, layer);
                CalcSamplePosOffsetMask(ref samplePosOffsetMask, PVSSamplePosOffsetMask.DownMask, pos, cellSize, Vector3.down, cellSize.y, layer);
                CalcSamplePosOffsetMask(ref samplePosOffsetMask, PVSSamplePosOffsetMask.LeftMask, pos, cellSize, Vector3.left, cellSize.x, layer);
                CalcSamplePosOffsetMask(ref samplePosOffsetMask, PVSSamplePosOffsetMask.RightMask, pos, cellSize, Vector3.right, cellSize.x, layer);
                CalcSamplePosOffsetMask(ref samplePosOffsetMask, PVSSamplePosOffsetMask.ForwardMask, pos, cellSize, Vector3.forward, cellSize.z, layer);
                CalcSamplePosOffsetMask(ref samplePosOffsetMask, PVSSamplePosOffsetMask.BackMask, pos, cellSize, Vector3.back, cellSize.z, layer);
                PVSVolumeUtils.CalcCamSampleOffsetType(ref samplePosOffsetMask, ignoreRayCheckDefaultOffsetType, pos, layer, cellSize);
                info.samplePosOffsetMask = samplePosOffsetMask;

                info.isInDefaultSampleOffsetArea = false;
                var camSampleOffAreaMgr = PVSCamSamDefaultOffAreaGizmosMgr.S;
                if (camSampleOffAreaMgr != null)
                {
                    var isInArea = camSampleOffAreaMgr.IsPointInGizmos(pos);
                    if (isInArea)
                    {
                        info.isInDefaultSampleOffsetArea = true;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector3 GetSamplingPositionAt(int index, Vector3 cellSize, Space space = Space.Self,
            PVSAlignment _alignment = PVSAlignment.MiddleCenter,
            PVSCoordSystem _coordSystem = PVSCoordSystem.CenterOrigin)
        {
            var position = transform.position;
            var rotation = transform.rotation;
            var samplePos = PVSVolumeUtils.GetSamplingPositionAt(position, rotation, volumeSize, index, cellSize,
                space, _alignment, _coordSystem);
            return samplePos;
        }

        /// <summary>
        /// 
        /// </summary>
        public void CreateCommonBakeDataAsset()
        {
            if (commonBakeData != null)
                return;

            commonBakeData = PVSVolumeUtils.CreateCommonBakeDataAsset();
        }

        /// <summary>
        /// 
        /// </summary>
        public void ResetSampleWorldPosList()
        {
            for (int i = 0; i < sampleWorldPosList.Length; i++)
            {
                sampleWorldPosList[i] = Vector3.zero;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsSampleWorldPosInList(Vector3 _sampleWorldPos, out int _findIndex)
        {
            _findIndex = -1;

            if (PVSCameraMgr.s_AllCameras.Count <= 0)
                return false;

            for (int i = 0; i < sampleWorldPosList.Length; i++)
            {
                var sampleWorldPos = sampleWorldPosList[i];
                if (sampleWorldPos == _sampleWorldPos)
                {
                    _findIndex = i;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetSampleWorldPos(int _index, Vector3 _sampleWorldPos)
        {
            sampleWorldPosList[_index] = _sampleWorldPos;
        }

        /// <summary>
        /// 
        /// </summary>
        bool IsSamplePosValid_Octree(Vector3 _samplePos)
        {
            var dis = Vector3.Distance(_samplePos, singleBakePoint);
            if (dis <= PVSDefine.s_SamplePosValidRange)
                return true;

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        bool IsSamplePosValid_UnOctree(Vector3 _samplePos)
        {
            var dis = Vector3.Distance(_samplePos, singleBakePoint);
            if (dis <= PVSDefine.s_SamplePosValidRange)
                return true;

            return false;
        }
        
        int GetIndexForWorldPos(Vector3 pos, Vector3 cellCount, Vector3 cellSize, out bool isOutOfBounds)
        {
            Quaternion orientation = volumeBakeData == null ? transform.rotation : volumeBakeData.orientation;     
            return GridMath.GetIndexForWorldPos(pos, transform.position, transform.rotation,
                volumeSize, orientation, cellCount, cellSize, out isOutOfBounds, out Vector3 _);
        }

        internal PVSBakeGroup GetRendererForId(int id)
        {
            if(id < 0 || id >= bakeGroups.Length )
            {
                return null;
            }
            return bakeGroups[id];
        }

        public Vector3 HandleSized
        {
            get => volumeBakeBounds.size;
            set => volumeBakeBounds = new Bounds(transform.position, value);
        }

        /// <summary>
        /// 
        /// </summary>
        void CalcSamplePosOffsetMask(ref uint _samplePosOffsetMask, PVSSamplePosOffsetMask _maskType,
            Vector3 _pos, Vector3 _cellSize, Vector3 _dir, float _distance, int _layer)
        {
            List<Vector3> posList = new List<Vector3>();
            posList.Add(_pos);

            AddSamplePosOffsetRayList(posList, _maskType, _pos, _cellSize, 1f);
            AddSamplePosOffsetRayList(posList, _maskType, _pos, _cellSize, 2f);

            bool isAllHit = true;
            foreach (var newPos in posList)
            {
                var isHit = Physics.Raycast(newPos, _dir, _distance, _layer);
                if (!isHit)
                {
                    isAllHit = false;
                    break;
                }
            }

            if (isAllHit)
            {
                _samplePosOffsetMask = _samplePosOffsetMask | (uint)_maskType;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void AddSamplePosOffsetRayList(List<Vector3> posList, PVSSamplePosOffsetMask _maskType, 
            Vector3 _pos, Vector3 _cellSize, float _range)
        {
            if (_maskType == PVSSamplePosOffsetMask.UpMask ||
                _maskType == PVSSamplePosOffsetMask.DownMask)
            {
                posList.Add(_pos + new Vector3(-_cellSize.x * _range, 0f, -_cellSize.z * _range));
                posList.Add(_pos + new Vector3(-_cellSize.x * _range, 0f, _cellSize.z * _range));
                posList.Add(_pos + new Vector3(_cellSize.x * _range, 0f, -_cellSize.z * _range));
                posList.Add(_pos + new Vector3(_cellSize.x * _range, 0f, _cellSize.z * _range));
            }
            else if (_maskType == PVSSamplePosOffsetMask.LeftMask ||
                _maskType == PVSSamplePosOffsetMask.RightMask)
            {
                posList.Add(_pos + new Vector3(0f, -_cellSize.y * _range, -_cellSize.z * _range));
                posList.Add(_pos + new Vector3(0f, -_cellSize.y * _range, _cellSize.z * _range));
                posList.Add(_pos + new Vector3(0f, _cellSize.y * _range, -_cellSize.z * _range));
                posList.Add(_pos + new Vector3(0f, _cellSize.y * _range, _cellSize.z * _range));
            }
            else if (_maskType == PVSSamplePosOffsetMask.ForwardMask ||
                _maskType == PVSSamplePosOffsetMask.BackMask)
            {
                posList.Add(_pos + new Vector3(-_cellSize.x * _range, -_cellSize.y * _range, 0f));
                posList.Add(_pos + new Vector3(-_cellSize.x * _range, _cellSize.y * _range, 0f));
                posList.Add(_pos + new Vector3(_cellSize.x * _range, -_cellSize.y * _range, 0f));
                posList.Add(_pos + new Vector3(_cellSize.x * _range, _cellSize.y * _range, 0f));
            }
        }
    }
}
