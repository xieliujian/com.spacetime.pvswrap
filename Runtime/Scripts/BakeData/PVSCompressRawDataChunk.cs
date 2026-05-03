using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ST.Core;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSCompressRawDataChunk
    {
        /// <summary>
        /// 
        /// </summary>
        public int chunkIdx;

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Vector3Int, PVSCompressRawDataGroup> groupDict = new Dictionary<Vector3Int, PVSCompressRawDataGroup>();

        /// <summary>
        /// 
        /// </summary>
        public List<PVSCompressRawData> rawDataList = new List<PVSCompressRawData>();

        /// <summary>
        /// 
        /// </summary>
        public List<PVSCompressShareRawData> shareRawDataList = new List<PVSCompressShareRawData>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_sampleIdx"></param>
        /// <param name="_samplePos"></param>
        /// <param name="_samplePosOffsetMask"></param>
        /// <param name="_rawGroupUnitRange"></param>
        /// <param name="_rawData"></param>
        public void AddGroup(int _sampleIdx, Vector3 _samplePos, uint _samplePosOffsetMask, Vector3 _rawGroupUnitRange, ref RawData _rawData)
        {
            GridMath.UnflattenToXYZ(_samplePos, _rawGroupUnitRange, out int x, out int y, out int z);
            Vector3Int groupKey = new Vector3Int(x, y, z);

            var rawGroup = CacheRawDataGroup(groupKey);
            if (rawGroup == null)
            {
                Debug.LogError("[PVS][PVSCompressRawDataChunk][AddGroup]: rawGroup == null");
                return;
            }

            var shareRawData = rawGroup.shareRawData;
            if (!shareRawDataList.Contains(shareRawData))
            {
                shareRawData.saveIdx = shareRawDataList.Count;
                shareRawDataList.Add(shareRawData);
            }

            var rawData = rawGroup.Fill(_sampleIdx, _samplePos, _samplePosOffsetMask, ref _rawData);
            rawData.saveChunkIdx = chunkIdx;
            rawData.saveIdx = rawDataList.Count;
            rawData.saveShareDataIdx = shareRawData.saveIdx;

            rawDataList.Add(rawData);
        }

        /// <summary>
        /// 
        /// </summary>
        public void CalcAllGroup()
        {
            foreach(var iter in groupDict)
            {
                var group = iter.Value;
                if (group == null)
                    continue;

                group.Calc();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_rawDataIdx"></param>
        /// <returns></returns>
        public int FindShareRawDataIdx(int _rawDataIdx)
        {
            var rawData = rawDataList[_rawDataIdx];
            var saveShareDataIdx = rawData.saveShareDataIdx;
            return saveShareDataIdx;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_sampleIdx"></param>
        /// <param name="_rawData"></param>
        /// <param name="_shareRawData"></param>
        /// <param name="_shareRawDataIdx"></param>
        public void GetRawData(int _sampleIdx, PVSCompressRawData _rawData,
                out PVSCompressShareRawData _shareRawData, out int _shareRawDataIdx)
        {
            _shareRawDataIdx = _rawData.saveShareDataIdx;
            _shareRawData = shareRawDataList[_shareRawDataIdx];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_sampleIdx"></param>
        /// <param name="_rawDataIdx"></param>
        /// <returns></returns>
        public PVSCompressRawData FindRawData(int _sampleIdx, out int _rawDataIdx)
        {
            _rawDataIdx = int.MaxValue;

            for (int i = 0; i < rawDataList.Count; i++)
            {
                var rawData = rawDataList[i];
                if (rawData.sampleIdx == _sampleIdx)
                {
                    _rawDataIdx = i;
                    return rawData;
                }
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupKey"></param>
        /// <returns></returns>
        PVSCompressRawDataGroup CacheRawDataGroup(Vector3Int groupKey)
        {
            PVSCompressRawDataGroup rawGroup = null;
            groupDict.TryGetValue(groupKey, out rawGroup);

            if (rawGroup == null)
            {
                rawGroup = new PVSCompressRawDataGroup();
                rawGroup.idx = groupKey;

                groupDict.Add(groupKey, rawGroup);
            }

            return rawGroup;
        }
    }
}
