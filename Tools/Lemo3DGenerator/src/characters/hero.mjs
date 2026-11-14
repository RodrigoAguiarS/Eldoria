// Herói jogável — ART_DIRECTION_AND_PROMPTS.md, Asset 01.
// "stylized low-poly 3D fantasy adventurer warrior, short sword, dark leather armor with steel pauldrons and
//  bracers, flowing royal-blue cape (#2F6BFF), slightly oversized head, chunky hands and boots, confident heroic
//  stance" · cabeça ~1/3 da altura · formas redondas e capa em "V" · ~3–5 mil triângulos · origem nos pés · 2 m.
import { sides, arm, leg, pauldron, eyes, torsoWeights, smooth, lerp, strut, ico, bevelBox, convex, lathe, torus, cyl, xform, jitter, extrude } from '../parts.mjs';
import { sheet } from '../geo.mjs';
import * as A from '../anim.mjs';

export const P = {
  height: 2.0, hipY: .70, spineY: .84, chestY: 1.02, neckY: 1.27, headY: 1.33,
  clavicle: .1, shoulderY: 1.21, shoulderX: .29, elbowY: .98, wristY: .77, gripDrop: .075, gripFwd: .01,
  legX: .115, legTopY: .66, kneeY: .38, ankleY: .11, toeZ: .13,
};

export const definition = {
  id: 'hero', name: 'Hero', role: 'Jogador (Player.prefab)', source: 'ART_DIRECTION_AND_PROMPTS.md — Asset 01',
  documented: true, heightMeters: 2.0, triangleBudget: [3000, 5000],
  materials: {   // valores do documento (URP Lit): smoothness/metallic
    skin: { color: '#E3B58F', smoothness: .2 },
    leather: { color: '#7A4B2A', smoothness: .25 },
    leatherDark: { color: '#4E3020', smoothness: .25, note: 'couro escuro (derivado do #7A4B2A em direção à sombra #1E1830)' },
    metal: { color: '#B9C4CF', metallic: .6, smoothness: .55 },
    cape: { color: '#2F6BFF', smoothness: .15 },
    brass: { color: '#E0B040', metallic: .7, smoothness: .5 },
    hair: { color: '#4A2C1A', smoothness: .15, note: 'suposição: cor do cabelo não definida no documento' },
    boot: { color: '#3B2418', smoothness: .25, note: 'couro escuro das botas (derivado)' },
    ink: { color: '#1E1830', smoothness: .1, note: 'olhos/boca na cor oficial de sombra' },
  },
  locomotion: { Idle: 0, Walk: 1.6, Run: 5 },   // PlayerMovement: 5 unidades/s
  extraBones: [['Cape_1', 'Chest', [0, .19, -.2]], ['Cape_2', 'Cape_1', [0, -.42, -.05]]],
  sockets: { Socket_Hand_R: { rot: [68, 0, 0] }, Socket_Hand_L: { rot: [0, 0, 0] } },
};

