using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public partial class PVSCamera
    {
#if UNITY_EDITOR

        /// <summary>
        /// 
        /// </summary>
        PVSSamplePosOffsetMgr m_PosOffsetMgr = new PVSSamplePosOffsetMgr();

        /// <summary>
        /// 
        /// </summary>
        public bool isDebugSamplePosOffset;
        public bool isTestIgnorePoint;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_index"></param>
        /// <returns></returns>
        public bool IsIgnoreIndex(int _index)
        {
            if (!isDebugSamplePosOffset)
                return false;

            if (!isTestIgnorePoint)
                return false;

            var isIgnore = m_PosOffsetMgr.IsIgnoreIndex(_index);
            return isIgnore;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="_isTest"></param>
        public void RefreshTestIgnorePoint(bool _isTest)
        {
            if (_isTest)
            {
                m_PosOffsetMgr.ClearAllBakeGroupMatPropBlock();
                m_PosOffsetMgr.ClearOffListSceneShow();
                m_PosOffsetMgr.SetOffListVisible(false);
            }
            else
            {
                m_PosOffsetMgr.SetOffListVisible(true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_isDebug"></param>
        public void RefreshDebugSamplePosOffset(bool _isDebug)
        {
            if (_isDebug)
            {
                isTestIgnorePoint = false;
                m_PosOffsetMgr.CollectOffsetList(this);
            }
            else
            {
                m_PosOffsetMgr.Clear();
            }
        }

#endif
    }
}

