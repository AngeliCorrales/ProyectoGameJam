"""Synthesises every sound effect / ambience of the game as 16-bit mono WAV files."""
import os, sys, wave
import numpy as np

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)
SR = 44100
rng = np.random.default_rng(42)


def t_(sec): return np.arange(int(sec * SR)) / SR


def write(name, x, peak=0.9):
    x = np.asarray(x, np.float64)
    m = np.max(np.abs(x)) or 1
    x = x / m * peak
    d = (np.clip(x, -1, 1) * 32767).astype("<i2")
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(d.tobytes())


def fftconv(a, b):
    n = len(a) + len(b) - 1
    N = 1 << (n - 1).bit_length()
    return np.fft.irfft(np.fft.rfft(a, N) * np.fft.rfft(b, N), N)[:n]


def lowpass(x, cutoff):
    X = np.fft.rfft(x); f = np.fft.rfftfreq(len(x), 1 / SR)
    X *= 1 / (1 + (f / cutoff) ** 4)
    return np.fft.irfft(X, len(x))


def bandpass(x, lo, hi):
    X = np.fft.rfft(x); f = np.fft.rfftfreq(len(x), 1 / SR)
    X *= ((f > lo) & (f < hi))
    return np.fft.irfft(X, len(x))


def reverb(x, seconds=1.5, mix=0.35, decay=4.0):
    ir_t = t_(seconds)
    ir = rng.normal(0, 1, len(ir_t)) * np.exp(-decay * ir_t)
    ir = lowpass(ir, 5000)
    wet = fftconv(x, ir)
    wet /= np.max(np.abs(wet)) or 1
    dry = np.concatenate([x, np.zeros(len(wet) - len(x))])
    dry /= np.max(np.abs(dry)) or 1
    return dry * (1 - mix) + wet * mix


def loopify(x, fade=0.25):
    """Make a seamless loop by cross-fading the tail into the head."""
    n = int(fade * SR)
    head, tail = x[:n].copy(), x[-n:]
    w = np.linspace(0, 1, n)
    x = x[:-n].copy()
    x[:n] = head * w + tail * (1 - w)
    return x


def env_ad(n, a, d):
    t = np.arange(n) / SR
    return np.minimum(1, t / max(a, 1e-4)) * np.exp(-t / d)


# --- Ambience: fluorescent hum + room tone (seamless 4 s loop) ---
t = t_(4.25)
hum = (0.5 * np.sin(2 * np.pi * 60 * t) + 0.35 * np.sin(2 * np.pi * 120 * t) + 0.15 * np.sin(2 * np.pi * 180 * t)
       + 0.08 * np.sign(np.sin(2 * np.pi * 120 * t)) * 0.3)
room = lowpass(rng.normal(0, 1, len(t)), 400) * 0.6
buzz = bandpass(rng.normal(0, 1, len(t)), 3000, 7000) * 0.04 * (1 + 0.5 * np.sin(2 * np.pi * 120 * t))
write("amb_hum", loopify(hum * 0.5 + room + buzz), 0.6)

# --- Ambience: dark drone for tension (seamless 8 s loop) ---
t = t_(8.5)
drone = (np.sin(2 * np.pi * 41.0 * t) + np.sin(2 * np.pi * 41.5 * t) * 0.8 + 0.4 * np.sin(2 * np.pi * 82.25 * t)
         + 0.25 * np.sin(2 * np.pi * 123.0 * t + np.sin(2 * np.pi * 0.25 * t) * 2))
wind = bandpass(rng.normal(0, 1, len(t)), 150, 900) * (0.6 + 0.4 * np.sin(2 * np.pi * 0.125 * t)) * 1.5
write("amb_drone", loopify(drone * 0.5 + wind), 0.7)

# --- Heartbeat loop (~66 bpm) ---
t = t_(0.9)
def thump(t0, f, amp):
    tt = np.clip(t - t0, 0, None)
    return amp * np.sin(2 * np.pi * f * tt) * np.exp(-tt * 18) * (t >= t0)
