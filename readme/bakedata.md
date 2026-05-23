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

## 8. 优势与劣势分析

### 8.1 优势

#### 空间分块（Chunk）支持流式加载

每个 Chunk 独立存储为一个 `.bytes` 文件，运行时可按相机位置按需加载，无需将整个场景可见性数据全部驻留内存。对于大型开放世界场景，未进入的区域的 Chunk 完全不占用内存。`PVSCellInfo.bounds` 提供了空间判断的依据，加载决策无需解析数据内容。

#### Group 共享数据利用空间局部性

空间相邻的采样点往往共享大量相同的可见渲染器（例如同一楼层的多个采样点都能看到同一批建筑）。将交集提取为 `shareRawData`，每个采样点只存差量，在采样点密集且场景结构化的情况下压缩率显著。差量越小，文件体积越小，IO 越快。

#### 高低字节编码（VisibilitySet2）稀疏存储

渲染器索引通常连续分配，高字节（`height = index >> 8`）相同的索引可以共用一行头。只存有数据的行（稀疏），跳过空行，对于可见渲染器数量少的采样点尤其节省空间。相比直接存 `ushort[]`，在索引聚集时有明显优势。

#### 两级索引结构，查询路径短

主索引（`rawDataIdxArray`）极小，每个采样点仅占 3～5 字节，整个场景索引可常驻内存。运行时查询只需一次主索引读取定位到 `(chunkIdx, rawIdx)`，再一次 Chunk 内偏移表读取定位到数据块，路径层数固定，无递归。

#### saveBigVisIndex 自动扩容

采样点数量超过 65535 时自动将 rawIdx 从 `ushort`（2 字节）切换为 `uint`（4 字节），无需修改其他代码，兼容中小型和超大型场景。

#### NativeArray 模式减少运行时 GC

`PVSBakeDataSerialize` 支持 `useNative=true`，通过 `UnsafeUtility.ReadArrayElement` 直接指针读取，绕过托管数组的边界检查，高频帧查询时更友好，且 NativeArray 不触发 GC。

#### PreferBinarySerialization 降低 ScriptableObject 体积

`PVSVolumeBakeData` 标注 `[PreferBinarySerialization]`，Unity 以二进制格式而非 YAML 文本序列化，`rawDataIdxArray`（byte[]）、`cellInfoList` 等字段磁盘体积显著小于文本格式。

---

### 8.2 劣势

#### 反序列化路径层级深，调试困难

运行时一次查询的调用链：

```
SampleAtIndex
  → SampleAtIndexByVer6
    → DeserializeRead（主索引）→ (chunkIdx, rawIdx)
      → SampleAtIndexByVer5
        → exBinData[chunkIdx].DeserializeRead_ByVer4(rawIdx)
          → GetIndicesByRawData(rawDataOffset)   // rawData
          → GetIndicesByRawData(shareDataOffset) // shareData
```

五层调用，任何一层的偏移计算错误都会导致静默读错数据（返回错误的可见集而非崩溃）。数据损坏极难定位，必须逐层打印偏移才能排查。

#### 每次查询都重复读取 shareRawData，无缓存

同一 Group 内所有采样点共享同一份 `shareRawData`，但 `DeserializeRead_ByVer4` 每次都从字节流中重新解码 `shareRawData` 并追加到 `indices`，没有任何缓存机制。在同一帧内多个相机或多次查询命中同一 Group 时，`shareRawData` 被重复解码。

#### FindCommonNumbers 在 Group 内采样点多样时效率低

```csharp
// 以第一个点的可见集初始化，逐一与后续点求交集
HashSet<ushort> commonNumbers = new HashSet<ushort>(srcRawData[0]);
for (int i = 1; i < rawDataList.Count; i++)
    commonNumbers.IntersectWith(srcRawData[i]);
```

