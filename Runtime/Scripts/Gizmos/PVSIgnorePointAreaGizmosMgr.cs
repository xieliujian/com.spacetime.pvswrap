using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSIgnorePointAreaGizmosMgr : PVSGizmosMgr
    {
        /// <summary>
        /// 
        /// </summary>
        static PVSIgnorePointAreaGizmosMgr s_Instance = null;

        /// <summary>
        /// 
        /// </summary>
        public static PVSIgnorePointAreaGizmosMgr S
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new PVSIgnorePointAreaGizmosMgr();
                }

                return s_Instance;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void ReCollect()
        {
            var srcArray = GameObject.FindObjectsByType<PVSIgnorePointAreaGizmos>(FindObjectsSortMode.None);
            ReCollect(srcArray);
        }
    }
}

