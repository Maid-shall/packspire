# 拠点ページ現行化監査

更新日: 2026-08-26

## 判定基準

- **現行**: ホームの密度・文字階層・余白を基準にした正本UXMLと画面固有USSがあり、旧共通上書きへ見た目を依存しない。
- **移行途中**: 正本UXMLまたは専用USSはあるが、旧共通USSの積層や他画面との共有スタイルが主な見た目を所有する。
- **旧式**: 正本UXMLはあっても画面固有USSがなく、ManagementV3、status-v2など旧世代の共通表現が主役になっている。

Unityの同期読出しとPanelSettings RenderTextureは黒画像になるが、Play Modeで画面遷移を完了した次フレームに`ScreenCapture.CaptureScreenshot`を予約する経路ではUIを含む1280×720画像を取得できる。以後はこの経路で一画面ずつ外観判定する。

## ホームから到達するページ

| 施設 | ScreenId | 正本UXML | 主な見た目 | 判定 | 理由 |
|---|---|---|---|---|---|
| ホーム | Hub | PackspireHubView.uxml | PackspireHub.uss | 現行・基準 | 現行プロダクト表現の基準。 |
| 保管庫 | Vault | PackspireVaultView.uxml | PackspireVaultView.uss | 現行 | UXMLが画面固有USSを直接所有する正本。 |
| 図鑑 | Compendium | PackspireCompendiumView.uxml | PackspireVaultCodexFinal.uss | 現行 | リポジトリ規則で正本が固定され、一覧・詳細の専用表現がある。 |
| キャラクター | Character | PackspireCharacterView.uxml | PackspireCharacter.uss | 現行 | 既存の配達人一覧・人物画・人物記録の構図を維持し、旧Roster／MisprintCommon依存を外した。 |
| 荷造り | Pack | PackspirePackingView.uxml | PackspirePacking.uss | 現行 | 6×4配置盤・所持品・3編成の完成構図を維持し、旧ManagementV3／MisprintCommon依存を外した。 |
| 商店 | Shop | PackspireShopView.uxml | PackspireShop.uss | 現行 | 商人・6商品の在庫・商品記録・購入伝票を専用化し、Reward／ResultとのUSS共有を解消した。 |
| 遠征準備 | Expedition | PackspireExpeditionView.uxml | PackspireExpedition.uss | 現行 | 行先・配達人・荷造りの出発許可画面として専用化し、Route／Management／Meta／ManagementV3依存を外した。 |
| 役職記録 | Status | PackspireStatusView.uxml | PackspireStatus.uss | 現行 | 人物・役職一覧・詳細・任命状態を1280×720内へ再配置し、旧ManagementV3／status-v2依存を外した。 |
| 家宝 | Heirloom | PackspireHeirloomView.uxml | PackspireHeirloom.uss | 現行 | 選択家宝を主役に、来歴・成長記録・継承系統を1280×720へ再配置し、旧ManagementV3依存を外した。 |
| 勢力 | Faction | PackspireFactionView.uxml | PackspireFaction.uss | 現行 | 使節・4勢力の台帳一覧・選択勢力の詳細を1280×720へ再配置し、旧ManagementV3依存を外した。 |

## 実装順

1. **役職記録（完了）**: 専用UXML／USS、一覧→詳細の選択連動、現役職／任命候補の状態を実画面確認済み。
2. **家宝（完了）**: 選択家宝、成長記録、選択モーダルを実画面確認済み。
3. **勢力（完了）**: 使節、4勢力の一覧、選択連動する詳細記録を実画面確認済み。
4. **キャラクター（完了）**: 既存の完成構図を保ったまま旧Roster／MisprintCommon依存を外し、実画面比較済み。
5. **荷造り（完了）**: 6×4配置盤・所持品・3編成の構図を保ったまま旧ManagementV3／MisprintCommon依存を外し、実画面比較済み。
6. **商店（完了）**: 商人・6商品の在庫・商品記録・購入伝票を専用USSへ分離し、Reward画面も実画面回帰確認済み。
7. **遠征準備（完了）**: 出発前の行先・配達人・荷造り確定と再開導線に役割を限定し、専用USSへ分離して実画面比較済み。

ホームから到達する9ページの現行化監査は完了。次は遠征中HUDの左欄・下欄を情報設計から現行化する。

## 一画面ごとの完了条件

- UXMLにシェル、一覧、詳細、固定状態があり、C#はデータと操作だけを担当する。
- 画面固有USSだけで主な配置・背景・余白・文字階層が説明できる。
- ホームへ戻った時に密度・色・主役の強さが別世代へ飛んで見えない。
- 空、選択、ロック、最大値などの動的状態を確認する。
- 1280×720論理座標と代表解像度で撮影比較し、旧セレクタと未参照素材を整理する。
