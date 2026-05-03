using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSCompressRawDataChunkMgr
    {
        /// <summary>
        /// 
        /// </summary>
        static PVSCompressRawDataChunkMgr s_Instance;

        /// <summary>
        /// 
        /// </summary>
        public static PVSCompressRawDataChunkMgr S
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new PVSCompressRawDataChunkMgr();
                }

                return s_Instance;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public RawData[] rawDataArray;

        /// <summary>
        /// 
        /// </summary>
        public List<PVSWorldPosInfo> samplePosInfoList;

        /// <summary>
        /// 
        /// </summary>
        public Vector3 volumeSize;

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<int, PVSCompressRawDataChunk> chunkDict = new Dictionary<int, PVSCompressRawDataChunk>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_rawDataArray"></param>
        /// <param name="_samplePosInfoList"></param>
        /// <param name="_volumeSize"></param>
        public void Init(RawData[] _rawDataArray, List<PVSWorldPosInfo> _samplePosInfoList, Vector3 _volumeSize)
        {
            rawDataArray = _rawDataArray;
            samplePosInfoList = _samplePosInfoList;
            volumeSize = _volumeSize;

            var rawGroupUnitRange = PVSCompressRawDataUtils.CalcCompressRawDataGroupUnitRange(_volumeSize);
            InitAllChunk(rawGroupUnitRange);
            CalcAllChunk();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_idxArray"></param>
        /// <param name="_saveChunkIdxArray"></param>
        /// <param name="_saveIdxArray"></param>
        public void FillIndexArray(uint[] _idxArray, ushort[] _saveChunkIdxArray, uint[] _saveIdxArray)
        {
            for (int i = 0; i < _idxArray.Length; i++)
            {
                _idxArray[i] = uint.MaxValue;
                _saveChunkIdxArray[i] = ushort.MaxValue;
                _saveIdxArray[i] = uint.MaxValue;
            }

            foreach (var iter in chunkDict)
            {
                var chunk = iter.Value;
                if (chunk == null)
                    continue;

                foreach(var rawData in chunk.rawDataList)
                {
                    var idx = rawData.sampleIdx;
                    _idxArray[idx] = (uint)idx;
                    _saveChunkIdxArray[idx] = (ushort)rawData.saveChunkIdx;
                    _saveIdxArray[idx] = (uint)rawData.saveIdx;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_sampleIdx"></param>
        /// <param name="_rawData"></param>
        /// <param name="_shareRawData"></param>
        public void GetRawData(int _sampleIdx, 
            out PVSCompressRawData _rawData, 
            out PVSCompressShareRawData _shareRawData,
            out int _rawDataIdx, out int _shareRawDataIdx)
        {
            _shareRawData = null;
            _shareRawDataIdx = int.MaxValue;

            _rawData = FindRawData(_sampleIdx, out int _chunkIdx, out _rawDataIdx);
            if (_rawData == null)
                return;

            var chunk = FindChunk(_chunkIdx);
            if (chunk == null)
                return;

            chunk.GetRawData(_sampleIdx, _rawData, out _shareRawData, out _shareRawDataIdx);
        }

        /// <summary>
        /// 
        /// </summary>
        void CalcAllChunk()
        {
            foreach (var iter in chunkDict)
            {
                var chunk = iter.Value;
                if (chunk == null)
                    continue;

                chunk.CalcAllGroup();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_rawGroupUnitRange"></param>
        void InitAllChunk(Vector3 _rawGroupUnitRange)
        {
            chunkDict.Clear();

            if (rawDataArray.Length != samplePosInfoList.Count)
            {
                Debug.LogError("[PVS][PVSRawDataChunkMgr][InitAllChunk]: rawDataArray.Length != samplePosList.Count");
                return;
            }

            for (int i = 0; i < samplePosInfoList.Count; i++)
            {
                var posInfo = samplePosInfoList[i];
                var samplePos = posInfo.pos;
                var samplePosOffsetMask = posInfo.samplePosOffsetMask;
                var rawData = rawDataArray[i];
                var hasData = rawData.HasData();
                if (!hasData)
                    continue;

                var chunk = CacheChunk(samplePos);
                if (chunk == null)
                {
                    Debug.LogError($"[PVS][PVSRawDataChunkMgr][InitAllChunk]: CacheChunk is null for samplePos: {samplePos}");
                    continue;
                }

                chunk.AddGroup(i, samplePos, samplePosOffsetMask, _rawGroupUnitRange, ref rawData);
            }    
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_sampleIdx"></param>
        /// <param name="_chunkIdx"></param>
        /// <param name="_rawDataIdx"></param>
        /// <returns></returns>
        PVSCompressRawData FindRawData(int _sampleIdx, out int _chunkIdx, out int _rawDataIdx)
        {
            PVSCompressRawData _rawData = null;
            _chunkIdx = _rawDataIdx = int.MaxValue;

            foreach (var iter in chunkDict)
            {
                var chunk = iter.Value;
                if (chunk == null)
                    continue;

                var rawData = chunk.FindRawData(_sampleIdx, out _rawDataIdx);
                if (rawData != null)
                {
                    _rawData = rawData;
                    _chunkIdx = chunk.chunkIdx;
                    break;
                }
            }

            return _rawData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_chunkIdx"></param>
        /// <returns></returns>
        PVSCompressRawDataChunk FindChunk(int _chunkIdx)
        {
            PVSCompressRawDataChunk chunk = null;
            chunkDict.TryGetValue(_chunkIdx, out chunk);
            return chunk;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="samplePos"></param>
        /// <returns></returns>
        PVSCompressRawDataChunk CacheChunk(Vector3 samplePos)
        {
            int chunkIdx = PVSBakeDataUtils.PosToIndex(samplePos);

            var chunk = FindChunk(chunkIdx);
            if (chunk != null)
                return chunk;

            chunk = new PVSCompressRawDataChunk();
            chunk.chunkIdx = chunkIdx;

            chunkDict.Add(chunkIdx, chunk);
            return chunk;
        }
    }
}

