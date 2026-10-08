"""Synthesises every sound in the launch video: the music bed and the effects.

Everything is generated from code so the repository carries no third-party audio.
Run from the video folder: python3 tools/make-audio.py
"""

import math
import os
import wave

import numpy as np

SR = 44100
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "public", "audio")
RNG = np.random.default_rng(7)


def write(name, data, gain=1.0):
    data = np.asarray(data, dtype=np.float64) * gain
    peak = float(np.max(np.abs(data))) or 1.0
    if peak > 0.98:
        data = data / peak * 0.98
    pcm = (data * 32767).astype("<i2")
    channels = 1 if pcm.ndim == 1 else pcm.shape[1]
    with wave.open(os.path.join(OUT, name), "wb") as handle:
        handle.setnchannels(channels)
        handle.setsampwidth(2)
        handle.setframerate(SR)
        handle.writeframes(pcm.tobytes())
    print(f"{name}: {len(pcm) / SR:.2f} s")


def seconds(duration):
    return np.arange(int(SR * duration)) / SR


def decay(n, tau):
    return np.exp(-np.arange(n) / SR / tau)


def attack(n, duration):
    ramp = np.ones(n)
    k = min(n, int(SR * duration))
    ramp[:k] = np.linspace(0.0, 1.0, k)
    return ramp


def fade_out(n, duration):
    ramp = np.ones(n)
    k = min(n, int(SR * duration))
    ramp[n - k:] = np.linspace(1.0, 0.0, k)
    return ramp


def noise(n):
    return RNG.standard_normal(n)


def band(x, lo, hi):
    """Bandpass with gentle edges, done in the frequency domain."""
    spectrum = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(len(x), 1 / SR)
    low_edge = 1 / (1 + (lo / np.maximum(freqs, 1e-3)) ** 4)
    high_edge = 1 / (1 + (freqs / hi) ** 4)
    return np.fft.irfft(spectrum * low_edge * high_edge, n=len(x))


def sweep(x, centers, width_octaves):
    """Time-varying bandpass: short overlapping windows, each filtered around its own centre."""
    size = 1024
    hop = size // 2
    window = np.hanning(size)
    out = np.zeros(len(x) + size)
    padded = np.concatenate([x, np.zeros(size)])
    count = (len(padded) - size) // hop
    for i in range(count):
        start = i * hop
        center = centers[min(len(centers) - 1, int(len(centers) * start / len(x)))]
        lo = center / (2 ** (width_octaves / 2))
        hi = center * (2 ** (width_octaves / 2))
        chunk = band(padded[start:start + size] * window, lo, hi)
        out[start:start + size] += chunk * window
    return out[: len(x)]


def sine(freq, n, phase=0.0):
    if np.isscalar(freq):
        return np.sin(2 * np.pi * freq * np.arange(n) / SR + phase)
    return np.sin(2 * np.pi * np.cumsum(freq) / SR + phase)


def soft_clip(x, drive=1.4):
    return np.tanh(x * drive) / math.tanh(drive)


# Effects


def whoosh(duration, lo, hi, peak_at, tau):
    n = int(SR * duration)
    centers = np.geomspace(lo, hi, 64)
    body = sweep(noise(n), centers, 1.6)
    k = int(SR * peak_at)
    env = np.concatenate([np.linspace(0, 1, k) ** 2, decay(n - k, tau)])
    return body * env


def pop():
    n = int(SR * 0.12)
    t = seconds(0.12)
    freq = 240 + 520 * np.exp(-t / 0.018)
    return sine(freq, n) * decay(n, 0.035)


def tick():
    n = int(SR * 0.035)
    return band(noise(n), 2500, 9000) * decay(n, 0.006)


def check():
    n = int(SR * 0.4)
    first = (sine(659.25, n) + 0.3 * sine(1318.5, n)) * decay(n, 0.11)
    shift = int(SR * 0.075)
    second = np.zeros(n)
    tail = n - shift
    second[shift:] = (sine(880, tail) + 0.3 * sine(1760, tail)) * decay(tail, 0.16)
    return (first * 0.8 + second) * attack(n, 0.004)


