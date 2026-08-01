# Simplified Mature Pop-Dark Direction

PACKSPIREの全画面と量産アートを検討するための構図見本です。
現行の簡略版ポップダークを共通基準とし、次を固定しています。

- 背景と通常パネルは大きな黒面と少数の光で構成する。
- キャラクター、装備、敵は太い外形と2段階のセル影で描く。
- 赤は危険・戦闘・選択、シアンは探索・情報、金は固定構造に使う。
- 装飾画像は主役、カード、選択枠、主要コマンドに限定する。
- 通常行、説明、区切りは余白と細い罫線を中心にする。
- キャラクターは成人の7～7.5頭身とし、衣装は4～5個の大きな面で作る。
- 装備は3種類以下の素材面と一つの属性色で作る。
- 敵は一つの主シルエットと一つの行動記号を持たせる。

## Screen set

`full-screen-set-v1`は現行`ScreenId`の16画面に対応します。

1. Character
2. Hub
3. Status
4. Vault
5. Heirloom
6. Faction
7. Expedition
8. Pack
9. GridBoard
10. Battle
11. Reward
12. Shop
13. Event
14. Compendium
15. GameOver
16. GameClear

## Production sheets

`production-sheets-v1`には次の量産基準を置きます。

- `01-characters.png`: キャラクター頭身、色分け、立ち絵と顔の密度
- `02-equipment.png`: 装備の輪郭、素材面、属性記号
- `03-enemies.png`: 敵のシルエット、体格差、行動記号

## Usage boundary

これらは構図とアートディレクションの見本であり、完成UI画像ではありません。
生成画像内の文字、数値、細かな枠形状、個別の操作仕様を実装へ写しません。

実装前に画面ごとの素材対応表を作り、背景、切り抜きアート、完成部品、
9-slice枠、アイコンへ分解します。固定構造はUXML、見た目はUSS、
データと操作はC#へ置きます。
