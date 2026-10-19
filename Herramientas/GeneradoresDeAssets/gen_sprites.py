"""Generates every 2D image the game uses (paintings, posters, signs, screens, decals, UI)."""
import os, sys, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops, ImageEnhance

OUT = sys.argv[1]
FONTS = sys.argv[2]
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(3)
ELITE = os.path.join(FONTS, "SpecialElite-Regular.ttf")
CREEP = os.path.join(FONTS, "Creepster-Regular.ttf")


def font(path, size): return ImageFont.truetype(path, size)


def save(im, name): im.save(os.path.join(OUT, name), optimize=True)


def noise_img(w, h, amt, seed=0):
    r = np.random.default_rng(seed)
    return r.normal(0, amt, (h, w, 1))


def add_noise(im, amt=8, seed=0):
    a = np.asarray(im.convert("RGB")).astype(np.float32)
    a += noise_img(im.width, im.height, amt, seed)
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))


def oil(im, seed=0):
    """Painterly look: mode filter + brush noise + canvas weave + yellow varnish + vignette."""
    im = im.filter(ImageFilter.GaussianBlur(1.2)).filter(ImageFilter.ModeFilter(7))
    a = np.asarray(im).astype(np.float32)
    h, w = a.shape[:2]
    r = np.random.default_rng(seed)
    strokes = Image.fromarray((r.random((h // 4, w // 4)) * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)
    strokes = strokes.filter(ImageFilter.GaussianBlur(2))
    s = (np.asarray(strokes).astype(np.float32) / 255 - 0.5)[..., None]
    a *= 1 + s * 0.18
    yy, xx = np.mgrid[0:h, 0:w]
    weave = (np.sin(xx * 1.6) * np.sin(yy * 1.6))[..., None] * 5
    a += weave
    a = a * np.array([1.0, 0.95, 0.80])          # old varnish
    d = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2)
    a *= (1 - 0.35 * np.clip(d - 0.4, 0, 1))[..., None]
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))


def grad(w, h, top, bottom):
    t = np.linspace(0, 1, h)[:, None, None]
    a = np.array(top)[None, None] * (1 - t) + np.array(bottom)[None, None] * t
    return Image.fromarray(np.repeat(a, w, 1).astype(np.uint8))


W, H = 640, 440   # paintings (frame 1.3 x 0.9 m)

# ---------------- Painting 1: seascape (target of "moved" anomaly) ----------------
im = grad(W, H, (70, 90, 120), (190, 170, 140))
d = ImageDraw.Draw(im)
d.rectangle([0, 250, W, H], fill=(40, 62, 80))
for i in range(40):
    y = 255 + i * 5
    d.line([(rng.integers(0, W), y), (rng.integers(0, W), y + rng.integers(-2, 3))], fill=(70, 95, 110), width=2)
d.ellipse([470, 120, 540, 190], fill=(235, 210, 160))
d.polygon([(250, 240), (330, 240), (315, 258), (262, 258)], fill=(50, 30, 20))
d.line([(290, 240), (290, 160)], fill=(60, 40, 25), width=4)
d.polygon([(292, 165), (292, 235), (340, 232)], fill=(220, 210, 190))
d.polygon([(0, 250), (0, 200), (120, 230), (200, 250)], fill=(60, 70, 60))
save(oil(im, 1), "painting_sea.png")

# ---------------- Painting 2: landscape + version with a figure ----------------
def landscape(figure):
    im = grad(W, H, (120, 130, 140), (210, 190, 150))
    d = ImageDraw.Draw(im)
    d.polygon([(0, 260), (160, 200), (330, 240), (480, 190), (W, 230), (W, H), (0, H)], fill=(85, 105, 70))
    d.polygon([(0, 320), (200, 290), (400, 330), (W, 300), (W, H), (0, H)], fill=(70, 92, 55))
    d.polygon([(300, H), (340, 290), (352, 290), (420, H)], fill=(150, 130, 100))      # path
    d.rectangle([110, 200, 122, 300], fill=(60, 40, 30))                                 # tree trunk
    d.ellipse([60, 120, 175, 230], fill=(50, 75, 45))
    d.rectangle([470, 230, 540, 275], fill=(170, 160, 140)); d.polygon([(462, 232), (505, 200), (548, 232)], fill=(110, 50, 40))
    d.rectangle([497, 250, 510, 275], fill=(50, 40, 35))
    if figure:
        # tall dark silhouette standing on the path, facing the viewer
        d.ellipse([336, 236, 352, 254], fill=(15, 12, 12))
        d.polygon([(333, 254), (355, 254), (360, 320), (328, 320)], fill=(15, 12, 12))
        d.line([(332, 258), (322, 312)], fill=(15, 12, 12), width=4)
        d.line([(356, 258), (366, 312)], fill=(15, 12, 12), width=4)
        d.rectangle([333, 318, 341, 352], fill=(15, 12, 12)); d.rectangle([347, 318, 355, 352], fill=(15, 12, 12))
        d.point([(341, 244), (348, 244)], fill=(255, 240, 230))
    return oil(im, 2)

save(landscape(False), "painting_landscape.png")
save(landscape(True), "painting_landscape_anomaly.png")

# ---------------- Painting 3: portrait + version with hollow eyes ----------------
def portrait(anomaly):
    im = grad(W, H, (40, 35, 30), (20, 18, 15))
    d = ImageDraw.Draw(im)
    cx = W // 2
    d.ellipse([cx - 170, 300, cx + 170, 560], fill=(25, 25, 32))                       # shoulders
    d.polygon([(cx - 25, 300), (cx + 25, 300), (cx, 360)], fill=(220, 215, 200))         # collar
    d.rectangle([cx - 28, 250, cx + 28, 310], fill=(200, 170, 145))                     # neck
    d.ellipse([cx - 80, 90, cx + 80, 290], fill=(214, 182, 156))                        # face
    d.chord([cx - 88, 70, cx + 88, 210], 180, 360, fill=(45, 30, 22))                   # hair
    d.line([(cx - 50, 160), (cx - 20, 156)], fill=(60, 40, 30), width=4)
    d.line([(cx + 20, 156), (cx + 50, 160)], fill=(60, 40, 30), width=4)
    if not anomaly:
        for ex in (cx - 35, cx + 35):
            d.ellipse([ex - 14, 172, ex + 14, 188], fill=(235, 230, 220))
            d.ellipse([ex - 6, 174, ex + 6, 186], fill=(70, 50, 35))
        d.line([(cx - 25, 245), (cx + 25, 245)], fill=(140, 80, 70), width=4)
    else:
        for ex in (cx - 35, cx + 35):
            d.ellipse([ex - 17, 166, ex + 17, 194], fill=(5, 3, 3))
            d.ellipse([ex + 2, 177, ex + 8, 183], fill=(200, 20, 15))                     # pupils turned to the side
            d.line([(ex, 194), (ex - 2, 260)], fill=(90, 10, 10), width=3)                # tears
        d.ellipse([cx - 20, 232, cx + 20, 272], fill=(20, 5, 5))                          # open mouth
    d.line([(cx, 185), (cx - 8, 222), (cx + 6, 224)], fill=(170, 130, 110), width=3)
    return oil(im, 3)

save(portrait(False), "painting_portrait.png")
save(portrait(True), "painting_portrait_anomaly.png")


# ---------------- Posters (0.6 x 0.85 m) ----------------
PW, PH = 480, 680

def poster(lines, bg, fg, crown=True, fnt=ELITE, sizes=None, accent=None, seed=0):
    im = Image.new("RGB", (PW, PH), bg)
    d = ImageDraw.Draw(im)
    d.rectangle([14, 14, PW - 15, PH - 15], outline=fg, width=4)
    y = 70
    if crown:
        d.polygon([(190, 150), (200, 90), (225, 125), (240, 75), (255, 125), (280, 90), (290, 150)], fill=fg)
        d.rectangle([190, 150, 290, 165], fill=fg)
        y = 210
    for i, t in enumerate(lines):
        s = sizes[i] if sizes else 64
        f = font(fnt, s)
        w = d.textlength(t, font=f)
        d.text(((PW - w) / 2, y), t, font=f, fill=accent if (accent and i == len(lines) - 1) else fg)
        y += s + 26
    im = add_noise(im, 7, seed)
    # paper wear
    a = np.asarray(im).astype(np.float32)
    yy, xx = np.mgrid[0:PH, 0:PW]
    a *= (1 - 0.35 * np.clip(np.hypot((xx - PW / 2) / PW, (yy - PH / 2) / PH) * 2 - 0.55, 0, 1))[..., None]
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))

