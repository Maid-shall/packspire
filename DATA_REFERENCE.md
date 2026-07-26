# PACKSPIRE データ系まとめ

最終更新: 2026-07-26
基準: 現行Unityコード

この文書は、現在のID、基本値、データ関係を確認するための索引です。差がある場合は `PackspireContentDatabase.asset` を優先します。

## 1. 基本列挙

| 種別 | 値 |
|---|---|
| `ItemType` | `Weapon`, `Armor`, `Rune`, `Supply` |
| `Element` | `Fire`, `Water`, `Wind`, `Earth` |
| `CardType` | `Attack`, `Skill`, `Power` |
| `ItemRarity` | `Common`, `Uncommon`, `Rare`, `Legendary`, `Cursed` |
| `ExplorationCardKind` | `Support`, `Use`, `Installation`, `Drawback` |
| `BattleCardAfterUse` | `Discard`, `ExhaustBattle`, `RemoveExpedition` |
| `ReactionScope` | `Mastery`, `Loadout`, `Expedition`, `Area`, `Moment`, `Memory` |
| `GridBoardPhase` | `Place`, `Path`, `Run`, `Done` |
| 画面 | `Character`, `Hub`, `Status`, `Vault`, `Heirloom`, `Faction`, `Expedition`, `Pack`, `GridBoard`, `Battle`, `Reward`, `Shop`, `Event`, `Compendium`, `GameOver`, `GameClear` |

## 2. 基本値

| 項目 | 値 |
|---|---:|
| 初期最大HP | 42 |
| 初期HP | 42 |
| 初期所持金 | 24 |
| 基本エネルギー | 3 |
| 初期手札 | 5 |
| 標準収納盤面 | 6×4 |
| GridBoard既定サイズ | 8 |
| GridBoard既定ターン | 3 |
| GridBoard既定EN | 3 |
| 既定区画数 | 3 |
| 配置術式の成熟値 | 3 |
| Doom段階 | 2 Doomごと、最大3 |
| Doom敵HP補正 | 1段階あたり約+12% |
| Doom敵攻撃補正 | 1段階あたり+1 |

## 3. 装備

| ID | 名称 | 種別 | 形状 | 属性 | 生成カード |
|---|---|---|---|---|---|
| `sword` | 欠けた剣 | Weapon | 縦2 | 火・火 | `slash`×2 |
| `shield` | 旅人の盾 | Armor | 横2 | 土・土 | `guard`×2 |
| `ember` | 熾火のルーン | Rune | 1 | 火×2点 | `spark` |
| `herb` | 薬草袋 | Supply | 1 | 水 | `mend` |
| `dagger` | 連撃の短剣 | Weapon | 1 | 風×2点 | `stab`×2 |
| `plate` | 古い胸当て | Armor | L字3 | 土・土・水 | `brace` |
| `crystal` | 共鳴結晶 | Rune | 縦2 | 水・風 | `focus` |
| `bomb` | 煤けた爆弾 | Supply | 1 | 火×2点 | `bomb` |
| `spear` | 折畳み槍 | Weapon | 縦3 | 風・火・風 | `pierce` |
| `buckler` | 歯車の小盾 | Armor | 1 | 土 | `parry` |
| `flask` | 錬金フラスコ | Supply | 横2 | 水・火 | `acid` |
| `charm` | 風読みの護符 | Rune | 1 | 風×2点 | `tailwind` |
| `cursed_blade` | 飢えた呪剣 | Weapon | 縦2 | 火×2点・土 | `devour` |

`value=2` のセルは、その属性を2点として集計します。

装備マスターは形状・レアリティ・入手帯・基礎耐久・共鳴タグに加え、`grantedCards` で戦闘面と探索面を一組として保持します。`count` によって同じ両面カードを複数枚生成できます。旧 `cardIds` / `explorationCardId` は既存アセット読込用の互換フィールドです。

入手済み個体はマスターを複製せず、`templateId` と耐久、特性、色、強化、傷跡、戦歴などの差分だけをJSONへ保存します。

装備マスターの`reactionContributions`は、配置中だけ加算される構成反応値です。第一稿では剣が剣極2、短剣が剣極1＋迅速2、盾が守護2、胸当てが守護2＋不動1などを供給します。

## 4. 反応値

