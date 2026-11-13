# 小作业 3：Quest 2 曲面射线计数器

## 当前状态（重构后）

- 2026-11-06 重构了 `Assets/VRHomework/Runtime` 和 EditMode 测试。**重构后的代码还没有在 Unity 里编译、运行，也没有重新构建 APK。**
- `Builds/QuestRayCounter.apk` 是**重构之前**构建的，不对应当前源码。重构后需要重新构建。
- 重构前的桌面鼠标预览、EditMode 测试 7/7 和 APK 构建结果见 `archive/pre-refactor-snapshot-2026-11-06`，只能说明旧代码的状态。
- Quest 2 真机运行、手柄交互和录屏都**尚未进行**。

## 版本

- Unity **2022.3.62f3**（无 c1 后缀）。作业讲义的测试版本是 2021.3.22f1c1，并要求组内版本精确一致，请和组员确认使用同一个。
- Oculus XR Plugin 4.2.0，XR Plugin Management 4.4.1，Built-in Render Pipeline。
- Android：Oculus Provider，IL2CPP，ARM64，OpenGLES3，最低 API 29。

## 打开与运行

1. Unity Hub 里选择“添加已有项目”，选中本文件夹，用 2022.3.62f3 打开。
2. 菜单 **VR Homework → 2 - Open demo scene**，点击 Play。场景里只有一个 `DemoBootstrap`，600 个按键在运行时生成，所以编辑状态下看不到。
3. 桌面预览：鼠标移动瞄准，左键切换，按住右键拖动转视角，WASD 移动，R 清空。先点一下 Game 窗口让它获得焦点。
4. Quest 2：右手柄指向按键，扣一下扳机切换一次，松开后才能再次触发。

## 代码结构（`Assets/VRHomework/Runtime`）

| 文件 | 作用 |
|---|---|
| `DemoBootstrap` | 场景入口，创建相机、面板、计数板、复位键、射线指针 |
| `CurvedButtonPanel` | 在圆柱面上生成 30×20 个按键，统计按下数量 |
| `RayButton` | 单个按键：记住松开/按下，按下时变红并后移 0.025 米 |
| `CounterBoard` | 按键旁边的计数板：`按下数 / 600` 和最近操作 |
| `RayPointer` | 每帧发射射线，扳机刚按下的那一帧触发命中的目标 |
| `TriggerEdge` | 把每帧扳机状态变成"刚按下"事件，处理按住、失焦、输入源切换（纯逻辑，有单元测试） |
| `XRTrackedRig` | 读取头显和右手柄位姿、扳机、震动；桌面预览相机 |
| `RayTarget`、`ResetPanelTarget` | 可被射线触发的目标基类，以及 RESET ALL |
| `PanelMaterials`、`ScenePrimitives` | 材质和创建立方体、文字的小工具 |

`Editor/QuestProjectTools.cs` 负责 XR 与 Android 的工程配置和构建菜单。`Tests/EditMode` 下有两组 EditMode 测试：`PanelInteractionTests`（面板生成、曲面、切换、计数、遮挡，7 项）和 `TriggerEdgeTests`（扳机边沿逻辑，5 项）。

## 设计要点

- **曲面。** 第 c 列的角度为 `(c - 14.5) × (键宽 + 缝隙) / 半径`，位置为 `(半径·sin, 高度, 半径·cos)`，并绕 Y 轴转同样的角度，使每个按键的 +Z 指向圆柱外侧。
- **凹陷。** 碰撞体固定不动，只有键帽沿自身 +Z 后移，所以凹陷后射线仍能命中同一个按键，可以再次点击松开。
- **触发。** 取扳机“上一帧没按、这一帧按下”的瞬间，按住不放只触发一次。追踪丢失时视为仍按着，恢复追踪时不会误触。

## 验证与构建

在本文件夹打开 PowerShell，把路径换成本机的 Unity.exe：

```powershell
# Unity EditMode 测试（也可在 Window → General → Test Runner 里运行）
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-Unity.ps1 -UnityEditor "D:\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe"

# 构建 APK，输出到 Builds\QuestRayCounter.apk
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Build-Quest.ps1 -UnityEditor "D:\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe"
```

工程路径含中文，构建脚本会先复制到 `D:\VRHomework3\build-时间\` 这个英文路径再构建。**构建会覆盖 `Builds\QuestRayCounter.apk`**，如需保留重构前的 APK，请先复制一份（重构前的版本也在 `archive` 快照里）。

## 仍需你完成

1. 在 Unity 里打开工程，确认 Console 没有编译错误，运行 EditMode 测试。
2. 桌面预览检查：点击按键变红并凹陷，计数增减，RESET ALL 和 R 键清空。
3. 重新构建 APK，装到 Quest 2，用右手柄实测指向、扳机、遮挡和帧率。
4. 实际回看后再录屏提交。提交内容至少包含 `Assets`、`Packages`、`ProjectSettings`（含 `.meta`）和录屏。

作业截止时间：2026 年 11 月 18 日 23:59。
