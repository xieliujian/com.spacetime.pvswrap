using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using ST.Core;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSBakeDataSerialize
    {
        /// <summary>
        /// 
        /// </summary>
        public byte[] datas;
        public NativeArray<byte> nativeDatas;
        public bool useNative;

        private bool m_bInit = false;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public PVSBakeDataSerialize(byte[] _data,bool useNative, int pvsSize)
        {
            this.useNative = useNative;
            if (useNative)
            {
                nativeDatas = new NativeArray<byte>(pvsSize, Allocator.Persistent);
                NativeArray<byte>.Copy(_data, nativeDatas, pvsSize);
            }
            else
            {
                datas = _data;
            }
            m_bInit = true;
        }

        public void Clear()
        {
            if(useNative && nativeDatas != null && nativeDatas.IsCreated)
            {
                m_bInit = false;

                nativeDatas.Dispose();
            }
        }
        public static uint BytesToUInt(NativeArray<byte> bytes, int startIndex)
        {
            if (startIndex + 4 > bytes.Length) // 确保有足够的字节来读取一个Int32
                throw new ArgumentException("Not enough bytes to convert to Int32.");

            unsafe
            {
                byte* ptr = (byte*)bytes.GetUnsafeReadOnlyPtr() + startIndex;
                {
                    return UnsafeUtility.ReadArrayElement<uint>(ptr, 0);
                }
            }
        }
        public static int BytesToInt(NativeArray<byte> bytes, int startIndex)
        {
            if (startIndex + 4 > bytes.Length) // 确保有足够的字节来读取一个Int32
                throw new ArgumentException("Not enough bytes to convert to Int32.");

            unsafe
            {
                byte* ptr = (byte*)bytes.GetUnsafeReadOnlyPtr() + startIndex;
                {
                    return UnsafeUtility.ReadArrayElement<int>(ptr, 0);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_realIdx"></param>
        /// <param name="_indices"></param>
        /// <param name="_saveSamplePosOffsetMask"></param>
        /// <param name="_samplePosOffsetMask"></param>
        public void DeserializeRead_ByVer4(int _realIdx, RapidList<ushort> _indices, 
            bool _saveSamplePosOffsetMask, bool _isSampleData, out uint _samplePosOffsetMask)
        {  
            _samplePosOffsetMask = 0;

            if (!m_bInit)
            {
                return;
            }
            var rawDataIdxOff = sizeof(int);
            rawDataIdxOff += _realIdx * sizeof(int) * 2;

            if (_saveSamplePosOffsetMask)
            {
                rawDataIdxOff += _realIdx * sizeof(uint);
            }

            var shareRawDataIdxOff = rawDataIdxOff + sizeof(int);
            var rawDataIdx = GetIntFromBuffer(rawDataIdxOff);
            var shareRawDataIdx = GetIntFromBuffer(shareRawDataIdxOff);

            if (_isSampleData)
            {
                GetIndicesByRawData(rawDataIdx, _indices);
                GetIndicesByRawData(shareRawDataIdx, _indices);
            }

            if(_saveSamplePosOffsetMask)
            {
                var samplePosOffsetMaskOff = shareRawDataIdxOff + sizeof(int);
                _samplePosOffsetMask = GetUIntFromBuffer(samplePosOffsetMaskOff);
            }
        }
        private uint GetUIntFromBuffer(int offset)
        {
            if (useNative)
            {
                return BytesToUInt(nativeDatas, offset);
            }
            uint var = BitConverter.ToUInt32(datas, offset);

            return var;
        }
        private int GetIntFromBuffer(int offset)
        {
            if( useNative )
            {
                return BytesToInt(nativeDatas, offset);
            }
            int var = BitConverter.ToInt32(datas, offset);

            return var;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_realIdx"></param>
        /// <param name="_indices"></param>
        public void DeserializeRead_ByVer3(int _realIdx, RapidList<ushort> _indices)
        {
            var readerOffset = sizeof(int);
            readerOffset += sizeof(int);
            readerOffset += _realIdx * sizeof(int);

            var idxOffset = GetIntFromBuffer(readerOffset);
            
            GetIndicesByRawData(idxOffset, _indices);
        }
        void GetIndicesByRawData_Navtive(int _rawDataIdx, RapidList<ushort> _indices)
        {
            var idxBegin = _rawDataIdx;
            var rowLen = GetIntFromBuffer(idxBegin);

            for (int i = 0; i < rowLen; i++)
            {
                var rowIdxOff = idxBegin + sizeof(int) + i * sizeof(int);
                int rowIndex = GetIntFromBuffer(rowIdxOff);
                int localOffset = idxBegin + rowIndex;

                byte height = nativeDatas[localOffset];
                localOffset += sizeof(byte);

                int lowCount = GetIntFromBuffer(localOffset);
                localOffset += sizeof(int);

                for (int j = 0; j < lowCount; j++)
                {
                    byte low = nativeDatas[localOffset + j];

                    ushort var = GridMath.UnflattenByte2UShort(height, low);
                    _indices.Add(var);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_rawDataLen"></param>
        /// <param name="_indices"></param>
        void GetIndicesByRawData(int _rawDataIdx, RapidList<ushort> _indices)
        {
            if(useNative)
            {
                GetIndicesByRawData_Navtive(_rawDataIdx, _indices);
                return;
            }
            var idxBegin = _rawDataIdx;
            var rowLen = GetIntFromBuffer(idxBegin);

            for (int i = 0; i < rowLen; i++)
            {
                var rowIdxOff = idxBegin + sizeof(int) + i * sizeof(int);
                int rowIndex = GetIntFromBuffer(rowIdxOff);
                int localOffset = idxBegin + rowIndex;

                byte height = datas[localOffset];
                localOffset += sizeof(byte);

                int lowCount = GetIntFromBuffer(localOffset);
                localOffset += sizeof(int);

                for (int j = 0; j < lowCount; j++)
                {
                    byte low = datas[localOffset + j];

                    ushort var = GridMath.UnflattenByte2UShort(height, low);
                    _indices.Add(var);
                }
            }
        }
    }
}