若 Group 内采样点的可见集差异较大（如高处点与低处点可见集几乎不重叠），交集为空，共享数据为零，但仍完整执行了所有 `IntersectWith`。空交集意味着本次分组压缩完全无效，所有采样点仍需存储全量数据。

#### FindRawData 是线性扫描（烘焙期）

```csharp
for (int i = 0; i < rawDataList.Count; i++) {
    if (rawDataList[i].sampleIdx == _sampleIdx) ...
}
```

烘焙阶段 `PVSCompressRawDataChunkMgr.GetRawData` 用此方法查找，复杂度 O(N)。大场景 Chunk 内采样点数量大时烘焙耗时上涨，可改为 `Dictionary<int, PVSCompressRawData>` 以 O(1) 替代。

#### chunkIdx 在主索引中以 byte 存储，容量上限为 255

```csharp
// chunkIdx 项目有效值不会超过 64，用 byte 存储不会溢出
byte chunkIdxByte = (byte)chunkIdx;
```

这是一个硬编码假设（`mapSize / chunkSize ≤ 255`）。若项目地图尺寸增大或 chunkSize 缩小，超过 255 个 Chunk 时会发生截断错误，且不会有任何运行时警告。

#### PVSCompressRawDataChunkMgr 是全局单例，不可并发

```csharp
static PVSCompressRawDataChunkMgr s_Instance;
```

同一进程内只能有一个烘焙任务在运行，无法对多个 Volume 并行烘焙。若烘焙中断，单例状态残留，下次烘焙必须重新 `Init` 才能清理旧数据，否则可能产生脏数据混入。

#### VisibilitySet2 头部开销对稀少可见集不划算

每个 `VisibilitySet2` 固定写入：

```
rowCount(4字节) + rowCount × rowOffset(4字节/行) + 每行 height(1) + lowCount(4) + lowData
```

若某采样点只有 1～2 个可见渲染器，头部元数据（至少 13 字节）远超有效数据（2～4 字节），不如直接存原始 `ushort[]`。

#### 版本共存，无自动迁移

代码中存在 Ver3 / Ver4 / Ver5 / Ver6 多个读取路径，`bakeDataVersion` 字段记录版本号但不驱动自动迁移。旧格式数据必须重新烘焙才能升级，没有工具支持跨版本数据升级，给长期维护带来负担。

---

### 8.3 优劣总结

| 维度 | 评价 |
|------|------|
| **运行时内存** | ✅ 流式加载，按需占用 |
| **文件体积** | ✅ 多层压缩，通常较小 |
| **查询性能** | ⚠️ 固定层数，但 shareData 无缓存 |
| **烘焙性能** | ⚠️ 大场景 FindRawData 线性扫描，可优化 |
| **可扩展性** | ⚠️ chunkIdx byte 上限 255，大地图存在风险 |
| **并发安全** | ❌ 单例 ChunkMgr，不支持并发烘焙 |
| **调试友好性** | ❌ 多层偏移间接，数据错误难以定位 |
| **版本演进** | ❌ 多版本共存，无自动迁移 |

---

## 9. 优化建议

### 9.1 缓存 shareRawData 解码结果（查询热路径）

**问题**：每次 `DeserializeRead_ByVer4` 都对 shareRawData 重新解码，同一 Group 在同一帧被多次命中时做无意义重复工作。

**建议**：在 `PVSBakeDataSerialize` 中维护一个按 shareRawDataOffset 为键的 `Dictionary<int, ushort[]>` 缓存，首次解码后存入，后续命中直接返回：

```csharp
// PVSBakeDataSerialize 内
Dictionary<int, ushort[]> _shareCache = new Dictionary<int, ushort[]>();

void GetIndicesByRawData_Cached(int offset, RapidList<ushort> indices)
{
    if (!_shareCache.TryGetValue(offset, out ushort[] cached))
    {
        var temp = new RapidList<ushort>();
        GetIndicesByRawData(offset, temp);
        cached = temp.ToArray();
        _shareCache[offset] = cached;
    }
    foreach (var idx in cached)
        indices.Add(idx);
}
```

