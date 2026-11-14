// Boss "Warden" — Boss_Warden.prefab (cápsula 2,2× ≈ 4,4 m, com Horn_L/Horn_R) + BossBrain:
// Slam (telegraph 1,4 s → 1,0 s na fase 2, raio 3,5 m), Volley (anel de 10 projéteis, telegraph 1,2 s),
// Recovering 0,8 s, Transition 3 s (rugido, mudança visual), Dead.
// O documento de arte NÃO descreve o boss: visual DERIVADO da família esqueleto (Asset 02) em escala 2,2×,
// com elmo de ferro e os chifres do placeholder existente, armadura pesada corroída e uma marreta para o Slam.
import { SKELETON_P, SKELETON_MATERIALS, HUNCH, skeletonBody, skull, spikedPauldron } from './_skeleton_kit.mjs';
import { convex, jitter, strut, ico, cone, torus, bevelBox, xform, cyl } from '../parts.mjs';
import * as A from '../anim.mjs';

export const P = SKELETON_P;
export const definition = {
  id: 'warden', name: 'Warden', role: 'Boss (Boss_Warden.prefab)', source: 'Derivado do Asset 02 + placeholder Boss_Warden (chifres)',
  documented: false, heightMeters: 4.6, scale: 2.2, triangleBudget: [4000, 7000],
  assumptions: ['Visual não documentado: esqueleto-carcereiro gigante com elmo de chifres (chifres do placeholder) e marreta (coerente com o Slam).', 'Fendas de runa vermelhas no peito = corrupção (#FF2A2A); a mudança visual da fase 2 pode intensificar essa emissão na Unity.'],
  materials: { ...SKELETON_MATERIALS, horn: { color: '#CFC6A8', smoothness: .3, note: 'chifre (osso oficial)' }, rune: { color: '#FF2A2A', emissive: '#FF2A2A', emissiveIntensity: 2.5, smoothness: .5 } },
  basePose: { ...HUNCH, Spine: [6, 0, 0], Chest: [8, 0, 0], UpperArm_L: [-4, 0, 10], UpperArm_R: [-4, 0, -10] },
  locomotion: { Idle: 0, Walk: 3.0 },
  sockets: { Socket_Hand_R: { rot: [28, 0, 0] } },
  animatorNotes: 'SlamWindup é loop (dura o telegraph de cada fase); disparar Slam ao fim do telegraph.',
};

export function body(M) {
  const out = skeletonBody(P, M, { bracers: ['L', 'R'], greaves: ['L', 'R'], heavyChest: true });
  out.push(...skull(P, M, { cy: 1.58, r: .25 }));
  // elmo: casca de ferro sobre topo/nuca, viseira angulosa
  const helm = [];
  for (let i = 0; i < 12; i++) { const a = i / 12 * Math.PI * 2; for (const [y, r] of [[1.5, .3], [1.7, .31], [1.86, .24], [1.94, .1]]) { const x = Math.sin(a) * r, z = Math.cos(a) * r - .01; if (z < .06 || y > 1.82) helm.push([x, y, z]); } }
  out.push({ geo: jitter(convex(helm), .02, 301), mat: M.armor, bone: 'Head' });
  out.push({ geo: jitter(convex([[-.27, 1.74, .1], [.27, 1.74, .1], [-.24, 1.7, .26], [.24, 1.7, .26], [0, 1.82, .27], [0, 1.66, .3]]), .01, 303), mat: M.rust, bone: 'Head' });
  // chifres (Horn_L/Horn_R do placeholder): três segmentos curvando para cima e para trás
  for (const sx of [-1, 1]) {
    const pts = [[sx * .24, 1.76, -.02], [sx * .38, 1.86, -.04], [sx * .45, 2.02, -.08], [sx * .44, 2.18, -.16]], rad = [.075, .06, .042, .02];
    for (let k = 0; k < 3; k++) out.push({ geo: strut(pts[k], pts[k + 1], rad[k], rad[k + 1], 5), mat: M.horn, bone: 'Head' });
  }
  // ombreiras maciças, gola de ferro e fendas de runa no peito
  out.push(...spikedPauldron(P, M, 'L', 1, { spikes: 3, k: 1.55 }));
  out.push(...spikedPauldron(P, M, 'R', -1, { spikes: 3, k: 1.55 }));
  out.push({ geo: jitter(torus(.2, .06, 9, 3, { pos: [0, 1.23, -.01], rot: [90, 0, 0], scale: [1.1, .85, 1] }), .012, 305), mat: M.armor, bone: 'Chest' });
  for (const [y, w] of [[1.12, .16], [1.04, .12], [.96, .08]]) out.push({ geo: xform(bevelBox(w, .018, .02, .2), { pos: [-.05, y, .2] }), mat: M.rune, bone: 'Chest' });
  // placas de cintura pesadas
  for (const a of [0, 60, -60, 180, 120, -120]) { const r = a * Math.PI / 180; out.push({ geo: xform(jitter(bevelBox(.16, .24, .04, .25, { bottom: 1.15 }), .01, a + 7), { pos: [Math.sin(r) * .22, .58, Math.cos(r) * .17], rot: [a === 0 ? -10 : 0, a, 0] }), mat: a % 120 ? M.armor : M.rust, bone: 'Hips' }); }
  return out;
}

