# 剧情音效接入与资源挂接

## 1. 本批接入范围

本批的直接交互使用剧情解释器已有的 `PlaySfx` 动作，把音效放在对应语义动作
成功之后。动作失败时后续音效不会执行，避免出现“交互未生效但播放成功反馈”
的情况。

`IA_Fall_Break` 同时用于回合归零和砸开大洞。前者可能由任意包含 `SpendTurns`
的剧本触发，不能安全地绑定到单一 JSON，所以由
`GameplayUIController.HandleRunEnded` 在收到 `EndingOne` 或
`TruthRevealed` 时统一播放，确保两类死亡只维护一条音效映射。

| 剧本与节点 | 触发行为 | AudioId |
| --- | --- | --- |
| `prop_dresser/take_wrench` | 成功打开抽屉 | `IA_Drawer_Open` |
| `prop_plank/take` | 成功拆下木板并放入携带槽 | `IA_Plank_Disassembly` |
| `prop_refrigerator/inspect` | 查看冰箱时打开冰箱门 | `IA_Fridge_Open` |
| `prop_refrigerator/inspect_followup` | 查看结束时关闭冰箱门 | `IA_Fridge_Close` |
| `prop_tea_set/put_in` | 成功拿起茶具 | `IA_PickUp_Ceramics` |
| `prop_vase/put_in` | 成功拿起花瓶 | `IA_PickUp_Ceramics` |
| `prop_bed_blanket/cut` | 成功剪裁床被 | `IA_CuttingFabric` |
| `prop_wrench/remove_rail` | 成功拆除轨道 | `IA_TrackDisassembly` |
| `prop_power_connector/connect` | 成功连接床的电源 | `IA_Electricity` |
| 周目结束回调 | 回合归零或再次砸墙触发坠毁流程 | `IA_Fall_Break` |

没有明确剧情节点的脚步、跳跃、环境循环、物品碰撞和通用 UI 音效不在本批
JSON 接入范围内。策划表中的 `SFX-01`、`SFX-02`、`SFX-03`、`SFX-04`、
`SFX-07` 等占位 ID 只有在对应的最终音频文件、稳定 `AudioId` 和触发归属
确认后才能继续挂接。全局 BGM 已使用 `Music` 接入，不需要写入剧情 JSON。
`SFX-06` 已按拆轨行为映射为
`IA_TrackDisassembly`。

## 2. Unity 资源状态与导入确认

剧情解释器运行时从 `Resources/Audio/SFX/{AudioId}` 加载音效。当前全部
33 个 WAV 已位于 `Assets/_Project/Resources/Audio/SFX/`，且各自带有
`.meta`。本批 9 个 AudioId 的资源路径已经就绪。

请由人类使用 Unity `2022.3.62f3c1` 完成以下确认：

1. 等待 Unity 导入 `SFX` 文件夹和资源。
2. 确认下列 9 个已接入文件均被识别为 `AudioClip`：
   - `IA_CuttingFabric.wav`
   - `IA_Drawer_Open.wav`
   - `IA_Electricity.wav`
   - `IA_Fridge_Close.wav`
   - `IA_Fridge_Open.wav`
   - `IA_PickUp_Ceramics.wav`
   - `IA_Plank_Disassembly.wav`
   - `IA_TrackDisassembly.wav`
   - `IA_Fall_Break.wav`
3. 一次性音效建议关闭 `Loop`；其余 Import Settings 保持音频成员确认过的
   设置。
4. 确认 `SFX.meta` 和 33 个音频 `.meta` 一并提交。不要改回包含全角
   `＆` 的旧文件名。

本批不需要新增组件、修改场景、Prefab 或 Inspector 引用。若音频未移动到
上述路径或导入失败，剧情仍会继续，但 Console 会出现
`[StoryAudio/PlaySfx]` 或 `GameplayUIController` 的资源缺失错误且不会听到
声音。

其余已有文件中，4 个环境循环音已完成代码但待场景挂接，另有 15 个尚无
触发逻辑；7 个缺失文件和全局 BGM 的验证安排见
`Documents/AudioIntegrationTodo.md`。

## 3. 校验与冒烟

1. 执行 `Tools/Jam Template/Validate Story Scripts`，预期所有剧情脚本通过。
2. 从 `Boot` 进入 `MainMenu`，再进入 `GamePlay`，确认 Console 没有新的
   Story 或 Audio 错误。
3. 逐项触发上表行为。直接交互音效应在语义动作成功后播放一次；条件不足、
   选项取消或交互失败时不应播放。
4. 冰箱查看分支应在第一句显示前播放开门声，在第二句完成后播放关门声。
5. 分别通过耗尽剩余回合与再次砸墙触发两类死亡；两条流程都应只播放一次
   `IA_Fall_Break`，结局二和结局三不应播放该音效。
6. 将 SFX 音量设为 `0`、`0.5`、`1` 各验证一次，并在返回主菜单、重新进入
   `GamePlay` 后确认音量持久化。
7. 暂停游戏时确认一次性音效遵循当前 `AudioListener.pause` 行为，恢复后游戏
   与剧情仍可继续。
