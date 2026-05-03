using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSCompressShareRawData
    {
        /// <summary>
        /// 
        /// </summary>
        public int saveIdx;

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
        /// <param name="_shareDataList"></param>
        public void CalcModifyRawData(HashSet<ushort> _shareDataList)
        {
            modifyRawData.Clear();
            modifyRawData.AddRange(_shareDataList);
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
    }
}

