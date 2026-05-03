
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Rendering;
using ST.Core;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    [CustomEditor((typeof(PVSVolume)))]
    public partial class PVSVolumeEditor : Editor
    {
        bool m_rendererFoldout;
        bool m_IsAreaExist;

        SerializedObject so;
        SerializedProperty volumeSize;
        SerializedProperty volumeBakeData;
        SerializedProperty commonBakeData;
        SerializedProperty bakeCellSize;
        SerializedProperty downsampleIterations;
        SerializedProperty outOfBoundsBehaviour;
        SerializedProperty additionalOccluders;
        SerializedProperty gizmosType;
        SerializedProperty openCamMaxDisOffset;
        SerializedProperty camMaxDisOffset;
        SerializedProperty openBakerFovSel;
        SerializedProperty bakerFov90;
        SerializedProperty openIgnoreRayCheckDefaultOffsetType;
        SerializedProperty ignoreRayCheckDefaultOffsetType;

        private void OnEnable()
        {
            PVSVolume cullingVolume = target as PVSVolume;     
            so = new SerializedObject(cullingVolume);    
            
            volumeSize = so.FindProperty("volumeSize");
            volumeBakeData = so.FindProperty("volumeBakeData");
            commonBakeData = so.FindProperty("commonBakeData");
            bakeCellSize = so.FindProperty("bakeCellSize");
            downsampleIterations = so.FindProperty("mergeDownsampleIterations");
            additionalOccluders = so.FindProperty("additionalOccluders");
            gizmosType = so.FindProperty("gizmosType");
            openCamMaxDisOffset = so.FindProperty("openCamMaxDisOffset");
            camMaxDisOffset = so.FindProperty("camMaxDisOffset");

            openBakerFovSel = so.FindProperty("openBakerFovSel");
            bakerFov90 = so.FindProperty("bakerFov90");

            openIgnoreRayCheckDefaultOffsetType = so.FindProperty("openIgnoreRayCheckDefaultOffsetType");
            ignoreRayCheckDefaultOffsetType = so.FindProperty("ignoreRayCheckDefaultOffsetType");
        }

        readonly CustomHandle.ActualHandle<PVSVolume, int> m_handle =
            new CustomHandle.ActualHandle<PVSVolume, int>();
        
        public override void OnInspectorGUI()
        {
            so.Update();
            {
                PVSVolume cullingVolume = target as PVSVolume;
                
                DrawUI(cullingVolume);
            }
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawUI(PVSVolume cullingVolume)
        {
            DrawGUI_BakeSetup(cullingVolume);
            DrawGUI_ManualSplit(cullingVolume);
            DrawGUI_CurrentBake(cullingVolume);
            DrawGUI_Visualization(cullingVolume);
        }
        
        /// <summary>
        /// 
        /// </summary>
        void DrawGUI_ManualSplit(PVSVolume cullingVolume)
        {
            if (!m_IsAreaExist)
                return;

            var commonBakeData = cullingVolume.commonBakeData;

            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Manual Split", EditorStyles.boldLabel);

                var desc = PVSDefine.s_ShowAllManualSplitPointDesc;
                if (GUILayout.Button(desc))
                {
                    PVSEditUtils.DisplayDialog(desc, () =>
                    {
                        PVSOctreeUtils.CollectAllManualSplitPoint(cullingVolume);
                    });
                }

                desc = PVSDefine.s_ClearSaveManualSplitPointDesc;
                if (GUILayout.Button(desc))
                {
                    PVSEditUtils.DisplayDialog(desc, () =>
                    {
                        if (commonBakeData != null)
                        {
                            commonBakeData.ClearManualSplitPointList();
                        }
                    });
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawGUI_BakeSetup(PVSVolume cullingVolume)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                DrawGUI_BakingVolumeConfiguration(cullingVolume);
                DrawGUI_Groups(cullingVolume);
                DrawGUI_Bake(cullingVolume);
            }
            GUILayout.EndVertical();
        }
        
        /// <summary>
        /// 
        /// </summary>
        void DrawGUI_CurrentBake(PVSVolume cullingVolume)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Current bake", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(commonBakeData, new GUIContent("Common Bake Data"));
                EditorGUILayout.PropertyField(volumeBakeData, new GUIContent( "Baked Data" ) );

                var fileVolumeBakeData = cullingVolume.volumeBakeData;
                if (fileVolumeBakeData != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(fileVolumeBakeData);
                    
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        if (PVSEditorUtil.TryGetAssetBakeSize(fileVolumeBakeData,
                            out string bakeSizeMb))
                        {
                            GUILayout.Label($"Current bake size: {bakeSizeMb}");
                        }

                        if (!fileVolumeBakeData.bakeCompleted && !PVSBakingManager.IsBaking)
                        {
                            EditorGUILayout.HelpBox(
                                $"This bake was not completed and might be corrupted. Please consider to bake again.",
                                MessageType.Error);
                        }
                    }
                }
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawGUI_Visualization(PVSVolume cullingVolume)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("GizmosType", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(gizmosType);
                PVSEditUtils.CalcCullingVolumeAllBakeData();

                bool visCellInfo = GUILayout.Toggle(cullingVolume.visCellInfo, PVSDefine.s_AreaCellInfoDesc);
                if (visCellInfo != cullingVolume.visCellInfo)
                {
                    cullingVolume.visCellInfo = visCellInfo;
                    UnityEditor.SceneView.RepaintAll();
                }

                var posOffsetDefaultTypeShow = GUILayout.Toggle(cullingVolume.posOffsetDefaultTypeShow, 
                    PVSDefine.s_ShowCamOffsetDefaultTypeDesc);
                if (posOffsetDefaultTypeShow != cullingVolume.posOffsetDefaultTypeShow)
                {
                    cullingVolume.posOffsetDefaultTypeShow = posOffsetDefaultTypeShow;
                    UnityEditor.SceneView.RepaintAll();
                }
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawGUI_Bake(PVSVolume cullingVolume)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Bake", EditorStyles.boldLabel);

                var isGUIEnable = !Application.isPlaying &&
                    cullingVolume.gameObject.scene != default &&
                    !UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(cullingVolume.gameObject.scene);
                GUI.enabled = isGUIEnable;

                if (GUILayout.Button($"Bake All"))
                {
                    DrawBakeAll();
                }

                GUI.enabled = true;
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawGUI_Other(PVSVolume cullingVolume)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Other", EditorStyles.boldLabel);

                GUILayout.Label("Allows to specifiy renderers that are occluders but not occludees.");

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(10);

                    EditorGUILayout.PropertyField(additionalOccluders, new GUIContent("Additional Occluders"));
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawGUI_Groups(PVSVolume cullingVolume)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Groups", EditorStyles.boldLabel);
                DrawBakeGroupList(cullingVolume);
                ClearBakeGroupList(cullingVolume);
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawGUI_BakingVolumeConfiguration(PVSVolume cullingVolume)
        {
            var isAreaExist = false;
#if ART_SCENE_PROJECT
            isAreaExist = PVSOctreeUtils.IsMaxDensityAreaExist(true);
#else
            isAreaExist = PVSOctreeUtils.IsMaxDensityAreaExist_Game(cullingVolume);
#endif
            m_IsAreaExist = isAreaExist;

            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Baking Volume Configuration", EditorStyles.boldLabel);

                DrawVolumeOctreeMode(isAreaExist);

                EditorGUILayout.PropertyField(volumeSize, new GUIContent("Volume Size"));
                EditorGUILayout.PropertyField(bakeCellSize, new GUIContent("Cell Size"));

                EditorGUILayout.PropertyField(openCamMaxDisOffset, new GUIContent(PVSDefine.s_OpenCamMaxDisOffsetDesc));
                if (openCamMaxDisOffset.boolValue)
                {
                    EditorGUILayout.PropertyField(camMaxDisOffset, new GUIContent(PVSDefine.s_CamMaxDisOffsetDesc));
                }

                EditorGUILayout.PropertyField(openBakerFovSel, new GUIContent(PVSDefine.s_OpenBakerFovSelDesc));
                if (openBakerFovSel.boolValue)
                {
                    EditorGUILayout.PropertyField(bakerFov90, new GUIContent(PVSDefine.s_BakerFov90Desc));
                }

                EditorGUILayout.PropertyField(openIgnoreRayCheckDefaultOffsetType, new GUIContent(PVSDefine.s_OpenIgnoreRayCheckDefaultOffsetTypeDesc));
                if (openIgnoreRayCheckDefaultOffsetType.boolValue)
                {
                    EditorGUILayout.PropertyField(ignoreRayCheckDefaultOffsetType, new GUIContent(PVSDefine.s_IgnoreRayCheckDefaultOffsetTypeDesc));
                }

                GUILayout.Space(10);
                DrawSamplingProviders(cullingVolume);
                DrawCheckVolumeSizeValid(cullingVolume, isAreaExist);
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawSamplingProviders(PVSVolume cullingVolume)
        {
            if (cullingVolume.SamplingProviders.Count != 1)
            {
                GUILayout.Label("Custom Sampling Providers in use:", EditorStyles.boldLabel);

                foreach (var x in cullingVolume.SamplingProviders)
                {
                    if (x.Name == DefaultActiveSamplingProvider.DefaultActiveSamplingProviderName)
                    {
                        continue;
                    }

                    GUILayout.Label($"- {x.Name}");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawBakeAll()
        {
            PVSEditUtils.DisplayDialog("DrawBakeAll", () =>
            {
                ScenePVSExport.Gen();
            });
        }

        /// <summary>
        /// 
        /// </summary>
        void ClearBakeGroupList(PVSVolume cullingVolume)
        {
            GUILayout.Space(10);

            if (GUILayout.Button($"Clear bake groups"))
            {
                if (UnityEditor.EditorUtility.DisplayDialog("Are you sure?",
                    "This will clear the baked data thus requiring a rebake.\n\nThis step cannot be reverted.",
                    "OK", "Cancel"))
                {
                    cullingVolume.bakeGroups = System.Array.Empty<PVSBakeGroup>();

                    EditorUtility.SetDirty(cullingVolume);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawBakeGroupList(PVSVolume cullingVolume)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(10);

                m_rendererFoldout = EditorGUILayout.Foldout(m_rendererFoldout,
                    $"Bake groups ({cullingVolume.bakeGroups.Length})");
            }
            GUILayout.EndHorizontal();

            if (!m_rendererFoldout)
                return;

            int nID = 0;
            foreach (var cullingBakeGroup in cullingVolume.bakeGroups)
            {
                Rect rect = EditorGUILayout.BeginVertical();
                GUILayout.Label($"Type: {cullingBakeGroup.groupType}");
                GUILayout.Label($"ID: {nID}");
                nID++;
                if (cullingBakeGroup.renderers != null)
                {
                    foreach (var renderer in cullingBakeGroup.renderers)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            if (renderer == null)
                            {
                                if (cullingVolume.IsStreamMode())
                                {
                                    EditorGUILayout.HelpBox("Runtime Wait Add", MessageType.Info);
                                }
                                else
                                {
                                    EditorGUILayout.HelpBox("Invalid renderer Or RunTime Wait Add", MessageType.Error);

                                    if (GUILayout.Button("X"))
                                    {
                                        cullingBakeGroup.renderers = cullingBakeGroup.renderers
                                            .Except(new Renderer[] { renderer }).ToArray();
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                GUILayout.Label(" - " + renderer.name);

                                if (GUILayout.Button("Select", GUILayout.Width(75)))
                                {
                                    UnityEditor.Selection.activeObject = renderer;
                                }

                                if (GUILayout.Button("X", GUILayout.Width(25)))
                                {
                                    cullingBakeGroup.renderers = cullingBakeGroup.renderers
                                        .Except(new Renderer[] { renderer }).ToArray();

                                    return;
                                }
                            }
                        }

                        GUILayout.EndHorizontal();
                    }
                }

                if (GUILayout.Button("Remove group"))
                {
                    cullingVolume.bakeGroups =
                        cullingVolume.bakeGroups.Except(new PVSBakeGroup[] { cullingBakeGroup })
                            .ToArray();
                    return;
                }

                EditorGUILayout.EndVertical();
                GUI.Box(rect, GUIContent.none);
                GUILayout.Space(5);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawVolumeOctreeMode(bool isAreaExist)
        {
            if (!isAreaExist)
                return;

            var strFormat = PVSDefine.s_VolumeOctreeModeFormat;
            var strDesc = string.Format(strFormat, PVSDefine.s_OctreeMaxLeafSize);
            GUILayout.Label(strDesc, EditorStyles.boldLabel);
            GUILayout.Space(10);
        }

        /// <summary>
        /// Checks if the volume size is valid and if not, offers to fix it.
        /// </summary>
        void DrawCheckVolumeSizeValid(PVSVolume cullingVolume, bool isAreaExist)
        {
            if (isAreaExist)
            {
                DrawCheckVolumeSizeValid_Octree(cullingVolume);
            }
            else
            {
                DrawCheckVolumeSizeValid_UnOctree(cullingVolume);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawCheckVolumeSizeValid_Octree(PVSVolume cullingVolume)
        {
            var leafSize = PVSDefine.s_OctreeMaxLeafSize;
            var invalidDescFormat = PVSDefine.s_OctreeVolumeSizeInvalidFormat;

            var volumeSizeX = cullingVolume.volumeSize.x;
            var volumeSizeY = cullingVolume.volumeSize.y;
            var volumeSizeZ = cullingVolume.volumeSize.z;
            DrawCheckVolumeSizeValid(invalidDescFormat, "X", volumeSizeX, leafSize);
            DrawCheckVolumeSizeValid(invalidDescFormat, "Y", volumeSizeY, leafSize);
            DrawCheckVolumeSizeValid(invalidDescFormat, "Z", volumeSizeZ, leafSize);
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawCheckVolumeSizeValid_UnOctree(PVSVolume cullingVolume)
        {
            var invalidDescFormat = PVSDefine.s_VolumeSizeInvalidFormat;

            var volumeSizeX = cullingVolume.volumeSize.x;
            var volumeSizeY = cullingVolume.volumeSize.y;
            var volumeSizeZ = cullingVolume.volumeSize.z;
            var cellSizeX = cullingVolume.bakeCellSize.x;
            var cellSizeY = cullingVolume.bakeCellSize.y;
            var cellSizeZ = cullingVolume.bakeCellSize.z;
            DrawCheckVolumeSizeValid(invalidDescFormat, "X", volumeSizeX, cellSizeX);
            DrawCheckVolumeSizeValid(invalidDescFormat, "Y", volumeSizeY, cellSizeY);
            DrawCheckVolumeSizeValid(invalidDescFormat, "Z", volumeSizeZ, cellSizeZ);
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawCheckVolumeSizeValid(string strFormat, string strAxis, float volumeSize, float cellSize)
        {
            var isValid = PVSVolumeUtils.IsVolumeSizeValid(volumeSize, cellSize);
            if (isValid)
                return;

            var refVal = Mathf.CeilToInt(volumeSize / cellSize) * cellSize;
            var strDesc = string.Format(strFormat, strAxis, cellSize, volumeSize, refVal);
            EditorGUILayout.HelpBox(strDesc, MessageType.Error);
        }
    }
}
