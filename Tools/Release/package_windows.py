"""Package a validated final Windows build locally; never publishes anything."""
import hashlib
import json
from pathlib import Path
import struct
import xml.etree.ElementTree as ET
import zipfile


def main():
    project = Path(__file__).resolve().parents[2]
    report = json.loads((project / 'Reports/Leadership/windows-build.json').read_text(encoding='utf-8-sig'))
    if report.get('version') != '1.0.0' or report.get('result') != 'Succeeded' or report.get('errors') or report.get('development'):
        raise SystemExit('A successful final 1.0.0 release build is required; candidate/profile builds are not packaged.')
    for name in ('EditMode-final.xml', 'PlayMode-final.xml'):
        result = ET.parse(project / 'Reports/Leadership' / name).getroot()
        if int(result.get('failed', '1')) or result.get('result') != 'Passed':
            raise SystemExit('Required suite is not green: ' + name)
    source = (project / report['executable']).resolve().parent
    if not source.is_relative_to((project / 'Builds').resolve()):
        raise SystemExit('Unexpected build directory.')
    executable = source / 'DungeonCrawler.exe'
    with executable.open('rb') as stream:
        header = stream.read(64); stream.seek(struct.unpack_from('<I', header, 60)[0]); signature = stream.read(6)
    if signature[:4] != b'PE\0\0' or struct.unpack_from('<H', signature, 4)[0] != 0x8664:
        raise SystemExit('The executable is not Windows x64.')
    files = []
    for path in sorted(source.rglob('*')):
        relative = path.relative_to(source)
        if not path.is_file() or any('dontship' in part.lower() or 'donotship' in part.lower() for part in relative.parts):
            continue
        if path.suffix.lower() in ('.pdb', '.mdb', '.log'):
            continue
        files.append(path)
    readme = (project / 'docs/WINDOWS-RELEASE-README.txt').read_bytes()
    build_info = (json.dumps(report, indent=2, ensure_ascii=False) + '\n').encode('utf-8')
    manifest = {}
    output = project / 'Builds/Release/DungeonCrawler-1.0.0-windows-x64.zip'
    output.parent.mkdir(parents=True, exist_ok=True)
    if output.exists():
        raise SystemExit('Package already exists; preserving it: ' + str(output))
    with zipfile.ZipFile(output, 'x', compression=zipfile.ZIP_DEFLATED, compresslevel=6) as package:
        for path in files:
            relative = path.relative_to(source).as_posix()
            data = path.read_bytes(); manifest[relative] = hashlib.sha256(data).hexdigest()
            package.writestr('DungeonCrawler/' + relative, data)
        for name, data in (('README.txt', readme), ('BUILD-INFO.json', build_info)):
            manifest[name] = hashlib.sha256(data).hexdigest(); package.writestr('DungeonCrawler/' + name, data)
        package.writestr('DungeonCrawler/SHA256-MANIFEST.json', json.dumps(manifest, indent=2) + '\n')
    with zipfile.ZipFile(output) as package:
        if package.testzip() is not None:
            raise SystemExit('Package CRC validation failed.')
        for name, expected in manifest.items():
            if hashlib.sha256(package.read('DungeonCrawler/' + name)).hexdigest() != expected:
                raise SystemExit('Package hash mismatch: ' + name)
    digest = hashlib.sha256(output.read_bytes()).hexdigest()
    evidence = {'package': str(output), 'sha256': digest, 'bytes': output.stat().st_size, 'files': len(manifest),
                'crcValidated': True, 'allFileHashesValidated': True, 'excludedDebugBackups': True, 'published': False}
    (project / 'Reports/Leadership/windows-package.json').write_text(json.dumps(evidence, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(evidence, indent=2))


if __name__ == '__main__':
    main()
