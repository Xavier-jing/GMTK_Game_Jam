# 音效接入 TODO

更新日期：2026-07-27

## 1. 当前结论

“音频文件存在”和“游戏中已经接入”是两个独立状态：

1. 文件必须位于 `AudioService` 能加载的 `Resources` 路径。
2. 游戏行为必须在正确时机调用该文件的 `AudioId`。
3. 最后还要在 Unity Play Mode 中验证音量、重复播放和生命周期。

当前盘点结果：

| 状态 | 数量 | 说明 |
| --- | ---: | --- |
| 已有 WAV 文件 | 33 | 当前均在 `Assets/_Project/Resources/Audio/SFX/` |
| 已写入接入逻辑 | 18 | 剧情/结局 9 个、UI 与回合反馈 5 个、环境循环 4 个 |
| 代码完成但待场景挂接 | 4 | 壁炉、水流、钟摆和 Gameplay 全局风声 |
| 有文件但尚无触发逻辑 | 15 | 见第 4.3 至 4.4 节 |
| 详细音效表中有 ID、但项目中无文件 | 7 | 见第 5 节 |
| BGM 文件 | 1 | `Music.mp3`，全局播放代码已接入 |

现有 33 个 WAV 已位于正确的运行时加载目录，且每个 WAV 都有对应 `.meta`。
其中 14 个已经具有实际触发点；4 个环境循环音的代码已完成，还需要人类在
`GamePlay` 场景挂接后才能触发。

## 2. P0：资源路径已解除阻塞，完成导入验证

- [x] 已创建
      `Assets/_Project/Resources/Audio/SFX/`。
- [x] 全部 33 个 WAV 已移入
      `Assets/_Project/Resources/Audio/SFX/`。
- [x] 33 个 WAV 均有对应 `.meta`，旧路径与新路径的 33 个 GUID 全部一致，
      且 `SFX.meta` 已生成。
- [x] `IA_Fall_Break.wav` 已使用合法名称；不要改回包含全角
      `＆` 的旧名称。
- [ ] 使用 Unity `2022.3.62f3c1` 打开项目并等待导入完成。
- [ ] 在 Inspector 中抽查文件类型均为 `AudioClip`。一次性
      音效关闭 Loop；循环环境音是否 Loop 将由运行时代码控制。
- [ ] 进入 Play Mode，触发已经接入的 14 个 AudioId，确认 Console 中没有
      `Resources/Audio/SFX` 资源缺失错误。

`AudioService` 的固定加载规则是：

- SFX：`Assets/_Project/Resources/Audio/SFX/{AudioId}.*`
- BGM：`Assets/_Project/Resources/Audio/BGM/{AudioId}.*`

后续音频移动、Import Settings 和场景引用仍需由人类在 Unity 中完成。
自动化智能体不得修改、移动或重新导入音频资产。

## 3. 已写入逻辑、等待 Play Mode 验证

| AudioId | 当前触发 |
| --- | --- |
| `IA_Drawer_Open` | 成功打开抽屉 |
| `IA_Plank_Disassembly` | 成功拆下木板 |
| `IA_Fridge_Open` | 查看冰箱时打开 |
| `IA_Fridge_Close` | 查看结束时关闭 |
| `IA_PickUp_Ceramics` | 拿起茶具或花瓶 |
| `IA_CuttingFabric` | 成功剪裁床被 |
| `IA_TrackDisassembly` | 成功拆除轨道 |
| `IA_Electricity` | 成功连接床的电源 |
| `IA_Fall_Break` | 回合归零或再次砸墙触发坠毁 |

- [ ] 条件不足、取消选择或交互失败时不播放成功音效。
- [ ] 每次成功行为只播放一次，不因剧情节点刷新而重复。
- [ ] 分别验证回合归零和再次砸墙；两条流程均只播放一次
      `IA_Fall_Break`。

## 4. UI 与环境循环代码已接入；其余 15 个现有音效待接入

### 4.1 P1：回合与 UI 反馈（代码已接入）

