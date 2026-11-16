"""Nivel Cero audio generator: original synthesized music, ambience and effects.

Run:  python -I Tools/Audio/generate_audio.py
Requires only numpy. Writes 16-bit PCM WAV files into
Unity/Assets/NivelCero/Resources/Audio/. No samples, loops or presets from third
parties are used; every waveform is computed here (see Docs/Arte/AUDIO.md).
Deterministic: fixed random seeds, so the output is reproducible.
"""
from pathlib import Path
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Unity/Assets/NivelCero/Resources/Audio"
OUT.mkdir(parents=True, exist_ok=True)
SR = 44100
RNG = np.random.default_rng(1977)


# ----------------------------------------------------------------- primitives

def t_axis(seconds):
    return np.arange(int(SR * seconds)) / SR


def phase_from_freq(freq):
    """Integrate an instantaneous-frequency array into phase (radians)."""
    return 2 * np.pi * np.cumsum(freq) / SR


def sine(freq, seconds=None):
    if np.isscalar(freq):
        return np.sin(2 * np.pi * freq * t_axis(seconds))
    return np.sin(phase_from_freq(freq))


def saw(freq, seconds=None, harmonics=24):
    """Band-limited additive sawtooth (no aliasing)."""
    n = int(SR * seconds) if np.isscalar(freq) else len(freq)
    f = np.full(n, freq) if np.isscalar(freq) else freq
    ph = phase_from_freq(f)
    out = np.zeros(n)
    for k in range(1, harmonics + 1):
        mask = (f * k) < SR * .45
        out += mask * np.sin(ph * k) / k
    return out * .6


def square(freq, seconds=None, harmonics=15):
    n = int(SR * seconds) if np.isscalar(freq) else len(freq)
    f = np.full(n, freq) if np.isscalar(freq) else freq
    ph = phase_from_freq(f)
    out = np.zeros(n)
    for k in range(1, harmonics * 2, 2):
        mask = (f * k) < SR * .45
        out += mask * np.sin(ph * k) / k
    return out * .8


def tri(freq, seconds=None):
    n = int(SR * seconds) if np.isscalar(freq) else len(freq)
    f = np.full(n, freq) if np.isscalar(freq) else freq
    ph = phase_from_freq(f)
    return (2 / np.pi) * np.arcsin(np.sin(ph))


def noise(seconds):
    return RNG.uniform(-1, 1, int(SR * seconds))


def env(n, a=.005, d=.1, s=.0, r=.05, hold=0.0):
    """ADSR in seconds; returns array length n."""
    out = np.zeros(n)
    ia, idd, ih = int(a * SR), int(d * SR), int(hold * SR)
    ir = int(r * SR)
    i = 0
    seg = min(ia, n - i); out[i:i + seg] = np.linspace(0, 1, seg, endpoint=False) if seg > 0 else 0; i += seg
    seg = min(ih, n - i); out[i:i + seg] = 1; i += seg
    seg = min(idd, n - i); out[i:i + seg] = np.linspace(1, s, seg, endpoint=False) if seg > 0 else 0; i += seg
    sustain_end = max(i, n - ir)
    out[i:sustain_end] = s
    seg = n - sustain_end
    if seg > 0:
        start = out[sustain_end - 1] if sustain_end > 0 else s
        out[sustain_end:] = np.linspace(start, 0, seg)
    return out


def expdecay(n, tau):
    return np.exp(-np.arange(n) / (SR * tau))


def fft_filter(x, lo=None, hi=None, slope=2):
    """Static zero-phase band filter (smooth Butterworth-like magnitude)."""
    n = len(x)
    spec = np.fft.rfft(x)
    f = np.fft.rfftfreq(n, 1 / SR)
    g = np.ones_like(f)
    if hi:
        g *= 1 / np.sqrt(1 + (f / hi) ** (2 * slope))
    if lo:
        g *= 1 / np.sqrt(1 + (lo / np.maximum(f, 1e-3)) ** (2 * slope))
    return np.fft.irfft(spec * g, n)


def sweep_lp(x, f_start, f_end, curve=1.0, res=0.0):
    """Time-varying one-pole (+ optional resonance via 2-pole) low-pass."""
    n = len(x)
    fc = f_start * (f_end / f_start) ** (np.linspace(0, 1, n) ** curve)
    y = np.zeros(n)
    a1 = a2 = 0.0
    for i in range(n):
        g = 1 - np.exp(-2 * np.pi * fc[i] / SR)
        a1 += g * (x[i] - a1 + res * (a1 - a2))
        a2 += g * (a1 - a2)
        y[i] = a2
    return y


