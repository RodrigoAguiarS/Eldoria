#!/usr/bin/env node
// A13 — solver de pegada das poses de render do lanceiro (ConceptLancer.mjs). Descida por coordenadas sobre a
// hierarquia do rig (sem skinning): ajusta braços e perna de trás para levar Socket_Hand_R/L a pontos da mesma haste
// e deixar os dois pés no chão. Imprime os ângulos para copiar em GUARD/WINDUP; não escreve arquivos.
//   node src/production/ConceptLancer_grip.mjs [guard|windup|both] [R_guarda] [L_guarda] [R_windup] [L_windup]
// Alvos usados no lote A13: guarda R [0,0.85,-0.04] L [0.05,0.93,0.45]; windup R [0,0.69,-0.34] L [0.04,0.73,0.06].
import { THREE } from '../lemo-three.mjs';
import { humanoidSkeleton } from '../rig.mjs';
import * as C from './ConceptLancer.mjs';
import { HUNCH } from '../characters/_skeleton_kit.mjs';
const D = Math.PI / 180, rig = humanoidSkeleton(C.P), hip0 = rig.bones.Hips.position.clone();
function apply(pose) {
  for (const b of rig.list) if (b.name !== 'Root') { const r = [0, 1, 2].map(c => (pose[b.name]?.[c] ?? 0) + (HUNCH[b.name]?.[c] ?? 0)); b.quaternion.setFromEuler(new THREE.Euler(r[0] * D, r[1] * D, r[2] * D, 'XYZ')); }
  const o = [0, 1, 2].map(c => (pose.$hips?.[c] ?? 0) + (HUNCH.$hips?.[c] ?? 0));
  rig.bones.Hips.position.set(hip0.x + o[0], hip0.y + o[1], hip0.z + o[2]); rig.root.updateMatrixWorld(true);
  const w = n => (rig.sockets[n] || rig.bones[n]).getWorldPosition(new THREE.Vector3());
  const lift = Math.max(0, ...['L', 'R'].flatMap(s => [.04 - w('Toes_' + s).y, .1 - w('Foot_' + s).y]));
  const W = n => w(n).add(new THREE.Vector3(0, lift, 0));
  return { R: W('Socket_Hand_R'), L: W('Socket_Hand_L'), head: W('Head'), lift, feet: ['L', 'R'].map(s => W('Toes_' + s).toArray()) };
}
const VARS = [['UpperArm_L', 0], ['UpperArm_L', 1], ['UpperArm_L', 2], ['LowerArm_L', 0], ['UpperArm_R', 0], ['UpperArm_R', 1], ['UpperArm_R', 2], ['LowerArm_R', 0], ['UpperLeg_R', 0], ['LowerLeg_R', 0], ['Foot_R', 0]];
function solve(pose, tR, tL) {
  const p = JSON.parse(JSON.stringify(pose));
  const cost = () => { const r = apply(p); return r.R.distanceTo(tR) ** 2 + r.L.distanceTo(tL) ** 2 + 4 * (r.feet[0][1] - r.feet[1][1]) ** 2 + 2 * Math.max(0, r.feet[1][2] + .3) ** 2 + 1e-6 * VARS.reduce((s, [b, c]) => s + (p[b]?.[c] ?? 0) ** 2, 0); };
  for (const [b] of VARS) p[b] ??= [0, 0, 0];
  let step = 16, best = cost();
  while (step > .05) { let imp = false; for (const [b, c] of VARS) for (const s of [1, -1]) { p[b][c] += s * step; const v = cost(); if (v < best - 1e-12) { best = v; imp = true; } else p[b][c] -= s * step; } if (!imp) step /= 2; }
  for (const [b] of VARS) p[b] = p[b].map(v => Math.round(v));
  const r = apply(p);
  return { pose: Object.fromEntries(VARS.map(([b]) => [b, p[b]])), err: [r.R.distanceTo(tR), r.L.distanceTo(tL)].map(v => +v.toFixed(3)), R: r.R.toArray().map(v => +v.toFixed(2)), L: r.L.toArray().map(v => +v.toFixed(2)), head: r.head.toArray().map(v => +v.toFixed(2)), lift: +r.lift.toFixed(3), feet: r.feet.map(f => f.map(v => +v.toFixed(2))) };
}
const V = (...a) => new THREE.Vector3(...a);
const which = process.argv[2] || 'both';
if (which !== 'windup') console.log('GUARD', JSON.stringify(solve(C.GUARD, V(...JSON.parse(process.argv[3] || '[0.0,0.85,-0.04]')), V(...JSON.parse(process.argv[4] || '[0.05,0.93,0.45]')))));
if (which !== 'guard') console.log('WINDUP', JSON.stringify(solve(C.WINDUP, V(...JSON.parse(process.argv[5] || '[0.0,0.69,-0.34]')), V(...JSON.parse(process.argv[6] || '[0.04,0.73,0.06]')))));
