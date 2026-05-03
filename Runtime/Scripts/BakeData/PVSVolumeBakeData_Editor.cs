using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public partial class PVSVolumeBakeData
    {
        /// <summary>
        /// 
        /// </summary>
        public override void DrawInspectorGUI()
        {
#if UNITY_EDITOR
            UnityEditor.EditorGUILayout.Toggle($"Bake completed", bakeCompleted);

            UnityEditor.EditorGUILayout.Vector3Field($"Volume Pos", volumePos);

            var vecRot = new Vector4(volumeRot.x, volumeRot.y, volumeRot.z, volumeRot.w);
            UnityEditor.EditorGUILayout.Vector4Field($"Volume Rot", vecRot);

            UnityEditor.EditorGUILayout.Vector3Field($"Volume Size", volumeSize);

            UnityEditor.EditorGUILayout.Vector3Field($"Cell size", cellSize);
            UnityEditor.EditorGUILayout.Vector3Field($"Cell count", cellCount);
#endif
        }
    }
}

