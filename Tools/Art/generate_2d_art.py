"""Nivel Cero 2D art generator (original pixel art, no external assets).

Run:  python -I Tools/Art/generate_2d_art.py
Writes sprite sheets into Unity/Assets/NivelCero/Resources/Academic2D/ and a
contact sheet for review into Docs/Arte/. Every pixel is authored by this code
following Docs/Arte/DIRECCION_ARTISTICA.md. Layouts (cell sizes, rows, frame
counts) are mirrored by Scripts/Academic/Academic2DArt.cs; change both together.
"""
from pathlib import Path
import math
import random
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Unity/Assets/NivelCero/Resources/Academic2D"
DOCS = ROOT / "Docs/Arte"
OUT.mkdir(parents=True, exist_ok=True)
DOCS.mkdir(parents=True, exist_ok=True)


def hx(value, alpha=255):
    value = value.lstrip("#")
    return (int(value[0:2], 16), int(value[2:4], 16), int(value[4:6], 16), alpha)


P = {
    "void": hx("070C12"), "deep": hx("0B121A"), "deep2": hx("121D28"),
    "metal0": hx("1E2D3B"), "metal1": hx("2E4354"), "metal2": hx("4B6476"), "edge": hx("8FA6B5"),
    "rivet": hx("6C8597"), "cyan": hx("4FD8E0"), "cyanD": hx("1F7F8C"), "cyanL": hx("B8F6F8"),
    "amber": hx("FFC857"), "amberD": hx("C9852B"), "amberL": hx("FFF1C4"),
    "red": hx("E8435A"), "redD": hx("8E1F33"), "redL": hx("FFB3BE"),
    "violet": hx("9B4FD6"), "violetD": hx("4A2266"), "white": hx("E6EEF2"), "grey": hx("9FB3C0"),
    "outline": hx("06090D"), "suit": hx("2B3E50"), "suitL": hx("3F596E"), "suitD": hx("1A2733"),
    "shell": hx("D5DEE5"), "shellD": hx("98A9B6"), "shellL": hx("F4F8FA"),
    "gmetal": hx("2A3440"), "gmetalL": hx("46525F"), "gmetalD": hx("161C24"),
}
CLEAR = (0, 0, 0, 0)


# ---------------------------------------------------------------- helpers

def canvas(w, h):
    return Image.new("RGBA", (w, h), CLEAR)


def outline(img, color=P["outline"], diagonal=False):
    """1 px dark contour around every opaque pixel (pixel-art convention)."""
    src = img.load()
    w, h = img.size
    out = img.copy()
    dst = out.load()
    offsets = [(1, 0), (-1, 0), (0, 1), (0, -1)]
    if diagonal:
        offsets += [(1, 1), (-1, 1), (1, -1), (-1, -1)]
    for y in range(h):
        for x in range(w):
            if src[x, y][3] != 0:
                continue
            for dx, dy in offsets:
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and src[nx, ny][3] > 0 and src[nx, ny][:3] != color[:3]:
                    dst[x, y] = color
                    break
    return out


def limb(d, a, b, width, color):
    d.line([a, b], fill=color, width=width)
    r = width // 2
    for p in (a, b):
        d.rectangle([p[0] - r, p[1] - r, p[0] - r + width - 1, p[1] - r + width - 1], fill=color)


def polar(origin, length, degrees):
    rad = math.radians(degrees)
    return (round(origin[0] + math.cos(rad) * length), round(origin[1] + math.sin(rad) * length))


def silhouette(img, color):
    out = canvas(*img.size)
    px, po = img.load(), out.load()
    for y in range(img.size[1]):
        for x in range(img.size[0]):
            if px[x, y][3] > 0:
                po[x, y] = color
    return out


def glitch_slices(img, rng, rows=3, shift=3, tint=P["violet"]):
    """Horizontal slice displacement with a violet ghost: GL1TCH language."""
    w, h = img.size
    out = img.copy()
    for _ in range(rows):
        y0 = rng.randrange(0, h - 4)
        band = rng.randrange(2, 6)
        dx = rng.choice([-shift, shift])
        piece = img.crop((0, y0, w, y0 + band))
        ghost = silhouette(piece, (tint[0], tint[1], tint[2], 150))
        out.paste(CLEAR, (0, y0, w, y0 + band))
        out.alpha_composite(ghost, (dx * 2 if dx > 0 else 0, y0)) if dx > 0 else out.alpha_composite(ghost, (0, y0))
        out.alpha_composite(piece, (max(0, dx), y0)) if dx > 0 else out.alpha_composite(piece.crop((-dx, 0, w, band)), (0, y0))
    return out


