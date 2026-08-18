# シームレス遠征：描画基盤メモ

更新日: 2026-08-14

## 採用方式

遠征の移動表現は、画面内の配達人を基準位置へ置き、背景・路側物・近接前景を異なる係数で左へ送る多層パララックスを正本とする。

背景と小物が個別に `Time.deltaTime` から移動量を推定してはならない。`JourneyWalkCyclePrototype` がそのフレームで実際に進んだワールド距離を一度だけ計算し、`WorldAdvanced` で購読者へ配る。

```text
JourneyWalkCyclePrototype
  ├─ 背景レイヤーを Advance(distance)
  └─ WorldAdvanced(distance)
       └─ JourneySceneryController
            ├─ 路側物を distance × 1.08
            └─ 近接前景を distance × 1.88
```

これにより一時停止、倍速、ミニゲーム中の低速移動が同じ距離ソースへ揃う。

## 責務

- `JourneyWalkCyclePrototype`
  - 歩行コマと補助モーション
  - 空、遠景、道路レイヤーの構築とスクロール
  - 時間帯・バイオーム表示
  - 実移動距離の通知
- `JourneySceneryController`
  - 路側物と近接前景の生成・破棄
  - ランドマーク通過
  - 道路プロファイル変更時の接地位置更新
- `JourneyPresentationConfig`
  - 描画レイヤー名
  - 道路ごとの素材とYアンカー
  - 基準移動速度
- `JourneyTravelGameplayPrototype`
  - 遠征フェーズ、分岐、イベント、ミニゲーム、戦闘、UI連携
  - 背景や小物のスクロール量は計算しない
  - ミニゲーム処理は `.MiniGames.cs`、戦闘処理は `.Battle.cs` に分け、基幹進行ファイルへ戻さない

## 描画レイヤー

下から順に以下を使う。

1. `Journey Background`
2. `Journey Roadside`
3. `Journey Actors`
4. `Journey Foreground`
5. `Journey Effects`

配達人本体と影は `SortingGroup` で一体化する。路側物を人物より後ろ、近接前景を人物より前に描く。今後、上下移動できる複数レーンを導入するまではY軸ソートを使わない。

## 道路プロファイル

`Standard`、`Wide`、`Narrow` は `JourneyPresentationConfig.GetRoad` から、背景素材と以下の接地情報を一緒に取得する。

- 中景Y
- 後景道路Y
- 地面Y
- 路側物Y
- 近接前景Y
- ランドマークY

地面だけを動かして小物のYを直書きのまま残してはならない。新しい道路を追加する場合も1つの定義へ全アンカーを登録する。

道路レイヤーは起動時に全種類を生成せず、最初は現在のプロファイルだけを生成する。別プロファイルは初回選択時に生成して再利用する。

## 画像インポート

- 完成背景: `Sprite / Single`、Full Rect、Mip Mapなし
- 小物・ランドマーク・歩行シート: `Sprite / Multiple`、専用Pivot、Mip Mapなし
- 実行時の `Sprite.Create` は、影・単色ベール・仮の小包などプログラム生成画像だけに限定する

完成PNGを実行時に再スライスしない。Pivot修正はインポート設定側で行う。

## 現段階で入れないもの

- 小物は同時に最大2個程度のため、Object Poolはまだ導入しない
- 4分割済みの小物は元から同一テクスチャなので、追加のSprite Atlas化は行わない
- バイオーム数が少ない試作中はResourcesを維持する。数が増え、実メモリ計測で問題が出た時点でAddressablesへ移す
- 絵画的な全幅背景を維持するため、TilemapやSpriteShapeへの置換は行わない

## 追加時の確認項目

- 停止中に背景・路側物・近接前景が動かない
- 倍速時も各レイヤーの距離比が変わらない
- 分岐・イベント・戦闘では近接前景が残らない
- 道路切替時に小物とランドマークが新しい接地Yへ追従する
- 背景に継ぎ目や未描画領域が出ない
- Unity ConsoleのError / Warningが0件
