# Product Art Gaps — Packspire UI

更新: 2026-07-22  
用途: コード側で仮USSのみの箇所。最終素材差し替え時のチェックリスト。

---

## Hub v3 — 施設リール

| 素材ID | 用途 | 推奨寸法 | 透明 | 9-slice | 備考 |
|---|---|---:|---|---|---|
| `hub-reel-frame-normal` | 施設リール項目の外枠 | 320×68 | 是 | 是 (12px) | 角切り六角寄り、暗い金属 |
| `hub-reel-frame-selected` | 選択時グロー枠 | 320×68 | 是 | 是 | 通常枠と同サイズ。border-width変化なし |
| `hub-reel-emblem-plate` | 紋章／アイコン底 | 40×40 | 是 | 否 | 現在は eyebrow テキストで代替 |
| `hub-reel-fade-mask` | 上下スクロールヒント | 280×24 | 是 | 否 | 現状 USS グラデ相当 |

## Hub v3 — 左下固定・街案内入口

| 素材ID | 用途 | 推奨寸法 | 透明 | 9-slice | 備考 |
|---|---|---:|---|---|---|
| `hub-street-map-folded` | 折り畳み地図／案内札 | 280×88 | 是 | 是 (8px) | 左下固定フッター。現状 Chrome + MAP 紋章 |
| `hub-street-map-hover` | ホバー発光（地図端・留め具） | 280×88 | 是 | 否 | 巨大矩形グロー禁止 |
| `hub-street-map-case` | 革製ケース（代替案） | 280×88 | 是 | 否 | 地図帳＋羽根ペン |

## Hub v3 — 街案内オーバーレイ（中央展開）

| 素材ID | 用途 | 推奨寸法 | 透明 | 9-slice | 備考 |
|---|---|---:|---|---|---|
| `hub-street-map-bg` | 将来の街地図背景 | 1600×900 | 否 | 否 | 現状はパネルUSSのみ |
| `hub-mappin-normal` | 地図ピン／一覧ピン | 280×56 | 是 | 是 | MapPin ボタン差し替え |
| `hub-mappin-locked` | 未解放ピン | 280×56 | 是 | 是 | Locked 状態 |
| `hub-category-seal-*` | カテゴリ紋章 (行動/工房/記録) | 48×48 | 是 | 否 | 任意 |

## Hub v3 — 中央ステージ

| 素材ID | 用途 | 推奨寸法 | 透明 | 9-slice | 備考 |
|---|---|---:|---|---|---|
| `hub-center-info-plate` | 施設名・説明の装飾枠 | 520×96 | 是 | 是 | 現状 USS 枠 |
| `hub-bg-theme-*` | 施設別背景差分 | 1920×1080 | 否 | 否 | themeKey ごと。未用意時は共通 `hub-bg-v1` |
| `hub-character-frame` | キャラ表示枠（任意） | 640×900 | 是 | 否 | 枠 PNG 焼き込み禁止 |

## 施設アイコン（将来）

| 素材ID | 用途 | 推奨寸法 | 透明 | 備考 |
|---|---|---:|---|---|
| `hub-icon-gate` 等 | `HubFacilityDef.iconResource` | 64×64 | 是 | 施設IDと1:1。現状 PopDark btn-card を仮参照 |

## 通知・状態

| 素材ID | 用途 | 推奨寸法 | 透明 |
|---|---|---:|---|
| `hub-notice-badge` | 新着／更新 | 20×20 | 是 |
| `hub-locked-seal` | 未解放 | 24×24 | 是 |

---

## 差し替え口（コード）

- 外枠: `.ps-hub-chrome-frame` — background-image 差し替え可
- 選択グロー: `.ps-hub-chrome-glow` —  opacity のみ。素材差し替え可
- 紋章: `.ps-hub-chrome-emblem` — `iconResource` ロード予定
- 掲示板: `.ps-hub-bulletin-board` — background-image
- テーマ背景: `.ps-hub-center-bg.ps-hub-theme-{key}` — 背景画像
- 地図座標: `HubFacilityDef.mapX / mapY` — 将来オーバーレイ配置用（現在は詳細表示のみ）
