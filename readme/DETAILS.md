[← 返回 README](../README.md)

# com.spacetime.pvswrap — 详细文档索引

## 架构说明

```
[场景]
  │
  ▼
PVSVolume            ← 定义烘培范围与渲染器分组
  │
  ├─ PVSVolumeUtils  ← 收集场景渲染器，构建 BakeGroup
  │
  ├─ PVSSceneOctree  ← 构建八叉树，生成采样点
  │
  ▼
ScenePVSExport       ← 编辑器入口：调用 com.spacetime.pvs 核心烘培
  │
  ▼
BakeData             ← 序列化压缩存储（Ver3 格式）
  │
  ▼
PVSCamera / PVSCameraMgr  ← 运行时按帧查询可见集，控制渲染器显隐
```

**分工边界：**

| 包 | 职责 |
|----|------|
| `com.spacetime.pvs` | 核心烘培算法、采样系统、渲染管线控制 |
| `com.spacetime.pvswrap` | 数据收集、数据存储、烘培调用、运行时剔除 |

---

## 子文档

| 文档 | 内容 |
|------|------|
| [modules.md](modules.md) | BakeData / Bridge / Camera / Gizmos / Octree / SamplingProvider / Utils / Volume / 编辑器扩展 |
| [workflows.md](workflows.md) | 烘培流程 / 运行时剔除流程 / 多场景合并 / 扩展采样提供者 |

---

[← 返回 README](../README.md)
