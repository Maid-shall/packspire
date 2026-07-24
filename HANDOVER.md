# PACKSPIRE 引き継ぎ資料（Codex復帰用）

更新: 2026-07-22  
対象: `C:\maid apps\Pick Spire`（Unity: `unity/PackspireUnity`）

> **このファイルが「今の正」**。`NEXT_STEPS.md` の拠点2.5D記述は 2026-07-18 時点の計画で、**現行の拠点UIとは一致しない**。長期案として残しているだけ。

---

## 1. まず知っておくこと

| 項目 | 内容 |
|---|---|
| 作業場所 | `C:\maid apps\Pick Spire` のみ（`Pick Spire - cursor` は触らない） |
| 製品軸 | Unity 版。Web 版は参照・試験用 |
| コミット | **ユーザーが明示したときだけ**。勝手に commit / push しない |
| 開発メニュー | Play 中 **F10** → 画面ジャンプ（Hub / Pack / Map / Battle 等） |
| テーマ計画 | `.cursor/plans/全体テーマ案_dd914710.plan.md` |

---

## 2. いま何をしているか（2026-07-22 時点）

**ビジュアル／世界観の大転換中。**

- 旧方向: DD 茶黒・Bloodborne 寄り（`HubDD` / `ChromeDD`、教会・聖遺箱語彙）
- 新方向: **ポップダーク**（シノアリス級の暗さ＋彩度）＋ **太線・通常頭身キャラ**（ディスガイア寄り）
- 裏設計: ダンテ『神曲』（漏斗・contrapasso・100）— **構造だけ借りる。プレイヤーに見せない**
- 表層語彙: **未確定**（税関・manifest 案は却下。候補 A〜F を plan 参照）

直近の実装は **Hub と全画面の UI クロームを PopDark へ差し替え**、教会語彙の削除、施設 eyebrow の暫定リネーム。

---

## 3. 2.5D まわり — 何を凍結したか

### 3.1 凍結・撤去したもの（拠点ホーム）

2026-07-17〜18 に試作した **「横スクロール2.5D街を歩く Hub」** は、**現行ルートから外した（凍結）**。

| 項目 | 状態 |
|---|---|
| `PackspirePresentationStage.cs` | **削除済み** |
| `HubPresentationCatalog.cs` | **削除済み** |
| `PackspireLayeredPuppet.cs` | **削除済み** |
| `Packspire2_5DPrototype.cs` | **削除済み**（NEXT_STEPS でも言及） |
| `BuildPresentationHome` / `ps-v2-home` | **C# から参照なし** |
| `PackspireTheme.uss` の `.ps-v2-*` | **レガシーCSSとして残存**（削除可だが未着手） |
| `Assets/Resources/Art/Hub/` 素材 | ディスク上に残る可能性あり。**現行 Hub からは未使用** |

**凍結理由（経緯）**

- 2.5D 試作は「歩いて建物を押す」体験として方向性は OK だったが、素材品質・キャラ位置・建物レイヤー分割で試行錯誤が長引いた
- その後 **テーマ大転換（DD → ポップダーク）** と **UI Toolkit カード型 Hub** へ寄せた方が、全画面の見た目統一と文言差し替えが早い
- `ShouldDrawLegacyOnGui = false` で旧 OnGUI 経路も停止済み

**現行の拠点**

- `PackspireUiFoundation.Hub.cs` の `BuildHub()` → **`ps-hub-home` カード型 UI**
- 背景: `Art/UI/PopDark/hub-bg-v1`
- 施設: 横レールのカードボタン（`HubFacilityCatalog.HubCards()`）
- キャラ: 左に **固定ショーケース肖像**（選択ロスターではなく概念 courier）

### 3.2 生かしているもの（探索マップ）

**探索マップの 2.5D / ハイブリッド描画は現役。凍結していない。**

| 項目 | 内容 |
|---|---|
| `ExplorationMapStage.cs` | ワールド空間で斜め／Oblique 描画 → **RenderTexture** |
| `PackspireUiFoundation.ExplorationMap.cs` | RT を UI Toolkit に重ね、HUD・操作 |
| `RoutePresentationMode` | `None` / **`RiteDebug` のみ**（旧ルート表示モードは削除） |
| 方針 | **地図本体＝ワールド、磁針・靄イベント等＝UITK**（ハイブリッド B） |