| AudioId | 推荐触发点 | 实施位置 |
| --- | --- | --- |
| `UI_Remaining_Increase` | 剩余回合增加 | `HudScreen.HandleTurnsChanged` |
| `UI_Remaining_Reduce` | 剩余回合减少，且减少后仍大于 1 | `HudScreen.HandleTurnsChanged` |
| `UI_Warning` | 剩余回合首次变为 1；替代本次 Reduce，避免叠音 | `HudScreen.HandleTurnsChanged` |
| `UI_Select` | 可交互选项获得键盘/手柄焦点，或鼠标首次悬停 | `StoryPresenter.ShowChoices` 创建按钮时 |
| `UI_Click` | 有效 UI 按钮确认 | 剧情选项及菜单按钮的成功回调 |

- [x] `HudScreen.OnShow` 只记录初始回合数，不播放增减音效。
- [x] `HandleTurnsChanged` 比较新旧数值；一次变化只播放一个提示音。
- [x] 从大于 1 的数值进入 1 时播放 `UI_Warning`；降至 0 时不叠加回合
      UI 音效。
- [x] 动态剧情按钮和所有 `ScreenBase` 控件均自动绑定反馈，不需要场景引用。
- [x] 鼠标悬停、键盘/手柄 Select 有 0.05 秒去重；禁用控件不播放。
- [x] `UI_Click` 仅绑定有效 Button 的鼠标左键 Click 或 Submit；`Esc` 和无效
      世界交互不播放。
- [x] UI 使用独立的 2 路 SFX 播放池，仍输出到 `SFX` Mixer Group，并在
      `AudioListener.pause` 时继续响应暂停菜单。
- [ ] Unity 导入 `UISfxFeedback.cs` 并生成、提交对应 `.meta`。
- [ ] 在 Play Mode 中验证鼠标、键盘、手柄、暂停菜单和重复开关界面。

`SFX-07` 是表格中的通用“有效交互确认”占位项，目前没有同名文件。若策划
确认它可以复用 `UI_Click`，再将有效普通交互接到
`PlayerInteractor.InteractWithCurrentTarget` 的成功分支；否则等待独立文件。

### 4.2 P1：环境循环音（代码完成、场景挂接待验证）

四项都使用 `AmbientLoopingSfxEmitter`。组件会在运行时创建专用的 2D
`AudioSource`，从 `Resources/Audio/SFX` 缓存加载 Clip，循环播放并输出到
`SFX` Mixer Group。一次性 SFX 和 UI 播放池不会抢占这些声源。

玩家距离模式只计算声源与 `Distance Target` 的 XZ 平面距离：Full Distance
内为完整音量，Silent Distance 外静音，中间使用 SmoothStep 衰减；音量以
每秒 `2` 的速度平滑变化。全局模式不需要 Distance Target。

- [x] `AudioService.TryConfigureLoopingSfxById` 已接入现有 SFX Mixer 和
      Resources Clip 缓存。
- [x] `AmbientLoopingSfxEmitter.cs` 已实现专用声源、循环、XZ 距离衰减、
      暂停跟随以及禁用/销毁清理。
- [ ] Unity 导入 `AmbientLoopingSfxEmitter.cs`，生成并提交
      `AmbientLoopingSfxEmitter.cs.meta`；无需手工添加 `AudioSource`。
- [ ] 在 `Assets/_Project/Scenes/GamePlay.unity` 当前层级路径
      `EventSystem/Fireplace` 上添加 `AmbientLoopingSfxEmitter`：
      Audio Id=`IA_Campfire`，Base Volume=`0.65`，
      Attenuation Mode=`PlayerDistance`，Distance Target=`Player`，
      Full Volume Distance=`2.5`，Silent Distance=`10`，
      Volume Change Speed=`2`。
- [ ] 在同场景当前层级路径 `EventSystem/Garden_sink_water` 上添加组件：
      Audio Id=`IA_Water`，Base Volume=`0.60`，
      Attenuation Mode=`PlayerDistance`，
      Distance Target=`Player`，Full Volume Distance=`2`，
      Silent Distance=`9`，Volume Change Speed=`2`。
- [ ] 使用现有
      `Assets/_Project/Art/House/house_com/Clock.png` 由人类在场景中放置
      根对象 `Clock`，再添加组件：Audio Id=`IA_Pendulum`，
      Base Volume=`0.35`，Attenuation Mode=`PlayerDistance`，
      Distance Target=`Player`，Full Volume Distance=`2`，
      Silent Distance=`8`，Volume Change Speed=`2`。
