[← 返回索引](DETAILS.md) | [← 返回 README](../README.md)

# com.spacetime.pvswrap — 数据存储系统

---

## 1. 总体架构

```
PVSBakeData（抽象基类，ScriptableObject）
    └── PVSVolumeBakeData（具体实现）
            │
            ├── rawDataIdxArray（byte[]）   ← 主索引文件，序列化进 ScriptableObject
            ├── cellInfoList                ← Chunk 空间信息
            │
            ├── [运行时] rawData[]          ← 烘焙中间结果（非序列化）
            ├── [运行时] exBinData          ← 已加载的 Chunk 二进制数据
            └── [运行时] exLoadFileLength   ← 已加载文件的字节长度
```

数据存储分为两层：

| 层 | 载体 | 内容 |
|----|------|------|
| **主索引层** | `rawDataIdxArray`（ScriptableObject 字段） | 每个采样点 → (chunkIdx, rawIdx) 的映射表 |
| **数据层** | `occlusion_{chunkIdx}.bytes` 文件 | 按空间分块的压缩可见性数据 |

---

## 2. 核心数据结构

### 2.1 RawData / PVSRawData

烘焙阶段每个采样点的原始可见性数据：

```csharp
struct RawData {
    ushort[] uncompressed;   // 可见渲染器索引列表（排序后）
}
```

`uncompressed` 中每个 `ushort` 对应一个可见渲染器在 `bakeGroups` 中的全局索引。烘焙完成后通过压缩系统写入磁盘，运行时从磁盘解码恢复。

---

### 2.2 VisibilitySetRow / VisibilitySet2 — 高低字节压缩编码

`ushort` 索引（最大 65535）被拆分为高字节（`height`）和低字节（`low`）：

```
ushort index  →  FlattenUShort2Byte(index, out byte height, out byte low)
             →  height = index >> 8,  low = index & 0xFF
```

按 `height` 分组存储，相同高字节的所有低字节放在同一行：

```csharp
struct VisibilitySetRow {
    byte   height;    // 高字节（行号）
    byte[] lowData;   // 该行所有低字节
}

struct VisibilitySet2 {
    VisibilitySetRow[] data;   // 只存有数据的行，稀疏存储
}
```

**效果**：当许多渲染器索引的高字节相同时，可减少存储的 `height` 重复次数，压缩率随渲染器索引聚集程度提高。

运行时还原：

```csharp
ushort index = GridMath.UnflattenByte2UShort(height, low);
// = (height << 8) | low
```

---

### 2.3 PVSCompressShareRawData — 组内共享数据

同一空间小组内（`PVSCompressRawDataGroup`）的多个采样点共享一份公共可见集：

```
Group 内所有采样点的 srcRawData 取交集 → shareData（公共渲染器）
每个采样点存储：modifyRawData = srcRawData - shareData（差量）
```

运行时读取：`完整可见集 = shareData + modifyRawData`

这利用了空间相邻点可见集相似性，大幅降低差量数据体积。

---

### 2.4 PVSCompressRawDataGroup — 空间分组

每个 `Group` 对应三维空间中的一个单元格（`Vector3Int groupKey`），由采样点世界坐标经 `GridMath.UnflattenToXYZ` 量化得到，量化粒度由 `rawGroupUnitRange` 控制（Octree 模式与非 Octree 模式粒度不同）。

```
Group 职责：
├── rawDataList        ← 该组内所有采样点的差量数据
└── shareRawData       ← 该组的公共可见集（共享，仅一份）
```

---

### 2.5 PVSCompressRawDataChunk — 空间分块（Chunk）

Chunk 对应磁盘上一个 `.bytes` 文件，按世界坐标的 XZ 宫格分区：

```csharp
int chunkIdx = PosToIndex(samplePos);
// chunkIdx = (z / chunkSize) * (mapSize / chunkSize) + (x / chunkSize)
// mapSize = s_MapSize, chunkSize = s_ChunkSize
```

每个 Chunk 包含若干 Group，每个 Group 包含若干采样点：

```
Chunk
├── groupDict: Vector3Int → PVSCompressRawDataGroup
├── rawDataList: PVSCompressRawData[]        ← 所有采样点（扁平化）
└── shareRawDataList: PVSCompressShareRawData[]  ← 所有 Group 的共享数据
```

---

### 2.6 PVSBakeDataSerialize — 运行时二进制读取器

