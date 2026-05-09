
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
	[PreferBinarySerialization]
	public partial class PVSVolumeBakeData : PVSBakeData
	{
        /// <summary>
        /// 
        /// </summary>
        public bool saveBigVisIndex;

		/// <summary>
		/// 
		/// </summary>
		public Vector3 cellCount;
		public Vector3 cellSize;

        /// <summary>
        /// 
        /// </summary>
        public Vector3 volumeSize;
        public Vector3 volumePos;
        public Quaternion volumeRot;

		/// <summary>
		/// 
		/// </summary>
		public Quaternion orientation;

        /// <summary>
        /// 
        /// </summary>
        public bool octreeAreaExist;

        /// <summary>
        /// 
        /// </summary>
        public byte[] rawDataIdxArray;

        /// <summary>
        /// 
        /// </summary>
        public List<PVSCellInfo> cellInfoList = new List<PVSCellInfo>();

        [NonSerialized]
        public RawData[] rawData;

        [NonSerialized] 
		public List<PVSWorldPosInfo> allSamplePosInfoList = new List<PVSWorldPosInfo>();

		[NonSerialized]
		public Dictionary<int, PVSBakeDataSerialize> exBinData = new Dictionary<int, PVSBakeDataSerialize>();

        [NonSerialized]
        public Dictionary<string, int> exLoadFileLength = new Dictionary<string, int>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="volume"></param>
        public void PreBake(PVSVolume volume)
        {
            var volumeNode = volume.transform;
            volumePos = volumeNode.position;
            volumeRot = volumeNode.rotation;
            volumeSize = volume.volumeSize;
            orientation = volumeNode.rotation;

            cellSize = volume.bakeCellSize;
            cellCount = GridMath.CalculateCellCount(volumeSize, cellSize);

            var sceneOctree = PVSSceneOctree.S;
            octreeAreaExist = sceneOctree.SplitAll(volume, volume.commonBakeData, volumePos, volumeRot, volumeSize, cellSize, false);
            sceneOctree.SplitAllForceArea(volumePos, volumeRot, volumeSize, cellSize);
            sceneOctree.CacheNodeInfo();

            cellCount = sceneOctree.cellNumVec;
            cellSize = sceneOctree.cellSize;

            var nodePosInfoList = sceneOctree.nodePosInfoList;

            rawData = new RawData[nodePosInfoList.Count];
            allSamplePosInfoList = new List<PVSWorldPosInfo>();
            allSamplePosInfoList.AddRange(nodePosInfoList);

            cellInfoList.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_saveBigVisIndex"></param>
        public override void SetSaveBigVisIndex(bool _saveBigVisIndex)
        {
            saveBigVisIndex = _saveBigVisIndex;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_idx"></param>
        public override void RemoveStreamData(int _idx)
		{
            if (_idx < 0)
                return;

			if (exBinData.ContainsKey(_idx))
            {
                exBinData[_idx].Clear();

                exBinData.Remove(_idx);
            }
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nIdx"></param>
        /// <param name="datas"></param>
        public override void FillStreamData(int nIdx, byte[] datas, int pvsSize, bool useNative)
		{
			if (exBinData.ContainsKey(nIdx))
			{
			    exBinData[nIdx] = new PVSBakeDataSerialize(datas, useNative, pvsSize);
			}
			else
			{
				exBinData.Add(nIdx, new PVSBakeDataSerialize(datas, useNative, pvsSize));
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="indices"></param>
        /// <param name="validateData"></param>
        public override void SetRawData(int index, ushort[] indices, bool validateData = true)
        {
	        if (validateData && indices.Length <= 0)
	        {
				Logger.LogWarning("[PVS] Cell without any visible renderers. Should be highly unlikely to happen unless you are performing a multi-scene bake with additional occluders.");
	        }

	        rawData[index] = new RawData()
	        {
		        uncompressed = indices
	        };
        }

        /// <summary>
        /// 
        /// </summary>
        public override void CompleteBake()
        {
	        if (rawData == null || rawData.Length <= 0)
		        return;

            CompleteStreamVer6();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="indices"></param>
        /// <param name="pos"></param>
        /// <param name="_leafNodeIdx"></param>
        /// <param name="_samplePosOffsetMask"></param>
        public override void SampleAtIndex(int index, RapidList<ushort> indices, Vector3 pos, 
            bool _isSampleData,
            out Vector3 _samplePos,
            out int _leafNodeIdx, out uint _samplePosOffsetMask)
        {
            _leafNodeIdx = 0;
            _samplePosOffsetMask = 0;
            _samplePos = Vector3.zero;

            SampleAtIndexByVer6(index, indices, pos, _isSampleData, out _samplePos, out _leafNodeIdx, out _samplePosOffsetMask);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_leafNodeIdx"></param>
        /// <returns></returns>
        public override Vector3 GetCellSize(int _leafNodeIdx)
        {
            var realCellSize = cellSize;
            if (octreeAreaExist)
            {
                realCellSize = PVSVolumeUtils.CalcOctreeCellSize(_leafNodeIdx);
            }

            return realCellSize;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return (rawDataIdxArray != null) && (rawDataIdxArray.Length > 0);
        }

        /// <summary>
        /// 
        /// </summary>
        void CompleteStreamVer6()
        {
            if (octreeAreaExist)
            {
                CompleteStreamVer6_Octree();
            }
            else
            {
                CompalteStreamVer6_UnOctree();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void CompalteStreamVer6_UnOctree()
        {
            var chunkMgr = PVSCompressRawDataChunkMgr.S;
            var idxArray = new uint[rawData.Length];
            var saveChunkIdxArray = new ushort[rawData.Length];
            var saveIdxArray = new uint[rawData.Length];

            chunkMgr.Init(rawData, allSamplePosInfoList, volumeSize);
            chunkMgr.FillIndexArray(idxArray, saveChunkIdxArray, saveIdxArray);

            rawDataIdxArray = PVSBakeDataUtils.SerializeWrite(saveBigVisIndex,
                saveChunkIdxArray, saveIdxArray);
            PVSCompressRawDataUtils.SaveBinData(chunkMgr, exBinData, exLoadFileLength, cellInfoList, volumePos, true);
        }

        /// <summary>
        /// 
        /// </summary>
        void CompleteStreamVer6_Octree()
        {
            var chunkMgr = PVSCompressRawDataChunkMgr.S;
            var sceneOctree = PVSSceneOctree.S;
            var samplePosList = sceneOctree.nodePosInfoList;
            var idxArray = new uint[rawData.Length];
            var saveIdxArray = new uint[rawData.Length];
            var saveChunkIdxArray = new ushort[rawData.Length];

            chunkMgr.Init(rawData, samplePosList, volumeSize);
            chunkMgr.FillIndexArray(idxArray, saveChunkIdxArray, saveIdxArray);
            sceneOctree.ProcessNodeInfo(idxArray, saveChunkIdxArray, saveIdxArray);
            rawDataIdxArray = sceneOctree.SerializeWrite(saveBigVisIndex);
            PVSCompressRawDataUtils.SaveBinData(chunkMgr, exBinData, exLoadFileLength, cellInfoList, volumePos, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="indices"></param>
        /// <param name="vPos"></param>
        /// <param name="_isSampleData"></param>
        /// <param name="_samplePos"></param>
        /// <param name="_leafNodeIdx"></param>
        /// <param name="_samplePosOffsetMask"></param>
        void SampleAtIndexByVer6(int index, RapidList<ushort> indices, Vector3 vPos,
            bool _isSampleData,
            out Vector3 _samplePos,
            out int _leafNodeIdx, out uint _samplePosOffsetMask)
        {
            _leafNodeIdx = 0;
            _samplePosOffsetMask = 0;
            _samplePos = Vector3.zero;

            if (octreeAreaExist)
            {
                SampleAtIndexByVer6_Octree(vPos, indices, _isSampleData, out _samplePos, out _leafNodeIdx, out _samplePosOffsetMask);
            }
            else
            {
                SampleAtIndexByVer6_UnOctree(index, indices, _isSampleData, out _samplePos, out _samplePosOffsetMask);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="_rapidListIndices"></param>
        /// <param name="_samplePos"></param>
        /// <param name="_samplePosOffsetMask"></param>
        void SampleAtIndexByVer6_UnOctree(int index, RapidList<ushort> _rapidListIndices,
            bool _isSampleData,
            out Vector3 _samplePos, out uint _samplePosOffsetMask)
        {
            _samplePosOffsetMask = 0;
            _samplePos = GetSamplePosUnOctree(index);

            if (rawDataIdxArray == null || rawDataIdxArray.Length <= 0)
                return;

            PVSBakeDataUtils.DeserializeRead(saveBigVisIndex, rawDataIdxArray, index, out byte _outChunkIdx,
                out ushort _outRawIdx, out uint _outRawUintIdx);
            if (_outChunkIdx == byte.MaxValue)
                return;

            var realRawIdx = saveBigVisIndex ? _outRawUintIdx : _outRawIdx;
            SampleAtIndexByVer5(_outChunkIdx, (int)realRawIdx, _rapidListIndices, _isSampleData, out _samplePosOffsetMask);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="_rapidListIndices"></param>
        /// <param name="_samplePos"></param>
        /// <param name="_leafNodeIdx"></param>
        /// <param name="_samplePosOffsetMask"></param>
        void SampleAtIndexByVer6_Octree(Vector3 pos, RapidList<ushort> _rapidListIndices,
            bool _isSampleData,
            out Vector3 _samplePos, out int _leafNodeIdx, out uint _samplePosOffsetMask)
        {
            _leafNodeIdx = 0;
            _samplePosOffsetMask = 0;
            _samplePos = Vector3.zero;

            if (rawDataIdxArray == null || rawDataIdxArray.Length <= 0)
                return;

            int leafNodeIdx = 0;
            PVSOctreeUtils.DeserializeRead(saveBigVisIndex, rawDataIdxArray, pos, volumePos,
                volumeRot, volumeSize, cellCount, cellSize, out byte _outChunkIdx, 
                out ushort _outRawIdx, out uint _outRawUintIdx, out _samplePos, ref leafNodeIdx);
            _leafNodeIdx = leafNodeIdx;

            if (_outChunkIdx == byte.MaxValue)
                return;

            var realRawIdx = saveBigVisIndex ? _outRawUintIdx : _outRawIdx;
            SampleAtIndexByVer5(_outChunkIdx, (int)realRawIdx, _rapidListIndices, _isSampleData, out _samplePosOffsetMask);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_selectChunk"></param>
        /// <param name="_realIndex"></param>
        /// <param name="_rapidListIndices"></param>
        /// <param name="_isSampleData"></param>
        /// <param name="samplePosOffsetMask"></param>
        void SampleAtIndexByVer5(int _selectChunk, int _realIndex, RapidList<ushort> _rapidListIndices,
            bool _isSampleData, out uint samplePosOffsetMask)
        {
            samplePosOffsetMask = 0;

            if (exBinData.ContainsKey(_selectChunk))
            {
                PVSBakeDataSerialize streamData = exBinData[_selectChunk];
                if (_rapidListIndices != null)
                {
                    streamData.DeserializeRead_ByVer4(_realIndex, _rapidListIndices, true, _isSampleData, out samplePosOffsetMask);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_cellIndex"></param>
        /// <returns></returns>
        Vector3 GetSamplePosUnOctree(int _cellIndex)
        {
            var _samplePos = PVSVolumeUtils.GetSamplingPositionAt(volumePos, volumeRot,
                volumeSize, _cellIndex, cellSize, Space.World,
                PVSAlignment.MiddleCenter,
                PVSCoordSystem.CenterOrigin);
            return _samplePos;
        }
    }
}

