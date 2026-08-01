# ホーム画面 実装基準

採用基準は [`hub-obsidian-misprint-implementation-target-v1.png`](hub-obsidian-misprint-implementation-target-v1.png)、最上位の視覚基準は [`03-obsidian-misprint.png`](../../../concepts/art-direction-candidates-2026-07/03-obsidian-misprint.png) とする。

黒い版面、大きな明朝活字、斜めの朱紙、シアンの見当ずれ、資料部品を直置きする編集レイアウトを維持する。生成り紙を画面全体や人物背景へ広げず、完成部品へ別の枠を重ねない。

## 実装座標

| 領域 | 1280x720 論理座標 | 内容 |
|---|---:|---|
| 左ナビ | `x:18, y:16, w:268, h:690` | ブランド、6施設、街路案内 |
| 人物舞台 | `x:286, y:0, w:500, h:720` | 行先、経路、人物、登録印 |
| 右資料 | `x:788, y:18, w:474, h:442` | 通貨、設定画、装備、カード、形状、記録 |
| 本日の指令 | `x:610, y:490, w:650, h:172` | 行先と荷造り、遠征準備 |
| 索引 | `x:970, y:666, w:290, h:44` | 保管庫、図鑑 |

ナビ行はすべて `78px`。選択時も寸法を変えず、朱紙とシアンの登録印だけを切り替える。

## 専用素材キット

正本フォルダ: `Assets/Resources/Art/UI/ObsidianMisprint`

| 分類 | 素材 |
|---|---|
| 背景 | `hub-misprint-background-v1` |
| 紙 | `hub-nav-selected-paper-v1`, `hub-mission-briefing-paper-v1`, `hub-objective-lines-v1`, `hub-decor-index-ticket-v1` |
| 人物資料 | `hub-courier-study-front-v1`, `hub-courier-study-back-v1` |
| 配達装備 | `hub-parcel-v1`, `hub-satchel-v1`, `hub-order-slip-v1` |
| ナビ印章 | `hub-icon-expedition-v1`, `hub-icon-loadout-v1`, `hub-icon-vault-v1`, `hub-icon-heirloom-v1`, `hub-icon-role-v1`, `hub-icon-compendium-v1`, `hub-icon-guide-v1` |
| 登録記号 | `hub-icon-wax-seal-v1`, `hub-icon-registration-arrow-v1` |
| 版画装飾 | `hub-decor-route-v1`, `hub-decor-brush-v1`, `hub-decor-cyan-offset-v1` |

装備カードは遠征画面と同じ共有カードを使用する。人物本体は現在選択中キャラクターの透過立ち絵を使用する。これらをホーム専用の画像へ焼き込まない。

## 状態と責務

- UXML: 画面領域、固定ラベル、固定操作、3x3占有形状、街路案内モーダル。
- USS: 座標、寸法、専用素材、文字階層、hover、`is-current`、`is-locked`、`is-open`。
- C#: 現在キャラ、目的地、所持金、装備カード、占有形状、記録、件数、画面遷移。
- C#へ固定座標、固定色、背景画像、装飾用インラインスタイルを置かない。
- 全画面モックを一枚絵の背景として使わない。

正本は `PackspireHubView.uxml`、`PackspireHub.uss`、`PackspireUiFoundation.Hub.cs`。追加上書きUSSを作らない。