持有已加载的单个 Chunk 字节数据，提供偏移索引访问：

```csharp
class PVSBakeDataSerialize {
    bool useNative;
    byte[]           datas;        // 标准托管数组（useNative=false）
    NativeArray<byte> nativeDatas; // 非托管数组（useNative=true，零 GC 读取）
}
```

两种模式读取 `int`/`uint` 的差异：

| 模式 | 读取方式 | 适用场景 |
|------|----------|----------|
| `useNative=false` | `BitConverter.ToInt32(datas, offset)` | 通用，托管内存 |
| `useNative=true` | `UnsafeUtility.ReadArrayElement<int>(ptr, 0)` | 高频读取，避免数组边界检查开销 |

---

### 2.7 PVSCellInfo — Chunk 空间元数据

```csharp
class PVSCellInfo {
    int    cellID;    // Chunk 索引
    int    pointNum;  // 该 Chunk 内的采样点数量
    Bounds bounds;    // Chunk 的世界空间包围盒
}
```

用于运行时流式加载决策（相机进入某 Bounds 时触发对应 Chunk 的加载）。

---

## 3. 烘焙写入流程（CompleteBake）

```
rawData[]（所有采样点原始 ushort[] 数据）
        │
        ▼
PVSCompressRawDataChunkMgr.Init()
    │
    ├─ PosToIndex(samplePos) → chunkIdx → 创建/缓存 PVSCompressRawDataChunk
    │
    └─ chunk.AddGroup(sampleIdx, samplePos, rawGroupUnitRange, ref rawData)
            │
            ├─ GridMath.UnflattenToXYZ → groupKey (Vector3Int)
            └─ group.Fill() → PVSCompressRawData（记录 srcRawData）

chunkMgr.CalcAllChunk()
    └─ 每个 Group.Calc():
            ├─ FindCommonNumbers(rawDataList) → shareData（交集）
            ├─ CalcModifyRawData(shareData) → 每点 modifyRawData（差量）
            └─ CompressModifyRawData() → VisibilitySet2（高低字节压缩）

chunkMgr.FillIndexArray(idxArray, saveChunkIdxArray, saveIdxArray)
    └─ 建立 sampleIdx → (chunkIdx, rawIdx) 映射

PVSBakeDataUtils.SerializeWrite(saveBigVisIndex, saveChunkIdxArray, saveIdxArray)
    └─ 生成 rawDataIdxArray（主索引，写入 ScriptableObject）

PVSCompressRawDataUtils.SaveBinData()
    └─ 每个 Chunk → SerializeWriteChunk() → occlusion_{chunkIdx}.bytes
```

---

## 4. 二进制文件格式

### 4.1 主索引文件（rawDataIdxArray，存储于 ScriptableObject）

所有采样点被分成若干"Splat"块，每块最多 `s_BakeDataSplatSize` 个条目：

```
[splatCount × int]:   每个 Splat 块在 data 区的起始偏移
[Splat[0]]:
    [hasData: byte]      ← 0 表示该 Splat 全为无效数据
    [entry × N]:
        [chunkIdx: byte]                              // 1 字节
        [rawIdx: ushort（若 saveBigVisIndex=false）]  // 2 字节
        [rawIdx: uint（若 saveBigVisIndex=true）]     // 4 字节
[Splat[1]]: ...
```

`saveBigVisIndex` 在采样点总数 ≥ 65535 时自动启用，将 rawIdx 扩展为 4 字节 uint。

运行时查找：

```
posIdx → splatIdx = posIdx / splatSize, localOff = posIdx % splatSize
→ 读 Splat[splatIdx] 的 entry[localOff] → (chunkIdx, rawIdx)
```

### 4.2 Chunk 数据文件（occlusion_{chunkIdx}.bytes）

```
[chunkIdx: int]                                    // 4 字节，Chunk 标识

[per-entry 索引表]:  共 rawDataList.Count 个条目
    [rawDataOffset: int]                           // 该条目 rawData 块的绝对偏移
    [shareRawDataOffset: int]                      // 对应 shareRawData 块的绝对偏移
    [samplePosOffsetMask: uint]（可选）            // 采样偏移掩码

[rawData 数据区]:    每条目一个 VisibilitySet2
[shareRawData 数据区]: 每个 Group 一个 VisibilitySet2
```

### 4.3 VisibilitySet2 二进制布局

