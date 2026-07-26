# 结局 CG 与致谢视频挂接

## 1. 最终运行流程

`LoopManager.EndRun` 仍是唯一结局入口。`GameplayUIController` 停止玩家控制并
将 `RunEndReason` 交给 `EndingSequencePresenter`：

| RunEndReason | 演出 | 完成后 |
| --- | --- | --- |
| `TruthRevealed` | 全黑播放 `EVT_TRUTH` 5句，再显示 END_01 CG 和2句 | 下一周目 |
| `TurnsExhausted` / `EndingOne` | 显示 END_01 CG 和2句 | 下一周目 |
| `EndingTwo` | 显示 END_02 CG 和5句，再播放致谢视频 | 主菜单 |
| `EndingThree` | 显示 END_03 CG 和6句，再播放致谢视频 | 主菜单 |

台词来自
`Assets/_Project/Resources/Story/ending_sequences.json`。文本使用逐字显示：
鼠标左键、Enter、Space 或手柄 South 第一次补完当前句，下一次进入下一句。
致谢视频不可跳过，播放完成或播放失败后返回主菜单。

## 2. GamePlay 场景挂接

必须由人类在 Unity `2022.3.62f3c1` 中完成以下操作。不要在 Play Mode 中
修改或保存场景。

1. 打开 `Assets/_Project/Scenes/GamePlay.unity`。
2. 删除场景根节点 `UIController` GameObject。该节点包含重复的
   `GameplayUIController`，必须只保留 `Canvas` 上的控制器。
3. 选择 `Canvas/EndingRoot`：
   - 保持初始状态为 Inactive；
   - RectTransform 设为全屏 Stretch，四边偏移均为 `0`；
   - Image 颜色设为黑色不透明 `(0, 0, 0, 1)`；
   - 开启 Raycast Target，阻止演出时点击穿透到游戏。
4. 将 `Canvas/EndingRoot/Image` 重命名为 `CgImage`：
   - RectTransform 保持全屏 Stretch；
   - Image 颜色设为白色 `(1, 1, 1, 1)`；
   - 开启 Preserve Aspect；
   - Raycast Target 可关闭；
   - 不设置默认 Sprite，运行时按结局切换。
5. 在 `EndingRoot` 下创建 `EndingText`，添加 `TextMeshProUGUI`：
   - 锚点位于画面底部安全区，推荐左右各保留画面宽度 `8%`；
   - 推荐高度为画面高度 `20%`，文本水平和垂直居中；
   - 颜色为白色，关闭 Raycast Target；
   - 字体和字号由 UI 成员按当前项目字体规范确认。
6. 在 `EndingRoot` 下创建 `CreditsVideo`：
   - 添加 `RawImage`，RectTransform 全屏 Stretch，关闭 Raycast Target；
   - 添加 `AspectRatioFitter`，Aspect Mode 设为 `Fit In Parent`；
   - 初始状态为 Inactive。黑边由 `EndingRoot` 提供。
7. 在始终激活的 `Canvas` 上添加：
   - `EndingSequencePresenter`
   - `VideoPlayer`
   - `AudioSource`
8. `AudioSource` 设置：
   - Play On Awake：关闭
   - Loop：关闭
   - Spatial Blend：`0`（2D）
9. `VideoPlayer` 设置：
   - Play On Awake：关闭
   - Loop：关闭
   - Render Mode：`API Only`
   - Audio Output Mode：`Audio Source`

`EndingSequencePresenter` 字段挂接如下：

| 字段 | 引用或取值 |
| --- | --- |
| Presentation Root | `Canvas/EndingRoot` |
| Cg Image | `Canvas/EndingRoot/CgImage` |
| Dialogue Text | `Canvas/EndingRoot/EndingText` |
| Characters Per Second | `40` |
| Ending One Cg | `Assets/_Project/Art/结局CG/shattered.png` |
| Ending Two Cg | `Assets/_Project/Art/结局CG/take_flight.png` |
| Ending Three Cg | `Assets/_Project/Art/结局CG/safe_landing.png` |
| Credits Video Image | `Canvas/EndingRoot/CreditsVideo` |
| Credits Aspect Ratio | `CreditsVideo` 上的 `AspectRatioFitter` |
| Video Player | `Canvas` 上的 `VideoPlayer` |
| Video Audio Source | `Canvas` 上的 `AudioSource` |
| Credits Video Clip | 策划导入的共用致谢 `VideoClip` |
| Bgm Fade Duration | `0.5` |

最后将 `Canvas/GameplayUIController.Ending Presenter` 指向同一 Canvas 上的
`EndingSequencePresenter`，保存场景并检查 Unity 生成的差异。

## 3. 致谢视频导入

致谢视频由策划使用 Premiere 制作。推荐导出为 H.264 MP4、单条 AAC
立体声音轨，画面比例与目标构建一致，并导入：

`Assets/_Project/Art/结局CG/credits.mp4`

导入完成后：

1. 等待 Unity 将文件识别为 `VideoClip`。
2. 将它挂到 `EndingSequencePresenter.Credits Video Clip`。
3. 不要开启 Loop 或 Play On Awake。
4. 提交视频及 Unity 自动生成的 `.meta`。

视频开始前当前 BGM 会淡出；视频音轨通过现有 BGM Mixer Group 播放，因此
遵循 Master 和 BGM 音量设置。结局结束并加载下一周目或 MainMenu 后，全局
`Music` 会恢复。若视频或引用缺失，程序会输出
`[EndingSequence]` 错误并安全返回主菜单，不会永久卡在结局状态。

## 4. 验证步骤

1. 执行 `Tools/Jam Template/Validate Story Scripts`，确认普通剧情和结局
   配置全部通过。
2. 执行
   `Tools/Jam Template/Tests/Run Ending Sequence Unit Test`。
3. 从 `Boot` 进入 `MainMenu`，再进入 `GamePlay`。
4. 依次验证回合耗尽、第二次砸墙、END_02、END_03 四种路径。
5. 真相路径必须先显示5句黑屏文本，再显示 END_01 CG 和2句；坠毁音效只
   播放一次。
6. END_02/03 必须播放完全部台词后才进入致谢视频；视频阶段所有确认输入
   都不能跳过。
7. 视频结束后应返回主菜单。再次进入 GamePlay 时，周目已增加，临时物品和
   场景状态已清空，`TruthKnown` 和已达成结局标记仍保留。
8. 分别将 Master/BGM 音量预先设为 `0`、`0.5`、`1`，确认视频音轨服从设置。
9. 临时清空 Credits Video Clip，确认 Console 报错且仍返回主菜单；测试后
   还原引用。
10. 检查常用分辨率下 CG、英文字幕和视频不拉伸、不越出安全区。

新增 C# 文件的 `.meta`、场景引用、视频资产及其 `.meta` 均由 Unity 和人类
成员生成、复核并提交。
