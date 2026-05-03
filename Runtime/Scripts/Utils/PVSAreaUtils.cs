
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public static class PVSAreaUtils
    {
        /// <summary>
        /// 获取当前GameObject及其子物体上所有Mesh的世界坐标顶点
        /// </summary>
        /// <returns></returns>
        public static List<Vector3> CollectWorldVertices(List<GameObject> _gameObjList)
        {
            // 清空之前的顶点数据
            List<Vector3> worldVertices = new List<Vector3>();

            foreach(var gameObj in _gameObjList)
            {
                // 获取所有子物体中的Mesh相关组件
                var componentArray = gameObj.GetComponentsInChildren(typeof(MeshFilter));

                // 遍历所有Mesh组件并收集顶点
                foreach (var component in componentArray)
                {
                    if (component == null)
                        continue;

                    var meshFilter = component as MeshFilter;
                    if (meshFilter == null)
                        continue;

                    // 处理MeshFilter组件
                    CollectVerticesFromMeshFilter(worldVertices, meshFilter);
                }
            }

            return worldVertices;
        }

        /// <summary>
        /// 从MeshFilter中收集顶点
        /// </summary>
        /// <param name="worldVertices"></param>
        /// <param name="meshFilter"></param>
        static void CollectVerticesFromMeshFilter(List<Vector3> worldVertices, MeshFilter meshFilter)
        {
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null)
                return;

            var trans = meshFilter.transform;
            Matrix4x4 localToWorldMatrix = trans.localToWorldMatrix;

            // 获取局部顶点坐标并转换为世界坐标
            Vector3[] localVertices = mesh.vertices;
            for (int i = 0; i < localVertices.Length; i++)
            {
                var localPos = localVertices[i];
                var worldVertex = localToWorldMatrix.MultiplyPoint3x4(localPos);
                worldVertices.Add(worldVertex);
            }
        }
    }
}

