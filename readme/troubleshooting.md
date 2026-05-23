[← 返回索引](DETAILS.md) | [← 返回 README](../README.md)

# com.spacetime.pvswrap — 疑难杂症

---

## 1. PVSBakerUnity.SamplePosition：CPU 读取像素 vs GPU 读取像素

### 背景

`PVSBakerUnity.SamplePosition` 在每个采样点渲染完成后，需要从 RenderTexture 中提取可见渲染器的颜色索引。  
通过 `PVSSettings.Instance.useUnityForRenderingCpuCompute` 可在两种实现之间切换：

| 开关值 | 实现路径 | 句柄类型 |
|--------|----------|----------|
| `true` | CPU 像素遍历 | `PVSBakerUnityCpuHandle` |
| `false`（默认） | Compute Shader GPU 管线 | `PVSBakerUnityHandle` |

---

### CPU 读取像素（`PVSBakerUnityCpuHandle`）

**执行流程：**

```
RenderTexture（GPU 显存）
      │
      │ Texture2D.ReadPixels()   ← GPU→CPU 同步回读，产生管线 Stall
      ▼
Color32[] pixels（CPU 内存，大小 = w × h）
      │
      │ 单线程遍历所有像素
      │   index = b*65536 + g*256 + r
      │   hashes[index] == false → 记录、标记
      ▼
ushort[] indices（可见渲染器索引列表）
```

**特点：**

- `Texture2D.ReadPixels` 强制 GPU 完成所有待处理命令并将像素回拷到 CPU，造成 **GPU-CPU 同步气泡**（Pipeline Stall）。
- CPU 需遍历 `w × h` 个像素（例如 6 视图拼合后可达数万像素），去重依赖 16 MB 的 `bool[] hashes` 静态数组（256³ 个颜色槽）。
- 内存占用随纹理尺寸线性增长：`Color32[]` 数组大小 = 4 × w × h 字节。
- 无 Compute Shader 依赖，**全平台兼容**（含不支持 Compute Shader 的目标平台）。
- 烘培时每个采样点都创建并立即销毁一个 `Texture2D`，GC 压力较高。

---

### GPU 读取像素（`PVSBakerUnityHandle`）

**执行流程：**

```
RenderTexture（GPU 显存，w × h）
      │
      │ Compute Shader: CSMain（Dispatch w/16 × h/16 线程组）
      │   每线程读一像素 → 原子写入 256×256 Hash RenderTexture
      ▼
OutputHashRT（256×256，每槽标记对应颜色是否出现）
      │
      │ Compute Shader: CSExtract（Dispatch 256/16 × 256/16 线程组）
      │   遍历 Hash RT → 非零槽追加到 AppendBuffer
      ▼
ComputeBuffer appendBuf（仅含唯一颜色，数量 ≤ MaxRenderers）
      │
      │ ComputeBuffer.CopyCount + GetData()   ← 仅回读极少量数据
      ▼
ushort[] indices（可见渲染器索引列表）
```

**特点：**

- **GPU 并行去重**：`CSMain` 以 16×16 线程组覆盖全图，所有像素同时处理，无单线程遍历瓶颈。
- `CSExtract` 将结果收集到 Append Buffer，CPU 仅需回读最终的精简整数列表（最多 `MaxRenderers` 个），**数据传输量极小**。
- 无临时 `Texture2D` 对象，GC 压力低；`ComputeBuffer` 在 `DoComplete` 后立即 `Dispose`。
- 需要目标平台支持 **Compute Shader**（DX11 / Metal / Vulkan / OpenGL ES 3.1+）。
- Hash RenderTexture（256×256）作为成员常驻显存，跨采样点复用，零额外分配。

---

### 两者对比总结

| 维度 | CPU 读取像素 | GPU 读取像素 |
|------|-------------|-------------|
| **去重位置** | CPU 单线程（`for` 循环） | GPU 并行（Compute Shader） |
| **GPU→CPU 数据量** | 全部像素（w × h × 4 字节） | 仅唯一索引列表（≤ MaxRenderers × 4 字节） |
| **管线 Stall** | 有（`ReadPixels` 强制同步） | 极小（`GetData` 读回少量数据） |
| **GC 分配** | 每采样点创建/销毁 `Texture2D` | 无临时对象（Buffer 复用） |
| **内存峰值** | `Color32[]`：4 × w × h 字节 | 固定：MaxRenderers × 4 字节 |
| **平台兼容性** | 全平台（不需要 Compute Shader） | 需要 Compute Shader 支持 |
| **烘培性能** | 较慢（适合调试、低端平台兜底） | 较快（推荐用于正式烘培） |
| **实现复杂度** | 低 | 高（需维护 Compute Shader） |

