[← 返回索引](DETAILS.md) | [← 返回 README](../README.md)

# com.spacetime.pvswrap — 模块说明

---

## BakeData — 烘培数据

> `Runtime/Scripts/BakeData/`

负责定义、存储和序列化 PVS 烘培结果数据。

| 类 | 说明 |
|----|------|
| `PVSBakeData_Ver3` | 当前版本（Ver3）烘培数据主结构，使用压缩格式 |
| `PVSBakeDataSerialize` | 负责 BakeData 的二进制序列化与反序列化 |
| `PVSCellInfo` | 单个格子的可见性信息 |
| `PVSCommonBakeData` | 多 Volume 共享的基础数据 |
| `PVSRawData` | 原始（未压缩）烘培数据 |
| `PVSCompressRawData` | 压缩后的原始数据 |
| `PVSCompressRawDataChunk` | 压缩数据的块单元 |
| `PVSCompressRawDataChunkMgr` | 压缩块的管理与索引 |
| `PVSCompressRawDataGroup` | 压缩数据分组 |
| `PVSCompressShareRawData` | 跨 Volume 共享的压缩数据 |
| `PVSVolumeBakeData` | 单个 Volume 完整的烘培数据（含元数据） |
| `PVSVolumeBakeData_Editor` | Editor 专用的 Volume 烘培数据扩展 |
| `PVSVolumeBakeData_Obsolete` | 废弃版本兼容层 |

---

## Bridge — 桥接层

> `Runtime/Scripts/Bridge/`

连接 `com.spacetime.pvswrap` 与 `com.spacetime.pvs` 核心的接口层。

| 类 | 说明 |
|----|------|
| `PVSWrapBridge` | 向 `com.spacetime.pvs` 注册回调，接收烘培完成事件并写入 BakeData |

---

## Camera — 运行时相机

> `Runtime/Scripts/Camera/`

运行时 PVS 查询与渲染器显隐控制。

| 类 | 说明 |
|----|------|
| `PVSCamera` | 主相机组件，按帧查询当前 Cell 的可见集并设置渲染器 |
| `PVSCamera_Draw` | `PVSCamera` 的调试绘制扩展（partial） |
| `PVSCamera_SamplePosOffDebug` | 采样点偏移调试扩展（partial） |
| `PVSCameraMgr` | 管理场景中所有 `PVSCamera` 实例 |
| `PVSSamplePosOffset` | 采样点偏移数据（单条记录） |
| `PVSSamplePosOffsetMgr` | 采样点偏移集合管理，支持运行时动态修改 |

---

## Define — 常量定义

> `Runtime/Scripts/Define/`

| 类 | 说明 |
|----|------|
| `PVSDefine` | 全局常量：路径前缀（`Assets/SpaceTime`）、资源根目录等 |

---

## Gizmos — 场景可视化

> `Runtime/Scripts/Gizmos/`

在 Scene 视图中绘制辅助图形，帮助配置烘培区域。

| 类 | 说明 |
|----|------|
| `PVSGizmos` | 基础 Gizmos 组件 |
| `PVSGizmosMgr` | 管理所有 Gizmos 组件 |
| `PVSCamSamDefaultOffAreaGizmos` | 标记默认关闭采样的相机区域 |
| `PVSCamSamDefaultOffAreaGizmosMgr` | 上述组件的管理器 |
| `PVSForceSampleAreaMgr` | 强制采样区域管理器 |
| `PVSIgnorePointAreaGizmos` | 标记忽略采样点的区域 |
| `PVSIgnorePointAreaGizmosMgr` | 上述组件的管理器 |
| `PVSMaxDensityAreaGizmos` | 标记最大采样密度区域 |
| `PVSMaxDensityAreaGizmosMgr` | 上述组件的管理器 |

---

## Octree — 场景八叉树

> `Runtime/Scripts/Octree/`

基于场景几何体构建八叉树，用于辅助采样点分布。

