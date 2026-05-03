using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSMaxDensityAreaGizmosMgr : PVSGizmosMgr
    {
        /// <summary>
        /// 
        /// </summary>
        static PVSMaxDensityAreaGizmosMgr s_Instance = null;

        /// <summary>
        /// 
        /// </summary>
        public static PVSGizmosMgr S
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new PVSMaxDensityAreaGizmosMgr();
                }

                return s_Instance;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void ReCollect()
        {
            var srcArray = GameObject.FindObjectsByType<PVSMaxDensityAreaGizmos>(FindObjectsSortMode.None);
            ReCollect(srcArray);
        }
    }
}