反応値マスター、役職・装備・イベントの供給値、役職解放レシピはScriptableObjectへ置きます。記憶印は`MetaSave.memoryReactions`、遠征・区画値は`RunState.reactionValues`へ差分だけをJSON保存します。

`ReactionSystem`は以下を一つのスナップショットへ集計します。

- 習得済み全役職のLv倍率値。
- 現在職と配置中装備の構成値。
- 遠征・区画の蓄積値。
- 判定時だけ渡す瞬間値。
- セーブに残る記憶印。

集計結果は範囲別合計、供給源数、供給源内訳を返します。解放レシピは複数レシピ間をOR、各レシピ内をANDで評価し、成立した役職IDを`unlockedRoles`へ永続化します。

初期反応ID:

`martial`, `blade_extreme`, `guard`, `observation`, `speed`, `artifice`, `hunt`, `fortification`, `siege`, `immovable`, `asymmetry`, `commerce`, `spore`, `void`, `sacrifice`, `blade_saint_seal`

## 5. 戦闘カード

攻撃の表示式は、期待値を中心に `2D6 + (期待値 - 7)` として示します。

| ID | 種別 | EN | 主効果 |
|---|---|---:|---|
| `basicStrike` | Attack | 1 | 期待5、`2D6-2` |
| `basicGuard` | Skill | 1 | ブロック5 |
| `basicTactic` | Skill | 0 | ブロック2 |
| `slash` | Attack | 1 | 期待7、`2D6+0` |
| `guard` | Skill | 1 | ブロック6 |
| `spark` | Power | 1 | 期待4、次攻撃+3 |
| `mend` | Skill | 1 | HP4回復 |
| `stab` | Attack | 0 | 期待3、`2D6-4` |
| `brace` | Skill | 2 | ブロック13 |
| `focus` | Power | 0 | EN+1 |
| `bomb` | Attack | 2 | 期待15、廃棄 |
| `pierce` | Attack | 2 | 期待11 |
| `parry` | Skill | 0 | ブロック3 |
| `acid` | Attack | 1 | 期待6 |
| `tailwind` | Power | 1 | EN+1、ブロック4 |
| `devour` | Attack | 1 | 期待13、自傷2 |
| `inferno` | Attack | 2 | 期待18 |
| `echoWall` | Skill | 1 | ブロック12 |
| `starBomb` | Attack | 2 | 期待24、廃棄 |

戦闘カードには `innate`（開始手札）、`retain`（保持）、`ethereal`（未使用時廃棄）、`unplayable`（直接使用不可）と、使用後の行き先を表す `afterUse` があります。`RemoveExpedition` はカードの両面をその遠征から除外し、次の戦闘・探索でも再生成しません。従来の `exhaust` も互換読込されます。

## 6. 探索カード・消耗品・状態異常

### 探索カード

| ID | 配置 | EN | 用途 |
|---|---|---:|---|
| `gb_lamp` | `lamp` | 1 | 灯を配置 |
| `gb_fog` | `fog` | 1 | 霧を配置 |
| `gb_seal` | `seal` | 1 | 封印を配置 |

探索カードは補助型、使用型、設置型、デメリット型に分類されます。対象はなし、セル、ルート、設置物、敵から選び、効果、持続、使用後処理をデータで指定します。

設置型は使用後に `GridInstallationState` となり、固有ID、元カード・装備、位置、設置ターン、進捗、現在段階を保存します。成長段階はカードマスターの `stages` に置き、段階ごとの名称、説明、カード画像、盤面画像、常時・進入時・ターン時・成熟時効果を参照できます。時間経過とルート通過は共通の進捗APIを通り、別条件も同じ入口へ追加できます。盤面ホバーは現在段階を、図鑑は全段階を表示します。

### 消耗品

| ID | 効果 |
|---|---|
| `heal` | HPを12回復 |
| `guard` | ブロック10 |
| `fire` | 期待値12のダイスダメージ |
| `energy` | EN+2 |

所持状態はセーブへIDで保存し、名称・説明・効果種別・効果量はScriptableObjectを参照します。

### 状態異常

| ID | 種別 | 主効果 |
|---|---|---|
| `strength` | 強化 | 与ダメージ加算 |
| `weak` | 弱体 | 与ダメージ低下 |
| `vulnerable` | 弱体 | 被ダメージ増加 |
| `poison` | 継続 | ターン終了時ダメージ、蓄積減少 |
| `burn` | 継続 | ターン終了時ダメージ |
| `regen` | 強化 | ターン終了時回復 |
| `armorBreak` | 弱体 | 獲得ブロック低下 |

