using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class PVSGizmos : MonoBehaviour
    {
        /// <summary>
        /// 
        /// </summary>
        protected static readonly Bounds s_UniformBounds = new Bounds(Vector3.zero, Vector3.one);

        public static bool isHideGizemos = false;

        /// <summary>
        /// 
        /// </summary>
        protected Color m_AreaColor = Color.red;

        /// <summary>
        /// 
        /// </summary>
        protected Mesh m_Mesh;

        /// <summary>
        /// 
        /// </summary>
        [Header("是否打开调试")]
        public bool isOpenDebug;
        [HideInInspector]
        public Vector3 debugPos;

        /// <summary>
        /// 
        /// </summary>
        public void OnDrawGizmos()
        {
            if (!isHideGizemos)
            {
                return;
            }
            OnDrawGizmosReal();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract Bounds GetAreaBox();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_point"></param>
        /// <returns></returns>
        public abstract bool IsPointInGizmos(Vector3 _point);

        /// <summary>
        /// 
        /// </summary>
        protected abstract void OnDrawGizmosReal();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected Bounds GetAABBAreaBox()
        {
            Bounds bound = new Bounds();
            bound.center = transform.position;
            bound.size = transform.localScale;
            return bound;
        }

        /// <summary>
        /// 
        /// </summary>
        protected void DrawAABBArea()
        {
            Gizmos.color = m_AreaColor;

            var pos = transform.position;
            var size = transform.localScale;
            Gizmos.DrawCube(pos, size);
        }

        /// <summary>
        /// 生成实心立方体的网格数据（顶点、三角形、法线）
        /// </summary>
        /// <returns></returns>
        Mesh CreateCubeMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "Editor Solid Cube";

            // 立方体8个顶点（基于本地坐标，中心在原点，大小（1,1,1））
            Vector3[] vertices = new Vector3[8]
            {
            new Vector3(-0.5f, -0.5f, -0.5f), // 0
            new Vector3(0.5f, -0.5f, -0.5f),  // 1
            new Vector3(0.5f, 0.5f, -0.5f),   // 2
            new Vector3(-0.5f, 0.5f, -0.5f),  // 3
            new Vector3(-0.5f, -0.5f, 0.5f),  // 4
            new Vector3(0.5f, -0.5f, 0.5f),   // 5
            new Vector3(0.5f, 0.5f, 0.5f),    // 6
            new Vector3(-0.5f, 0.5f, 0.5f)    // 7
            };

            // 三角形索引（每个面由2个三角形组成，共6个面×2=12个三角形）
            int[] triangles = new int[72]
            {
            // 前面（Z-）
            0, 1, 2,  0, 2, 3,
            // 后面（Z+）
            4, 6, 5,  4, 7, 6,
            // 左面（X-）
            0, 3, 7,  0, 7, 4,
            // 右面（X+）
            1, 5, 6,  1, 6, 2,
            // 下面（Y-）
            0, 5, 1,  0, 4, 5,
            // 上面（Y+）
            3, 2, 6,  3, 6, 7,

            // 前面（Z-）
            0, 2, 1,  0, 3, 2,
            // 后面（Z+）
            4, 5, 6,  4, 6, 7, 
            // 左面（X-）
            0, 7, 3,   0, 4, 7, 
            // 右面（X+）
            1, 6, 5,  1, 2, 6, 
            // 下面（Y-）
            0, 1, 5,  0, 5, 4, 
            // 上面（Y+）
            3, 6, 2,  3, 7, 6,
            };

            // 法线（每个顶点的法线方向，确保光照正确）
            Vector3[] normals = new Vector3[8]
            {
            -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward,
            Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward
            };

            // 赋值网格数据
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.RecalculateBounds(); // 重新计算边界

            return mesh;
        }
    }
}
