using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ST.Core;

namespace ST.PVS
{
    /// <summary>
    /// Frequently used camera operations.
    /// </summary>
    public static class PVSCameraUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="checkSamplePosOffsetMask"></param>
        /// <param name="_samplePosOffsetMask"></param>
        /// <param name="_offsetVal"></param>
        /// <returns></returns>
        public static bool IsSamplePosOffsetMask(bool checkSamplePosOffsetMask, uint _samplePosOffsetMask, Vector3 _offsetVal)
        {
            if (!checkSamplePosOffsetMask)
                return false;

            bool isMask = false;
            if (_offsetVal.y < 0)
            {
                isMask = IsSamplePosOffsetMask(_samplePosOffsetMask, (uint)PVSSamplePosOffsetMask.DownMask);
            }
            else if (_offsetVal.y > 0)
            {
                isMask = IsSamplePosOffsetMask(_samplePosOffsetMask, (uint)PVSSamplePosOffsetMask.UpMask);
            }
            else if (_offsetVal.x < 0)
            {
                isMask = IsSamplePosOffsetMask(_samplePosOffsetMask, (uint)PVSSamplePosOffsetMask.LeftMask);
            }
            else if (_offsetVal.x > 0)
            {
                isMask = IsSamplePosOffsetMask(_samplePosOffsetMask, (uint)PVSSamplePosOffsetMask.RightMask);
            }
            else if (_offsetVal.z < 0)
            {
                isMask = IsSamplePosOffsetMask(_samplePosOffsetMask, (uint)PVSSamplePosOffsetMask.BackMask);
            }
            else if (_offsetVal.z > 0)
            {
                isMask = IsSamplePosOffsetMask(_samplePosOffsetMask, (uint)PVSSamplePosOffsetMask.ForwardMask);
            }

            return isMask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_samplePosOffsetMask"></param>
        /// <returns></returns>
        public static bool IsCamSampleOffsetDefaultType(uint _samplePosOffsetMask)
        {
            var isDefaultTypeValid = IsSamplePosOffsetMask(_samplePosOffsetMask, (uint)PVSSamplePosOffsetMask.CamSampleOffsetDefaultType);
            return isDefaultTypeValid;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_samplePosOffsetMask"></param>
        /// <returns></returns>
        public static uint SaveCamSampleOffsetDefaultType(uint _samplePosOffsetMask)
        {
            var val = _samplePosOffsetMask | (uint)PVSSamplePosOffsetMask.CamSampleOffsetDefaultType;
            return val;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_samplePosOffsetMask"></param>
        /// <returns></returns>
        public static bool IsSamplePosOffsetMaskValid(uint _samplePosOffsetMask)
        {
            var isDownMaskValid = IsSamplePosOffsetMask(_samplePosOffsetMask, (uint)PVSSamplePosOffsetMask.DownMask);
            var isUpMaskValid = IsSamplePosOffsetMask(_samplePosOffsetMask, (uint)PVSSamplePosOffsetMask.UpMask);
            var isLeftMaskValid = IsSamplePosOffsetMask(_samplePosOffsetMask, (uint)PVSSamplePosOffsetMask.LeftMask);
            var isRightMaskValid = IsSamplePosOffsetMask(_samplePosOffsetMask, (uint)PVSSamplePosOffsetMask.RightMask);
            var isBackMaskValid = IsSamplePosOffsetMask(_samplePosOffsetMask, (uint)PVSSamplePosOffsetMask.BackMask);
            var isForwardMaskValid = IsSamplePosOffsetMask(_samplePosOffsetMask, (uint)PVSSamplePosOffsetMask.ForwardMask);

            var isValid = isDownMaskValid || isUpMaskValid || isLeftMaskValid || isRightMaskValid || isBackMaskValid || isForwardMaskValid;
            return isValid;
        }

        /// <summary>
        /// Display the description of the sample position offset mask.
        /// </summary>
        /// <param name="_cullingVolume"></param>
        /// <param name="_worldPos"></param>
        /// <param name="_localPos"></param>
        /// <param name="_samplePosOffsetMask"></param>
        public static void ShowSamplePosOffsetMaskDesc(PVSVolume _cullingVolume, Vector3 _worldPos, 
            Vector3 _localPos, out uint _samplePosOffsetMask)
        {
            _samplePosOffsetMask = 0;

            var commonBakeData = _cullingVolume.commonBakeData;
            if (commonBakeData == null)
                return;

            var valid = commonBakeData.FindSampleOffsetMaskInfo(_worldPos);
            if (valid == null)
                return;

            var samplePosOffsetMask = valid.samplePosOffsetMask;
            var desc = GetSamplePosOffsetMaskDesc(samplePosOffsetMask);
            _samplePosOffsetMask = samplePosOffsetMask;

#if UNITY_EDITOR
            var namePos = _localPos;

            var customStyle = new GUIStyle();
            customStyle.fontSize = 24;
            customStyle.normal.textColor = Color.red;
            customStyle.alignment = TextAnchor.MiddleCenter;
            UnityEditor.Handles.Label(namePos, desc, customStyle);
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_samplePosOffsetMask"></param>
        /// <returns></returns>
        public static string GetSamplePosOffsetMaskDesc(uint _samplePosOffsetMask)
        {
            string desc = "";

            var valArray = Enum.GetValues(typeof(PVSSamplePosOffsetMask));
            var nameArray = Enum.GetNames(typeof(PVSSamplePosOffsetMask));
            var num = PVSDefine.s_PVSSamplePosOffsetMaskNameArray.Length;

            for (int i = 0; i < num; i++)
            {
                var enumVal = (PVSSamplePosOffsetMask)valArray.GetValue(i);
                var val = (uint)enumVal;
                var name = PVSDefine.s_PVSSamplePosOffsetMaskNameArray[i];
                var isMask = IsSamplePosOffsetMask(_samplePosOffsetMask, val);
                if (isMask)
                {
                    desc += name;
                }
            }

            return desc;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_unContainList"></param>
        /// <returns></returns>
        public static RapidList<ushort> GetListUnRepeat(RapidList<ushort> _srcList, RapidList<ushort> _unContainList)
        {
            RapidList<ushort> list = new RapidList<ushort>();

            for (int i = 0; i < _srcList.count; i++)
            {
                var val = _srcList[i];
                if (_unContainList.Contains(val))
                    continue;

                if (!list.Contains(val))
                {
                    list.Add(val);
                }
            }

            return list;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_rapidListUshort"></param>
        /// <returns></returns>
        public static int GetListUnRepeatNum(RapidList<ushort> _rapidListUshort)
        {
            List<ushort> list = new List<ushort>();

            for (int i = 0; i < _rapidListUshort.count; i++)
            {
                var val = _rapidListUshort[i];
                if (!list.Contains(val))
                {
                    list.Add(val);
                }
            }

            return list.Count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_samplePosOffsetMask"></param>
        /// <param name="_maskType"></param>
        /// <returns></returns>
        static bool IsSamplePosOffsetMask(uint _samplePosOffsetMask, uint _maskType)
        {
            var isMask = (_maskType & _samplePosOffsetMask) != 0;
            return isMask;
        }
    }
}