save(poster(["MANTÉN", "LA CALMA", "Y SIGUE", "TRABAJANDO"], (150, 30, 30), (240, 225, 200), seed=1), "poster_calm.png")
save(poster(["NO", "MIRES", "DOS", "VECES"], (20, 10, 10), (200, 25, 20), crown=False, fnt=CREEP,
            sizes=[110, 110, 110, 110], seed=2), "poster_calm_anomaly.png")

def safety(days, red):
    im = Image.new("RGB", (PW, PH), (235, 225, 190))
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, PW, 150], fill=(30, 110, 50))
    f = font(ELITE, 54); t = "SEGURIDAD"; d.text(((PW - d.textlength(t, font=f)) / 2, 45), t, font=f, fill=(250, 250, 240))
    f = font(ELITE, 40)
    for i, t in enumerate(["DÍAS SIN", "INCIDENTES"]):
        d.text(((PW - d.textlength(t, font=f)) / 2, 200 + i * 52), t, font=f, fill=(40, 40, 40))
    d.rectangle([90, 340, PW - 90, 560], fill=(250, 250, 245), outline=(40, 40, 40), width=6)
    f = font(ELITE, 150)
    d.text(((PW - d.textlength(days, font=f)) / 2, 360), days, font=f, fill=(170, 20, 15) if red else (40, 40, 40))
    f = font(ELITE, 26); t = "Gracias por su atención."
    d.text(((PW - d.textlength(t, font=f)) / 2, 600), t, font=f, fill=(60, 60, 60))
    return add_noise(im, 6, 5)

