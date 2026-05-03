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
    public partial class PVSBakeDataUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="chunkSet2"></param>
        /// <param name="exBinData"></param>
        /// <param name="exLoadFileLength"></param>
        public static void CreateBin(Dictionary<int, List<VisibilitySet2>> chunkSet2,
            Dictionary<int, PVSBakeDataSerialize> exBinData,
            Dictionary<string, int> exLoadFileLength)
        {
#if UNITY_EDITOR
            exBinData.Clear();
            exLoadFileLength.Clear();

            var dir = CreateBinFolder(true);

            foreach (var cur in chunkSet2)
            {
                var chunkID = cur.Key;
                var dataAsset = CreateBinPath(dir, chunkID, out string absFilePath);
                var saveBytes = SerializeWriteBin(chunkID, cur.Value);
                CreateBin_SaveFile(absFilePath, saveBytes);
                CreateBin_FillData(exBinData, exLoadFileLength, chunkID, dataAsset, saveBytes);
            }
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_rawData"></param>
        /// <param name="_visibilityIndex"></param>
        /// <param name="_visibleNodeIdxArray"></param>
        /// <param name="_selectChunkIndex"></param>
        /// <param name="_data2"></param>
        public static void CompleteStream_Compress(RawData[] _rawData,
            uint[] _visibilityIndex, uint[] _visibleNodeIdxArray,
            ushort[] _selectChunkIndex, VisibilitySet2[] _data2)
        {
            int nDestSize = 0;
            int nSrcSize = 0;
            int activeCount = 0;

            for (int i = 0; i < _rawData.Length; ++i)
            {
                _selectChunkIndex[i] = ushort.MaxValue;

                PVSTemp.ListUshort.Clear();
                PVSTemp.ListUshort.AddRange(_rawData[i].uncompressed);
                PVSTemp.ListUshort.Sort();
                nDestSize += SetDataCompressed2(_data2, i, PVSTemp.ListUshort.ToArray());
                if (PVSTemp.ListUshort.Count != 0)
                {
                    _visibilityIndex[i] = (uint)i;
                    activeCount++;
                }
                else
                {
                    _visibilityIndex[i] = uint.MaxValue;
                }

                if (_visibleNodeIdxArray != null)
                {
                    _visibleNodeIdxArray[i] = _visibilityIndex[i];
                }

                nSrcSize += _rawData[i].uncompressed.Length * 2;
            }

            Logger.Log("rawdata size " + nSrcSize + " " + "Compressed Size" + nDestSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_chunkSet2"></param>
        /// <param name="_visibilityIndex"></param>
        /// <param name="_selectChunkIndex"></param>
        /// <param name="_data2"></param>
        /// <param name="samplePosList"></param>
        public static void CompleteStream_FillChunk(Dictionary<int, List<VisibilitySet2>> _chunkSet2,
            uint[] _visibilityIndex, ushort[] _selectChunkIndex, VisibilitySet2[] _data2,
            List<PVSWorldPosInfo> allSamplePosInfoList)
        {
            for (int i = 0; i < _data2.Length; i++)
            {
                var posInfo = allSamplePosInfoList[i];
                Vector3 vPos = posInfo.pos;

                int nChunkIdx = PosToIndex(vPos);
                uint nRealIndex = _visibilityIndex[i];
                if (nRealIndex != uint.MaxValue)
                {
                    _visibilityIndex[i] = SetChunkData(_chunkSet2, nChunkIdx, _data2[i]);
                    _selectChunkIndex[i] = (ushort)nChunkIdx;

                    if (_visibilityIndex[i] >= ushort.MaxValue)
                    {
                        Logger.LogError($"[PVS][PVSBakeDataUtils] _visibilityIndex[i] >= ushort.MaxValue" +
                            $"ChunkIdx : {_selectChunkIndex[i]} _visibilityIndex : {_visibilityIndex[i]}");
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data2"></param>
        /// <param name="index"></param>
        /// <param name="indices"></param>
        /// <returns></returns>
        static int SetDataCompressed2(VisibilitySet2[] _data2, int index, ushort[] indices)
        {
            if (indices.Length <= 0)
                return 0;

            var tempDatas = CompressRawData(indices, out int _outSize);

            _data2[index] = new VisibilitySet2();
            _data2[index].data = tempDatas;

            return _outSize;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_chunkSet2"></param>
        /// <param name="nChunkIdx"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        static ushort SetChunkData(Dictionary<int, List<VisibilitySet2>> _chunkSet2,
            int nChunkIdx, VisibilitySet2 data)
        {
            List<VisibilitySet2> curChunkSet2 = null;
            if (_chunkSet2.ContainsKey(nChunkIdx))
            {
                curChunkSet2 = _chunkSet2[nChunkIdx];
            }
            else
            {
                _chunkSet2.Add(nChunkIdx, new List<VisibilitySet2>());
                curChunkSet2 = _chunkSet2[nChunkIdx];
            }

            curChunkSet2.Add(data);
            ushort nIndex = (ushort)(curChunkSet2.Count - 1);
            return nIndex;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chunkIdx"></param>
        /// <param name="chunkSet2"></param>
        /// <returns></returns>
        static RapidList<byte> SerializeWriteBin(int chunkIdx, List<VisibilitySet2> chunkSet2)
        {
            RapidList<byte> saveBytes = new RapidList<byte>();

            saveBytes.AddItems(BitConverter.GetBytes(chunkIdx)); //offset 0

            RapidList<int> indexOffset = new RapidList<int>();
            int nOffset = 0;
            RapidList<byte> curSave = new RapidList<byte>();
            for (int i = 0; i < chunkSet2.Count; i++)
            {
                VisibilitySet2 set2 = chunkSet2[i];
                indexOffset.Add(nOffset);
                nOffset = set2.SerializeWrite(curSave);
            }

            byte[] sizeByte = BitConverter.GetBytes(indexOffset.count);
            saveBytes.AddItems(sizeByte);  //offset 4 = VisibilitySet2数量

            for (int i = 0; i < indexOffset.count; i++)
            {
                int Offset = indexOffset[i] + sizeof(int) * 2 + indexOffset.count * sizeof(int);
                sizeByte = BitConverter.GetBytes(Offset);
                saveBytes.AddItems(sizeByte);
            }

            for (int i = 0; i < curSave.count; i++)
            {
                saveBytes.Add(curSave[i]);
            }

            return saveBytes;
        }
    }
}