---

### 选择建议

- **正式烘培流程**：保持默认 `useUnityForRenderingCpuCompute = false`，使用 GPU 模式获得最佳性能。
- **排查颜色解析问题**：切换为 CPU 模式，可在 C# 代码中逐像素断点调试，直接观察 `Color32` 数值。
- **目标平台不支持 Compute Shader**：必须使用 CPU 模式，但应评估大场景下的烘培时长是否可接受。
- **内存受限的烘培机器**：GPU 模式内存占用更低、GC 更友好，优先选用。

---

## 2. PVSBakingBehaviour.CompletePending：CPU 处理 vs GPU 处理

### 背景

`CompletePending` 是烘焙主循环中的批量结算方法，每积累 `baker.BatchCount`（默认 2048）个采样点后被调用一次，负责将所有待处理句柄的结果提交到 `BakeData`：

```csharp
void CompletePending(List<PVSBakeHandle> pending)
{
    for (int k = 0; k < pending.Count; ++k)
    {
        pending[k].Handle.Complete();                              // 触发 DoComplete()
        BakeData.SetRawData(pending[k].Index, pending[k].Handle.indices);
    }
    pending.Clear();
}
```

`Handle.Complete()` 调用对应句柄的 `DoComplete()`，CPU 与 GPU 两种模式的行为完全不同。

---

### CPU 模式（`PVSBakerUnityCpuHandle.DoComplete()`）

**GPU 同步时机：在 `SamplePosition` 内部已完成。**

```
SamplePosition()                            CompletePending()
──────────────────────────────────          ──────────────────────────────────
渲染 → ReadPixels()【GPU stall 在此】       DoComplete()
→ Color32[] 已在 CPU 内存                       │
→ 返回 CpuHandle（Pixels 已就绪）           遍历 Color32[] pixels（纯 CPU）
                                                 │ index = b*65536 + g*256 + r
                                                 │ hashes[index] → tmpIndices
                                                 ▼
                                            indices[] ← 填充完毕
                                            BakeData.SetRawData()
```

**特点：**

- `CompletePending` 执行期间 **无任何 GPU 交互**，全程为 CPU 遍历像素的纯计算。
- GPU 同步气泡（Pipeline Stall）分散在每次 `SamplePosition` 调用时，每个采样点都单独触发一次 `ReadPixels`。
- 批次大小（`BatchCount = 2048`）对 GPU 利用率无帮助：每次 `SamplePosition` 调用已各自完成了 GPU→CPU 回读，批次仅减少循环调用开销。
- 峰值内存 = `BatchCount × w × h × 4` 字节的 `Color32[]` 数组同时存在于 CPU 内存（GC 在 `pending.Clear()` 后才释放）。

---

### GPU 模式（`PVSBakerUnityHandle.DoComplete()`）

**GPU 同步时机：在 `CompletePending` 内的 `GetData()` 处发生。**

```
SamplePosition()（每次调用）               CompletePending()（每 BatchCount 次后调用一次）
──────────────────────────────────          ──────────────────────────────────
渲染 → CSMain Dispatch【异步入队】          DoComplete()（对每个 pending handle）
→ CSExtract Dispatch【异步入队】                │
→ 返回 UnityHandle                              │ CopyCount(appendBuf → countBuf)
  （appendBuf/countBuf 仍在 GPU）              │ countBuf.GetData()【GPU stall 在此】
                                                │ appendBuf.GetData(count 个 int)
↑ 重复 BatchCount 次，GPU 持续工作 ↑           │ 解码 int → ushort indices[]
                                                │ appendBuf.Dispose()
                                                ▼
                                            indices[] ← 填充完毕
                                            BakeData.SetRawData()
```

**特点：**

