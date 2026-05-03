using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ST.Core;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public struct VisibilitySetRow
    {
        public byte height;
        public byte[] lowData;

        //编辑器使用，暂时不考虑效率
        public int SerializeWrite(RapidList<byte> saveBytes)
        {
            saveBytes.Add(height);
            int size = lowData.Length;
            byte[] sizeByte = BitConverter.GetBytes(size);
            saveBytes.AddItems(sizeByte);
            saveBytes.AddItems(lowData);

            return saveBytes.count;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public struct VisibilitySet2
    {
        public VisibilitySetRow[] data;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool HasData()
        {
            var hasData = data != null && data.Length > 0;
            return hasData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="saveBytes"></param>
        /// <returns></returns>
        public int SerializeWrite(RapidList<byte> saveBytes)
        {
            RapidList<int> indexOffset = new RapidList<int>();
            int nOffset = 0;

            RapidList<byte> curSave = new RapidList<byte>();
            var hasData = HasData();
            if (hasData)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    indexOffset.Add(nOffset);
                    nOffset = data[i].SerializeWrite(curSave);
                }
            }

            byte[] sizeByte = BitConverter.GetBytes(indexOffset.count);
            saveBytes.AddItems(sizeByte);

            for (int i = 0; i < indexOffset.count; i++)
            {
                int offset = indexOffset[i] + sizeof(int) + indexOffset.count * sizeof(int);
                sizeByte = BitConverter.GetBytes(offset);
                saveBytes.AddItems(sizeByte);
            }

            for (int i = 0; i < curSave.count; i++)
            {
                saveBytes.Add(curSave[i]);
            }

            return saveBytes.count;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public struct VisibilityIndex
    {
        public ushort[] data;
    }

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public struct VisibilityUintIndex
    {
        public uint[] data;
    }

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public struct ChunkIndex
    {
        public ushort[] data;
    }

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public struct RawData
    {
        public ushort[] uncompressed;

        /// <summary>
        /// 是否有数据
        /// </summary>
        /// <returns></returns>
        public bool HasData()
        {
            return uncompressed != null && uncompressed.Length > 0;
        }
    }
}
