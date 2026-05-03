
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using ST.PVS;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Logger = ST.Core.Logging.Logger;

namespace ST.PVS
{
    public class PVSMenuOptions
    {
        private static void BakeAll(bool bNew)
        {
            if (UnityEditor.SceneManagement.EditorSceneManager.sceneCount <= 1)
            {
                BakeSingle();
            }
            else
            {
                BakeMulti();
            }
        }

        private static void BakeSingle()
        {
            Logger.Log("Single scene bake.");
            
            PVSBakingBehaviour[] bakingBehaviours = GameObject.FindObjectsOfType<PVSBakingBehaviour>();

            PVSBakingManager.BakeNow(bakingBehaviours);
        }

        private static void BakeMulti()
        {
            Logger.Log("Multi scene bake.");
            
            List<UnityEngine.SceneManagement.Scene> scenesToBake = new List<UnityEngine.SceneManagement.Scene>();
            
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);

                scenesToBake.Add(scene);
            }
            
            PVSBakingManager.BakeMultiScene(scenesToBake);
        }

        private static void CreateNewBakingVolume()
        {
            GameObject go = new GameObject("PVS Baking Volume");

            PVSVolume vol = go.AddComponent<PVSVolume>();

            vol.bakeCellSize = new Vector3(10, 5, 10);
            vol.volumeBakeBounds = new Bounds(Vector3.zero, new Vector3(100, 5, 100));

            UnityEditor.Selection.activeObject = go;
            UnityEditor.SceneView.lastActiveSceneView.Frame(vol.volumeBakeBounds, false);
        }
        
        private static void CreateNewExcludeVolume()
        {
            GameObject go = new GameObject("PVS Exclude Volume");

            PVSExcludeVolume vol = go.AddComponent<PVSExcludeVolume>();

            vol.volumeExcludeBounds = new Bounds(Vector3.zero, new Vector3(100, 5, 100));

            UnityEditor.Selection.activeObject = go;
            UnityEditor.SceneView.lastActiveSceneView.Frame(vol.volumeExcludeBounds, false);
        }
        
        private static void SelectSettings()
        {
            UnityEditor.Selection.activeObject = PVSSettings.Instance;
        }
    }
}
#endif
