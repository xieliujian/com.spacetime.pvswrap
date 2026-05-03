using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Logger = ST.Core.Logging.Logger;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    [ExecuteInEditMode]
    public class PVSCamSamDefaultOffAreaGizmosTest : MonoBehaviour
    {
        /// <summary>
        /// 
        /// </summary>
        public GameObject selGo;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (selGo == null)
                return;

            var pos = selGo.transform.position;
            var isInRange = PVSCamSamDefaultOffAreaGizmosMgr.S.IsPointInGizmos(pos);
            Logger.Log($"[PVSCamSamDefaultOffAreaGizmosTest] isInRange : {isInRange}");
        }
    }
}

