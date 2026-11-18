#!/usr/bin/env node
// A12 — MODEL-DESTRUCTIBLE: vaso de barro baixo e caixa de madeira reforçada, intactos e quebrados (visual do vaso F09;
// ID em Assets/_Project/Art/ProductionSpecs/ASSET_MANIFEST.json, ficha A12 em PROMPT_CATALOG.md).
//
// Fonte de autoria editável dos quatro modelos estáticos. Mesmo vocabulário low-poly do Lemo3DGenerator (hulls convexos,
// normais de face) e a mesma convenção do ModelChest.mjs (A11): metros, +Y para cima, +Z frente, pivô na base central.
// Só visual: nenhum arquivo de domínio é lido em runtime ou escrito (DestructibleProp, DestructiblePropPresenter,
// DestructiblePropSceneBuilder, cenas, prefabs e Mat_Vase_F09 ficam intactos). Sem collider, física, loot, burst ou animação.
//
// Formas (cada peça é um sólido convexo fechado):
//   Vaso   — anéis decagonais empilhados (pé, barriga baixa/alta, ombro, gargalo, boca com interior escuro), duas rachaduras
//            finas em lascas escuras rentes à parede, uma mancha de musgo no pé.
//   Quebrado — pé + barriga cortada em altura irregular (topo escuro = aberto), aro/gargalo caído para dentro e quatro cacos
//            planos nos cantos livres do quadrado do footprint (fora do círculo do vaso).
//   Caixa  — núcleo, quatro montantes escuros, três tábuas por lado, tampo de três tábuas, travessas e uma diagonal por lado,
//            uma tábua rachada na frente, musgo no pé.
//   Quebrada — fundo escuro, montantes partidos com topo de madeira clara, primeira fileira de tábuas, restos da segunda
//            atrás/esquerda, duas tábuas caídas para dentro.
// Footprint: todas as variantes cabem no vaso F09 de primitivas (Body esfera 0,9 × 1 × 0,9 em y 0,5 + Rim; ±0,45 m em X/Z,
// 1,04 m de altura) e a variante quebrada nunca sai dos bounds X/Z da intacta (mesmo pivô).
//
// Saídas:
//   Assets/_Project/Art/ProductionStaging/MODEL-DESTRUCTIBLE/
//     MODEL-DESTRUCTIBLE_Jar_Intact.fbx, _Jar_Broken.fbx, _Crate_Intact.fbx, _Crate_Broken.fbx (+ .meta)   FBX 7.4 ASCII
//     MODEL-DESTRUCTIBLE_Palette.png (+ .meta)  paleta 16×16 RGB (células 4×4 px), BaseMap do material único
//   …/Source~/
//     MODEL-DESTRUCTIBLE.glb                   revisão/intercâmbio DCC (quatro variantes lado a lado; não importado pela Unity)
//     MODEL-DESTRUCTIBLE.source.json           parâmetros, paleta, material, nós, integração proposta
//     ModelDestructible.generator.json         validação interna (geometria em memória)
//
// Uso (dentro de Tools/Lemo3DGenerator):
//   node src/production/ModelDestructible.mjs            gera
//   node src/production/ModelDestructible.mjs --check    só valida
// Depois: python -I Tools/Lemo3DGenerator/src/production/ModelDestructible_preview.py  (relê os FBX do disco, valida, previews)
// Lemo-Opuscar é usado SOMENTE LEITURA (three.js r170, ConvexGeometry, GLTFExporter).
import { mkdirSync, writeFileSync, readFileSync } from 'node:fs';
import { join, resolve, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
import { THREE, GLTFExporter, ConvexGeometry, THREE_VERSION, LEMO_ROOT } from '../lemo-three.mjs';
import { finish, tris, signedVolume } from '../geo.mjs';
import { encodePNG } from './ItemIcons.mjs';

const HERE = dirname(fileURLToPath(import.meta.url)), LAB = resolve(HERE, '..', '..'), PROJECT = resolve(LAB, '..', '..');
const has = k => process.argv.includes('--' + k);
export const ID = 'MODEL-DESTRUCTIBLE';
export const NAME = 'MODEL-DESTRUCTIBLE';
export const STAGING = 'Assets/_Project/Art/ProductionStaging/MODEL-DESTRUCTIBLE';
const OUT = join(PROJECT, STAGING), SRC = join(OUT, 'Source~');
const FBX_TEMPLATE = join(PROJECT, 'Assets/_Project/Art/Items/Models/Loot_XpOrb.fbx.meta');        // ModelImporter real (Unity 6000.6), estático
const TEX_TEMPLATE = join(PROJECT, 'Tools/FxHealGenerator/texture.meta.template');                 // cópia de FX_Spark.png.meta

// ———————————————————————— parâmetros editáveis ————————————————————————
// Footprint do vaso F09 atual (DestructiblePropSceneBuilder.ConfigureRelease): esfera Body escala (0,9; 1; 0,9) em y 0,5 e
// cilindro Rim (0,52; 0,08; 0,52) em y 0,96 → X/Z ±0,45 m, topo 1,04 m. Nenhuma variante A12 pode passar disso.
export const FOOTPRINT_F09 = { halfX: 0.45, halfZ: 0.45, height: 1.04, source: 'Assets/_Project/Scripts/Editor/DestructiblePropSceneBuilder.cs (Body Sphere 0.9×1×0.9 @ y 0.5; Rim Cylinder 0.52×0.08×0.52 @ y 0.96)' };
export const BUDGET = { trianglesPerProp: 600, materials: 1 };

export const SHAPE = {
  jar: {
    sides: 10,
    // anéis [raio do vértice, y] de baixo para cima: pé, base da barriga, barriga, ombro, gargalo, topo do gargalo
    rings: [[0.25, 0], [0.30, 0.05], [0.39, 0.27], [0.35, 0.47], [0.17, 0.58], [0.16, 0.62]],
    lip: { r: 0.19, y0: 0.62, y1: 0.655, rTop: 0.175, yTop: 0.675 },
    mouth: { r: 0.12, y: [0.668, 0.68] },   // disco escuro da boca, rente ao topo do aro (aro claro em volta)
    // rachaduras: [peça, face k, pontos (s ao longo do anel, t de baixo para cima)]
    cracks: [['belly_low', 0, [[0.30, 0.97], [0.46, 0.62], [0.36, 0.30]]], ['belly_high', 9, [[0.62, 0.04], [0.50, 0.42], [0.58, 0.66]]]],   // face 0 = frente, 9 = frente-esquerda
    crackWidth: 0.0075,
    moss: { faces: [8, 9], s: [0.08, 0.92], t: [0.1, 0.85] },   // esquerda e frente-esquerda do pé
    broken: {
      floorY: 0.14,                                                            // fundo interno (topo escuro da parte baixa maciça)
      wall: 0.035,                                                             // espessura da parede de barro partida
      jagged: [0.22, 0.19, 0.31, 0.22, 0.28, 0.18, 0.26, 0.34, 0.27, 0.21],   // altura do corte por vértice do anel
      missing: [0],                                                            // faces sem parede (vão na frente: mostra o interior)
      rim: { tiltX: 40, tiltZ: 12, pos: [-0.03, 0.28, -0.02] },               // aro/gargalo caído dentro do corpo, boca para a frente
      shards: [                                                                // cacos planos nos cantos livres do quadrado
        { c: [0.305, 0.27], r: 0.075, yaw: 20, h: 0.03 },
        { c: [-0.30, 0.27], r: 0.068, yaw: -35, h: 0.028 },
        { c: [-0.30, -0.27], r: 0.072, yaw: 60, h: 0.03 },
        { c: [0.31, -0.265], r: 0.06, yaw: 5, h: 0.026 },
      ],
    },
  },
  crate: {
    w: 0.70, h: 0.62, post: 0.075, coreInset: 0.035, boardInset: 0.008, rows: 3, gap: 0.012, y0: 0.03,
    top: { planks: 3, gap: 0.01, thick: 0.026, battenH: 0.05 },
    brace: { width: 0.06, thick: 0.018, margin: 0.05 },
    crack: { side: '+Z', row: 1, points: [[-0.12, 1.0], [-0.06, 0.58], [-0.1, 0.3], [-0.02, 0.0]], width: 0.008 },
    moss: { side: '+Z', u: [-0.275, -0.06], v: [0.0, 0.12] },
    broken: {
      postHeights: { '-x-z': [0.30, 0.36], '+x-z': [0.34, 0.27], '-x+z': [0.17, 0.22], '+x+z': [0.24, 0.16] },   // [altura em -lado, +lado] = topo partido inclinado
      backRow1: [[-1, 0], [0.10, 0], [-0.05, -0.05], [-1, -0.05]],   // tábua de trás da 2ª fileira partida: (u, Δy) com u −1 = montante
      leftRow1: [[-1, 0], [-0.10, 0], [-0.18, -0.09], [-1, -0.09]],  // toco da 2ª fileira à esquerda
      braceTop: 0.30,
      planks: [
        { size: [0.5, 0.026, 0.15], rot: [0, 28, 18], pos: [0.02, 0.12, -0.01] },
        { size: [0.44, 0.026, 0.13], rot: [-20, -62, 0], pos: [-0.05, 0.11, 0.06] },
      ],
    },
  },
};

// Paleta 16×16 com células 4×4 px (UV no centro da célula). Cores da ficha + derivadas registradas.
export const PALETTE = {
  clay: { cell: [0, 0], hex: '#7B4E32', note: 'barro: ferrugem oficial #8A4A22 dessaturado (30% para o próprio cinza de luminância)' },
  clayLight: { cell: [1, 0], hex: '#915C3B', note: 'barro iluminado (ombro, chanfro da boca, topo dos cacos): derivado ×1,18 de #7B4E32 — gradiente pintado' },
  clayDark: { cell: [2, 0], hex: '#593824', note: 'barro escuro (pé, gargalo, aro, laterais dos cacos): derivado ×0,72 de #7B4E32' },
  interior: { cell: [3, 0], hex: '#2A1D16', note: 'interior/boca/corte do barro e fundo da caixa quebrada: derivado escurecido (lê "aberto" pela câmera)' },
  crack: { cell: [0, 1], hex: '#3A2519', note: 'rachadura: entre interior e barro escuro, discreta' },
  wood: { cell: [1, 1], hex: '#7A4B2A', note: 'madeira oficial (tábuas e tampo)' },
  woodDark: { cell: [2, 1], hex: '#53331D', note: 'madeira escura (montantes, travessas, diagonais, frestas): derivado ×0,68 de #7A4B2A' },
  woodFresh: { cell: [3, 1], hex: '#9B7048', note: 'madeira partida (pontas quebradas): derivado clareado de #7A4B2A, sem saturar' },
  moss: { cell: [0, 2], hex: '#4C5A44', note: 'musgo oficial do cenário (desgaste no pé)' },
};
export const MATERIAL = { name: `${NAME}_Prop`, shader: 'Universal Render Pipeline/Lit', surface: 'Opaque', baseMap: `${NAME}_Palette.png`, baseColor: '#FFFFFF', metallic: 0, smoothness: 0.15, note: 'fosco, único e compartilhado pelos quatro FBX (mesmo nome em todos → remapeamento por nome para um único .mat na integração)' };

// ———————————————————————— geometria ————————————————————————
const V = (x, y, z) => new THREE.Vector3(x, y, z);
const hull = pts => finish(new ConvexGeometry(pts.map(p => V(...p))));
const box = (x0, x1, y0, y1, z0, z1) => { const g = new THREE.BoxGeometry(x1 - x0, y1 - y0, z1 - z0); g.translate((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2); return finish(g); };
const sub = (a, b) => a.map((v, i) => v - b[i]), add3 = (a, b) => a.map((v, i) => v + b[i]), mul = (a, k) => a.map(v => v * k);
const cross = (a, b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];
const norm = a => { const l = Math.hypot(...a); return a.map(v => v / l); };
const lerp = (a, b, t) => a.map((v, i) => v + (b[i] - v) * t);
const rot = (g, [rx, ry, rz], pos) => { g.applyMatrix4(new THREE.Matrix4().compose(V(...pos), new THREE.Quaternion().setFromEuler(new THREE.Euler(rx * Math.PI / 180, ry * Math.PI / 180, rz * Math.PI / 180, 'XYZ')), V(1, 1, 1))); return finish(g); };

// —— vaso: anéis decagonais com uma face plana voltada para +Z (face k vai do vértice k ao k+1) ——
const J = SHAPE.jar, NS = J.sides;
const ang = k => (k - 0.5) * 2 * Math.PI / NS;   // vértice k em (k − 0,5)·36° a partir de +Z: a face k = 0 (vértices 0…1) fica de frente
const ringPt = (r, y, k) => [r * Math.sin(ang(k)), y, r * Math.cos(ang(k))];
const ring = (r, y) => Array.from({ length: NS }, (_, k) => ringPt(r, y, k));
const JR = J.rings;
const JAR_BANDS = { foot: [0, 1], belly_low: [1, 2], belly_high: [2, 3], shoulder: [3, 4], neck: [4, 5] };
function facePoint(band, k, s, t) {
  const [a, b] = JAR_BANDS[band], A0 = ringPt(...JR[a], k), A1 = ringPt(...JR[a], k + 1), B0 = ringPt(...JR[b], k), B1 = ringPt(...JR[b], k + 1);
  return lerp(lerp(A0, A1, s), lerp(B0, B1, s), t);
}
function faceNormal(band, k) {
  const [a, b] = JAR_BANDS[band], A0 = ringPt(...JR[a], k), A1 = ringPt(...JR[a], k + 1), B0 = ringPt(...JR[b], k);
  let n = norm(cross(sub(A1, A0), sub(B0, A0))); const c = facePoint(band, k, 0.5, 0.5);
  if (n[0] * c[0] + n[2] * c[2] < 0) n = mul(n, -1);
  return n;
}
// lasca fina rente a um plano: 8 pontos (±largura, +fora/−dentro) → sólido convexo fechado
function sliver(p, q, n, w, out = 0.003, inn = 0.006) {
  const side = norm(cross(n, sub(q, p))), pts = [];
  for (const e of [p, q]) for (const sw of [-w, w]) for (const o of [out, -inn]) pts.push(add3(add3(e, mul(side, sw)), mul(n, o)));
  return hull(pts);
}
// placa rente a uma face (quadrilátero s×t) — musgo
function facePatch(band, k, [s0, s1], [t0, t1], out = 0.004, inn = 0.006) {
  const n = faceNormal(band, k), pts = [];
  for (const s of [s0, s1]) for (const t of [t0, t1]) { const p = facePoint(band, k, s, t); pts.push(add3(p, mul(n, out)), add3(p, mul(n, -inn))); }
  return hull(pts);
}

const jarShell = n => n.y > 0.97 ? 'interior' : n.y < -0.97 ? 'clayDark' : 'clay';
const jarPieces = {
  foot: () => hull([...ring(...JR[0]), ...ring(...JR[1])]),
  belly_low: () => hull([...ring(...JR[1]), ...ring(...JR[2])]),
  belly_high: () => hull([...ring(...JR[2]), ...ring(...JR[3])]),
  shoulder: () => hull([...ring(...JR[3]), ...ring(...JR[4])]),
  neck: () => hull([...ring(...JR[4]), ...ring(...JR[5])]),
  lip: () => hull([...ring(J.lip.r, J.lip.y0), ...ring(J.lip.r, J.lip.y1), ...ring(J.lip.rTop, J.lip.yTop)]),
};
const lipSwatch = n => n.y > 0.4 ? 'clayLight' : n.y < -0.5 ? 'clayDark' : 'clay';   // topo do aro claro; a boca escura é o disco `mouth`
const mouthDisc = () => hull([...ring(J.mouth.r, J.mouth.y[0]), ...ring(J.mouth.r, J.mouth.y[1])]);
// raio do vértice do anel numa altura y (interpolado entre os anéis do perfil)
function rAt(y) { for (let i = 0; i + 1 < JR.length; i++) if (y <= JR[i + 1][1]) return JR[i][0] + (JR[i + 1][0] - JR[i][0]) * (y - JR[i][1]) / (JR[i + 1][1] - JR[i][1]); return JR[JR.length - 1][0]; }
function jarDetails(add, node) {
  for (const [band, k, pts] of J.cracks) {
    const n = faceNormal(band, k);
    for (let i = 0; i + 1 < pts.length; i++) add(node, `crack_${band}_${i}`, sliver(facePoint(band, k, ...pts[i]), facePoint(band, k, ...pts[i + 1]), n, J.crackWidth), 'crack');
  }
  for (const k of J.moss.faces) add(node, `moss_${k}`, facePatch('foot', k, J.moss.s, J.moss.t), 'moss');
}

// —— caixa ——
const C = SHAPE.crate, HW = C.w / 2, IN = HW - C.post, CORE = HW - C.coreInset, BOUT = HW - C.boardInset;
const bh = (C.h - 2 * C.y0 - (C.rows - 1) * C.gap) / C.rows;
const rowY = i => [C.y0 + i * (bh + C.gap), C.y0 + i * (bh + C.gap) + bh];
// lados: eixo normal, sinal, eixo ao longo (u). Caixa em coordenadas (u, y, d) → (x, y, z)
const SIDES = { '+Z': { n: 'z', s: 1 }, '-Z': { n: 'z', s: -1 }, '+X': { n: 'x', s: 1 }, '-X': { n: 'x', s: -1 } };
function sideBox(side, u0, u1, y0, y1, d0, d1) {   // d = distância do centro ao longo da normal do lado
  const S = SIDES[side], [a, b] = S.s > 0 ? [d0, d1] : [-d1, -d0];
  return S.n === 'z' ? box(u0, u1, y0, y1, a, b) : box(a, b, y0, y1, u0, u1);
}
function sidePt(side, u, y, d) { const S = SIDES[side]; return S.n === 'z' ? [u, y, S.s * d] : [S.s * d, y, u]; }
function sideHull(side, quad2d, d0, d1) {   // polígono convexo (u, y) extrudado entre d0 e d1
  const pts = []; for (const [u, y] of quad2d) for (const d of [d0, d1]) pts.push(sidePt(side, u, y, d)); return hull(pts);
}
function bracePoly(u0, y0, u1, y1, w) {   // faixa diagonal de largura w entre (u0, y0) e (u1, y1)
  const dir = norm([u1 - u0, y1 - y0, 0]), pn = [-dir[1] * w / 2, dir[0] * w / 2];
  return [[u0 + pn[0], y0 + pn[1]], [u0 - pn[0], y0 - pn[1]], [u1 - pn[0], y1 - pn[1]], [u1 + pn[0], y1 + pn[1]]];
}
const CORNERS = { '-x-z': [-1, -1], '+x-z': [1, -1], '-x+z': [-1, 1], '+x+z': [1, 1] };
const postBox = (sx, sz, h) => box(sx > 0 ? IN : -HW, sx > 0 ? HW : -IN, 0, h, sz > 0 ? IN : -HW, sz > 0 ? HW : -IN);
// classificação: face voltada para dentro da caixa (n · centróide < 0 no plano XZ) → madeira escura
const boardFace = (n, c) => (n.x * c.x + n.z * c.z < -0.02 && Math.abs(n.y) < 0.5) ? 'woodDark' : 'wood';
const freshTop = base => (n, c) => n.y > 0.5 ? 'woodFresh' : base;

function crateIntact(add) {
  const node = 'Crate_Intact', T = C.top;
  add(node, 'core', box(-CORE, CORE, C.y0, C.h - T.thick, -CORE, CORE), 'woodDark');
  for (const [key, [sx, sz]] of Object.entries(CORNERS)) add(node, `post_${key}`, postBox(sx, sz, C.h), 'woodDark');
  for (const side of Object.keys(SIDES)) for (let i = 0; i < C.rows; i++) { const [y0, y1] = rowY(i); add(node, `board_${side}_${i}`, sideBox(side, -IN, IN, y0, y1, CORE, BOUT), boardFace); }
  // tampo: três tábuas ao longo de Z entre as travessas de ±Z; travessas escuras nas bordas ±Z
  const pw = (2 * CORE - (T.planks - 1) * T.gap) / T.planks;
  for (let i = 0; i < T.planks; i++) { const x0 = -CORE + i * (pw + T.gap); add(node, `top_plank_${i}`, box(x0, x0 + pw, C.h - T.thick, C.h - 0.004, -IN, IN), 'wood'); }
  for (const s of [-1, 1]) add(node, `top_batten_${s > 0 ? '+Z' : '-Z'}`, box(-IN, IN, C.h - T.battenH, C.h, s > 0 ? IN : -BOUT, s > 0 ? BOUT : -IN), 'woodDark');
  // diagonais (alternando o sentido) por fora das tábuas
  const B = C.brace;
  for (const [side, dir] of [['+Z', 1], ['-Z', -1], ['+X', -1], ['-X', 1]]) {
    const [ua, ub] = dir > 0 ? [-IN + B.width / 2, IN - B.width / 2] : [IN - B.width / 2, -IN + B.width / 2];
    add(node, `brace_${side}`, sideHull(side, bracePoly(ua, C.y0 + B.margin, ub, C.h - T.battenH - B.margin * 0.4, B.width), BOUT - 0.004, BOUT + B.thick), 'woodDark');
  }
  // rachadura numa tábua da frente (fileira do meio) e musgo no pé da frente-esquerda
  const ck = C.crack, [ry0, ry1] = rowY(ck.row), cp = ck.points.map(([u, t]) => sidePt(ck.side, u, ry0 + (ry1 - ry0) * t, BOUT)), nrm = sidePt(ck.side, 0, 0, 1);
  for (let i = 0; i + 1 < cp.length; i++) add(node, `crack_${i}`, sliver(cp[i], cp[i + 1], nrm, ck.width), 'crack');
  const m = C.moss; add(node, 'moss', sideHull(m.side, [[m.u[0], C.y0 + m.v[0]], [m.u[1], C.y0 + m.v[0]], [m.u[1] - 0.05, C.y0 + m.v[1]], [m.u[0], C.y0 + m.v[1]]], BOUT - 0.004, BOUT + 0.004), 'moss');
}

function crateBroken(add) {
  const base = 'Crate_Broken_Base', T = C.top, BR = C.broken;
  add(base, 'floor', box(-CORE, CORE, 0, C.y0, -CORE, CORE), n => n.y > 0.5 ? 'interior' : 'woodDark');
  for (const [key, [sx, sz]] of Object.entries(CORNERS)) {   // montante partido: topo inclinado ao longo de X
    const [hA, hB] = BR.postHeights[key], x0 = sx > 0 ? IN : -HW, x1 = sx > 0 ? HW : -IN, z0 = sz > 0 ? IN : -HW, z1 = sz > 0 ? HW : -IN;
    add(base, `post_${key}`, hull([[x0, 0, z0], [x1, 0, z0], [x0, 0, z1], [x1, 0, z1], [x0, hA, z0], [x1, hB, z0], [x0, hA - 0.03, z1], [x1, hB + 0.02, z1]]), freshTop('woodDark'));
  }
  for (const side of Object.keys(SIDES)) { const [y0, y1] = rowY(0); add(base, `board_${side}_0`, sideBox(side, -IN, IN, y0, y1, CORE, BOUT), boardFace); }
  // segunda fileira: trás inteira, esquerda partida em ângulo; terceira: um pedaço atrás
  // segunda fileira: só pedaços partidos atrás e à esquerda (u −1 = encosta no montante; Δy a partir do topo da tábua)
  const [r1a, r1b] = rowY(1), poly = q => q.map(([u, dy], i) => [u === -1 ? -IN : u, i < 2 ? r1a : r1b + dy]);
  const endFresh = axis => (n, c) => (n[axis] > 0.3 && Math.abs(n.y) < 0.9) || n.y > 0.5 ? 'woodFresh' : boardFace(n, c);
  add(base, 'board_-Z_1_broken', sideHull('-Z', poly(BR.backRow1), CORE, BOUT), endFresh('x'));
  add(base, 'board_-X_1_broken', sideHull('-X', poly(BR.leftRow1), CORE, BOUT), endFresh('z'));
  // diagonal de trás encurtada (presa às tábuas que restaram)
  const B = C.brace;
  add(base, 'brace_-Z_broken', sideHull('-Z', bracePoly(IN - B.width / 2, C.y0 + B.margin, 0.02, BR.braceTop, B.width), BOUT - 0.004, BOUT + B.thick), freshTop('woodDark'));
  // musgo continua no pé da frente-esquerda (mesma posição da intacta)
  const m = C.moss; add(base, 'moss', sideHull(m.side, [[m.u[0], C.y0 + m.v[0]], [m.u[1], C.y0 + m.v[0]], [m.u[1] - 0.05, C.y0 + m.v[1]], [m.u[0], C.y0 + m.v[1]]], BOUT - 0.004, BOUT + 0.004), 'moss');
  // tábuas caídas para dentro
  BR.planks.forEach((p, i) => {
    const g = new THREE.BoxGeometry(...p.size);
    add(`Crate_Broken_Plank_${i}`, 'plank', rot(g, p.rot, p.pos), (n) => Math.abs(n.x) > 0.75 ? 'woodFresh' : 'wood');
  });
}

function jarIntact(add) {
  const node = 'Jar_Intact';
  add(node, 'foot', jarPieces.foot(), 'clayDark');
  add(node, 'belly_low', jarPieces.belly_low(), jarShell);
  add(node, 'belly_high', jarPieces.belly_high(), jarShell);
  add(node, 'shoulder', jarPieces.shoulder(), n => n.y > 0.97 ? 'clayLight' : n.y < -0.97 ? 'clayDark' : 'clayLight');
  add(node, 'neck', jarPieces.neck(), 'clayDark');
  add(node, 'lip', jarPieces.lip(), lipSwatch);
  add(node, 'mouth', mouthDisc(), 'interior');
  jarDetails(add, node);
}

function jarBroken(add) {
  const base = 'Jar_Broken_Base', JB = J.broken, fy = JB.floorY;
  add(base, 'foot', jarPieces.foot(), 'clayDark');
  // parte baixa maciça até o fundo interno (topo escuro = interior visto pela abertura)
  add(base, 'belly_floor', hull([...ring(...JR[1]), ...ring(rAt(fy), fy)]), n => n.y > 0.97 ? 'interior' : n.y < -0.97 ? 'clayDark' : 'clay');
  // paredes finas partidas: uma placa convexa por face do anel, do fundo até a altura irregular dos dois vértices
  for (let k = 0; k < NS; k++) {
    if (JB.missing.includes(k)) continue;
    const pts = [];
    for (const kk of [k, k + 1]) {
      const h = JB.jagged[kk % NS];
      for (const y of [fy, h]) { const r = rAt(y); pts.push(ringPt(r, y, kk), ringPt(r - JB.wall, y, kk)); }
    }
    add(base, `wall_${k}`, hull(pts), (n, c) => n.y > 0.3 ? 'clayLight' : (n.x * c.x + n.z * c.z < 0 ? 'clayDark' : 'clay'));
  }
  for (const k of J.moss.faces) add(base, `moss_${k}`, facePatch('foot', k, J.moss.s, J.moss.t), 'moss');
  // aro + gargalo num só sólido convexo, tombado para dentro do corpo partido
  const rimPts = [...ring(...JR[4]), ...ring(J.lip.r, J.lip.y0), ...ring(J.lip.r, J.lip.y1), ...ring(J.lip.rTop, J.lip.yTop)].map(([x, y, z]) => [x, y - (JR[4][1] + J.lip.yTop) / 2, z]);
  add('Jar_Broken_Rim', 'rim', rot(new ConvexGeometry(rimPts.map(p => V(...p))), [JB.rim.tiltX, 0, JB.rim.tiltZ], JB.rim.pos), (n) => {
    const up = new THREE.Vector3(0, 1, 0).applyEuler(new THREE.Euler(JB.rim.tiltX * Math.PI / 180, 0, JB.rim.tiltZ * Math.PI / 180, 'XYZ'));
    const d = n.dot(up); return d > 0.97 ? 'interior' : d > 0.4 ? 'clayLight' : d < -0.97 ? 'interior' : 'clayDark';
  });
  // cacos: polígono irregular de 5 pontos, chanfrado no topo (face de cima = parede externa, clara)
  JB.shards.forEach((s, i) => {
    const pts = [], yaw = s.yaw * Math.PI / 180, radii = [1, 0.78, 1.1, 0.7, 0.92];
    for (let k = 0; k < 5; k++) {
      const a = yaw + k * 2 * Math.PI / 5, r = s.r * radii[k];
      pts.push([s.c[0] + r * Math.cos(a), 0, s.c[1] + r * Math.sin(a)], [s.c[0] + 0.78 * r * Math.cos(a), s.h, s.c[1] + 0.78 * r * Math.sin(a)]);
    }
    add(`Jar_Broken_Shard_${i}`, 'shard', hull(pts), n => n.y > 0.9 ? 'clayLight' : n.y < -0.9 ? 'clayDark' : 'clayDark');
  });
}

export const VARIANTS = [
  { file: `${NAME}_Jar_Intact.fbx`, family: 'jar', state: 'intact', build: jarIntact },
  { file: `${NAME}_Jar_Broken.fbx`, family: 'jar', state: 'broken', build: jarBroken },
  { file: `${NAME}_Crate_Intact.fbx`, family: 'crate', state: 'intact', build: crateIntact },
  { file: `${NAME}_Crate_Broken.fbx`, family: 'crate', state: 'broken', build: crateBroken },
];

const cellUV = sw => { const [cx, cy] = PALETTE[sw].cell; return [(cx * 4 + 2) / 16, 1 - (cy * 4 + 2) / 16]; };   // V para cima (FBX/Unity)

// Monta os nós de uma variante: cada nó tem pivô na base central da sua geometria (x, y mín, z); o arquivo inteiro tem a
// origem no pivô do prop (base central). Cor por vértice para o GLB, UV de paleta para a Unity, um material.
export function buildVariant(v) {
  const pieces = [];
  v.build((node, name, geo, swatch) => pieces.push({ node, name, geo, swatch: typeof swatch === 'function' ? swatch : () => swatch }));
  const nodes = {};
  for (const pc of pieces) (nodes[pc.node] ||= []).push(pc);
  const out = [];
  for (const [nodeName, list] of Object.entries(nodes)) {
    const b = new THREE.Box3(); for (const pc of list) { pc.geo.computeBoundingBox(); b.union(pc.geo.boundingBox); }
    const atProp = nodeName.endsWith('_Base') || nodeName.endsWith('_Intact');
    const origin = atProp ? V(0, 0, 0) : V((b.min.x + b.max.x) / 2, b.min.y, (b.min.z + b.max.z) / 2);
    const pos = [], nrm = [], uv = [], col = [], faces = [], plist = [];
    for (const pc of list) {
      const p = pc.geo.attributes.position, n = pc.geo.attributes.normal;
      for (let i = 0; i < p.count; i += 3) {
        const A = V(p.getX(i), p.getY(i), p.getZ(i)), B = V(p.getX(i + 1), p.getY(i + 1), p.getZ(i + 1)), Cc = V(p.getX(i + 2), p.getY(i + 2), p.getZ(i + 2));
        const fn = V().subVectors(B, A).cross(V().subVectors(Cc, A)).normalize(), ctr = V().add(A).add(B).add(Cc).divideScalar(3);
        const sw = pc.swatch(fn, ctr);
        if (!PALETTE[sw]) throw new Error(`${nodeName}/${pc.name}: amostra ${sw}`);
        const [u, vv] = cellUV(sw), c = new THREE.Color(PALETTE[sw].hex);
        [A, B, Cc].forEach((q, k) => { const w = q.clone().sub(origin); pos.push(w.x, w.y, w.z); nrm.push(n.getX(i + k), n.getY(i + k), n.getZ(i + k)); uv.push(u, vv); col.push(c.r, c.g, c.b); });
        faces.push({ swatch: sw, piece: `${pc.name}#${list.indexOf(pc)}` });
      }
      plist.push({ name: pc.name, triangles: tris(pc.geo), signedVolume: +signedVolume(pc.geo).toFixed(7) });
    }
    const geo = new THREE.BufferGeometry();
    geo.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); geo.setAttribute('normal', new THREE.Float32BufferAttribute(nrm, 3));
    geo.setAttribute('uv', new THREE.Float32BufferAttribute(uv, 2)); geo.setAttribute('color', new THREE.Float32BufferAttribute(col, 3));
    geo.computeBoundingBox();
    const mat = new THREE.MeshStandardMaterial({ name: MATERIAL.name, color: 0xffffff, vertexColors: true, metalness: MATERIAL.metallic, roughness: 1 - MATERIAL.smoothness, flatShading: true });
    const mesh = new THREE.Mesh(geo, mat); mesh.name = nodeName; mesh.position.copy(origin);
    out.push({ name: nodeName, mesh, faces, pieces: plist, origin: origin.toArray().map(x => +x.toFixed(6)) });
  }
  return out;
}

// ———————————————————————— FBX 7.4 ASCII: N nós Mesh, 1 material, 1 textura, sem animação ————————————————————————
const fnum = v => { if (!Number.isFinite(v)) throw new Error('valor não finito no FBX'); const s = (+v.toFixed(6)).toString(); return s === '-0' ? '0' : s; };
const farr = (name, a, fmt = fnum, ind = '\t\t') => `${ind}${name}: *${a.length} {\n${ind}\ta: ${a.map(fmt).join(',')}\n${ind}} \n`;
export function toFBX(nodes) {
  let nextId = 5000000; const id = () => nextId++;
  const objects = [], conns = [], counts = {}, info = {};
  const addObj = (type, text) => { objects.push(text); counts[type] = (counts[type] || 0) + 1; };
  const m = MATERIAL, mid = id();
  addObj('Material', `\tMaterial: ${mid}, "Material::${m.name}", "" {\n\t\tVersion: 102\n\t\tShadingModel: "phong"\n\t\tMultiLayer: 0\n\t\tProperties70:  {\n` +
    `\t\t\tP: "DiffuseColor", "Color", "", "A",1,1,1\n\t\t\tP: "DiffuseFactor", "Number", "", "A",1\n\t\t\tP: "EmissiveColor", "Color", "", "A",0,0,0\n\t\t\tP: "EmissiveFactor", "Number", "", "A",0\n` +
    `\t\t\tP: "Shininess", "Number", "", "A",${fnum(m.smoothness * 100)}\n\t\t\tP: "ReflectionFactor", "Number", "", "A",${fnum(m.metallic)}\n\t\t}\n\t}\n`);
  const file = m.baseMap, nm = file.replace(/\.png$/, ''), vid = id(), tid = id();
  addObj('Video', `\tVideo: ${vid}, "Video::${nm}", "Clip" {\n\t\tType: "Clip"\n\t\tProperties70:  {\n\t\t\tP: "Path", "KString", "XRefUrl", "", "${file}"\n\t\t}\n\t\tUseMipMap: 0\n\t\tFilename: "${file}"\n\t\tRelativeFilename: "${file}"\n\t}\n`);
  addObj('Texture', `\tTexture: ${tid}, "Texture::${nm}", "" {\n\t\tType: "TextureVideoClip"\n\t\tVersion: 202\n\t\tTextureName: "Texture::${nm}"\n\t\tProperties70:  {\n\t\t\tP: "UseMaterial", "bool", "", "",1\n\t\t}\n\t\tMedia: "Video::${nm}"\n\t\tFileName: "${file}"\n\t\tRelativeFilename: "${file}"\n\t\tModelUVTranslation: 0,0\n\t\tModelUVScaling: 1,1\n\t\tTexture_Alpha_Source: "None"\n\t\tCropping: 0,0,0,0\n\t}\n`);
  conns.push(`\tC: "OO",${vid},${tid}`, `\tC: "OP",${tid},${mid}, "DiffuseColor"`);
  for (const nd of nodes) {
    const { mesh, faces } = nd, o = nd.origin, modelId = id();
    addObj('Model', `\tModel: ${modelId}, "Model::${nd.name}", "Mesh" {\n\t\tVersion: 232\n\t\tProperties70:  {\n\t\t\tP: "RotationActive", "bool", "", "",1\n\t\t\tP: "InheritType", "enum", "", "",1\n\t\t\tP: "ScalingMax", "Vector3D", "Vector", "",0,0,0\n\t\t\tP: "DefaultAttributeIndex", "int", "Integer", "",0\n` +
      `\t\t\tP: "Lcl Translation", "Lcl Translation", "", "A",${o.map(fnum).join(',')}\n\t\t\tP: "Lcl Rotation", "Lcl Rotation", "", "A",0,0,0\n\t\t\tP: "Lcl Scaling", "Lcl Scaling", "", "A",1,1,1\n\t\t}\n\t\tShading: Y\n\t\tCulling: "CullingOff"\n\t}\n`);
    conns.push(`\tC: "OO",${modelId},0`);
    // pontos de controle deduplicados POR PEÇA (peças encostadas não se fundem; cada peça segue fechada no arquivo)
    const g = mesh.geometry, Pp = g.attributes.position, N = g.attributes.normal, T = g.attributes.uv;
    const key = new Map(), cp = [], pvi = [], nrm = [], uvk = new Map(), uv = [], uvi = [], matIdx = [];
    for (let i = 0; i < Pp.count; i++) {
      const k = `${faces[Math.floor(i / 3)].piece}|${Pp.getX(i).toFixed(5)},${Pp.getY(i).toFixed(5)},${Pp.getZ(i).toFixed(5)}`;
      let c = key.get(k); if (c === undefined) { c = cp.length / 3; key.set(k, c); cp.push(Pp.getX(i), Pp.getY(i), Pp.getZ(i)); }
      pvi.push(i % 3 === 2 ? -(c + 1) : c); nrm.push(N.getX(i), N.getY(i), N.getZ(i));
      const tk = `${T.getX(i).toFixed(4)},${T.getY(i).toFixed(4)}`; let t = uvk.get(tk); if (t === undefined) { t = uv.length / 2; uvk.set(tk, t); uv.push(T.getX(i), T.getY(i)); } uvi.push(t);
      if (i % 3 === 0) matIdx.push(0);
    }
    const gid = id();
    addObj('Geometry', `\tGeometry: ${gid}, "Geometry::${nd.name}", "Mesh" {\n` + farr('Vertices', cp) + farr('PolygonVertexIndex', pvi, String) +
      `\t\tGeometryVersion: 124\n\t\tLayerElementNormal: 0 {\n\t\t\tVersion: 101\n\t\t\tName: ""\n\t\t\tMappingInformationType: "ByPolygonVertex"\n\t\t\tReferenceInformationType: "Direct"\n` + farr('Normals', nrm, fnum, '\t\t\t') + `\t\t}\n` +
      `\t\tLayerElementUV: 0 {\n\t\t\tVersion: 101\n\t\t\tName: "UVMap"\n\t\t\tMappingInformationType: "ByPolygonVertex"\n\t\t\tReferenceInformationType: "IndexToDirect"\n` + farr('UV', uv, fnum, '\t\t\t') + farr('UVIndex', uvi, String, '\t\t\t') + `\t\t}\n` +
      `\t\tLayerElementMaterial: 0 {\n\t\t\tVersion: 101\n\t\t\tName: ""\n\t\t\tMappingInformationType: "AllSame"\n\t\t\tReferenceInformationType: "IndexToDirect"\n` + farr('Materials', [0], String, '\t\t\t') + `\t\t}\n` +
      `\t\tLayer: 0 {\n\t\t\tVersion: 100\n\t\t\tLayerElement:  {\n\t\t\t\tType: "LayerElementNormal"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t\tLayerElement:  {\n\t\t\t\tType: "LayerElementMaterial"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t\tLayerElement:  {\n\t\t\t\tType: "LayerElementUV"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t}\n\t}\n`);
    conns.push(`\tC: "OO",${gid},${modelId}`, `\tC: "OO",${mid},${modelId}`);
    info[nd.name] = { polygons: Pp.count / 3, controlPoints: cp.length / 3, translation: o };
  }
  const total = Object.values(counts).reduce((a, b) => a + b, 0) + 1;
  const header = `; FBX 7.4.0 project file\n; Gerado por Tools/Lemo3DGenerator/src/production/ModelDestructible.mjs (three.js do Lemo-Opuscar)\n; ----------------------------------------------------\n\n` +
    `FBXHeaderExtension:  {\n\tFBXHeaderVersion: 1003\n\tFBXVersion: 7400\n\tCreator: "Lemo3DGenerator ModelDestructible"\n}\n` +
    `GlobalSettings:  {\n\tVersion: 1000\n\tProperties70:  {\n\t\tP: "UpAxis", "int", "Integer", "",1\n\t\tP: "UpAxisSign", "int", "Integer", "",1\n\t\tP: "FrontAxis", "int", "Integer", "",2\n\t\tP: "FrontAxisSign", "int", "Integer", "",1\n\t\tP: "CoordAxis", "int", "Integer", "",0\n\t\tP: "CoordAxisSign", "int", "Integer", "",1\n\t\tP: "OriginalUpAxis", "int", "Integer", "",1\n\t\tP: "OriginalUpAxisSign", "int", "Integer", "",1\n\t\tP: "UnitScaleFactor", "double", "Number", "",100\n\t\tP: "OriginalUnitScaleFactor", "double", "Number", "",100\n\t}\n}\n\n` +
    `Documents:  {\n\tCount: 1\n\tDocument: ${id()}, "Scene", "Scene" {\n\t\tProperties70:  {\n\t\t\tP: "SourceObject", "object", "", ""\n\t\t}\n\t\tRootNode: 0\n\t}\n}\n\nReferences:  {\n}\n\n` +
    `Definitions:  {\n\tVersion: 100\n\tCount: ${total}\n\tObjectType: "GlobalSettings" {\n\t\tCount: 1\n\t}\n` + Object.entries(counts).map(([k, n]) => `\tObjectType: "${k}" {\n\t\tCount: ${n}\n\t}\n`).join('') + `}\n\n`;
  return { text: header + `Objects:  {\n${objects.join('')}}\n\nConnections:  {\n${conns.join('\n')}\n}\n`, info };
}

// ———————————————————————— paleta PNG, metas ————————————————————————
const hex = h => [1, 3, 5].map(i => parseInt(h.slice(i, i + 2), 16));
function palettePNG() {
  const w = 16, h = 16, px = Buffer.alloc(w * h * 4), fill = hex(PALETTE.clayDark.hex);
  for (let i = 0; i < w * h; i++) { px[i * 4] = fill[0]; px[i * 4 + 1] = fill[1]; px[i * 4 + 2] = fill[2]; px[i * 4 + 3] = 255; }   // células livres = barro escuro
  for (const sw of Object.values(PALETTE)) {
    const [r, g, b] = hex(sw.hex), [cx, cy] = sw.cell;
    for (let y = cy * 4; y < cy * 4 + 4; y++) for (let x = cx * 4; x < cx * 4 + 4; x++) { const o = (y * w + x) * 4; px[o] = r; px[o + 1] = g; px[o + 2] = b; }
  }
  return encodePNG(w, h, px);
}
export const stableGuid = key => createHash('sha256').update('Eldoria/A12/' + key).digest('hex').slice(0, 32);
function textureMeta(file) {
  let t = readFileSync(TEX_TEMPLATE, 'utf8').replace('{{GUID}}', stableGuid(file));
  const set = (k, v) => { const re = () => new RegExp(`(\\n[ \\t]*${k}: )[^\\r\\n]*`, 'g'); if (!re().test(t)) throw new Error(`template sem ${k}`); t = t.replace(re(), `$1${v}`); };
  set('enableMipMap', 0); set('sRGBTexture', 1); set('filterMode', 0); set('alphaUsage', 0); set('alphaIsTransparency', 0); set('textureCompression', 0); set('maxTextureSize', 32);
  return t;
}
// ModelImporter do Loot_XpOrb sem mudanças além do GUID: estático (importAnimation 0, animationType 0), sem colliders,
// escala 1, unidades do arquivo, materiais pela descrição do FBX.
function modelMeta(file) {
  let t = readFileSync(FBX_TEMPLATE, 'utf8');
  const rep = (re, v) => { if (!re.test(t)) throw new Error(`template ModelImporter sem ${re}`); t = t.replace(re, v); };
  rep(/^guid: [0-9a-f]{32}$/m, `guid: ${stableGuid(file)}`);
  if (!/addColliders: 0/.test(t) || !/globalScale: 1/.test(t) || !/^  importAnimation: 0\r?$/m.test(t) || !/^  animationType: 0\r?$/m.test(t)) throw new Error('template ModelImporter inesperado');
  return t;
}
const folderMeta = key => `fileFormatVersion: 2\nguid: ${stableGuid(key)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`;

// ———————————————————————— validação interna ————————————————————————
function boundsOf(nodes) {
  const all = new THREE.Box3();
  for (const nd of nodes) all.union(nd.mesh.geometry.boundingBox.clone().translate(V(...nd.origin)));
  return all;
}
const r4 = a => a.map(v => +v.toFixed(4));
export function validate(built) {
  const issues = [], out = {};
  for (const v of VARIANTS) {
    const nodes = built[v.file], b = boundsOf(nodes), size = b.getSize(V()), ctr = b.getCenter(V());
    let t = 0; const nodeRows = [];
    for (const nd of nodes) {
      const tt = tris(nd.mesh.geometry); t += tt;
      for (const pc of nd.pieces) if (pc.signedVolume <= 0) issues.push(`${v.file}/${nd.name}/${pc.name}: volume ≤ 0`);
      const bySwatch = {}; for (const f of nd.faces) bySwatch[f.swatch] = (bySwatch[f.swatch] || 0) + 1;
      nodeRows.push({ name: nd.name, triangles: tt, pieces: nd.pieces.length, origin: nd.origin, facesBySwatch: bySwatch, pieceList: nd.pieces });
    }
    if (t > BUDGET.trianglesPerProp) issues.push(`${v.file}: ${t} tris > ${BUDGET.trianglesPerProp}`);
    if (Math.abs(b.min.y) > 1e-4) issues.push(`${v.file}: não apoia em y = 0 (${b.min.y})`);
    if (Math.max(Math.abs(b.min.x), Math.abs(b.max.x)) > FOOTPRINT_F09.halfX || Math.max(Math.abs(b.min.z), Math.abs(b.max.z)) > FOOTPRINT_F09.halfZ || b.max.y > FOOTPRINT_F09.height) issues.push(`${v.file}: sai do footprint F09`);
    out[v.file] = { family: v.family, state: v.state, triangles: t, nodes: nodeRows, bounds: { min: r4(b.min.toArray()), max: r4(b.max.toArray()), size: r4(size.toArray()), center: r4(ctr.toArray()) } };
  }
  for (const fam of ['jar', 'crate']) {
    const I = out[VARIANTS.find(v => v.family === fam && v.state === 'intact').file].bounds, B = out[VARIANTS.find(v => v.family === fam && v.state === 'broken').file].bounds;
    const eps = 1e-4;
    if (B.min[0] < I.min[0] - eps || B.max[0] > I.max[0] + eps || B.min[2] < I.min[2] - eps || B.max[2] > I.max[2] + eps || B.max[1] > I.max[1] + eps) issues.push(`${fam}: quebrado sai dos bounds do intacto (${JSON.stringify(B)} vs ${JSON.stringify(I)})`);
    if (Math.abs(I.center[0]) > 0.01 || Math.abs(I.center[2]) > 0.01) issues.push(`${fam}: pivô longe do centro da base (${I.center})`);
  }
  return { ok: !issues.length, issues, variants: out };
}

// ———————————————————————— principal ————————————————————————
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const built = Object.fromEntries(VARIANTS.map(v => [v.file, buildVariant(v)]));
  const val = validate(built);
  for (const v of VARIANTS) { const r = val.variants[v.file]; console.log(`${v.file}: ${r.triangles} tris, ${r.nodes.length} nós, size ${JSON.stringify(r.bounds.size)}, min ${JSON.stringify(r.bounds.min)}, max ${JSON.stringify(r.bounds.max)}`); }
  if (!val.ok) { console.error('FALHOU:\n  ' + val.issues.join('\n  ')); process.exit(1); }
  if (has('check')) { console.log('check OK'); process.exit(0); }
  mkdirSync(SRC, { recursive: true });
  const fbxInfo = {};
  for (const v of VARIANTS) {
    const fbx = toFBX(built[v.file]);
    writeFileSync(join(OUT, v.file), fbx.text);
    writeFileSync(join(OUT, v.file + '.meta'), modelMeta(v.file));
    fbxInfo[v.file] = { family: v.family, state: v.state, nodes: built[v.file].map(n => n.name), info: fbx.info, bytes: Buffer.byteLength(fbx.text) };
  }
  writeFileSync(join(OUT, MATERIAL.baseMap), palettePNG());
  writeFileSync(join(OUT, MATERIAL.baseMap + '.meta'), textureMeta(MATERIAL.baseMap));
  writeFileSync(join(PROJECT, STAGING + '.meta'), folderMeta('folder/' + ID));
  // GLB de revisão: as quatro variantes lado a lado em X (1,1 m entre pivôs)
  const LAYOUT = { [VARIANTS[0].file]: -1.65, [VARIANTS[1].file]: -0.55, [VARIANTS[2].file]: 0.55, [VARIANTS[3].file]: 1.65 };
  const scene = new THREE.Group(); scene.name = NAME;
  for (const v of VARIANTS) { const g = new THREE.Group(); g.name = v.file.replace(/\.fbx$/, ''); g.position.x = LAYOUT[v.file]; for (const nd of built[v.file]) g.add(nd.mesh); scene.add(g); }
  const glb = Buffer.from(await new GLTFExporter().parseAsync(scene, { binary: true, onlyVisible: false }));
  writeFileSync(join(SRC, `${NAME}.glb`), glb);
  const source = {
    id: ID, batch: 'A12', name: NAME, generator: 'Tools/Lemo3DGenerator/src/production/ModelDestructible.mjs', three: THREE_VERSION, lemoRoot: LEMO_ROOT, node: process.version,
    units: 'metros, +Y para cima, +Z frente (ficha A12); origem de cada FBX = pivô do prop na base central; nós *_Intact e *_Broken_Base na origem, fragmentos com pivô na base central da própria peça',
    footprint_f09: FOOTPRINT_F09, budget: BUDGET, shape: SHAPE, palette: PALETTE, material: MATERIAL,
    variants: Object.fromEntries(VARIANTS.map(v => [v.file, { family: v.family, state: v.state, triangles: val.variants[v.file].triangles, bounds: val.variants[v.file].bounds, nodes: val.variants[v.file].nodes.map(n => ({ name: n.name, origin: n.origin, triangles: n.triangles, pieces: n.pieces })) }])),
    fbx: fbxInfo, glb: { file: `${NAME}.glb`, layoutX: LAYOUT, note: 'somente revisão; não é importado pela Unity' },
    guids: { folder: stableGuid('folder/' + ID), palette: stableGuid(MATERIAL.baseMap), ...Object.fromEntries(VARIANTS.map(v => [v.file, stableGuid(v.file)])) },
    import: {
      model: 'ModelImporter copiado de Assets/_Project/Art/Items/Models/Loot_XpOrb.fbx.meta (Unity 6000.6, serializedVersion 24600) mudando só o GUID: estático (importAnimation 0, animationType 0), globalScale 1, useFileUnits, addColliders 0, normais importadas, materialImportMode 2 (descrição do FBX; o material sai embutido em cada FBX — remapear para um .mat único na integração).',
      texture: 'Template FX_Spark (Tools/FxHealGenerator/texture.meta.template) com point, sem mipmaps, sem compressão, sem alpha, max 32, sRGB.',
    },
    integration_proposal: {
      status: 'NÃO INTEGRADO — tarefa Codex separada. DestructibleProp.cs, DestructiblePropPresenter.cs, DestructiblePropDefinition.cs, DestructiblePropSceneBuilder.cs, Prop_Vase_F09.asset, Mat_Vase_F09, cenas e prefabs não foram alterados.',
      presenter: 'DestructiblePropPresenter já alterna os GameObjects "intact" e "rubble" por estado: instanciar *_Intact.fbx como filho de Intact e *_Broken.fbx como filho de Rubble. Nada no código muda.',
      flash: 'DamageFlash.targetRenderer → MeshRenderer do nó Jar_Intact (ou Crate_Intact). O intacto é um único nó/renderer de propósito: o tint por MaterialPropertyBlock em _BaseColor multiplica a paleta, então um único renderer cobre o prop inteiro.',
      colliders: 'Colliders e layer continuam do domínio (layer Destructible, fora da NavMesh/visão/projéteis). Preferência: manter os colliders atuais do vaso (Sphere do Body + Capsule do Rim) sem MeshRenderer, como volumes de acerto, e desligar só os renderers das primitivas Body/Rim/Shard_*; o jar A12 (0,78 × 0,69 m) cabe dentro deles. Para a caixa, um BoxCollider ~0,70 × 0,62 × 0,70 m centrado em y 0,31, definido pela tarefa de domínio. Os FBX não trazem colisor.',
      placement: 'Ambos cabem no footprint do vaso F09 (±0,45 m, 1,04 m) e a variante quebrada nunca sai dos bounds da intacta, então a posição lateral atual (sala + (−5,5; 0; −2)) e as distâncias de portas/spawns/centro continuam válidas. Caixa: +Z é a frente (rachadura e musgo); qualquer rotação Y serve.',
      family: 'Qual família vai em cada sala (vaso, caixa ou ambos alternados) é decisão de design da tarefa de integração; ambos usam a mesma definição de 40 HP sem regra nova. Não registrar como inimigo da sala nem participar da limpeza.',
      material: `Um único Material URP/Lit "${MATERIAL.name}" (metallic 0, smoothness 0,15, _BaseMap = ${MATERIAL.baseMap}) compartilhado pelos quatro FBX: Search and Remap por nome ou externalObjects nos .meta via builder. Não reutilizar Mat_Vase_F09 (cor sólida, sem paleta).`,
      burst: 'Não há burst de quebra no F09 atual (troca estática). Se um burst for adicionado depois, reutilizar um efeito existente (por exemplo partículas do FX-SLAM/FX-LOOT) disparado pelo evento Broken; não faz parte deste lote.',
      tests: ['quebra única troca Intact → Rubble sem dano/drops repetidos', 'flash de dano visível no renderer A12 intacto e respeitando a opção de flash', 'desativar/reativar mantém o estado; reiniciar volta intacto', 'NavMesh e portas inalterados (layer e colliders do domínio)', 'leitura do vaso e da caixa na câmera real das cinco salas, ao lado do Hero'],
    },
    licence: 'Projeto Eldoria; geometria própria gerada por código (three.js r170 MIT, somente leitura no Lemo-Opuscar). Sem IA generativa nem asset de terceiros; desenho genérico, sem referência comercial.',
  };
  writeFileSync(join(SRC, `${NAME}.source.json`), JSON.stringify(source, null, 2));
  writeFileSync(join(SRC, 'ModelDestructible.generator.json'), JSON.stringify({ ...val, glbBytes: glb.length }, null, 2));
  console.log('OK →', relative(PROJECT, OUT));
}