save(safety("127", False), "poster_safety.png")
save(safety("0", True), "poster_safety_anomaly.png")


# ---------------- Exit sign ----------------
def exit_sign(text, col, bg):
    im = Image.new("RGB", (512, 176), bg)
    d = ImageDraw.Draw(im)
    d.rectangle([6, 6, 505, 169], outline=col, width=6)
    # running man pictogram
    x0, y0 = 40, 30
    d.ellipse([x0 + 40, y0, x0 + 64, y0 + 24], fill=col)
    d.line([(x0 + 50, y0 + 26), (x0 + 38, y0 + 80)], fill=col, width=12)
    d.line([(x0 + 38, y0 + 80), (x0 + 60, y0 + 116)], fill=col, width=10)
    d.line([(x0 + 38, y0 + 80), (x0 + 10, y0 + 110)], fill=col, width=10)
    d.line([(x0 + 48, y0 + 40), (x0 + 76, y0 + 62)], fill=col, width=9)
    d.line([(x0 + 48, y0 + 40), (x0 + 20, y0 + 50)], fill=col, width=9)
    f = font(ELITE, 70 if len(text) < 8 else 56)
    w = d.textlength(text, font=f)
    d.text((150 + (340 - w) / 2, 50), text, font=f, fill=col)
    return im

save(exit_sign("SALIDA", (230, 255, 235), (20, 140, 60)), "exit_sign.png")
save(exit_sign("NO SALGAS", (255, 225, 220), (150, 15, 10)), "exit_sign_anomaly.png")