// marreta de duas mãos: cabo ao longo de +Z do soquete, cabeça de ferro com cintas de ferrugem e espigões
export const equipment = M => [{
  socket: 'Socket_Hand_R', name: 'Maul', parts: [
    { geo: strut([0, 0, -.22], [0, 0, .72], .03, .028, 6), mat: M.armor },
    { geo: xform(jitter(bevelBox(.36, .26, .26, .18), .012, 311), { pos: [0, 0, .8] }), mat: M.armor },
    { geo: xform(bevelBox(.38, .07, .28, .2), { pos: [0, 0, .71] }), mat: M.rust },
    { geo: xform(bevelBox(.38, .07, .28, .2), { pos: [0, 0, .89] }), mat: M.rust },
    { geo: cone(.05, .14, 4, { pos: [.24, 0, .8], rot: [0, 0, -90] }), mat: M.rust },
    { geo: cone(.05, .14, 4, { pos: [-.24, 0, .8], rot: [0, 0, 90] }), mat: M.rust },
    { geo: cyl(.04, .04, .05, 6, { rot: [90, 0, 0], pos: [0, 0, -.24] }), mat: M.rust },
  ],
}];

export function clips() {
  const over = { Spine: [-10, 0, 0], Chest: [-12, 0, 0], Head: [8, 0, 0], UpperArm_R: [-160, 0, -18], LowerArm_R: [-45, 0, 0], Hand_R: [-25, 0, 0], UpperArm_L: [-155, 0, 22], LowerArm_L: [-55, 0, 0], UpperLeg_L: [-14, 0, 6], UpperLeg_R: [12, 0, -6], LowerLeg_L: [10, 0, 0], LowerLeg_R: [12, 0, 0], $hips: [0, -.03, -.02] };
  const strike = { Hips: [8, 0, 0], Spine: [16, 0, 0], Chest: [18, 0, 0], Head: [-18, 0, 0], UpperArm_R: [-78, 0, -10], LowerArm_R: [-8, 0, 0], Hand_R: [-6, 0, 0], UpperArm_L: [-74, 0, 12], LowerArm_L: [-12, 0, 0], UpperLeg_L: [-45, 0, 6], UpperLeg_R: [12, 0, -6], LowerLeg_L: [45, 0, 0], LowerLeg_R: [30, 0, 0], Foot_L: [0, 0, 0], Foot_R: [-20, 0, 0], $hips: [0, -.14, .04] };
  const gather = { Spine: [10, 0, 0], Chest: [14, 0, 0], Head: [-6, 0, 0], UpperArm_R: [-50, 0, -5], LowerArm_R: [-90, 0, 0], UpperArm_L: [-50, 0, 5], LowerArm_L: [-90, 0, 0], $hips: [0, -.06, 0] };
  const spread = { Spine: [-12, 0, 0], Chest: [-16, 0, 0], Head: [-20, 0, 0], UpperArm_R: [-40, 0, -95], LowerArm_R: [-10, 0, 0], UpperArm_L: [-40, 0, 95], LowerArm_L: [-10, 0, 0], $hips: [0, .02, 0] };
  const roar = { Spine: [-14, 0, 0], Chest: [-20, 0, 0], Neck: [-10, 0, 0], Head: [-28, 0, 0], UpperArm_R: [-20, 0, -70], LowerArm_R: [-40, 0, 0], UpperArm_L: [-20, 0, 70], LowerArm_L: [-40, 0, 0], UpperLeg_L: [-10, 0, 10], UpperLeg_R: [6, 0, -10], LowerLeg_L: [12, 0, 0], LowerLeg_R: [10, 0, 0], $hips: [0, -.05, 0] };
  return [
    A.idle('Idle', { duration: 2.8, breath: 4, sway: 1.5, armOut: 12, sword: 1 }),
    A.locomotion('Walk', { duration: 1.3, stride: 24, knee: 30, armSwing: 14, elbow: 20, lean: 6, bob: .05, twist: 6, sword: 1 }),
    { name: 'SlamWindup', duration: 1.0, loop: true, keys: [[0, over], [.5, { ...over, Chest: [-14, 2, 0], UpperArm_R: [-163, 0, -18] }], [1.0, over]] },
    { name: 'Slam', duration: .9, loop: false, events: { impact: .18 }, keys: [[0, over], [.18, strike], [.45, strike], [.9, {}]] },
    { name: 'Volley', duration: 1.5, loop: false, events: { fire: 1.2 }, keys: [[0, {}], [.5, gather], [1.1, gather], [1.22, spread], [1.5, {}]] },
    { name: 'Roar', duration: 2.4, loop: false, keys: [[0, {}], [.4, gather], [.8, roar], [1.6, { ...roar, Head: [-32, 4, 0] }], [2.4, {}]] },
    A.hit('Hit', .5, .8),
    A.death('Death', { duration: 2.2, hipsRest: -.5, back: -.4 }),
  ];
}