def sheet(frames, cols, cell):
    rows = math.ceil(len(frames) / cols)
    img = canvas(cols * cell[0], rows * cell[1])
    for i, f in enumerate(frames):
        if f is not None:
            img.alpha_composite(f, ((i % cols) * cell[0], (i // cols) * cell[1]))
    return img


# ---------------------------------------------------------------- Zero
# Frame 48x56. Feet rest on row 51 (4 px above the bottom edge) so that the
# sprite pivot (0.5, 0.5) matches the centre of the 1.5-unit capsule collider.
ZW, ZH = 48, 56


def zero_frame(bob=0, lean=0, legs=((0, 0), (0, 0)), back_arm=100, cable=160, crouch=0,
               visor=P["cyan"], hurt=False, head_tilt=0, flat=0, airborne=False):
    img = canvas(ZW, ZH)
    d = ImageDraw.Draw(img)
    thigh_len, shin_len = 8 - crouch // 3, 9 - crouch // 3
    if not airborne:
        # Ground contact: place the hip so the lowest foot rests on row 51.
        drop = max(math.sin(math.radians(90 - s)) * thigh_len + math.sin(math.radians(90 - s + b)) * shin_len for s, b in legs)
        hip_y = 51 - round(drop)
        bob = hip_y - 33 - crouch
    hip = (23 + lean // 2, 33 + bob + crouch)
    shoulder = (23 + lean, 23 + bob + crouch)
    # Legs: (hip angle offset, knee bend) for back and front leg; angles in degrees from vertical.
    for index, (swing, bend) in enumerate(legs):
        color = P["suitD"] if index == 0 else P["suit"]
        boot = P["shellD"] if index == 0 else P["shell"]
        knee = polar(hip, thigh_len, 90 - swing)
        foot = polar(knee, shin_len, 90 - swing + bend)
        if index == 1:
            limb(d, (hip[0] + 1, hip[1]), knee, 4, color)
        else:
            limb(d, (hip[0] - 1, hip[1]), knee, 4, color)
        limb(d, knee, foot, 4, color)
        fx = 1 if swing >= -5 else -1
        d.rectangle([foot[0] - 2, foot[1] - 1, foot[0] + 3 * fx if fx > 0 else foot[0] + 2, foot[1] + 1], fill=boot)
    # Back arm (front arm + weapon are a separate aimed sprite).
    elbow = polar((shoulder[0] - 3, shoulder[1] + 1), 6, back_arm)
    hand = polar(elbow, 6, back_arm - 15)
    limb(d, (shoulder[0] - 3, shoulder[1] + 1), elbow, 3, P["suitD"])
    limb(d, elbow, hand, 3, P["suitD"])
    d.rectangle([hand[0] - 1, hand[1] - 1, hand[0] + 1, hand[1] + 1], fill=P["shellD"])
    # Torso: suit with a light chest plate and a small cyan core.
    tx, ty = shoulder[0], shoulder[1]
    d.polygon([(tx - 6, ty - 1), (tx + 6, ty - 1), (hip[0] + 5, hip[1]), (hip[0] - 5, hip[1])], fill=P["suit"])
    d.line([(tx + 5, ty), (hip[0] + 4, hip[1] - 1)], fill=P["suitL"])
    d.polygon([(tx - 1, ty), (tx + 5, ty), (tx + 4, ty + 5), (tx, ty + 5)], fill=P["shellD"])
    d.rectangle([tx - 6, ty - 1, tx - 2, ty + 1], fill=P["shellD"])  # collar / back pad
    d.rectangle([tx + 2, ty + 2, tx + 3, ty + 3], fill=P["cyan"])
    d.rectangle([hip[0] - 5, hip[1] - 2, hip[0] + 5, hip[1]], fill=P["suitD"])  # belt
    d.point((hip[0] + 3, hip[1] - 1), fill=P["amberD"])
    # Cable tail from the back of the helmet: the asymmetric signature.
    hx0, hy0 = shoulder[0] - 5 + head_tilt, shoulder[1] - 8
    seg = [(hx0, hy0)]
    for i in range(1, 5):
        seg.append(polar(seg[-1], 4, cable + i * 12 * (1 if cable < 180 else -1)))
    d.line(seg, fill=P["cyanD"], width=2)
    d.point(seg[-1], fill=P["cyan"])
    # Helmet: rounded light shell, horizontal visor.
    cx, cy = shoulder[0] + 1 + head_tilt, shoulder[1] - 9 + flat
    d.ellipse([cx - 8, cy - 8 + flat, cx + 8, cy + 7], fill=P["shell"])
    d.ellipse([cx - 8, cy - 1, cx + 8, cy + 7], fill=P["shellD"])
    d.rectangle([cx - 7, cy - 2, cx + 7, cy + 3], fill=P["shell"])
    d.arc([cx - 7, cy - 7 + flat, cx + 5, cy + 3], 200, 290, fill=P["shellL"])
    d.rectangle([cx - 1, cy - 1, cx + 8, cy + 2], fill=P["outline"])
    d.rectangle([cx, cy, cx + 7, cy + 1], fill=visor)
    d.point((cx + 6, cy), fill=P["cyanL"] if visor == P["cyan"] else visor)
    d.rectangle([cx - 6, cy + 4, cx - 3, cy + 5], fill=P["shellD"])  # jaw vent
    img = outline(img)
    img.info["shoulder"] = (shoulder[0] + 2, shoulder[1] + 1)
    if hurt:
        tint = canvas(ZW, ZH)
        mask = silhouette(img, (255, 110, 130, 80))
        tint.alpha_composite(mask)
        img.alpha_composite(tint)
        img.info["shoulder"] = (shoulder[0] + 2, shoulder[1] + 1)
    return img


def zero_frames():
    frames = {}
    frames["idle"] = [zero_frame(bob=b, cable=c, legs=((-4, 2), (4, -2)), back_arm=96 + b * 3)
                      for b, c in [(0, 158), (0, 162), (1, 166), (1, 162)]]
    run = []
    cycle = [(28, 34), (16, 50), (-2, 40), (-20, 18), (-28, 8), (-14, 4), (6, 10), (20, 22)]
    for i in range(8):
        front = cycle[i]
        back = cycle[(i + 4) % 8]
        bob = -1 if i % 4 in (1, 2) else 0
        run.append(zero_frame(bob=bob, lean=3, legs=((back[0], back[1]), (front[0], front[1])),
                              back_arm=70 + (i % 8) * 0 + (35 if i < 4 else -5), cable=170 + (i % 2) * 6))
    frames["run"] = run
    frames["jump"] = [zero_frame(bob=-3, lean=2, legs=((-20, 50), (30, 60)), back_arm=200, cable=150, airborne=True),
                      zero_frame(bob=-4, lean=2, legs=((-10, 40), (24, 70)), back_arm=210, cable=145, airborne=True)]
    frames["fall"] = [zero_frame(bob=-2, lean=1, legs=((-6, 25), (16, 30)), back_arm=230, cable=200, airborne=True),
                      zero_frame(bob=-2, lean=1, legs=((-8, 22), (18, 26)), back_arm=236, cable=206, airborne=True)]
    frames["land"] = [zero_frame(bob=0, crouch=4, legs=((-18, 40), (18, -40)), back_arm=130, cable=200, flat=1)]
    frames["dash"] = [zero_frame(bob=-1, lean=7, legs=((-40, 30), (40, 60)), back_arm=165, cable=176, head_tilt=2, airborne=True),
                      zero_frame(bob=-1, lean=8, legs=((-46, 32), (44, 64)), back_arm=170, cable=178, head_tilt=2, airborne=True)]
    frames["hurt"] = [zero_frame(bob=0, lean=-4, legs=((-14, 20), (10, 10)), back_arm=230, cable=110, hurt=True, head_tilt=-2),
                      zero_frame(bob=1, lean=-3, legs=((-10, 18), (8, 10)), back_arm=220, cable=140, head_tilt=-1)]
    death = []
    rng = random.Random(9)
    base = zero_frame(bob=0, lean=-4, legs=((-14, 20), (10, 10)), back_arm=230, cable=110, head_tilt=-2, visor=P["red"])
    death.append(base)
    death.append(zero_frame(bob=5, crouch=5, legs=((-40, 80), (30, 70)), back_arm=150, cable=120, visor=P["redD"], flat=1))
    knee = zero_frame(bob=9, crouch=6, legs=((-60, 100), (50, 90)), back_arm=120, cable=130, visor=P["redD"], flat=1)
    death.append(knee)
    death.append(glitch_slices(knee, rng, rows=4, shift=2))
    # Pixel dissolve into data: Zero is a program, not a body.
    for step in range(2):
        f = knee.copy()
        px = f.load()
        for y in range(ZH):
            for x in range(ZW):
                if px[x, y][3] and rng.random() < .35 + step * .35:
                    px[x, y] = CLEAR if rng.random() < .7 else P["cyanD"]
        death.append(glitch_slices(f, rng, rows=3, shift=3))
    frames["death"] = death
    return frames


ZERO_ROWS = ["idle", "run", "jump", "fall", "land", "dash", "hurt", "death"]


def zero_arm(angle_deg, weapon):
    """Front arm + weapon at a pre-rendered aim angle (facing right).

    48x48 cell, shoulder pivot at the centre (24, 24). Pre-rendering angles keeps
    pixels on the grid instead of rotating pixel art at runtime.
    """
    img = canvas(48, 48)
    d = ImageDraw.Draw(img)
    sh = (24, 24)
    elbow = polar(sh, 5, -angle_deg + 25)
    hand = polar(sh, 9, -angle_deg)
    limb(d, sh, elbow, 3, P["suit"])
    limb(d, elbow, hand, 3, P["suit"])
    if weapon == "blaster":
        tip = polar(hand, 9, -angle_deg)
        limb(d, hand, tip, 4, P["metal1"])
        mid = polar(hand, 4, -angle_deg)
        d.rectangle([mid[0] - 1, mid[1] - 1, mid[0], mid[1]], fill=P["metal2"])
        d.rectangle([tip[0] - 1, tip[1] - 1, tip[0], tip[1]], fill=P["cyan"])
        grip = polar(hand, 3, -angle_deg + 90)
        d.point(grip, fill=P["metal0"])
    else:
        hilt = polar(hand, 3, -angle_deg)
        tip = polar(hand, 13, -angle_deg)
        limb(d, hand, hilt, 3, P["metal2"])
        d.line([hilt, tip], fill=P["cyan"], width=2)
        d.point(tip, fill=P["cyanL"])
    d.rectangle([hand[0] - 1, hand[1] - 1, hand[0] + 1, hand[1] + 1], fill=P["shell"])
    return outline(img)


ARM_ANGLES = [-90, -67, -45, -22, 0, 22, 45, 67, 90]


# ---------------------------------------------------------------- Bit
BW = 24


def bit_frame(fin=0, eye="open", look=(1, 0), glow=0, bob=0):
    img = canvas(BW, BW)
    d = ImageDraw.Draw(img)
    cx, cy = 12, 12 + bob
    # Side fins flap: the "breathing" of a small drone.
    for side in (-1, 1):
        x0 = cx + side * 8
        d.polygon([(x0, cy - 1), (x0 + side * (4 + fin), cy - 3 - fin), (x0 + side * (3 + fin), cy + 2)], fill=P["cyanD"])
    d.ellipse([cx - 8, cy - 8, cx + 8, cy + 8], fill=P["shell"])
    d.ellipse([cx - 8, cy - 2, cx + 8, cy + 8], fill=P["shellD"])
    d.arc([cx - 7, cy - 7, cx + 4, cy + 3], 200, 280, fill=P["shellL"])
    d.line([(cx, cy - 8), (cx + 1, cy - 11)], fill=P["metal2"])
    d.point((cx + 1, cy - 11), fill=P["amber"] if glow else P["cyan"])
    # Big single eye: the expressive face.
    d.ellipse([cx - 5, cy - 4, cx + 5, cy + 5], fill=P["outline"])
    ex, ey = cx + look[0], cy + look[1]
    if eye == "open":
        d.ellipse([ex - 3, ey - 3, ex + 3, ey + 3], fill=P["cyan"])
        d.rectangle([ex - 1, ey - 1, ex, ey], fill=P["cyanL"])
    elif eye == "happy":
        d.arc([ex - 3, ey - 2, ex + 3, ey + 4], 200, 340, fill=P["cyan"], width=2)
    elif eye == "blink":
        d.line([(ex - 3, ey), (ex + 3, ey)], fill=P["cyan"], width=1)
    elif eye == "alert":
        d.ellipse([ex - 4, ey - 4, ex + 4, ey + 4], fill=P["amber"])
        d.rectangle([ex - 1, ey - 2, ex, ey + 2], fill=P["outline"])
    elif eye == "worried":
        d.ellipse([ex - 3, ey - 2, ex + 3, ey + 3], fill=P["cyan"])
        d.line([(ex - 4, ey - 4), (ex + 1, ey - 2)], fill=P["shell"], width=2)
    img = outline(img)
    if glow:
        ring = canvas(BW, BW)
        rd = ImageDraw.Draw(ring)
        rd.ellipse([1, 1 + bob, 22, 22 + bob], outline=(79, 216, 224, 90 + glow * 40), width=1)
        ring.alpha_composite(img)
        img = ring
    return img


def bit_frames():
    f = {}
    f["idle"] = [bit_frame(fin=a, bob=b) for a, b in [(0, 0), (1, 0), (2, -1), (1, -1)]]
    f["blink"] = [bit_frame(fin=1, eye="blink"), bit_frame(fin=1)]
    f["talk"] = [bit_frame(fin=2, eye="happy", glow=1), bit_frame(fin=1, eye="open", glow=2, look=(1, -1)),
                 bit_frame(fin=2, eye="happy", glow=1, bob=-1), bit_frame(fin=0, eye="open", glow=2)]
    f["alert"] = [bit_frame(fin=3, eye="alert", glow=2), bit_frame(fin=2, eye="alert", glow=1, bob=-1)]
    f["worried"] = [bit_frame(fin=0, eye="worried", look=(0, 1)), bit_frame(fin=1, eye="worried", look=(0, 1), bob=1)]
    f["look"] = [bit_frame(eye="open", look=l) for l in [(-1, 0), (0, -1), (1, 0), (0, 1)]]
    return f


BIT_ROWS = ["idle", "blink", "talk", "alert", "worried", "look"]


# ---------------------------------------------------------------- Guardian
GW = 128


def guardian_frame(variant=0, lids=0.0, core=0.0, arms=0.0, recoil=0, thruster=0, flash=False,
                   death=0.0, seed=1, bob=0):
    """variant 0..2 = phase armour; lids 0 closed..1 open; arms 0 down..1 raised."""
    rng = random.Random(seed)
    img = canvas(GW, GW)
    d = ImageDraw.Draw(img)
    cx, cy = 64, 58 + bob + recoil
    sink = int(death * 26)
    cy += sink
    # Thrusters (hover): short, readable, warm-red under the skirt.
    if death < .5:
        for tx in (cx - 14, cx + 14):
            length = 8 + (thruster % 3) * 3
            d.polygon([(tx - 4, cy + 34), (tx + 4, cy + 34), (tx, cy + 34 + length)], fill=P["redD"])
            d.polygon([(tx - 2, cy + 34), (tx + 2, cy + 34), (tx, cy + 34 + length - 4)], fill=P["amber"])
    # Arms: heavy claws hanging from shoulders; raised before a floor slam.
    for side in (-1, 1):
        if variant >= 2 and side == -1 and death > 0:
            continue
        sh = (cx + side * 40, cy - 14)
        ang = 90 - side * (20 + arms * 110)
        elbow = polar(sh, 16, ang)
        hand = polar(elbow, 16, ang + side * (25 - arms * 40))
        limb(d, sh, elbow, 9, P["gmetalD"])
        limb(d, elbow, hand, 7, P["gmetal"])
        d.rectangle([elbow[0] - 3, elbow[1] - 3, elbow[0] + 3, elbow[1] + 3], fill=P["gmetalL"])
        for k in (-1, 0, 1):
            claw = polar(hand, 8, ang + side * (25 - arms * 40) + k * 28)
            d.line([hand, claw], fill=P["metal2"], width=3)
            d.point(claw, fill=P["red"])
    # Chassis: wide shoulders, narrow skirt (heavy, aggressive trapezoid).
    body = [(cx - 38, cy - 24), (cx + 38, cy - 24), (cx + 26, cy + 30), (cx + 14, cy + 36), (cx - 14, cy + 36), (cx - 26, cy + 30)]
    d.polygon(body, fill=P["gmetal"])
    d.polygon([(cx - 36, cy - 22), (cx + 36, cy - 22), (cx + 33, cy - 14), (cx - 33, cy - 14)], fill=P["gmetalL"])
    d.polygon([(cx - 24, cy + 22), (cx + 24, cy + 22), (cx + 14, cy + 36), (cx - 14, cy + 36)], fill=P["gmetalD"])
    for x in (-20, -10, 10, 20):
        d.line([(cx + x, cy + 24), (cx + x * .6, cy + 34)], fill=P["outline"])
    # Crown spikes: threat reads through shape, not only colour.
    for x, h in ((-22, 10), (-9, 14), (9, 14), (22, 10)):
        d.polygon([(cx + x - 4, cy - 24), (cx + x + 4, cy - 24), (cx + x + 1, cy - 24 - h)], fill=P["gmetalL"])
        d.point((cx + x + 1, cy - 24 - h + 2), fill=P["red"])
    # Shoulder plates: lost one per phase.
    for side in (-1, 1):
        lost = (variant >= 1 and side == -1) or (variant >= 2 and side == 1)
        sx = cx + side * 40
        if not lost:
            plate = [(sx - side * 10, cy - 30), (sx + side * 12, cy - 26), (sx + side * 14, cy - 4), (sx - side * 2, cy + 2), (sx - side * 12, cy - 12)]
            d.polygon(plate, fill=P["metal1"])
            d.line([plate[0], plate[1]], fill=P["edge"], width=2)
            for i in range(3):
                d.line([(sx - side * 6 + side * i * 6, cy - 16), (sx - side * 2 + side * i * 6, cy - 22)], fill=P["red"])
        else:
            # Exposed socket: red-lit cabling, sparks.
            d.ellipse([sx - 9, cy - 22, sx + 9, cy - 4], fill=P["gmetalD"])
            for i in range(4):
                a = rng.randrange(0, 360)
                d.line([(sx, cy - 13), polar((sx, cy - 13), 7, a)], fill=P["redD"], width=2)
            d.point((sx + rng.randrange(-5, 6), cy - 13 + rng.randrange(-5, 6)), fill=P["amberL"])
    # Phase damage: cracks across the chassis.
    if variant >= 1:
        crack = [(cx - 30, cy - 10), (cx - 22, cy - 4), (cx - 25, cy + 6), (cx - 16, cy + 14)]
        d.line(crack, fill=P["redD"], width=1)
    if variant >= 2:
        crack = [(cx + 28, cy - 12), (cx + 20, cy - 2), (cx + 24, cy + 8), (cx + 12, cy + 18)]
        d.line(crack, fill=P["red"], width=1)
    # Core eye with armoured lids.
    ex, ey = cx, cy + 2
    d.ellipse([ex - 15, ey - 15, ex + 15, ey + 15], fill=P["outline"])
    d.ellipse([ex - 13, ey - 13, ex + 13, ey + 13], fill=P["redD"])
    ring = int(4 + core * 6)
    if lids > .3:
        d.ellipse([ex - 11, ey - 11, ex + 11, ey + 11], fill=P["red"])
        d.ellipse([ex - ring, ey - ring, ex + ring, ey + ring], fill=P["amber"] if core > .2 else P["redL"])
        d.ellipse([ex - ring // 2, ey - ring // 2, ex + ring // 2, ey + ring // 2], fill=P["amberL"] if core > .5 else P["white"])
    else:
        d.ellipse([ex - 6, ey - 3, ex + 6, ey + 3], fill=P["red"])
    gap = int(lids * 11)
    d.polygon([(ex - 16, ey - 1 - gap), (ex + 16, ey - 1 - gap), (ex + 12, ey - 15), (ex - 12, ey - 15)], fill=P["metal1"])
    d.line([(ex - 15, ey - 1 - gap), (ex + 15, ey - 1 - gap)], fill=P["edge"])
    d.polygon([(ex - 16, ey + 1 + gap), (ex + 16, ey + 1 + gap), (ex + 12, ey + 15), (ex - 12, ey + 15)], fill=P["gmetalD"])
    img = outline(img, diagonal=False)
    if death > 0:
        px = img.load()
        for y in range(GW):
            for x in range(GW):
                if px[x, y][3] and rng.random() < death * .75:
                    px[x, y] = CLEAR if rng.random() < .75 else (P["violet"] if rng.random() < .5 else P["red"])
        img = glitch_slices(img, rng, rows=int(2 + death * 6), shift=int(2 + death * 6))
    if flash:
        img = silhouette(img, (255, 255, 255, 235))
    return img


def guardian_frames():
    rows = {}
    for v in range(3):
        rows["idle%d" % v] = [guardian_frame(v, thruster=t, bob=b) for t, b in [(0, 0), (1, -1), (2, -1), (1, 0)]]
        rows["charge%d" % v] = [guardian_frame(v, lids=l, core=c, thruster=i) for i, (l, c) in enumerate([(.35, .2), (.6, .55), (.85, 1)])]
        rows["fire%d" % v] = [guardian_frame(v, lids=1, core=1, recoil=-3, thruster=2), guardian_frame(v, lids=.8, core=.4, recoil=-1, thruster=1)]
        rows["exposed%d" % v] = [guardian_frame(v, lids=1, core=.15, thruster=0), guardian_frame(v, lids=1, core=.35, thruster=1, bob=1)]
        rows["raise%d" % v] = [guardian_frame(v, arms=.5, lids=.2, thruster=1), guardian_frame(v, arms=1, lids=.4, core=.3, thruster=2, bob=-2)]
        rows["slam%d" % v] = [guardian_frame(v, arms=.1, lids=.4, recoil=4, thruster=0), guardian_frame(v, arms=0, lids=.3, recoil=2, thruster=1)]
        rows["flash%d" % v] = [guardian_frame(v, flash=True)]
        rng = random.Random(40 + v)
        rows["glitch%d" % v] = [glitch_slices(guardian_frame(v, lids=.5, core=.5), rng, rows=5, shift=4),
                                glitch_slices(guardian_frame(min(2, v + 1), lids=.2), rng, rows=4, shift=3)]
    rows["death"] = [guardian_frame(2, lids=1, core=1 - t * .2, death=t, seed=70 + i, thruster=i)
                     for i, t in enumerate([0, .12, .25, .38, .52, .66, .8, .95])]
    rows["dormant"] = [guardian_frame(0, lids=0, core=0, thruster=0, bob=6)]
    return rows


# ---------------------------------------------------------------- tiles (32x32)
T = 32


def tile_metal(top=None, left_edge=False, right_edge=False, variant=0, arena=False, platform=False):
    img = canvas(T, T)
    d = ImageDraw.Draw(img)
    base, light, dark = (P["metal1"], P["metal2"], P["metal0"])
    if arena:
        base, light, dark = (hx("2A3442"), hx("3D4858"), hx("181E27"))
    d.rectangle([0, 0, T - 1, T - 1], fill=base)
    # Panel seams and bevel: modular, ordered (healthy system).
    d.rectangle([0, 0, T - 1, 0], fill=light)
    d.line([(0, 15), (T - 1, 15)], fill=dark)
    d.line([(0, 16), (T - 1, 16)], fill=light if variant != 2 else base)
    d.line([(15, 0), (15, 15)], fill=dark)
    d.line([(7 if variant else 23, 16), (7 if variant else 23, 31)], fill=dark)
    for rx, ry in ((3, 4), (28, 4), (3, 27), (28, 27)):
        d.point((rx, ry), fill=P["rivet"])
    if variant == 1:
        for y in range(20, 29, 3):
            d.line([(11, y), (20, y)], fill=dark)
    if variant == 3:
        d.rectangle([20, 20, 27, 26], fill=P["metal0"])
        d.point((23, 23), fill=P["cyanD"])
    if top:
        # Walkable top: 3 px light edge with a highlight line, consistent everywhere.
        d.rectangle([0, 0, T - 1, 3], fill=P["edge"] if not arena else hx("7D8794"))
        d.line([(0, 0), (T - 1, 0)], fill=P["white"])
        d.line([(0, 4), (T - 1, 4)], fill=P["outline"])
        if arena:
            for x in range(0, T, 8):
                d.polygon([(x, 5), (x + 4, 5), (x + 8, 9), (x + 4, 9)], fill=P["redD"])
    if left_edge:
        d.rectangle([0, 0, 2, T - 1], fill=dark)
        d.line([(0, 0), (0, T - 1)], fill=P["outline"])
        if top:
            d.rectangle([0, 0, 3, 3], fill=P["grey"])
    if right_edge:
        d.rectangle([T - 3, 0, T - 1, T - 1], fill=dark)
        d.line([(T - 1, 0), (T - 1, T - 1)], fill=P["outline"])
        if top:
            d.rectangle([T - 4, 0, T - 1, 3], fill=P["grey"])
    if platform:
        # Catwalk girder: open truss so it reads as "floating platform".
        img = canvas(T, T)
        d = ImageDraw.Draw(img)
        d.rectangle([0, 0, T - 1, 9], fill=P["metal2"])
        d.rectangle([0, 0, T - 1, 2], fill=P["edge"])
        d.line([(0, 0), (T - 1, 0)], fill=P["white"])
        d.line([(0, 10), (T - 1, 10)], fill=P["outline"])
        d.rectangle([0, 22, T - 1, 26], fill=P["metal1"])
        for x in range(0, T, 16):
            d.line([(x, 10), (x + 15, 22)], fill=P["metal1"], width=2)
            d.line([(x + 15, 10), (x, 22)], fill=P["metal0"], width=2)
        if left_edge:
            d.rectangle([0, 0, 3, 26], fill=P["metal2"])
            d.line([(0, 0), (0, 26)], fill=P["outline"])
        if right_edge:
            d.rectangle([T - 4, 0, T - 1, 26], fill=P["metal2"])
            d.line([(T - 1, 0), (T - 1, 26)], fill=P["outline"])
    return img


def tile_hazard(frame):
    img = canvas(T, T)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 6, T - 1, T - 1], fill=P["redD"])
    for x in range(-T, T, 8):
        o = x + frame * 4
        d.polygon([(o, T - 1), (o + 4, T - 1), (o + 4 + 20, 6), (o + 20, 6)], fill=P["outline"])
    for x in range(0, T, 8):
        d.polygon([(x, 6), (x + 4, 0), (x + 8, 6)], fill=P["red"])
    d.line([(0, 6), (T - 1, 6)], fill=P["redL"])
    return img


def tile_backpanel(variant):
    img = canvas(T, T)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, T - 1, T - 1], fill=hx("16222D"))
    d.rectangle([1, 1, T - 2, T - 2], outline=hx("1D2B38"))
    if variant == 1:
        for y in range(6, 27, 4):
            d.line([(6, y), (25, y)], fill=hx("0E161E"))
    if variant == 2:
        d.rectangle([6, 7, 25, 20], fill=hx("0C141B"))
        for i, y in enumerate(range(9, 19, 3)):
            d.line([(8, y), (8 + (5, 14, 9, 12)[i], y)], fill=P["cyanD"])
    if variant == 3:
        d.ellipse([8, 8, 23, 23], fill=hx("0E161E"))
        d.line([(15, 9), (15, 22)], fill=hx("1D2B38"))
        d.line([(9, 15), (22, 15)], fill=hx("1D2B38"))
    return img


def tile_misc(kind):
    img = canvas(T, T)
    d = ImageDraw.Draw(img)
    if kind == "girder":
        d.rectangle([0, 10, T - 1, 21], fill=hx("1A2633"))
        d.line([(0, 10), (T - 1, 10)], fill=hx("2A3B4A"))
        for x in range(0, T, 8):
            d.line([(x, 11), (x + 7, 20)], fill=hx("111A23"))
    elif kind == "pillar":
        d.rectangle([9, 0, 22, T - 1], fill=hx("1A2633"))
        d.line([(9, 0), (9, T - 1)], fill=hx("2A3B4A"))
        d.rectangle([11, 4, 20, 6], fill=hx("111A23"))
    elif kind == "pipeH":
        d.rectangle([0, 12, T - 1, 19], fill=hx("23323F"))
        d.line([(0, 13), (T - 1, 13)], fill=hx("33495A"))
        d.rectangle([14, 10, 17, 21], fill=hx("1A2633"))
    elif kind == "pipeV":
        d.rectangle([12, 0, 19, T - 1], fill=hx("23323F"))
        d.line([(13, 0), (13, T - 1)], fill=hx("33495A"))
        d.rectangle([10, 14, 21, 17], fill=hx("1A2633"))
    elif kind in ("corruptA", "corruptB"):
        rng = random.Random(3 if kind == "corruptA" else 8)
        for _ in range(7):
            x, y = rng.randrange(0, 26), rng.randrange(0, 28)
            w, h = rng.randrange(3, 10), rng.randrange(1, 4)
            d.rectangle([x, y, min(T - 1, x + w), y + h], fill=(155, 79, 214, rng.choice([90, 140, 200])))
        for _ in range(3):
            y = rng.randrange(0, 30)
            d.line([(0, y), (T - 1, y)], fill=(74, 34, 102, 120))
    elif kind == "cable":
        pts = [(4, 0), (6, 10), (9, 20), (8, 31)]
        d.line(pts, fill=hx("101820"), width=3)
        pts2 = [(20, 0), (22, 8), (21, 18)]
        d.line(pts2, fill=hx("152029"), width=2)
        d.point((21, 19), fill=P["cyanD"])
    elif kind == "console":
        d.rectangle([3, 12, 28, 31], fill=hx("1C2A36"))
        d.polygon([(3, 12), (28, 12), (25, 6), (6, 6)], fill=hx("26384A"))
        d.rectangle([8, 8, 23, 11], fill=hx("0C141B"))
        d.point((10, 9), fill=P["cyanD"])
        d.point((13, 9), fill=P["amberD"])
        for y in range(16, 29, 4):
            d.line([(6, y), (25, y)], fill=hx("13202A"))
    elif kind == "rack":
        d.rectangle([5, 0, 26, T - 1], fill=hx("17232E"))
        for y in range(3, 30, 5):
            d.rectangle([7, y, 24, y + 3], fill=hx("0E161E"))
            d.point((22, y + 1), fill=P["cyanD"] if y % 2 else hx("2A3B4A"))
    elif kind == "signArrow":
        d.rectangle([4, 8, 27, 23], fill=hx("102029"))
        d.rectangle([4, 8, 27, 23], outline=P["cyanD"])
        d.polygon([(10, 13), (18, 13), (18, 10), (23, 16), (18, 21), (18, 18), (10, 18)], fill=P["cyan"])
    elif kind == "signHazard":
        d.line([(15, 22), (15, T - 1)], fill=hx("1A2633"), width=3)
        d.polygon([(15, 2), (28, 22), (3, 22)], fill=P["red"])
        d.polygon([(15, 6), (24, 20), (6, 20)], fill=P["outline"])
        d.rectangle([14, 10, 16, 15], fill=P["red"])
        d.rectangle([14, 17, 16, 18], fill=P["red"])
    elif kind == "wallCap":
        d.rectangle([0, 0, T - 1, T - 1], fill=P["metal0"])
        d.rectangle([2, 0, T - 3, T - 1], fill=P["metal1"])
        for y in range(0, T, 8):
            d.line([(2, y), (T - 3, y)], fill=P["metal0"])
        d.polygon([(4, 2), (10, 2), (4, 8)], fill=P["redD"])
    elif kind == "wall":
        d.rectangle([0, 0, T - 1, T - 1], fill=P["metal0"])
        d.rectangle([3, 0, T - 4, T - 1], fill=P["metal1"])
        d.line([(3, 0), (3, T - 1)], fill=P["metal2"])
        for y in range(4, T, 8):
            d.point((8, y), fill=P["rivet"])
            d.point((23, y), fill=P["rivet"])
    return img


TILE_ORDER = [
    "groundL", "groundM", "groundM2", "groundR", "fill", "fill2", "fillL", "fillR",
    "platL", "platM", "platR", "wall", "wallCap", "hazard0", "hazard1", "back0",
    "back1", "back2", "back3", "girder", "pillar", "pipeH", "pipeV", "corruptA",
    "corruptB", "cable", "console", "rack", "signArrow", "signHazard", "arenaTop", "arenaFill",
]


def tiles():
    t = {
        "groundL": tile_metal(top=True, left_edge=True), "groundM": tile_metal(top=True),
        "groundM2": tile_metal(top=True, variant=3), "groundR": tile_metal(top=True, right_edge=True),
        "fill": tile_metal(variant=0), "fill2": tile_metal(variant=1), "fillL": tile_metal(left_edge=True, variant=2),
        "fillR": tile_metal(right_edge=True, variant=2),
        "platL": tile_metal(platform=True, left_edge=True), "platM": tile_metal(platform=True),
        "platR": tile_metal(platform=True, right_edge=True),
        "wall": tile_misc("wall"), "wallCap": tile_misc("wallCap"), "hazard0": tile_hazard(0), "hazard1": tile_hazard(1),
        "arenaTop": tile_metal(top=True, arena=True), "arenaFill": tile_metal(arena=True, variant=1),
    }
    for i in range(4):
        t["back%d" % i] = tile_backpanel(i)
    for k in ["girder", "pillar", "pipeH", "pipeV", "corruptA", "corruptB", "cable", "console", "rack", "signArrow", "signHazard"]:
        t[k] = tile_misc(k)
    return [t[name] for name in TILE_ORDER]


# ---------------------------------------------------------------- FX & props (48x48 cells)
FX = 48


def fx_cell(draw_fn):
    img = canvas(FX, FX)
    draw_fn(ImageDraw.Draw(img), img)
    return img


def fx_frames():
    rows = {}
    # Boss orb: red core with sharp points (hostile = spiky).
    orb = []
    for i in range(4):
        def f(d, img, i=i):
            c = 24
            for k in range(6):
                a = k * 60 + i * 15
                d.polygon([polar((c, c), 5, a - 25), polar((c, c), 11 + (k + i) % 2 * 2, a), polar((c, c), 5, a + 25)], fill=P["red"])
            d.ellipse([c - 6, c - 6, c + 6, c + 6], fill=P["redD"])
            d.ellipse([c - 4, c - 4, c + 4, c + 4], fill=P["redL"])
            d.ellipse([c - 2, c - 2, c + 2, c + 2], fill=P["white"])
        orb.append(outline(fx_cell(f)))
    rows["orb"] = orb
    # Zero bolt (cyan, rounded, elongated).
    bolt = []
    for i in range(2):
        def f(d, img, i=i):
            d.rounded_rectangle([10, 21, 38, 26], 2, fill=P["cyanD"])
            d.rounded_rectangle([16 + i * 2, 22, 38, 25], 2, fill=P["cyan"])
            d.line([(24 + i * 2, 23), (37, 23)], fill=P["cyanL"])
        bolt.append(fx_cell(f))
    rows["bolt"] = bolt
    # Impacts: cyan (hit), white (deflect on armour), red (hostile hit on Zero).
    for name, cols in (("hitCyan", (P["cyanL"], P["cyan"])), ("hitWhite", (P["white"], P["grey"])), ("hitRed", (P["redL"], P["red"]))):
        frames = []
        for i in range(4):
            def f(d, img, i=i, cols=cols):
                c = 24
                r = 3 + i * 4
                for k in range(8):
                    a = k * 45 + (i % 2) * 22
                    d.line([polar((c, c), r * .4, a), polar((c, c), r, a)], fill=cols[0] if i < 2 else cols[1], width=2 if i < 2 else 1)
                if i == 0:
                    d.ellipse([c - 4, c - 4, c + 4, c + 4], fill=cols[0])
            frames.append(fx_cell(f))
        rows[name] = frames
    # Ground shockwave travelling along the floor (jumpable, red, toothed).
    wave = []
    for i in range(4):
        def f(d, img, i=i):
            base = 47
            h = 14 + (i % 2) * 4
            d.polygon([(4, base), (14, base - h), (22, base - h + 6), (30, base - h - 4), (44, base)], fill=P["redD"])
            d.polygon([(10, base), (16, base - h + 6), (24, base - h + 10), (30, base - h + 2), (38, base)], fill=P["red"])
            for k in range(3):
                x = 8 + k * 12 + i * 2
                d.line([(x, base - 2), (x + 3, base - h - 6 + k * 3)], fill=P["redL"])
        wave.append(outline(fx_cell(f)))
    rows["wave"] = wave
    # Falling corrupted block (hazard from GL1TCH: violet + red stripes).
    block = []
    for i in range(2):
        def f(d, img, i=i):
            stripes = canvas(28, 32)
            sd = ImageDraw.Draw(stripes)
            sd.rectangle([0, 0, 27, 31], fill=P["violetD"])
            for x in range(-24, 30, 8):
                sd.line([(x + i * 3, 31), (x + 20 + i * 3, 0)], fill=P["redD"], width=2)
            img.alpha_composite(stripes, (10, 8))
            d.rectangle([10, 8, 37, 39], outline=P["red"])
            d.rectangle([14, 14, 33, 33], outline=P["violet"])
            d.rectangle([20 - i, 20, 27 - i, 27], fill=P["red"])
        block.append(outline(fx_cell(f)))
    rows["block"] = block
    # Target reticle (warning, pulsing) and column warning.
    ret = []
    for i in range(2):
        def f(d, img, i=i):
            c, r = 24, 13 - i * 3
            d.arc([c - r, c - r, c + r, c + r], 20, 70, fill=P["red"], width=2)
            d.arc([c - r, c - r, c + r, c + r], 110, 160, fill=P["red"], width=2)
            d.arc([c - r, c - r, c + r, c + r], 200, 250, fill=P["red"], width=2)
            d.arc([c - r, c - r, c + r, c + r], 290, 340, fill=P["red"], width=2)
            d.rectangle([c - 1, c - 1, c + 1, c + 1], fill=P["redL"])
        ret.append(fx_cell(f))
    rows["reticle"] = ret
    col = []
    for i in range(2):
        def f(d, img, i=i):
            for y in range(0, FX, 8):
                d.polygon([(8, y + i * 4), (40, y + i * 4 - 8), (40, y + i * 4 - 4), (8, y + i * 4 + 4)], fill=(232, 67, 90, 150))
            d.line([(8, 0), (8, FX - 1)], fill=(232, 67, 90, 220))
            d.line([(39, 0), (39, FX - 1)], fill=(232, 67, 90, 220))
        col.append(fx_cell(f))
    rows["column"] = col
    # Blade slash arc (cyan).
    slash = []
    for i in range(4):
        def f(d, img, i=i):
            c = (14, 24)
            start = -80 + i * 30
            for w, colr in ((5, P["cyanD"]), (3, P["cyan"]), (1, P["cyanL"])):
                d.arc([c[0] - 22, c[1] - 22, c[0] + 22, c[1] + 22], start, start + 90 - i * 15, fill=colr, width=w)
        slash.append(fx_cell(f))
    rows["slash"] = slash
    # Dust puffs (landing / jump).
    dust = []
    for i in range(4):
        def f(d, img, i=i):
            for side in (-1, 1):
                x = 24 + side * (4 + i * 4)
                r = 4 - i // 2
                d.ellipse([x - r, 44 - r - i, x + r, 44 + r - i], fill=(143, 166, 181, 200 - i * 45))
        dust.append(fx_cell(f))
    rows["dust"] = dust
    # Code fragment: amber rhombus, rotating (width varies).
    shard = []
    for i in range(8):
        def f(d, img, i=i):
            c = 24
            w = max(1, round(abs(math.cos(i * math.pi / 8)) * 9))
            d.polygon([(c, 10), (c + w, 24), (c, 38), (c - w, 24)], fill=P["amber"])
            d.polygon([(c, 10), (c + w, 24), (c, 24)], fill=P["amberL"])
            d.polygon([(c, 38), (c - w, 24), (c, 24)], fill=P["amberD"])
        shard.append(outline(fx_cell(f)))
    rows["shard"] = shard
    # Checkpoint beacon: off, activating, on (cyan, rounded = safe).
    beacon = []
    for i, state in enumerate(["off", "on0", "on1", "on2"]):
        def f(d, img, state=state):
            d.rectangle([17, 30, 30, 47], fill=P["metal1"])
            d.rectangle([17, 30, 30, 31], fill=P["edge"])
            d.rectangle([20, 12, 27, 30], fill=P["metal0"])
            lit = state != "off"
            d.ellipse([16, 4, 31, 19], fill=P["cyan"] if lit else P["metal2"])
            d.ellipse([20, 8, 27, 15], fill=P["cyanL"] if lit else P["metal1"])
            if lit:
                k = int(state[-1])
                d.arc([10 - k * 2, -2 - k * 2, 37 + k * 2, 25 + k * 2], 200, 340, fill=P["cyan"], width=1)
            d.rectangle([21, 36, 26, 38], fill=P["cyan"] if lit else P["metal0"])
        beacon.append(outline(fx_cell(f)))
    rows["beacon"] = beacon
    # Terminal: idle (2), locked, open/solved.
    term = []
    for state in ["idle0", "idle1", "locked", "solved"]:
        def f(d, img, state=state):
            screen = {"idle0": P["cyan"], "idle1": P["cyanD"], "locked": P["red"], "solved": P["cyanL"]}[state]
            d.rectangle([12, 24, 35, 47], fill=P["metal1"])
            d.rectangle([12, 24, 35, 25], fill=P["edge"])
            d.polygon([(8, 4), (39, 4), (37, 24), (10, 24)], fill=P["metal0"])
            d.rectangle([12, 7, 35, 20], fill=P["outline"])
            if state.startswith("idle"):
                for k, y in enumerate(range(9, 19, 3)):
                    d.line([(14, y), (14 + (12, 18, 9, 15)[k] - (state == "idle1") * 3, y)], fill=screen)
            elif state == "locked":
                d.line([(18, 9), (29, 18)], fill=screen, width=2)
                d.line([(29, 9), (18, 18)], fill=screen, width=2)
            else:
                d.line([(16, 14), (21, 18), (31, 9)], fill=screen, width=2)
            d.rectangle([16, 30, 31, 33], fill=P["metal0"])
            d.point((18, 31), fill=screen)
            d.point((22, 31), fill=P["amberD"])
        term.append(outline(fx_cell(f)))
    rows["terminal"] = term
    # [E] key prompt icon.
    def key(d, img):
        d.rounded_rectangle([14, 14, 33, 33], 3, fill=P["deep2"], outline=P["cyan"])
        d.text((20, 16), "E", fill=P["white"])
    rows["keyE"] = [fx_cell(key)]
    # Warning triangle (UI / world).
    def warn(d, img):
        d.polygon([(24, 8), (42, 40), (6, 40)], fill=P["red"])
        d.polygon([(24, 14), (36, 37), (12, 37)], fill=P["outline"])
        d.rectangle([23, 20, 25, 30], fill=P["red"])
        d.rectangle([23, 33, 25, 35], fill=P["red"])
    rows["warn"] = [fx_cell(warn)]
    return rows


FX_ROWS = ["orb", "bolt", "hitCyan", "hitWhite", "hitRed", "wave", "block", "reticle", "column", "slash",
           "dust", "shard", "beacon", "terminal", "keyE", "warn"]


# ---------------------------------------------------------------- portal, firewall, door (64x128 cells)

def portal_frames():
    frames = []
    for state in ["closed", "open0", "open1", "open2", "open3"]:
        img = canvas(64, 128)
        d = ImageDraw.Draw(img)
        d.rectangle([4, 16, 13, 127], fill=P["metal1"])
        d.rectangle([50, 16, 59, 127], fill=P["metal1"])
        d.polygon([(4, 16), (59, 16), (52, 4), (11, 4)], fill=P["metal2"])
        d.line([(11, 4), (52, 4)], fill=P["edge"])
        for y in range(28, 120, 16):
            d.rectangle([6, y, 11, y + 3], fill=P["cyanD"] if state != "closed" else P["metal0"])
            d.rectangle([52, y, 57, y + 3], fill=P["cyanD"] if state != "closed" else P["metal0"])
        if state == "closed":
            d.rectangle([14, 17, 49, 127], fill=hx("141B24"))
            for x in range(14, 50, 6):
                d.line([(x, 17), (x, 127)], fill=hx("1C2531"))
            d.rectangle([26, 60, 37, 72], outline=P["redD"])
        else:
            k = int(state[-1])
            d.rectangle([14, 17, 49, 127], fill=hx("0D3A44"))
            for i in range(5):
                y = 17 + ((i * 24 + k * 6) % 110)
                d.rectangle([16, y, 47, y + 2], fill=P["cyanD"])
            d.rectangle([22, 17, 41, 127], fill=hx("1F7F8C"))
            d.rectangle([28, 17, 35, 127], fill=P["cyan"])
            d.line([(31, 17), (31, 127)], fill=P["cyanL"])
        frames.append(outline(img))
    return frames


def barrier_frames():
    """Firewall (red laser grid, hostile) and arena shutter (metal + stripes)."""
    frames = []
    for k in range(2):
        img = canvas(32, 32)
        d = ImageDraw.Draw(img)
        for x in (6, 15, 24):
            d.rectangle([x, 0, x + 1, 31], fill=P["red"] if (x // 9 + k) % 2 else P["redD"])
        for y in range(k * 4, 32, 8):
            d.line([(4, y), (27, y + 4)], fill=(232, 67, 90, 160))
        d.line([(15, 0), (15, 31)], fill=P["redL"])
        frames.append(img)
    shutter = canvas(32, 32)
    d = ImageDraw.Draw(shutter)
    d.rectangle([2, 0, 29, 31], fill=P["metal1"])
    d.line([(2, 0), (2, 31)], fill=P["edge"])
    d.line([(29, 0), (29, 31)], fill=P["outline"])
    for y in range(0, 32, 8):
        d.polygon([(4, y), (10, y), (27, y + 6), (21, y + 6)], fill=P["redD"])
    frames.append(shutter)
    emitter = canvas(32, 32)
    d = ImageDraw.Draw(emitter)
    d.rectangle([4, 8, 27, 31], fill=P["metal0"])
    d.rectangle([4, 8, 27, 10], fill=P["edge"])
    d.rectangle([12, 2, 19, 8], fill=P["metal1"])
    d.rectangle([14, 3, 17, 6], fill=P["red"])
    frames.append(outline(emitter))
    return frames


# ---------------------------------------------------------------- parallax (640x480, tileable)
PW, PH = 640, 480


def parallax_far():
    img = canvas(PW, PH)
    d = ImageDraw.Draw(img)
    for y in range(PH):
        t = y / PH
        c = tuple(int(a + (b - a) * t) for a, b in zip(hx("0E1823")[:3], hx("070C12")[:3]))
        d.line([(0, y), (PW, y)], fill=c + (255,))
    rng = random.Random(11)
    # Distant server towers: tall, regular, very low contrast.
    x = 0
    while x < PW:
        w = rng.randrange(36, 70)
        h = rng.randrange(180, 360)
        top = PH - h
        col = hx("101C28")
        d.rectangle([x, top, x + w, PH], fill=col)
        d.rectangle([x, top, x + w, top + 3], fill=hx("172636"))
        for wy in range(top + 14, PH - 20, 12):
            for wx in range(x + 6, x + w - 6, 9):
                if rng.random() < .12:
                    d.rectangle([wx, wy, wx + 2, wy + 1], fill=hx("1E4A55") if rng.random() < .8 else hx("3A2A55"))
        x += w + rng.randrange(14, 40)
    # Seam: ensure horizontal tiling by copying the left edge.
    left = img.crop((0, 0, 24, PH))
    img.paste(left, (PW - 24, 0))
    return img


def parallax_mid():
    img = canvas(PW, PH)
    d = ImageDraw.Draw(img)
    rng = random.Random(23)
    col, colL = hx("15212D"), hx("1D2C3A")
    # Cooling stacks and a long catwalk silhouette.
    for x in (40, 250, 470):
        w = rng.randrange(60, 90)
        top = rng.randrange(150, 230)
        d.rectangle([x, top, x + w, PH], fill=col)
        d.ellipse([x - 6, top - 10, x + w + 6, top + 12], fill=colL)
        for y in range(top + 30, PH, 40):
            d.line([(x, y), (x + w, y)], fill=hx("101A24"), width=3)
    d.rectangle([0, 300, PW, 308], fill=colL)
    for x in range(0, PW, 32):
        d.line([(x, 308), (x + 16, 330)], fill=col, width=2)
        d.line([(x + 32, 308), (x + 16, 330)], fill=col, width=2)
    d.rectangle([0, 330, PW, 334], fill=col)
    # Hanging cables.
    for x in range(20, PW, 120):
        pts = [(x + i * 10, 120 + int(math.sin(i / 3) * 6 + i * i * .6)) for i in range(10)]
        d.line(pts, fill=hx("0E161E"), width=2)
    d.rectangle([0, 420, PW, PH], fill=col)
    left = img.crop((0, 0, 30, PH))
    img.paste(left, (PW - 30, 0))
    return img


def parallax_near():
    img = canvas(PW, PH)
    d = ImageDraw.Draw(img)
    col = hx("1A2836")
    for x in (80, 400):
        d.rectangle([x, 0, x + 40, PH], fill=col)
        d.line([(x, 0), (x, PH)], fill=hx("223445"), width=2)
        for y in range(40, PH, 70):
            d.rectangle([x - 6, y, x + 46, y + 8], fill=hx("152230"))
    # Big horizontal duct near the ceiling.
    d.rectangle([0, 34, PW, 66], fill=col)
    d.line([(0, 36), (PW, 36)], fill=hx("26394B"), width=2)
    for x in range(0, PW, 80):
        d.rectangle([x, 30, x + 10, 70], fill=hx("152230"))
    return img


# ---------------------------------------------------------------- portraits for dialogue (48x48)

def portrait(kind, mood=0):
    img = canvas(48, 48)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, 47, 47], fill=P["deep2"])
    if kind == "bit":
        face = bit_frame(fin=2, eye=["open", "happy", "alert", "worried"][mood], glow=1)
        face = face.resize((48, 48), Image.NEAREST)
        img.alpha_composite(face)
        d.rectangle([0, 0, 47, 47], outline=P["cyanD"])
    elif kind == "glitch":
        rng = random.Random(5)
        for _ in range(26):
            x, y = rng.randrange(4, 40), rng.randrange(6, 40)
            d.rectangle([x, y, x + rng.randrange(2, 10), y + rng.randrange(1, 3)], fill=rng.choice([P["violet"], P["violetD"], P["red"]]))
        d.polygon([(12, 18), (20, 22), (12, 24)], fill=P["red"])
        d.polygon([(36, 18), (28, 22), (36, 24)], fill=P["red"])
        d.line([(14, 34), (20, 31), (26, 35), (34, 31)], fill=P["violet"], width=2)
        d.rectangle([0, 0, 47, 47], outline=P["violet"])
    elif kind == "guardian":
        g = guardian_frame(0, lids=.6, core=.3).crop((16, 10, 112, 106)).resize((48, 48), Image.NEAREST)
        img.alpha_composite(g)
        d.rectangle([0, 0, 47, 47], outline=P["redD"])
    return img


def main():
    zf = zero_frames()
    zero_sheet = canvas(ZW * 8, ZH * len(ZERO_ROWS))
    for r, name in enumerate(ZERO_ROWS):
        for c, f in enumerate(zf[name]):
            zero_sheet.alpha_composite(f, (c * ZW, r * ZH))
    zero_sheet.save(OUT / "zero.png")
    # Shoulder anchor per frame (pixels from the frame's top-left), read by Academic2DArt.
    lines = []
    for name in ZERO_ROWS:
        lines.append(name + ":" + ";".join("%d,%d" % f.info.get("shoulder", (25, 24)) for f in zf[name]))
    (OUT / "zero_shoulders.txt").write_text(chr(10).join(lines) + chr(10), encoding="utf-8")

    arms = canvas(48 * len(ARM_ANGLES), 96)
    for i, a in enumerate(ARM_ANGLES):
        arms.alpha_composite(zero_arm(a, "blaster"), (i * 48, 0))
        arms.alpha_composite(zero_arm(a, "blade"), (i * 48, 48))
    arms.save(OUT / "zero_arm.png")

    bf = bit_frames()
    bit_sheet = canvas(BW * 4, BW * len(BIT_ROWS))
    for r, name in enumerate(BIT_ROWS):
        for c, f in enumerate(bf[name]):
            bit_sheet.alpha_composite(f, (c * BW, r * BW))
    bit_sheet.save(OUT / "bit.png")

    gf = guardian_frames()
    g_rows = []
    for v in range(3):
        g_rows.append(gf["idle%d" % v] + gf["charge%d" % v] + gf["fire%d" % v] + gf["exposed%d" % v])  # 11
        g_rows.append(gf["raise%d" % v] + gf["slam%d" % v] + gf["flash%d" % v] + gf["glitch%d" % v])  # 7
    g_rows.append(gf["death"] + gf["dormant"])  # 9
    guardian_sheet = canvas(GW * 11, GW * len(g_rows))
    for r, row in enumerate(g_rows):
        for c, f in enumerate(row):
            guardian_sheet.alpha_composite(f, (c * GW, r * GW))
    guardian_sheet.save(OUT / "guardian.png")

    tile_list = tiles()
    tileset = sheet(tile_list, 8, (T, T))
    tileset.save(OUT / "tileset.png")

    ff = fx_frames()
    fx_sheet = canvas(FX * 8, FX * len(FX_ROWS))
    for r, name in enumerate(FX_ROWS):
        for c, f in enumerate(ff[name]):
            fx_sheet.alpha_composite(f, (c * FX, r * FX))
    fx_sheet.save(OUT / "fx.png")

    portal = sheet(portal_frames(), 5, (64, 128))
    portal.save(OUT / "portal.png")
    sheet(barrier_frames(), 4, (32, 32)).save(OUT / "barrier.png")
    parallax_far().save(OUT / "parallax_far.png")
    parallax_mid().save(OUT / "parallax_mid.png")
    parallax_near().save(OUT / "parallax_near.png")
    portraits = [portrait("bit", m) for m in range(4)] + [portrait("glitch"), portrait("guardian")]
    sheet(portraits, 6, (48, 48)).save(OUT / "portraits.png")

    # Review contact sheet (scaled 3x, on the deep background) for Docs/Arte.
    review = canvas(1400, 1500)
    rd = ImageDraw.Draw(review)
    rd.rectangle([0, 0, 1400, 1500], fill=P["deep"])
    y = 10
    for img, scale in ((zero_sheet, 2), (arms, 3), (bit_sheet, 3), (tileset, 3), (fx_sheet.crop((0, 0, 384, 48 * 8)), 1)):
        big = img.resize((img.width * scale, img.height * scale), Image.NEAREST)
        if big.width > 1380:
            big = big.crop((0, 0, 1380, big.height))
        review.alpha_composite(big, (10, y))
        y += big.height + 12
    review.save(DOCS / "hoja_sprites_2d.png")
    gsmall = guardian_sheet.resize((guardian_sheet.width // 2, guardian_sheet.height // 2), Image.NEAREST)
    bg = Image.new("RGBA", gsmall.size, P["deep"])
    bg.alpha_composite(gsmall)
    bg.save(DOCS / "hoja_guardian_2d.png")
    print("2D art written to", OUT)


if __name__ == "__main__":
    main()
