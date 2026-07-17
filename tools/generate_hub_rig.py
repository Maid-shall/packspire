from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1] / "unity" / "PackspireUnity" / "Assets" / "Resources" / "Art"
CHARACTER_SOURCE = ROOT / "Prototype2_5D" / "character-v1.png"
GUILD_SOURCE = ROOT / "Hub" / "hub-guild-close-v2.png"
GATE_SOURCE = ROOT / "Hub" / "hub-gate-v1.png"
FORGE_SOURCE = ROOT / "Hub" / "hub-forge-v1.png"
OUTPUT = ROOT / "HubRig"


def keyed_character() -> Image.Image:
    image = Image.open(CHARACTER_SOURCE).convert("RGBA")
    data = np.array(image)
    # The source already contains a correct alpha channel. Re-keying black
    # would erase the black leather armor.
    matte = data[:, :, 3] == 0
    data[matte,0:3]=0
    return Image.fromarray(data, "RGBA")


def keyed_environment(path: Path) -> Image.Image:
    image = Image.open(path).convert("RGBA")
    data = np.array(image)
    rgb = data[:, :, :3].astype(np.float32) / 255.0
    competing = np.maximum(rgb[:, :, 0], rgb[:, :, 2])
    dominance = rgb[:, :, 1] - competing
    chroma_strength = np.clip((dominance - .015) / .075, 0.0, 1.0)
    chroma_strength *= np.clip((rgb[:, :, 1] - .22) / .28, 0.0, 1.0)
    data[:, :, 3] = np.uint8((1.0 - chroma_strength) * 255.0)
    edge = chroma_strength > 0.02
    green_limit=np.maximum(data[:, :, 0],data[:, :, 2]).astype(np.float32)*1.06
    data[:, :, 1] = np.where(
        edge,
        np.minimum(data[:, :, 1],green_limit),
        data[:, :, 1],
    ).astype(np.uint8)
    fully_keyed=chroma_strength > 0.98
    data[fully_keyed,0:3]=0
    return Image.fromarray(data, "RGBA")


def keyed_guild() -> Image.Image:
    return keyed_environment(GUILD_SOURCE)


def polygon_mask(size, points, blur=5):
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).polygon(points, fill=255)
    return mask.filter(ImageFilter.GaussianBlur(blur)) if blur else mask


def ellipse_mask(size, box, blur=5):
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).ellipse(box, fill=255)
    return mask.filter(ImageFilter.GaussianBlur(blur)) if blur else mask


def rectangle_mask(size, box, blur=4):
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).rectangle(box, fill=255)
    return mask.filter(ImageFilter.GaussianBlur(blur)) if blur else mask


def multiply_masks(*masks):
    values = [np.array(mask, dtype=np.float32) / 255.0 for mask in masks]
    result = np.ones_like(values[0])
    for value in values:
        result *= value
    return Image.fromarray(np.uint8(np.clip(result, 0, 1) * 255), "L")


def union_masks(*masks):
    values = [np.array(mask, dtype=np.uint8) for mask in masks]
    result = np.maximum.reduce(values)
    return Image.fromarray(result, "L")


def subtract_mask(base, cut):
    a = np.array(base, dtype=np.int16)
    b = np.array(cut, dtype=np.int16)
    return Image.fromarray(np.uint8(np.clip(a - b, 0, 255)), "L")


def color_mask(image, predicate):
    data = np.array(image.convert("RGBA"))
    result = predicate(data[:, :, :3]).astype(np.uint8) * data[:, :, 3]
    return Image.fromarray(result, "L").filter(ImageFilter.GaussianBlur(2))


def part_from(image, mask):
    output = image.copy()
    source_alpha = np.array(image.getchannel("A"), dtype=np.float32) / 255.0
    part_alpha = np.array(mask, dtype=np.float32) / 255.0
    output.putalpha(Image.fromarray(np.uint8(source_alpha * part_alpha * 255), "L"))
    return output


def inpaint_underlay(image, moving_mask, radius=22):
    # A soft neighboring-color fill is sufficient because the covering part
    # only moves a few pixels. At rest the original moving layer sits on top.
    blurred = image.filter(ImageFilter.GaussianBlur(radius))
    output = image.copy()
    output.paste(blurred, (0, 0), moving_mask)
    output.putalpha(image.getchannel("A"))
    return output