hb = thump(0.0, 55, 1.0) + thump(0.22, 48, 0.7)
write("heartbeat", lowpass(hb, 200), 0.9)

# --- Footsteps on carpet (4 variations) ---
for i in range(4):
    t = t_(0.28)
    n = rng.normal(0, 1, len(t))
    s = lowpass(n, 700 + 150 * i) * env_ad(len(t), 0.004, 0.045) * 1.2
    s += np.sin(2 * np.pi * (70 + 8 * i) * t) * env_ad(len(t), 0.002, 0.03) * 0.8
    s += lowpass(rng.normal(0, 1, len(t)), 2500) * env_ad(len(t), 0.03, 0.05) * 0.25 * (t > 0.06)  # fabric scuff
    write(f"step_{i + 1}", s, 0.8)

# --- Light flicker crackle ---
t = t_(1.6)
gate = (np.sin(2 * np.pi * 9 * t + 3 * np.sin(2 * np.pi * 2.3 * t)) > 0.1).astype(float)
gate = lowpass(gate, 600)
crack = bandpass(rng.normal(0, 1, len(t)), 1500, 9000) * gate * 0.8
hum2 = np.sin(2 * np.pi * 120 * t) * gate * 0.6
pops = np.zeros(len(t))
for p in rng.integers(0, len(t) - 400, 9):
    pops[p:p + 300] += rng.normal(0, 1, 300) * np.exp(-np.arange(300) / 50)
write("light_flicker", (crack + hum2 + pops) * np.linspace(1, 0.6, len(t)), 0.8)

# --- Light dying pop ---
t = t_(0.6)
pop = rng.normal(0, 1, len(t)) * env_ad(len(t), 0.001, 0.02) + np.sin(2 * np.pi * 3200 * t) * env_ad(len(t), 0.001, 0.08) * 0.3
write("light_off", reverb(pop, 0.6, 0.3), 0.8)

# --- Correct report: soft bell ---
t = t_(1.4)
bell = sum(a * np.sin(2 * np.pi * f * t) * np.exp(-t * d) for f, a, d in
           ((523.25, 1, 2.5), (784, 0.6, 3.2), (1046.5, 0.4, 4), (1318.5, 0.2, 5)))
write("report_ok", reverb(bell * env_ad(len(t), 0.005, 10), 1.2, 0.3), 0.7)

# --- Wrong report: harsh buzz ---
t = t_(0.55)
sq = np.sign(np.sin(2 * np.pi * 92 * t)) + 0.6 * np.sign(np.sin(2 * np.pi * 97 * t))
write("report_wrong", lowpass(sq, 2500) * env_ad(len(t), 0.005, 0.35), 0.8)

# --- Anomaly cue: distant whisper + low thump (does NOT say where) ---
t = t_(2.6)
wh = bandpass(rng.normal(0, 1, len(t)), 1800, 6500)
syll = np.clip(np.sin(2 * np.pi * 3.1 * t + 2 * np.sin(2 * np.pi * 0.7 * t)), 0, 1) ** 2
form = bandpass(rng.normal(0, 1, len(t)), 600, 1400) * 0.4
cue = (wh + form) * syll * np.sin(np.pi * t / t[-1]) ** 2
cue += np.sin(2 * np.pi * 38 * t) * np.exp(-t * 3) * 1.5
write("anomaly_cue", reverb(cue, 2.0, 0.55, 2.5), 0.55)

# --- Glitch / static burst (used when an anomaly is reported and fixed) ---
t = t_(0.45)
st = rng.normal(0, 1, len(t))
st = np.round(st * 4) / 4                                   # bit-crush
st *= (np.sin(2 * np.pi * 30 * t) > -0.3) * env_ad(len(t), 0.002, 0.2)
write("static", st, 0.5)

