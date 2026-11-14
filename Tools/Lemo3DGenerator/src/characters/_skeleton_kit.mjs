// Kit da família "esqueleto corrompido" (ART_DIRECTION_AND_PROMPTS.md, Asset 02):
// "corrupted skeleton warrior · rusty jagged sword · damaged cracked bone-and-iron armor · glowing red eyes (#FF2A2A)
//  · hunched aggressive stance · exaggerated angular shapes and oversized skull · same proportions family as the hero"
// Materiais: osso #CFC6A8 (sm .2) · armadura corroída #3E4152 + ferrugem #8A4A22 (metallic .4/.5) · olhos #FF2A2A emissão HDR ~3.
import { sides, smooth, strut, ico, bevelBox, convex, lathe, torus, cyl, cone, xform, jitter, extrude } from '../parts.mjs';

export const SKELETON_P = {
  height: 2.0, hipY: .68, spineY: .82, chestY: 1.0, neckY: 1.24, headY: 1.31,
  clavicle: .1, shoulderY: 1.19, shoulderX: .27, elbowY: .95, wristY: .74, gripDrop: .06, gripFwd: .01,
  legX: .11, legTopY: .64, kneeY: .37, ankleY: .1, toeZ: .12,
};

export const SKELETON_MATERIALS = {
  bone: { color: '#CFC6A8', smoothness: .2 },
  boneDark: { color: '#8F8770', smoothness: .15, note: 'osso sombreado (derivado do #CFC6A8)' },
  armor: { color: '#3E4152', metallic: .4, smoothness: .35 },
  rust: { color: '#8A4A22', metallic: .5, smoothness: .3 },
  socket: { color: '#1E1830', smoothness: .05, note: 'órbitas/cavidades na cor oficial de sombra' },
  eyes: { color: '#FF2A2A', emissive: '#FF2A2A', emissiveIntensity: 3, smoothness: .5 },
};

// hunch: postura curvada e agressiva aplicada a todos os clips (aditiva)
export const HUNCH = { Spine: [9, 0, 0], Chest: [12, 0, 0], Neck: [6, 0, 0], Head: [-16, 0, 0], UpperArm_L: [-6, 0, 6], UpperArm_R: [-6, 0, -6], LowerArm_L: [-10, 0, 0], LowerArm_R: [-10, 0, 0], UpperLeg_L: [-8, 0, 3], UpperLeg_R: [-8, 0, -3], LowerLeg_L: [14, 0, 0], LowerLeg_R: [14, 0, 0], Foot_L: [-6, 0, 0], Foot_R: [-6, 0, 0], $hips: [0, -.035, 0] };

const tw = P => { const a = (P.hipY + P.spineY) / 2, b = (P.spineY + P.chestY) / 2; return (x, y) => { const s1 = smooth(a - .08, a + .08, y), s2 = smooth(b - .08, b + .08, y); return [['Hips', 1 - s1], ['Spine', s1 * (1 - s2)], ['Chest', s2]]; }; };