def bandpass_noise(seconds, center, width):
    return fft_filter(noise(seconds), lo=center - width / 2, hi=center + width / 2, slope=3)


def reverb(x, seconds=1.4, mix=.25, damp=4000, pre=.012):
    n_ir = int(SR * seconds)
    ir = RNG.normal(0, 1, n_ir) * np.exp(-np.arange(n_ir) / (SR * seconds / 5))
    ir = fft_filter(ir, hi=damp)
    ir[:int(pre * SR)] = 0
    ir /= np.sqrt(np.sum(ir ** 2)) + 1e-9
    n = len(x) + n_ir
    size = 1 << (n - 1).bit_length()
    wet = np.fft.irfft(np.fft.rfft(x, size) * np.fft.rfft(ir, size), size)[:len(x) + n_ir]
    dry = np.concatenate([x, np.zeros(n_ir)])
    return dry * (1 - mix) + wet * mix * .6


def loop_reverb(x, seconds=2.0, mix=.3, damp=3500):
    """Reverb whose tail wraps to the loop start: seamless loops."""
    y = reverb(x, seconds, mix, damp)
    n = len(x)
    out = y[:n].copy()
    tail = y[n:]
    out[:len(tail)] += tail
    return out


def softclip(x, drive=1.0):
    return np.tanh(x * drive) / np.tanh(drive)


def bitcrush(x, bits=6, hold=4):
    q = 2 ** bits
    y = np.round(x * q) / q
    return np.repeat(y[::hold], hold)[:len(x)]


def fade(x, fin=.003, fout=.015):
    x = x.copy()
    a, b = int(fin * SR), int(fout * SR)
    if a:
        x[:a] *= np.linspace(0, 1, a)
    if b:
        x[-b:] *= np.linspace(1, 0, b)
    return x


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[:len(p)] += p
    return out


def at(x, offset_s, total_s=None):
    pad = np.zeros(int(offset_s * SR))
    y = np.concatenate([pad, x])
    if total_s:
        n = int(total_s * SR)
        y = np.pad(y, (0, max(0, n - len(y))))[:n]
    return y


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12)


def normalize(x, peak_db=-3.0):
    peak = np.max(np.abs(x)) + 1e-9
    return x * (10 ** (peak_db / 20) / peak)


