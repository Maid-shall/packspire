# PACKSPIRE Unity

Web版とは独立したUnity 6プロジェクトです。Unity Hubから `unity/PackspireUnity` を開いてください。

## 現在の移植範囲

- キャラクター作成と永続保存
- 勢力別拠点、役職ステータス、勢力、保管庫、家宝、鍛錬、図鑑
- 全13装備、全カード、全19役職、全7敵、3ダンジョン、5バックパック、4勢力
- 6×4色マス、装備形状、回転、色一致、装備隣接LINK、職業基本カード
- 3つの荷造りセット、バックパック選択、装備カード最大8枚の選択デッキ
- 手札、エネルギー、攻撃、防御、回復、耐久、破損、共通状態異常、消耗品
- 6×6探索マップ、警戒・崩壊・汚染、戦闘、商人、イベント、休息、報酬、帰還、敗北
- 家宝の使用履歴・傷跡と、成功時の戦利品全回収
- PlayerPrefs JSON v15による永続セーブとJSON入出力

メインシーンは `Assets/Scenes/Main.unity` です。画面は `PackspireGame` が生成し、ルールは `Systems` に分離しています。状態異常などWeb版と共有しやすい定義は `Assets/Resources/Packspire/content.json` から読み込みます。固定カタログも段階的にJSONへ移す前提です。

## 起動

1. Unity HubでUnity 6.3 `6000.3.18f1`を選択します。
2. このフォルダーをプロジェクトとして開きます。
3. `Assets/Scenes/Main.unity`を開き、Playを押します。

## Web / Vercel

このPCのUnity 6.3には現在Web Build Supportが入っていません。Unity Hubの該当EditorにWeb Build Supportを追加後、Unityメニューの `PACKSPIRE > Build Web` を実行します。成果物は `Builds/Web` に生成されます。

Vercelではこの成果物を静的サイトとして公開します。Web版を残す場合は `/unity/` 以下へ配置し、移行完了後にルートと入れ替えます。

## Web版とのセーブ互換

Unity版は `packspire_unity_save_v15` を使用します。旧Unity v1は自動移行しますが、現在のWeb版localStorageとは別保存です。`SaveSystem.Export / Import` を変換窓口として、正式移行時にWebセーブの取り込み処理を追加します。
