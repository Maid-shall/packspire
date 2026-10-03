# Vault references

## Selected direction

`vault-ui-target-v2-pop-dark.png` は保管庫の現行実装目標。

- 採用: ポップダークの配色、セル画調、左棚と右詳細の密度、属性ピップ、1パターン占有形状、カードと効果欄の配置
- 実装素材: `VaultCodexV3`、`VaultCodexV4`、`VaultCodexV5`、共通戦闘カード
- 非採用: 生成画像内の文字、生成された枠の直接切り出し、過剰な髑髏・鎖・角の追加
- 論理座標と素材対応の正本: `docs/ui/specs/vault-fixed-layout.md`

`vault-ui-target-v1.png` と `vault-ui-target-v3-restrained-pop-dark.png` は比較案として保持する。

## Rejected

`rejected-overdecorated.png` は実装目標ではありません。

反面教師として残している問題:

- ホーム画面より装飾密度が高い。
- 枠、紋章、発光色が同じ強度で競合する。
- 完成部品と追加アイコンを重ねている。
- 複数の素材世代が一画面で混在する。
- 主役装備を透明切り抜きではなく四角い画像として扱っている。
- 1280x720の論理解像度を前提にした文字・寸法設計が不足している。

次の参考案は、ホーム画面との比較と品質ゲートを通した後に別名で追加する。
