# シームレス遠征戦闘 UI仕様

更新日: 2026-08-15

## 視覚基準

- 最上位ブランド基準: 現行ホーム画面の黒革、生成紙、古金、封蝋、青緑の配達印。
- 承認済み構図: 2026-08-15にユーザーが提示した戦闘UI案。
- 主役: 手札と戦場。情報枠は人物と操作を補助し、画面外枠として競合しない。
- 静かな領域: 戦場中央と上空を空け、HUDを人物付近と画面下へ限定する。

## 座標基準

PanelSettingsは1280x720論理座標、検証キャプチャは1920x1080とする。

| 領域 | 1920x1080物理座標 | 1280x720 USS論理座標 |
|---|---:|---:|
| プレイヤーHUD | x=198, y=30, w=366, h=162 | x=132, y=20, w=244, h=108 |
| 敵HUD開始位置 | x=983, y=108 | x=655, y=72 |
| コマンド面 | y=702, h=378 | y=468, h=252 |
| 左資源ドック | x=18, bottom=8, w=351, h=188 | x=12, bottom=5, w=234, h=125 |
| 手札領域 | x=366, right=276 | x=244, right=184 |
| 右操作列 | right=15, w=261 | right=10, w=174 |

## 素材対応表

| Mock component | Production source | 分類 | Status |
|---|---|---|---|
| HP外枠 | `Art/Battle/UI/JourneyApproved/journey-vitals-parchment-v1` | 完成部品 | ready |
| 敵行動札 | `Art/Battle/UI/JourneyApproved/journey-action-badge-v1` | 完成部品 | ready |
| 防御印 | `Art/Battle/UI/battle-vitals-shield-v1` | 完成部品 | ready |
| 左資源ドック | `Art/Battle/UI/JourneyApproved/journey-resource-dock-v1` | 完成部品 | ready |
| 山札・捨札 | 左資源ドック内の動的ラベル | 動的内容 | ready |
| 戦闘ログ紙片 | `Art/Battle/UI/JourneyApproved/journey-command-strip-v1` | 完成部品 | ready |
| コマンド面・一覧面 | 無地の黒革色面＋古金境界線 | 画面固有USS | ready |
| スキル | `Art/Battle/UI/JourneyApproved/journey-skill-plaque-v1` | 完成部品 | ready |
| ターン終了 | `Art/Battle/UI/JourneyApproved/journey-end-turn-envelope-v1` | 完成部品 | ready |
| 手札 | 既存`PackspireDocketCard`と本編カードアート | 完成部品 | ready |

承認済み全画面モックを正本とし、画像編集で文字・数値を除去した専用素材を使用する。
編集元は`docs/ui/assets/journey-battle-approved/journey-battle-ui-kit-source-v1.png`へ保管する。
旧Battle UI D6素材および文字入りHP素材は、この画面の戦闘UIには混在させない。

## 動的状態

- HP、ブロック、エネルギー、山札、捨札、次行動、状態異常はC#から内容だけを更新する。
- HPは`ProgressBar`ではなく専用のtrack/fill要素を使い、C#がfill幅だけを百分率で更新する。
- ブロック0の人物には防御表現を表示しない。ブロック中は体力票全体へ青い半透明膜を重ね、右端へ青い盾印と防御値を表示する。
- 敵の次行動は黒金の行動札、防御値は青い盾印に分離し、同じ記号や同じ枠を流用しない。
- 状態異常は最大5件を印章チップとして生成し、詳細はtooltipへ表示する。
- 山札・捨札は各ボタンから一覧オーバーレイを開く。戦闘中の入力受付中のみ使用可能。
- 一覧表示中はカード、スキル、ターン終了を停止する。
- 手札は1〜10枚で共有扇配置を使い、7枚以上では重なり量を増やす。
- 3体表示は構図検査用。実戦の複数対象選択は別工程とする。

## 素材キット許可

| 領域 | Primary kit | Allowed shared assets | 禁止 |
|---|---|---|---|
| 人物HUD | JourneyApproved | NotoSerifJP、共通状態文字 | 文字焼き込みHP、旧緑黒Vitals、汎用シアン箱 |
| コマンド面 | JourneyApproved | Infernal Docketカード | 旧Battle UI D6、旧紫カード枠 |
| 状態異常 | Obsidian Misprint shared wax | ContentDatabaseの記号 | 別世代の大型アイコン枠 |