每帧结束或 Chunk 卸载时清空缓存。该优化对相机静止或缓慢移动的场景效果最明显。

---

### 9.2 将 FindRawData 线性扫描改为字典（烘焙性能）

**问题**：`PVSCompressRawDataChunk.FindRawData` 遍历 `rawDataList` 线性查找，O(N)。

**建议**：在 Chunk 内增加一个 `Dictionary<int, PVSCompressRawData>`，以 `sampleIdx` 为键：

```csharp
// PVSCompressRawDataChunk 内
Dictionary<int, PVSCompressRawData> _rawDataDict = new Dictionary<int, PVSCompressRawData>();

public void AddGroup(int _sampleIdx, ...)
{
    var rawData = rawGroup.Fill(...);
    rawDataList.Add(rawData);
    _rawDataDict[_sampleIdx] = rawData;   // 同步维护
    ...
}

public PVSCompressRawData FindRawData(int _sampleIdx, out int _rawDataIdx)
{
    if (_rawDataDict.TryGetValue(_sampleIdx, out var rawData))
    {
        _rawDataIdx = rawData.saveIdx;
        return rawData;
    }
    _rawDataIdx = int.MaxValue;
    return null;
}
```

大场景每个 Chunk 含数千个采样点时，烘焙阶段的 `GetRawData` 调用从 O(N) 降到 O(1)。

---

### 9.3 将 exBinData 字典改为数组（运行时查询）

**问题**：`exBinData` 是 `Dictionary<int, PVSBakeDataSerialize>`，每次 `SampleAtIndex` 都做一次哈希查找。

**建议**：chunkIdx 的有效范围已知（`mapSize / chunkSize` 最大 255），可直接用数组替代字典：

```csharp
// PVSVolumeBakeData 内
[NonSerialized]
PVSBakeDataSerialize[] _binDataArray; // 长度 = maxChunkCount

void InitBinDataArray()
{
    int maxChunk = (PVSDefine.s_MapSize / PVSDefine.s_ChunkSize);
    _binDataArray = new PVSBakeDataSerialize[maxChunk * maxChunk];
}

public override void FillStreamData(int nIdx, byte[] datas, int pvsSize, bool useNative)
{
    _binDataArray[nIdx] = new PVSBakeDataSerialize(datas, useNative, pvsSize);
    // 保留 exBinData 用于兼容
}
```

数组下标访问 O(1) 且缓存更友好，避免哈希碰撞和字典对象开销。

---

### 9.4 扩展 chunkIdx 存储宽度，消除 255 上限

**问题**：主索引中 chunkIdx 以 `byte` 存储，硬上限 255 个 Chunk，大地图有溢出风险。

**建议**：参照 `saveBigVisIndex` 的做法，增加 `saveBigChunkIndex` 标志位，按需将 chunkIdx 从 1 字节扩展到 2 字节（`ushort`，支持 65535 个 Chunk）：

```
// 主索引 entry 格式（saveBigChunkIndex=true 时）
[chunkIdx: ushort]   // 2 字节，替代原 1 字节
[rawIdx: ushort / uint]
```

同时在 `SerializeWrite` / `DeserializeRead` 中增加对应分支，并在烘焙前检测实际 Chunk 数量自动选择宽度。

---

### 9.5 烘焙时检测无效 Group，跳过空交集分组

**问题**：`FindCommonNumbers` 在 Group 内采样点可见集差异大时交集为空，共享压缩完全无效，但仍完整执行了所有 `IntersectWith`。

**建议**：快速估算后跳过：

```csharp
public void Calc()
{
    // 采样点数为 1 时无需求交集，直接跳过 share 计算
    if (rawDataList.Count <= 1)
    {
        CompressModifyRawData_NoShare();
        return;
    }

    var shareDataList = PVSCompressRawDataUtils.FindCommonNumbers(rawDataList);

    // 交集为空或占比过低时，不使用共享数据
    if (shareDataList.Count < 4)
    {
        CompressModifyRawData_NoShare();
        return;
    }

    CalcModifyRawData(shareDataList);
    CompressModifyRawData();
}
```