188 セル試作マップ、地区ロック、サブマップ出入り、駒移動などはこの経路で動く想定。

### 3.3 将来再開する場合

`NEXT_STEPS.md` 第3段階「2.5D拠点を本番構造へ整える」は **長期案として温存**。

再開するなら:

1. Git 履歴 / ブランチ `cursor/hub-2-5d-presentation-5837` から Stage 実装を参照
2. **PopDark 素材と新語彙** に合わせて建物・背景を作り直す（DD Hub 素材は流用しない）
3. カード Hub と **併存させない** — どちらか一方を本番に固定

---

## 4. 現行デザイン指針

### 4.1 レイヤー構造

```
[表層] 未確定の世界語彙（梱包・記録・劇場…候補から1つ選ぶ）
   ↓ プレイヤーが見る
[演出] ポップダーク UI ＋ 通常頭身・太線キャラ
   ↓ 開発者だけ知る
[裏]   神曲構造（漏斗層・contrapasso・~100 エントリ）
```

### 4.2 やること / やらないこと

| ✅ やる | ❌ やらない |
|---|---|
| ポップダーク（紫インディゴ地、クリーム金文字、コーラル eyebrow） | Bloodborne 直引用（血瓶、教会、ゴシック聖遺箱） |
| 通常頭身（7〜8 頭）＋太線 |  chibi 化 |
| 梱包・塔・未登録荷・記録室など **中立語彙** | 神曲用語の表出（漏斗・罪・七層など） |
| 回復 lore: 「何かを潰して澱を飲む → 存在が刻まれる」 | UI に罪ゲージを出す |
| UI Toolkit 一本化 | 旧 OnGUI 本番復帰 |

### 4.3 キャラアート

- **第一概念画像を正式採用**（courier / `hero-courier-hub-v1`）
- 参照: `assets/packspire-disgaea-lines-normal-proportions.png`（リポジトリルート付近）
- Hub はこの固定肖像。ロスター側は旧 DD 肖像が残っている可能性あり → 順次差し替え

### 4.4 Hub 文言（暫定・実装済み）

| 旧 | 新 |
|---|---|
| THE BLACK RELIQUARY / 黒き聖遺箱 | **STRATA REGISTRY / 塔の記録室** |
| GATE / FORGE / CODEX / GUILD | **STRATA / PACK / INDEX / RANK**（eyebrow） |

フレーバー: 「未登録の荷は、塔の影を濃くする。」

### 4.5 色板（USS 実装値の目安）

- 背景: `rgb(22–26, 16–20, 48–56)` 系インディゴ
- 本文: `rgb(255, 228, 180)` クリーム金
- Eyebrow / アクセント: `rgb(255, 140, 120)` コーラル
- 旧 `ChromeDD/btn-plate-*` → **`PopDark/btn-*`** に置換済み（Hub / Archive / Rite / Book / Tabletop / Roster / Battle 幅広ボタン）

---

## 5. 主要ファイルマップ

```
unity/PackspireUnity/Assets/Scripts/UI/
  PackspireUiFoundation.Hub.cs          ← 現行拠点（カード型）
  PackspireUiFoundation.Shared.cs     ← HubBackgroundArt, 肖像ロード
  PackspireUiFoundation.MetaScreens.cs ← Vault / Compendium
  PackspireUiFoundation.PreparationScreens.cs ← Pack / Expedition
  PackspireUiFoundation.ExplorationMap.cs ← 探索（2.5D 現役）
  ExplorationMapStage.cs              ← RT 描画ステージ
  PackspireUiFoundation.CharacterRoster.cs
  PackspireUiFoundation.Battle.cs
  PackspireUiFoundation.Router.cs     ← 画面切替

unity/PackspireUnity/Assets/Scripts/Core/
  HubFacilityCatalog.cs               ← 施設定義・eyebrow
  CharacterCatalog.cs

unity/PackspireUnity/Assets/Resources/
  UI/PackspireTheme.uss               ← 全画面スタイル（PopDark ブロック末尾付近）
  Art/UI/PopDark/                     ← hub-bg, btn-*, plaque, portrait-frame
  Art/Portraits/PopDark/hero-courier-hub-v1.png

PackspireGame.cs                      ← ShouldDrawLegacyOnGui = false
RoutePresentationMode.cs              ← RiteDebug のみ
```

