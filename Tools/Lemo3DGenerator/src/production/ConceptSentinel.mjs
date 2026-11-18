#!/usr/bin/env node
// A14 — Sentinela de Pedra: FONTE DE RENDER do conceito 2D (PROMPT_CATALOG.md, A14; condicional F15, já prototipada).
// NÃO é modelo de produção: não exporta FBX, não entra no catálogo de personagens nem no Unity. O GLB gerado aqui
// existe só para preview/concept_sentinel.html desenhar a folha 2048×1536 com a linguagem low-poly do projeto
// (mesmos helpers de geometria, mesma luz, mesma paleta de ambiente). Malha/rig Generic/takes serão tarefas novas.
//   node src/production/ConceptSentinel.mjs [--out <pasta>]
//
// Personagem original: guardião de blocos rachados, torso baixo e largo, braços em pilares, núcleo ciano estreito.
// Sem elmo, chifres ou marreta (traços do Warden). Escala tirada da primitiva F15 (StoneSentinelSceneBuilder):
// colisor 2,5 × 2,7 × 1,4 m, torso 1,8 m de largura, braços em x ±1,25 m, núcleo vertical 0,18 × 1,3 m na frente.
import { mkdirSync, writeFileSync } from 'node:fs';
import { join, resolve, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import { assemble, toGLB } from '../build.mjs';
import { THREE_VERSION } from '../lemo-three.mjs';
import { sides, strut, bevelBox, convex, xform, jitter } from '../parts.mjs';

const HERE = dirname(fileURLToPath(import.meta.url)), LAB = resolve(HERE, '..', '..'), PROJECT = resolve(LAB, '..', '..');
const argv = process.argv.slice(2), arg = (k, d) => { const i = argv.indexOf('--' + k); return i >= 0 ? argv[i + 1] : d; };
const OUT = resolve(arg('out', join(PROJECT, 'Assets/_Project/Art/Concepts/Source~/StoneSentinel')));

// Proporções (metros) no rig humanoide do gerador — os nomes de osso são só a hierarquia de pose do render.
// Ombros largos e baixos, pernas curtas e grossas, braços longos até perto do chão.
export const P = {
  height: 2.7, hipY: .78, spineY: 1.02, chestY: 1.32, neckY: 1.98, headY: 2.06,
  clavicle: .6, shoulderY: 2.0, shoulderX: 1.22, elbowY: 1.4, wristY: .8, gripDrop: .25, gripFwd: 0,
  legX: .48, legTopY: .74, kneeY: .42, ankleY: .14, toeZ: .22,
};

// Paleta da ficha A14 (= cores oficiais do ambiente em ART_DIRECTION_AND_PROMPTS.md): pedra fosca, musgo contido,
// runas/núcleo ciano como únicos emissivos frios; rachadura laranja opcional, fina e de baixa intensidade.
export const MATERIALS = {
  stone: { color: '#55596B', smoothness: .08, note: 'pedra (piso oficial)' },
  stoneDark: { color: '#3E4152', smoothness: .06, note: 'juntas, reentrâncias e base (parede oficial)' },
  moss: { color: '#4C5A44', smoothness: .03, note: 'musgo/desgaste oficial' },
  rune: { color: '#4FD8FF', emissive: '#4FD8FF', emissiveIntensity: 1.6, smoothness: .4, note: 'runas esparsas' },
  core: { color: '#4FD8FF', emissive: '#4FD8FF', emissiveIntensity: 2.6, smoothness: .5, note: 'núcleo estreito embutido' },
  crack: { color: '#1E1830', smoothness: .02, note: 'fendas (sombra oficial)' },
  ember: { color: '#FF9A2E', emissive: '#FF9A2E', emissiveIntensity: 1.2, smoothness: .3, note: 'rachadura ameaçadora (opcional)' },
};

// bloco de pedra: caixa chanfrada, irregular (jitter determinístico), com afunilamento opcional
const blk = (w, h, d, pos, o = {}) => xform(jitter(bevelBox(w, h, d, o.c ?? .14, { top: o.top ?? 1, bottom: o.bottom ?? 1, topZ: o.topZ ?? 1, bottomZ: o.bottomZ ?? 1 }), o.j ?? Math.min(w, h, d) * .07, o.seed ?? 1), { pos, rot: o.rot ?? [0, 0, 0] });
// fio emissivo entre pontos (rachadura/runa)
const wire = (pts, r, mat, bone) => pts.slice(1).map((b, i) => ({ geo: strut(pts[i], b, r, r, 4), mat, bone }));

function torso(M) {
  const out = [];
  // pélvis: um bloco largo e baixo, assentado sobre as pernas curtas
  out.push({ geo: blk(1.25, .42, .9, [0, .8, -.02], { top: 1, bottom: .84, seed: 11 }), mat: M.stone, bone: 'Hips' });
  out.push({ geo: blk(1.0, .1, .7, [0, .58, -.02], { seed: 12 }), mat: M.stoneDark, bone: 'Hips' });
  // ventre: dois blocos separados por uma fenda (o corpo é empilhado, não esculpido)
  for (const [s, sx] of sides) out.push({ geo: blk(.66, .48, .98, [sx * .35, 1.2, 0], { top: 1.04, rot: [0, 0, sx * -2], seed: 20 + sx }), mat: M.stone, bone: 'Spine' });
  out.push({ geo: blk(.9, .12, .8, [0, .98, 0], { seed: 23 }), mat: M.stoneDark, bone: 'Spine' });
  out.push({ geo: blk(.16, .42, .86, [0, 1.2, -.02], { seed: 24 }), mat: M.stoneDark, bone: 'Spine' });   // fundo da fenda (não vaza luz do núcleo)
  // peito: massa larga em três blocos, mais larga em cima (trapézio), com a "canga" de pedra por cima
  out.push({ geo: blk(.9, .74, 1.12, [0, 1.72, .03], { top: 1.1, seed: 31 }), mat: M.stone, bone: 'Chest' });
  for (const [s, sx] of sides) out.push({ geo: blk(.5, .7, 1.02, [sx * .64, 1.76, -.01], { top: 1.12, rot: [0, 0, sx * -4], seed: 32 + sx }), mat: M.stone, bone: 'Chest' });
  out.push({ geo: blk(1.86, .22, .92, [0, 2.17, -.08], { top: .92, seed: 35 }), mat: M.stoneDark, bone: 'Chest' });
  out.push({ geo: blk(1.0, .06, .5, [0, 2.3, -.18], { seed: 36, j: .02 }), mat: M.moss, bone: 'Chest' });
  // reentrância e núcleo ciano estreito (como o "Core" vertical da primitiva F15)
  // moldura escura em quatro peças (trilhos + tampas) à frente do núcleo: o ciano fica 4 cm para dentro
  for (const sx of [-1, 1]) out.push({ geo: blk(.09, .98, .14, [sx * .105, 1.62, .58], { c: .25, seed: 37 + sx, j: .008 }), mat: M.stoneDark, bone: 'Chest' });
  for (const sy of [-1, 1]) out.push({ geo: blk(.3, .1, .14, [0, 1.62 + sy * .47, .58], { c: .25, seed: 39 + sy, j: .008 }), mat: M.stoneDark, bone: 'Chest' });
  out.push({ geo: blk(.11, .8, .06, [0, 1.62, .58], { c: .3, seed: 38, j: .004 }), mat: M.core, bone: 'Chest' });
  // rachadura laranja opcional: um único fio no bloco esquerdo, partindo da reentrância
  out.push(...wire([[.16, 1.92, .59], [.3, 1.82, .57], [.27, 1.66, .57], [.44, 1.52, .53], [.5, 1.44, .52]], .016, M.ember, 'Chest'));
  // fendas escuras finas: os blocos são rachados, não lisos
  out.push(...wire([[-.2, 2.02, .6], [-.3, 1.86, .6], [-.26, 1.7, .6], [-.38, 1.5, .58]], .009, M.crack, 'Chest'));
  out.push(...wire([[.5, 1.14, .5], [.38, 1.24, .51], [.24, 1.18, .51]], .009, M.crack, 'Spine'));
  out.push(...wire([[-.3, .92, .43], [-.18, .82, .44], [-.06, .86, .44]], .009, M.crack, 'Hips'));
  // costas: crista dorsal de três pedras e uma runa curta (lê de trás sem copiar a frente)
  [[1.42, .5, .3], [1.76, .56, .34], [2.08, .44, .28]].forEach(([y, w, h], i) => out.push({ geo: blk(w, h, .26, [0, y, -.62], { top: .7, rot: [-8, 0, (i - 1) * 6], seed: 40 + i }), mat: i === 1 ? M.stone : M.stoneDark, bone: 'Chest' }));
  out.push(...wire([[-.12, 1.82, -.76], [0, 1.7, -.77], [.12, 1.82, -.76]], .012, M.rune, 'Chest'));
  out.push({ geo: blk(.5, .05, .3, [.2, 1.04, -.48], { seed: 44, j: .015 }), mat: M.moss, bone: 'Hips' });
  return out;
}

function head(M) {
  // cabeça pequena, afundada à frente da canga, abaixo da linha dos ombros: sem elmo, sem chifres
  return [
    { geo: blk(.5, .38, .48, [0, 2.24, .5], { top: .86, seed: 51 }), mat: M.stone, bone: 'Head' },
    { geo: blk(.56, .1, .5, [0, 2.43, .5], { seed: 52 }), mat: M.stoneDark, bone: 'Head' },     // aba da testa
    { geo: blk(.26, .035, .05, [0, 2.3, .74], { c: .3, seed: 53, j: .004 }), mat: M.rune, bone: 'Head' },   // uma fenda ciano
  ];
}

function limbs(M) {
  const out = [];
  for (const [s, sx] of sides) {
    const x = sx * P.shoulderX;
    // ombro: pedra maciça com musgo por cima; só o esquerdo leva uma segunda pedra (assimetria discreta)
    out.push({ geo: blk(.72, .6, .82, [x, 2.12, 0], { top: .84, rot: [0, sx * 6, sx * -5], seed: 60 + sx }), mat: M.stone, bone: `UpperArm_${s}` });
    out.push({ geo: blk(.58, .06, .6, [x, 2.43, 0], { seed: 62 + sx, j: .025 }), mat: M.moss, bone: `UpperArm_${s}` });
    if (sx > 0) out.push({ geo: blk(.34, .24, .4, [x - .05, 2.55, -.08], { top: .7, rot: [0, 20, 8], seed: 64 }), mat: M.stoneDark, bone: `UpperArm_${s}` });
    out.push(...wire([[x - sx * .2, 2.38, .43], [x - sx * .05, 2.24, .44], [x + sx * .02, 2.06, .44]], .009, M.crack, `UpperArm_${s}`));
    // braço em pilar: tambor superior, junta escura, tambor inferior mais largo, base de pilar como mão
    out.push({ geo: blk(.5, .52, .56, [x, 1.64, 0], { seed: 66 + sx }), mat: M.stone, bone: `UpperArm_${s}` });
    out.push({ geo: blk(.4, .14, .44, [x, 1.36, 0], { c: .25, seed: 68 + sx }), mat: M.stoneDark, bone: `LowerArm_${s}` });
    out.push({ geo: blk(.6, .58, .64, [x, 1.02, 0], { top: .9, seed: 70 + sx }), mat: M.stone, bone: `LowerArm_${s}` });
    // runa curta só na face externa do antebraço (esparsa; nada de faixa contínua)
    out.push(...wire([[x + sx * .31, 1.16, -.1], [x + sx * .315, 1.02, 0], [x + sx * .31, 1.16, .1]], .011, M.rune, `LowerArm_${s}`));
    out.push({ geo: blk(.66, .08, .7, [x, .72, .02], { c: .25, seed: 72 + sx }), mat: M.stoneDark, bone: `Hand_${s}` });
    out.push(...wire([[x + sx * .15, 1.24, .33], [x + sx * .02, 1.08, .33], [x + sx * .1, .9, .34]], .009, M.crack, `LowerArm_${s}`));
    out.push({ geo: blk(.74, .46, .78, [x, .46, .03], { top: .92, seed: 74 + sx }), mat: M.stone, bone: `Hand_${s}` });
    // pernas curtas e grossas, pé em laje escura
    const lx = sx * P.legX;
    out.push({ geo: blk(.48, .3, .52, [lx, .6, 0], { seed: 80 + sx }), mat: M.stoneDark, bone: `UpperLeg_${s}` });
    out.push({ geo: blk(.56, .34, .62, [lx, .3, .02], { bottom: 1.08, seed: 82 + sx }), mat: M.stone, bone: `LowerLeg_${s}` });
    out.push({ geo: blk(.7, .15, .86, [lx, .075, .1], { top: .9, seed: 84 + sx }), mat: M.stoneDark, bone: `Foot_${s}` });
    out.push({ geo: blk(.3, .04, .3, [lx - sx * .1, .17, .2], { seed: 86 + sx, j: .012 }), mat: M.moss, bone: `Foot_${s}` });
  }
  return out;
}

export const definition = {
  id: 'stone_sentinel_concept', name: 'StoneSentinel', role: 'A14 — conceito 2D da Sentinela de Pedra F15 (fonte de render, sem FBX)',
  source: 'PROMPT_CATALOG.md A14 + docs/STONE-SENTINEL-F15.md', documented: false, heightMeters: 2.7,
  materials: MATERIALS, basePose: {}, sockets: {},
};
export function body(M) { return [...torso(M), ...head(M), ...limbs(M)]; }

// Poses estáticas. Convenções do anim.mjs: frente +Z, esquerda +X; braço à frente = X−; joelho dobra = X+;
// inclinar à frente = X+; girar para a esquerda = Y+; abrir braço esquerdo = +Z, direito = −Z.
// STAND = postura de luta neutra (vistas ortográficas): base aberta, braços um pouco afastados do tronco.
export const STAND = {
  $hips: [0, -.03, 0],
  Spine: [4, 0, 0], Chest: [4, 0, 0], Head: [-6, 0, 0],
  UpperArm_L: [-4, 0, 3], UpperArm_R: [-4, 0, -3], LowerArm_L: [-6, 0, -2], LowerArm_R: [-6, 0, 2],
  UpperLeg_L: [-6, 0, 4], UpperLeg_R: [-6, 0, -4], LowerLeg_L: [10, 0, 0], LowerLeg_R: [10, 0, 0], Foot_L: [-4, 0, -4], Foot_R: [-4, 0, 4],
};
// WINDUP = antecipação do leque frontal: pés plantados olhando o alvo (o setor é travado na direção da raiz),
// os dois pilares abertos para os lados e um pouco para trás, tronco recuado expondo o núcleo, cabeça baixa.
// O golpe que se segue fecha os dois braços pela frente, cobrindo o setor; a pose é simétrica como o setor.
// Braços longe do tronco: a silhueta separa os membros e o núcleo aceso fica à vista da câmera de jogo.
export const WINDUP = {
  $hips: [0, -.12, -.03],
  Spine: [-4, 0, 0], Chest: [-7, 0, 0], Neck: [6, 0, 0], Head: [12, 0, 0],
  UpperArm_L: [14, -8, 60], LowerArm_L: [-34, 0, 4], Hand_L: [-10, 0, 0],
  UpperArm_R: [16, 8, -56], LowerArm_R: [-38, 0, -4], Hand_R: [-10, 0, 0],
  UpperLeg_L: [-16, 0, 12], LowerLeg_L: [26, 0, 0], Foot_L: [-8, 0, -12],
  UpperLeg_R: [-12, 0, -13], LowerLeg_R: [24, 0, 0], Foot_R: [-10, 0, 13],
};
const still = (name, pose) => ({ name, duration: .2, loop: false, keys: [[0, pose], [.2, pose]] });
export const clips = () => [still('Stand', STAND), still('Windup', WINDUP), still('Bind', {})];

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const ch = assemble({ definition, P, body, clips });
  mkdirSync(OUT, { recursive: true });
  writeFileSync(join(OUT, 'StoneSentinel_render.glb'), await toGLB(ch));
  const src = {
    id: 'A14', resource: 'REF-CONCEPT-11', kind: 'fonte de render do conceito 2D (não é malha de produção)',
    generator: 'Tools/Lemo3DGenerator/src/production/ConceptSentinel.mjs', three: THREE_VERSION,
    palette: Object.fromEntries(Object.entries(MATERIALS).map(([k, v]) => [k, v.color])),
    emissive: Object.fromEntries(Object.entries(MATERIALS).filter(([, v]) => v.emissive).map(([k, v]) => [k, v.emissiveIntensity])),
    proportions: P, triangles: ch.stats.triangles, meshes: ch.stats.meshes,
    f15Reference: { collider: [2.5, 2.7, 1.4], torso: [1.8, 2.2, 1.3], armX: 1.25, core: [.18, 1.3], fan: { angle: 100, radius: 6 }, note: 'primitiva/dados F15 usados só para escala e leitura; o conceito não define colisor, dano nem área' },
    poses: { Stand: STAND, Windup: WINDUP },
    clipsInGlb: ch.clips.map(c => ({ name: c.name, groundLift: c.userData.groundLift, note: 'pose estática de render, não take de produção' })),
  };
  writeFileSync(join(OUT, 'StoneSentinel.source.json'), JSON.stringify(src, null, 2));
  console.log(`OK StoneSentinel (render): ${ch.stats.triangles} tris → ${relative(PROJECT, OUT)}`);
}
