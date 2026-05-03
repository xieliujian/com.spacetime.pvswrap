using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    [CustomEditor(typeof(PVSSamplePosOffset))]
    [CanEditMultipleObjects]
    public class PVSSamplePosOffsetInspector : Editor
    {
        /// <summary>
        /// 
        /// </summary>
        SerializedProperty m_PosIndex;
        SerializedProperty m_PosProp;
        SerializedProperty m_IsIgnore;

        /// <summary>
        /// 
        /// </summary>
        void OnEnable()
        {
            m_PosIndex = serializedObject.FindProperty("posIndex");
            m_PosProp = serializedObject.FindProperty("pos");
            m_IsIgnore = serializedObject.FindProperty("isIgnore");
        }

        /// <summary>
        /// 
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawUI();
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawUI()
        {
            EditorGUILayout.PropertyField(m_PosIndex);
            EditorGUILayout.PropertyField(m_PosProp);

            DrawUI_IsIgnore();
            DrawUI_IsSceneShow();
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawUI_IsSceneShow()
        {
            foreach (Object target in targets)
            {
                var _offset = target as PVSSamplePosOffset;
                if (_offset == null)
                    continue;

                if (_offset.isMainPos)
                    continue;

                EditorGUI.BeginChangeCheck();
                bool isSceneShow = GUILayout.Toggle(_offset.isSceneShow, "isSceneShow");

                if (EditorGUI.EndChangeCheck())
                {
                    _offset.isSceneShow = isSceneShow;
                    _offset.RefreshSceneShow();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawUI_IsIgnore()
        {
            foreach (Object target in targets)
            {
                var _offset = target as PVSSamplePosOffset;
                if (_offset == null)
                    continue;

                if (_offset.isMainPos)
                    return;

                EditorGUILayout.PropertyField(m_IsIgnore);
            }
        }
    }
}