# ---------------- Monitor screens ----------------
def screen_off():
    im = grad(512, 320, (14, 18, 20), (6, 8, 9))
    d = ImageDraw.Draw(im)
    d.polygon([(0, 0), (180, 0), (60, 320), (0, 320)], fill=(22, 27, 30))  # reflection
    return add_noise(im, 3, 9)

def screen_on(text):
    im = Image.new("RGB", (512, 320), (8, 25, 12))
    d = ImageDraw.Draw(im)
    for y in range(0, 320, 4): d.line([(0, y), (512, y)], fill=(5, 15, 8))
    # crude face
    d.ellipse([186, 40, 326, 200], outline=(80, 255, 120), width=4)
    d.ellipse([215, 90, 245, 120], fill=(80, 255, 120)); d.ellipse([267, 90, 297, 120], fill=(80, 255, 120))
    d.arc([220, 130, 292, 185], 200, 340, fill=(80, 255, 120), width=4)
    f = font(ELITE, 78); w = d.textlength(text, font=f)
    d.text(((512 - w) / 2, 212), text, font=f, fill=(150, 255, 170))
    a = np.asarray(im).astype(np.float32)
    for _ in range(5):                       # a few glitch bands (keep text readable)
        y = int(rng.choice([rng.integers(0, 35), rng.integers(200, 215), rng.integers(290, 316)])); hgt = rng.integers(2, 6); sh = rng.integers(-25, 25)
        a[y:y + hgt] = np.roll(a[y:y + hgt], sh, axis=1)
    return add_noise(Image.fromarray(a.astype(np.uint8)), 10, 10)

save(screen_off(), "monitor_off.png")
save(screen_on("TE VEO"), "monitor_anomaly.png")


# ---------------- Clock face (RGBA, round) ----------------
S = 512
im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(im)
d.ellipse([4, 4, S - 5, S - 5], fill=(30, 30, 30, 255))
d.ellipse([26, 26, S - 27, S - 27], fill=(238, 232, 215, 255))
f = font(ELITE, 56)
for h in range(1, 13):
    ang = math.radians(h * 30 - 90)
    t = str(h); tw = d.textlength(t, font=f)
    x = S / 2 + math.cos(ang) * 180 - tw / 2; y = S / 2 + math.sin(ang) * 180 - 34
    d.text((x, y), t, font=f, fill=(30, 30, 30, 255))
for m in range(60):
    ang = math.radians(m * 6)
    r0 = 222 if m % 5 else 210
    d.line([(S / 2 + math.cos(ang) * r0, S / 2 + math.sin(ang) * r0), (S / 2 + math.cos(ang) * 228, S / 2 + math.sin(ang) * 228)],
           fill=(30, 30, 30, 255), width=3 if m % 5 else 6)
save(im, "clock_face.png")


# ---------------- Door number plates ----------------
for n in ("101", "102", "103", "104"):
    im = Image.new("RGB", (256, 112), (190, 165, 90))
    d = ImageDraw.Draw(im)
    d.rectangle([4, 4, 251, 107], outline=(90, 70, 30), width=4)
    f = font(ELITE, 70); w = d.textlength(n, font=f)
    d.text(((256 - w) / 2, 14), n, font=f, fill=(40, 30, 15))
    save(add_noise(im, 6, int(n)), f"door_plate_{n}.png")
im = Image.new("RGB", (256, 112), (120, 20, 15)); d = ImageDraw.Draw(im)
f = font(ELITE, 70); w = d.textlength("666", font=f); d.text(((256 - w) / 2, 14), "666", font=f, fill=(20, 5, 5))
save(add_noise(im, 6, 66), "door_plate_anomaly.png")


