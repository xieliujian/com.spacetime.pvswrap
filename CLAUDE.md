# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

`com.spacetime.pvswrap` 是 SpaceTime 项目的 PVS Wrap 包，对 `com.spacetime.pvs` 进行封装，提供更高层次的接口与集成。

## Project Structure

### Package (`Packages/com.spacetime.pvswrap/`)

- **Runtime/Scripts/** - 运行时脚本
- **Editor/Scripts/** - Unity Editor 工具

### Assembly Structure

- `com.spacetime.pvswrap.runtime` - 运行时程序集
- `com.spacetime.pvswrap.editor` - 编辑器程序集

## Coding Conventions

与 `com.spacetime.core` 保持一致，详见 `Packages/com.spacetime.core/CLAUDE.md`。

### Naming

| 类别 | 规则 | 示例 |
|------|------|------|
| 私有实例字段 | `m_` 前缀 + PascalCase | `m_PVSManager`, `m_Config` |
| 私有/内部静态字段 | `s_` 前缀 + PascalCase | `s_Instance` |
| 属性 | camelCase | `pvsManager`, `isReady` |
| 公共方法 | PascalCase | `DoInit`, `Wrap`, `Query` |
| 命名空间 | `ST.PVSWrap` |  |

### Access Modifiers

省略 `private` 关键字（默认即为私有，无需显式声明）。

### Null Guard Style

```csharp
// Correct
if (m_PVSManager == null)
    return;

m_PVSManager.Query();
```

### XML Documentation Comments

所有类、方法、字段、属性均须添加中文 XML 文档注释。

### Logging

统一使用 `Logger` 静态类，禁止直接调用 `UnityEngine.Debug`。

### Section Dividers

```csharp
// ──────────────────────────────────────────
// 初始化 / 封装
// ──────────────────────────────────────────
```

## Key Dependencies

- Unity 2022.3.35f1c1
- com.spacetime.core
- com.spacetime.pvs
