using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSCompressRawData
    {
        /// <summary>
        /// 
        /// </summary>
        public int sampleIdx;

        /// <summary>
        /// 
        /// </summary>
        public int saveChunkIdx;

        /// <summary>
        /// 在Chunk中的索引位置
        /// </summary>
        public int saveIdx;

        /// <summary>
        /// 在ShareData中的索引位置
        /// </summary>
        public int saveShareDataIdx;

        /// <summary>
        /// 
        /// </summary>
        public Vector3 samplePos;

        /// <summary>
        /// 
        /// </summary>
        public RawData rawData;

        /// <summary>
        /// 
        /// </summary>
        public List<ushort> srcRawData = new List<ushort>();

        /// <summary>
        /// 
        /// </summary>
        public List<ushort> modifyRawData = new List<ushort>();

        /// <summary>
        /// 
        /// </summary>
        public VisibilitySet2 compressData = new VisibilitySet2();

        /// <summary>
        /// 
        /// </summary>
        public uint samplePosOffsetMask;

        /// <summary>
        /// 
        /// </summary>
        public void Init()
        {
            FillSrcRawDataList();
        }

        /// <summary>
        /// 
        /// </summary>
        public void CompressModifyRawData()
        {
            if (modifyRawData.Count <= 0)
                return;

            compressData.data = PVSBakeDataUtils.CompressRawData(modifyRawData.ToArray(), out int _);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_shareDataList"></param>
        public void CalcModifyRawData(HashSet<ushort> _shareDataList)
        {
            modifyRawData.Clear();

            foreach(var data in srcRawData)
            {
                if (_shareDataList.Contains(data))
                    continue;

                modifyRawData.Add(data);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void FillSrcRawDataList()
        {
            srcRawData.Clear();

            foreach (var data in rawData.uncompressed)
            {
                srcRawData.Add(data);
            }
        }
    }
}

