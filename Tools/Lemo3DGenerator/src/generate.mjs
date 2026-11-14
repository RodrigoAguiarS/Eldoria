#!/usr/bin/env node
// node src/generate.mjs [--character hero|all] [--out <pasta>] [--formats glb,fbx]
import { mkdirSync, writeFileSync } from 'node:fs';
import { join, resolve, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadCharacter, assemble, toGLB } from './build.mjs';
import { toFBX } from './export-fbx.mjs';
import { THREE_VERSION, LEMO_ROOT } from './lemo-three.mjs';
import { CATALOG } from './catalog.mjs';

const HERE = dirname(fileURLToPath(import.meta.url)), LAB = resolve(HERE, '..'), PROJECT = resolve(LAB, '..', '..');
const argv = process.argv.slice(2), arg = (k, d) => { const i = argv.indexOf('--' + k); return i >= 0 ? argv[i + 1] : d; };
const OUT = resolve(arg('out', join(PROJECT, 'Assets/_Project/Art/Models/Generated')));
const formats = arg('formats', 'glb,fbx').split(',');
const which = arg('character', 'all');
const ids = which === 'all' ? CATALOG.filter(c => c.module).map(c => c.module) : which.split(',');

for (const id of ids) {
  const mod = await loadCharacter(id), ch = assemble(mod), def = ch.def;
  const dir = join(OUT, def.name); mkdirSync(dir, { recursive: true });
  const files = {};
  if (formats.includes('glb')) { const b = await toGLB(ch); writeFileSync(join(dir, `${def.name}.glb`), b); files.glb = `${def.name}.glb`; }
  if (formats.includes('fbx')) { writeFileSync(join(dir, `${def.name}.fbx`), toFBX(ch)); files.fbx = `${def.name}.fbx`; }
  const manifest = {
    id: def.id, name: def.name, role: def.role, source: def.source, documented: def.documented, assumptions: def.assumptions || [],
    generator: 'Tools/Lemo3DGenerator', lemo: { root: LEMO_ROOT, three: THREE_VERSION },
    units: 'metros (1 unidade Unity = 1 m), Y para cima, frente +Z, pivô nos pés', heightMeters: def.heightMeters,
    triangles: ch.stats.triangles, triangleBudget: def.triangleBudget, meshes: ch.stats.meshes,
    bones: ch.rig.list.map(b => b.name), sockets: Object.keys(ch.rig.sockets),
    equipment: ch.equipment.map(e => ({ name: e.mesh.name, socket: e.socket })),
    materials: Object.values(ch.M).map(m => ({ name: m.name, ...m.userData })),
    clips: ch.clips.map(c => ({ name: c.name, duration: +c.duration.toFixed(3), loop: c.userData.loop, events: c.userData.events, groundLift: c.userData.groundLift })),
    locomotion: def.locomotion || {},   // clip → velocidade (m/s) para a blend tree da Unity
    animatorNotes: def.animatorNotes || '',
    files,
  };
  writeFileSync(join(dir, `${def.name}_manifest.json`), JSON.stringify(manifest, null, 2));
  console.log(`OK ${def.name}: ${ch.stats.triangles} tris, ${ch.rig.list.length} ossos, ${ch.clips.length} clips, ${Object.keys(files).join('+')} → ${relative(PROJECT, dir)}`);
}
