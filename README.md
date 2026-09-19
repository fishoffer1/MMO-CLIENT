# MMO-CLIENT

一个使用 **Unity 2022.3 / C#** 构建的多人在线 RPG（MMO）**客户端**，覆盖网络接入、登录与角色管理、地图实体同步、战斗表现、动画状态机与 UGUI 界面。

配套服务端：[MMO-SERVER](https://github.com/fishoffer1/MMO-SERVER)（.NET 8 / C#）

> 目标：跑通「登录 → 选择角色 → 进入地图 → 移动同步 → 打怪掉落 → 聊天」的完整表现闭环。

---

## 技术栈

| 分类 | 技术 |
| --- | --- |
| 引擎 / 语言 | Unity 2022.3.62f3c1 · C# |
| 界面 | UGUI（Login / RoleList / 战斗 HUD / 聊天） |
| 动画 | Animancer（动画状态机与事件回调） |
| 补间 / 反馈 | DOTween（相机受击抖动） |
| 网络 | Summer（自研 TCP 长连接框架的客户端侧：`Connection` / `SocketReceiver` / `MessageRouter`） |
| 通信协议 | Google Protobuf |
| 配置序列化 | Newtonsoft.Json |
| 日志 | Serilog |
| 事件系统 | Kaiyun.Event（引入的第三方事件总线） |

---

## ⚠️ 运行前必须准备

**受授权限制，本项目使用的商业 / 第三方插件均未入库**，`.gitignore` 已将其排除。直接 Clone 本项目**无法编译**，需自行导入以下依赖：

| 依赖 | 说明 | 被哪些代码使用 |
| --- | --- | --- |
| **Animancer** | Unity Asset Store 商业插件 | `Animator/HeroAnimations.cs`、`Scripts/BisicCharacter.cs` |
| **DOTween (Demigiant)** | 免费插件，需官网下载 | `Scripts/UIManager.cs`（受击时相机抖动） |
| **Newtonsoft.Json** | JSON 序列化 | `Mgr/DataManager.cs` |
| **protobuf** | Google Protobuf 运行库 | `Plugins/Summer/Proto/*` 等 3 个文件 |
| **Serilog** | 日志库（`Serilog.dll`） | 17 个文件 |

导入后，将上表插件放入 `Assets/Assets/Plugins/` 对应目录下即可。

> 同理，**美术资源（模型 / 图集 / 特效 / 动画剪辑）也未入库**，场景中的预制体引用会显示为缺失。本文档描述的是**代码结构**，网络与逻辑部分不依赖美术资源。

---

## 场景与运行流程

代码中的场景跳转顺序：

```
LoginScene  ──登录成功──▶  RoleList  ──选择角色──▶  地图场景（由配置决定）
  NetStart.cs:72           LoginScript.cs:63        GameApp.cs:36
```

进入地图时，`GameApp` 读取 `SpaceDefine.json` 中的 `Resource` 字段来加载对应场景，做到**换地图不改代码**：

| 场景 | 用途 |
| --- | --- |
| `Scenes/LoginScene.unity` | 登录 / 注册 |
| `Scenes/RoleList.unity` | 角色列表、创建与删除 |
| `Scenes/Scene1.unity` / `Scene2.unity` | 游戏地图 |
| `Scenes/World.unity` | 世界场景 |
| `Scenes/ChatScene.unity` | 聊天 |
| `GUI_Parts/Demo.unity` | UI 组件演示 |

---

## 目录结构

```
Assets/
└── Assets/
    ├── Animator/            # 动画状态机（HeroAnimations）
    ├── GUI_Parts/           # 战斗 HUD（技能栏、冷却）
    ├── Plugins/
    │   └── Summer/          # 自研网络框架（客户端侧）
    │       ├── Core/        # DataStream 二进制编解码
    │       ├── Network/     # Connection / SocketReceiver / MessageRouter
    │       └── Proto/       # Protobuf 生成的消息类
    ├── Resources/
    │   ├── Data/            # JSON 数值配置
    │   └── Define/          # 配置结构定义
    ├── Scenes/              # 游戏场景
    ├── Scripts/
    │   ├── Battle/          # 技能 / 弹道
    │   ├── Entities/        # Entity / Actor / Character / Monster
    │   ├── Mgr/             # DataManager / EntityManager / GameObjectManager
    │   ├── UI/              # LoginScript / RoleListController / UnitFrame
    │   └── u3d_scripts/     # 事件系统、程序入口
    └── SimpleChatBox/       # 聊天组件
```

---

## 核心模块

### 网络接入（`Scripts/NetClient.cs` · `NetStart.cs`）

- `NetClient.ConnectToServer()`：建立 TCP 连接，`MessageRouter.Instance.Start()` 启动消息分发线程
- `NetStart`：用 `MessageRouter.Subscribe<T>()` 按消息类型注册处理器，业务层不直接接触 Socket
- 心跳保活由 `SendHeartMessage()` 周期发送；主线程事件队列由 `Kaiyun.Event.Tick()` 驱动

### 实体同步与插值（`Scripts/GameEntity.cs`）

服务端下发的坐标是 **int 定点数（×1000）**，客户端还原为 `Vector3`：

- **位置**用 `Vector3.Lerp`、**朝向**用 `Quaternion.Lerp`，两者插值速率不同，避免转向与位移互相拖累
- 上报位置做了节流：仅在 `transform.hasChanged` 为真时进入协程，最多每 `0.1s` 发一次同步请求
- 本地重力模拟与服务端同步并存，落到地面时不再向下累积
- 头顶名字用 `OnGUI` 绘制，并按视锥 + 距离剔除

### 动画状态机（`Animator/HeroAnimations.cs`）

- 用 `AnimancerState.Events.OnEnd` 回调驱动「播完接下一个」，而非在 `Update` 里轮询
- 维护**动画优先级**：攻击 / 受击等高优先级动画播放期间，低优先级的移动、待机请求被拦截，避免动作被打断
- 服务端广播的技能释放会触发对应的动画事件

### 战斗表现（`Scripts/Battle/`）

- `Skill.cs`：客户端侧的技能状态机（吟唱 / 释放 / 冷却），与服务端状态机对应
- `Missile.cs`：**纯表现层**——按 `Skill.Def.MissileSpeed * 0.001`（同样走 ×1000 定点约定）逐帧朝目标推进，特效预制体由配置字段 `Skill.Def.Missile` 决定，命中后自毁。**不做任何伤害计算**，伤害由服务端结算后广播回来
- `GUI_Parts/AbilityBar.cs` · `AbilityGroup.cs`：技能图标与冷却读秒

### 配置驱动（`Scripts/Mgr/DataManager.cs`）

`DataManager.Init()` 在启动时从 `Resources/Data/` 加载 JSON，客户端与服务端**共用同一套配置结构**，保证双端数值一致：

| 文件 | 说明 |
| --- | --- |
| `SpaceDefine.json` | 地图定义（含场景资源名） |
| `UnitDefine.json` | 单位（角色 / 怪物）定义 |
| `SkillDefine.json` | 技能定义 |

解析时注册了自定义 `JsonConverter`（`FloatArrayConverter` / `IntArrayConverter`），支持把 `"[1,2,3]"` 这种字符串形式的数组直接反序列化为 `float[]` / `int[]`。

---

## 通信协议

全部消息使用 Protobuf 定义，与服务端共用（完整列表见 `Plugins/Summer/Proto/Message.cs`）：

| 协议 | 说明 |
| --- | --- |
| `HeartBeatRequest` / `HeartBeatResponse` | 心跳保活 |
| `NetActor` / `NetEntity` | 角色与实体数据 |
| `PropertyUpdate` / `PropertyUpdateResponse` | 属性增量更新 |
| `SpaceEntitySyncRequest` / `SpaceEntitySyncResponse` | 地图实体位置同步 |
| `Damage` / `DamageResponse` | 伤害结算与广播 |
| `SpellRequest` | 技能施放 |
| `ChatRequest` / `ChatResponse` | 聊天 |

---

## 说明

- **第三方资源不在本仓库范围内**：商业插件、美术资源、音频均未入库，请自行准备运行环境（见上文「运行前必须准备」）。
- **非本人编写的代码**，特此声明：
  - `Scripts/UnityMainThreadDispatcher.cs` — 开源库 *UnityMainThreadDispatcher*（作者 Pim de Witte，Apache-2.0 协议）
  - `Scripts/u3d_scripts/Event.cs` — 引入的第三方事件总线 `Kaiyun.Event`
- 本仓库仅包含本人编写的逻辑代码、场景与配置。
