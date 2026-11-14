// Inimigo ranged — Enemy_Ranged.prefab / EnemyData: projétil esférico (0,4 m), windup 0,5 s, alcance 9 m, mantém 5 m.
// O documento de arte NÃO descreve este inimigo: visual DERIVADO da família esqueleto documentada (mesmo crânio,
// ossos e paleta) + capuz de tecido gasto (#4C5A44, "musgo/desgaste") e cajado com orbe de corrupção (#FF2A2A).
import { SKELETON_P, SKELETON_MATERIALS, HUNCH, skeletonBody, skull } from './_skeleton_kit.mjs';
import { convex, jitter, strut, ico, cone, torus, xform, sides } from '../parts.mjs';
import * as A from '../anim.mjs';

export const P = SKELETON_P;
export const definition = {
  id: 'skeleton_ranged', name: 'SkeletonRanged', role: 'Inimigo ranged (Enemy_Ranged.prefab)', source: 'Derivado do Asset 02 (sem descrição própria no documento)',
  documented: false, heightMeters: 2.0, triangleBudget: [3000, 4000],
  assumptions: ['Visual não documentado: esqueleto conjurador com capuz e cajado; aprovar com a direção de arte.', 'Orbe vermelho = cor de corrupção oficial (#FF2A2A), coerente com o projétil esférico do jogo.'],
  materials: { ...SKELETON_MATERIALS, cloth: { color: '#4C5A44', smoothness: .1, note: 'tecido gasto (musgo/desgaste oficial)' }, staff: { color: '#5A3A22', smoothness: .2, note: 'madeira escura (derivada do couro/ferrugem)' } },
  basePose: { ...HUNCH, Spine: [6, 0, 0], Chest: [8, 0, 0] },
  locomotion: { Idle: 0, Chase: 3.0 },
  sockets: { Socket_Hand_R: { rot: [-72, 0, 0] } },
};

export function body(M) {
  const out = skeletonBody(P, M, { armorChest: false, bracers: [], greaves: [] });
  out.push(...skull(P, M, { cy: 1.6, r: .26 }));
  // capuz (casca convexa aberta na frente: pontos só atrás/acima do rosto) e manto nos ombros
  const hood = [];
  for (let i = 0; i < 14; i++) { const a = (i / 14) * Math.PI * 2; for (const [y, r] of [[1.42, .27], [1.62, .31], [1.82, .27], [1.95, .12]]) { const x = Math.sin(a) * r, z = Math.cos(a) * r * 1.05 - .02; if (z < .12 || y > 1.88) hood.push([x, y, z]); } }
  hood.push([0, 2.02, -.08]);
  out.push({ geo: jitter(convex(hood), .02, 201), mat: M.cloth, bone: 'Head' });
  out.push({ geo: jitter(convex([[-.36, 1.12, 0], [.36, 1.12, 0], [-.3, 1.28, -.15], [.3, 1.28, -.15], [-.26, 1.3, .12], [.26, 1.3, .12], [0, 1.36, -.05], [-.3, .98, -.18], [.3, .98, -.18], [0, .92, -.24]]), .02, 203), mat: M.cloth, bone: 'Chest' });
  // faixa de tecido pendente (cintura)
  out.push({ geo: jitter(convex([[-.12, .7, .16], [.12, .7, .16], [-.1, .4, .14], [.08, .36, .15], [0, .3, .14], [-.1, .7, .12], [.1, .4, .11]]), .015, 205), mat: M.cloth, bone: 'Hips' });
  out.push({ geo: torus(.19, .025, 9, 3, { pos: [0, .7, 0], rot: [90, 0, 0], scale: [1, .75, 1] }), mat: M.rust, bone: 'Hips' });
  return out;
}

// cajado: eixo no +Z do soquete (aponta para cima pela rotação do soquete), orbe no topo
export const equipment = M => [{
  socket: 'Socket_Hand_R', name: 'CorruptedStaff', parts: [
    { geo: strut([0, 0, -.5], [0, 0, 1.05], .025, .022, 6), mat: M.staff },
    { geo: torus(.07, .018, 6, 3, { pos: [0, 0, 1.08] }), mat: M.rust },
    ...[0, 120, 240].map(a => ({ geo: cone(.022, .16, 4, { pos: [Math.sin(a * Math.PI / 180) * .07, Math.cos(a * Math.PI / 180) * .07, 1.14], rot: [90, 0, 0] }), mat: M.rust })),
    { geo: ico(.075, 1, { pos: [0, 0, 1.17] }), mat: M.eyes },
  ],
}];

export function clips() {
  const ready = { UpperArm_R: [-30, 0, -12], LowerArm_R: [-35, 0, 0] };
  const raise = { Chest: [-8, 10, 0], Head: [-6, -6, 0], UpperArm_R: [-115, 0, -20], LowerArm_R: [-35, 0, 0], UpperArm_L: [-60, 0, 25], LowerArm_L: [-40, 0, 0], Hand_L: [-30, 0, 0], $hips: [0, -.02, 0] };
  const cast = { Spine: [8, -6, 0], Chest: [14, -10, 0], Head: [-10, 6, 0], UpperArm_R: [-75, 0, -10], LowerArm_R: [-5, 0, 0], Hand_R: [20, 0, 0], UpperArm_L: [-80, 0, 15], LowerArm_L: [-10, 0, 0], UpperLeg_L: [-18, 0, 3], UpperLeg_R: [10, 0, -3], LowerLeg_L: [14, 0, 0], $hips: [0, -.05, .03] };
  return [
    A.idle('Idle', { duration: 2.2, breath: 3, sway: 2, armOut: 8 }),
    A.locomotion('Chase', { duration: .8, stride: 28, knee: 42, armSwing: 18, elbow: 25, lean: 6, bob: .04, sword: 1 }),
    { name: 'Attack', duration: 1.0, loop: false, events: { fire: .5 }, keys: [[0, ready], [.3, raise], [.5, raise], [.58, cast], [.75, cast], [1.0, ready]] },
    A.hit('Hit', .4, 1.2),
    A.death('Death', { duration: 1.4, hipsRest: -.5, back: -.4 }),
  ];
}
export { sides };
