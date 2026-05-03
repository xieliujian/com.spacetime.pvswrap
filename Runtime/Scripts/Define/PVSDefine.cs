using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public enum PVSBakeDataVer
    {
        Ver3 = 3,
        Ver4,
        Ver5,           // 增加偏移采样标志
        Ver6,           // ChunkIdx从ushort改为uint, 突破65535限制
        Ver7,           // PVSCellInfo的bound计算增加了Volume的位置，导致运行时bound因为加载距离被卸载，没有数据
    }

    /// <summary>
    /// 
    /// </summary>
    public enum PVSAlignment
    {
        LowerLeft,
        MiddleCenter,
        UpperRight,
    }

    /// <summary>
    /// 
    /// </summary>
    public enum PVSCoordSystem
    {
        // 以(0,0)为中心的坐标系（常用于数学、物理）
        CenterOrigin,

        // 以左下角(0,0)为起始点的坐标系（常用于图像、游戏）
        BottomLeftOrigin,
    }

    /// <summary>
    /// 
    /// </summary>
    public enum PVSSamplePosOffsetMask
    {
        UpMask = 1 << 0,
        DownMask = 1 << 1,
        LeftMask = 1 << 2,
        RightMask = 1 << 3,
        ForwardMask = 1 << 4,
        BackMask = 1 << 5,

        CamSampleOffsetDefaultType = 1 << 6,
    }

    /// <summary>
    /// 
    /// </summary>
    public enum PVSVolumeGizmosType
    {
        None = 0,
        Default,
        ManualSplit,
    }

    /// <summary>
    /// 
    /// </summary>
    public enum PVSCamSampleOffsetType
    {
        Default,
        OnlySelf,
        Type1,
        Type2,
        CameraDir,
    }

    public delegate void DialogEvent();

    /// <summary>
    /// 
    /// </summary>
    public class PVSDefine
    {
        /// <summary>
        /// 版本入口
        /// </summary>
        public static readonly int s_CurBakeDataVer = (int)PVSBakeDataVer.Ver7;

        /// <summary>
        /// 
        /// </summary>
        public static readonly int s_ChunkSize = 128;
        public static readonly int s_MapSize = 1024;

        /// <summary>
        /// 
        /// </summary>
        public static readonly float s_GameCamMinDistance = -0.01f;
        public static readonly float s_GameCamMaxDistance = 15f;
        public static readonly float s_OffsetUnitSize = 3f;
        public static readonly float s_GameCamMaxZDownDis = 9f;

        /// <summary>
        /// 
        /// </summary>
        public static readonly float s_GizmosAreaAlpha = 0.5f;
        public static readonly Color s_MaxDensityAreaColor = Color.red;
        public static readonly Color s_IgnorePointAreaColor = Color.cyan;
        public static readonly Color s_CamSampleOffAreaColor = Color.green;
        public static readonly Color s_ForceSampleAreaColor = Color.yellow;

        /// <summary>
        /// 
        /// </summary>
        public static readonly string s_Baker_IgnorePVS_Tag = "Ignore_PVS";
        public static readonly float s_Baker_AlphaClip_75 = 0.51f;
        public static readonly float s_Baker_AlphaClip_95 = 1.0f;

        /// <summary>
        /// 
        /// </summary>
        public static readonly string s_StartAreaDesc = "------------------------------------注释------------------------------------";
        public static readonly string s_EndAreaDesc = "------------------------------------------------------------------------";
        public static readonly string s_MaxDensityAreaDesc = "用来框选密度最大的Cell区域";
        public static readonly string s_IgnorePointAreaDesc = "用来框选忽略采样的区域";
        public static readonly string s_CamSamDefaultOffAreaDesc = "用来框选相机Default范围采样类型的区域";
        public static readonly string s_ForceSampleAreaDesc = "用来框选强制采样的区域";

        public static readonly string s_VolumeOctreeModeFormat = "当前是八叉树切割模式, VolumeSize请设置成 {0} 的整数倍";
        public static readonly string s_OctreeVolumeSizeInvalidFormat = "当前是八叉树切割模式, CellSize {0} : {1} 不能被 {2} 整除, 填入参考值 {3}";
        public static readonly string s_VolumeSizeInvalidFormat = "CellSize {0} : {1} 不能被 {2} 整除, 填入参考值 {3}";

        public static readonly string s_Bake_VolumeSizeError = "VolumeSize不能整除CellSize, 请检查 PVSVolume 脚本查看信息";
        public static readonly string s_CheckSamplePosOffsetMaskDesc = "检测采样点偏移遮罩";

        public static readonly string s_ForceSampleExpPointDesc = "强制采样外扩点";
        public static readonly string s_ShowAllManualSplitPointDesc = "显示所有的手动拆分点";
        public static readonly string s_ClearSaveManualSplitPointDesc = "清理所有的手动拆分点";
        public static readonly string s_AreaCellInfoDesc = "显示Cell区域";
        public static readonly string s_ShowCamOffsetDefaultTypeDesc = "默认采样和方向采样显示";

        public static readonly string s_CamSampleOffsetTypeDesc = "相机采样偏移类型";
        public static readonly string s_IsDebugSamplePosOffsetDesc = "调试采样点偏移";
        public static readonly string s_IsTestIgnorePointDesc = "测试忽略点";

        public static readonly string s_OpenCamMaxDisOffsetDesc = "打开摄像机最大距离偏移采样";
        public static readonly string s_CamMaxDisOffsetDesc = "摄像机最大距离偏移采样值";

        public static readonly string s_OpenBakerFovSelDesc = "打开烘培器Fov选择开关";
        public static readonly string s_BakerFov90Desc = "烘培器Fov选择90度烘培";
        public static readonly string s_OpenIgnoreRayCheckDefaultOffsetTypeDesc = "打开忽略射线检测采样默认类型";
        public static readonly string s_IgnoreRayCheckDefaultOffsetTypeDesc = "忽略射线检测采样默认类型";

        public static readonly string s_SceneStreamer_CamSamDefaultOffAreaGizmosHide = "PVSCamSamDefaultOffAreaGizmos 节点隐藏了";
        public static readonly string s_SceneStreamer_IgnorePointAreaHide = "PVSIgnorePointArea 节点隐藏了";
        public static readonly string s_GUI_CanPVSExportDesc = "没有导出PVS数据";

        /// <summary>
        /// 
        /// </summary>
        public const string TERRAIN_MESH_PVSROOT_NONE_NAME = "TerrainMeshPVSRootNode";
        public static readonly string s_BakeGroupFoliageNameTag = "_Cube_Grass";
        public static readonly string s_VolumeName = "Global/CullingVolume";

        /// <summary>
        /// 
        /// </summary>
        public static readonly Vector3 s_OctreeChildNumVec = new Vector3(2, 2, 2);
        public static readonly int s_OctreeChildNum = 8;
        public static readonly int s_OctreeMaxLeafSize = 16;
        public static readonly int s_OctreeMinLeafSize = 4;
        public static readonly int s_OctreeAutoMinLeafSize = 8;
        public static readonly int s_OctreeMaxLeafNum = (int)Mathf.Log(s_OctreeMaxLeafSize, 2) - (int)Mathf.Log(s_OctreeMinLeafSize, 2) + 1;
        public static readonly Vector3 s_OctreeMinLeafSizeVec = new Vector3(s_OctreeMinLeafSize, s_OctreeMinLeafSize, s_OctreeMinLeafSize);
        
        public static readonly int s_GizmosVolumeOctreeRange = 2;
        public static readonly int s_GizmosVolumeUnOctreeRange = 3;
        public static readonly Color s_GizmosVolumeUnOctreeColor = new Color(0.5f, 0.25f, 0f, 0.3f);
        public static readonly Color s_GizmosVolumeOctreeNeighborColor = new Color(1, 1, 0, 0.5f);

        public static readonly Color s_Gizmos_CamSamplePosOffset_CamDir_Color = Color.green;
        public static readonly Color s_Gizmos_CamSamplePosOffset_Default_Color = Color.red;

        public static readonly Color[] s_GizmosVolumeOctreeColorArray =
        {
            new Color(1, 0, 0, 0.5f),
            new Color(0, 1, 0, 0.5f),
            new Color(0, 0, 1, 0.5f),
            new Color(1, 1, 0, 0.15f),
            new Color(1, 0, 1, 0.15f),
            new Color(0, 1, 1, 0.15f),
        };

        /// <summary>
        /// 
        /// </summary>
        public static readonly string[] s_PVSSamplePosOffsetMaskNameArray = {
            "上",
            "下",
            "左",
            "右",
            "前",
            "后",
        };

        /// <summary>
        /// 
        /// </summary>
        public static readonly float s_SamplePosValidRange = 100f;
        public static readonly int s_BakeDataSplatSize = 100;

        /// <summary>
        /// RawData分组经过性能测试，2 * 2格子压缩最大
        /// </summary>
        public static readonly float s_UnOctreeRawDataGroupUnitSize = 10;
        public static readonly int s_UnOctreeRawDataGroupUnitRange = 2;
        public static readonly float s_OctreeRawDataGroupUnitSize = s_OctreeMinLeafSize;
        public static readonly int s_OctreeRawDataGroupUnitRange = 2;

        /// <summary>
        /// 
        /// </summary>
        public static readonly byte s_EmptyByte = 0;
        public static readonly byte s_OneByte = 1;

        /// <summary>
        /// 
        /// </summary>
        public static readonly Vector3[] s_OffsetArray = new Vector3[]
        {
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1),
            new Vector3(1, 0, -1),

            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1),

            new Vector3(-1, 0, 0),
            new Vector3(-1, 0, 1),
            new Vector3(-1, 0, -1),
        };

        /// <summary>
        /// 
        /// </summary>
        public static readonly Vector3[] s_CamSamplePosOffsetArray = new Vector3[]
        {
            new Vector3(0, 0, 0),

            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1),
            new Vector3(1, 0, 1),
            new Vector3(-1, 0, -1),
            new Vector3(-1, 0, 1),
            new Vector3(1, 0, -1),

            new Vector3(0, 1, 0),
            new Vector3(0, 1, 1),
            new Vector3(-1, 1, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, -1),

            new Vector3(0, -1, 0),
            new Vector3(1, -1, 0),
            new Vector3(0, -1, 1),
            new Vector3(-1, -1, 0),
            new Vector3(0, -1, -1),
        };

        /// <summary>
        /// 
        /// </summary>
        public static readonly Vector3[] s_CamSamplePosOffset_CamDir_Array = new Vector3[]
        {
            new Vector3(0, 0, 0),

            new Vector3(1, 0, 0),       // 水平四个点
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1),

            new Vector3(0, 1, 0),   // 上下

            new Vector3(0, -1, 0),
        };

        /// <summary>
        /// 
        /// </summary>
        public static readonly Vector3[] s_CamSamplePosOffsetArrayOnlySelf = new Vector3[]
        {
            new Vector3(0, 0, 0),
        };

        /// <summary>
        /// 
        /// </summary>
        public static readonly Vector3[] s_CamSamplePosOffsetArrayCross = new Vector3[]
        {
            new Vector3(0, 0, 0),

            new Vector3(0, 1, 0),
            new Vector3(0, -1, 0),

            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1),
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
        };

        /// <summary>
        /// 
        /// </summary>
        public static readonly Vector3[] s_CamSamplePosOffsetArrayCross2 = new Vector3[]
        {
            new Vector3(0, 0, 0),

            new Vector3(0, 1, 0),
            new Vector3(0, -1, 0),

            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1),
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),

            new Vector3(0, 1, -1),
            new Vector3(0, 1, 1),
            new Vector3(1, 1, -1),
            new Vector3(1, 1, 1),
        };

        /// <summary>
        /// Shader
        /// </summary>
        public static readonly int s_ShaderSurfacePropId = Shader.PropertyToID("_Surface");

        /// <summary>
        /// 
        /// </summary>
        public static IActiveSamplingProvider s_DefaultSamplingProvider = new DefaultActiveSamplingProvider();
        public static IActiveSamplingProvider s_CanMoveSamplingProvider = new CanMoveSamplingProvider();
    }
}

