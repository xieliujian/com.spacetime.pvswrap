# com.spacetime.pvswrap

SpaceTime PVS 上层封装包，负责**数据收集、数据存储与烘培调用**，构建于 `com.spacetime.pvs`（核心烘培）之上，提供面向具体场景的完整工作流。

---

## 参考

本包整体架构参考自以下 Unity 插件：

- [Perfect Culling - Occlusion Culling System](https://assetstore.unity.com/packages/tools/utilities/perfect-culling-occlusion-culling-system-193611) by KKKoenigz — Unity Asset Store

---

## 功能概述

| 职责 | 说明 |
|------|------|
| **数据收集** | 扫描场景渲染器、管理 Volume 范围、构建 Octree 与采样点 |
| **数据存储** | BakeData 序列化/反序列化，支持版本化压缩数据格式 |
| **烘培调用** | 封装烘培流程入口（`ScenePVSExport`），衔接 `com.spacetime.pvs` 核心 |
| **运行时剔除** | `PVSCamera` / `PVSCameraMgr` 按帧查询可见集并控制渲染器显隐 |
| **编辑器辅助** | Inspector、Gizmos、菜单项、Scene GUI 手柄等编辑器扩展 |

---

## 依赖

| 包 | 职责 |
|----|------|
| `com.spacetime.core` | 基础工具（RapidList、GridMath、IO、日志） |
| `com.spacetime.pvs` | 核心烘培算法与采样系统 |
| `com.lingren.scenemerge` *(可选)* | 多场景合并支持，缺席时相关代码自动剔除 |

---

## 命名空间

所有运行时与编辑器代码均使用：

```
ST.PVS
```

---

## 目录结构

```
com.spacetime.pvswrap/
├── Runtime/Scripts/
│   ├── BakeData/          # 烘培数据结构与序列化
│   ├── Bridge/            # 与 com.spacetime.pvs 的桥接层
│   ├── Camera/            # 运行时相机采样与剔除控制
│   ├── Define/            # 全局常量定义
│   ├── Gizmos/            # 场景辅助可视化组件
│   ├── Octree/            # 场景八叉树（用于采样点生成）
│   ├── SamplingProvider/  # 扩展采样点模式
│   ├── Utils/             # 工具函数（烘培、相机、数学等）
│   ├── Validation/        # 烘培数据校验
│   └── Volume/            # PVSVolume 主体与绘制
└── Editor/Scripts/
    ├── Export/            # 场景 PVS 数据导出入口
    ├── Gizmos/            # Gizmos Inspector
    ├── Inspector/         # 采样点偏移 Inspector
    ├── Other/             # VolumeEditor、CameraEditor、菜单
    └── Utils/             # 编辑器工具函数
```

---

## 快速开始

1. 在场景中添加 **PVSVolume** 组件，配置 `volumeSize` 与 `bakeGroups`。
2. 在编辑器菜单执行 **SpaceTime → PVS → Gen PVS Data** 触发烘培。
3. 运行时挂载 **PVSCamera** 到主摄像机，自动启用 PVS 剔除。

---

## 详细文档

| 文档 | 内容 |
|------|------|
| [DETAILS.md](readme/DETAILS.md) | 架构说明与子文档索引 |
| [modules.md](readme/modules.md) | 各模块详细说明 |
| [workflows.md](readme/workflows.md) | 烘培流程 / 运行时剔除 / 扩展采样 |
