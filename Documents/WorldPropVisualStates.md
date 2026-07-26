# 道具交互视觉状态

## 已确认的状态素材

当前剧情、周目状态和美术素材三者能够明确对应的道具有两个：

| 道具 | 剧情命令 | 状态标记 | 默认 Sprite | 变更后 Sprite |
| --- | --- | --- | --- | --- |
| 床头柜（`Dresser`） | `OpenDresser` | `DresserOpened` | `Assets/_Project/Art/House/house_com/Bedside_table_Closed.png` | `Assets/_Project/Art/House/house_com/Bedside_table_Open.png` |
| 床体（`CableBed`） | `RevealBedSwitch` | `BedLifted` | `Assets/_Project/Art/House/house_com/Bed_group_close.png` | `Assets/_Project/Art/House/house_com/Bed_group_open.png` |

`WorldStoryInteractable` 会在刷新道具显示时读取 `RunState`。状态命令成功后立即换成变更后的 Sprite；新周目调用 `RunState.Reset()` 后自动恢复默认 Sprite。携带、丢弃和按剧情进度隐藏道具的原有规则保持不变。

## 需要人类在 Unity 中挂接

必须使用 Unity `2022.3.62f3c1` 完成以下场景设置。不要给状态图片另建第二套交互对象；同一道具只保留一个有效的 `WorldStoryInteractable` 和 `StoryTarget`。

### 床头柜

1. 打开 `Assets/_Project/Scenes/GamePlay.unity`。
2. 选择 `====物品====/Bedside_table_Closed`，确认其 `WorldStoryInteractable`：
   - `Prop Id` 为 `Dresser`；
   - `First Script Id` 为 `prop_dresser`；
   - 同对象的 `StoryTarget.Target Id` 为 `prop_dresser`。
3. 在 `Run-state sprite` 中设置：
   - `Visual State Renderer`：该对象负责显示床头柜的子对象 `GameObject` 上的 `SpriteRenderer`；
   - `Default State Sprite`：`Bedside_table_Closed.png`；
   - `Changed State Sprite`：`Bedside_table_Open.png`。
4. 场景中如果还有单独的交互根对象 `====物品====/Bedside_table_Open`，将其停用或移除，避免重复碰撞体和重复 `StoryTarget`。房间背景层中的同名装饰对象也应由场景负责人确认是否与可交互版本重叠。

### 床体

1. 在同一场景选择 `====物品====/Bed_group_close`，确认其 `WorldStoryInteractable`：
   - `Prop Id` 为 `CableBed`；
   - `First Script Id` 为 `prop_cable_bed`；
   - 同对象的 `StoryTarget.Target Id` 为 `prop_cable_bed`。
2. 在 `Run-state sprite` 中设置：
   - `Visual State Renderer`：该对象负责显示床体的子对象 `GameObject` 上的 `SpriteRenderer`；
   - `Default State Sprite`：`Bed_group_close.png`；
   - `Changed State Sprite`：`Bed_group_open.png`。
3. 将单独的交互根对象 `====物品====/Bed_group_open` 停用或移除，保证只有关闭状态的根对象负责交互，打开状态由 Sprite 切换产生。

以上操作只引用已有 Sprite，不会生成新的美术资产。保存场景后不需要手动创建 `.meta`；本次代码没有新增 `.cs` 文件。

## 暂不接入的候选素材

- `Fridge.png` 只有关闭状态图片。剧情虽然在查看时播放开门和关门音效，但当前仓库没有冰箱开门 Sprite；`FridgeUnplugged` 表示断电，不能代替门的开关状态。
- `Window.png` / `Window_broken.png` 有成对素材，但现有道具枚举和剧情命令没有确认“百叶窗破损”事件。
- `Cabinet.png` / `Cabinet_broken.png` 有成对素材，但现有剧情没有“柜体坠落并破损”的命令。
- 木板拿取、大小墙洞切换、床开关出现等效果已经由携带状态或 `WorldPropRules.IsPresent()` 控制整个对象显隐，不需要再交换 Sprite。

在策划确认冰箱开门图或破损事件的准确触发点后，再为对应道具增加独立状态；不要把音效名称直接当作游戏状态。

## Play Mode 冒烟

1. 初始进入 `GamePlay`，确认床头柜显示关闭图、床体显示关闭图，Console 没有 `run-state sprites` 配置警告。
2. 在床头柜剧情选择拾取扳手，确认 `OpenDresser` 成功后立即显示打开图，并且扳手/剪刀的原有显隐仍正常。
3. 满足真相与轨道移除条件后，在床体剧情选择掀起床垫，确认 `RevealBedSwitch` 成功后显示打开图，床开关同时出现。
4. 携带和放下床头柜或床体，确认重新显示时仍保持当前周目的正确 Sprite。
5. 开始下一周目，确认两个道具都恢复默认图，没有残留上一周目的状态。
6. 查看冰箱，确认现有剧情与开关门音效不受影响；在没有开门 Sprite 前画面维持当前图片。
