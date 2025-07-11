<h1 align="center">AGV Tunnel Simulation · Unity Edition</h1>
<p align="center">
  Unity 2025 LTS | Blender Assets | HTTP Camera Streaming
</p>

> 这是智能巡线车云平台的 **三维仿真仓库**：  
> - **场景** 由 Blender 模型快速搭建  
> - **缺陷图像** 来自公开数据集 / 网络素材  
> - **图传接口** 与后端 Python CV 微服务完全一致（POST `/stream`）  
> 目前仅提供 **Linux x86-64 可执行包**；Windows 请自行编译 Unity Player。

---

## 1. 快速开始

| 方式 | 步骤 |
| ---- | ---- |
| **Linux 发行版** | 下载 `1.0.0.zip`解压 → `chmod +x` → 直接运行 |
| **本地编译** | Unity Hub ➜ *Open Project* ➜ 选择仓库 ➜ `File ▸ Build Settings…` ➜ 目标平台 ➜ **Build** |

> **Unity 版本**：建议 6000.1.9f LTS；高低版本如遇 API 变动请自行升级脚本。

---

## 2. 目录结构

```text
agv-simulation
├─ Assets/
│ ├─ Scenes/ 主场景 Tunnel.unity
│ ├─ turnul/ Blender 导出 FBX
│ ├─ Scripts/ 摄像-串流 & 巡逻控制
│ └─ Scripts/Textures 缺陷贴图
├─ ProjectSettings/ Unity 配置
└─ Build/ 预编译二进制（Linux）
```


---

## 3. 图传接口

> Unity ↔ 后端 Python 微服务（`0.0.0.0:8000/stream`）HTTP POST

- **Content-Type**：`application/json`
- **发送频率**：默认 `~33 FPS`（`sendInterval = 0.03 s`）
- **单帧分辨率**：`1440 × 1080`，JPEG 70% 质量
- **JSON Schema**

```jsonc
{
  "timestamp": 1720701234567,          // Unix 毫秒
  "position": { "x": -4.3, "y": 1.2, "z": 15.8 },
  "rotation": { "x": 0.0, "y": 90.0, "z": 0.0 },
  "image_base64": "...."               // JPEG → Base64
}
```

**后端期待：** HTTP 200 视作成功；建议分批 ACK/丢弃策略在服务端处理。

## 4. 关键脚本

| 脚本 | 功能 | 主要字段 |
| ---- | ---- | -------- |
| **CameraStreamer.cs** | 离屏渲染 → JPG → Base64 → HTTP POST | `sendInterval`、`serverUrl` |
| **PatrolController.cs** | 车体巡逻 & 手动驾驶切换 | `P` 切换模式，`startPoint / endPoint` 路点 |

### 4.1 CameraStreamer 速览
- 创建隐藏的 **`CaptureCamera`**，复制主相机参数，渲染至 `RenderTexture`  
- `LateUpdate` 同步主相机位姿到拍摄相机  
- **`CaptureLoop()`**  
  1. `Render()` 当前帧  
  2. `EncodeToJPG(70)` → Base64  
  3. 入 `ConcurrentQueue`  
- **`SendLoopAsync()`** 消费队列，`HttpClient.PostAsync()` 异步推流

### 4.2 PatrolController 控制键
| 模式 | 操作 | 键位 |
| ---- | ---- | ---- |
| 手动 | 前后 / 左右移动 | `W / S` · `A / D` |
|      | 左旋 / 右旋 | `Q` · `E` |
| 自动 | 启停自动巡逻 | `P` 切换；在起点复位并往返行驶 |

---

## 5. 配置指南
| 参数 | 位置 | 默认 | 说明 |
| ---- | ---- | ---- | ---- |
| `sendInterval` | `CameraStreamer` | `0.03` | 采集周期（秒）≈ 33 FPS |
| `serverUrl` | `CameraStreamer` | `http://localhost:8000/stream` | 后端推流入口 |
| `moveSpeed` / `rotateSpeed` | `PatrolController` | `2 m/s` / `180 °/s` | 手动 & 自动共用 |
| `startPoint` / `endPoint` | 场景空物体 | — | 自动巡逻路径端点 |

---

## 6. 依赖与兼容
- **Linux**：已在 Ubuntu 24.04 + NV 驱动环境验证
- **Windows**：需安装 Unity 对应平台模块，自行 Build
- **后端**：兼容 `agv-backend`（Spring Boot）+ `cv_microservice`（Python）
- **网络**：本机调试默认 `localhost`；Docker / 远程部署请修改 `serverUrl`

---

## 7. TODO / Roadmap
- HDRP + RTX，提升隧道材质真实感  
- WebRTC 替代 HTTP POST，降低图传延时  
- 随机缺陷生成器，模拟多级缺陷 & 位置  
- Windows / macOS 预编译包发布  

---

## 8. License
Apache 2.0 — 模型与贴图如含第三方素材，请遵循各自授权。