# ---------------- Handprints decal (RGBA) ----------------
def hand(d, x, y, s, rot, col):
    pts = []
    palm = [(-0.35, 0), (0.35, 0), (0.4, 0.55), (-0.4, 0.55)]
    def tr(px, py):
        c, sn = math.cos(rot), math.sin(rot)
        return (x + (px * c - py * sn) * s, y + (px * sn + py * c) * s)
    d.polygon([tr(*p) for p in palm], fill=col)
    d.ellipse([tr(-0.42, 0.25)[0] - 0.42 * s, tr(-0.42, 0.25)[1] - 0.32 * s, tr(0.42, 0.25)[0] + 0.0 * s, tr(0.42, 0.25)[1] + 0.35 * s], fill=col)
    for fx, ln in ((-0.3, 0.55), (-0.1, 0.65), (0.1, 0.62), (0.28, 0.5)):
        a = tr(fx, 0); b = tr(fx * 1.1, -ln)
        d.line([a, b], fill=col, width=int(0.17 * s))
        d.ellipse([b[0] - 0.085 * s, b[1] - 0.085 * s, b[0] + 0.085 * s, b[1] + 0.085 * s], fill=col)
    a = tr(0.38, 0.35); b = tr(0.75, 0.05)
    d.line([a, b], fill=col, width=int(0.18 * s))

im = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
d = ImageDraw.Draw(im)
col = (110, 12, 10, 235)
for (x, y, s, r) in ((150, 200, 120, -0.2), (330, 150, 115, 0.25), (260, 360, 125, 0.05), (420, 330, 100, 0.4), (100, 400, 95, -0.5)):
    hand(d, x, y, s, r, col)
im = im.filter(ImageFilter.GaussianBlur(1.5))
a = np.asarray(im).astype(np.float32)
a[..., 3] *= np.clip(0.6 + rng.random((512, 512)) * 0.6, 0, 1)
for _ in range(10):  # drips
    x = rng.integers(60, 460); y0 = rng.integers(150, 380); ln = rng.integers(30, 120)
    a[y0:y0 + ln, x:x + 4] = [110, 12, 10, 220]
save(Image.fromarray(a.astype(np.uint8)), "handprints.png")


# ---------------- UI ----------------
im = Image.new("RGBA", (64, 64), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
d.ellipse([26, 26, 38, 38], fill=(0, 0, 0, 140)); d.ellipse([28, 28, 36, 36], fill=(255, 255, 255, 230))
save(im, "ui_crosshair.png")

im = Image.new("RGBA", (128, 128), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
d.ellipse([8, 34, 120, 94], fill=(0, 0, 0, 120))
d.chord([10, 30, 118, 98], 180, 360, fill=(240, 235, 225, 255)); d.chord([10, 30, 118, 98], 0, 180, fill=(240, 235, 225, 255))
d.ellipse([44, 44, 84, 84], fill=(160, 20, 20, 255)); d.ellipse([56, 56, 72, 72], fill=(10, 5, 5, 255))
save(im, "ui_eye.png")

S = 512; yy, xx = np.mgrid[0:S, 0:S]
r = np.hypot((xx - S / 2) / (S / 2), (yy - S / 2) / (S / 2))
alpha = np.clip((r - 0.55) / 0.75, 0, 1) ** 1.6 * 255
v = np.zeros((S, S, 4), np.uint8); v[..., 3] = alpha.astype(np.uint8)
save(Image.fromarray(v), "ui_vignette.png")

g = rng.integers(0, 255, (256, 256)).astype(np.uint8)
gi = np.zeros((256, 256, 4), np.uint8); gi[..., :3] = g[..., None]; gi[..., 3] = 26
save(Image.fromarray(gi), "ui_grain.png")

# Jump-scare face
S = 1024
im = Image.new("RGB", (S, S), (0, 0, 0)); d = ImageDraw.Draw(im)
d.ellipse([230, 110, 794, 960], fill=(196, 190, 176))
d.ellipse([330, 330, 480, 520], fill=(0, 0, 0)); d.ellipse([544, 330, 694, 520], fill=(0, 0, 0))
d.ellipse([395, 410, 420, 435], fill=(255, 40, 30)); d.ellipse([604, 410, 629, 435], fill=(255, 40, 30))
d.polygon([(512, 520), (480, 640), (545, 640)], fill=(120, 110, 100))
d.ellipse([400, 690, 624, 930], fill=(10, 0, 0))
for i in range(9):
    x = 420 + i * 23
    d.polygon([(x, 700), (x + 18, 700), (x + 9, 760)], fill=(210, 200, 180))
for _ in range(60):  # cracks
    x, y = rng.integers(250, 780), rng.integers(140, 940)
    d.line([(x, y), (x + rng.integers(-60, 60), y + rng.integers(-60, 60))], fill=(80, 70, 60), width=2)
im = im.filter(ImageFilter.GaussianBlur(2))
a = np.asarray(im).astype(np.float32)
yy, xx = np.mgrid[0:S, 0:S]
a *= (1 - np.clip(np.hypot(xx - S / 2, yy - S / 2) / (S * 0.62), 0, 1))[..., None] ** 0.6
a += rng.normal(0, 18, a.shape)
a[..., 1:] *= 0.88
save(Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)), "jumpscare.png")

