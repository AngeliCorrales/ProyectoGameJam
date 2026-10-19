"""Builds Assets/Resources/Textures: CC0 downloaded textures (resized) + procedural tileable ones."""
import os, sys
import numpy as np
from PIL import Image, ImageFilter

OUT = sys.argv[1]
BR = sys.argv[2]          # backrooms-textures/img
PH = sys.argv[3]          # poly haven chair textures dir
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(7)


def save(arr_or_img, name, size=None):
    im = arr_or_img if isinstance(arr_or_img, Image.Image) else Image.fromarray(np.clip(arr_or_img, 0, 255).astype(np.uint8))
    if size: im = im.resize((size, size), Image.LANCZOS)
    im.save(os.path.join(OUT, name), optimize=True)


# ---------- Downloaded CC0 textures (amini-allight/backrooms-textures, Poly Haven) ----------
def conv(src, dst, size=512, mode="RGB"):
    im = Image.open(src).convert(mode)
    save(im, dst, size)

conv(f"{BR}/backrooms-wall-diffuse.png", "wall_albedo.png")
conv(f"{BR}/backrooms-wall-normal.png", "wall_normal.png")
conv(f"{BR}/backrooms-carpet-diffuse.png", "carpet_albedo.png")
conv(f"{BR}/backrooms-carpet-normal.png", "carpet_normal.png")
conv(f"{BR}/backrooms-ceiling-tile-diffuse.png", "ceiling_albedo.png")
conv(f"{BR}/backrooms-ceiling-tile-normal.png", "ceiling_normal.png")
conv(f"{BR}/backrooms-ceiling-light-diffuse.png", "lamp_albedo.png", 256)
conv(f"{BR}/backrooms-ceiling-light-emission.png", "lamp_emission.png", 256)
conv(f"{PH}/plastic_monobloc_chair_01_diff_1k.jpg", "chair_albedo.png", 512)
conv(f"{PH}/plastic_monobloc_chair_01_nor_gl_1k.jpg", "chair_normal.png", 512)


# ---------- Procedural tileable textures ----------
def tile_noise(n, beta=2.0, seed=0):
    """Periodic fractal noise via random spectrum (tiles perfectly)."""
    r = np.random.default_rng(seed)
    fx = np.fft.fftfreq(n)[:, None]
    fy = np.fft.fftfreq(n)[None, :]
    f = np.sqrt(fx * fx + fy * fy); f[0, 0] = 1
    spec = (r.normal(size=(n, n)) + 1j * r.normal(size=(n, n))) / f ** (beta / 2)
    spec[0, 0] = 0
    img = np.real(np.fft.ifft2(spec))
    img -= img.min(); img /= img.max()
    return img


def tile_noise_aniso(n, sx, sy, seed=0):
    r = np.random.default_rng(seed)
    fx = np.fft.fftfreq(n)[:, None] * sx
    fy = np.fft.fftfreq(n)[None, :] * sy
    f = np.sqrt(fx * fx + fy * fy); f[0, 0] = 1
    spec = (r.normal(size=(n, n)) + 1j * r.normal(size=(n, n))) / f ** 1.1
    spec[0, 0] = 0
    img = np.real(np.fft.ifft2(spec)); img -= img.min(); img /= img.max()
    return img


def normal_from_height(h, strength=2.0):
    gy, gx = np.gradient(h)
    # wrap edges for tiling
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5
    nx, ny, nz = -gx * strength * 255 / 40, gy * strength * 255 / 40, np.ones_like(h)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    rgb = np.stack([nx / l, ny / l, nz / l], -1) * 0.5 + 0.5
    return (rgb * 255).astype(np.uint8)


N = 512
yy, xx = np.mgrid[0:N, 0:N] / N

# Wood (grain runs along U)
warp = tile_noise(N, 2.6, 11)
grain = tile_noise_aniso(N, 1.0, 14.0, 12)
rings = np.sin((yy * 9 + warp * 2.2 + grain * 0.6) * 2 * np.pi * 2) * 0.5 + 0.5
fine = tile_noise_aniso(N, 1.0, 40.0, 13)
h = rings * 0.55 + fine * 0.45
dark = np.array([58, 34, 20]); light = np.array([128, 82, 48])
wood = dark + (light - dark) * h[..., None]
wood *= (0.9 + 0.2 * tile_noise(N, 2.0, 14))[..., None]
save(wood, "wood_albedo.png"); save(normal_from_height(h, 1.2), "wood_normal.png")

# Dark painted door (wood + paint)
paint = np.array([62, 52, 44]) * (0.85 + 0.3 * (fine * 0.4 + rings * 0.2 + 0.4))[..., None]
save(paint, "door_albedo.png"); save(normal_from_height(h * 0.5, 0.8), "door_normal.png")

# Brushed metal (cabinets, handles)
brushed = tile_noise_aniso(N, 1.0, 60.0, 21) * 0.7 + tile_noise(N, 2.2, 22) * 0.3
metal = np.array([128, 130, 132]) * (0.75 + 0.35 * brushed)[..., None]
save(metal, "metal_albedo.png"); save(normal_from_height(brushed, 0.6), "metal_normal.png")

# Plastic (water cooler, monitor shell): subtle speckle
sp = tile_noise(N, 1.2, 31)
plastic = np.array([210, 207, 198]) * (0.92 + 0.12 * sp)[..., None]
save(plastic, "plastic_albedo.png")

# Dark plastic (monitor bezel, phone)
dplastic = np.array([30, 30, 32]) * (0.85 + 0.3 * sp)[..., None]
save(dplastic, "plastic_dark_albedo.png")

# Baseboard / skirting (dark varnished wood strip)
save(np.array([45, 30, 22]) * (0.8 + 0.4 * h)[..., None], "baseboard_albedo.png")

# Dirty floor for the exit room (white tiles)
tile = np.ones((N, N))
g = 8
lines = ((np.mod(xx * 4, 1) < 0.02) | (np.mod(yy * 4, 1) < 0.02)).astype(float)
tiles = np.array([228, 228, 222]) * (1 - lines * 0.35)[..., None] * (0.93 + 0.1 * tile_noise(N, 2.0, 41))[..., None]
save(tiles, "tiles_albedo.png"); save(normal_from_height(1 - lines, 1.5), "tiles_normal.png")

# Plant leaves (green mottled) and pot soil
leaf = np.array([40, 92, 38]) * (0.7 + 0.5 * tile_noise(N, 1.6, 51))[..., None]
save(leaf, "leaf_albedo.png")
pot = np.array([120, 62, 40]) * (0.8 + 0.3 * tile_noise(N, 2.2, 52))[..., None]
save(pot, "pot_albedo.png")

# Water (cooler bottle) – light blue
save(np.array([120, 170, 210]) * (0.9 + 0.15 * tile_noise(N, 1.8, 61))[..., None], "water_albedo.png")

print("textures ok")