export function skull(P, M, o = {}) {
  const out = [], c = [0, (o.cy ?? 1.6), .03], r = o.r ?? .27, eye = o.eyeMat ?? M.eyes;
  // crânio facetado e oversized, com maxilar anguloso
  out.push({ geo: jitter(ico(r, 1, { pos: c, scale: [1, 1.0, 1.08] }), .018, 51), mat: M.bone, bone: 'Head' });
  out.push({ geo: jitter(convex([[-.17, c[1] - .14, .02], [.17, c[1] - .14, .02], [-.13, c[1] - .3, .12], [.13, c[1] - .3, .12], [-.08, c[1] - .32, .24], [.08, c[1] - .32, .24], [-.16, c[1] - .12, .2], [.16, c[1] - .12, .2], [0, c[1] - .2, .27]]), .01, 53), mat: M.bone, bone: 'Head' });
  // z da superfície frontal do crânio (elipsoide r × r × 1,08r centrado em c) para assentar os detalhes
  const S = (x, dy, out = 0) => [x, c[1] + dy, c[2] + r * 1.08 * Math.sqrt(Math.max(0, 1 - (x / r) ** 2 - (dy / r) ** 2)) + out];
  // arcadas: cristas angulosas acima das órbitas
  for (const s of [-1, 1]) out.push({ geo: convex([S(s * .02, .07, .01), S(s * .18, .09, .01), S(s * .19, .03, .02), S(s * .04, .02, .03), S(s * .1, .1, -.03), S(s * .1, .06, .045)]), mat: M.bone, bone: 'Head' });
  // órbitas escuras (placas rasas sobre a superfície) + olhos emissivos à frente delas
  for (const s of [-1, 1]) {
    out.push({ geo: convex([S(s * .03, .03, .006), S(s * .16, .04, .006), S(s * .15, -.07, .006), S(s * .04, -.06, .006), S(s * .1, -.015, -.03)]), mat: M.socket, bone: 'Head' });
    const e = S(s * .095, -.012, .012);
    out.push({ geo: ico(.036, 0, { pos: e, scale: [1.25, .85, .5] }), mat: eye, bone: 'Head' });
  }
  out.push({ geo: convex([S(-.03, -.1, .006), S(.03, -.1, .006), S(0, -.04, .008), S(0, -.08, -.03)]), mat: M.socket, bone: 'Head' });   // cavidade nasal
  for (let k = -3; k <= 3; k++) out.push({ geo: bevelBox(.026, .045, .02, .2), mat: M.bone, bone: 'Head', at: [k * .03, c[1] - .2, .262 - Math.abs(k) * .008] });     // dentes
  return out.map(p => p.at ? { ...p, geo: xform(p.geo, { pos: p.at }) } : p);
}

export function boneLimb(a, b, r, M, bone, knob = r * 1.5) {
  return [{ geo: strut(a, b, r, r * .85, 5), mat: M.bone, bone }, { geo: ico(knob, 0, { pos: b }), mat: M.bone, bone }];
}

export function claw(P, M, side, sx, o = {}) {
  const x = sx * P.shoulderX, y = P.wristY - P.gripDrop, out = [], s = o.s ?? 1;
  out.push({ geo: xform(bevelBox(.1 * s, .11 * s, .1 * s, .3), { pos: [x, y + .02, .01] }), mat: M.bone, bone: `Hand_${side}` });
  for (let k = -1; k <= 1; k++) out.push({ geo: strut([x + sx * k * .03 * s, y - .02, .05 * s], [x + sx * k * .032 * s, y - .09 * s, .07 * s], .016 * s, .01 * s, 4), mat: M.bone, bone: `Hand_${side}` });
  out.push({ geo: strut([x - sx * .05 * s, y + .01, .04], [x - sx * .06 * s, y - .05 * s, .08 * s], .016 * s, .01 * s, 4), mat: M.bone, bone: `Hand_${side}` });
  return out;
}

