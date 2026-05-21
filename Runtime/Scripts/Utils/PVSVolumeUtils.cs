using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ST.Core;
using Logger = ST.Core.Logging.Logger;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public static class PVSVolumeUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static PVSVolume GetCullingVolume()
        {
            return GameObject.FindObjectOfType<PVSVolume>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Transform GetPVSCamTrans(out PVSCamera _cullCam)
        {
            Transform camTrans = null;
            _cullCam = null;

            var allCamScript = PVSCameraMgr.s_AllCameras;
            var camExist = allCamScript != null && allCamScript.Count > 0;
            if (camExist)
            {
                var camScript = allCamScript[0];
                _cullCam = camScript;
                camTrans = camScript.transform;
            }
            else
            {
#if UNITY_EDITOR
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    Camera sceneViewCamera = sceneView.camera;
                    if (sceneViewCamera != null)
                    {
                        camTrans = sceneViewCamera.transform;
                    }
                }
#endif
            }

            return camTrans;
        }

        /// <summary>
        /// 
        /// </summary>
        public static Vector3 GetSamplingPositionAt(Vector3 position, Quaternion rotation, Vector3 volumeSize, int index, Vector3 cellSize, 
            Space space, PVSAlignment _alignment, PVSCoordSystem _coordSystem)
        {
            Vector3 gridSize = GridMath.CalculateCellCount(volumeSize, cellSize);
            return GetSamplingPositionAt(position, rotation, index, gridSize, cellSize, space, _alignment, _coordSystem);
        }

        /// <summary>
        /// 
        /// </summary>
        public static Vector3 GetSamplingPositionAt(Vector3 position, Quaternion rotation, int index, Vector3 gridSize, Vector3 cellSize,
            Space space, PVSAlignment _alignment, PVSCoordSystem _coordSystem)
        {
            Vector3 halfGridSize = gridSize * 0.5f;

            GridMath.UnflattenToXYZ(index, out int x, out int y, out int z, gridSize);

            Vector3 localPos = Vector3.zero;
            if (_coordSystem == PVSCoordSystem.BottomLeftOrigin)
            {
                localPos = new Vector3(x * cellSize.x, y * cellSize.y, z * cellSize.z);
            }
            else
            {
                localPos = new Vector3(x * cellSize.x, y * cellSize.y, z * cellSize.z) -
                    new Vector3(halfGridSize.x * cellSize.x, halfGridSize.y * cellSize.y, halfGridSize.z * cellSize.z);
            }

            if (_alignment == PVSAlignment.LowerLeft)
            {

            }
            else if (_alignment == PVSAlignment.MiddleCenter)
            {
                localPos += cellSize / 2;
            }
            else if (_alignment == PVSAlignment.UpperRight)
            {
                localPos += cellSize;
            }

            if (space == Space.World)
            {
                return position + rotation * localPos;
            }

            return localPos;
        }

        /// <summary>
        /// 
        /// </summary>
        public static Vector3 GetSamplingChildPositionAt(Vector3 _lowerLeftPos, int _cellIndex, Vector3 _cellNumVec, Vector3 _cellSize, Quaternion _rotation, 
            Space _space, PVSAlignment _alignment, out Vector3 _outGridLocalPos)
        {
            _outGridLocalPos = Vector3.zero;
            Vector3 finalPos = Vector3.zero;

            GridMath.UnflattenToXYZ(_cellIndex, out int x, out int y, out int z, _cellNumVec);
            Vector3 localPos = new Vector3(x * _cellSize.x, y * _cellSize.y, z * _cellSize.z);
            _outGridLocalPos = localPos;

            if (_alignment == PVSAlignment.LowerLeft)
            {

            }
            else if (_alignment == PVSAlignment.MiddleCenter)
            {
                localPos += _cellSize / 2;
            }

            if (_space == Space.World)
            {
                Vector3 worldPos = _rotation * localPos;
                finalPos = _lowerLeftPos + worldPos;
            }
            else
            {
                finalPos = _lowerLeftPos + localPos;
            }

            return finalPos;
        }

        /// <summary>
        /// 
        /// </summary>
        public static HashSet<PVSBakeGroup> CreateBakeGroupsForRenderers2(HashSet<List<Renderer>> selectedRenderers)
        {
            HashSet<PVSBakeGroup> result = new HashSet<PVSBakeGroup>();

            CreateBakeGroups(selectedRenderers, result, false);
            CreateBakeGroups(selectedRenderers, result, true);

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void ResetScene()
        {
#if UNITY_EDITOR
            ShowTerrain(true);
            SetAllDistanceLODScriptEnable(true);

            GameObject TerrainMeshGO = GameObject.Find(PVSDefine.TERRAIN_MESH_PVSROOT_NONE_NAME);
            if (TerrainMeshGO != null)
            {
                UnityEditor.EditorUtility.SetDirty(TerrainMeshGO);
                GameObject.DestroyImmediate(TerrainMeshGO);
            }

            Debug.Log("释放TerrainMeshRootNode");
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool IsVolumeSizeValid(PVSVolume volume, bool isReCollect)
        {
            var isAreaExist = PVSOctreeUtils.IsMaxDensityAreaExist(isReCollect);
            bool isValid = true;

            if (isAreaExist)
            {
                var leafSize = PVSDefine.s_OctreeMaxLeafSize;
                var leafSizeV3 = new Vector3(leafSize, leafSize, leafSize);
                isValid = IsVolumeSizeValid(volume.volumeSize, leafSizeV3);
            }
            else
            {
                isValid = IsVolumeSizeValid(volume.volumeSize, volume.bakeCellSize);
            }

            return isValid;
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool IsVolumeSizeValid(float volumeSize, float cellSize)
        {
            var isValid = volumeSize % cellSize == 0;
            return isValid;
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool IsVolumeSizeValid(Vector3 volumeSize, Vector3 cellSize)
        {
            var isValidX = volumeSize.x % cellSize.x == 0;
            var isValidY = volumeSize.y % cellSize.y == 0;
            var isValidZ = volumeSize.z % cellSize.z == 0;
            var isValid = isValidX && isValidY && isValidZ;
            return isValid;
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool IsIndexValid(int _x, int _y, int _z, Vector3 _cellCount)
        {
            if (_x < 0 || _x >= _cellCount.x)
                return false;

            if (_y < 0 || _y >= _cellCount.y)
                return false;

            if (_z < 0 || _z >= _cellCount.z)
                return false;

            return true;
        }

        /// <summary>
        /// Collects all objects in the scene and creates bake groups based on the specified volume size.
        /// </summary>
        public static PVSBakeGroup[] CollAllObjs(Vector3 _volumeSize, out int _grassGroupBegin)
        {
            ProcessTerrain();
            SetAllDistanceLODScriptEnable(false);

            Dictionary<int, List<GameObject>> dict = PVSWrapBridge.onSceneToolsGetAllPrefabs();
            HashSet<List<Renderer>> selectedRenderers = new HashSet<List<Renderer>>();

            CollAllObjs_AddTerrainPrefabList(dict);

#if LR_SCENE_MERGE_ENABLED
            var isMergeScene = LR.Scene.SceneMerge.SceneMergeUtils.IsSceneMerge();
            if (isMergeScene)
            {
                PVSSceneMergeUtils.FillBakeGroupSet(dict, selectedRenderers);
            }
            else
#endif
            {
                CollAllObjs_FillSelectRenderList(dict, selectedRenderers);
            }

            var bakeGroups = CreateBakeGroupList(selectedRenderers, out _grassGroupBegin);

            return bakeGroups;
        }

        /// <summary>
        /// 
        /// </summary>
        public static Bounds CalcCenterPosAndBound(List<Renderer> renderArray)
        {
            List<Bounds> boundlist = new List<Bounds>();

            if (renderArray != null)
            {
                foreach (var render in renderArray)
                {
                    if (render == null)
                        continue;

                    boundlist.Add(render.bounds);
                }
            }

            var centerPos = Vector3.zero;
            if (boundlist.Count > 0)
            {
                foreach (var bound in boundlist)
                {
                    centerPos += bound.center;
                }

                centerPos /= boundlist.Count;
            }

            var newBound = new Bounds(centerPos, Vector3.zero);
            foreach (var bound in boundlist)
            {
                newBound.Encapsulate(bound);
            }

            return newBound;
        }

        /// <summary>
        /// 
        /// </summary>
        public static Vector3 CalcOctreeCellSize(int _leafNodeIdx)
        {
            var cellSize = PVSDefine.s_OctreeMinLeafSizeVec;
            var realNodeVal = (PVSDefine.s_OctreeMaxLeafNum - 1) - _leafNodeIdx;
            var realVal = Mathf.Pow(PVSDefine.s_OctreeChildNumVec.x, realNodeVal);
            cellSize *= realVal;

            cellSize.x = Mathf.Clamp(cellSize.x, PVSDefine.s_OctreeMinLeafSize, PVSDefine.s_OctreeMaxLeafSize);
            cellSize.y = Mathf.Clamp(cellSize.y, PVSDefine.s_OctreeMinLeafSize, PVSDefine.s_OctreeMaxLeafSize);
            cellSize.z = Mathf.Clamp(cellSize.z, PVSDefine.s_OctreeMinLeafSize, PVSDefine.s_OctreeMaxLeafSize);

            return cellSize;
        }

        /// <summary>
        /// 
        /// </summary>
        public static PVSCommonBakeData CreateCommonBakeDataAsset()
        {
            PVSCommonBakeData commonBakeData = null;

#if UNITY_EDITOR
            string strPath = PVSBakeDataUtils.GetActiveScenePath();
            string path = string.Format("{0}/OcclusionCommon_{1}.asset", strPath,
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name);

            commonBakeData = ScriptableObject.CreateInstance<PVSCommonBakeData>();
            UnityEditor.AssetDatabase.CreateAsset(commonBakeData, path);
            UnityEditor.AssetDatabase.SaveAssets();
#endif

            return commonBakeData;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void CalcCamSampleOffsetType(ref uint _samplePosOffsetMask, 
            bool _ignoreRayCheckDefaultOffsetType,
            Vector3 _pos,
            int _layer,
            Vector3 _cellSize)
        {
            if (!_ignoreRayCheckDefaultOffsetType)
            {
                CalcCamSampleOffsetType_Ray(ref _samplePosOffsetMask, PVSSamplePosOffsetMask.UpMask, _pos, Vector3.up, _cellSize.y, _layer);
                CalcCamSampleOffsetType_Ray(ref _samplePosOffsetMask, PVSSamplePosOffsetMask.DownMask, _pos, Vector3.down, _cellSize.y, _layer);
                CalcCamSampleOffsetType_Ray(ref _samplePosOffsetMask, PVSSamplePosOffsetMask.LeftMask, _pos, Vector3.left, _cellSize.x, _layer);
                CalcCamSampleOffsetType_Ray(ref _samplePosOffsetMask, PVSSamplePosOffsetMask.RightMask, _pos, Vector3.right, _cellSize.x, _layer);
                CalcCamSampleOffsetType_Ray(ref _samplePosOffsetMask, PVSSamplePosOffsetMask.ForwardMask, _pos, Vector3.forward, _cellSize.z, _layer);
                CalcCamSampleOffsetType_Ray(ref _samplePosOffsetMask, PVSSamplePosOffsetMask.BackMask, _pos, Vector3.back, _cellSize.z, _layer);
            }
            
            CalcCamSampleOffsetType_Area(ref _samplePosOffsetMask, _pos);
        }

        /// <summary>
        /// 
        /// </summary>
        static void CalcCamSampleOffsetType_Area(ref uint _samplePosOffsetMask, Vector3 _pos)
        {
            var isValid = PVSCameraUtils.IsCamSampleOffsetDefaultType(_samplePosOffsetMask);
            if (isValid)
                return;

            var camSampleOffAreaMgr = PVSCamSamDefaultOffAreaGizmosMgr.S;
            if (camSampleOffAreaMgr != null)
            {
                var isInArea = camSampleOffAreaMgr.IsPointInGizmos(_pos);
                if (!isInArea)
                    return;
            }

            _samplePosOffsetMask = PVSCameraUtils.SaveCamSampleOffsetDefaultType(_samplePosOffsetMask);
        }

        /// <summary>
        /// 
        /// </summary>
        static void CalcCamSampleOffsetType_Ray(ref uint _samplePosOffsetMask, 
            PVSSamplePosOffsetMask _maskType, 
            Vector3 _pos,
            Vector3 _dir,
            float _distance,
            int _layer
            )
        {
            var isValid = PVSCameraUtils.IsCamSampleOffsetDefaultType(_samplePosOffsetMask);
            if (isValid)
                return;

            Vector3 endPos = _pos;
            Vector3 startPos = endPos + _dir * _distance;
            Vector3 rayDir = -_dir;

            var isHit = Physics.Raycast(startPos, rayDir, _distance, _layer);
            if (isHit)
            {
                _samplePosOffsetMask = PVSCameraUtils.SaveCamSampleOffsetDefaultType(_samplePosOffsetMask);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void DrawGUI_OffsetPosDesc(bool isInSampleList, int _findIndex, 
            PVSCamera _cullCam, Vector3[] _offsetArray, Vector3 _localPos)
        {
#if UNITY_EDITOR
            if (isInSampleList)
            {
                if (_findIndex != 0)
                {
                    var customStyle = new GUIStyle();
                    customStyle.fontSize = 24;
                    customStyle.normal.textColor = Color.red;
                    customStyle.alignment = TextAnchor.MiddleCenter;

                    if (_cullCam != null)
                    {
                        string offsetPosDesc = "";
                        if (_findIndex >= 0)
                        {
                            if (_offsetArray != null)
                            {
                                offsetPosDesc = _offsetArray[_findIndex].ToString();
                            }
                        }

                        UnityEditor.Handles.Label(_localPos, offsetPosDesc, customStyle);
                    }
                }
            }
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool IsObjCollectInBakeGroup(GameObject selectedGameObject, out List<Renderer> _outRenderList)
        {
            _outRenderList = new List<Renderer>();

            if (selectedGameObject == null)
                return false;

            if (selectedGameObject.name == "Global")
                return false;

            var isObjVis = selectedGameObject.activeInHierarchy;
            if (!isObjVis)
                return false;

            var ignorePVSTag = PVSDefine.s_Baker_IgnorePVS_Tag;
            if (selectedGameObject.tag == ignorePVSTag)
            {
                Logger.Log(ignorePVSTag + " " + selectedGameObject.name);
                return false;
            }

            var lodGroup = selectedGameObject.GetComponentInChildren<LODGroup>();
            if (lodGroup != null)
            {
                var isSceneGroup = PVSWrapBridge.isSceneObjectGroup(lodGroup);
                if (isSceneGroup)
                    return false;
            }

            List<Renderer> srcRenderers = new List<Renderer>();
            selectedGameObject.GetComponentsInChildren<Renderer>(true, srcRenderers);
            if (srcRenderers == null || srcRenderers.Count <= 0)
                return false;

            foreach (var render in srcRenderers)
            {
                if (render == null)
                    continue;

                var isFilter = RendererFilter(render);
                if (!isFilter)
                    continue;

                _outRenderList.Add(render); 
            }

            if (_outRenderList == null || _outRenderList.Count <= 0)
                return false;

            bool bHasEffect = false;
            foreach (var cur in _outRenderList)
            {
                bHasEffect = HasEffectByRender(cur);
                if (bHasEffect)
                    break;
            }

            if (bHasEffect)
                return false;

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        static void CreateBakeGroups(HashSet<List<Renderer>> _selectedRenderers,
            HashSet<PVSBakeGroup> _result, bool isFoliageCollect)
        {
            var foliageTag = PVSDefine.s_BakeGroupFoliageNameTag;

            foreach (List<Renderer> renderersVar in _selectedRenderers)
            {
                List<Renderer> addVar = new List<Renderer>();

                foreach (Renderer renderer in renderersVar)
                {
                    if (renderer == null)
                        continue;

                    if (isFoliageCollect)
                    {
                        if (renderer.gameObject.name == foliageTag)
                        {
                            addVar.Add(renderer);
                        }
                    }
                    else
                    {
                        if (renderer.gameObject.name != foliageTag)
                        {
                            addVar.Add(renderer);
                        }
                    }
                }

                if (addVar.Count > 0)
                {
                    var bakeGroup = new PVSBakeGroup();
                    bakeGroup.renderers = addVar.ToArray();

                    var groupType = isFoliageCollect ? PVSBakeGroup.GroupType.Foliage
                        : PVSBakeGroup.GroupType.Other;
                    bakeGroup.groupType = groupType;
                    _result.Add(bakeGroup);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        static PVSBakeGroup[] CreateBakeGroupList(HashSet<List<Renderer>> selectedRenderers, out int _grassGroupBegin)
        {
            HashSet<PVSBakeGroup> result = CreateBakeGroupsForRenderers2(selectedRenderers);

            int terIndex = 0;
            foreach (var val in result)
            {
                val.RefreshTerrain();
                terIndex++;
            }

            var bakeGroups = result.ToArray();

            _grassGroupBegin = -1;
            for (int i = 0; i < bakeGroups.Length; i++)
            {
                if (bakeGroups[i].groupType == PVSBakeGroup.GroupType.Foliage)
                {
                    if (_grassGroupBegin == -1)
                    {
                        _grassGroupBegin = i;
                        break;
                    }
                }
            }

            return bakeGroups;
        }

        /// <summary>
        /// 
        /// </summary>
        static void CollAllObjs_FillSelectRenderList(Dictionary<int, List<GameObject>> dict, HashSet<List<Renderer>> selectedRenderers)
        {
            foreach (var curArea in dict)
            {
                foreach (var selectedGameObject in curArea.Value)
                {
                    var isObjCollect = IsObjCollectInBakeGroup(selectedGameObject, out List<Renderer> renderers);
                    if (!isObjCollect)
                        continue;

                    selectedRenderers.Add(renderers);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        static void CollAllObjs_AddTerrainPrefabList(Dictionary<int, List<GameObject>> dict)
        {
            List<GameObject> terrains = new List<GameObject>();
            GameObject obj = GameObject.Find(PVSDefine.TERRAIN_MESH_PVSROOT_NONE_NAME);
            if (obj)
            {
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    Transform tran = obj.transform.GetChild(i);
                    if (tran != null)
                    {
                        terrains.Add(tran.gameObject);
                    }
                }
            }

            GameObject FoliageProxy = GameObject.Find("FoliageProxy");
            if (FoliageProxy)
            {
                for (int i = 0; i < FoliageProxy.transform.childCount; i++)
                {
                    Transform tran = FoliageProxy.transform.GetChild(i);
                    if (tran != null)
                    {
                        terrains.Add(tran.gameObject);
                    }
                }
            }

            dict.Add(65535, terrains);
        }

        /// <summary>
        /// 
        /// </summary>
        static bool RendererFilter(Renderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }

            if ((!renderer.enabled || !renderer.gameObject.activeInHierarchy))
            {
                return false;
            }

            if (renderer.sharedMaterial && renderer.sharedMaterial.shader != null && renderer.sharedMaterial.shader.name == "LingRen/Scene/SceneObjUnlit")
            {
                return false;
            }

            if (renderer.sharedMaterial && renderer.sharedMaterial.shader != null && renderer.sharedMaterial.shader.name == "LingRen/Scene/Volumetrics/LightBeam")
            {
                return false;
            }

            if (renderer.sharedMaterial && renderer.sharedMaterial.shader != null && renderer.sharedMaterial.shader.name.Contains("LingRen/Particle"))
            {
                return false;
            }

            if (renderer.sharedMaterial && renderer.sharedMaterial.shader != null && renderer.sharedMaterial.shader.name.Contains("LingRen/Scene/Aurora"))
            {
                return false;
            }

            if (!PVSConstants.SupportedRendererTypes.Contains(renderer.GetType()))
            {
                return false;
            }

            PVSRendererTag rendererTag = renderer.GetComponent<PVSRendererTag>();

            if (rendererTag != null && rendererTag.ExcludeRendererFromBake)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        static void SetAllDistanceLODScriptEnable(bool _enable)
        {
            var callback = PVSBridge.onPVSProcDistanceLOD;
            if (callback == null)
                return;

            callback(_enable);
        }

        /// <summary>
        /// 
        /// </summary>
        static void ProcessTerrain()
        {
            PVSWrapBridge.onGenerateSectorObjectPositionInfo();
            PVSWrapBridge.onLoadRuntimeTerrainForPVS();

            GameObject obj = GameObject.Find(PVSDefine.TERRAIN_MESH_PVSROOT_NONE_NAME);
            if (obj == null)
                return;

            List<Renderer> renderers = new List<Renderer>();
            obj.GetComponentsInChildren<Renderer>(true, renderers);
            for (int i = 0; i < renderers.Count; i++)
            {
                renderers[i].gameObject.isStatic = true;
            }

            ShowTerrain(false);
        }

        /// <summary>
        /// 
        /// </summary>
        static bool HasEffectByRender(Renderer renderer)
        {
            var sharedMaterial = renderer.sharedMaterial;

            if (sharedMaterial != null && sharedMaterial.shader != null && sharedMaterial.shader.name == "LingRen/Scene/SceneObjUnlit")
            {
                return true;
            }

            if (sharedMaterial != null && sharedMaterial.shader != null && sharedMaterial.shader.name == "LingRen/Scene/Volumetrics/LightBeam")
            {
                return true;
            }

            if (sharedMaterial != null && sharedMaterial.shader != null && sharedMaterial.shader.name.Contains("LingRen/Particle"))
            {
                return true;
            }

            if (sharedMaterial != null && sharedMaterial.shader != null && sharedMaterial.shader.name.Contains("LingRen/Scene/PlaneVertexCloud"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        static void ShowTerrain(bool bShow)
        {
            List<GameObject> sectorList = new List<GameObject>();

            UnityEngine.Object[] allObjectsInScene = GameObject.FindObjectsOfType(typeof(GameObject));
            foreach (var item in allObjectsInScene)
            {
                if (item.name.Contains("SectorNode"))
                {
                    sectorList.Add((GameObject)item);
                }
            }

            foreach(var sector in sectorList)
            {
                if (sector == null)
                    continue;

                List<UnityEngine.Terrain> renderers = new List<UnityEngine.Terrain>();
                sector.GetComponentsInChildren<UnityEngine.Terrain>(true, renderers);

                foreach(var render in renderers)
                {
                    if (render == null) 
                        continue;

                    render.enabled = bShow;
                }
            }
        }
    }
}
