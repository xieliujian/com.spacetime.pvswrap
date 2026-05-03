using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSIgnorePointAreaGizmos : PVSGizmos
    {
        /// <summary>
        /// 
        /// </summary>
        public PVSIgnorePointAreaGizmos()
        {
            var color = PVSDefine.s_IgnorePointAreaColor;
            var alpha = PVSDefine.s_GizmosAreaAlpha;

            alpha = 1f;
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
        public override bool IsPointInGizmos(Vector3 _point)
        {
            Matrix4x4 matrix4X4 = transform.localToWorldMatrix.inverse;
            Vector3 localPoint = matrix4X4.MultiplyPoint3x4(_point);
            var isInRange = s_UniformBounds.Contains(localPoint);
            return isInRange;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnDrawGizmosReal()
        {
            var oldColor = Gizmos.color;
            var oldMatrix = Gizmos.matrix;

            Gizmos.color = m_AreaColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);

            Gizmos.matrix = oldMatrix;
            Gizmos.color = oldColor;
        }
    }
}