// corpo base do esqueleto; o: { armorChest, pauldronL, pauldronR, greave, bracerR, cloth }
export function skeletonBody(P, M, o = {}) {
  const out = [], W = tw(P);
  // pélvis e coluna
  out.push({ geo: jitter(convex([[-.17, .62, -.06], [.17, .62, -.06], [-.2, .72, 0], [.2, .72, 0], [-.12, .58, .09], [.12, .58, .09], [0, .55, .03], [-.08, .74, -.1], [.08, .74, -.1]]), .012, 61), mat: M.bone, bone: 'Hips' });
  for (let k = 0; k < 5; k++) { const y = .74 + k * .07; out.push({ geo: xform(bevelBox(.075, .045, .07, .3), { pos: [0, y, -.05] }), mat: M.boneDark, weights: W }); }
  // caixa torácica: anéis de costela achatados + esterno
  [[.92, .17], [1.0, .2], [1.08, .205], [1.15, .17]].forEach(([y, r], i) => out.push({ geo: jitter(torus(r, .022, 9, 3, { pos: [0, y, 0], rot: [90, 0, 0], scale: [1, .72, 1] }), .01, 70 + i), mat: M.bone, bone: 'Chest' }));
  out.push({ geo: xform(bevelBox(.05, .26, .04, .3), { pos: [0, 1.04, .14] }), mat: M.bone, bone: 'Chest' });
  out.push({ geo: strut([-.2, 1.18, 0], [.2, 1.18, 0], .028, .028, 5), mat: M.bone, bone: 'Chest' });   // clavículas
  out.push({ geo: cyl(.04, .05, .12, 5, { pos: [0, 1.26, -.01] }), mat: M.boneDark, bone: 'Neck' });
  // armadura de ferro rachada no peito (assimétrica)
  if (o.armorChest !== false) {
    out.push({ geo: jitter(convex([[-.21, .92, .1], [.08, .9, .16], [-.22, 1.17, .1], [.06, 1.2, .17], [-.12, 1.22, .08], [-.05, 1.06, .2], [-.2, 1.05, -.02], [.0, 1.18, .05]]), .014, 81), mat: M.armor, bone: 'Chest' });
    out.push({ geo: strut([-.21, .93, .11], [.07, .91, .17], .012, .012, 4), mat: M.rust, bone: 'Chest' });
    if (o.heavyChest) out.push({ geo: jitter(convex([[.04, .9, .15], [.22, .92, .1], [.05, 1.18, .17], [.22, 1.15, .1], [.15, 1.22, .06], [.1, 1.05, .2], [.2, 1.05, -.02]]), .014, 83), mat: M.armor, bone: 'Chest' });
  }
  // saiote em tiras (ferro + ferrugem)
  if (o.cloth !== false) {
    out.push({ geo: torus(.19, .025, 9, 3, { pos: [0, .7, 0], rot: [90, 0, 0], scale: [1, .75, 1] }), mat: M.rust, bone: 'Hips' });
    for (const [a, w, h] of [[0, .14, .2], [35, .1, .16], [-35, .1, .17], [180, .16, .22], [145, .1, .15], [-145, .1, .18]]) {
      const r = a * Math.PI / 180;
      out.push({ geo: xform(jitter(convex([[-w / 2, 0, 0], [w / 2, 0, 0], [-w / 2, 0, .02], [w / 2, 0, .02], [-w * .4, -h, .03], [w * .35, -h * .85, .03], [0, -h * 1.1, .02]]), .01, a), { pos: [Math.sin(r) * .19, .69, Math.cos(r) * .14], rot: [0, a, 0] }), mat: a % 2 ? M.armor : M.rust, bone: 'Hips' });
    }
  }
  // membros ósseos
  for (const [s, sx] of sides) {
    const x = sx * P.shoulderX, lx = sx * P.legX;
    out.push({ geo: ico(.055, 0, { pos: [x, P.shoulderY, 0] }), mat: M.bone, bone: `UpperArm_${s}` });
    out.push(...boneLimb([x, P.shoulderY, 0], [x, P.elbowY, 0], .035, M, `UpperArm_${s}`).map((p, i) => i ? { ...p, bone: `LowerArm_${s}` } : p));
    out.push({ geo: strut([x, P.elbowY, 0], [x, P.wristY, 0], .032, .028, 5), mat: M.bone, bone: `LowerArm_${s}` });
    out.push({ geo: strut([x + sx * .03, P.elbowY - .02, 0], [x + sx * .025, P.wristY + .02, 0], .016, .014, 4), mat: M.boneDark, bone: `LowerArm_${s}` });
    out.push(...claw(P, M, s, sx));
    out.push({ geo: ico(.06, 0, { pos: [lx, P.legTopY, 0] }), mat: M.bone, bone: `UpperLeg_${s}` });
    out.push(...boneLimb([lx, P.legTopY, 0], [lx, P.kneeY, .01], .042, M, `UpperLeg_${s}`, .062).map((p, i) => i ? { ...p, bone: `LowerLeg_${s}` } : p));
    out.push({ geo: strut([lx, P.kneeY, .01], [lx, P.ankleY + .02, 0], .036, .03, 5), mat: M.bone, bone: `LowerLeg_${s}` });
    out.push({ geo: jitter(convex([[lx - .055, 0, -.06], [lx + .055, 0, -.06], [lx - .05, .1, -.03], [lx + .05, .1, -.03], [lx - .07, 0, .2], [lx + .07, 0, .2], [lx - .04, .05, .19], [lx + .04, .05, .19]]), .01, 90 + sx), mat: M.bone, bone: `Foot_${s}` });
    if (o.bracers?.includes(s)) out.push({ geo: strut([x, P.elbowY - .03, 0], [x, P.wristY + .02, 0], .06, .068, 6), mat: M.rust, bone: `LowerArm_${s}` });
    if (o.greaves?.includes(s)) out.push({ geo: jitter(strut([lx, P.kneeY - .02, .01], [lx, P.ankleY + .05, 0], .07, .066, 6), .012, 95), mat: M.armor, bone: `LowerLeg_${s}` });
  }
  return out;
}

