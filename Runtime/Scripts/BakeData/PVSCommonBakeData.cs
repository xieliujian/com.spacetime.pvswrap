using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    [PreferBinarySerialization]
    public class PVSCommonBakeData : ScriptableObject
    {
        /// <summary>
        /// 
        /// </summary>
        public List<Vector3Int> saveManualSplitPosList = new List<Vector3Int>();

        /// <summary>
        /// 
        /// </summary>
        public List<PVSSampleOffsetMaskValid> sampleOffsetMaskList = new List<PVSSampleOffsetMaskValid>();

        /// <summary>
        /// 
        /// </summary>
        [NonSerialized]
        public Dictionary<Vector3, PVSSampleOffsetMaskValid> sampleOffsetMaskDict = 
            new Dictionary<Vector3, PVSSampleOffsetMaskValid>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_pos"></param>
        /// <returns></returns>
        public bool IsSameManualSplitPoint(Vector3 _pos)
        {
            var calcPos = PVSOctreeUtils.CalcManualSplitPoint(_pos);
            var isExist = saveManualSplitPosList.Contains(calcPos);
            return isExist;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_pos"></param>
        /// <returns></returns>
        public PVSSampleOffsetMaskValid FindSampleOffsetMaskInfo(Vector3 _pos)
        {
            PVSSampleOffsetMaskValid _info = null;
            sampleOffsetMaskDict.TryGetValue(_pos, out _info);
            return _info;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsSampleOffsetMaskDictFill()
        {
            var isFill = sampleOffsetMaskDict.Count > 0;
            return isFill;
        }

        /// <summary>
        /// 
        /// </summary>
        public void RebuildSampleOffsetMaskDict()
        {
            sampleOffsetMaskDict.Clear();

            foreach (var item in sampleOffsetMaskList)
            {
                if (item == null) 
                    continue;

                var pos = item.pos;

                if (!sampleOffsetMaskDict.ContainsKey(pos))
                {
                    sampleOffsetMaskDict.Add(pos, item);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void ClearManualSplitPointList()
        {
            saveManualSplitPosList.Clear();

            SetAssetDirty();
        }

        /// <summary>
        /// 
        /// </summary>
        public void ClearSampleOffsetMaskList()
        {
            sampleOffsetMaskList.Clear();

            SetAssetDirty();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_pos"></param>
        public void SwitchManualSplitPoint(Vector3 _pos)
        {
            var calcPos = PVSOctreeUtils.CalcManualSplitPoint(_pos);
            var isExist = saveManualSplitPosList.Contains(calcPos);
            if (isExist)
            {
                saveManualSplitPosList.Remove(calcPos);
            }
            else
            {
                saveManualSplitPosList.Add(calcPos);
            }

            SetAssetDirty();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_samplePos"></param>
        /// <param name="_cellSize"></param>
        /// <param name="_samplePosOffsetMask"></param>
        public void AddSampleOffsetMaskList(Vector3 _samplePos, Vector3 _cellSize, uint _samplePosOffsetMask)
        {
            PVSSampleOffsetMaskValid validInfo = new PVSSampleOffsetMaskValid();
            validInfo.pos = _samplePos;
            validInfo.cellSize = _cellSize;
            validInfo.samplePosOffsetMask = _samplePosOffsetMask;
            sampleOffsetMaskList.Add(validInfo);

            SetAssetDirty();
        }

        /// <summary>
        /// 
        /// </summary>
        void SetAssetDirty()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}