def save_parts(folder, parts):
    folder.mkdir(parents=True, exist_ok=True)
    for name, image in parts.items():
        image.save(folder / f"{name}.png", optimize=True)


def build_character():
    image = keyed_character()
    size = image.size
    w, h = size
    silhouette = image.getchannel("A")

    dark_hair_pixels = color_mask(
        image,
        lambda rgb: (
            (np.max(rgb, axis=2) < 125)
            & (np.min(rgb, axis=2) < 75)
        ),
    )
    skin_pixels = color_mask(
        image,
        lambda rgb: (
            (rgb[:, :, 0] > 70)
            & (rgb[:, :, 0].astype(np.int16) > rgb[:, :, 1].astype(np.int16) + 5)
            & (rgb[:, :, 1].astype(np.int16) > rgb[:, :, 2].astype(np.int16) - 8)
        ),
    )

    back_hair_shape = ellipse_mask(size, (w * .30, h * .03, w * .70, h * .26), 5)
    front_hair_shape = polygon_mask(
        size,
        [(w*.31,h*.04),(w*.69,h*.04),(w*.66,h*.20),(w*.56,h*.24),(w*.36,h*.22)],
        4,
    )
    ahoge_shape = polygon_mask(
        size,
        [(w*.43,h*.015),(w*.57,h*.015),(w*.59,h*.12),(w*.41,h*.12)],
        3,
    )
    face_shape = ellipse_mask(size, (w * .37, h * .09, w * .64, h * .26), 4)
    eyes_shape = rectangle_mask(size, (w * .43, h * .100, w * .61, h * .125), 1)
    left_arm_shape = polygon_mask(
        size,
        [(w*.29,h*.25),(w*.41,h*.27),(w*.36,h*.43),(w*.32,h*.61),(w*.25,h*.62),(w*.22,h*.43)],
        4,
    )
    right_arm_shape = polygon_mask(
        size,
        [(w*.59,h*.27),(w*.71,h*.25),(w*.78,h*.43),(w*.74,h*.61),(w*.67,h*.62),(w*.63,h*.43)],
        4,
    )
    cloth_shape = polygon_mask(
        size,
        [(w*.28,h*.45),(w*.72,h*.45),(w*.76,h*.72),(w*.24,h*.72)],
        7,
    )
    legs_shape = rectangle_mask(size, (w * .22, h * .61, w * .78, h * .99), 6)

    # Limit every spatial mask to actual source pixels.
    back_hair = multiply_masks(back_hair_shape, dark_hair_pixels, silhouette)
    front_hair = multiply_masks(front_hair_shape, dark_hair_pixels, silhouette)
    ahoge = multiply_masks(ahoge_shape, dark_hair_pixels, silhouette)
    face = multiply_masks(face_shape, skin_pixels, silhouette)
    eyes = multiply_masks(eyes_shape, silhouette)
    left_arm = multiply_masks(left_arm_shape, silhouette)
    right_arm = multiply_masks(right_arm_shape, silhouette)
    cloth = multiply_masks(cloth_shape, silhouette)
    cloth = subtract_mask(cloth, union_masks(left_arm, right_arm))
    legs = multiply_masks(legs_shape, silhouette)
    legs = subtract_mask(legs, union_masks(left_arm, right_arm, cloth))
    torso_region = rectangle_mask(size, (w*.22, h*.20, w*.78, h*.68), 8)
    torso = multiply_masks(torso_region, silhouette)
    torso = subtract_mask(torso, union_masks(left_arm, right_arm, face, front_hair))
    arm_sockets = union_masks(
        multiply_masks(
            left_arm,
            rectangle_mask(size, (w*.285,h*.25,w*.385,h*.59), 4),
        ),
        multiply_masks(
            right_arm,
            rectangle_mask(size, (w*.615,h*.25,w*.715,h*.59), 4),
        ),
    )
    torso = union_masks(torso, arm_sockets)
    torso_underpaint = inpaint_underlay(
        image,
        cloth,
        12,
    )
    socket_shadow = Image.new("RGBA", size, (19, 20, 22, 255))
    torso_underpaint.paste(socket_shadow, (0, 0), arm_sockets)

    # Remove eyes from the face base and softly fill the socket so the eye
    # layer can squash to a blink without leaving a duplicate pair behind.
    face_underpaint = inpaint_underlay(image, eyes, 7)
    face_without_eyes = subtract_mask(face, eyes)

    parts = {
        "character-legs": part_from(image, legs),
        "character-torso": part_from(torso_underpaint, torso),
        "character-back-hair": part_from(image, back_hair),
        "character-face": part_from(face_underpaint, face_without_eyes),
        "character-eyes": part_from(image, eyes),
        "character-front-hair": part_from(image, front_hair),
        "character-ahoge": part_from(image, ahoge),
        "character-arm-left": part_from(image, left_arm),
        "character-arm-right": part_from(image, right_arm),
        "character-cloth": part_from(image, cloth),
    }
    save_parts(OUTPUT / "Character", parts)