// ombreira de ferro angulosa com espinhos (inimigos = formas espinhosas)
export function spikedPauldron(P, M, side, sx, o = {}) {
  const k = o.k ?? 1, x = sx * (P.shoulderX + .02), y = P.shoulderY + .04, out = [];
  out.push({ geo: jitter(convex([[x - .13 * k, y - .03, -.12 * k], [x + .13 * k, y - .03, -.12 * k], [x - .13 * k, y - .03, .12 * k], [x + .13 * k, y - .03, .12 * k], [x, y + .1 * k, 0], [x + sx * .16 * k, y - .08, 0], [x - sx * .06, y + .06 * k, .06], [x - sx * .06, y + .06 * k, -.06]]), .016, 100 + sx), mat: o.mat ?? M.armor, bone: `Shoulder_${side}` });
  const n = o.spikes ?? 2;
  for (let i = 0; i < n; i++) {
    const t = n === 1 ? .5 : i / (n - 1), z = (t - .5) * .14 * k;
    out.push({ geo: cone(.04 * k, .2 * k, 4, { pos: [x + sx * .05, y + .13 * k, z], rot: [0, 0, -sx * 35] }), mat: o.spikeMat ?? M.rust, bone: `Shoulder_${side}` });
  }
  out.push({ geo: strut([x - .12 * k, y - .04, .1 * k], [x + .12 * k, y - .04, .1 * k], .012, .012, 4), mat: M.rust, bone: `Shoulder_${side}` });
  return out;
}

// espada enferrujada e serrilhada (espaço local do soquete: lâmina em +Z)
export function jaggedSword(M, len = .62, w = .085) {
  const pts = [[-w / 2, 0], [w / 2, 0], [w * .55, len * .25], [w * .3, len * .3], [w * .5, len * .55], [w * .25, len * .62], [w * .4, len * .85], [0, len], [-w * .35, len * .78], [-w * .55, len * .6], [-w * .35, len * .52], [-w * .5, len * .3]];
  return [
    { geo: extrude(pts, .018, { bevel: .005, o: { rot: [90, 0, 0], pos: [0, 0, .09] } }), mat: M.rust },
    { geo: xform(bevelBox(.2, .04, .05, .25), { pos: [0, 0, .08], rot: [0, 0, 8] }), mat: M.armor },
    { geo: cyl(.022, .022, .15, 5, { rot: [90, 0, 0] }), mat: M.armor },
    { geo: cone(.03, .06, 4, { rot: [-90, 0, 0], pos: [0, 0, -.1] }), mat: M.armor },
  ];
}

export { sides };