- [ ] 在场景根对象 `====Envitoment====` 上添加组件：
      Audio Id=`IA_Wind_HouseFall`，Base Volume=`0.30`，
      Attenuation Mode=`Global2D`，Volume Change Speed=`2`；
      Distance Target 留空。它会随 Gameplay 对象启用而播放，并在离开
      场景时自动停止。
- [ ] 由人类保存并提交 `GamePlay.unity`。不要修改四个 WAV 的 Import
      Settings；MainMenu 和 SandBox 不挂这些组件。
- [ ] 依次站在 Full、衰减区间和 Silent 距离验证壁炉、水流与钟摆；改变
      Player 的 Y 高度不应改变音量。缺少 Audio Id、Clip、AudioService 或
      Player 引用时，应只出现一次包含组件对象名称的错误并停止播放。
- [ ] 同时播放四项以及一次性/UI SFX，确认互不抢占；分别把 SFX/Master
      音量设为 `0`、`0.5`、`1`，确认 BGM 音量不影响环境音。
- [ ] 暂停后四项冻结、恢复后继续；离开 Gameplay 后全部停止。试听循环
      接缝；爆音或空白属于原始音频循环点问题，交给音频成员处理。

### 4.3 P1：世界交互与状态变化

| AudioId | 所需触发条件 | 当前阻塞 |
| --- | --- | --- |
| `IA_Drawer_Close` | 抽屉真正关闭完成 | 当前剧情只有打开行为，没有关闭节点 |
| `IA_Curtain_Open` | 窗帘从关闭变为打开 | 尚无稳定窗帘交互对象或剧情命令 |
| `IA_Curtain_Close` | 窗帘从打开变为关闭 | 同上 |
| `IA_Blinds_Break` | 百叶窗进入破损状态 | 场景有 `Window`/`Window_broken`，但尚未确认是否就是该事件 |
| `IA_Dresser_Break` | 梳妆台坠落并破损 | 尚无明确的落地/破损事件 |
| `IA_Shrub` | 玩家进入或穿过灌木 | 尚未确认灌木 Collider 与对象路径 |

- [ ] 策划先确认每项的唯一状态变化，不把声音绑在“尝试交互”上。
- [ ] 能由剧情 JSON 表达的行为，在成功改变状态的动作之后追加 `PlaySfx`。
- [ ] 物理碰撞或 Trigger 行为使用小型 `.cs` 组件，并加入一次性保护或冷却，
      防止每个物理帧重复播放。
- [ ] 由人类在 Unity 中确认并记录窗帘、百叶窗、梳妆台和灌木的准确
      GameObject/Prefab 路径，再完成组件挂接与场景保存。

### 4.4 P2：角色移动

尚未接入的移动音效：

- `IA_JumpLand`
- `IA_Footstep_Carpet01`、`IA_Footstep_Carpet02`
- `IA_FootStep_Rock01`、`IA_FootStep_Rock02`
- `IA_FootStep_Grass01`、`IA_FootStep_Grass02`
- `IA_FootStep_Wood01`、`IA_FootStep_Wood02`

- [ ] 在 `PlayerInAirState` 从空中状态切到 Idle 的真实落地边沿播放一次
      `IA_JumpLand`；不要在持续 Grounded 检查中每帧播放。
- [ ] 决定下沉落地状态 `PlayerSinkingState` 是否复用
      `IA_JumpLand`，还是只保留镜头震动和结局音效。
- [ ] 新增最小的 `PlayerFootstepAudio.cs`，根据“正在移动、已着地、步频计时”
      播放脚步；每种地面在 01/02 中随机选择且避免连续重复。
- [ ] 新增地面类型标记 `.cs`，包含 Carpet、Rock、Grass、Wood 四类。
- [ ] 由人类在 `GamePlay` 场景中盘点所有可行走 Collider，把地面标记组件
      挂到准确对象并选择类型；把脚步组件挂到名为 `Player` 的对象。
- [ ] 建议起始步频：走路 `0.38–0.48s` 一次，再按动画实际节奏试听调整。
- [ ] Unity 生成并提交两个新增脚本的 `.meta`，由人类保存并提交场景引用。

此方案不需要修改现有动画资产；符合比赛的资产编辑限制，也减少给多方向动画
逐个添加 Animation Event 的工作量。

## 5. 表格中存在、但项目里没有文件的音效

详细音效表中的以下 7 个 AudioId 没有对应 WAV：

