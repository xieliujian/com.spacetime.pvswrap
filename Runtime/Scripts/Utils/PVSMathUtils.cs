using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public static class PVSMathUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static float CalcVector3Max(Vector3 vec)
        {
            var max = Mathf.Max(vec.y, vec.z);
            max = Mathf.Max(vec.x, max);
            return max;
        }

        /// <summary>
        /// 检测点是否在Mesh内部
        /// </summary>
        /// <param name="localPoint"></param>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static bool IsPointInMesh(Vector3 localPoint, Mesh _mesh)
        {
            Ray ray = new Ray(localPoint, Vector3.right);
            int intersectionCount = 0;

            Mesh newMesh = new Mesh();
            newMesh.vertices = _mesh.vertices;
            newMesh.triangles = _mesh.triangles;
            newMesh.normals = _mesh.normals;
            newMesh.uv = _mesh.uv;

            int[] triangles = newMesh.triangles;
            Vector3[] vertices = newMesh.vertices;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = vertices[triangles[i]];
                Vector3 v1 = vertices[triangles[i + 1]];
                Vector3 v2 = vertices[triangles[i + 2]];

                if (RayIntersectsTriangle(ray, v0, v1, v2))
                {
                    intersectionCount++;
                }
            }

            return intersectionCount % 2 == 1;
        }

        /// <summary>
        /// 检测射线是否与三角形相交（Möller-Trumbore算法）
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        static bool RayIntersectsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;

            Vector3 h = Vector3.Cross(ray.direction, edge2);
            float a = Vector3.Dot(edge1, h);

            if (a > -Mathf.Epsilon && a < Mathf.Epsilon)
                return false;

            float f = 1.0f / a;
            Vector3 s = ray.origin - v0;
            float u = f * Vector3.Dot(s, h);

            if (u < 0.0f || u > 1.0f)
                return false;

            Vector3 q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(ray.direction, q);

            if (v < 0.0f || u + v > 1.0f)
                return false;

            float t = f * Vector3.Dot(edge2, q);

            if (t > Mathf.Epsilon)
                return true;

            return false;
        }
    }
}
