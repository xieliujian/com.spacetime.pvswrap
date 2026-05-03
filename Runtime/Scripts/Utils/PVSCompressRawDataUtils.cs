using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ST.Core;
using Logger = ST.Core.Logging.Logger;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public static class PVSCompressRawDataUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawDataList"></param>
        /// <returns></returns>
        public static HashSet<ushort> FindCommonNumbers(List<PVSCompressRawData> rawDataList)
        {
            if (rawDataList == null || rawDataList.Count == 0)
                return new HashSet<ushort>();

            var srcRawData = rawDataList[0].srcRawData;
            HashSet<ushort> commonNumbers = new HashSet<ushort>(srcRawData);

            for (int i = 1; i < rawDataList.Count; i++)
            {
                srcRawData = rawDataList[i].srcRawData;
                commonNumbers.IntersectWith(srcRawData);

                if (commonNumbers.Count == 0)
                    break;
            }

            return commonNumbers;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawDataArray"></param>
        /// <returns></returns>
        public static List<RawData> GetValidRawDataList(RawData[] rawDataArray)
        {
            List<RawData> tempRawDataList = new List<RawData>();

            for (int i = 0; i < rawDataArray.Length; i++)
            {
                var rawData = rawDataArray[i];
                var hasData = rawData.HasData();
                if (!hasData)
                    continue;

                tempRawDataList.Add(rawData);
            }

            return tempRawDataList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawDataArray"></param>
        /// <returns></returns>
        public static HashSet<ushort> FindCommonNumbers(RawData[] rawDataArray)
        {
            if (rawDataArray == null || rawDataArray.Length == 0)
                return new HashSet<ushort>();

            var srcRawData = rawDataArray[0].uncompressed;
            HashSet<ushort> commonNumbers = new HashSet<ushort>(srcRawData);

            for (int i = 1; i < rawDataArray.Length; i++)
            {
                srcRawData = rawDataArray[i].uncompressed;
                commonNumbers.IntersectWith(srcRawData);

                if (commonNumbers.Count == 0)
                    break;
            }

            return commonNumbers;
        }

        /// <summary>
        /// 计算压缩后的RawDataGroupUnit范围
        /// </summary>
        /// <param name="volumeSize"></param>
        /// <returns></returns>
        public static Vector3 CalcCompressRawDataGroupUnitRange(Vector3 volumeSize)
        {
            var isAreaExist = PVSOctreeUtils.IsMaxDensityAreaExist(true);
            var unitSize = isAreaExist ? PVSDefine.s_OctreeRawDataGroupUnitSize 
                : PVSDefine.s_UnOctreeRawDataGroupUnitSize;
            var unitRange = isAreaExist ? PVSDefine.s_OctreeRawDataGroupUnitRange :
                PVSDefine.s_UnOctreeRawDataGroupUnitRange;

            var cellNum = GridMath.CalculateCellCount(volumeSize, unitSize);
            var maxCellNum = GridMath.CalcMaxValue(cellNum);
            var rawGroupUnitRange = GridMath.CalculateCellCount(volumeSize, maxCellNum);
            rawGroupUnitRange *= unitRange;

            Logger.Log($"[PVS][CalcCompressRawDataGroupUnitRange]: {rawGroupUnitRange}");

            return rawGroupUnitRange;
        }

        /// <summary>
        /// 保存压缩数据
        /// </summary>
        /// <param name="_chunkMgr"></param>
        /// <param name="_exBinData"></param>
        /// <param name="_exLoadFileLength"></param>
        /// <param name="_cellInfoList"></param>
        /// <param name="_volumePos"></param>
        /// <param name="_saveSamplePosOffsetMask"></param>
        public static void SaveBinData(PVSCompressRawDataChunkMgr _chunkMgr, 
            Dictionary<int, PVSBakeDataSerialize> _exBinData,
            Dictionary<string, int> _exLoadFileLength,
            List<PVSCellInfo> _cellInfoList,
            Vector3 _volumePos,
            bool _saveSamplePosOffsetMask)
        {
#if UNITY_EDITOR
            _exBinData.Clear();
            _exLoadFileLength.Clear();

            var binDir = PVSBakeDataUtils.CreateBinFolder(true);
            PVSBakeDataUtils.DeleteAssetsInFolder(binDir);

            foreach (var iter in _chunkMgr.chunkDict)
            {
                var chunk = iter.Value;
                if (chunk == null)
                    continue;

                var chunkIdx = chunk.chunkIdx;
                var binPath = PVSBakeDataUtils.CreateBinPath(binDir, chunkIdx, out string absBinPath);
                var saveBytes = SerializeWriteChunk(chunk, _saveSamplePosOffsetMask);
                PVSBakeDataUtils.CreateBin_SaveFile(absBinPath, saveBytes);
                PVSBakeDataUtils.CreateBin_FillData(_exBinData, _exLoadFileLength, chunkIdx, binPath, saveBytes);

                AddCellInfo(_cellInfoList, _volumePos, chunkIdx, chunk.rawDataList.Count);
            }
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_cellInfoList"></param>
        /// <param name="_volumePos"></param>
        /// <param name="_chunkIdx"></param>
        /// <param name="_pointNum"></param>
        static void AddCellInfo(List<PVSCellInfo> _cellInfoList, Vector3 _volumePos, int _chunkIdx, int _pointNum)
        {
            PVSCellInfo findCellInfo = null;

            foreach (var item in _cellInfoList)
            {
                if (item.cellID == _chunkIdx)
                {
                    findCellInfo = item;
                    break;
                }
            }

            if (findCellInfo == null)
            {
                findCellInfo = new PVSCellInfo();
                _cellInfoList.Add(findCellInfo);
            }

            if (findCellInfo != null)
            {
                findCellInfo.cellID = _chunkIdx;
                findCellInfo.pointNum = _pointNum;

                PVSBakeDataUtils.GetPVSAreaInfoByID(_volumePos, _chunkIdx, out _, out Bounds outBound);
                findCellInfo.bounds = outBound;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="_saveSamplePosOffsetMask"></param>
        /// <returns></returns>
        static RapidList<byte> SerializeWriteChunk(PVSCompressRawDataChunk chunk, bool _saveSamplePosOffsetMask)
        {
            RapidList<int> rawDataIndexOffset = new RapidList<int>();
            RapidList<byte> rawDataSaveByte = new RapidList<byte>();
            RapidList<int> shareRawDataIndexOffset = new RapidList<int>();
            RapidList<byte> shareRawDataSaveByte = new RapidList<byte>();

            RapidList<byte> saveBytes = new RapidList<byte>();

            SerializeWriteChunk_RawData(chunk, out rawDataIndexOffset, out rawDataSaveByte);
            SerializeWriteChunk_ShareRawData(chunk, out shareRawDataIndexOffset, out shareRawDataSaveByte);
            SerializeWriteChunk_ChunkIdx(saveBytes, chunk);
            SerializeWriteChunk_OffsetInfo(saveBytes, chunk, rawDataIndexOffset, rawDataSaveByte, shareRawDataIndexOffset, shareRawDataSaveByte, _saveSamplePosOffsetMask);
            SerializeWriteChunk_SaveData(saveBytes, rawDataSaveByte, shareRawDataSaveByte);

            return saveBytes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_saveBytes"></param>
        /// <param name="_rawDataSaveByte"></param>
        /// <param name="_shareRawDataSaveByte"></param>
        static void SerializeWriteChunk_SaveData(RapidList<byte> _saveBytes, RapidList<byte> _rawDataSaveByte, RapidList<byte> _shareRawDataSaveByte)
        {
            for (int i = 0; i < _rawDataSaveByte.count; i++)
            {
                _saveBytes.Add(_rawDataSaveByte[i]);
            }

            for (int i = 0; i < _shareRawDataSaveByte.count; i++)
            {
                _saveBytes.Add(_shareRawDataSaveByte[i]);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_saveBytes"></param>
        /// <param name="_chunk"></param>
        static void SerializeWriteChunk_ChunkIdx(RapidList<byte> _saveBytes, PVSCompressRawDataChunk _chunk)
        {
            if (_chunk == null)
                return;

            var chunkIdx = _chunk.chunkIdx;
            _saveBytes.AddItems(BitConverter.GetBytes(chunkIdx));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_saveBytes"></param>
        /// <param name="_chunk"></param>
        /// <param name="_rawDataIndexOffset"></param>
        /// <param name="_rawDataSaveByte"></param>
        /// <param name="_shareRawDataIndexOffset"></param>
        /// <param name="_shareRawDataSaveByte"></param>
        static void SerializeWriteChunk_OffsetInfo(RapidList<byte> _saveBytes, PVSCompressRawDataChunk _chunk,
            RapidList<int> _rawDataIndexOffset, RapidList<byte> _rawDataSaveByte,
            RapidList<int> _shareRawDataIndexOffset, RapidList<byte> _shareRawDataSaveByte,
            bool _saveSamplePosOffsetMask)
        {
            var idxSize = _rawDataIndexOffset.count;

            if (_chunk.rawDataList.Count != idxSize)
            {
                Logger.LogError($"[PVS][SerializeWriteChunk_OffsetInfo]: chunk rawDataList count not equal idxSize, " +
                    $"chunkIdx:{_chunk.chunkIdx}, rawDataList count:{_chunk.rawDataList.Count}, idxSize:{idxSize}");
            }

            for (int i = 0; i < idxSize; i++)
            {
                var baseOffset = sizeof(int);

                baseOffset += idxSize * sizeof(int) * 2;
                if (_saveSamplePosOffsetMask)
                {
                    baseOffset += idxSize * sizeof(uint);
                }
               
                var rawDataOffset = baseOffset + _rawDataIndexOffset[i];
                var sizeByte = BitConverter.GetBytes(rawDataOffset);
                _saveBytes.AddItems(sizeByte);

                var rawDataSize = _rawDataSaveByte.count;
                var shareRawDataOffset = baseOffset + rawDataSize;
                var saveShareDataIdx = _chunk.FindShareRawDataIdx(i);
                shareRawDataOffset += _shareRawDataIndexOffset[saveShareDataIdx];
                sizeByte = BitConverter.GetBytes(shareRawDataOffset);
                _saveBytes.AddItems(sizeByte);

                if (_saveSamplePosOffsetMask)
                {
                    var samplePosOffsetMask = _chunk.rawDataList[i].samplePosOffsetMask;
                    sizeByte = BitConverter.GetBytes(samplePosOffsetMask);
                    _saveBytes.AddItems(sizeByte);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_chunk"></param>
        /// <param name="_indexOffset"></param>
        /// <param name="_saveByte"></param>
        static void SerializeWriteChunk_ShareRawData(PVSCompressRawDataChunk _chunk,
            out RapidList<int> _indexOffset,
            out RapidList<byte> _saveByte
            )
        {
            var shareRawDataList = _chunk.shareRawDataList;
            RapidList<int> indexOffset = new RapidList<int>();
            RapidList<byte> saveByte = new RapidList<byte>();

            int nOffset = 0;
            foreach (var rawData in shareRawDataList)
            {
                var compressData = rawData.compressData;
                indexOffset.Add(nOffset);
                nOffset = compressData.SerializeWrite(saveByte);
            }

            _indexOffset = indexOffset;
            _saveByte = saveByte;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_chunk"></param>
        /// <param name="_indexOffset"></param>
        /// <param name="_saveByte"></param>
        static void SerializeWriteChunk_RawData(PVSCompressRawDataChunk _chunk, 
            out RapidList<int> _indexOffset,
            out RapidList<byte> _saveByte
            )
        {
            var rawDataList = _chunk.rawDataList;
            RapidList<int> indexOffset = new RapidList<int>();
            RapidList<byte> saveByte = new RapidList<byte>();

            int nOffset = 0;
            foreach (var rawData in rawDataList)
            {
                var compressData = rawData.compressData;
                indexOffset.Add(nOffset);
                nOffset = compressData.SerializeWrite(saveByte);
            }

            _indexOffset = indexOffset;
            _saveByte = saveByte;
        }
    }
}
