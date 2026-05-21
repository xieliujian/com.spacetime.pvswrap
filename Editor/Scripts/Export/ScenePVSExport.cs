
using ST.PVS;
using UnityEditor;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class ScenePVSExport
    {
        /// <summary>
        /// 
        /// </summary>
        public static bool s_bFinish = false;

        /// <summary>
        /// 
        /// </summary>
        public static bool s_IsSinglePointBakeMode = false;
        public static Vector3 s_SingleBakePoint;

        /// <summary>
        /// 
        /// </summary>
        public static bool s_HasPVSScript = true;
        public static bool s_CanPVSExport = true;

        /// <summary>
        /// 
        /// </summary>
        public static void GenSinglePoint()
        {
            s_bFinish = false;
            s_IsSinglePointBakeMode = true;
            s_CanPVSExport = true;

            GenPvsData(true, OnFinish);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Gen()
        {
            s_bFinish = false;
            s_IsSinglePointBakeMode = false;

            GenPvsData(true, OnFinish);
        }

#if false
        [MenuItem("MHT/测试烘培数据有效性")]
        public static void TestBakeDataValidation()
        {
            PVSBakeDataValidation.S.Validation();
        }

#endif

        /// <summary>
        /// 
        /// </summary>
        public static void ClearPVSInfo()
        {
            PVSVolume cullingVolumeCmp = PVSVolumeUtils.GetCullingVolume();
            if (cullingVolumeCmp == null)
                return;

            if (!s_CanPVSExport)
            {
                GameObject.DestroyImmediate(cullingVolumeCmp);
            }
            else
            {
                cullingVolumeCmp.ProcessStreamMode();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool IsCanPVSExport()
        {
            if (!s_HasPVSScript)
                return true;

            return s_CanPVSExport;
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool GenPvsData(bool isGenerateSceneData = true, System.Action onBackedFinish = null)
        {
            PVSVolume cullingVolumeCmp = PVSVolumeUtils.GetCullingVolume();
            s_HasPVSScript = cullingVolumeCmp != null;

            if (!s_HasPVSScript)
            {
                onBackedFinish?.Invoke();
                return false;
            }

            if (!s_CanPVSExport)
            {
                onBackedFinish?.Invoke();
                return false;
            }

            PVSWrapBridge.onResToolsEnableSet?.Invoke(false);

            var binDir = PVSBakeDataUtils.CreateBinFolder(false);
            PVSBakeDataUtils.DeleteAssetsInFolder(binDir);

            string strScenePath = PVSBakeDataUtils.GetActiveScenePath();
            string pvsMainPath = string.Format("{0}/Occlusion_{1}.asset", strScenePath,
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name);
            AssetDatabase.DeleteAsset(pvsMainPath);
                
            if (cullingVolumeCmp.commonBakeData == null)
            {
                cullingVolumeCmp.CreateCommonBakeDataAsset();
            }

            DeleteVoxelScript();

            QualitySettings.maximumLODLevel = 0;

            UnityEditor.EditorUtility.DisplayProgressBar($"ScenePVSExport", "CollAllObjs", 0);

#if LR_SCENE_MERGE_ENABLED
            cullingVolumeCmp.ClearSceneMergeBlockList();
#endif
            cullingVolumeCmp.bakeGroups = PVSVolumeUtils.CollAllObjs(cullingVolumeCmp.volumeSize,
                out cullingVolumeCmp.grassGroupBegin);

            PVSMaxDensityAreaGizmosMgr.S.ReCollect();
            PVSIgnorePointAreaGizmosMgr.S.ReCollect();
            PVSCamSamDefaultOffAreaGizmosMgr.S.ReCollect();
            PVSForceSampleAreaMgr.S.Collect();

            if (cullingVolumeCmp.volumeBakeData != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(cullingVolumeCmp.volumeBakeData);
                AssetDatabase.DeleteAsset(assetPath);
            }

            if (pvsMainPath.Length == 0)
            {
                onBackedFinish?.Invoke();
                return false;
            }

            cullingVolumeCmp.volumeBakeData = ScriptableObject.CreateInstance<PVSVolumeBakeData>();

            EditorUtility.SetDirty(cullingVolumeCmp);
            AssetDatabase.CreateAsset(cullingVolumeCmp.volumeBakeData, pvsMainPath);
            AssetDatabase.SaveAssets();

            cullingVolumeCmp.volumeBakeData.bakeDataVersion = PVSDefine.s_CurBakeDataVer;
            cullingVolumeCmp.isSinglePointBakeMode = s_IsSinglePointBakeMode;
            cullingVolumeCmp.singleBakePoint = s_SingleBakePoint;

            PVSAPI.Bake.OnAllBakesFinished -= onBackedFinish;
            if (isGenerateSceneData)
            {
                PVSAPI.Bake.OnAllBakesFinished += onBackedFinish;
            }

            PVSBakingManager.BakeNow(cullingVolumeCmp);
            return true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public static void RemoveBakesFinishedCallBack(System.Action onBackedFinish)
        {
            PVSAPI.Bake.OnAllBakesFinished -= onBackedFinish;
        }

        /// <summary>
        /// 
        /// </summary>
        static void DeleteVoxelScript()
        {
            var voxelGo = GameObject.Find("VoxelEditorRoot");
            if (voxelGo == null)
                return;

            GameObject.DestroyImmediate(voxelGo);
        }

        /// <summary>
        /// 
        /// </summary>
        static void OnFinish()
        {
            s_bFinish = true;

            if (!s_CanPVSExport || !s_HasPVSScript)
                return;

            PVSWrapBridge.onResToolsEnableSet?.Invoke(true);
            EditorUtility.DisplayProgressBar("PVS验证", "PVS验证", 1f);
            PVSBakeDataValidation.S.Validation();
            EditorUtility.ClearProgressBar();
        }
    }
}