def write(name, x, peak_db=-3.0, stereo=None, loop=False):
    if stereo is not None:
        data = np.stack([x, stereo], axis=1)
        data = normalize(data, peak_db)
    else:
        data = normalize(x if loop else fade(x), peak_db)
    pcm = np.clip(data, -1, 1)
    pcm = (pcm * 32767).astype("<i2")
    with wave.open(str(OUT / (name + ".wav")), "wb") as w:
        w.setnchannels(2 if stereo is not None else 1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    peak = 20 * np.log10(np.max(np.abs(pcm / 32767)) + 1e-9)
    REPORT.append((name, len(pcm) / SR, peak))


REPORT = []


# ----------------------------------------------------------------- music
BPM = 84
BEAT = 60 / BPM
BAR = BEAT * 4
# D minor / Dorian colour. Chords per bar (roots and voicing in MIDI).
PROG = [[50, 57, 62, 65], [46, 53, 58, 62], [48, 55, 60, 64], [45, 52, 57, 61]]  # Dm, Bb, C, A


def pad_voice(notes, seconds, bright=900):
    n = int(seconds * SR)
    out = np.zeros(n)
    for k, note in enumerate(notes):
        for det in (-.07, .07):
            f = midi(note) * 2 ** (det / 12)
            out += saw(f, seconds, harmonics=12) * (.5 if k else .7)
    out = fft_filter(out, lo=80, hi=bright)
    return out * env(n, a=1.2, d=.5, s=.8, r=1.4)


def pluck(note, seconds=.5, bright=3000):
    n = int(seconds * SR)
    f = midi(note)
    x = tri(f, seconds) * .7 + sine(f * 2, seconds) * .2
    x = sweep_lp(x, bright, 400, curve=.5)
    return x * expdecay(n, .14)


def music_explore():
    bars = 16
    loop_len = bars * BAR
    total = loop_len + 4.0  # padded; overflow folds back to the loop start
    n = int(total * SR)
    L = np.zeros(n); R = np.zeros(n)
    pattern = [0, 2, 3, 1, 2, 3, 0, 3]  # chord-tone indexes for the 8th-note pulse
    for b in range(bars):
        chord = PROG[(b // 2) % 4]
        start = b * BAR
        pad = pad_voice([c + 12 for c in chord[1:]], BAR * 1.15, bright=700 + 200 * (b % 4 == 3))
        seg = at(pad, start, total)
        L += seg * .9; R += seg * .9
        sub = sine(midi(chord[0] - 12), BAR) * env(int(BAR * SR), a=.4, d=.3, s=.8, r=.6) * .55
        sub_seg = at(sub, start, total)
        L += sub_seg; R += sub_seg
        if b >= 2:
            for i in range(8):
                if (b % 4 == 3 and i > 5) or (i in (5,) and b % 2):
                    continue
                note = chord[pattern[i]] + 24
                p = pluck(note, .6) * (.22 if i % 2 == 0 else .15)
                pos = start + i * BEAT / 2
                pan = .35 + .3 * ((i * 3) % 4) / 3
                seg = at(p, pos, total)
                L += seg * (1 - pan); R += seg * pan
        # A soft metallic bell every 4 bars: the "machine breathing".
        if b % 4 == 0:
            f = midi(chord[3] + 24)
            bell_n = int(2.5 * SR)
            bell = (sine(f, 2.5) + .5 * sine(f * 2.76, 2.5) + .25 * sine(f * 5.4, 2.5)) * expdecay(bell_n, .7) * .12
            seg = at(bell, start + BEAT * 2, total)
            L += seg * .7; R += seg * .3
    # Quiet air bed.
    air = fft_filter(noise(total), lo=300, hi=1500) * .02 * np.concatenate([np.ones(int(loop_len * SR)), np.zeros(n - int(loop_len * SR))])
    L += air; R += np.roll(air, 3000)
    L, R = fold(L, loop_len), fold(R, loop_len)
    L = loop_reverb(L, 2.6, .35); R = loop_reverb(R, 2.6, .35)
    return L, R


def fold(x, loop_seconds):
    n = int(loop_seconds * SR)
    out = x[:n].copy()
    over = x[n:]
    out[:len(over)] += over
    return out


def kick(seconds=.45):
    n = int(seconds * SR)
    f = 45 + 110 * np.exp(-np.arange(n) / (SR * .03))
    return sine(f) * expdecay(n, .12) + fft_filter(noise(seconds), hi=2500) * expdecay(n, .006) * .3


def metal_snare(seconds=.3):
    n = int(seconds * SR)
    body = bandpass_noise(seconds, 2200, 2600) * expdecay(n, .07)
    ring = (sine(330, seconds) + sine(510, seconds) * .6 + sine(870, seconds) * .3) * expdecay(n, .05) * .5
    return body + ring


def hat(seconds=.06):
    n = int(seconds * SR)
    return fft_filter(noise(seconds), lo=7000) * expdecay(n, .015)


def music_boss():
    bars = 8
    loop_len = bars * BAR
    total = loop_len + 4.0  # padded; overflow folds back to the loop start
    n = int(total * SR)
    L = np.zeros(n); R = np.zeros(n)
    bass_line = [0, 0, 12, 0, 10, 0, 7, 8]  # 8th-note ostinato over chord root
    for b in range(bars):
        chord = PROG[(b // 2) % 4]
        start = b * BAR
        root = chord[0] - 12
        for i in range(8):
            note = root + bass_line[i]
            dur = BEAT / 2
            m = int(dur * SR)
            tone = saw(midi(note), dur, harmonics=30)
            tone = sweep_lp(tone, 2200, 300, curve=.4, res=.3) * env(m, a=.002, d=.12, s=.5, r=.03)
            seg = at(tone, start + i * dur, total) * .55
            L += seg; R += seg
        for beat in range(4):
            pos = start + beat * BEAT
            k = at(kick(), pos, total) * .9
            L += k; R += k
            if beat in (1, 3):
                s = at(metal_snare(), pos, total) * .5
                L += s * .6; R += s * .4
            for sub in range(2):
                h = at(hat(), pos + sub * BEAT / 2, total) * (.18 if sub else .1)
                L += h * .3; R += h * .7
        pad = pad_voice([c + 12 for c in chord], BAR, bright=1400) * .35
        seg = at(pad, start, total)
        L += seg; R += seg
        # Tension stab: minor-second cluster on the last beat of each 2 bars.
        if b % 2 == 1:
            st = BEAT
            stab = (saw(midi(chord[1] + 12), st) + saw(midi(chord[1] + 13), st)) * expdecay(int(st * SR), .12) * .22
            stab = fft_filter(stab, hi=2600)
            seg = at(stab, start + 3 * BEAT, total)
            L += seg * .3; R += seg * .7
    L, R = fold(L, loop_len), fold(R, loop_len)
    L = loop_reverb(L, 1.4, .18); R = loop_reverb(R, 1.4, .18)
    return softclip(L, 1.3), softclip(R, 1.3)


def ambience():
    total = 24.0
    n = int(total * SR)
    t = t_axis(total)
    hum = (sine(50, total) * .5 + sine(100, total) * .25 + sine(150, total) * .08) * (.8 + .2 * np.sin(2 * np.pi * t / 6))
    air = fft_filter(noise(total), lo=120, hi=900) * .25 * (.7 + .3 * np.sin(2 * np.pi * t / 8 + 1))
    L = hum * .35 + air
    R = hum * .35 + np.roll(air, 7000)
    # Distant clanks and servo whirs at fixed, irregular times.
    for pos, freq, side in ((3.1, 420, .2), (9.7, 610, .8), (14.2, 380, .6), (19.5, 520, .3)):
        m = int(1.2 * SR)
        clank = (bandpass_noise(1.2, freq, 160) * 3 + sine(freq * 1.5, 1.2) * .3) * expdecay(m, .18)
        seg = at(clank, pos, total) * .35
        L += seg * (1 - side); R += seg * side
    for pos in (6.0, 17.0):
        m = int(1.6 * SR)
        f = np.linspace(180, 260, m)
        whir = square(f, harmonics=6) * env(m, a=.3, d=.2, s=.7, r=.6) * .06
        whir = fft_filter(whir, hi=1200)
        seg = at(whir, pos, total)
        L += seg * .4; R += seg * .6
    L = loop_reverb(L, 3.0, .4, damp=2000); R = loop_reverb(R, 3.0, .4, damp=2000)
    return L, R


def stinger_victory():
    total = 4.5
    out = np.zeros(int(total * SR))
    notes = [62, 65, 69, 74, 77]
    for i, note in enumerate(notes):
        f = midi(note + 12)
        m = int((total - i * .14) * SR)
        bell = (sine(f, m / SR) + .4 * sine(f * 2, m / SR) + .15 * sine(f * 3.01, m / SR)) * expdecay(m, .9) * .3
        out[int(i * .14 * SR):int(i * .14 * SR) + m] += bell[:len(out) - int(i * .14 * SR)]
    chord = pad_voice([62, 69, 74, 78], 3.6, bright=2200) * .5
    out[:len(chord)] += chord
    return reverb(out, 2.2, .35)[:int(total * SR) + SR]


def stinger_defeat():
    total = 3.2
    n = int(total * SR)
    f = np.linspace(midi(57), midi(45), n)
    tone = saw(f, harmonics=16) * env(n, a=.01, d=.5, s=.5, r=1.5)
    tone2 = saw(f * 2 ** (1 / 12), harmonics=16) * env(n, a=.01, d=.5, s=.4, r=1.5)
    x = fft_filter(tone + tone2 * .7, hi=1400) * .5
    x = bitcrush(x, bits=7, hold=3) * .6 + x * .4
    return reverb(x, 1.8, .3)


# ----------------------------------------------------------------- effects

def sfx_steps():
    out = {}
    for i, (center, thump) in enumerate(((1800, 90), (2300, 100), (1500, 82))):
        s = .16
        n = int(s * SR)
        tap = bandpass_noise(s, center, 1400) * expdecay(n, .012)
        low = sine(thump, s) * expdecay(n, .03) * .5
        ring = sine(center * .37, s) * expdecay(n, .04) * .12
        out["step_%d" % i] = (tap + low + ring) * .6
    return out


def sfx_jump():
    s = .22
    n = int(s * SR)
    f = np.linspace(260, 620, n)
    chirp = tri(f) * env(n, a=.004, d=.18, s=0, r=.02) * .5
    air = sweep_lp(noise(s), 800, 4000) * env(n, a=.01, d=.18, s=0, r=.02) * .6
    return chirp + air


def sfx_land():
    s = .3
    n = int(s * SR)
    thud = sine(np.linspace(110, 55, n)) * expdecay(n, .05)
    rattle = bandpass_noise(s, 3200, 2000) * expdecay(n, .03) * .5
    return thud + rattle


def sfx_dash():
    s = .32
    n = int(s * SR)
    whoosh = sweep_lp(noise(s), 600, 6000, curve=.6) * env(n, a=.01, d=.25, s=0, r=.05)
    swoop = sine(np.linspace(220, 80, n)) * expdecay(n, .08) * .5
    zap = square(np.linspace(900, 300, n), harmonics=6) * expdecay(n, .03) * .15
    return whoosh * .8 + swoop + zap


def sfx_hurt():
    s = .35
    n = int(s * SR)
    tone = square(np.linspace(420, 140, n), harmonics=10) * env(n, a=.002, d=.3, s=0, r=.03)
    crunch = bitcrush(tone, 4, 8) * .7
    stutter = (np.sin(2 * np.pi * 28 * np.arange(n) / SR) > 0).astype(float) * .6 + .4
    return softclip((crunch + fft_filter(noise(s), lo=1500) * expdecay(n, .04) * .4) * stutter, 1.5)


def sfx_death():
    s = 1.6
    n = int(s * SR)
    f = 600 * np.exp(-np.linspace(0, 3, n))
    x = square(f, harmonics=8) * env(n, a=.002, d=1.4, s=0, r=.1)
    x = bitcrush(x, bits=5, hold=6)
    glitch = (RNG.random(n // 2205 + 1) > .5).repeat(2205)[:n]
    x = x * (.5 + .5 * glitch)
    tail = fft_filter(noise(s), hi=900) * expdecay(n, .5) * .3
    return reverb(x * .7 + tail, 1.2, .25)


def sfx_respawn():
    s = 1.2
    n = int(s * SR)
    out = np.zeros(n)
    for i, note in enumerate([69, 74, 78, 81, 86]):
        m = int(.6 * SR)
        b = (sine(midi(note), .6) + .3 * sine(midi(note) * 2, .6)) * expdecay(m, .2) * .4
        o = int(i * .07 * SR)
        out[o:o + m] += b[:n - o]
    swell = sweep_lp(noise(s), 300, 5000, curve=.7) * env(n, a=.5, d=.4, s=0, r=.3) * .25
    return reverb(out + swell, 1.0, .3)


def sfx_blaster():
    s = .2
    n = int(s * SR)
    f = 1400 * np.exp(-np.linspace(0, 2.5, n)) + 180
    body = square(f, harmonics=9) * expdecay(n, .045)
    click = fft_filter(noise(s), lo=2500) * expdecay(n, .004) * .8
    sub = sine(np.linspace(160, 70, n)) * expdecay(n, .04) * .5
    return softclip(body * .6 + click + sub, 1.4)


def sfx_blade():
    s = .3
    n = int(s * SR)
    whoosh = sweep_lp(noise(s), 1200, 7000, curve=.5) * env(n, a=.03, d=.2, s=0, r=.05)
    hum = saw(np.linspace(180, 260, n), harmonics=10) * env(n, a=.02, d=.25, s=0, r=.03) * .25
    return whoosh + fft_filter(hum, hi=2500)


def sfx_hit_enemy():
    s = .25
    n = int(s * SR)
    thud = sine(np.linspace(180, 60, n)) * expdecay(n, .05)
    crackle = bitcrush(fft_filter(noise(s), lo=1200), 4, 5) * expdecay(n, .05) * .5
    return softclip(thud + crackle, 1.3)


def sfx_deflect():
    s = .35
    n = int(s * SR)
    ping = (sine(2650, s) + .5 * sine(3970, s) + .3 * sine(5300, s)) * expdecay(n, .07)
    tick = fft_filter(noise(s), lo=5000) * expdecay(n, .003)
    return ping * .5 + tick * .5


def sfx_enemy_shot():
    s = .35
    n = int(s * SR)
    buzz = saw(np.linspace(240, 120, n), harmonics=20) * env(n, a=.004, d=.28, s=0, r=.03)
    return softclip(fft_filter(buzz, hi=1800) + sine(np.linspace(90, 50, n)) * expdecay(n, .07) * .6, 1.5)


def sfx_boss_activate():
    s = 2.8
    n = int(s * SR)
    rise = saw(np.linspace(40, 110, n), harmonics=24) * env(n, a=1.8, d=.4, s=.6, r=.5)
    rise = sweep_lp(rise, 200, 2400, curve=1.5)
    out = rise * .6
    for pos in (.2, .7, 1.25, 1.9):
        m = int(.5 * SR)
        clunk = (sine(np.linspace(140, 60, m)) * expdecay(m, .06) + bandpass_noise(.5, 900, 600) * expdecay(m, .04) * 1.5)
        out += at(clunk, pos, s) * .7
    eye = (sine(midi(74), .8) + sine(midi(75), .8)) * env(int(.8 * SR), a=.05, d=.6, s=0, r=.1) * .25
    out += at(eye, 2.0, s)
    return reverb(softclip(out, 1.2), 1.8, .3)


def sfx_boss_charge():
    s = .85
    n = int(s * SR)
    f = np.linspace(200, 900, n) ** 1
    whine = (sine(f) + .4 * sine(f * 1.5)) * env(n, a=.6, d=.2, s=.3, r=.05)
    grit = fft_filter(noise(s), lo=2000) * np.linspace(0, .25, n)
    return whine * .6 + grit


def sfx_boss_fire():
    s = .7
    n = int(s * SR)
    blast = fft_filter(noise(s), hi=3000) * expdecay(n, .1)
    body = saw(np.linspace(300, 70, n), harmonics=18) * expdecay(n, .15) * .5
    sub = sine(np.linspace(80, 35, n)) * expdecay(n, .2)
    return softclip(blast * .6 + body + sub, 1.6)


def sfx_boss_slam():
    s = 1.0
    n = int(s * SR)
    impact = sine(np.linspace(90, 30, n)) * expdecay(n, .25)
    crash = fft_filter(noise(s), hi=1800) * expdecay(n, .12) * .8
    debris = bandpass_noise(s, 2500, 1500) * expdecay(n, .3) * .2
    return softclip(impact + crash + debris, 1.5)


def sfx_boss_hurt():
    s = .4
    n = int(s * SR)
    groan = saw(np.linspace(150, 95, n), harmonics=14) * env(n, a=.01, d=.3, s=0, r=.05)
    clang = (sine(620, s) + .6 * sine(1480, s)) * expdecay(n, .05) * .5
    return fft_filter(groan, hi=1500) * .7 + clang


def sfx_boss_phase():
    s = 1.5
    n = int(s * SR)
    roar = saw(np.linspace(70, 140, n), harmonics=28) * env(n, a=.05, d=1.2, s=0, r=.2)
    roar = bitcrush(fft_filter(roar, hi=2200), 5, 4)
    stut = (np.sin(2 * np.pi * np.linspace(6, 20, n) * np.arange(n) / SR) > -.2).astype(float)
    shriek = sine(np.linspace(1500, 700, n)) * env(n, a=.02, d=.8, s=0, r=.1) * .2
    return reverb(softclip(roar * stut + shriek, 1.4), 1.3, .3)


def sfx_boss_death():
    s = 3.6
    n = int(s * SR)
    out = np.zeros(n)
    for pos, amp in ((.0, 1.0), (.45, .8), (.8, .7), (1.3, 1.0), (1.9, .6)):
        out += at(sfx_boss_fire() * amp, pos, s)
    fall = square(600 * np.exp(-np.linspace(0, 4, n)), harmonics=6) * env(n, a=.01, d=3.2, s=0, r=.3) * .3
    out += bitcrush(fall, 4, 10)
    rumble = fft_filter(noise(s), hi=200) * env(n, a=.5, d=2.8, s=0, r=.2) * 1.5
    return reverb(softclip(out + rumble, 1.3), 2.4, .35)


def sfx_shield_open():
    s = .6
    n = int(s * SR)
    hiss = sweep_lp(noise(s), 5000, 800) * env(n, a=.01, d=.5, s=0, r=.05) * .6
    servo = square(np.linspace(300, 520, n), harmonics=5) * env(n, a=.05, d=.45, s=0, r=.05) * .15
    chime = sine(midi(81), s) * expdecay(n, .15) * .3
    return hiss + fft_filter(servo, hi=1500) + at(chime, .25, s)


def sfx_shockwave():
    s = 1.1
    n = int(s * SR)
    rumble = fft_filter(noise(s), hi=400) * env(n, a=.02, d=1.0, s=0, r=.05) * 2
    grind = bandpass_noise(s, 900, 500) * env(n, a=.05, d=.9, s=0, r=.1) * .6
    return softclip(rumble + grind, 1.3)


def sfx_block_fall():
    s = .7
    n = int(s * SR)
    whistle = sine(np.linspace(1800, 500, n)) * env(n, a=.05, d=.6, s=0, r=.05) * .3
    glitch = bitcrush(square(np.linspace(300, 200, n), harmonics=5), 3, 30) * .1
    return whistle + glitch


def sfx_block_impact():
    s = .5
    n = int(s * SR)
    return softclip(sine(np.linspace(120, 45, n)) * expdecay(n, .08) + bitcrush(fft_filter(noise(s), hi=2500), 4, 6) * expdecay(n, .06) * .6, 1.4)


def sfx_terminal_open():
    s = .55
    out = np.zeros(int(s * SR))
    for i, note in enumerate([76, 83, 88]):
        m = int(.12 * SR)
        b = square(midi(note), .12, harmonics=4) * env(m, a=.002, d=.09, s=0, r=.02) * .3
        o = int(i * .09 * SR)
        out[o:o + m] += b
    hum = fft_filter(noise(s), lo=200, hi=900) * env(len(out), a=.05, d=.4, s=0, r=.1) * .2
    return out + hum


def sfx_rotate():
    s = .08
    n = int(s * SR)
    return (bandpass_noise(s, 4200, 2500) * expdecay(n, .006) + sine(1650, s) * expdecay(n, .01) * .3)


def sfx_hack_error():
    s = .5
    n = int(s * SR)
    a = square(midi(52), .22, harmonics=8) * env(int(.22 * SR), a=.002, d=.18, s=0, r=.02)
    b = square(midi(53), .22, harmonics=8) * env(int(.22 * SR), a=.002, d=.18, s=0, r=.02)
    x = mix(a + b * .8, at(a * .8 + b, .24))
    return fft_filter(x, hi=2500) * .7


def sfx_hack_success():
    s = 1.1
    out = np.zeros(int(s * SR))
    for i, note in enumerate([69, 73, 76, 81]):
        m = int(.5 * SR)
        b = (sine(midi(note), .5) + .35 * sine(midi(note) * 2, .5) + .1 * square(midi(note), .5, harmonics=3)) * expdecay(m, .18) * .35
        o = int(i * .075 * SR)
        out[o:o + m] += b[:len(out) - o]
    return reverb(out, .9, .25)


def sfx_alarm():
    s = .9
    n = int(s * SR)
    t = np.arange(n) / SR
    f = np.where((t * 5).astype(int) % 2 == 0, 740, 560)
    return fft_filter(square(f.astype(float), harmonics=6), hi=2000) * env(n, a=.01, d=.8, s=0, r=.05) * .5


def sfx_fragment():
    s = 1.0
    out = np.zeros(int(s * SR))
    for i, note in enumerate([74, 81, 86, 90]):
        m = int(.6 * SR)
        f = midi(note)
        b = (sine(f, .6) + .5 * sine(f * 2.0, .6) + .2 * sine(f * 3.0, .6)) * expdecay(m, .22) * .3
        o = int(i * .06 * SR)
        out[o:o + m] += b[:len(out) - o]
    shimmer = fft_filter(noise(s), lo=6000) * env(len(out), a=.01, d=.5, s=0, r=.1) * .08
    return reverb(out + shimmer, 1.0, .3)


def sfx_checkpoint():
    s = 1.6
    n = int(s * SR)
    chord = pad_voice([62, 69, 74], s, bright=1800) * .5
    chime = mix(at((sine(midi(86), .8) + .4 * sine(midi(98), .8)) * expdecay(int(.8 * SR), .3) * .4, .15))
    return reverb(mix(chord, chime), 1.4, .3)


def sfx_portal_open():
    s = 2.0
    n = int(s * SR)
    shimmer = sum(sine(midi(note) * np.linspace(.98, 1.0, n)) for note in (74, 78, 81, 86)) * env(n, a=1.0, d=.6, s=0, r=.3) * .2
    swell = sweep_lp(noise(s), 200, 6000, curve=.8) * env(n, a=1.2, d=.6, s=0, r=.2) * .3
    return reverb(shimmer + swell, 1.6, .35)


def sfx_portal_enter():
    s = 1.4
    n = int(s * SR)
    up = sine(np.linspace(200, 1600, n) ) * env(n, a=.05, d=1.2, s=0, r=.1) * .4
    whoosh = sweep_lp(noise(s), 400, 8000, curve=.5) * env(n, a=.2, d=1.0, s=0, r=.1) * .5
    return reverb(up + whoosh, 1.2, .3)


def sfx_ui_move():
    s = .05
    n = int(s * SR)
    return sine(1800, s) * expdecay(n, .008) * .6 + bandpass_noise(s, 5000, 3000) * expdecay(n, .003) * .2


def sfx_ui_confirm():
    s = .22
    a = sine(midi(81), .1) * env(int(.1 * SR), a=.002, d=.08, s=0, r=.01)
    b = sine(midi(88), .14) * env(int(.14 * SR), a=.002, d=.12, s=0, r=.01)
    return mix(a * .5, at(b * .5, .06))


def sfx_ui_back():
    a = sine(midi(84), .1) * env(int(.1 * SR), a=.002, d=.08, s=0, r=.01)
    b = sine(midi(76), .14) * env(int(.14 * SR), a=.002, d=.12, s=0, r=.01)
    return mix(a * .5, at(b * .5, .06))


def sfx_pause(on=True):
    s = .35
    n = int(s * SR)
    x = noise(s)
    y = sweep_lp(x, 6000, 300) if on else sweep_lp(x, 300, 6000)
    return y * env(n, a=.02, d=.3, s=0, r=.03) * .5


def bit_chirp(seed):
    rng = np.random.default_rng(seed)
    s = .28
    n = int(s * SR)
    out = np.zeros(n)
    syll = 3
    for i in range(syll):
        m = int(.08 * SR)
        f0 = 700 + rng.random() * 500
        f = np.linspace(f0, f0 * (1.25 if i % 2 == 0 else .85), m)
        mod = np.sin(phase_from_freq(f * 2)) * 2.5
        tone = np.sin(phase_from_freq(f) + mod) * env(m, a=.005, d=.06, s=0, r=.01)
        o = int(i * .085 * SR)
        out[o:o + m] += tone
    return fft_filter(out, hi=5000) * .5


def glitch_voice(seed):
    rng = np.random.default_rng(seed)
    s = .7
    n = int(s * SR)
    f = 90 + 40 * np.sin(np.linspace(0, 9, n))
    x = saw(f, harmonics=30)
    x = bitcrush(x, 3, 12)
    gate = (rng.random(n // 1500 + 1) > .35).repeat(1500)[:n]
    return fft_filter(x * gate, lo=200, hi=3000) * env(n, a=.02, d=.6, s=0, r=.05) * .5


def door_close():
    s = 1.0
    n = int(s * SR)
    slide = bandpass_noise(s, 700, 600) * env(n, a=.05, d=.6, s=0, r=.05) * .7
    slam = at(sine(np.linspace(100, 40, int(.5 * SR))) * expdecay(int(.5 * SR), .1), .55, s)
    return softclip(slide + slam, 1.4)


def firewall_loop():
    s = 2.0
    n = int(s * SR)
    t = np.arange(n) / SR
    buzz = saw(120, s, harmonics=20) * (.6 + .4 * np.sin(2 * np.pi * 4 * t))
    return fft_filter(buzz, lo=150, hi=1800) * .5


def main():
    L, R = music_explore(); write("music_explore", L, -6, stereo=R, loop=True)
    L, R = music_boss(); write("music_boss", L, -5, stereo=R, loop=True)
    L, R = ambience(); write("amb_system", L, -10, stereo=R, loop=True)
    write("stinger_victory", stinger_victory(), -4)
    write("stinger_defeat", stinger_defeat(), -5)
    for name, x in sfx_steps().items():
        write(name, x, -12)
    effects = {
        "jump": (sfx_jump, -9), "land": (sfx_land, -8), "dash": (sfx_dash, -6), "hurt": (sfx_hurt, -4),
        "death": (sfx_death, -4), "respawn": (sfx_respawn, -6), "blaster": (sfx_blaster, -7), "blade": (sfx_blade, -7),
        "hit_enemy": (sfx_hit_enemy, -6), "deflect": (sfx_deflect, -9), "enemy_shot": (sfx_enemy_shot, -7),
        "boss_activate": (sfx_boss_activate, -3), "boss_charge": (sfx_boss_charge, -7), "boss_fire": (sfx_boss_fire, -4),
        "boss_slam": (sfx_boss_slam, -3), "boss_hurt": (sfx_boss_hurt, -7), "boss_phase": (sfx_boss_phase, -3),
        "boss_death": (sfx_boss_death, -3), "shield_open": (sfx_shield_open, -6), "shockwave": (sfx_shockwave, -5),
        "block_fall": (sfx_block_fall, -9), "block_impact": (sfx_block_impact, -6),
        "terminal_open": (sfx_terminal_open, -8), "rotate": (sfx_rotate, -12), "hack_error": (sfx_hack_error, -6),
        "hack_success": (sfx_hack_success, -5), "alarm": (sfx_alarm, -7), "fragment": (sfx_fragment, -5),
        "checkpoint": (sfx_checkpoint, -5), "portal_open": (sfx_portal_open, -6), "portal_enter": (sfx_portal_enter, -5),
        "ui_move": (sfx_ui_move, -14), "ui_confirm": (sfx_ui_confirm, -9), "ui_back": (sfx_ui_back, -10),
        "pause_in": (lambda: sfx_pause(True), -10), "pause_out": (lambda: sfx_pause(False), -10),
        "door_close": (door_close, -4),
    }
    for name, (fn, peak) in effects.items():
        write(name, fn(), peak)
    for i in range(3):
        write("bit_chirp_%d" % i, bit_chirp(30 + i), -11)
    for i in range(2):
        write("glitch_voice_%d" % i, glitch_voice(50 + i), -8)
    write("firewall_loop", firewall_loop(), -14, loop=True)
    for name, seconds, peak in REPORT:
        print("%-18s %6.2fs  peak %5.1f dBFS" % (name, seconds, peak))


if __name__ == "__main__":
    main()