カードと敵行動は状態異常ID・対象・量・持続を直接参照します。表示定義と効果参照はJSONではなく同じScriptableObject内にあります。

## 7. 共鳴

標準共鳴 `classic` の現行ルールです。

### LINK

| 組み合わせ | 効果 |
|---|---|
| `sword` + `shield` | ダメージ+1、ブロック+2 |
| `ember` + Weapon | ダメージ+2 |
| `crystal` + 隣接装備 | コスト-1 |

### カード置換

| 組み合わせ | 置換 |
|---|---|
| `sword` + `ember` | `slash` → `inferno` |
| `shield` + `crystal` | `guard` → `echoWall` |
| `bomb` + `flask` | 対象カード群 → `starBomb` |

## 8. 収納術式

### 術核

| ID | 名称 | サイズ | 回転 |
|---|---|---:|---|
| `standard` | 標準術核 | 6×4 | 自由回転 |
| `merchant` | 交易術核 | 6×4 | 自由回転 |
| `arcane` | 魔導術核 | 6×4 | 自由回転 |
| `coffin` | 棺型術核 | 6×4 | 90度単位 |
| `living` | 生体術核 | 6×4 | 反転相当のみ |

### 導線

| ID | 内容 |
|---|---|
| `classic` | 現行の属性一致補正 |
| `mute` | 色一致補正なしの検証用 |

`classic` の基本補正:

- 火 → ダメージ。
- 水 → ブロック、回復。
- 風 → コスト軽減。
- 土 → ブロック。

### 共鳴式

| ID | 内容 |
|---|---|
| `classic` | 現行LINKとカード置換 |
| `silent` | LINK・置換なしの検証用 |

### 安定性

| ID | 耐久倍率 | 過負荷閾値 | 過負荷係数 |
|---|---:|---:|---:|
| `stable` | 1.0 | 99 | 0.5 |
| `volatile` | 1.5 | 8 | 0.65 |

## 9. 色特性

| ID | 条件 | 効果 |
|---|---|---|
| `fire_dmg_5` | 火5 | ダメージ+1 |
| `fire_dmg_7` | 火7 | ダメージ+2 |
| `water_block_5` | 水5 | ブロック+1 |
| `water_block_7` | 水7 | ブロック+2 |
| `wind_cost_5` | 風5 | コスト-1 |
| `wind_cost_7` | 風7 | コスト-1 |
| `earth_block_5` | 土5 | ブロック+1 |
| `earth_block_7` | 土7 | ブロック+2 |
| `water_heal_5` | 水5 | 回復+1 |
| `water_heal_7` | 水7 | 回復+2 |
| `wind_draw_7` | 風7 | ドロー+1 |
| `earth_recycle_7` | 土7 | リサイクル+1 |
| `fire_free_7` | 火7 | 耐久消費なし |

色特性はLINKとは別系統です。

## 10. 敵

`moves` は敵が順に使用する基礎行動値です。Doomとダンジョン補正が加わります。

敵単体、探索盤面上の追跡、遭遇編成、ダンジョン総消耗の評価方針は [ENEMY_ENCOUNTER_BALANCE_FRAMEWORK.md](ENEMY_ENCOUNTER_BALANCE_FRAMEWORK.md) を参照します。現行の`tier`は粗いカタログ値であり、正式な難易度は行動列と基準ロードアウトとのシミュレーションから算出する方針です。

| ID | Tier | HP | 行動列 |
|---|---:|---:|---|
| `sentinel` | 1 | 34 | 攻撃8 / 防御9＋強化2 |
| `rats` | 1 | 29 | 攻撃8 / 攻撃6 |
| `porter` | 1 | 38 | 攻撃10 / 防御12 |
| `mage` | 2 | 45 | 妨害7＋毒3 / 攻撃11 |
| `beast` | 2 | 50 | 妨害9＋防具破壊2 / 攻撃10 |
| `knight` | 2 | 54 | 攻撃13 / 妨害6＋脆弱1 |
| `dragon` | 2 | 62 | 攻撃14 / 攻撃10 / 攻撃16 |
| `boss` | 3 | 72 | 強化＋防御14 / 攻撃12 / 攻撃17 |

