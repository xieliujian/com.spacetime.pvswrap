using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    [CustomEditor(typeof(PVSCamSamDefaultOffAreaGizmosTest))]
    public class PVSCamSamDefaultOffAreaGizmosTestInspector : Editor
    {
        /// <summary>
        /// 
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("刷新数据"))
            {
                PVSCamSamDefaultOffAreaGizmosMgr.S.ReCollect();
            }
        }
    }
}
