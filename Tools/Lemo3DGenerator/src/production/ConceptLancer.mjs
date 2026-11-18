#!/usr/bin/env node
// A13 — Lanceiro corrompido: FONTE DE RENDER do conceito 2D (PROMPT_CATALOG.md, A13; condicional F13, já prototipado).
// NÃO é modelo de produção: não exporta FBX, não entra no catálogo de personagens nem no Unity. O GLB gerado aqui
// existe só para a página preview/concept_lancer.html desenhar a folha 2048×1536 com a mesma linguagem low-poly da
// família SkeletonMelee/Elite (mesmo kit, mesmo crânio, mesma paleta). Modelagem/rig Generic/clip serão tarefas novas.
//   node src/production/ConceptLancer.mjs [--out <pasta>]
import { mkdirSync, writeFileSync } from 'node:fs';
import { join, resolve, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import { assemble, toGLB } from '../build.mjs';
import { THREE_VERSION } from '../lemo-three.mjs';
import { SKELETON_P, SKELETON_MATERIALS, HUNCH, skeletonBody, skull } from '../characters/_skeleton_kit.mjs';
import { convex, jitter, strut, ico, cone, torus, cyl, bevelBox, xform, extrude } from '../parts.mjs';

const HERE = dirname(fileURLToPath(import.meta.url)), LAB = resolve(HERE, '..', '..'), PROJECT = resolve(LAB, '..', '..');
const argv = process.argv.slice(2), arg = (k, d) => { const i = argv.indexOf('--' + k); return i >= 0 ? argv[i + 1] : d; };
const OUT = resolve(arg('out', join(PROJECT, 'Assets/_Project/Art/Concepts/Source~/SkeletonLancer')));

export const P = SKELETON_P;   // mesmas proporções do melee: cabeça ~1/3 da altura, 2 m

// paleta pedida pela ficha A13 (idêntica à família): osso #CFC6A8 · ferro #3E4152 · ferrugem #8A4A22 · olhos #FF2A2A
export const MATERIALS = SKELETON_MATERIALS;

// Lança curta e grossa (espaço local: haste em +Z; origem = mão de trás). A página de render orienta a lança da mão
// direita (trás) para a esquerda (frente), então as duas mãos sempre seguram a mesma haste em todas as vistas.
// Comprimento total 1,28 m (0,30 m atrás da mão de trás); na guarda a ponta fica ~0,9 m à frente do pivô. Não define alcance de dano: o dano da F13 é a distância do
// segmento percorrido até o alvo (hitRadius 0,75 m), independente da arma.
export const SPEAR = { butt: -.30, shaftEnd: .70, headLen: .26, headW: .15, shaftR: .036 };
export function spear(M) {
  const S = SPEAR, out = [];
  out.push({ geo: strut([0, 0, S.butt], [0, 0, S.shaftEnd], S.shaftR, S.shaftR * .92, 6), mat: M.boneDark });       // haste grossa (madeira/osso escurecido)
  out.push({ geo: cone(S.shaftR * 1.5, .1, 5, { rot: [-90, 0, 0], pos: [0, 0, S.butt - .03] }), mat: M.armor });      // ponteira de ferro
  for (const z of [.05, .36]) out.push({ geo: cyl(S.shaftR * 1.35, S.shaftR * 1.35, .07, 6, { rot: [90, 0, 0], pos: [0, 0, z] }), mat: M.rust });   // amarras
  out.push({ geo: cyl(S.shaftR * 1.6, S.shaftR * 1.25, .1, 6, { rot: [90, 0, 0], pos: [0, 0, S.shaftEnd - .02] }), mat: M.armor });   // colar
  // ponta larga em folha, facetada e grossa (lida de longe; nada de lança fina)
  const L = S.headLen, w = S.headW, z0 = S.shaftEnd + .02;
  out.push({ geo: jitter(convex([[0, 0, z0 + L], [-w / 2, 0, z0 + L * .32], [w / 2, 0, z0 + L * .32], [-w * .3, 0, z0], [w * .3, 0, z0], [0, .035, z0 + L * .35], [0, -.035, z0 + L * .35], [0, .02, z0 + .02], [0, -.02, z0 + .02]]), .006, 301), mat: M.rust });
  // farpas de ferro na base da ponta (forma angulosa/espinhosa da família)
  for (const s of [-1, 1]) out.push({ geo: cone(.022, .09, 4, { pos: [s * w * .42, 0, z0 + .01], rot: [-90, 0, s * 0] }), mat: M.armor });
  return out;
}

// Ombreira assimétrica: placa de ferro em lâminas sobrepostas só no ombro esquerdo (o ombro que vai à frente na
// postura), larga e achatada como um escudo de ombro; o direito fica nu. Contrasta com a ombreira-domo espinhosa
// do melee e com o par simétrico da elite.
function layeredPauldron(M) {
  const y = P.shoulderY + .06, out = [];
  // três lâminas que descem sobre o ombro, cada uma mais baixa, mais para fora e menor
  [[0, .16, .30, .15], [1, .23, .34, .135], [2, .29, .37, .115]].forEach(([i, x0, x1, hd]) => {
    const yy = y - i * .06;
    out.push({ geo: jitter(convex([[x0, yy + .05, -hd], [x0, yy + .05, hd], [x1, yy - .05, -hd * 1.04], [x1, yy - .05, hd * 1.04], [(x0 + x1) / 2, yy + .07, 0], [x1 + .015, yy - .09, 0], [x0 + .01, yy + .02, -hd * .9], [x0 + .01, yy + .02, hd * .9]]), .01, 400 + i), mat: M.armor, bone: 'Shoulder_L' });
    out.push({ geo: strut([x1, yy - .055, -hd], [x1, yy - .055, hd], .013, .013, 4), mat: M.rust, bone: 'Shoulder_L' });   // borda enferrujada
  });
  // aba alta junto ao pescoço (guarda-pescoço) — o traço assimétrico que marca a silhueta do lanceiro
  out.push({ geo: jitter(convex([[.15, y + .02, -.13], [.15, y + .02, .13], [.19, y + .02, -.13], [.19, y + .02, .13], [.14, y + .2, -.09], [.14, y + .2, .08], [.17, y + .23, -.02], [.18, y + .17, .1]]), .008, 405), mat: M.armor, bone: 'Shoulder_L' });
  out.push({ geo: strut([.145, y + .21, -.09], [.145, y + .21, .08], .012, .012, 4), mat: M.rust, bone: 'Shoulder_L' });
  // um único espinho curto, inclinado para fora e para trás (não aponta para frente: não sugere arma)
  out.push({ geo: cone(.035, .13, 4, { pos: [.27, y + .06, -.07], rot: [-35, 0, -40] }), mat: M.rust, bone: 'Shoulder_L' });
  // correia diagonal no peito que segura a placa
  out.push({ geo: strut([.2, 1.17, .07], [-.17, .9, .16], .018, .018, 4), mat: M.rust, bone: 'Chest' });
  return out;
}

// Base larga: tassetes de ferro mais largos que o saiote do melee e grevas nas duas pernas.
function wideTassets(M) {
  const out = [];
  for (const s of [-1, 1]) out.push({ geo: xform(jitter(convex([[-.07, 0, 0], [.08, 0, 0], [-.07, 0, .03], [.08, 0, .03], [-.05, -.2, .05], [.11, -.18, .05], [.03, -.23, .04]]), .01, 420 + s), { pos: [s * .2, .69, .02], rot: [0, s * 60, s * 10] }), mat: M.armor, bone: 'Hips' });
  return out;
}

export const definition = {
  id: 'skeleton_lancer_concept', name: 'SkeletonLancer', role: 'A13 — conceito 2D do lanceiro F13 (fonte de render, sem FBX)',
  source: 'PROMPT_CATALOG.md A13 + docs/LANCER-F13.md', documented: false, heightMeters: 2.0,
  materials: MATERIALS, basePose: HUNCH,
  // nenhum soquete especial: a lança vai no Socket_Hand_R e é reorientada pela página para a mão esquerda
  sockets: {},
};

export function body(M) {
  const out = skeletonBody(P, M, { bracers: ['L', 'R'], greaves: ['L', 'R'], heavyChest: false });
  out.push(...skull(P, M, { cy: 1.6, r: .27 }));
  out.push(...layeredPauldron(M));
  out.push(...wideTassets(M));
  return out;
}
export const equipment = M => [{ socket: 'Socket_Hand_R', name: 'Spear', parts: spear(M) }];

// Poses estáticas (somam-se ao HUNCH da família). Ângulos dos braços e da perna de trás resolvidos numericamente
// para as duas mãos tocarem a mesma haste (erro ≤ 8 cm) e os dois pés tocarem o chão; ver Source~/README.md. Convenções do anim.mjs: frente +Z, esquerda +X;
// braço/perna para frente = X−; joelho dobra = X+; inclinar à frente = X+; girar para a esquerda = Y+.
// GUARD = "postura de aviso" durante o telegraph de ≥1 s: base larga, pé esquerdo à frente, lança nivelada à frente.
export const GUARD = {
  $hips: [0, -.12, 0],
  Hips: [0, -12, 0], Spine: [6, -8, 0], Chest: [6, -14, 0], Neck: [0, 12, 0], Head: [-6, 18, 0],
  UpperLeg_L: [-40, 10, 16], LowerLeg_L: [42, 0, 0], Foot_L: [-2, 0, -6],
  UpperLeg_R: [23, -10, -18], LowerLeg_R: [18, 0, 0], Foot_R: [-1, 0, 8],
  Shoulder_L: [0, 18, 0], UpperArm_L: [-32, -16, -14], LowerArm_L: [-93, 0, 0], Hand_L: [0, 0, 0],
  UpperArm_R: [34, 37, 30], LowerArm_R: [-63, 0, 0], Hand_R: [10, 0, 0],
};
// WINDUP = compromisso da investida: mesmo corpo, mais baixo, tronco carregado à frente sobre a perna da frente,
// lança recolhida junto ao corpo e ainda apontada na linha reta do avanço; perna de trás estendida como mola.
export const WINDUP = {
  $hips: [0, -.24, -.05],
  Hips: [10, -20, 0], Spine: [14, -10, 0], Chest: [10, -20, 0], Neck: [-4, 16, 0], Head: [-22, 22, 0],
  UpperLeg_L: [-68, 12, 14], LowerLeg_L: [74, 0, 0], Foot_L: [-6, 0, -6],
  UpperLeg_R: [23, -12, -16], LowerLeg_R: [21, 0, 0], Foot_R: [0, 0, 8],
  Shoulder_L: [0, 14, 0], UpperArm_L: [39, -35, -22], LowerArm_L: [-76, 0, 0], Hand_L: [0, 0, 0],
  UpperArm_R: [10, 2, 15], LowerArm_R: [8, 0, 0], Hand_R: [16, 0, 0],
};
const still = (name, pose) => ({ name, duration: .2, loop: false, keys: [[0, pose], [.2, pose]] });
export const clips = () => [still('Guard', GUARD), still('Windup', WINDUP), still('Bind', {})];

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const ch = assemble({ definition, P, body, equipment, clips });
  mkdirSync(OUT, { recursive: true });
  const glb = await toGLB(ch);
  writeFileSync(join(OUT, 'SkeletonLancer_render.glb'), glb);
  const src = {
    id: 'A13', resource: 'REF-CONCEPT-10', kind: 'fonte de render do conceito 2D (não é malha de produção)',
    generator: 'Tools/Lemo3DGenerator/src/production/ConceptLancer.mjs', three: THREE_VERSION,
    family: 'Tools/Lemo3DGenerator/src/characters/_skeleton_kit.mjs (mesmo skull/skeletonBody/paleta do SkeletonMelee/Elite)',
    palette: Object.fromEntries(Object.entries(MATERIALS).map(([k, v]) => [k, v.color])),
    triangles: ch.stats.triangles, meshes: ch.stats.meshes,
    spear: { ...SPEAR, totalLength: +(SPEAR.shaftEnd + .02 + SPEAR.headLen - SPEAR.butt).toFixed(2), note: 'visual; não implica hitbox (F13 usa hitRadius 0,75 m ao longo do segmento)' },
    poses: { Guard: GUARD, Windup: WINDUP, base: 'HUNCH (família)' },
    clipsInGlb: ch.clips.map(c => ({ name: c.name, groundLift: c.userData.groundLift, note: 'pose estática de render, não clip de produção' })),
  };
  writeFileSync(join(OUT, 'SkeletonLancer.source.json'), JSON.stringify(src, null, 2));
  console.log(`OK SkeletonLancer (render): ${ch.stats.triangles} tris → ${relative(PROJECT, OUT)}`);
}