- `SamplePosition` 每次调用只提交 GPU 命令（Render + 2 Dispatch），**不等待 GPU 完成**，立即返回。
- `CompletePending` 中 `GetData()` 是第一个等待点，此时 GPU 已积压了整批（最多 2048 帧）的 Compute Shader 工作。GPU 在批次期间持续并行执行，CPU 同步开销被分摊到 BatchCount 个采样点上。
- 每次 `GetData()` 只回读极少量数据（`count × 4` 字节，`count ≤ MaxRenderers`），带宽消耗极低。
- `appendBuf.Dispose()` 在 `DoComplete()` 结束时释放，ComputeBuffer 生命周期严格限定在一个批次内。

---

### 两者对比总结

| 维度 | CPU 模式（CpuHandle） | GPU 模式（UnityHandle） |
|------|-----------------------|------------------------|
| **GPU stall 位置** | `SamplePosition` 内（每采样点一次） | `CompletePending` 内（每批次一次） |
| **CompletePending 工作** | 纯 CPU 像素遍历，无 GPU 交互 | `GetData()` 触发 GPU 同步 + 回读精简数据 |
| **BatchCount 对 GPU 的意义** | 无加速（stall 已分散） | 关键：GPU 持续并行，同步开销摊薄到整批 |
| **批次峰值内存（CPU 侧）** | `BatchCount × w × h × 4` 字节 | `BatchCount × MaxRenderers × 4` 字节 |
| **Stall 次数（共 N 个采样点）** | N 次（每采样点一次） | `⌈N / BatchCount⌉` 次（每批次一次） |
| **GPU 并行度** | 低（每帧独立 stall） | 高（批次内命令流水线执行） |
| **ComputeBuffer 生命周期** | 不涉及 | 一个批次内创建、使用、释放 |

### BatchCount 的核心作用（GPU 模式）

```
BatchCount = 2048 时：

SamplePos[0] Dispatch → SamplePos[1] Dispatch → ... → SamplePos[2047] Dispatch
└────────────────── GPU 持续运行 2048 帧 ──────────────────┘
                                                             │
                                             CompletePending（一次 GetData stall）
                                                             │
                                             读回 2048 个精简结果
```

GPU 模式下，`BatchCount` 越大，GPU 管线越饱满，单位采样点的同步开销越低。但也意味着更多 `ComputeBuffer` 同时存活，显存占用随之增加。

---

## 3. PVSBakerUnityCpuHandle 与 PVSBakerUnityHandle 深度分析

两个类均继承自 `PVSBakerHandle`，是 `SamplePosition` 返回的具体句柄，核心差异在于**颜色去重在哪里完成、由谁完成**。

---

### 3.1 PVSBakerUnityCpuHandle — CPU 像素遍历句柄

#### 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `Pixels` | `Color32[]` | 从 RenderTexture 读回的完整像素数组（由 `SamplePosition` 填充） |
| `m_Hash` | `int[]` | 颜色索引 → 分组索引的映射表（来自 `PVSSceneColor.Hash`，共享引用） |
| `hashes`（static） | `bool[16,777,216]` | 颜色命中标记数组，256³ = 16 MB，进程生命周期内常驻 |
| `tmpIndices`（static） | `List<ushort>` | 可复用的临时结果列表，避免每帧分配 |

#### DoComplete() 执行流程

```
Array.Clear(hashes, 0, 16_777_216)      ← 清零 16 MB bool[]（每批次调用一次）
tmpIndices.Clear()

for indexPixel in [0, w×h):
    pixel = Pixels[indexPixel]           ← 读 Color32（r, g, b, a）
    index = b*65536 + g*256 + r          ← 将 RGB 打包为 24 位整数（颜色唯一键）

    if index <= 0 || hashes[index]:      ← 跳过背景色（0）和已处理颜色
        continue

    hashes[index] = true                 ← 标记已命中
    tmpIndices.Add(m_Hash[index])        ← 颜色键 → 分组索引

tmpIndices.Sort()                        ← 排序（便于后续二分查找）
indices = new ushort[tmpIndices.Count]
tmpIndices.CopyTo(indices)
```

#### 关键特性

- **时间复杂度**：O(w×h) 遍历 + O(k log k) 排序（k = 唯一可见渲染器数）。
- **16 MB 静态数组**：`hashes` 在类首次加载时分配，永不释放。每次 `DoComplete` 必须整体清零（`Array.Clear` 对 16M 元素约耗 1～3 ms），是 CPU 模式的固定开销。
- **零额外堆分配**：`hashes` 和 `tmpIndices` 均为静态复用，不触发 GC（除最终 `new ushort[]` 赋值给 `indices`）。
- **线程安全**：static 字段为主线程专用，烘焙流程在 Editor 主线程执行，无并发问题。
- **颜色编码**：背景色固定为 `index = 0`（即 RGB 全零），由 `if index <= 0` 过滤。

