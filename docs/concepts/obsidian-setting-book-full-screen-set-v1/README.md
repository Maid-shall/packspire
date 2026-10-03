# Obsidian Setting Book Full Screen Set V1

黒曜版画を基礎に、ゲーム全体を設定資料集として見せる非戦闘画面の
構図見本です。`10-battle.png`は戦闘表示方式が未決定のため意図的に
含めていません。

## Visual grammar

- 黒地は収蔵台、製本、画面外周に使う。
- 生成り紙は現在選択中の対象と主要操作に限定する。
- 一画面の完成カラー絵は原則一体または一品に絞る。
- 背面図、側面図、没案、分解図、背景レイアウトは線画で置く。
- 線画は雰囲気素材であり、重要な文字やクリック領域へ重ねない。
- 朱色は選択、封印、警告、没案の取り消し線に使う。
- シアンは操作反応、登録線、探索情報に使う。
- 装飾枠の反復ではなく、紙端、罫線、登録番号、封蝋で階層を作る。

## Implementation boundary

各PNGは構図見本であり、一枚絵として画面背景へ実装しません。
実装前に背景線画、紙面、完成絵、ラフ線画、印章、アイコンを素材へ
分解します。固定構造はUXML、見た目は画面固有USS、データと操作は
C#へ置きます。

## Screens

1. Character
2. Hub
3. Status / Roles
4. Vault
5. Heirloom
6. Faction
7. Expedition Preparation
8. Packing
9. Exploration Grid Board
10. Battle - deferred
11. Reward
12. Shop
13. Event
14. Compendium
15. Game Over
16. Game Clear
