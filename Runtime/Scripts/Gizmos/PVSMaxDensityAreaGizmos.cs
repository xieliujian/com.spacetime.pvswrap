using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSMaxDensityAreaGizmos : PVSGizmos
    {
        /// <summary>
        /// 
        /// </summary>
        public PVSMaxDensityAreaGizmos()
        {
            var color = PVSDefine.s_MaxDensityAreaColor;
            var alpha = PVSDefine.s_GizmosAreaAlpha;
            m_AreaColor = new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override Bounds GetAreaBox()
        {
            var bound = GetAABBAreaBox();
            return bound;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_point"></param>
        /// <returns></returns>
        public override bool IsPointInGizmos(Vector3 _point) => throw new System.NotImplementedException();

        /// <summary>
        /// 
        /// </summary>
        protected override void OnDrawGizmosReal()
        {
            var oldColor = Gizmos.color;
            DrawAABBArea();
            Gizmos.color = oldColor;
        }
    }
}