---

### 3.2 PVSBakerUnityHandle — GPU Compute Shader 句柄

#### 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `appendBuf` | `ComputeBuffer` (Append) | GPU 端收集唯一颜色打包值的 Append Buffer |
| `countBuf` | `ComputeBuffer` (IndirectArguments) | 读取 Append Buffer 计数用的间接参数 Buffer |
| `m_Hash` | `int[]` | 颜色索引 → 分组索引的映射表（同 CpuHandle） |
| `m_Out`（static） | `int[MaxRenderers]` | 复用的整数输出数组，避免每帧分配 |
| `m_CounterOutput`（static） | `int[1]` | 复用的计数读取数组 |

#### DoComplete() 执行流程

```
ComputeBuffer.CopyCount(appendBuf, countBuf, 0)
    ← 将 appendBuf 的原子计数器复制到 countBuf（纯 GPU 命令）

countBuf.GetData(m_CounterOutput)
    ← 【GPU stall #1】等待 GPU 完成所有待处理命令，读回 1 个 int（count）

if count > 0:
    appendBuf.GetData(m_Out, 0, 0, count)
        ← 【GPU stall #2】读回 count 个 int（打包颜色值）

    for i in [0, count):
        q = m_Out[i]                     ← 解包打包整数
        b = q / 65536
        q -= b * 65536
        g = q / 256
        r = q % 256
        index = b*65536 + g*256 + r      ← 还原颜色键
        indices[i] = m_Hash[index]       ← 颜色键 → 分组索引

    Array.Sort(indices)

appendBuf.Dispose()                      ← 立即释放 ComputeBuffer
countBuf.Dispose()
appendBuf = null
countBuf = null
```

#### 关键特性

- **两次 GPU stall**：`countBuf.GetData`（读 1 int）+ `appendBuf.GetData`（读 count 个 int）。stall 不可合并，因为需要先知道 count 才能确定读取范围。
- **数据量极小**：回读数据 = `count × 4` 字节（`count ≤ MaxRenderers`），与纹理分辨率无关。
- **ComputeBuffer 生命周期**：在 `SamplePosition` 中创建，在 `DoComplete` 末尾 `Dispose`，生命周期严格限定在一个批次内。批次越大（BatchCount 越高），GPU 端积压的 Append Buffer 越多（同时存活于显存中）。
- **整数解包**：GPU 端存储的是打包 RGB 整数（与 CPU 模式编码相同），CPU 端解包还原后查 `m_Hash`，逻辑与 CPU 模式等价，只是去重在 GPU 完成。
- **无静态大数组**：不需要 16 MB `bool[]`，也不需要清零开销。

---

### 3.3 两者实现对比

| 维度 | `PVSBakerUnityCpuHandle` | `PVSBakerUnityHandle` |
|------|--------------------------|-----------------------|
| **去重执行者** | CPU（`bool[] hashes` 标记） | GPU（Compute Shader Append Buffer） |
| **输入数据大小** | `w × h` 个 `Color32`（全量像素） | `count` 个 `int`（仅唯一颜色，≤ MaxRenderers） |
| **静态内存** | 16 MB `bool[]` 常驻 + `List<ushort>` | `int[MaxRenderers]` + `int[1]`（字节级） |
| **GPU stall 次数** | 0（stall 在 SamplePosition 已发生） | 2（CopyCount + GetData × 2） |
| **固定 CPU 开销** | `Array.Clear` 16 MB（每次 DoComplete） | 解包循环（仅 count 次，通常 < 千次） |
| **ComputeBuffer** | 不涉及 | 创建于 SamplePosition，Dispose 于 DoComplete |
| **颜色编码公式** | `b*65536 + g*256 + r`（CPU 端解码） | 同上（GPU 存储，CPU 端解码） |
| **排序对象** | `List<ushort> tmpIndices`（静态复用） | `ushort[] indices`（直接 Array.Sort） |
| **适用场景** | 调试、无 Compute Shader 平台 | 正式烘焙（高性能，低数据传输） |

---

[← 返回索引](DETAILS.md) | [← 返回 README](../README.md)
