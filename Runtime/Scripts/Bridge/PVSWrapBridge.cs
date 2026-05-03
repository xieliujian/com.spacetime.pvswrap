using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    public delegate bool IsSceneObjectGroup(LODGroup lodGroup);
    public delegate bool Voxel_IsCanWalk(Vector3 pos, List<Vector3> result, string sceneId = "");
    public delegate bool Voxel_IsInCanWalkVoxelArea(Vector3 pos, out Vector3 voxelPos, string sceneId = "");
    public delegate Dictionary<int, List<GameObject>> SceneTools_GetAllPrefabs();
    public delegate List<GameObject> GetForceSampleAreaObjListFunc(bool isReadableSet);

    /// <summary>
    /// 
    /// </summary>
    public class PVSWrapBridge
    {
        public static Voxel_IsCanWalk voxelIsCanWalk;
        public static Voxel_IsInCanWalkVoxelArea voxelIsInCanWalkVoxelArea;
        public static IsSceneObjectGroup isSceneObjectGroup;
        public static SceneTools_GetAllPrefabs onSceneToolsGetAllPrefabs;
        public static PVSVoidFunc onGenerateSectorObjectPositionInfo;
        public static PVSVoidFunc onLoadRuntimeTerrainForPVS;

        public static PVSBoolFunc onResToolsEnableSet;
        public static GetForceSampleAreaObjListFunc onGetForceSampleAreaObjList;
        public static PVSVoidFunc onUnpackAllSectorNodeFunc;
    }
}

