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
| キャラクター | Character | PackspireCharacterView.uxml | PackspireCharacter.uss＋旧共通6層 | 移行途中 | 専用構造はあるが旧管理系・装飾系の積層が残る。 |
| 荷造り | Pack | PackspirePackingView.uxml | PackspirePacking.uss＋旧共通4層 | 移行途中 | 専用画面はあるが共通旧層への依存が大きい。 |
| 商店 | Shop | PackspireShopView.uxml | PackspireCommerce.uss | 移行途中 | Reward／GameOver／GameClearと見た目を共有し、拠点固有の主役が弱い。 |
| 遠征準備 | Expedition | PackspireExpeditionView.uxml | Route＋ManagementV3等 | 移行途中 | 専用USSがなく、複数世代の共通表現を組み合わせている。 |
| 役職記録 | Status | PackspireStatusView.uxml | PackspireStatus.uss | 現行 | 人物・役職一覧・詳細・任命状態を1280×720内へ再配置し、旧ManagementV3／status-v2依存を外した。 |
| 家宝 | Heirloom | PackspireHeirloomView.uxml | PackspireHeirloom.uss | 現行 | 選択家宝を主役に、来歴・成長記録・継承系統を1280×720へ再配置し、旧ManagementV3依存を外した。 |
| 勢力 | Faction | PackspireFactionView.uxml | ManagementV3等 | 旧式 | 専用USSがなく、管理画面共通表現へ依存する。 |

## 実装順

1. **役職記録（完了）**: 専用UXML／USS、一覧→詳細の選択連動、現役職／任命候補の状態を実画面確認済み。
2. **家宝（完了）**: 選択家宝、成長記録、選択モーダルを実画面確認済み。
3. **勢力**: 街路案内から到達する旧式ページ。関係値と所属を主役にする。
4. **キャラクター**: 専用構造を保ち、旧共通USS依存を外す。
5. **荷造り**: 現行遠征導線との連続性を確認しながら専用USSへ寄せる。
6. **商店**: ラン中Reward／Resultへ影響しない拠点固有セレクタへ分離する。
7. **遠征準備**: シームレス遠征開始画面との役割重複を整理してから現行化する。

次の実装対象は**勢力**とする。

## 一画面ごとの完了条件

- UXMLにシェル、一覧、詳細、固定状態があり、C#はデータと操作だけを担当する。
- 画面固有USSだけで主な配置・背景・余白・文字階層が説明できる。
- ホームへ戻った時に密度・色・主役の強さが別世代へ飛んで見えない。
- 空、選択、ロック、最大値などの動的状態を確認する。
- 1280×720論理座標と代表解像度で撮影比較し、旧セレクタと未参照素材を整理する。
