using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public partial class PVSEditUtils
    {
        /// <summary>
        /// 
        /// </summary>
        static GUIStyle s_RichTextStyle = new GUIStyle(EditorStyles.label);
        static GUIStyle s_MiddleTextStyle = new GUIStyle(EditorStyles.label);
        static GUIStyle s_MultiRowTextStyle = new GUIStyle(EditorStyles.label);

        /// <summary>
        /// 
        /// </summary>
        public static bool CanStreamer()
        {
            var globalNodeName = "Global";
            var globalGo = GameObject.Find(globalNodeName);
            if (globalGo == null)
            {
#if UNITY_EDITOR
#if LR_SCENE_MERGE_ENABLED
                EditorUtility.DisplayDialog(LR.Scene.SceneMerge.SceneMergeDefine.s_SceneStreamer_GlobalNodeHide,
                    LR.Scene.SceneMerge.SceneMergeDefine.s_SceneStreamer_GlobalNodeHide, "OK");
#else
                EditorUtility.DisplayDialog("Global node not found", "Global node not found", "OK");
#endif
#endif
                return false;
            }

            var defaultArea = globalGo.transform.Find("PVSCamSamDefaultOffAreaGizmos");
            if (defaultArea != null)
            {
                if (!defaultArea.gameObject.activeSelf)
                {
                    EditorUtility.DisplayDialog(PVSDefine.s_SceneStreamer_CamSamDefaultOffAreaGizmosHide,
                            PVSDefine.s_SceneStreamer_CamSamDefaultOffAreaGizmosHide, "OK");
                    return false;
                }
            }

            var ignoreArea = globalGo.transform.Find("PVSIgnorePointArea");
            if (ignoreArea != null)
            {
                if (!ignoreArea.gameObject.activeSelf)
                {
                    EditorUtility.DisplayDialog(PVSDefine.s_SceneStreamer_IgnorePointAreaHide,
                        PVSDefine.s_SceneStreamer_IgnorePointAreaHide, "OK");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public static GUIStyle GetRichTextStyle()
        {
            s_RichTextStyle.richText = true;
            s_RichTextStyle.alignment = TextAnchor.MiddleCenter;
            return s_RichTextStyle;
        }

        /// <summary>
        /// 
        /// </summary>
        public static GUIStyle GetRichTextStyle(TextAnchor anchor)
        {
            s_RichTextStyle.richText = true;
            s_RichTextStyle.alignment = anchor;
            return s_RichTextStyle;
        }

        /// <summary>
        /// 
        /// </summary>
        public static GUIStyle GetMiddleTextStyle()
        {
            s_MiddleTextStyle.alignment = TextAnchor.MiddleCenter;
            return s_MiddleTextStyle;
        }

        /// <summary>
        /// 
        /// </summary>
        public static GUIStyle GetMultiRowTextStyle()
        {
            s_MultiRowTextStyle.wordWrap = true;
            return s_MultiRowTextStyle;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void DisplayDialog(string strTitle, DialogEvent callback)
        {
            if (EditorUtility.DisplayDialog(strTitle, $"{strTitle}？", "OK", "Cancel"))
            {
                if (callback != null)
                {
                    callback();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void CalcCullingVolumeAllBakeData()
        {
            string strPath = PVSBakeDataUtils.GetActiveScenePath();
            string dir = string.Format("{0}/occlusion/", strPath);

            var cullingVolume = GameObject.FindObjectOfType<ST.PVS.PVSVolume>();
            if (cullingVolume == null || cullingVolume.BakeData == null)
                return;

            var bakeData = cullingVolume.BakeData;
            if (bakeData == null)
                return;

            for (int i = 0; i < 64; i++)
            {
                string dataAsset = string.Format("{0}occlusion_{1}.bytes", dir, i);
                string assetRootPath = System.IO.Path.GetFullPath(Application.dataPath);
                string absFilePath = assetRootPath.Substring(0, assetRootPath.Length - 6) + dataAsset;

                var isExist = File.Exists(absFilePath);
                if (!isExist)
                    continue;

                FileStream reader = File.OpenRead(absFilePath);
                byte[] bytes = new byte[reader.Length];
                reader.Read(bytes, 0, (int)reader.Length);

                bakeData.FillStreamData(i, bytes, bytes.Length, false);

                reader.Close();
            }
        }
    }
}
