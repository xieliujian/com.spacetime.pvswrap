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

[← 返回索引](DETAILS.md) | [← 返回 README](../README.md)