export function body(M) {
  const out = [], tw = torsoWeights(P);
  // —— tronco: túnica/armadura de couro (sólido de revolução octogonal achatado) ——
  out.push({ geo: jitter(lathe([[1e-4, .55], [.2, .57], [.215, .64], [.19, .76], [.195, .86], [.23, .98], [.245, 1.1], [.235, 1.19], [.16, 1.27], [1e-4, 1.29]], 10, { sz: .78 }), .008, 2), mat: M.leather, weights: tw });
  // placa peitoral de couro mais escuro (silhueta e leitura de armadura)
  out.push({ geo: jitter(convex([[-.17, .9, .13], [.17, .9, .13], [-.2, 1.16, .15], [.2, 1.16, .15], [-.12, 1.22, .12], [.12, 1.22, .12], [0, 1.0, .205], [0, 1.15, .21], [-.15, .92, .08], [.15, .92, .08], [-.19, 1.17, .08], [.19, 1.17, .08]]), .006, 5), mat: M.leatherDark, bone: 'Chest' });
  // talabarte (ombro esquerdo → quadril direito) com argola de latão
  out.push({ geo: strut([.17, 1.19, .17], [-.17, .83, .17], .02, .02, 5), mat: M.leatherDark, weights: tw });
  out.push({ geo: torus(.03, .01, 6, 3, { pos: [.02, 1.02, .2] }), mat: M.brass, bone: 'Chest' });
  // cinto + fivela
  out.push({ geo: torus(.205, .028, 10, 3, { pos: [0, .8, 0], rot: [90, 0, 0], scale: [1, .78, 1] }), mat: M.leatherDark, bone: 'Spine' });
  out.push({ geo: xform(bevelBox(.08, .065, .03, .25), { pos: [0, .8, .17] }), mat: M.brass, bone: 'Spine' });
  // saiote: quatro placas (tassets) em volta do quadril
  for (const [ang, w] of [[0, .2], [180, .22], [90, .14], [-90, .14]]) {
    const a = ang * Math.PI / 180;
    out.push({ geo: xform(jitter(bevelBox(w, .17, .04, .25, { bottom: 1.12 }), .006, ang + 9), { pos: [Math.sin(a) * .2, .64, Math.cos(a) * .16], rot: [ang === 0 ? -8 : ang === 180 ? 8 : 0, ang, ang === 90 ? 8 : ang === -90 ? -8 : 0] }), mat: M.leatherDark, bone: 'Hips' });
  }
  // rebites
  for (const sx of [-1, 1]) out.push({ geo: ico(.018, 0, { pos: [sx * .12, 1.12, .18] }), mat: M.brass, bone: 'Chest' });
  // pescoço
  out.push({ geo: cyl(.075, .085, .12, 7, { pos: [0, 1.3, 0] }), mat: M.skin, bone: 'Neck' });

  // —— cabeça (~1/3 da altura): crânio redondo facetado, cabelo, franja, rosto ——
  const hc = [0, 1.63, .02];
  out.push({ geo: jitter(ico(.27, 1, { pos: hc, scale: [1, 1.1, .96] }), .01, 21), mat: M.skin, bone: 'Head' });
  out.push({ geo: ico(.05, 0, { pos: [.268, 1.61, 0], scale: [.6, 1, 1] }), mat: M.skin, bone: 'Head' });
  out.push({ geo: ico(.05, 0, { pos: [-.268, 1.61, 0], scale: [.6, 1, 1] }), mat: M.skin, bone: 'Head' });
  {
    // cabelo: casca convexa sobre o topo/nuca (pontos de um icosaedro maior acima da testa ou atrás)
    const pts = [];
    const ic = ico(.292, 1, { scale: [1.03, 1.08, 1] }).attributes.position;
    for (let i = 0; i < ic.count; i++) { const x = ic.getX(i), y = ic.getY(i), z = ic.getZ(i); if (y > .07 - z * .1 || z < -.09) pts.push([x + hc[0], y + hc[1] + .02, z + hc[2] - .01]); }
    out.push({ geo: jitter(convex(pts), .014, 31), mat: M.hair, bone: 'Head' });
    // franja em mechas
    for (const [x, d] of [[-.15, .09], [-.06, .12], [.04, .1], [.13, .08]]) out.push({ geo: jitter(convex([[x - .07, 1.83, .17], [x + .07, 1.83, .17], [x, 1.84, .24], [x + .02, 1.84 - d, .27], [x - .05, 1.8, .23]]), .006, x * 100), mat: M.hair, bone: 'Head' });
  }
  out.push(...eyes(M, { y: 1.645, z: .262, dx: .095, w: .055, h: .075 }));
  for (const s of [-1, 1]) out.push({ geo: xform(bevelBox(.08, .022, .025, .3), { pos: [s * .1, 1.725, .258], rot: [0, 0, s * -8] }), mat: M.hair, bone: 'Head' });   // sobrancelhas
  out.push({ geo: convex([[-.025, 1.62, .27], [.025, 1.62, .27], [0, 1.575, .3], [0, 1.6, .25]]), mat: M.skin, bone: 'Head' });      // nariz
  out.push({ geo: xform(bevelBox(.07, .016, .02, .3), { pos: [0, 1.53, .262] }), mat: M.ink, bone: 'Head' });                       // boca

  // —— membros ——
  for (const [s, sx] of sides) {
    out.push(...arm(P, M, s, sx, { armR: .068, hand: .155 }));
    out.push(...leg(P, M, s, sx, { legR: .09, footLen: .3, footW: .088, cuffMat: M.leather }));
    out.push(...pauldron(P, M, s, sx, { r: .16, h: .115 }));
    out.push({ geo: ico(.03, 0, { pos: [sx * .12, 1.24, .15] }), mat: M.brass, bone: 'Chest' });   // presilhas da capa
  }

  // —— capa em "V" (malha dupla face com espessura, presa ao peito, com 2 ossos de capa) ——
  const capeTop = 1.25, hem = u => .46 - .2 * (1 - Math.abs(2 * u - 1));
  const capePt = (u, v) => {
    const w = lerp(.22, .37, v), x = lerp(-w, w, u), y = lerp(capeTop, hem(u), v);
    const z = -.2 - .05 * v - .055 * (1 - Math.pow(2 * u - 1, 2)) - .02 * Math.sin(u * Math.PI * 6) * v;   // dobras leves
    return [x, y, z];
  };
  out.push({
    geo: sheet(6, 6, capePt, .022, (u) => [Math.sin((u - .5) * 1.4) * .4, 0, -1]), mat: M.cape,
    weights: (x, y) => { const v = (capeTop - y) / (capeTop - .32); const a = smooth(0, .38, v), b = smooth(.38, 1, v); return [['Chest', 1 - a], ['Cape_1', a * (1 - b)], ['Cape_2', a * b]]; },
  });
  // gola da capa
  out.push({ geo: jitter(torus(.2, .035, 10, 3, { pos: [0, 1.25, -.03], rot: [90, 0, 0], scale: [1.1, .85, 1] }), .006, 41), mat: M.cape, bone: 'Chest' });
  return out;
}

