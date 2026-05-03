using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSCamSamDefaultOffAreaGizmosMgr : PVSGizmosMgr
    {
        /// <summary>
        /// 
        /// </summary>
        static PVSCamSamDefaultOffAreaGizmosMgr s_Instance = null;

        /// <summary>
        /// 
        /// </summary>
        public static PVSCamSamDefaultOffAreaGizmosMgr S
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new PVSCamSamDefaultOffAreaGizmosMgr();
                }

                return s_Instance;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void ReCollect()
        {
            var srcArray = GameObject.FindObjectsByType<PVSCamSamDefaultOffAreaGizmos>(FindObjectsSortMode.None);
            ReCollect(srcArray);
        }
    }
}