# Title background: one-point perspective of a dim corridor with a figure at the end
Wt, Ht = 1600, 900
im = Image.new("RGB", (Wt, Ht), (0, 0, 0)); d = ImageDraw.Draw(im)
vx, vy = Wt / 2, Ht * 0.48
def lerp(a, b, t): return a + (b - a) * t
for k in range(14, 0, -1):
    t = k / 14
    x0, x1 = lerp(vx - 60, -200, t), lerp(vx + 60, Wt + 200, t)
    y0, y1 = lerp(vy - 40, -150, t), lerp(vy + 45, Ht + 160, t)
    shade = int(lerp(20, 95, t))
    d.polygon([(x0, y1), (x1, y1), (lerp(vx + 60, Wt + 200, t - 1 / 14), lerp(vy + 45, Ht + 160, t - 1 / 14)),
               (lerp(vx - 60, -200, t - 1 / 14), lerp(vy + 45, Ht + 160, t - 1 / 14))], fill=(shade // 2, shade // 2, shade // 4))
    d.polygon([(x0, y0), (x0, y1), (lerp(vx - 60, -200, t - 1 / 14), lerp(vy + 45, Ht + 160, t - 1 / 14)),
               (lerp(vx - 60, -200, t - 1 / 14), lerp(vy - 40, -150, t - 1 / 14))], fill=(shade, int(shade * 0.95), shade // 2))
    d.polygon([(x1, y0), (x1, y1), (lerp(vx + 60, Wt + 200, t - 1 / 14), lerp(vy + 45, Ht + 160, t - 1 / 14)),
               (lerp(vx + 60, Wt + 200, t - 1 / 14), lerp(vy - 40, -150, t - 1 / 14))], fill=(int(shade * 0.85), int(shade * 0.8), shade // 2))
    lx = (x0 + x1) / 2
    d.rectangle([lx - 80 * t, y0 + 4, lx + 80 * t, y0 + 4 + 14 * t], fill=(int(200 * t + 40),) * 2 + (int(150 * t + 30),))
d.rectangle([vx - 60, vy - 40, vx + 60, vy + 45], fill=(4, 4, 4))
d.ellipse([vx - 7, vy - 22, vx + 7, vy - 8], fill=(0, 0, 0)); d.rectangle([vx - 8, vy - 8, vx + 8, vy + 40], fill=(0, 0, 0))
im = im.filter(ImageFilter.GaussianBlur(3))
im = add_noise(im, 10, 77)
a = np.asarray(im).astype(np.float32)
yy, xx = np.mgrid[0:Ht, 0:Wt]
a *= (1 - np.clip(np.hypot((xx - Wt / 2) / Wt, (yy - Ht / 2) / Ht) * 1.6 - 0.2, 0, 1))[..., None]
save(Image.fromarray(np.clip(a * 0.8, 0, 255).astype(np.uint8)), "title_background.png")

print("sprites ok")