// espada curta na mão direita (espaço local do Socket_Hand_R: lâmina ao longo de +Z)
export function equipment(M) {
  return [{
    socket: 'Socket_Hand_R', name: 'Sword', parts: [
      { geo: extrude([[-.037, 0], [.037, 0], [.034, .5], [0, .6], [-.034, .5]], .016, { bevel: .006, o: { rot: [90, 0, 0], pos: [0, 0, .1] } }), mat: M.metal },
      { geo: xform(bevelBox(.04, .006, .5, .3), { pos: [0, .013, .36] }), mat: M.metal },                    // nervura
      { geo: xform(bevelBox(.23, .036, .046, .3), { pos: [0, 0, .09] }), mat: M.brass },                    // guarda
      { geo: cyl(.023, .023, .16, 6, { rot: [90, 0, 0], pos: [0, 0, 0] }), mat: M.leatherDark },           // cabo
      { geo: ico(.035, 0, { pos: [0, 0, -.095] }), mat: M.brass },                                         // pomo
    ],
  }];
}

const swordGuard = (k = 1) => () => ({ Cape_1: [12 * k, 0, 0], Cape_2: [8 * k, 0, 0] });
export function clips() {
  const flutter = (base, amp, f = 1) => (u) => ({ Cape_1: [base + amp * Math.sin(u * Math.PI * 2 * f), 0, 1.5 * Math.sin(u * Math.PI * 2)], Cape_2: [base * .6 + amp * Math.sin(u * Math.PI * 2 * f - 1), 0, 0] });
  const withCape = (c, fn) => { const ex = c.extra; c.extra = (u, t) => ({ ...(ex ? ex(u, t) : {}), ...fn(u, t) }); return c; };
  return [
    withCape(A.idle('Idle', { duration: 2.4, sword: 1 }), flutter(3, 1.5)),
    withCape(A.locomotion('Walk', { duration: 1.0, stride: 26, knee: 32, armSwing: 22, elbow: 14, lean: 4, bob: .035, sword: 1 }), flutter(12, 4, 2)),
    withCape(A.locomotion('Run', { duration: .64, stride: 42, knee: 70, armSwing: 40, elbow: 55, lean: 14, bob: .06, twist: 7, sword: 1 }), flutter(32, 7, 2)),
    withCape(A.slash('Attack', { duration: .7, windup: .22, hit: .32 }), swordGuard(1)),
    withCape(A.slash('HeavyAttack', { duration: 1.2, windup: .55, hit: .7, heavy: true }), (u) => ({ Cape_1: [u < .55 ? 6 : 18, 0, 0], Cape_2: [u < .55 ? 4 : 12, 0, 0] })),
    withCape(A.slash('Attack_01', { duration: .45, windup: .10, hit: .16 }), swordGuard(1)),
    withCape(A.heroCounterSlash('Attack_02', { duration: .5, windup: .12, hit: .19 }), swordGuard(1.2)),
    withCape(A.slash('Attack_03', { duration: .72, windup: .24, hit: .32, heavy: true }), swordGuard(1.5)),
    withCape(A.heroSpin('Special', .9), (u) => ({ Cape_1: [18, 0, -12 * Math.sin(u * Math.PI)], Cape_2: [25, 0, -20 * Math.sin(u * Math.PI)] })),
    withCape(A.dash('Dash', .4), () => ({ Cape_1: [34, 0, 0], Cape_2: [18, 0, 0] })),
    withCape(A.hit('Hit', .45), (u) => ({ Cape_1: [10 * Math.sin(u * Math.PI), 0, 0] })),
    withCape(A.death('Death', { duration: 1.6, hipsRest: -.52, back: -.45 }), (u) => ({ Cape_1: [-15 * u, 0, 0], Cape_2: [-30 * u, 0, 0] })),
    withCape(A.victory('Victory', 2.0), (u) => ({ Cape_1: [6 + 4 * Math.sin(u * 9), 0, 0] })),
  ];
}