- `IA_ItemFall_Medium`
- `IA_ItemFall_Small`
- `IA_HeadImpact`
- `IA_Item_Hit_Big`
- `IA_Item_Hit_Medium`
- `IA_Item_Hit_Small`
- `UI_01`

- [ ] 音频负责人确认这些资源是遗漏、取消，还是改名。
- [ ] 文件交付后先核对 AudioId，再放入
      `Assets/_Project/Resources/Audio/SFX/`。
- [ ] 物品落地/碰撞音需要按物品尺寸和碰撞冲量分级，并设置单物体冷却。
- [ ] 头部碰撞音需要玩家头部碰撞边界或可靠的接触点判定；未具备判定前不接。
- [ ] 策划说明 `UI_01` 的具体用途；用途未知时不要绑定到任意按钮。

主设计表还有以下占位需求，没有最终同名资源：

| 占位项 | 需求 | 推荐下一步 |
| --- | --- | --- |
| `SFX-01` | 剩余 1 回合时灯光闪烁、电流异常 | 确认是否复用 `IA_Electricity` |
| `SFX-02` | 砸大洞、真相开始时墙体与房屋坠毁连续声 | 确认是一条新音频，还是多个现有音效组合 |
| `SFX-03` | 结局二积极收束 | 提供独立结局音乐或 Sting |
| `SFX-04` | 结局三安全落地、释然 | 提供独立结局音乐或 Sting |
| `SFX-07` | 有效普通交互确认 | 确认是否复用 `UI_Click` |

## 6. 全局 BGM（代码已接入、Play Mode 待验证）

最终文件为 `Assets/_Project/Resources/Audio/BGM/Music.mp3`，运行时 AudioId
为 `Music`。

- [x] `AppContext` 在每次非 Boot 场景加载后请求播放 `Music`，使用 1 秒淡入。
- [x] BGM 由常驻的 `AudioService` 持有，输出到 `BGM` Mixer Group；
      `MainMenu → GamePlay → SandBox` 等普通切场景不会重新开始或叠播。
- [x] Boot 视频期间不启动该 BGM，避免与启动视频的直接音轨重叠。
- [x] 结局 Credits 视频开始前仍会淡出全局 BGM；进入下一周目或返回
      MainMenu 后，场景加载事件会恢复 `Music`。
- [ ] 使用 Unity `2022.3.62f3c1` 等待 `Music.mp3` 导入完成，确认识别为
      `AudioClip`，并提交 `BGM.meta` 与 `Music.mp3.meta`。
- [ ] 从 Boot 进入 MainMenu，确认启动视频结束后开始播放；连续进入
      GamePlay、返回 MainMenu 和进入 SandBox，确认音乐不中断、不从头开始、
      不产生第二路叠音。
- [ ] 分别把 Master/BGM 音量设为 `0`、`0.5`、`1`，重启游戏后确认设置
      仍然生效；SFX 音量不应影响 BGM。
- [ ] 暂停时 BGM 随 `AudioListener.pause` 暂停，恢复后从原位置继续。
- [ ] 验证 43 秒循环接缝无爆音或明显空白；若存在问题，交由音频成员调整
      原始文件循环点。

## 7. 完成标准

- [ ] `Boot → MainMenu → GamePlay` 和 `MainMenu → SandBox` 无新增 Audio
      或 Story Console 错误。
- [ ] HUD、暂停菜单、设置菜单、返回主菜单的 UI 音效无漏播或双响。
- [ ] SFX、BGM、Master 音量在 `0`、`0.5`、`1` 下分别验证，并确认持久化。
- [ ] 暂停与恢复后，一次性音效和循环音状态正确。
- [ ] 离开 Gameplay 后，环境循环音全部停止，没有残留 AudioSource。
- [ ] 运行剧情校验；所有 `PlaySfx`/`SwitchBgm` AudioId 都符合命名规则。
- [x] 使用 Unity `2022.3.62f3c1` 的程序集引用完成离线 C# 编译检查。
- [ ] 在 Unity `2022.3.62f3c1` 中完成正式脚本编译和 Play Mode 人工冒烟。
- [ ] 最终 Git 状态仅包含获准的 `.cs`、`.json`、`.md` 修改，以及人类在
      Unity 中完成的资源移动、自动生成 `.meta` 和场景引用。