def build_guild():
    image = keyed_guild()
    size = image.size
    w, h = size
    silhouette = image.getchannel("A")

    roof = multiply_masks(rectangle_mask(size, (0, 0, w, h*.43), 5), silhouette)
    foundation = multiply_masks(rectangle_mask(size, (0, h*.72, w, h), 5), silhouette)
    columns = union_masks(
        multiply_masks(rectangle_mask(size, (w*.30,h*.35,w*.43,h*.86), 5), silhouette),
        multiply_masks(rectangle_mask(size, (w*.60,h*.34,w*.72,h*.86), 5), silhouette),
    )
    door_arch = ellipse_mask(size, (w*.445,h*.53,w*.575,h*.72), 3)
    door_lower = rectangle_mask(size, (w*.445,h*.625,w*.575,h*.89), 3)
    door = multiply_masks(union_masks(door_arch, door_lower), silhouette)
    door_left = multiply_masks(
        door,
        rectangle_mask(size, (w*.435,h*.51,w*.512,h*.91), 2),
    )
    door_right = multiply_masks(
        door,
        rectangle_mask(size, (w*.512,h*.51,w*.585,h*.91), 2),
    )
    sign = multiply_masks(
        ellipse_mask(size, (w*.39,h*.20,w*.63,h*.48), 4),
        silhouette,
    )
    awning = multiply_masks(
        rectangle_mask(size, (w*.67,h*.43,w*.99,h*.78), 5),
        silhouette,
    )
    questboard = multiply_masks(
        rectangle_mask(size, (w*.01,h*.45,w*.39,h*.94), 5),
        silhouette,
    )
    banners = color_mask(
        image,
        lambda rgb: (
            (rgb[:, :, 2].astype(np.int16) > rgb[:, :, 0].astype(np.int16) + 10)
            & (rgb[:, :, 2] > 45)
            & (rgb[:, :, 0] < 125)
        ),
    )
    warm = color_mask(
        image,
        lambda rgb: (
            (rgb[:, :, 0] > 110)
            & (rgb[:, :, 0].astype(np.int16) > rgb[:, :, 2].astype(np.int16) + 35)
            & (rgb[:, :, 1] > 55)
        ),
    )
    lamp_region = rectangle_mask(size, (w*.10,h*.35,w*.91,h*.90), 2)
    lamps = multiply_masks(warm, lamp_region, silhouette)
    sign = subtract_mask(sign, door)
    awning = subtract_mask(awning, door)
    questboard = subtract_mask(questboard, door)
    banners = subtract_mask(banners, door)
    lamps = subtract_mask(lamps, door)

    moving_without_door = union_masks(sign, awning, questboard, banners, lamps)
    moving = union_masks(door, moving_without_door)
    underpaint = inpaint_underlay(image, moving_without_door, 20)
    dark_interior=Image.new("RGBA",size,(7,8,9,255))
    underpaint.paste(dark_interior,(0,0),door)

    static_used = union_masks(roof, foundation, columns)
    body = subtract_mask(silhouette, static_used)
    body = subtract_mask(body, moving)
    roof_static = subtract_mask(roof, moving)
    foundation_static = subtract_mask(foundation, moving)
    columns_static = subtract_mask(columns, moving)

    parts = {
        "guild-underpaint": part_from(underpaint, silhouette),
        "guild-body": part_from(image, body),
        "guild-foundation": part_from(image, foundation_static),
        "guild-roof": part_from(image, roof_static),
        "guild-columns": part_from(image, columns_static),
        "guild-doorway": part_from(dark_interior, door),
        "guild-door-left": part_from(image, door_left),
        "guild-door-right": part_from(image, door_right),
        "guild-sign": part_from(image, sign),
        "guild-awning": part_from(image, awning),
        "guild-banners": part_from(image, banners),
        "guild-lamps": part_from(image, lamps),
        "guild-questboard": part_from(image, questboard),
    }
    save_parts(OUTPUT / "Guild", parts)


