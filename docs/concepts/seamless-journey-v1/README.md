# シームレス遠征ドキュメント索引

更新: 2026-08-18  
状態: 現行

このディレクトリの入口です。遠征について新しい判断や作業項目を追加する場合は、まず
[`JOURNEY_PRODUCT_PLAN.md`](JOURNEY_PRODUCT_PLAN.md) を更新します。日付付きの監査記録や
過去の草案を、新しい正本として増やしません。

## 現行の正本

| 文書 | 責務 |
|---|---|
| [`JOURNEY_PRODUCT_PLAN.md`](JOURNEY_PRODUCT_PLAN.md) | 遠征全体の採用方針、未決事項、優先順位、完了条件 |
| [`../../ui/specs/journey-battle-command.md`](../../ui/specs/journey-battle-command.md) | 新リアルタイム戦闘の画面・リール・入力仕様 |
| [`../../../ENEMY_ENCOUNTER_BALANCE_FRAMEWORK.md`](../../../ENEMY_ENCOUNTER_BALANCE_FRAMEWORK.md) | 敵単体、遭遇、遠征全体の難易度設計。リアルタイム化は更新予定 |
| [`JOURNEY_RENDERING_ARCHITECTURE_2026-08-14.md`](JOURNEY_RENDERING_ARCHITECTURE_2026-08-14.md) | 横スクロール、多層背景、ワールド移動量の技術責務 |
| [`JOURNEY_BACKGROUND_ART_SPEC.md`](JOURNEY_BACKGROUND_ART_SPEC.md) | 背景を制作する際の美術・構図契約 |
| [`JOURNEY_BACKGROUND_ASSET_SPEC.md`](JOURNEY_BACKGROUND_ASSET_SPEC.md) | 背景素材の書き出し、Unity登録、Catalog規格 |
| [`../../ui/specs/courier-route.md`](../../ui/specs/courier-route.md) | 航路台帳の元仕様。シームレス遠征ではオーバーレイとして再利用 |

## 履歴・参考資料

以下は判断経緯を残す資料です。現在の作業順や完成条件には使いません。

| 文書 | 扱い |
|---|---|
| `JOURNEY_GAME_DESIGN_DRAFT_2026-08-14.md` | 初期ゲーム設計草案。採用事項は全体計画へ移行済み |
| `GAP_AUDIT_2026-08-14.md` | 2026-08-14時点の不足監査。現在の不足一覧ではない |
| `JOURNEY_BATTLE_AUDIT_2026-08-15.md` | 旧戦闘試作の実装・検証記録 |
| `BACKGROUND_REVIEW_BRIEF_2026-08-14.md` | Unity AIへ送った背景レビュー依頼の記録 |
| `ANIMATION_PROTOTYPE.md` | 初期歩行プロトタイプの操作・受入記録 |
| `01-journey.png` / `02-battle.png` / `03-event.png` | 初期構図参考。実装正本ではない |

## 更新規則

- 全体方針、未決事項、優先順位は `JOURNEY_PRODUCT_PLAN.md` だけへ記録する。
- 戦闘UIの寸法・素材・表示規則は `journey-battle-command.md` だけへ記録する。
- 敵を追加する数値・遭遇ルールは `ENEMY_ENCOUNTER_BALANCE_FRAMEWORK.md` だけへ記録する。
- 背景の美術要件とUnity登録要件を混ぜない。
- 完了した作業は全体計画で完了へ移し、別の「最終版」ノートを作らない。