| 类 | 说明 |
|----|------|
| `PVSOctreeNode` | 八叉树节点 |
| `PVSSceneOctree` | 场景八叉树，构建与查询接口 |

---

## SamplingProvider — 扩展采样点模式

> `Runtime/Scripts/SamplingProvider/`

对 `com.spacetime.pvs` 采样点系统的具体实现与扩展。

| 类 | 说明 |
|----|------|
| `BaseSamplingPointMode` | 采样点模式基类 |
| `CanMoveSamplingProvider` | 可移动对象的采样提供者 |
| `HighSamplingPointMode` | 高密度采样模式 |
| `HighLeftRightSamplingPointMode` | 左右高密度采样模式 |
| `HighLeftRightDownSamplingPointMode` | 上下左右高密度采样模式 |
| `ManualSplitSamplingPointMode` | 手动分割采样模式 |
| `ManualSplitSamplingProvider` | 手动分割采样提供者 |

---

## Utils — 工具函数

> `Runtime/Scripts/Utils/`

| 类 | 说明 |
|----|------|
| `PVSAreaUtils` | 区域范围计算工具 |
| `PVSBakeDataUtils` | BakeData 读写辅助函数 |
| `PVSBakeDataUtils_Ver3` | Ver3 格式专用读写工具 |
| `PVSBakerUtils` | 烘培参数计算与准备 |
| `PVSCameraUtils` | 相机相关工具（视锥、位置等） |
| `PVSCompressRawDataUtils` | 压缩数据构建与解压工具 |
| `PVSMathUtils` | PVS 专用数学工具 |
| `PVSOctreeUtils` | 八叉树构建辅助 |
| `PVSVolumeUtils` | 场景渲染器收集、BakeGroup 构建 |
| `PVSSceneMergeUtils` | *(需 `com.lingren.scenemerge`)* 多场景合并数据工具 |

---

## Validation — 数据校验

> `Runtime/Scripts/Validation/`

| 类 | 说明 |
|----|------|
| `PVSBakeDataValidation` | 校验 BakeData 完整性与格式版本 |
| `PVSSampleOffsetMaskValid` | 校验采样偏移掩码合法性 |

---

## Volume — PVS Volume

> `Runtime/Scripts/Volume/`

| 类 | 说明 |
|----|------|
| `PVSVolume` | 主 MonoBehaviour，定义烘培范围、渲染器分组，支持 Scene 手柄缩放 |
| `PVSVolume_Draw` | `PVSVolume` 的 Gizmos 绘制扩展（partial） |

---

## 编辑器扩展

> `Editor/Scripts/`

| 类 | 位置 | 说明 |
|----|------|------|
| `ScenePVSExport` | Export/ | 核心烘培调用入口，封装 `PVSAPI.Bake` |
| `PVSVolumeEditor` | Other/ | `PVSVolume` 的 CustomEditor |
| `PVSVolumeEditor_SceneGUI` | Other/ | Scene GUI 手柄绘制（partial） |
| `PVSCameraEditor` | Other/ | `PVSCamera` 的 CustomEditor |
| `PVSMenuOptions` | Other/ | Unity 菜单项（SpaceTime → PVS） |
| `PVSEditUtils` | Utils/ | 编辑器工具函数（对象收集、全局节点检查） |
| `PVSGizmosInspector` | Gizmos/ | Gizmos 基础 Inspector |
| `PVSCamSamDefaultOffAreaGizmosInspector` | Gizmos/ | 默认关闭区域 Inspector |
| `PVSIgnorePointAreaGizmosInspector` | Gizmos/ | 忽略点区域 Inspector |
| `PVSMaxDensityAreaGizmosInspector` | Gizmos/ | 最大密度区域 Inspector |
| `PVSSamplePosOffsetInspector` | Inspector/ | 采样点偏移 Inspector |

---

[← 返回索引](DETAILS.md) | [← 返回 README](../README.md)
