using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public partial class PVSVolume
    {

#if UNITY_EDITOR

        /// <summary>
        /// 
        /// </summary>
        void OnDrawGizmos()
        {
            if (volumeBakeData == null)
                return;

            DrawGizmos_AreaInfo();
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawGizmos_AreaInfo()
        {
            if (!visCellInfo)
                return;

            var cellInfoList = volumeBakeData.cellInfoList;
            if (cellInfoList == null || cellInfoList.Count <= 0)
                return;

            Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            Handles.matrix = matrix;
            Handles.zTest = CompareFunction.LessEqual;
            var volumePos = volumeBakeData.volumePos;

            foreach (var cellInfo in cellInfoList)
            {
                if (cellInfo == null)
                    continue;

                var cellIndex = cellInfo.cellID;
                var pointNum = cellInfo.pointNum;
                var bounds = cellInfo.bounds;
                var cellSize = new Vector3(bounds.size.x, 500f, bounds.size.z);

                var boundCenter = bounds.center;
                if (volumeBakeData.bakeDataVersion < (int)PVSBakeDataVer.Ver7)
                {
                    boundCenter -= new Vector3(volumePos.x, 0f, volumePos.z);
                }

                Handles.color = Color.cyan;
                Handles.DrawWireCube(boundCenter, cellSize);

                var cellName = $"Cell{cellIndex} Num : {pointNum}";

                var customStyle = new GUIStyle();
                customStyle.fontSize = 24;
                customStyle.normal.textColor = Color.red;
                customStyle.alignment = TextAnchor.MiddleCenter;
                Handles.Label(boundCenter, cellName, customStyle);
            }
        }
#endif
    }
}