---

## 6. 既知の不具合・未完了

### 6.1 Hub キャラが見えない（チェッカーボード）

- **症状**: 肖像フレーム内がチェッカー、キャラ不可視
- **原因**: `portrait-frame-v1.png` の中央に **偽透明（チェッカー焼き込み）** の可能性が高い
- **次の一手**（いずれか）:
  1. フレーム PNG を **真の alpha 透過** で作り直す
  2. フレーム PNG をやめ **CSS 枠のみ** にする
  3. キャラ＋枠を **1 枚に合成** したアセットにする

### 6.2 テーマ作業の未完了（plan todos）

| ID | 内容 | 状態 |
|---|---|---|
| pick-surface-skin | 表層世界観・語彙を1案確定 | **未** |
| define-motifs | 世界観1文・3モチーフ・色板確定 | **未** |
| disguise-bible | 神曲→表層変換表 | 表層確定後 |
| sin-mechanic-lore | 回復フレーバー名称 | **未** |
| hub-reskin | Hub 名称・背景・銘板 | **一部済**（文言・PopDark bg） |
| facility-relabel | 施設ラベル全面 | **一部済**（eyebrow のみ） |
| screen-rollout | 全画面 PopDark 統一 | **USS 一括済、要 Visual QA** |

### 6.3 Git / Unity

- Hub・テーマ変更は **未コミット** の可能性大（Library 生成物はコミットしない）
- Unity Cloud の `Token Exchange failed` は **ローカル Play には通常無関係**

---

## 7. 確認手順（Visual QA）

Play → **F10** で順に確認:

1. **Hub** — 背景 PopDark、STRATA REGISTRY、施設カード、肖像（要修正）
2. **Pack** — 術式盤・PopDark 背景
3. **Vault / Compendium** — Book シェル・色板
4. **Expedition** — 準備画面
5. **Map** — ExplorationMapStage の RT 表示・駒移動
6. **Battle** — 幅広ボタン PopDark
7. **Character** — ロスター（旧肖像が残っていないか）

---

## 8. 次にやるとよいこと（優先度順）

1. **Hub 肖像表示の修正**（フレーム PNG かレイアウト）
2. **表層スキン1案の確定**（plan 候補 A〜F からユーザー判断）
3. 確定後: **disguise-bible**（神曲→表層対応表）と残画面の文言統一
4. ロスター肖像を **courier 基準** に順次差し替え
5. Visual QA 後、ユーザー指示があれば **コミット**
6. （任意）レガシー `.ps-v2-*` CSS と未使用 `Art/Hub/` の整理
7. （将来）2.5D 街 Hub 再開するか、`NEXT_STEPS.md` を現行方針に合わせて改訂

---

## 9. 参照ドキュメント

| ファイル | 用途 |
|---|---|
| `README.md` | ゲーム全体 |
| `GAME_DATA.md` | データ・現仕様 |
| `NEXT_STEPS.md` | 旧〜長期実装順（**拠点2.5Dは長期案**） |
| `UNIMPLEMENTED_IDEAS.md` | 保留アイデア |
| `.cursor/plans/全体テーマ案_dd914710.plan.md` | テーマ確定事項・候補 |

---

## 10. 用語クイックリファレンス

| 用語 | 意味 |
|---|---|
| PopDark | 新 UI テーマ（紫暗＋彩度、ChromeDD 後継） |
| STRATA | 層・塔の階層（表層語彙の暫定。神曲の漏斗とは表で結びつけない） |
| RiteDebug | 探索マップの現行表示モード |
| 凍結 | コードから切り離し、再開は明示判断まで着手しない |
| カード Hub | 現行 `BuildHub()` — 2.5D 街ではない |