阈值（如 `< 4`）可根据实际场景调整。避免在交集极小时引入共享数据头部开销反而增大体积。

---

### 9.6 对稀少可见集采样点使用原始 ushort[] 直接存储

**问题**：`VisibilitySet2` 对可见渲染器数量 ≤ 3 的采样点，头部元数据（rowCount + rowOffset + height + lowCount）开销远大于有效数据本身。

**建议**：在 `CompressRawData` 入口加一个阈值判断，少于 N 个索引时直接写 `ushort[]`，标记一个 flag byte 区分两种格式：

```csharp
// VisibilitySetRow[] 序列化时
if (indices.Length <= 4)
{
    // flag=0: 原始格式
    saveBytes.Add(0);
    saveBytes.Add((byte)indices.Length);
    foreach (var idx in indices) saveBytes.AddItems(BitConverter.GetBytes(idx));
}
else
{
    // flag=1: VisibilitySet2 压缩格式
    saveBytes.Add(1);
    compressData.SerializeWrite(saveBytes);
}
```

对大量"几乎不可见"的远端格子效果显著。

---

### 9.7 引入版本路由，废弃旧版读取路径

**问题**：Ver3/4/5/6 多版本读取路径共存，长期维护成本高，新开发者难以理解哪条路径是当前主路径。

**建议**：
1. 将 `bakeDataVersion` 作为强制校验字段，加载时若版本不匹配直接报错提示重新烘焙，而不是静默走旧版路径。
2. 在 Editor 工具中提供 **"检测过期数据"** 菜单项，扫描场景中所有 `PVSVolumeBakeData`，标记版本落后的资产。
3. 确定最终版本后，将旧版读取代码移入 `#if PVS_LEGACY_SUPPORT` 编译开关，正式包中默认关闭。

---

### 9.8 ChunkMgr 去单例化，支持多 Volume 并发烘焙

**问题**：`PVSCompressRawDataChunkMgr.S` 全局单例，无法同时处理多个 Volume。

**建议**：将 `ChunkMgr` 改为实例化对象，由 `PVSVolumeBakeData.CompleteBake` 在栈上创建并传递：

```csharp
public override void CompleteBake()
{
    var chunkMgr = new PVSCompressRawDataChunkMgr(); // 不再用单例
    chunkMgr.Init(rawData, allSamplePosInfoList, volumeSize);
    ...
    PVSCompressRawDataUtils.SaveBinData(chunkMgr, ...);
}
```

每个 Volume 的烘焙独立持有自己的 `ChunkMgr` 实例，天然线程隔离，支持未来并行烘焙。

---

### 9.9 优化建议优先级总结

| 优先级 | 建议 | 收益 | 改动量 |
|--------|------|------|--------|
| ⭐⭐⭐ | 9.2 FindRawData 改字典 | 烘焙提速（大场景明显） | 小 |
| ⭐⭐⭐ | 9.1 shareRawData 缓存 | 运行时高频查询减少重复解码 | 中 |
| ⭐⭐⭐ | 9.5 跳过空交集分组 | 减少无效烘焙计算和体积膨胀 | 小 |
| ⭐⭐ | 9.3 exBinData 改数组 | 运行时查询缓存更友好 | 小 |
| ⭐⭐ | 9.4 chunkIdx 扩展宽度 | 消除大地图溢出风险 | 中 |
| ⭐⭐ | 9.6 稀少可见集直接存储 | 减少小数据集头部开销 | 中 |
| ⭐ | 9.7 版本路由清理 | 降低维护成本 | 大 |
| ⭐ | 9.8 ChunkMgr 去单例 | 支持并发烘焙 | 大 |

---

[← 返回索引](DETAILS.md) | [← 返回 README](../README.md)