# --- Door: locked rattle, unlock clunk, open creak ---
t = t_(0.7)
rat = np.zeros(len(t))
for k, p in enumerate((0.0, 0.09, 0.17, 0.3, 0.38)):
    i0 = int(p * SR); n = 2500
    rat[i0:i0 + n] += (bandpass(rng.normal(0, 1, n), 800, 5000) + np.sin(2 * np.pi * (900 + 80 * k) * np.arange(n) / SR)) * np.exp(-np.arange(n) / 350)
write("door_locked", reverb(rat, 0.5, 0.2), 0.8)

t = t_(0.8)
cl = (np.sin(2 * np.pi * 120 * t) + lowpass(rng.normal(0, 1, len(t)), 1500)) * env_ad(len(t), 0.001, 0.07)
cl += np.sin(2 * np.pi * 1800 * t) * env_ad(len(t), 0.001, 0.03) * 0.4
write("door_unlock", reverb(cl, 0.7, 0.3), 0.8)

t = t_(2.2)
f0 = 180 + 70 * np.sin(2 * np.pi * 0.6 * t) + 25 * np.sin(2 * np.pi * 7 * t)
ph = 2 * np.cumsum(f0) / SR * np.pi
saw = (np.mod(ph / (2 * np.pi), 1) * 2 - 1)
creak = bandpass(saw, 300, 3000) * (0.5 + 0.5 * np.abs(np.sin(2 * np.pi * 11 * t))) * np.sin(np.pi * t / t[-1])
write("door_open", reverb(creak, 1.0, 0.3), 0.8)

# --- Clock tick ---
t = t_(0.08)
tick = bandpass(rng.normal(0, 1, len(t)), 2000, 8000) * env_ad(len(t), 0.0005, 0.006) + np.sin(2 * np.pi * 2600 * t) * env_ad(len(t), 0.0005, 0.01) * 0.5
write("clock_tick", tick, 0.6)

# --- Jump-scare scream ---
t = t_(1.8)
sc = np.zeros(len(t))
for k, det in enumerate((0, 7, -11, 19, 31)):
    f = (520 + det) * (1 + 0.6 * np.exp(-t * 2)) + 30 * np.sin(2 * np.pi * (6 + k) * t)
    ph = 2 * np.pi * np.cumsum(f) / SR
    sc += np.mod(ph / (2 * np.pi), 1) * 2 - 1
sc += bandpass(rng.normal(0, 1, len(t)), 1000, 8000) * 3
sc += np.sin(2 * np.pi * 45 * t) * 4 * np.exp(-t * 2)
sc *= env_ad(len(t), 0.01, 0.9)
write("jumpscare", np.tanh(sc * 0.6), 0.98)

# --- Lose: deep boom with tail ---
t = t_(1.5)
boom = (np.sin(2 * np.pi * (60 * np.exp(-t * 1.5) + 28) * t) + lowpass(rng.normal(0, 1, len(t)), 300)) * env_ad(len(t), 0.005, 0.6)
write("lose", reverb(boom, 2.5, 0.45, 1.8), 0.9)

# --- Win: rising airy pad ---
t = t_(3.5)
pad = sum(np.sin(2 * np.pi * f * (1 + 0.04 * t) * t) * a for f, a in ((261.6, 1), (329.6, 0.7), (392, 0.6), (523.2, 0.4)))
pad *= np.sin(np.pi * t / t[-1]) ** 1.5
pad += bandpass(rng.normal(0, 1, len(t)), 3000, 10000) * 0.15 * np.sin(np.pi * t / t[-1])
write("win", reverb(pad, 2.0, 0.4), 0.7)

# --- UI ---
t = t_(0.12)
write("ui_click", np.sin(2 * np.pi * 880 * t) * env_ad(len(t), 0.001, 0.025) + bandpass(rng.normal(0, 1, len(t)), 2000, 6000) * env_ad(len(t), 0.0005, 0.004), 0.5)
t = t_(0.08)
write("ui_hover", np.sin(2 * np.pi * 1320 * t) * env_ad(len(t), 0.001, 0.015), 0.25)

print("audio ok")