def click():
    n = int(SR * 0.09)
    burst = band(noise(n), 1200, 6000) * decay(n, 0.007)
    thump = sine(110, n) * decay(n, 0.03)
    return burst * 0.7 + thump * 0.6


def riser(duration=1.3):
    n = int(SR * duration)
    t = seconds(duration)
    centers = np.geomspace(180, 6500, 96)
    body = sweep(noise(n), centers, 2.2)
    tone = sine(150 + 650 * (t / duration) ** 2, n) * 0.18
    env = (t / duration) ** 1.8
    return (body + tone) * env


def impact(duration=1.1):
    n = int(SR * duration)
    t = seconds(duration)
    freq = 42 + 60 * np.exp(-t / 0.06)
    body = sine(freq, n) * (0.2 + 0.8 * decay(n, 0.32))
    burst = band(noise(n), 60, 1500) * decay(n, 0.012) * 0.5
    return soft_clip(body + burst, 1.8) * fade_out(n, 0.2)


def shimmer(duration=2.2):
    n = int(SR * duration)
    partials = [(1760, 1.0), (2217.46, 0.7), (2637.02, 0.6), (3520, 0.4)]
    out = np.zeros(n)
    for freq, amp in partials:
        for detune in (-0.003, 0.003):
            out += amp * sine(freq * (1 + detune), n, RNG.uniform(0, 2 * np.pi))
    out *= attack(n, 0.08) * decay(n, 0.6)
    air = band(noise(n), 6000, 14000) * attack(n, 0.3) * decay(n, 0.5) * 0.12
    return out / 6 + air


def chime(duration=1.8):
    n = int(SR * duration)
    out = np.zeros(n)
    for index, freq in enumerate((587.33, 880.0, 1174.66)):
        start = int(SR * 0.08 * index)
        length = n - start
        note = sine(freq, length) * decay(length, 0.55)
        note += 0.25 * sine(freq * 2.76, length) * decay(length, 0.18)
        out[start:] += note * attack(length, 0.004)
    return out / 3


# Music bed

BPM = 100
BEAT = 60 / BPM
BAR = 4 * BEAT
NOTES = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "Bb": 10}


def pitch(name, octave):
    semitone = NOTES[name] + 12 * (octave - 4)
    return 440.0 * 2 ** ((semitone - 9) / 12)


CHORDS = [
    [("D", 3), ("F", 3), ("A", 3)],
    [("Bb", 2), ("D", 3), ("F", 3)],
    [("F", 3), ("A", 3), ("C", 4)],
    [("C", 3), ("E", 3), ("G", 3)],
]


def pad_voice(freq, n, cutoff, detune):
    t = np.arange(n) / SR
    out = np.zeros(n)
    for harmonic in range(1, 8):
        weight = (1 / harmonic ** 1.5) / (1 + (harmonic * freq / cutoff) ** 4)
        out += weight * np.sin(2 * np.pi * freq * harmonic * (1 + detune) * t + RNG.uniform(0, 2 * np.pi))
    return out


