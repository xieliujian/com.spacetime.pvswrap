using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class PVSGizmosMgr
    {
        /// <summary>
        /// 
        /// </summary>
        List<PVSGizmos> m_GizmosList = new List<PVSGizmos>();

        /// <summary>
        /// 
        /// </summary>
        public abstract void ReCollect();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool HasData()
        {
            return m_GizmosList.Count > 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_bound"></param>
        /// <returns></returns>
        public bool IsCollision(Bounds _bound)
        {
            foreach(var gizmos in m_GizmosList)
            {
                if (gizmos == null)
                    continue;

                var area = gizmos.GetAreaBox();
                if (area.Intersects(_bound))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_point"></param>
        /// <returns></returns>
        public bool IsPointInGizmos(Vector3 _point)
        {
            foreach (var gizmos in m_GizmosList)
            {
                if (gizmos == null)
                    continue;

                var isCol = gizmos.IsPointInGizmos(_point);
                if (isCol)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_Array"></param>
        protected void ReCollect(PVSGizmos[] _Array)
        {
            m_GizmosList.Clear();

            if (_Array == null || _Array.Length <= 0)
                return;

            foreach(var gizmos in _Array)
            {
                if (gizmos == null)
                    continue;

                var gameObj = gizmos.gameObject;
                if (gameObj == null)
                    continue;

                var isVisible = gameObj.activeInHierarchy;
                if (!isVisible)
                    continue;

                if (m_GizmosList.Contains(gizmos))
                    continue;

                m_GizmosList.Add(gizmos);
            }
        }
    }
}

