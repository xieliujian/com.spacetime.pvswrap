using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    [CustomEditor(typeof(PVSMaxDensityAreaGizmos))]
    public class PVSMaxDensityAreaGizmosInspector : PVSGizmosInspector
    {
        /// <summary>
        /// 
        /// </summary>
        public PVSMaxDensityAreaGizmosInspector()
        {
            m_Desc = PVSDefine.s_MaxDensityAreaDesc;
        }
    }
}