```
[rowCount: int]                       // 有效行数（仅含数据的 height 槽位数）
[rowOffset[0]: int]                   // 第 0 行数据相对于 rowCount 后的偏移
...
[rowOffset[rowCount-1]: int]
[Row[0]]:
    [height: byte]
    [lowCount: int]
    [low[0] ... low[lowCount-1]: byte]
[Row[1]]: ...
```

---

## 5. 运行时读取流程（SampleAtIndex）

```
SampleAtIndex(index, pos, ...)
        │
        ├─ octreeAreaExist = false（非 Octree 模式）
        │       │
        │       ├─ PVSBakeDataUtils.DeserializeRead(rawDataIdxArray, index)
        │       │       → (chunkIdx, rawIdx)
        │       │
        │       └─ SampleAtIndexByVer5(chunkIdx, rawIdx, ...)
        │
        └─ octreeAreaExist = true（Octree 模式）
                │
                ├─ PVSOctreeUtils.DeserializeRead(rawDataIdxArray, pos, ...)
                │       → (chunkIdx, rawIdx, samplePos, leafNodeIdx)
                │
                └─ SampleAtIndexByVer5(chunkIdx, rawIdx, ...)

SampleAtIndexByVer5(chunkIdx, rawIdx, ...):
    exBinData[chunkIdx].DeserializeRead_ByVer4(rawIdx, ...)

DeserializeRead_ByVer4(realIdx, indices, ...):
    ├─ 读 rawDataOffset    → GetIndicesByRawData(rawDataOffset, indices)
    ├─ 读 shareDataOffset  → GetIndicesByRawData(shareDataOffset, indices)
    └─ 读 samplePosOffsetMask（若已保存）

GetIndicesByRawData(offset, indices):
    ├─ 读 rowCount
    ├─ for each row:
    │       ├─ 读 height (byte)
    │       ├─ 读 lowCount (int)
    │       └─ for each low: indices.Add(UnflattenByte2UShort(height, low))
```

最终 `indices` = shareData 索引 + perCell 差量索引（合并结果即完整可见集）。

---

## 6. 关键设计决策

| 决策 | 原因 |
|------|------|
| **Chunk 文件分割** | 支持流式加载（Stream Mode），按需加载相机附近的 Chunk，节省内存 |
| **Splat 块分割主索引** | 单个 Splat 全为空时可用空数组表示，减少主索引文件体积 |
| **Group 共享数据** | 空间相邻采样点可见集高度相似，共享公共部分可大幅降低差量存储量 |
| **高低字节压缩** | 渲染器索引通常聚集在某些范围，同高字节的索引可紧密打包 |
| **saveBigVisIndex** | 采样点数超过 65535 时自动切换，避免 rawIdx 溢出 |
| **NativeArray 模式** | 运行时频繁读取时避免托管数组的边界检查开销 |
| **PreferBinarySerialization** | ScriptableObject 使用二进制序列化，减少 `rawDataIdxArray` 的磁盘体积 |

---

## 7. 相关类索引

| 类 | 位置 | 职责 |
|----|------|------|
| `PVSBakeData` | `com.spacetime.pvs` | 抽象基类，定义数据接口 |
| `PVSVolumeBakeData` | `BakeData/` | 具体实现，持有所有序列化字段 |
| `PVSCommonBakeData` | `BakeData/` | 手动分割点与偏移掩码配置 |
| `PVSBakeDataSerialize` | `BakeData/` | 运行时 Chunk 字节读取器 |
| `PVSCellInfo` | `BakeData/` | Chunk 空间元数据 |
| `PVSCompressRawDataChunkMgr` | 单例 | 管理所有 Chunk 的构建与查询 |
| `PVSCompressRawDataChunk` | `BakeData/` | 单个 Chunk，含 Group 字典与扁平 rawDataList |
| `PVSCompressRawDataGroup` | `BakeData/` | 同一空间小组，计算共享数据与差量 |
| `PVSCompressRawData` | `BakeData/` | 单个采样点的压缩数据（差量 + compressData） |
| `PVSCompressShareRawData` | `BakeData/` | 同组公共可见集的压缩数据 |
| `PVSBakeDataUtils` | `Utils/` | 序列化/反序列化、文件路径、压缩算法 |
| `PVSCompressRawDataUtils` | `Utils/` | Chunk 序列化写入、公共数据计算 |

---

[← 返回索引](DETAILS.md) | [← 返回 README](../README.md)