def build_simple_building(source: Path, folder_name: str, prefix: str, with_sign: bool, with_awning: bool):
    image = keyed_environment(source)
    size = image.size
    w, h = size
    silhouette = image.getchannel("A")

    roof = multiply_masks(rectangle_mask(size, (0, 0, w, h * .40), 5), silhouette)
    foundation = multiply_masks(rectangle_mask(size, (0, h * .74, w, h), 5), silhouette)
    door_arch = ellipse_mask(size, (w * .40, h * .46, w * .60, h * .70), 3)
    door_lower = rectangle_mask(size, (w * .40, h * .58, w * .60, h * .90), 3)
    door = multiply_masks(union_masks(door_arch, door_lower), silhouette)
    door_left = multiply_masks(door, rectangle_mask(size, (w * .38, h * .45, w * .505, h * .92), 2))
    door_right = multiply_masks(door, rectangle_mask(size, (w * .495, h * .45, w * .62, h * .92), 2))

    banners = color_mask(
        image,
        lambda rgb: (
            (rgb[:, :, 2].astype(np.int16) > rgb[:, :, 0].astype(np.int16) + 10)
            & (rgb[:, :, 2] > 45)
            & (rgb[:, :, 0] < 125)
        ),
    )
    warm = color_mask(
        image,
        lambda rgb: (
            (rgb[:, :, 0] > 110)
            & (rgb[:, :, 0].astype(np.int16) > rgb[:, :, 2].astype(np.int16) + 35)
            & (rgb[:, :, 1] > 55)
        ),
    )
    lamps = multiply_masks(warm, rectangle_mask(size, (w * .08, h * .30, w * .92, h * .88), 2), silhouette)
    sign = multiply_masks(ellipse_mask(size, (w * .36, h * .14, w * .64, h * .42), 4), silhouette) if with_sign else Image.new("L", size, 0)
    awning = multiply_masks(rectangle_mask(size, (w * .62, h * .40, w * .99, h * .74), 5), silhouette) if with_awning else Image.new("L", size, 0)

    sign = subtract_mask(sign, door)
    awning = subtract_mask(awning, door)
    banners = subtract_mask(banners, door)
    lamps = subtract_mask(lamps, door)

    moving_without_door = union_masks(sign, awning, banners, lamps)
    moving = union_masks(door, moving_without_door)
    underpaint = inpaint_underlay(image, moving_without_door, 16)
    dark_interior = Image.new("RGBA", size, (7, 8, 9, 255))
    underpaint.paste(dark_interior, (0, 0), door)

    static_used = union_masks(roof, foundation)
    body = subtract_mask(silhouette, static_used)
    body = subtract_mask(body, moving)
    roof_static = subtract_mask(roof, moving)
    foundation_static = subtract_mask(foundation, moving)

    parts = {
        f"{prefix}-underpaint": part_from(underpaint, silhouette),
        f"{prefix}-body": part_from(image, body),
        f"{prefix}-foundation": part_from(image, foundation_static),
        f"{prefix}-roof": part_from(image, roof_static),
        f"{prefix}-doorway": part_from(dark_interior, door),
        f"{prefix}-door-left": part_from(image, door_left),
        f"{prefix}-door-right": part_from(image, door_right),
        f"{prefix}-banners": part_from(image, banners),
        f"{prefix}-lamps": part_from(image, lamps),
    }
    if with_sign:
        parts[f"{prefix}-sign"] = part_from(image, sign)
    if with_awning:
        parts[f"{prefix}-awning"] = part_from(image, awning)
    save_parts(OUTPUT / folder_name, parts)


if __name__ == "__main__":
    build_character()
    build_guild()
    build_simple_building(GATE_SOURCE, "Gate", "gate", with_sign=False, with_awning=False)
    build_simple_building(FORGE_SOURCE, "Forge", "forge", with_sign=True, with_awning=True)
    print(f"Generated puppet assets under {OUTPUT}")
