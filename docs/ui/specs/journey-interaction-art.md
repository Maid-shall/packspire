# 休息・帰還祠の部品仕様

2026-10-02。ゲームルール、選択肢、回復値は変更しない。

## ホームから採るスタイル

黒い刷り汚しの情報面、温かい生成り文字、細い真鍮色の区切り、深い青緑の状態色、控えめな赤布。
人物・装備と同様、輪郭を持つ物体絵を透過配置する。発光枠や白い巨大アイコンを重ねない。
本文と選択肢を優先し、絵は状況を理解する補助に留める。

## 素材対応・許可キット

| 部品 | 種類 | 正本 | 使用領域 |
|---|---|---|---|
| ベンチと手当用品 | オブジェクト絵、1254×1254 RGBA | Assets/Resources/Art/JourneyPrototype/Complete/journey-rest-supplies.png | 休息の全面イベント左欄 |
| 帰還祠 | オブジェクト絵、1024×1536 RGBA | Assets/Resources/Art/JourneyPrototype/Complete/journey-return-shrine.png | ボス後の街路 |
| 情報面 | 背景 | ObsidianMisprint/hub-misprint-background-v1 | 既存休息パネル |
| 文・操作 | UXML/USS | PackspireJourneyCompleteView / PackspireJourneyComplete | 既存の選択・帰還・続行 |

両画像は共通部品。休息6地点の建物外観を描き分けたものではない。
既存の背景・本文・選択肢を維持する。別キットの飾り枠を追加しない。
元の封筒・中継印画像は他画面の共有素材なので削除しない。

## 座標

| 部品 | 1280×720 論理座標 | 1920×1080 実画面 |
|---|---|---|
| 休息面 | x92 y76 w1096 h572 | x138 y114 w1644 h858 |
| 左図像欄 | 36%のflex列、26px余白、scale-to-fit | 39px余白、比率維持 |
| 祠の操作領域（到着位置） | x570 y184 w270 h455 | x855 y276 w405 h682.5 |
| 祠の絵 | w270 h405、scale-to-fit | w405 h607.5 |
| 帰還ラベル | w178 h42、絵の下に6px余白 | w267 h63、9px余白 |
| 続行操作 | right72 y595 w154 h42 | right108 y892.5 w231 h63 |

祠の本体と操作ラベルは一つのButton。子の絵・ラベルはpicking-mode Ignore。
透明背景を保持し、hover/focusはラベルの枠と面だけに付ける。

## 戦果受領からの接近

第1・第2層ボスの戦果受領直後から街路に祠を見せる。接近距離は既存の
離脱歩行3秒×基準歩行速度1.25の3.75ワールド単位。経過時間で祠だけを動かさず、
walkerのWorldAdvancedで通知される実際の地面移動量を積算する。
Cameraの投影と画面の論理幅から動的translateを求め、到着位置はUSSに置く。

- 到着前は祠本体だけを表示。題名・帰還ラベル・続行操作は隠し、操作も無効化する。
- 祠前で減速・停止し、歩行姿勢の落着きに0.18秒を置いて選択肢を表示する。
- 一時停止・台帳・バッグの既存停止条件へ従う。倍速でも地面との移動比率は変えない。
- 続行時はラベルだけを隠し、祠本体は既存0.8秒の歩行に合わせて後ろへ流す。
- 接近中の保存境界は既存Checkpoint。再開は到着状態へ戻し、受領済み戦果を再付与しない。
- 通常戦闘後の3秒歩行と最深部クリアには接近演出を追加しない。

移動演出は新しいノード・日数コスト・回復・報酬を発生させない。

## 生成履歴

内蔵 imagegen を使用。CLI/APIへの切替なし。画像編集・背景除去なし。
生成元は Codex generated_images 内で保存し、採用PNGを上記プロジェクト内へコピー。
UnityはInput alpha、alphaIsTransparency、Clamp、Bilinear、NPOT None、mipmapなし、非圧縮で取り込む。

### 帰還祠の最終プロンプト

Use case: stylized-concept. Asset type: production 2D game world prop, single isolated return shrine on a truly transparent background. Primary request: a small roadside return device in PACKSPIRE, a ruined gothic-industrial courier world. A narrow charcoal stone pedestal with a shallow pointed roof, aged brass fittings, a single inset teal light above a sealed circular brass dispatch mechanism, a modest worn crimson ribbon. It must look like a real object standing on the street, NOT a flat symbol, UI badge, heraldic seal, round magic circle, or huge archway. Style: crisp hand-painted ink illustration, faceted stone geometry, etched edges, charcoal black, warm aged brass and ivory highlights, subdued deep teal, rough parchment-like surface detail on the object only. Front elevation, almost orthographic side-scroller camera, slight visible right side; heavy base flat on a horizontal ground line, complete silhouette, no top-down/isometric viewpoint. Restrained teal glow confined to the inset, no large bloom. Composition: one shrine only, tall 2:3 object proportion centered on portrait canvas, object fills about 90 percent of canvas height, tiny transparent safety margin, entire roof and feet visible. No surrounding scenery, no floor rectangle, no backdrop, no people, no UI, no writing, no watermark. True alpha transparency around and between object parts, no baked checkerboard.

### 休息用品の最終プロンプト

Use case: stylized-concept. Asset type: production 2D game illustration, isolated rest and equipment-organizing props on a truly transparent background. Primary request: a modest rest station for a courier in PACKSPIRE, a ruined gothic-industrial courier world. One worn charcoal wooden bench with a folded deep-teal travel cloak on its left end, an open low supply crate beside it holding neatly rolled ivory bandages, a travel satchel with aged brass buckles, and a small brass lantern hanging from a short iron hook fixed to the bench. A compact coherent arrangement, not an elaborate shop or building. Style: crisp hand-painted ink illustration, etched edges and faceted material planes, dark worn wood and iron, warm aged brass and ivory highlights, subdued deep teal cloth, one restrained crimson fabric detail. Front-facing with a little visible right side, almost orthographic side-scroller camera, no top-down or isometric viewpoint. Complete silhouette and all feet visible. Square canvas, group fills about 85 percent of canvas width and height, transparent safety margin. Warm lantern illumination localized to the objects; no broad haze or bloom. No scenery, no wall, no floor rectangle, no people, no UI frames, no writing, no logos, no watermark. True alpha transparency around and between object parts, no baked checkerboard.
