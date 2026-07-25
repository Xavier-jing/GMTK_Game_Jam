# GMTK Game Jam

这是一个团队 Game Jam 使用的 Unity 2D URP 项目。

## 项目信息

- Unity：`2022.3.62f3c1`
- 渲染管线：Universal Render Pipeline
- 输入：Unity Input System
- 主要场景：`Boot`、`MainMenu`、`GamePlay`、`SandBox`

> 比赛严禁使用人工智能辅助生成或修改的艺术资产。所有图像、模型、字体、动画、音乐、音效和视频，以及 Unity 中的导入、场景和 Prefab 修改，都必须由人类成员完成。

## 文档入口

- [智能体协作与写入限制](./AGENTS.md)
- [美术与音频资产技术规格](./Documents/ART_ASSET_SPEC.md)
- [Unity 内容接入指南](./Documents/UNITY_CONTENT_INTEGRATION_GUIDE.md)
- [资产交付与合规验收清单](./Documents/ASSET_HANDOFF_CHECKLIST.md)

## 首次打开工程

1. 使用 Unity Hub 安装并选择 Unity `2022.3.62f3c1`。
2. 打开仓库根目录并等待导入、C# 编译完成。
3. 先解决 Console 中的编译错误。
4. 执行 `Tools > Jam Template > Sync Build Settings`。
5. 打开 `Assets/_Project/Scenes/Boot.unity` 并进入 Play Mode。
6. 确认加载界面结束后进入 MainMenu，再检查 GamePlay 和 SandBox。

首次导入或新增 C# 文件后产生的 `.meta` 必须由人类检查并提交。不要删除已有 `.meta`，否则场景和 Prefab 引用可能失效。

## 团队分工

- 策划：定义内容用途、触发条件、验收标准和范围优先级。
- 美术/音频：人工创作、导出、记录来源与许可证。
- 程序：维护 C# 框架、提供 Inspector 挂接点和验证步骤。
- 内容接入负责人：在 Unity Editor 中导入资产、设置引用并提交 `.meta`。

## 当前代码框架

- `AppContext`：持久化服务入口。
- `SceneLoader`：Boot、MainMenu、Gameplay、Sandbox 场景切换。
- `UIService`：MainMenu、Hud、Pause、Settings 界面切换。
- `AudioService`：BGM/SFX 播放和 Mixer 音量。
- `InputReader`：键盘与手柄输入。

需要接入 UI、音频或其他人工资产时，请先阅读 [Unity 内容接入指南](./Documents/UNITY_CONTENT_INTEGRATION_GUIDE.md)。
