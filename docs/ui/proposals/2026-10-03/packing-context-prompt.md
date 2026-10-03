# 荷造り — 下＋右の修正版生成指示

内蔵imagegenによる画像編集。packing-workspace-overview.pngを編集対象、現行ホームをブランド参照として使用。
用途は構図案。部品素材やゲームルールの正本ではない。

## 選択装備の重複画像を除去する局所訂正

Use case: precise-object-edit.
Image1 edit target: PACKSPIRE PC packing proposal.
Change ONLY the TOP contents of RIGHT contextual panel, below heading '選択装備' and above heading '関連するLINK' (approx x1340..1645 y190..376 of1672x941image). Every other pixel region must remain unchanged: exact large6x4board, all equipment positions/jewels/footprints, inventoryfourcolumns, header, footer, bottomresults, rightLINKrows and toggle. Do NOT change board count, size, ratio, colors, or overall layout.
Problem: top right currently repeats the stamp artwork TWICE in rectangles; those are unnecessary. Remove both framed stamp illustrations AND the frame around this top section. Replace with calm TEXT and a simple small occupancy glyph, NOT a new image preview or big icon.
At top, large ivory name '裁決の手印'. Beneath, small muted line '武器 / 配置中'. Then label '占有形状' with EXACT TWO equally sized small square cells stacked vertically, each square filled with a restrained muted-red dot indicating the item's fire color. These are a tiny utilitarian2-cell shape diagram, not stamp paintings. Use quiet thin gold outline and darkfill for eachsquare, not cyanportraitframes. Space diagonally aside for concise currentorientation '0°' and small existing button '回転 90°'. No duplicate weapon art anywhere in that topcontext area. Two-square silhouette sufficiently readable, around20pixels per square onthe1672x941canvas. Leave comfortable blank area between text andshape/control; no newartwork to fill it.
Keep right two adjacent-effect rows exactly as input. Keep original selected stamp in board and inventory only. No other changes, no character. The final should maintain clear PC mouse-driven working hierarchy: large board, large equipmentlist, specific right context, full-loadout bottom results. Single full-page image, notcrop.

Use case: precise-object-edit.
Asset type: ONE whole-page PC game UI layout proposal, PACKSPIRE packing/loadout.
Input image1: EDIT TARGET, the user-accepted normal packing concept. Image2: EXISTING HOME brand reference ONLY. Absolutely no characters or portraits.
Primary request: revise ONLY the right workspace to use BOTH bottom and right areas without shrinking the board, while preserving the left equipment inventory and brand.
Keep exact inventory layout, FOUR columns, search/category controls, same width, tile sizes and visible rows. Keep top header, charcoal texture, warm ivory Japanese typography, slender brass rules, restrained cyan states, vermilion selection. Never make inventory narrower.
BOARD: retain the SAME large per-cell size as image1. Do NOT turn it into a thumbnail. Translate the board to the LEFT within the right workspace, using previously vacant stage margin. Preserve EXACT SIX columns and FOUR rows, twenty-four perfectly SQUARE cells. The rectangular board is 3:2 aspect ratio. It occupies about60% of the right working region's width; contextual sidebar uses remaining30–35%, gutter. If image width1672, interior board around x685..1225 and y168..528, six90px-squarecolumns andfour90px-squarerows; use proportional positions if output dimensions differ. Thin modular slate-brass border, tangible red/blue/green/amber JEWEL attributes on each cell. Preserve worldbuilding and readable gemstone sockets. No giant extra ornament or magic circle.
ONE coherent object illustration per equipment footprint, no per-cell repetition. Positions expressed as1-based row/column: stamp vertical2cells r1–2c1; sealed ledger horizontal2cells r1c2–3, directly ADJACENT to stamp; pin-case singlecell r2c2, adjacent to stamp; crystal vertical2cells r2–3c3; coffin vertical2cells r1–2c6; shield horizontal2cells r3c5–6; ribbon horizontal2cells r4c2–3; small flask singlecell r4c6. Other cells EMPTY. This arrangement must preserve square cell geometry; draw cell subdivisions subtly even within occupied shapes. Stamp selected in cyan, highlight only stamp/ledger common boundary and related matched jewels. Do not stretch individual squares to fit objects.
RIGHT CONTEXT: put a quiet charcoal contextual panel immediately right of the large board, aligned to its top and bottom; about300px wide on the1672px reference, not skinny. NO giant repeated artwork or square image cards. Heading '選択装備'. Main name '裁決の手印', large readable. Beneath, exact TWO-square vertical occupancy silhouette; '配置中' and permitted orientation control '回転 90°'. Two short readable effect sections:
'関連するLINK'
'死信綴りと隣接'
'手印の攻撃 +1'
'綴りの防御 +2'
then '宛先杭箱と隣接'
'手印の札が変化'
Use thin separators and small target locators. Lower quiet toggle '盤上で関連箇所を表示'. The panel is item context, NOT entire loadout stats. No character role, portrait, HP, unrelated totals. No durability values invented. No hover text covering board.
Remove duplicate full-width selected-item strip under board. Put compact viewcontrols '− 100% ＋ 全体表示' immediately below board, SMALL. Short legend '盤の属性 ○ / 装備の属性 ◆'. Context panel includes the rotation control so do not duplicate that under board.
BOTTOM GLOBAL RESULTS: use the freed vertical area under board and sidebar for a broad tidy result region, still enough height for practical rows. Heading '編成全体の結果'. Default two side-by-side populated groups: LEFT '隣接LINK・札変化' with three short scoped rows, RIGHT '色一致・配達印' with four elemental rows. Exact examples: '手印 × 死信綴り | 攻撃 +1・防御 +2'; '共鳴結晶 × 装備 | 対象札 コスト −1'; '手印 × 宛先杭箱 | 生成札を置換'. Color groups as in original: '火の一致 4 | 焼却印 2回'; '水の一致 2 | 冷却印 1回'; '風の一致 1 | 消音印 1回'; '地の一致 3 | 補綴印 2回'. No new effects or imaginary +25% bonuses. Small count badge or bounded scrollbar for more rows; no oversized cards. At bottom region header small '生成札' and 'すべての効果' actions. These actions DO NOT shrink board or auto-change zoom.
Preserve left inventory, saving control, title and footer from image1. Bottom and right information must be distinct, no repeated same effect lists in both panes except selected-item context intentionally showing its contribution.
Constraints: SAME visual brand as provided HOME. One full screen only, no collage, no annotations outside UI, no logo invention, no character. Do not fill empty space with ornamental shapes. No extra gameplay rules, no fake balancing values. This is composition proposal, not production assets. Board main focus, inventory usable for many items, results subordinate and readable. Board should appear at least as large as in input, not larger ornaments pretending board became larger.
