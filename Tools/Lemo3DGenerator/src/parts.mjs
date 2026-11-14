// Peças reutilizáveis de corpo e equipamento (geometria em espaço de bind, metros).
// Cada função devolve uma lista de peças { geo, mat, bone } ou { geo, mat, weights }.
import { THREE } from './lemo-three.mjs';
import { strut, ico, bevelBox, convex, lathe, torus, cyl, xform, jitter, extrude } from './geo.mjs';

export const sides = [['L', 1], ['R', -1]];
export const lerp = (a, b, t) => a + (b - a) * t;
export const smooth = (a, b, x) => { const t = Math.min(1, Math.max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t); };

// pesos do tronco por altura: Hips → Spine → Chest (deformação suave do tronco inteiriço)
export function torsoWeights(P) {
  const a = (P.hipY + P.spineY) / 2, b = (P.spineY + P.chestY) / 2;
  return (x, y) => {
    const s1 = smooth(a - .1, a + .1, y), s2 = smooth(b - .1, b + .1, y);
    return [['Hips', 1 - s1], ['Spine', s1 * (1 - s2)], ['Chest', s2]];
  };
}

// braço: manga, cotovelo, antebraço/braçadeira, punho
export function arm(P, M, side, sx, o = {}) {
  const x = sx * P.shoulderX, r = o.armR ?? .07, out = [];
  out.push({ geo: strut([x, P.shoulderY - .02, 0], [x, P.elbowY + .01, 0], r, r * .92, o.armSeg ?? 6), mat: o.sleeve ?? M.leather, bone: `UpperArm_${side}` });
  out.push({ geo: ico(r * .95, 0, { pos: [x, P.elbowY, 0] }), mat: o.elbow ?? o.sleeve ?? M.leather, bone: `LowerArm_${side}` });
  out.push({ geo: strut([x, P.elbowY - .01, 0], [x, P.wristY + .01, 0], r * 1.05, r * 1.2, o.armSeg ?? 6), mat: o.bracer ?? M.metal, bone: `LowerArm_${side}` });
  if (o.bracerRim !== false) out.push({ geo: torus(r * 1.2, .012, 8, 3, { pos: [x, P.wristY + .015, 0], rot: [90, 0, 0] }), mat: o.rim ?? M.brass, bone: `LowerArm_${side}` });
  out.push(...fist(P, M, side, sx, o));
  return out;
}

export function fist(P, M, side, sx, o = {}) {
  const x = sx * P.shoulderX, y = P.wristY - P.gripDrop, s = o.hand ?? .15, out = [];
  out.push({ geo: jitter(bevelBox(s * .9, s, s * .95, .28), .006, 3), mat: o.skin ?? M.skin, bone: `Hand_${side}`, at: [x, y, .01] });
  out.push({ geo: bevelBox(s * .3, s * .45, s * .35, .3), mat: o.skin ?? M.skin, bone: `Hand_${side}`, at: [x - sx * s * .4, y + s * .05, s * .35] });   // polegar
  // knuckles (relevo dos dedos)
  for (let k = 0; k < 3; k++) out.push({ geo: bevelBox(s * .26, s * .2, s * .18, .3), mat: o.skin ?? M.skin, bone: `Hand_${side}`, at: [x + sx * (k - 1) * s * .28, y - s * .32, s * .3] });
  return out.map(p => p.at ? { ...p, geo: xform(p.geo, { pos: p.at }) } : p);
}

