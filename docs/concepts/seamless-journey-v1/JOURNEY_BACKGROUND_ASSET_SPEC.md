# シームレス遠征・背景素材規格

役割: 背景素材の書き出し・命名・Unity登録・Catalog設定の正本。美術構図と接地線の
制作契約は[`JOURNEY_BACKGROUND_ART_SPEC.md`](JOURNEY_BACKGROUND_ART_SPEC.md)を参照する。

最終更新: 2026-08-14

## 目的

新しい背景素材を追加した際の縮尺差、Y位置ずれ、1pxの隙間、
光源方向の反転を防ぐ。既存試作素材を一括変換する規格ではなく、
今後作成する正本素材へ適用する。

## 正本キャンバス

- 画像サイズ: 1920x1080 px
- アスペクト比: 16:9
- PPU: 100
- Pivot: Center `(0.5, 0.5)`
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Filter Mode: Bilinear
- Wrap Mode: Clamp
- Mesh Type: Full Rect
- Max Size: 2048以上
- Color Space: sRGB
- 透過レイヤー: PNG RGBA、Alpha Is Transparency有効
- Sky: PNG RGBまたはRGBA。画面全域を不透明に覆う

`JourneyWalkCyclePrototype` はSprite全体をカメラへ合わせて拡縮する。
同じバイオーム・道幅の各レイヤーは透明余白を含む同一キャンバスで書き出し、
個別画像だけを内容物へ合わせてタイトに切り抜かない。

## 座標と接地基準

- 全レイヤーの原点はキャンバス中央。
- Ground上面、StreetBack下端、Mid下端は、同じマスター構図から書き出す。
- 接地位置の微調整は `RoadProfile` 単位で行う。
- 個別素材ごとの補正値を増やして位置ずれを隠さない。
- Wide、Standard、Narrowで意図的にパースを変える場合は別Spriteにする。

## レイヤー別仕様

| Layer | Alpha | 反復 | 左右反転 | 備考 |
|---|---:|---|---:|---|
| Sky Day/Dusk/Night | 不透明 | 固定 | 不可 | 3枚の雲・地平線位置を揃える |
| Far A/B | 透過 | A/B | 可 | 文字、看板、片側光源を描かない |
| Mid A/B | 透過 | A/B | 不可 | 建築の影と窓光の方向を統一 |
| StreetBack A/B | 透過 | A/B | 不可 | 道具、標識、配管の向きを保持 |
| Ground | 透過 | 単独タイル | 不可 | 左右端を完全シームレスにする |
| Ground Accent | 透過 | 動的小物 | 可否を個別指定 | 水溜まり、亀裂、排水口など |
| Roadside Props | 透過 | 動的小物 | 可否を個別指定 | 接地Pivotを揃える |
| Close Foreground | 透過 | 動的小物 | 可否を個別指定 | キャラを隠す幅を抑える |

## 継ぎ目条件

- Far/Mid/StreetBackのA右端はB左端へ繋がる。
- B右端はA左端へ繋がる。
- Groundの右端は同じGroundの左端へ繋がる。
- 端から8px以内へ強い縦線、塔、窓、発光点を置かない。
- 半透明ピクセルのRGBを黒で塗り潰さず、元の色を維持する。
- Unity実機で37.5ワールド単位移動後にも隙間がないことを確認する。

## 光源規格

- バイオーム内の主光源方向を全レイヤーで統一する。
- Mid、StreetBack、Groundをプログラムで左右反転しない。
- 昼夕夜の差は空、Tint、局所照明で作り、建築の主影方向を逆転させない。
- A/B差分でも窓光、ランタン、反射の方向を揃える。

## 命名

```text
{biome}-sky-{day|dusk|night}-v1.png
{biome}-far-{a|b}-v1.png
{biome}-mid-{wide|standard|narrow}-{a|b}-v1.png
{biome}-streetback-{wide|standard|narrow}-{a|b}-v1.png
{biome}-ground-{wide|standard|narrow}-v1.png
{biome}-ground-accent-{kind}-v1.png
{biome}-roadside-props-v1.png
```

バイオーム名は `ash`、`drowned`、`black-bell` を使用する。
`Final2`、`V3`のような用途不明の末尾を増やさず、旧版を置き換える場合だけ
明示的なバージョン番号を上げる。

## Unityへの登録

本番の背景参照と接地オフセットは、以下のCatalogを正本とする。

```text
Assets/Resources/Data/Journey/JourneyPresentationCatalog.asset
```

- `Biome` ごとにSky、Farと3種類の `RoadProfile` を保持する。
- `RoadProfile` ごとにMid A/B、StreetBack A/B、Ground、各Y座標を保持する。
- 新素材の差し替えと接地調整はCatalogのInspectorで行い、ランタイムコードへ
  個別パスや座標を追加しない。
- `Tools/Packspire/Rebuild Journey Presentation Catalog` は現在の試作構成へ
  Catalog全体を戻すための再生成メニューである。手作業の調整後には不用意に実行しない。
- Catalogが読めない場合だけ、`JourneyPresentationConfig` の既存素材フォールバックを使う。
- 新素材を登録した後は、開発者メニューの9種類（3 Biome × 3 RoadProfile）を確認する。

現時点の水没書庫・黒鐘区画は、専用素材が未制作のSky、StreetBack、Groundを
灰市外縁と共有している。これはCatalog上でも共有参照として明示し、専用素材の追加時に
Standardから順番に置き換える。

## 量産順序

### Gate 1: Standard構図の美術確定

各バイオームにつき以下を作る。

- Sky Day/Dusk/Night
- Far A/B
- Mid Standard A/B
- StreetBack Standard A/B
- Ground Standard

Standardの実画面が承認されるまでWide/Narrowを量産しない。

### Gate 2: 道幅差分

- Mid Wide/Narrow A/B
- StreetBack Wide/Narrow A/B
- Ground Wide/Narrow

### Gate 3: 小物と環境差分

- Ground Accent
- Roadside Props
- Close Foreground
- 天候用SpriteまたはParticle素材

## 既存試作素材の扱い

1672x941、1774x887などの既存素材はLegacyとしてそのまま利用する。
新規量産素材と同じフォルダへ無計画に混在させず、置換時に1920x1080へ揃える。
既存素材の寸法差を理由に正本規格を変更しない。
