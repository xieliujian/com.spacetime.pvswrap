using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ST.PVS
{
    [CustomEditor(typeof(PVSCamSamDefaultOffAreaGizmos))]
    public class PVSCamSamDefaultOffAreaGizmosInspector : PVSGizmosInspector
    {
        /// <summary>
        /// 
        /// </summary>
        public PVSCamSamDefaultOffAreaGizmosInspector()
        {
            m_Desc = PVSDefine.s_CamSamDefaultOffAreaDesc;
        }
    }
}