// perna: coxa, joelho, cano da bota, pé da bota com sola
export function leg(P, M, side, sx, o = {}) {
  const x = sx * P.legX, r = o.legR ?? .09, out = [];
  out.push({ geo: strut([x, P.legTopY + .04, 0], [x, P.kneeY + .01, 0], r * 1.05, r * .9, 7), mat: o.thigh ?? M.leatherDark, bone: `UpperLeg_${side}` });
  out.push({ geo: ico(r * .88, 0, { pos: [x, P.kneeY, .01] }), mat: o.knee ?? o.thigh ?? M.leatherDark, bone: `LowerLeg_${side}` });
  out.push({ geo: strut([x, P.kneeY - .01, 0], [x, P.ankleY + .02, 0], r * .95, r * 1.02, 7), mat: o.boot ?? M.boot, bone: `LowerLeg_${side}` });
  if (o.cuff !== false) out.push({ geo: xform(lathe([[1e-4, -.035], [r * 1.18, -.03], [r * 1.25, .03], [1e-4, .035]], 8), { pos: [x, P.kneeY - .05, 0] }), mat: o.cuffMat ?? M.boot, bone: `LowerLeg_${side}` });
  out.push(...boot(P, M, side, sx, o));
  return out;
}

export function boot(P, M, side, sx, o = {}) {
  const x = sx * P.legX, w = o.footW ?? .085, len = o.footLen ?? .3, h = P.ankleY + .03;
  const pts = [];
  for (const s of [-1, 1]) pts.push([x + s * w, 0, -.08], [x + s * w * .9, h, -.07], [x + s * w * 1.05, 0, len * .55], [x + s * w * .8, h * .75, len * .5], [x + s * w * .7, 0, len * .72], [x + s * w * .45, h * .45, len * .72]);
  pts.push([x, h * .2, len * .78]);
  const out = [{ geo: jitter(convex(pts), .006, 7 + sx), mat: o.boot ?? M.boot, bone: `Foot_${side}` }];
  out.push({ geo: xform(bevelBox(w * 2.15, .025, len * .9, .2), { pos: [x, .0125, len * .3 - .04] }), mat: o.sole ?? M.sole ?? M.boot, bone: `Foot_${side}` });
  return out;
}

// ombreira em domo inclinada para fora, com friso e rebite
export function pauldron(P, M, side, sx, o = {}) {
  const r = o.r ?? .16, h = o.h ?? .11, out = [];
  const tilt = { pos: [sx * (P.shoulderX + (o.dx ?? .01)), P.shoulderY + (o.dy ?? .03), o.dz ?? 0], rot: [0, 0, -sx * (o.tilt ?? 28)] };
  out.push({ geo: xform(jitter(lathe([[1e-4, h], [r * .7, h * .86], [r, h * .35], [r * 1.04, 0], [1e-4, -.005]], o.seg ?? 7, { sz: o.sz ?? .95 }), .006, 11), tilt), mat: o.mat ?? M.metal, bone: `Shoulder_${side}` });
  if (o.trim !== false) out.push({ geo: xform(torus(r * 1.02, .016, 8, 3, { rot: [90, 0, 0], pos: [0, .005, 0] }), tilt), mat: o.trimMat ?? M.brass, bone: `Shoulder_${side}` });
  if (o.rivet !== false) out.push({ geo: xform(ico(.022, 0, { pos: [0, h + .005, 0] }), tilt), mat: o.trimMat ?? M.brass, bone: `Shoulder_${side}` });
  for (const sp of o.spikes || []) out.push({ geo: xform(cone(sp.r ?? .035, sp.h ?? .12, 4, { pos: sp.pos, rot: sp.rot ?? [0, 0, 0] }), tilt), mat: o.spikeMat ?? o.mat ?? M.metal, bone: `Shoulder_${side}` });
  return out;
}
import { cone } from './geo.mjs';

// olhos simples (caixas escuras) com brilho opcional
export function eyes(M, { y, z, dx = .09, w = .05, h = .07, mat, bone = 'Head', glow }) {
  const out = [];
  for (const s of [-1, 1]) {
    out.push({ geo: xform(bevelBox(w, h, .03, .3), { pos: [s * dx, y, z] }), mat: mat ?? M.ink, bone });
    if (glow) out.push({ geo: xform(bevelBox(w * .4, h * .35, .01, .2), { pos: [s * dx - w * .15, y + h * .2, z + .016] }), mat: glow, bone });
  }
  return out;
}

export { THREE, strut, ico, bevelBox, convex, lathe, torus, cyl, xform, jitter, extrude, cone };
