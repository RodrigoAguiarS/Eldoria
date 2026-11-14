"""Original, deterministic placeholder audio. No downloads or third-party samples.

Run only when Unity is idle: python Tools/AudioGenerator/generate.py
"""
from array import array
import argparse
import math
from pathlib import Path
import random
import wave

RATE = 24000
TAU = math.tau


def write(path, samples, peak):
    scale = peak / max(max(abs(v) for v in samples), .001)
    pcm = array('h', (round(max(-1, min(1, v * scale)) * 32767) for v in samples))
    import sys
    if sys.byteorder != 'little':
        pcm.byteswap()
    with wave.open(str(path), 'wb') as output:
        output.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
        output.writeframes(pcm.tobytes())


def effect(name, seconds, seed):
    rng = random.Random(seed)
    result, noise, phase = [], 0., 0.
    for i in range(round(seconds * RATE)):
        t = i / RATE
        u = t / seconds
        noise = noise * .72 + rng.uniform(-1, 1) * .28
        envelope = min(1., t / .008) * (1 - u) ** 2
        if name == 'Swing':
            v = noise * math.sin(math.pi * u) + .08 * math.sin(TAU * 350 * t)
        elif name == 'Impact':
            v = .6 * math.sin(TAU * 180 * t) * math.exp(-18 * t) + .25 * math.sin(TAU * 1327 * t) * math.exp(-35 * t) + noise * .5
        elif name == 'Hurt':
            phase += TAU * (90 - 40 * u) / RATE
            v = math.sin(phase) * .65 + noise * .2
        elif name == 'Death':
            phase += TAU * (210 - 145 * u) / RATE
            v = math.sin(phase) * .6 + math.sin(phase * 1.5) * .12 + noise * .15
        elif name == 'Roar':
            phase += TAU * (65 + 12 * math.sin(TAU * 4 * t)) / RATE
            v = math.sin(phase) * .6 + math.sin(phase * 2.02) * .18 + noise * .45
        else:
            frequencies = {'Pickup': (659., 880.), 'Ui': (440., 660.), 'Victory': (293.66, 349.23, 440., 587.33)}[name]
            v = 0.
            for index, frequency in enumerate(frequencies):
                onset = index * seconds / (len(frequencies) + 2)
                local = t - onset
                if local >= 0:
                    v += math.sin(TAU * frequency * local) * math.exp(-local * 8) / len(frequencies)
        result.append(v * envelope)
    return result


def music():
    beat = 60 / 84
    duration = beat * 16
    melody = (293.66, 349.23, 440., 349.23, 261.63, 349.23, 440., 523.25,
              293.66, 349.23, 440., 587.33, 261.63, 349.23, 329.63, 220.)
    result = []
    for i in range(round(duration * RATE)):
        t = i / RATE
        note = int(t / beat) % len(melody)
        local = t % beat
        envelope = min(1., local / .025) * math.exp(-local * 5)
        pad = .13 * math.sin(TAU * 146.83 * t) + .08 * math.sin(TAU * 220 * t)
        phrase = math.sin(TAU * melody[note] * local) * envelope * .32
        bass = .12 * math.sin(TAU * 73.415 * t) * math.exp(-local * 7)
        edge = min(1., t / .025, (duration - t) / .025)
        result.append((pad + phrase + bass) * max(0., edge))
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--out', type=Path, default=Path(__file__).resolve().parents[2] / 'Assets/_Project/Audio/Generated')
    args = parser.parse_args()
    args.out.mkdir(parents=True, exist_ok=True)
    for index, (name, duration) in enumerate((('Swing', .22), ('Impact', .16), ('Hurt', .2), ('Death', .65), ('Roar', .95), ('Pickup', .4), ('Ui', .1), ('Victory', 1.4))):
        write(args.out / (name + '.wav'), effect(name, duration, 210 + index), .58)
    write(args.out / 'DungeonLoop.wav', music(), .38)
    print('Generated 8 original SFX and one music loop:', args.out)


if __name__ == '__main__':
    main()
