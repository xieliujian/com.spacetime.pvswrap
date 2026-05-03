using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class PVSSampleOffsetMaskValid
    {
        /// <summary>
        /// 
        /// </summary>
        public Vector3 pos;
        public Vector3 cellSize;
        public uint samplePosOffsetMask;

        /// <summary>
        /// 
        /// </summary>
        public int visNumUnMask;
        public int visNumMask;

        /// <summary>
        /// 
        /// </summary>
        public int reduceVisNum;
    }
}
