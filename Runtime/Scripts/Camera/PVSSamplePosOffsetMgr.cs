using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ST.Core;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSSamplePosOffsetMgr
    {
        /// <summary>
        /// 
        /// </summary>
        List<PVSSamplePosOffset> m_OffsetList = new List<PVSSamplePosOffset>();

        /// <summary>
        /// 
        /// </summary>
        PVSCamera m_Camera;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_index"></param>
        /// <returns></returns>
        public bool IsIgnoreIndex(int _index)
        {
            foreach (var offset in m_OffsetList)
            {
                if (offset == null)
                    continue;

                if (offset.posIndex == _index)
                    return offset.isIgnore;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_camera"></param>
        public void CollectOffsetList(PVSCamera _camera)
        {
            m_Camera = _camera;
            Clear();
            InternalCollectOffsetList(_camera);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            ClearOffsetList();
            ClearAllBakeGroupMatPropBlock();
        }

        /// <summary>
        /// 
        /// </summary>
        public void ClearAllBakeGroupMatPropBlock()
        {
            var volume = GetVolume();
            if (volume == null)
                return;

            ClearAllBakeGroupMatPropBlock(volume);
        }

        /// <summary>
        /// 
        /// </summary>
        public void ClearOffListSceneShow()
        {
            foreach (var offset in m_OffsetList)
            {
                if (offset == null)
                    continue;

                offset.isSceneShow = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_visible"></param>
        public void SetOffListVisible(bool _visible)
        {
            foreach (var offset in m_OffsetList)
            {
                if (offset == null)
                    continue;

                offset.SetVisible(_visible);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void RefreshSceneShow()
        {
            var _camera = m_Camera;
            var volume = GetVolume();
            if (volume == null)
                return;

            ClearAllBakeGroupMatPropBlock(volume);

            var bakeData = volume.volumeBakeData;
            if (bakeData == null)
                return;

            var hasSceneShow = HasSceneShowInfo();
            if (!hasSceneShow)
                return;

            var camPos = _camera.GetCamPos();
            var offsetArray = _camera.GetOffsetArray(camPos, out bool _outCamPosUseDefaultSampleOff);
            var maxSamples = _camera.GetMaxSampleNum(offsetArray);
            var checkSamplePosOffsetMask = volume.checkSamplePosOffsetMask;

            var rapidListUshort = PVSTemp.rapidListUshort;

            rapidListUshort.Clear();
            volume.GetIndicesForWorldPos(camPos, rapidListUshort, true, out _, out int _leafNodeIdx, out uint samplePosOffsetMask);

            var cellSize = bakeData.GetCellSize(_leafNodeIdx);
            var hasData = rapidListUshort.count > 0;
            var rapidListUshort2 = CalcSceneShowIdSet(maxSamples, offsetArray, hasData, checkSamplePosOffsetMask,
                samplePosOffsetMask, camPos, cellSize, volume, rapidListUshort, _outCamPosUseDefaultSampleOff);

            FillBakeGroupMatPropBlock(volume, rapidListUshort, Color.green);
            FillBakeGroupMatPropBlock(volume, rapidListUshort2, Color.red);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool HasSceneShowInfo()
        {
            foreach (var offset in m_OffsetList)
            {
                if (offset == null)
                    continue;

                if (offset.isSceneShow)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxSamples"></param>
        /// <param name="offsetArray"></param>
        /// <param name="hasData"></param>
        /// <param name="checkSamplePosOffsetMask"></param>
        /// <param name="samplePosOffsetMask"></param>
        /// <param name="camPos"></param>
        /// <param name="cellSize"></param>
        /// <param name="volume"></param>
        /// <param name="rapidListUshort"></param>
        /// <returns></returns>
        HashSet<ushort> CalcSceneShowIdSet(int maxSamples, Vector3[] offsetArray, bool hasData, bool checkSamplePosOffsetMask,
            uint samplePosOffsetMask, Vector3 camPos, Vector3 cellSize, 
            PVSVolume volume, RapidList<ushort> rapidListUshort,
            bool _camPosUseDefaultSampleOff)
        {
            var rapidListUshort1 = PVSTemp.rapidListUshort1;
            var rapidListUshort2 = new HashSet<ushort>(2048);
            rapidListUshort2.Clear();

            for (int neighborIndex = 0; neighborIndex < maxSamples; ++neighborIndex)
            {
                var offsetVal = offsetArray[neighborIndex];
                var isSample = PVSCamera.IsNeighborPosSample(hasData, offsetVal, neighborIndex, neighborIndex,
                    checkSamplePosOffsetMask, samplePosOffsetMask, _camPosUseDefaultSampleOff);
                if (!isSample)
                    continue;

                var offsetInfo = m_OffsetList[neighborIndex];
                if (offsetInfo == null || !offsetInfo.isSceneShow)
                    continue;

                var worldPos = m_Camera.GetOffsetWorldPos(camPos, offsetVal, 1, cellSize, _camPosUseDefaultSampleOff);

                rapidListUshort1.Clear();
                volume.GetIndicesForWorldPos(worldPos, rapidListUshort1, true, out _, out _, out _);

                int rapidList1Num = rapidListUshort1.count;
                for (int indexIndices = 0; indexIndices < rapidList1Num; ++indexIndices)
                {
                    var ushortVal = rapidListUshort1.buffer[indexIndices];
                    if (rapidListUshort.Contains(ushortVal))
                        continue;

                    rapidListUshort2.Add(ushortVal);
                }
            }

            return rapidListUshort2;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        PVSVolume GetVolume()
        {
            var volumeList = PVSVolume.AllVolumes;
            if (volumeList == null || volumeList.Count == 0)
                return null;

            var volume = volumeList[0];
            return volume;
        }

        /// <summary>
        /// 
        /// </summary>
        void ClearOffsetList()
        {
            foreach (var offset in m_OffsetList)
            {
                if (offset == null)
                    continue;

                offset.Clear();
            }

            m_OffsetList.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_volume"></param>
        /// <param name="_idList"></param>
        /// <param name="_color"></param>
        void FillBakeGroupMatPropBlock(PVSVolume _volume, RapidList<ushort> _idList, Color _color)
        {
            var block = CreatePropBlock(_color);

            for (int i = 0; i < _idList.count; i++)
            {
                var id = _idList.buffer[i];
                var bakeGroup = _volume.GetRendererForId(id);
                if (bakeGroup == null)
                    continue;

                bakeGroup.SetMatPropBlockColor(block);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_volume"></param>
        /// <param name="_idSet"></param>
        /// <param name="_color"></param>
        void FillBakeGroupMatPropBlock(PVSVolume _volume, HashSet<ushort> _idSet, Color _color)
        {
            var block = CreatePropBlock(_color);

            foreach (var id in _idSet)
            {
                var bakeGroup = _volume.GetRendererForId(id);
                if (bakeGroup == null)
                    continue;

                bakeGroup.SetMatPropBlockColor(block);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        MaterialPropertyBlock CreatePropBlock(Color _color)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetFloat("_OnlyLODColor", 1f);
            block.SetColor("_LodColor", _color);
            return block;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_volume"></param>
        void ClearAllBakeGroupMatPropBlock(PVSVolume _volume)
        {
            var bakeGroups = _volume.bakeGroups;
            foreach (var bakeGroup in bakeGroups)
            {
                if (bakeGroup == null)
                    continue;

                bakeGroup.ClearMatPropBlock();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_volume"></param>
        /// <param name="_camera"></param>
        /// <param name="offsetArray"></param>
        /// <param name="neighborIndex"></param>
        /// <param name="checkSamplePosOffsetMask"></param>
        /// <param name="samplePosOffsetMask"></param>
        /// <param name="camPos"></param>
        /// <param name="cellSize"></param>
        void CollectOffset(PVSVolume _volume, PVSCamera _camera, Vector3[] offsetArray, 
            int neighborIndex, bool checkSamplePosOffsetMask, 
            uint samplePosOffsetMask, Vector3 camPos, Vector3 cellSize,
            bool _camPosUseDefaultSampleOff)
        {
            var rapidListUshort1 = PVSTemp.rapidListUshort1;
            var offsetVal = offsetArray[neighborIndex];
            var isMainPos = (neighborIndex == 0) ? true : false;
            var isSamplePosOffsetMask = PVSCameraUtils.IsSamplePosOffsetMask(checkSamplePosOffsetMask,
                samplePosOffsetMask, offsetVal);
            var worldPos = _camera.GetOffsetWorldPos(camPos, offsetVal, 1, cellSize, _camPosUseDefaultSampleOff);

            rapidListUshort1.Clear();
            _volume.GetIndicesForWorldPos(worldPos, rapidListUshort1, true, out Vector3 _sampleWorldPos, out _, out _);
            var isEmptyPos = rapidListUshort1.count <= 0 ? true : false;

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = offsetVal.ToString();
            cube.transform.SetParent(_camera.transform);
            cube.transform.position = worldPos;
            cube.transform.rotation = Quaternion.identity;
            cube.transform.localScale = Vector3.one * 0.1f;

            PVSSamplePosOffset offsetInfo = cube.AddComponent<PVSSamplePosOffset>();
            offsetInfo.offsetMgr = this;
            offsetInfo.posIndex = neighborIndex;
            offsetInfo.isMainPos = isMainPos;
            offsetInfo.isOffsetMaskPos = isSamplePosOffsetMask;
            offsetInfo.isEmptyPos = isEmptyPos;
            offsetInfo.cubeName = cube.name;
            offsetInfo.pos = _sampleWorldPos;
            offsetInfo.cellSize = cellSize;
            offsetInfo.go = cube;
            offsetInfo.Init();

            m_OffsetList.Add(offsetInfo);
        }

        /// <summary>
        /// 
        /// </summary>
        void InternalCollectOffsetList(PVSCamera _camera)
        {
            if (_camera == null)
                return;

            var volume = GetVolume();
            if (volume == null)
                return;

            var bakeData = volume.volumeBakeData;
            if (bakeData == null)
                return;

            var camPos = _camera.GetCamPos();
            var offsetArray = _camera.GetOffsetArray(camPos, out bool _outCamPosUseDefaultSampleOff);
            var maxSamples = _camera.GetMaxSampleNum(offsetArray);
            var checkSamplePosOffsetMask = volume.checkSamplePosOffsetMask;

            var rapidListUshort = PVSTemp.rapidListUshort;
            volume.GetIndicesForWorldPos(camPos, rapidListUshort, true, out _, out int _leafNodeIdx, out uint samplePosOffsetMask);
            var cellSize = bakeData.GetCellSize(_leafNodeIdx);

            for (int neighborIndex = 0; neighborIndex < maxSamples; ++neighborIndex)
            {
                CollectOffset(volume, _camera, offsetArray, neighborIndex, checkSamplePosOffsetMask,
                        samplePosOffsetMask, camPos, cellSize, _outCamPosUseDefaultSampleOff);
            }
        }
    }
}

