
using System;
using System.Collections;
using System.Collections.Generic;
using ST.PVS;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace ST.PVS
{
    [CustomEditor(typeof(PVSCamera))]
    public class PVSCameraEditor : Editor
    {
        SerializedObject so;
        SerializedProperty includeNeighborCells;

        bool m_camFoldout;
        bool m_volumeFoldout;

        /// <summary>
        /// 
        /// </summary>
        private void OnEnable()
        {
            PVSCamera camera = target as PVSCamera;

            so = new SerializedObject(camera);
            includeNeighborCells = so.FindProperty("neighborCellIncludeRadius");
        }

        /// <summary>
        /// 
        /// </summary>
        public override void OnInspectorGUI()
        {
            so.Update();
            {
                PVSCamera camera = target as PVSCamera;

                DrawUI(camera);
            }

            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawUI(PVSCamera camera)
        {
            Setup(camera);
            Stats(camera);
            Utility(camera);
            DrawDebug(camera);

            if (camera.GetComponent<Camera>().useOcclusionCulling)
            {
                EditorGUILayout.HelpBox("You are using Umbra (Unity's Occlusion Culling system) and Perfect Culling simultaneously. Consider picking one.", MessageType.Info);
            }
            
            if (StaticOcclusionCulling.umbraDataSize > 0)
            {
                EditorGUILayout.HelpBox("You baked Occlusion Data for Umbra (Unity's Occlusion Culling system). This might impact the Frustum Culling preview.", MessageType.Info);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void Setup(PVSCamera camera)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Setup", EditorStyles.boldLabel);
                
                EditorGUILayout.PropertyField( includeNeighborCells, new GUIContent( "Include Neighbor Cell Radius" ) );
            }
            GUILayout.EndVertical();
        }
        
        /// <summary>
        /// 
        /// </summary>
        void Stats(PVSCamera camera)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Stats", EditorStyles.boldLabel);
                
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(10);

                    m_camFoldout = EditorGUILayout.Foldout(m_camFoldout,
                        $"Active culling cameras ({PVSCameraMgr.s_AllCameras.Count})");

                }
                GUILayout.EndHorizontal();
                
                if (m_camFoldout)
                {
                    foreach (var cam in PVSCameraMgr.s_AllCameras)
                    { 
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(10);
                            GUILayout.Label(cam.name);
                            
                            if (GUILayout.Button("Select", GUILayout.Width(150)))
                            {
                                UnityEditor.Selection.activeObject = cam;
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(10);

                    m_volumeFoldout = EditorGUILayout.Foldout(m_volumeFoldout,
                        $"Active culling volumes ({PVSVolume.AllVolumes.Count})");

                }
                GUILayout.EndHorizontal();
                
                if (m_volumeFoldout)
                {
                    foreach (var vol in PVSVolume.AllVolumes)
                    { 
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(10);
                            GUILayout.Label(vol.name);

                            if (GUILayout.Button("Select", GUILayout.Width(150)))
                            {
                                UnityEditor.Selection.activeObject = vol;
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.Space(15);

                if (camera.lastTotal != 0)
                {
                    GUILayout.Label($"Total renderers: {camera.lastTotal}");
                    GUILayout.Label(
                        $" - Culled: {camera.lastCulled} ({Mathf.Round((camera.lastCulled / (float) camera.lastTotal) * 100f)}%)");

                    GUILayout.Label($" - Visible: {camera.lastVisible}");
                    GUILayout.Label($"Last Frame Hash (only changes on culling updates): {PVSCamera.s_LastFrameHash}");
                }
                
                EditorGUI.BeginChangeCheck();

                bool showInGameStats = GUILayout.Toggle(camera.showInGameStats , " Show in-game stats window");
                    
                if (EditorGUI.EndChangeCheck())
                {
                    camera.showInGameStats = showInGameStats;
                    
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }
            }
            GUILayout.EndVertical();
        }
        
        /// <summary>
        /// 
        /// </summary>
        void Utility(PVSCamera camera)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Utility", EditorStyles.boldLabel);

                GUI.enabled = Application.isPlaying;
                {
                    EditorGUI.BeginChangeCheck();

                    bool invertCulling = GUILayout.Toggle(camera.invertCulling, " Invert Culling (renders culled objects)");
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        camera.invertCulling = invertCulling;
                        
                        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    }
                }
                GUI.enabled = true;
                
                if (!Application.isPlaying)
                {
                    GUILayout.Space(5);
                    
                    EditorGUILayout.HelpBox($"Some functionality is only available in Play Mode!", MessageType.Info);
                }
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawDebug(PVSCamera _camera)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                DrawCamSampleOffsetType(_camera);
                DrawDebugSamplePosOffset(_camera);
                DrawIsTestIgnorePoint(_camera);
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawCamSampleOffsetType(PVSCamera _camera)
        {
            {
                EditorGUI.BeginChangeCheck();

                var camSampleOffsetTypeDesc = PVSDefine.s_CamSampleOffsetTypeDesc;
                var camSampleOffsetType = EditorGUILayout.EnumPopup(camSampleOffsetTypeDesc, _camera.camSampleOffsetType);

                if (EditorGUI.EndChangeCheck())
                {
                    _camera.camSampleOffsetType = (PVSCamSampleOffsetType)camSampleOffsetType;
                    PVSCamera.RefreshCamSampleOffsetType();
                }
            }
            GUI.enabled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawIsTestIgnorePoint(PVSCamera _camera)
        {
            if (!_camera.isDebugSamplePosOffset)
                return;

            GUI.enabled = Application.isPlaying;
            {
                EditorGUI.BeginChangeCheck();

                var isTestIgnorePointDesc = PVSDefine.s_IsTestIgnorePointDesc;
                bool isTestIgnorePoint = GUILayout.Toggle(_camera.isTestIgnorePoint, isTestIgnorePointDesc);

                if (EditorGUI.EndChangeCheck())
                {
                    _camera.isTestIgnorePoint = isTestIgnorePoint;
                    _camera.RefreshTestIgnorePoint(isTestIgnorePoint);
                }
            }
            GUI.enabled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawDebugSamplePosOffset(PVSCamera _camera)
        {
            GUI.enabled = Application.isPlaying;
            {
                EditorGUI.BeginChangeCheck();

                var debugSamplePosOffsetDesc = PVSDefine.s_IsDebugSamplePosOffsetDesc;
                bool isDebugSamplePosOffset = GUILayout.Toggle(_camera.isDebugSamplePosOffset, debugSamplePosOffsetDesc);

                if (EditorGUI.EndChangeCheck())
                {
                    _camera.isDebugSamplePosOffset = isDebugSamplePosOffset;
                    _camera.RefreshDebugSamplePosOffset(isDebugSamplePosOffset);
                }
            }
            GUI.enabled = true;
        }
    }
}
