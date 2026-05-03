using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSRawData
    {
        /// <summary>
        /// 未压缩数据
        /// </summary>
        public ushort[] uncompressed;

        /// <summary>
        /// 是否有数据
        /// </summary>
        /// <returns></returns>
        public bool HasData()
        {
            var hasData = uncompressed != null && uncompressed.Length > 0;
            return hasData;
        }
    }
}
