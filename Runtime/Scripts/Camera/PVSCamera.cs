
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using ST.Core;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public partial class PVSCamera : MonoBehaviour
    {
        /// <summary>
        /// 
        /// </summary>
        Camera m_Camera;
        bool m_InvertCulling = false;

        [Tooltip("Allows to take into account neighbor cells to prevent popping issues. It's a great way to compensate for a too sparse bake. This comes with a minor performance impact.\n\n" +
                 "You can achieve even better results without performance implications by baking this in by using the Merge-Downsample feature for your bakes.")]
        [Range(0, 2)]
        public int neighborCellIncludeRadius = 0;
        public PVSCamSampleOffsetType camSampleOffsetType = PVSCamSampleOffsetType.CameraDir;

        /// <summary>
        /// Total number of visible renderers for the current frame.
        /// </summary>
        public static int s_LastFrame = -1;
        public static int s_LastFrameHash = -1;
        public static int s_TotalVisibleNum;
        public static readonly bool[] s_VisibleRenderers = new bool[PVSConstants.MaxRenderers];
        public static bool s_VisibleIsValid = false;

        /// <summary>
        /// 
        /// </summary>
        public bool showInGameStats { get; set; }
        public int lastVisible { get; private set; }
        public int lastTotal { get; private set; }
        public int lastCulled
        {
            get { return lastTotal - lastVisible; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool invertCulling
        {
            get => m_InvertCulling;
            set
            {
                m_InvertCulling = value;
                SetDirty();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="camPos"></param>
        public void PVSCull(Vector3 camPos)
        {
            var offsetArray = GetOffsetArray(camPos, out bool _outCamPosUseDefaultSampleOff);
            int maxSamples = GetMaxSampleNum(offsetArray);

#if DEBUG_MODE
            UnityEngine.Profiling.Profiler.BeginSample("PVSCamera_IsSampleChange");
#endif
            var isSampleChange = IsSampleChange(camPos, maxSamples, offsetArray, _outCamPosUseDefaultSampleOff);
#if DEBUG_MODE
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            if (!isSampleChange)
                return;

#if DEBUG_MODE
            UnityEngine.Profiling.Profiler.BeginSample("PVSCamera_CalcVisRenderArray");
#endif
            CalcVisRenderArray(camPos, maxSamples, offsetArray, _outCamPosUseDefaultSampleOff, out bool hasData);
#if DEBUG_MODE
            UnityEngine.Profiling.Profiler.EndSample();
#endif

#if DEBUG_MODE
            UnityEngine.Profiling.Profiler.BeginSample("PVSCamera_CalcUnVisRenderArray");
#endif
            CalcUnVisRenderArray(hasData);
#if DEBUG_MODE
            UnityEngine.Profiling.Profiler.EndSample();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_camPos"></param>
        /// <param name="_rapidListUshort"></param>
        /// <param name="_rapidListUshort1"></param>
        /// <param name="_volume"></param>
        /// <param name="_checkSamplePosOffsetMask"></param>
        /// <param name="_samplePosOffsetMask"></param>
        public void CalcSampleData(Vector3 _camPos, RapidList<ushort> _rapidListUshort, RapidList<ushort> _rapidListUshort1, 
            PVSVolume _volume, bool _checkSamplePosOffsetMask, out uint _samplePosOffsetMask)
        {
            var offsetArray = GetOffsetArray(_camPos, out bool _outCamPosUseDefaultSampleOff);
            int maxSamples = GetMaxSampleNum(offsetArray);

            CalcSampleData(_rapidListUshort, _rapidListUshort1, this, _volume, _checkSamplePosOffsetMask, neighborCellIncludeRadius,
                _camPos, maxSamples, offsetArray, false, invertCulling, _outCamPosUseDefaultSampleOff, out _, out _samplePosOffsetMask);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rapidListUshort"></param>
        /// <param name="rapidListUshort1"></param>
        /// <param name="volume"></param>
        /// <param name="_camPos"></param>
        /// <param name="_maxSamples"></param>
        /// <param name="_offsetArray"></param>
        /// <param name="_isCalcVisRenderArray"></param>
        /// <param name="_hasData"></param>
        public static void CalcSampleData(RapidList<ushort> rapidListUshort, RapidList<ushort> rapidListUshort1, 
            PVSCamera _camera,
            PVSVolume volume, bool _checkSamplePosOffsetMask, int _neighborCellIncludeRadius,
            Vector3 _camPos, int _maxSamples, Vector3[] _offsetArray, bool _isCalcVisRenderArray, bool _invertCulling,
            bool _camPosUseDefaultSampleOff,
            out bool _hasData, out uint _samplePosOffsetMask)
        {
            _hasData = false;
            _samplePosOffsetMask = 0;
            rapidListUshort.Clear();
            volume.ResetSampleWorldPosList();

            if (_isCalcVisRenderArray)
            {
                s_VisibleIsValid = _hasData;
                System.Array.Clear(s_VisibleRenderers, 0, s_VisibleRenderers.Length);
            }

            var bakeData = volume.volumeBakeData;
            if (bakeData == null)
                return;

            volume.GetIndicesForWorldPos(_camPos, rapidListUshort, true,out Vector3 _samplePos, out int _leafNodeIdx, out _samplePosOffsetMask);
            volume.SetSampleWorldPos(0, _samplePos);
            if (rapidListUshort.count > 0)
            {
                _hasData = true;
            }

            if (_isCalcVisRenderArray)
            {
                s_VisibleIsValid = _hasData;
                FillVisibleRenderersArray(volume, rapidListUshort, _invertCulling);
            }

            var cellSize = bakeData.GetCellSize(_leafNodeIdx);

            for (int neighborIndex = 0; neighborIndex < _maxSamples; ++neighborIndex)
            {
                int j = 1;
                {
                    var offsetVal = _offsetArray[neighborIndex];
                    var isSample = IsNeighborPosSample(_hasData, offsetVal, j, neighborIndex, _checkSamplePosOffsetMask, 
                        _samplePosOffsetMask, _camPosUseDefaultSampleOff);
                    if (!isSample)
                        continue;

#if UNITY_EDITOR
                    if (_camera != null)
                    {
                        var isIgnoreIndex = _camera.IsIgnoreIndex(neighborIndex);
                        if (isIgnoreIndex)
                            continue;
                    }
#endif

                    var worldPos = _camera.GetOffsetWorldPos(_camPos, offsetVal, j, cellSize, _camPosUseDefaultSampleOff);

                    rapidListUshort1.Clear();
                    volume.GetIndicesForWorldPos(worldPos, rapidListUshort1, true, out Vector3 _neighWorldPos, out _, out _);
                    volume.SetSampleWorldPos(neighborIndex, _neighWorldPos);

                    if (_isCalcVisRenderArray)
                    {
                        FillVisibleRenderersArray(volume, rapidListUshort1, _invertCulling);
                    }
                    else
                    {
                        int rapidList1Num = rapidListUshort1.count;
                        for (int indexIndices = 0; indexIndices < rapidList1Num; ++indexIndices)
                        {
                            var ushortVal = rapidListUshort1.buffer[indexIndices];
                            rapidListUshort.Add(ushortVal);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_camPos"></param>
        /// <param name="_offsetVal"></param>
        /// <param name="_j"></param>
        /// <param name="_cellSize"></param>
        /// <param name="_camPosUseDefaultSampleOff"></param>
        /// <returns></returns>
        public Vector3 GetOffsetWorldPos(Vector3 _camPos, Vector3 _offsetVal, int _j, Vector3 _cellSize, bool _camPosUseDefaultSampleOff)
        {
            var worldPos = Vector3.zero;
            var forward = transform.forward;
            var right = transform.right;
            var up = transform.up;

            if (_camPosUseDefaultSampleOff)
            {
                worldPos = GetOffsetWorldPos_Default(_camPos, _offsetVal, _j, _cellSize);
            }
            else if (camSampleOffsetType == PVSCamSampleOffsetType.CameraDir)
            {
                worldPos = GetOffsetWorldPos_CamDir(_camPos, _offsetVal, _j, _cellSize, forward, right, up);
            }
            else
            {
                worldPos = GetOffsetWorldPos_Default(_camPos, _offsetVal, _j, _cellSize);
            }

            return worldPos;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_hasData"></param>
        /// <param name="_offsetVal"></param>
        /// <param name="j"></param>
        /// <param name="neighborIndex"></param>
        /// <param name="_checkSamplePosOffsetMask"></param>
        /// <param name="_samplePosOffsetMask"></param>
        /// <returns></returns>
        public static bool IsNeighborPosSample(bool _hasData, Vector3 _offsetVal, int j, int neighborIndex, 
            bool _checkSamplePosOffsetMask, uint _samplePosOffsetMask, bool _camPosUseDefaultSampleOff)
        {
            if (!_hasData)
                return false;

            if (j == 1 && neighborIndex == 0)
                return false;

            // 摄像机方向采样点忽略遮罩（不好计算，编辑器遮罩是按照轴方向计算，采样也需要按照轴方向）
            if (_camPosUseDefaultSampleOff)
            {
                var isSamplePosOffsetMask = PVSCameraUtils.IsSamplePosOffsetMask(_checkSamplePosOffsetMask, _samplePosOffsetMask, _offsetVal);
                if (isSamplePosOffsetMask)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void RefreshCamSampleOffsetType()
        {
            foreach (var volume in PVSVolume.AllVolumes)
            {
                volume.bChange = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_camPos"></param>
        /// <returns></returns>
        public Vector3[] GetOffsetArray(Vector3 _camPos, out bool _outCamPosUseDefaultSampleOff)
        {
            var offsetArray = PVSDefine.s_CamSamplePosOffsetArray;
            _outCamPosUseDefaultSampleOff = false;

            switch (camSampleOffsetType)
            {
                case PVSCamSampleOffsetType.OnlySelf:
                    offsetArray = PVSDefine.s_CamSamplePosOffsetArrayOnlySelf;
                    break;
                case PVSCamSampleOffsetType.Type1:
                    offsetArray = PVSDefine.s_CamSamplePosOffsetArrayCross;
                    break;
                case PVSCamSampleOffsetType.Type2:
                    offsetArray = PVSDefine.s_CamSamplePosOffsetArrayCross2;
                    break;
                case PVSCamSampleOffsetType.CameraDir:
                    offsetArray = PVSDefine.s_CamSamplePosOffset_CamDir_Array;
                    break;
            }

            foreach (var volume in PVSVolume.AllVolumes)
            {
                if (volume == null)
                    continue;

                var volumeBakeData = volume.volumeBakeData;
                if (volumeBakeData == null)
                    continue;

                if (volumeBakeData.cellSize == Vector3.zero)
                    continue;

                var rapidListUshort = PVSTemp.rapidListUshort;
                rapidListUshort.Clear();
                volume.GetIndicesForWorldPos(_camPos, rapidListUshort, false, out _, out _, out uint _samplePosOffsetMask);
                _outCamPosUseDefaultSampleOff = PVSCameraUtils.IsCamSampleOffsetDefaultType(_samplePosOffsetMask);
                if (_outCamPosUseDefaultSampleOff)
                {
                    offsetArray = PVSDefine.s_CamSamplePosOffsetArray;
                }

                break;
            }

            return offsetArray;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_offsetArray"></param>
        /// <returns></returns>
        public int GetMaxSampleNum(Vector3[] _offsetArray)
        {
            int maxSamples = neighborCellIncludeRadius != 0 ? _offsetArray.Length : 1;
            return maxSamples;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Vector3 GetCamPos()
        {
            Vector3 camPos = transform.position;
            return camPos;
        }

        /// <summary>
        /// Fill the visible renderers array based on the indices in the rapid list.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="rapidListUshort"></param>
        /// <param name="_invertCulling"></param>
        static void FillVisibleRenderersArray(PVSVolume volume, RapidList<ushort> rapidListUshort, bool _invertCulling)
        {
            int rapidListNum = rapidListUshort.count;
            for (int indexIndices = 0; indexIndices < rapidListNum; ++indexIndices)
            {
                int index = rapidListUshort.buffer[indexIndices];

                if (s_VisibleRenderers[index])
                    continue;

                PVSBakeGroup r = volume.GetRendererForId(index);
                if (r == null)
                    continue;

#if !ART_SCENE_PROJECT
                r.Toggle(!_invertCulling);
#endif

                s_VisibleRenderers[index] = true;
                ++s_TotalVisibleNum;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void Awake()
        {
            camSampleOffsetType = PVSCamSampleOffsetType.CameraDir;
            m_Camera = GetComponent<Camera>();
        }
        
        /// <summary>
        /// 
        /// </summary>
        void OnEnable()
        {
            PVSCameraMgr.s_AllCameras.Add(this);
            s_VisibleIsValid = false;
            SetDirty();

#if UNITY_2019_1_OR_NEWER
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
#endif
            Camera.onPreCull += CamPreCull;
        }

        /// <summary>
        /// 
        /// </summary>
        void OnDisable()
        {
            PVSCameraMgr.s_AllCameras.Remove(this);
            s_VisibleIsValid = false;
            SetDirty();

#if UNITY_2019_1_OR_NEWER
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
#endif
            Camera.onPreCull -= CamPreCull;

#if !ART_SCENE_PROJECT
            // Toggle everything back on. Just in case.
            foreach (var volume in PVSVolume.AllVolumes)
            {
                // We also force null checks because OnDisable() might have been called as part of an active destruction process (scene change, etc.)
                volume.ToggleAllRenderers(true, true);
            }
#endif

            lastTotal = 0;
            lastVisible = 0;
            System.Array.Clear(s_VisibleRenderers, 0, s_VisibleRenderers.Length);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="camera"></param>
        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            CamPreCull(camera);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        void CamPreCull(Camera camera)
        {
            if (camera != m_Camera)     // Another camera rendering. We are not interested in it.
                return;

            Vector3 camPos = GetCamPos();
            PVSCull(camPos);
        }

        /// <summary>
        /// 
        /// </summary>
        void SetDirty()
        {
            s_LastFrameHash = -1;
        }   

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_hasData"></param>
        void CalcUnVisRenderArray(bool _hasData)
        {
            // Only want to toggle everything off once per frame.
            // This makes sure that we don't disable renderers that another camera enabled before us.
            if (Time.frameCount != s_LastFrame)
            {
                bool bHide = invertCulling;
                if (!_hasData)
                {
                    bHide = !invertCulling;
                }

#if !ART_SCENE_PROJECT
                foreach (var volume in PVSVolume.AllVolumes)
                {
                    volume.ToggleAllRenderersByState(bHide, s_VisibleRenderers);
                }
#endif

                s_LastFrame = Time.frameCount;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_camPos"></param>
        /// <param name="_maxSamples"></param>
        /// <param name="_offsetArray"></param>
        /// <param name="_hasData"></param>
        void CalcVisRenderArray(Vector3 _camPos, int _maxSamples, Vector3[] _offsetArray, bool _camPosUseDefaultSampleOff, out bool _hasData)
        {
            s_TotalVisibleNum = 0;
            _hasData = false;

            lastTotal = 0;
            lastVisible = 0;
            s_VisibleIsValid = false;

            var rapidListUshort = PVSTemp.rapidListUshort;
            var rapidListUshort1 = PVSTemp.rapidListUshort1;

            foreach (var volume in PVSVolume.AllVolumes)
            {
                if (volume == null)
                    continue;

                var volumeBakeData = volume.volumeBakeData;
                if (volumeBakeData == null)
                    continue;

                if (volumeBakeData.cellSize == Vector3.zero)
                    continue;

#if DEBUG_MODE
                UnityEngine.Profiling.Profiler.BeginSample("PVSCamera_CalcVisRenderArray_CalcSampleData");
#endif
                CalcSampleData(rapidListUshort, rapidListUshort1, this, volume, volume.checkSamplePosOffsetMask, neighborCellIncludeRadius,
                    _camPos, _maxSamples, _offsetArray, true, invertCulling, _camPosUseDefaultSampleOff, out _hasData, out _);
#if DEBUG_MODE
                UnityEngine.Profiling.Profiler.EndSample();
#endif

                lastTotal += volume.RenderersCount;
            }

            lastVisible += s_TotalVisibleNum;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_camPos"></param>
        /// <param name="_maxSamples"></param>
        /// <param name="_offsetArray"></param>
        /// <param name="_camPosUseDefaultSampleOff"></param>
        /// <returns></returns>
        bool IsSampleChange(Vector3 _camPos, int _maxSamples, Vector3[] _offsetArray, bool _camPosUseDefaultSampleOff)
        {
            bool isChange = true;

            // We calculate a hash for all visible cell indices to tell whether our camera is dirty or not.
            int thisFrameHash = 13;

            foreach (var volume in PVSVolume.AllVolumes)
            {
                var bakeData = volume.volumeBakeData;
                if (bakeData == null)
                    continue;

                for (int neighborIndex = 0; neighborIndex < _maxSamples; ++neighborIndex)
                {
                    int j = 1;
                    {
                        unchecked
                        {
                            if (volume.bChange)
                            {
                                SetDirty();
                                volume.bChange = false;
                            }

                            // 八叉树模式4米距离计算改变，非八叉树模式bakeSize作为改变范围
                            var cellSize = bakeData.cellSize;
                            if (bakeData.octreeAreaExist)
                            {
                                cellSize = PVSDefine.s_OctreeMinLeafSizeVec;
                            }

                            var samplePos = GetOffsetWorldPos(_camPos, _offsetArray[neighborIndex], j, cellSize, _camPosUseDefaultSampleOff);
                            int index = volume.GetIndexForWorldPos(samplePos, cellSize, out bool isOutOfBounds);
                            thisFrameHash = thisFrameHash * 17 + index;
                        }
                    }
                }
            }

#if !ART_SCENE_PROJECT
            // Hashes match. Nothing to do.
            if (s_LastFrameHash == thisFrameHash)
            {
                isChange = false;
            }
#endif

#if UNITY_EDITOR
            if (isDebugSamplePosOffset)
            {
                isChange = true;
            }
#endif

            s_LastFrameHash = thisFrameHash;
            return isChange;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_camPos"></param>
        /// <param name="_offsetVal"></param>
        /// <param name="_j"></param>
        /// <param name="_cellSize"></param>
        /// <param name="_forward"></param>
        /// <param name="_right"></param>
        /// <param name="_up"></param>
        /// <returns></returns>
        Vector3 GetOffsetWorldPos_CamDir(Vector3 _camPos, Vector3 _offsetVal, int _j, Vector3 _cellSize,
            Vector3 _forward, Vector3 _right, Vector3 _up)
        {
            var offsetDir = Vector3.zero;

            if (_offsetVal.y > 0)
            {
                offsetDir += _up;
            }
            else if (_offsetVal.y < 0)
            {
                offsetDir -= _up;
            }

            if (_offsetVal.x > 0)
            {
                offsetDir += _right;
            }
            else if (_offsetVal.x < 0)
            {
                offsetDir -= _right;
            }

            if (_offsetVal.z > 0)
            {
                offsetDir += _forward;
            }
            else if (_offsetVal.z < 0)
            {
                offsetDir -= _forward;
            }

            var worldPos = _camPos + Vector3.Scale(offsetDir * _j, _cellSize);
            return worldPos;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_camPos"></param>
        /// <param name="_offsetVal"></param>
        /// <param name="_j"></param>
        /// <param name="_cellSize"></param>
        /// <returns></returns>
        Vector3 GetOffsetWorldPos_Default(Vector3 _camPos, Vector3 _offsetVal, int _j, Vector3 _cellSize)
        {
            var worldPos = _camPos + Vector3.Scale(_offsetVal * _j, _cellSize);
            return worldPos;
        }
    }
}
