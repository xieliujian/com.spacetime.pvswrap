[← 返回索引](DETAILS.md) | [← 返回 README](../README.md)

# com.spacetime.pvswrap — 工作流程

---

## 1. 烘培流程（编辑器）

```
编辑器菜单 / ScenePVSExport.GenPvsData()
        │
        ├─ PVSVolumeUtils.CollAllObjs()      ← 收集场景中所有可见渲染器，构建 BakeGroup
        │       │
        │       └─ (可选) PVSSceneMergeUtils.FillBakeGroupSet()  ← 多场景合并模式下收集
        │
        ├─ PVSVolume.bakeGroups = ...        ← 写入 Volume 组件
        │
        ├─ PVSAPI.Bake.StartBake()           ← 调用 com.spacetime.pvs 核心烘培
        │
        └─ PVSWrapBridge（回调）
                │
                ├─ 接收烘培完成事件
                ├─ PVSBakeDataUtils_Ver3 构建 BakeData
                └─ PVSBakeDataSerialize 序列化写入磁盘（Assets/SpaceTime/...）
```

### 关键步骤说明

1. **数据收集**：`PVSVolumeUtils.CollAllObjs` 扫描场景中处于 Volume 范围内的 Renderer，按草地 / 普通对象分组，返回 `PVSBakeGroup[]`。
2. **烘培触发**：`ScenePVSExport.GenPvsData` 将分组结果写入 `PVSVolume`，再调用 `PVSAPI.Bake.StartBake`，由 `com.spacetime.pvs` 执行核心采样与 GPU 渲染。
3. **数据写入**：烘培完成后，`PVSWrapBridge` 收到回调，将结果压缩为 Ver3 格式并序列化到 `Assets/SpaceTime/` 目录下对应的 `.bytes` 文件。

---

## 2. 运行时剔除流程

```
PVSCameraMgr.Update()
        │
        └─ foreach PVSCamera
                │
                ├─ 获取当前相机世界坐标
                ├─ GridMath.GetIndexForWorldPos() → Cell 索引
                ├─ PVSBakeData_Ver3.GetVisibleRenderers(cellIndex)  ← 查询可见集
                └─ 批量设置 Renderer.enabled（显示/隐藏）
```

### 采样点偏移

`PVSSamplePosOffsetMgr` 在运行时动态维护一组偏移向量，用于修正相机实际采样位置（例如第一人称视角高度补偿），避免因视点偏差导致错误的可见集查询。

---

## 3. 多场景合并支持（可选）

当项目安装了 `com.lingren.scenemerge` 包时，编译器自动定义 `LR_SCENE_MERGE_ENABLED`，以下额外逻辑生效：

- `PVSVolumeUtils.CollAllObjs` 调用 `PVSSceneMergeUtils.FillBakeGroupSet`，跨场景收集渲染器。
- `PVSVolume` 持有 `SceneMergeCellExportBlock[]` 字段，支持多场景导出块标记。
- `ScenePVSExport` 在导出前调用 `PVSVolume.ClearSceneMergeBlockList()` 清理旧数据。

若未安装 `com.lingren.scenemerge`，上述代码被预处理器剔除，包可正常编译与运行。

---

## 4. 扩展采样提供者

`com.spacetime.pvswrap` 提供了多种基于场景具体需求的采样点模式，补充 `com.spacetime.pvs` 的默认采样逻辑：

| 模式 | 适用场景 |
|------|----------|
| `HighSamplingPointMode` | 高处视点（如城墙、平台） |
| `HighLeftRightSamplingPointMode` | 高处 + 水平偏移视点 |
| `HighLeftRightDownSamplingPointMode` | 高处 + 水平 + 俯视视点 |
| `ManualSplitSamplingPointMode` | 手动分割格子以增加采样密度 |
| `CanMoveSamplingProvider` | 可移动对象（如载具）的采样提供者 |

**注册方式：** 在烘培前通过 `PVSAPI` 将自定义提供者注册到 `com.spacetime.pvs`，烘培完成后由核心按提供者配置生成采样点。

---

## 5. 注意事项

- **资源路径前缀**：所有生成的烘培数据默认存放于 `Assets/SpaceTime/` 下，路径常量定义在 `PVSDefine`。
- **BakeData 版本**：当前使用 Ver3 格式（压缩块存储），旧版数据可通过 `PVSVolumeBakeData_Obsolete` 兼容读取。
- **草地分组**：`PVSVolume.grassGroupBegin` 标记草地分组起始索引，运行时相机可独立控制草地渲染器的显隐策略。
- **线程安全**：烘培过程中数据收集在主线程执行，GPU 采样由 `com.spacetime.pvs` 管理，写盘在烘培完成回调中进行，无需额外同步。

---

[← 返回索引](DETAILS.md) | [← 返回 README](../README.md)
