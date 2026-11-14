"""Summarize opt-in player frame JSON. Missing counters never become zero-cost claims."""
import argparse
import json
import math
from pathlib import Path
import statistics


def summary(values):
    if not values:
        return None
    ordered = sorted(values)
    return {'mean': statistics.mean(values), 'median': statistics.median(values),
            'p95': ordered[min(len(ordered) - 1, math.ceil(len(ordered) * .95) - 1)], 'max': max(values)}


def analyze(path):
    data = json.loads(path.read_text(encoding='utf-8-sig'))
    samples = data['samples']
    focused = [row for row in samples if row.get('focused', False) and not row['paused']]
    configurations = sorted({(row['width'], row['height'], row['qualityIndex']) for row in focused})
    result = {key: value for key, value in data.items() if key != 'samples'}
    result['file'] = str(path)
    result['focusedUnpausedFrames'] = len(focused)
    result['excludedFrames'] = len(samples) - len(focused)
    result['configurations'] = configurations
    result['frameMs'] = summary([row['frameMs'] for row in focused])
    result['cpuMs'] = summary([row['cpuMs'] for row in focused if row['cpuMs'] > 0]) if data['cpuCounter'] else None
    result['gpuMs'] = summary([row['gpuMs'] for row in focused if row['gpuMs'] > 0]) if data['gpuCounter'] else None
    result['missingGpuFrames'] = sum(row['gpuMs'] <= 0 for row in focused)
    result['gcBytesPerFrame'] = summary([row['gcBytes'] for row in focused]) if data['gcCounter'] and data['gcProbeDetected'] else None
    result['usedMiB'] = summary([row['usedBytes'] / 1048576 for row in focused if row['usedBytes'] > 0])
    result['managedMiB'] = summary([row['managedBytes'] / 1048576 for row in focused if row['managedBytes'] > 0])
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('files', type=Path, nargs='+')
    parser.add_argument('--out', type=Path)
    args = parser.parse_args()
    text = json.dumps([analyze(path) for path in args.files], indent=2, ensure_ascii=False) + '\n'
    if args.out:
        args.out.parent.mkdir(parents=True, exist_ok=True)
        args.out.write_text(text, encoding='utf-8')
    print(text)


if __name__ == '__main__':
    main()
