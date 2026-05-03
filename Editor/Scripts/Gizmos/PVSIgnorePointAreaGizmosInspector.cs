using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    [CustomEditor(typeof(PVSIgnorePointAreaGizmos))]
    public class PVSIgnorePointAreaGizmosInspector : PVSGizmosInspector
    {
        /// <summary>
        /// 
        /// </summary>
        public PVSIgnorePointAreaGizmosInspector()
        {
            m_Desc = PVSDefine.s_IgnorePointAreaDesc;
        }
    }
}
