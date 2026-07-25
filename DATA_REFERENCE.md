# PACKSPIRE データ系まとめ

最終更新: 2026-07-26
基準: 現行Unityコード

この文書は、現在コードに存在するID、基本値、データ関係を確認するための索引です。コードと差がある場合はコードを優先します。日本語文字列の一部はソース上で文字化けしているため、名称は既存の設計用語を併記しています。

## 1. 基本列挙

| 種別 | 値 |
|---|---|
| `ItemType` | `Weapon`, `Armor`, `Rune`, `Supply` |
| `Element` | `Fire`, `Water`, `Wind`, `Earth` |
| `CardType` | `Attack`, `Skill`, `Power` |
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

## 4. 戦闘カード

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

## 5. 共鳴

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

## 6. 収納術式

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

## 7. 色特性

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

## 8. 敵

`moves` は敵が順に使用する基礎行動値です。Doomとダンジョン補正が加わります。

| ID | Tier | HP | 行動値 |
|---|---:|---:|---|
| `sentinel` | 1 | 34 | 8, 5 |
| `rats` | 1 | 29 | 8, 6 |
| `porter` | 1 | 38 | 10, 0 |
| `mage` | 2 | 45 | 7, 11 |
| `beast` | 2 | 50 | 9, 10 |
| `knight` | 2 | 54 | 13, 6 |
| `dragon` | 2 | 62 | 14, 10, 16 |
| `boss` | 3 | 72 | 12, 12, 17 |

`dragon` には専用ポートレート `Art/Portraits/enemy-dragon-v1` が設定されています。他の敵は正式アートの追加が必要です。

## 9. ダンジョン

| ID | 名称 | 戦闘数 | HP倍率 | 攻撃加算 | 金倍率 |
|---|---|---:|---:|---:|---:|
| `old_spire` | 古塔パックスパイア | 5 | 1.00 | 0 | 1.00 |
| `ash_forge` | 灰熱の鋳造坑 | 6 | 1.35 | 2 | 1.25 |
| `hollow_archive` | 虚ろなる大記憶庫 | 8 | 1.70 | 4 | 1.55 |

GridBoard上の区画数は戦闘数から2～4へ変換されます。旧36ノード式マップは現行仕様ではありません。

## 10. キャラクター

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

現ソースの日本語名・説明は文字化けしているため、正式データ移行時に再定義します。

## 11. 役職

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

## 12. 勢力

| ID | 名称 |
|---|---|
| `iron` | 鉄殻軍 |
| `spore` | 胞子教団 |
| `guild` | 荷造り師組合 |
| `void` | 虚無の巡礼者 |

各勢力は評判値と段階名を持ちます。正式な報酬、役職解除、イベント分岐はコンテンツ投入時に確定します。

## 13. 拠点施設

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

## 14. 保存データ

### セーブキー

- 現行: `packspire_unity_save_v17`
- 移行元: v16、v1
- `MetaSave.version`: 17

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

## 15. データ追加時のチェック

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

## 16. 主なデータソース

| データ | ファイル |
|---|---|
| 基本モデル | `unity/PackspireUnity/Assets/Scripts/Core/Models.cs` |
| 装備・カード・役職・敵・ダンジョン・勢力 | `unity/PackspireUnity/Assets/Scripts/Core/GameCatalog.cs` |
| キャラクター | `unity/PackspireUnity/Assets/Scripts/Core/CharacterCatalog.cs` |
| 拠点施設 | `unity/PackspireUnity/Assets/Scripts/Core/HubFacilityCatalog.cs` |
| 収納術式 | `unity/PackspireUnity/Assets/Scripts/Systems/StorageFormulaCatalog.cs` |
| 格子盤定数と状態 | `unity/PackspireUnity/Assets/Scripts/Systems/GridBoardSystem.cs` |
| セーブ処理 | `unity/PackspireUnity/Assets/Scripts/Systems/SaveSystem.cs` |

データ移行が完了したら、この文書の表は新しい正本から自動生成できる形へ寄せるのが望ましいです。