`dragon` には専用ポートレート `Art/Portraits/enemy-dragon-v1` が設定されています。他の敵は正式アートの追加が必要です。

## 11. ダンジョン

| ID | 名称 | 戦闘数 | HP倍率 | 攻撃加算 | 金倍率 |
|---|---|---:|---:|---:|---:|
| `old_spire` | 紫晶の封鐘塔 | 7 | 1.00 | 0 | 1.00 |
| `ash_forge` | 灰熱の鋳造坑 | 6 | 1.35 | 2 | 1.25 |
| `hollow_archive` | 虚ろなる大記憶庫 | 8 | 1.70 | 4 | 1.55 |

`old_spire` は4つの `DungeonAreaContent` を明示した完成版サンプルです。区画IDは `threshold`、`crossroads`、`deep_vault`、`bell_heart`。最終区画は帰還点の代わりに `boss` を配置し、その撃破で踏破します。専用報酬プールは `old_spire_trials` です。

`DungeonAreaContent.layoutRows` を指定すると、その区画はランダムな長方形生成ではなく個別レイアウトを使います。各文字は `.` が床、`S` が入口、`#` が通行不能、`X` が盤面外です。行数と各行の文字数は区画サイズと一致させ、`S` は1つだけ置きます。`X` は外周の欠けや内部の奈落として扱い、UIではセル自体を描画しません。

明示区画がない旧形式のダンジョンだけは、戦闘数からGridBoard区画数を2～4へ変換します。旧36ノード式マップは現行仕様ではありません。

## 12. キャラクター

現行ID:

- `ren` — 既定キャラクター。
- `mio`
- `kuro`
- `hina`
- `sena` — アクション用ポートレート設定あり。

キャラクターデータは以下を持ちます。

- 名前、肩書き、説明。
- 立ち絵Resourcesパス。
- 選択画面用正面絵。
- 拠点用横顔。
- 特性ID、特性値。
- 戦闘中1回のアクティブスキル。
- バナー用フォーカス座標とズーム。

名称・説明・特性・画像参照は `PackspireContentDatabase.asset` のキャラクター欄で編集します。

## 13. 役職

### 基本

- `warrior`
- `guardian`
- `scout`
- `artificer`

### 上級

- `blade_master`
- `bulwark`
- `hunter`
- `grand_artificer`

### 隠し・複合・配置・勢力系

- `arsenal_lord`
- `pack_saint`
- `rune_weaver`
- `grid_dancer`
- `quickblade`
- `anchor_knight`
- `siege_channeler`
- `right_hand_swordsman`
- `iron_vanguard`
- `spore_druid`
- `guild_factor`
- `void_apostle`

役職カタログと効果用の基盤はありますが、全解除条件・全効果・表示文が完成済みとはみなしません。

## 14. 勢力

| ID | 名称 |
|---|---|
| `iron` | 鉄殻軍 |
| `spore` | 胞子教団 |
| `guild` | 荷造り師組合 |
| `void` | 虚無の巡礼者 |

各勢力は評判値と段階名を持ちます。正式な報酬、役職解除、イベント分岐はコンテンツ投入時に確定します。

## 15. 拠点施設

| ID | 接続画面 |
|---|---|
| `gate` | Expedition |
| `forge` | Pack |
| `vault` | Vault |
| `heirloom` | Heirloom |
| `guild` | Status |
| `codex` | Compendium |
| `embassy` | Faction |
| `shop` | Shop |
| `barracks` | Character |

施設はテーマ、マップ上の座標、解放状態、アイコンResourcesパスを持ちます。

## 16. 保存データ

### セーブキー

- 現行: `packspire_unity_save_v17`
- 書き込み途中: `packspire_unity_save_v17_staging`
- 自動退避: `packspire_unity_save_v17_backup`
- 移行元: v16、v1
- `MetaSave.version`: 17

保存時は「旧現行値をbackupへ退避 → stagingへ新規値を書き込み → 現行値を更新」の順で確定します。読み込み時はstaging、現行、backup、旧版の順に有効なJSONを探索するため、書き込み中断や現行データ破損から復旧できます。

### MetaSave

主な永続項目:

- 拠点所持金。
- 保管庫レベル。
- ラン回数、勝利数、ダンジョン解放数。
- 選択キャラクター、役職、勢力、バッグ、家宝、ロードアウト。
- 保管庫の装備。
- ロードアウト。
- 消耗品と容量。
- 発見済みアイテム・敵・ダンジョン。
- 解放済み役職・秘密。
- 役職レベル。
- 勢力評判。

既定値:

- `baseGold=24`
- `selectedCharacterId=ren`
- `currentRole=warrior`
- `currentFaction=iron`
- `selectedBackpack=standard`
- `consumableCapacity=5`
- 基本4役職を解放済み。
- `sword`, `shield`, `ember`, `herb` を発見済み。

### RunState

ランをまたいで保持する項目:

- HP、最大HP、所持金。
- キャラクター、役職、ダンジョン、勢力。
- 収納術式4構成。
- インベントリ、戦利品袋。
- 配置情報。
- 消耗品。
- 区画・戦闘進行。

戦闘ごとに初期化される項目:

- エネルギー。
- ブロック。
- 一時攻撃強化。
- 状態。
- 山札、手札、捨て札。

## 17. データ追加時のチェック

### 装備

- 一意な`templateId`。
- 占有セルと持ち手セル。
- 回転後も範囲内判定できる形。
- 属性と点数。
- 戦闘カード参照。
- 探索面参照。
- 名称、説明、画像。
- LINK・色特性・耐久との相互作用。

### カード

- 一意なID。
- 種別、コスト、効果データ。
- 表示文を効果値から生成できること。
- 対象、廃棄、ドロー、状態などの明示。
- 画像とプレビュー。
- ダイス式と期待値の一致。

### 敵

- ID、Tier、基礎HP。
- 行動列または行動AI。
- 次行動の表示データ。
- 画像。
- 戦利品・報酬。
- Doomとダンジョン倍率適用後の上限確認。

### イベント

- ID、盤面アイコン。
- 本文、選択肢、条件、結果。
- 1回限りか再訪可能か。
- 解決後の復帰先。
- セーブ対象となる状態。

## 18. 主なデータソース

| データ | ファイル |
|---|---|
| 基本モデル | `unity/PackspireUnity/Assets/Scripts/Core/Models.cs` |
| マスターデータ入口 | `unity/PackspireUnity/Assets/Resources/Packspire/PackspireContentDatabase.asset` |
| カード・状態・消耗品 | `unity/PackspireUnity/Assets/Resources/Packspire/Content/CardContent.asset` |
| 装備・収納術式 | `unity/PackspireUnity/Assets/Resources/Packspire/Content/ItemContent.asset` |
| 役職・敵・キャラクター | `unity/PackspireUnity/Assets/Resources/Packspire/Content/ActorContent.asset` |
| ダンジョン・施設・イベント・商人・報酬・バランス | `unity/PackspireUnity/Assets/Resources/Packspire/Content/WorldContent.asset` |
| データ型と検証 | `unity/PackspireUnity/Assets/Scripts/Core/PackspireContentDatabase.cs` |
| 初期データ生成 | `unity/PackspireUnity/Assets/Editor/PackspireContentAssetBuilder.cs` |
| 装備・カード等の互換API | `unity/PackspireUnity/Assets/Scripts/Core/GameCatalog.cs` |
| キャラクター互換API | `unity/PackspireUnity/Assets/Scripts/Core/CharacterCatalog.cs` |
| 拠点施設互換API | `unity/PackspireUnity/Assets/Scripts/Core/HubFacilityCatalog.cs` |
| 収納術式互換API | `unity/PackspireUnity/Assets/Scripts/Systems/StorageFormulaCatalog.cs` |
| 格子盤定数と状態 | `unity/PackspireUnity/Assets/Scripts/Systems/GridBoardSystem.cs` |
| セーブ処理 | `unity/PackspireUnity/Assets/Scripts/Systems/SaveSystem.cs` |
| ビルド前検証 | `unity/PackspireUnity/Assets/Editor/PackspireContentBuildValidator.cs` |
| EditModeテスト | `unity/PackspireUnity/Assets/Editor/Tests/PackspireEditModeTests.cs` |

セーブデータは `SaveSystem` がJSONで管理し、マスターデータはScriptableObjectで管理します。セーブ内にはScriptableObject参照ではなく既存の文字列IDを保持するため、現在のセーブ互換性は維持されます。