def bed(duration=56.0):
    n = int(SR * duration)
    t = np.arange(n) / SR
    left = np.zeros(n)
    right = np.zeros(n)
    bars = int(math.ceil(duration / BAR))
    cross = int(SR * 0.25)

    for bar in range(bars):
        chord = CHORDS[bar % len(CHORDS)]
        start = int(bar * BAR * SR)
        end = min(n, int((bar + 1) * BAR * SR) + cross)
        length = end - start
        if length <= 0:
            continue
        # The filter opens as the video builds, then closes for the ending.
        progress = min(1.0, bar / 10)
        cutoff = 500 + 1900 * progress
        if bar >= bars - 2:
            cutoff = 700
        segment_l = np.zeros(length)
        segment_r = np.zeros(length)
        for name, octave in chord:
            freq = pitch(name, octave)
            segment_l += pad_voice(freq, length, cutoff, -0.0007)
            segment_r += pad_voice(freq, length, cutoff, 0.0007)
        root = pitch(chord[0][0], chord[0][1] - 1)
        sub = sine(root, length) * 0.55
        segment_l += sub
        segment_r += sub
        ramp = min(cross, length // 2)
        envelope = np.ones(length)
        envelope[:ramp] = np.linspace(0, 1, ramp)
        envelope[length - ramp:] = np.linspace(1, 0, ramp)
        left[start:end] += segment_l * envelope
        right[start:end] += segment_r * envelope

    pad_gain = 0.16
    left *= pad_gain
    right *= pad_gain

    # Sidechain-style pumping on every beat gives the pad its motion.
    beat_phase = np.mod(t, BEAT)
    pump = 1 - 0.3 * np.exp(-beat_phase / 0.11)
    left *= pump
    right *= pump

    drums = np.zeros(n)
    kick_n = int(SR * 0.35)
    kick_t = seconds(0.35)
    kick = sine(45 + 95 * np.exp(-kick_t / 0.04), kick_n) * decay(kick_n, 0.17)
    hat_n = int(SR * 0.08)
    hat = band(noise(hat_n), 7000, 15000) * decay(hat_n, 0.022)
    clap_n = int(SR * 0.3)
    clap = np.zeros(clap_n)
    for offset in (0, 0.011, 0.023):
        k = int(SR * offset)
        clap[k:] += band(noise(clap_n - k), 900, 4500) * decay(clap_n - k, 0.07)
    clap *= 0.33

    beats = int(duration / BEAT)
    drums_end = 51.0
    for beat in range(beats):
        when = beat * BEAT
        if when >= drums_end:
            break
        bar = int(when / BAR)
        pos = int(when * SR)
        kick_gain = 0.32 if bar < 4 else 0.55
        drums[pos:pos + kick_n] += kick[: max(0, min(kick_n, n - pos))] * kick_gain
        if bar >= 2:
            for eighth in (0, 0.5):
                pos_h = int((when + eighth * BEAT) * SR)
                gain = 0.07 if eighth == 0 else 0.11
                drums[pos_h:pos_h + hat_n] += hat[: max(0, min(hat_n, n - pos_h))] * gain
        if bar >= 8 and beat % 4 in (1, 3):
            drums[pos:pos + clap_n] += clap[: max(0, min(clap_n, n - pos))] * 0.6

    # A plucked arpeggio two octaves up adds sparkle through the middle of the piece.
    pluck_n = int(SR * 0.22)
    pluck_env = decay(pluck_n, 0.07)
    sixteenths = int(duration / (BEAT / 4))
    arp = np.zeros(n)
    for step in range(sixteenths):
        when = step * BEAT / 4
        bar = int(when / BAR)
        if bar < 4 or when >= 50.0:
            continue
        chord = CHORDS[bar % len(CHORDS)]
        name, octave = chord[step % 3]
        freq = pitch(name, octave + 2)
        pos = int(when * SR)
        length = min(pluck_n, n - pos)
        arp[pos:pos + length] += (sine(freq, length) + 0.2 * sine(freq * 2, length))[:length] * pluck_env[:length] * 0.1

    master_l = left + drums + arp * 0.9
    master_r = right + drums + arp * 1.1
    tail = fade_out(n, 3.0)
    stereo = np.stack([soft_clip(master_l, 1.2) * tail, soft_clip(master_r, 1.2) * tail], axis=1)
    return stereo


def main():
    os.makedirs(OUT, exist_ok=True)
    write("whoosh.wav", whoosh(0.5, 220, 2600, 0.14, 0.09), 0.8)
    write("whoosh-soft.wav", whoosh(0.36, 300, 1800, 0.1, 0.06), 0.5)
    write("whoosh-down.wav", whoosh(0.45, 2400, 220, 0.08, 0.12), 0.6)
    write("pop.wav", pop(), 0.7)
    write("tick.wav", tick(), 0.6)
    write("check.wav", check(), 0.5)
    write("click.wav", click(), 0.7)
    write("riser.wav", riser(), 0.75)
    write("impact.wav", impact(), 0.95)
    write("shimmer.wav", shimmer(), 0.5)
    write("chime.wav", chime(), 0.6)
    write("bed.wav", bed(), 0.9)


if __name__ == "__main__":
    main()
