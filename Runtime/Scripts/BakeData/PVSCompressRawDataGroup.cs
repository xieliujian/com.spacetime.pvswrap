using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSCompressRawDataGroup
    {
        /// <summary>
        /// 
        /// </summary>
        public Vector3Int idx;

        /// <summary>
        /// 
        /// </summary>
        public List<PVSCompressRawData> rawDataList = new List<PVSCompressRawData>();

        /// <summary>
        /// 
        /// </summary>
        public PVSCompressShareRawData shareRawData = new PVSCompressShareRawData();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_sampleIdx"></param>
        /// <param name="_samplePos"></param>
        /// <param name="_rawData"></param>
        public PVSCompressRawData Fill(int _sampleIdx, Vector3 _samplePos, uint _samplePosOffsetMask, ref RawData _rawData)
        {
            var rawData = new PVSCompressRawData();
            rawData.sampleIdx = _sampleIdx;
            rawData.samplePos = _samplePos;
            rawData.samplePosOffsetMask = _samplePosOffsetMask;
            rawData.rawData = _rawData;
            rawData.Init();

            rawDataList.Add(rawData);
            return rawData;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Calc()
        {
            var shareDataList = PVSCompressRawDataUtils.FindCommonNumbers(rawDataList);
            CalcModifyRawData(shareDataList);
            CompressModifyRawData();
        }

        /// <summary>
        /// 
        /// </summary>
        void CompressModifyRawData()
        {
            shareRawData.CompressModifyRawData();

            foreach (var rawData in rawDataList)
            {
                if (rawData == null)
                    continue;

                rawData.CompressModifyRawData();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_shareDataList"></param>
        void CalcModifyRawData(HashSet<ushort> _shareDataList)
        {
            shareRawData.CalcModifyRawData(_shareDataList);

            foreach (var rawData in rawDataList)
            {
                if (rawData == null)
                    continue;

                rawData.CalcModifyRawData(_shareDataList);
            }
        }
    }
}

