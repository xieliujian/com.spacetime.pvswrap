using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSForceSampleAreaMgrTest : MonoBehaviour
    {
        /// <summary>
        /// Start is called before the first frame update
        /// </summary>
        void Start()
        {

        }

        /// <summary>
        /// Update is called once per frame
        /// </summary>
        void Update()
        {

        }

#if UNITY_EDITOR

        /// <summary>
        /// 
        /// </summary>
        void OnDrawGizmos()
        {
            var areaPosList = PVSForceSampleAreaMgr.S.areaPosList;
            if (areaPosList.Count <= 0)
                return;

            foreach (var areaPos in areaPosList)
            {
                var defaultColor = PVSDefine.s_GizmosVolumeUnOctreeColor;
                UnityEditor.Handles.color = defaultColor;
                UnityEditor.Handles.SphereHandleCap(-1, areaPos, Quaternion.identity, 0.5f, EventType.Repaint);
            }
        }

#endif
    }
}
