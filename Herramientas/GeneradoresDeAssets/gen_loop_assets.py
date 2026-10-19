"""Assets for the room-loop version: room number signs, 'ATRAS' exit sign, guard portrait, duck squeak."""
import os, sys, math, wave
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
R = sys.argv[1]; S = os.path.join(R, "Sprites"); A = os.path.join(R, "Audio"); F = os.path.join(R, "Fonts")
ELITE = os.path.join(F, "SpecialElite-Regular.ttf")
sys.argv = [sys.argv[0], S, F]
import importlib.util
spec = importlib.util.spec_from_file_location("gs", os.path.join(os.path.dirname(os.path.abspath(__file__)), "gen_sprites_lib.py"))

def font(sz): return ImageFont.truetype(ELITE, sz)
rng = np.random.default_rng(9)

def noise(im, amt=6):
    a = np.asarray(im.convert("RGB")).astype(np.float32) + rng.normal(0, amt, (im.height, im.width, 1))
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))

def room_sign(text, big, bg=(230, 222, 200), fg=(25, 25, 25)):
    im = Image.new("RGB", (512, 256), bg); d = ImageDraw.Draw(im)
    d.rectangle([8, 8, 503, 247], outline=fg, width=8)
    f = font(46); w = d.textlength(text, font=f); d.text(((512 - w) / 2, 26), text, font=f, fill=fg)
    f = font(130 if len(big) <= 2 else 74); w = d.textlength(big, font=f)
    d.text(((512 - w) / 2, 78 if len(big) <= 2 else 112), big, font=f, fill=(150, 20, 15) if big in ("?",) else fg)
    return noise(im)

for n in range(10):
    room_sign("CUARTO", str(n)).save(os.path.join(S, f"room_{n}.png"))
room_sign("ESTÁS EN LA", "SALIDA", (40, 120, 60), (240, 245, 235)).save(os.path.join(S, "room_exit.png"))

# Exit sign variant: 'ATRÁS' (rule change: the way out is behind you)
im = Image.new("RGB", (512, 176), (20, 140, 60)); d = ImageDraw.Draw(im); col = (230, 255, 235)
d.rectangle([6, 6, 505, 169], outline=col, width=6)
d.polygon([(60, 88), (120, 40), (120, 70), (170, 70), (170, 106), (120, 106), (120, 136)], fill=col)   # arrow pointing back/left
f = font(70); t = "ATRÁS"; w = d.textlength(t, font=f); d.text((190 + (300 - w) / 2, 50), t, font=f, fill=col)
im.save(os.path.join(S, "exit_sign_back.png"))

# Portrait of the guard (the player): navy uniform, cap, badge
W, H = 640, 440
t = np.linspace(0, 1, H)[:, None, None]
bgc = np.array([40, 35, 30]) * (1 - t) + np.array([20, 18, 15]) * t
im = Image.fromarray(np.repeat(bgc, W, 1).astype(np.uint8)); d = ImageDraw.Draw(im); cx = W // 2
d.ellipse([cx - 170, 300, cx + 170, 560], fill=(28, 38, 70))
d.rectangle([cx - 60, 380, cx - 30, 410], fill=(220, 180, 70))                       # badge
d.rectangle([cx - 28, 250, cx + 28, 310], fill=(200, 160, 130))
d.ellipse([cx - 80, 100, cx + 80, 290], fill=(210, 165, 135))
d.chord([cx - 92, 60, cx + 92, 190], 180, 360, fill=(28, 38, 70))                    # cap
d.rectangle([cx - 92, 120, cx + 92, 140], fill=(28, 38, 70))
d.polygon([(cx - 70, 140), (cx + 70, 140), (cx + 95, 162), (cx - 95, 162)], fill=(12, 12, 12))  # brim
d.rectangle([cx - 12, 88, cx + 12, 110], fill=(220, 180, 70))
for ex in (cx - 35, cx + 35):
    d.ellipse([ex - 13, 182, ex + 13, 198], fill=(235, 230, 220)); d.ellipse([ex - 6, 184, ex + 6, 196], fill=(30, 30, 30))
d.line([(cx - 22, 248), (cx + 22, 248)], fill=(130, 70, 60), width=4)
d.line([(cx, 195), (cx - 8, 230), (cx + 6, 232)], fill=(165, 120, 100), width=3)
im = im.filter(ImageFilter.GaussianBlur(1.1)).filter(ImageFilter.ModeFilter(7))
a = np.asarray(im).astype(np.float32) * np.array([1.0, 0.95, 0.82])
yy, xx = np.mgrid[0:H, 0:W]
a *= (1 - 0.35 * np.clip(np.hypot((xx - W / 2) / (W / 2), (yy - H / 2) / (H / 2)) - 0.4, 0, 1))[..., None]
Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(os.path.join(S, "painting_guard.png"))

# Duck squeak
SR = 44100; tt = np.arange(int(0.45 * SR)) / SR
f0 = 900 + 500 * np.sin(np.pi * tt / 0.45) - 300 * tt
ph = 2 * np.pi * np.cumsum(f0) / SR
x = (np.sign(np.sin(ph)) * 0.4 + np.sin(ph * 2) * 0.4) * np.sin(np.pi * tt / 0.45) ** 0.6
x += rng.normal(0, 0.05, len(tt)) * np.sin(np.pi * tt / 0.45)
x = x / np.max(np.abs(x)) * 0.8
with wave.open(os.path.join(A, "squeak.wav"), "wb") as w:
    w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((x * 32767).astype("<i2").tobytes())
print("ok")
