# PACKSPIRE UI Architecture

最終更新: 2026-07-30

## Ownership

UI Toolkitの責務は次の三層に固定する。

| Layer | Owns | Must not own |
|---|---|---|
| UXML View | 固定レイヤー、列、スクロール領域、名前付き差込口 | セーブデータ、選択処理、ゲーム進行 |
| USS Style | 寸法、配置、文字階層、状態別の見た目、画像部品 | 画面生成、ゲーム状態の分岐 |
| C# Presenter | データ取得、一覧生成、イベント、状態更新、動的演出 | 固定画面の列や枠の組み立て、固定色・固定寸法 |

`PackspireUiFoundation.ViewResources.cs` がUXML読込と必須差込口の検証を担当する。
`PackspireUiFoundation.Router.cs` は遷移だけを担当し、画面参照の破棄は
`PackspireUiFoundation.ScreenCleanup.cs` が担当する。

## Resource Scope

- document rootへ付けるUSSは`PackspireTheme.uss`だけにする。
- 画面用USSは`ApplyScreenStyleSheets`が現在画面の`screenRoot`へ付ける。
- 画面を離れる際に前画面のUSSを外す。
- 画面固有USSを別画面の補修目的で読み込まない。
- UXMLから参照する要素には意味のある一意な`name`を付ける。
- C#は`RequireViewElement<T>`で取得し、欠落を黙って無視しない。

## Screen Views

| Screen | UXML | Presenter |
|---|---|---|
| Character | `PackspireCharacterView.uxml` | `CharacterRoster.cs` |
| Hub | `PackspireHubView.uxml` | `Hub.cs` |
| Status | `PackspireStatusView.uxml` | `MetaScreens.cs` |
| Vault | `PackspireVaultView.uxml` | `VaultFixedView.cs` |
| Compendium | `PackspireCompendiumView.uxml` | `MetaScreens.cs`, `CompendiumPresentation.cs` |
| Heirloom | `PackspireHeirloomView.uxml` | `HeirloomScreen.cs` |
| Faction | `PackspireFactionView.uxml` | `FactionScreen.cs` |
| Expedition | `PackspireExpeditionView.uxml` | `ExpeditionScreen.cs` |
| Pack | `PackspirePackingView.uxml` | `PreparationScreens*.cs` |
| GridBoard | `PackspireGridBoardView.uxml` | `GridBoard*.cs` |
| Battle | `PackspireBattleView.uxml` | `Battle.cs` |
| Reward | `PackspireRewardView.uxml` | `RewardScreen.cs` |
| Shop | `PackspireShopView.uxml` | `ShopScreen.cs` |
| Event | `PackspireEventView.uxml` | `RunScreens.cs` |
| Result | `PackspireResultView.uxml` | `RunResultScreen.cs` |

盤面セル、カード、在庫行、選択候補など個数が状態で変わる要素はC#生成でよい。
その親となる固定領域はUXMLに置く。

## Change Procedure

1. 対象画面のUXMLで構造と差込口を確認する。
2. 対象画面のUSSだけを変更する。
3. Presenterではデータ投入とイベントだけを変更する。
4. 新しい固定`Container(...)`を作る前にUXMLへ置けない理由を確認する。
5. 固定の幅、色、余白をC#へ書かない。動的座標、進捗率、回転だけを例外とする。
6. XML妥当性、必須name、C#コンパイル、画面遷移を検証する。

## Prohibited

- rootへ全USSを一括登録する。
- 別画面の見た目を直すためグローバルセレクタを追加する。
- UXML欠落時に無関係な画面レイアウトへ黙ってフォールバックする。
- `V2`、`V3`、`Final2`のような上書き用ファイルを増やす。
- 固定レイアウトをC#の`Container`とインラインstyleだけで完成させる。
