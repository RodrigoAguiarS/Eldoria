#!/usr/bin/env node
// A15 — Cripta inundada: FONTE DE RENDER do conceito 2D de ambiente (PROMPT_CATALOG.md, A15; condicional F14, já integrada).
// NÃO é kit de produção: não exporta FBX, não gera colliders, NavMesh, shader de água nem cena. O GLB gerado aqui existe só
// para preview/concept_crypt.html desenhar a folha 2048×1536 (vista 45° da sala + estudo de peças) com a linguagem
// low-poly do projeto (mesmos helpers de geometria e paleta do DungeonRoom_Concept_01/ModularKit).
//   node src/production/ConceptCrypt.mjs [--out <pasta>]
//
// Gabarito: Room_Normal (interior 16×16 m, paredes de 1 m de espessura, vão de 4 m ao norte e ao sul, módulos de 2 m),
// altura de parede 1,4 m como DungeonMvpSceneBuilder.WallHeight. Cada módulo de borda de 2×2 m tem 1 m de calçada seca e
// 1 m de canal raso (água 0,20 m abaixo do piso); nos vãos das portas o canal passa sob uma laje seca (passarela). O centro
// de 12×12 m é piso seco, e os 10×10 m centrais não têm nada acima do piso. Pilares baixos (1,5 m) em (±6, ±6).
// Coordenadas no referencial glTF do gerador (+Y para cima, +Z = norte/frente); a sala é simétrica em X, então o
// espelhamento de eixo na importação da Unity não muda o layout.
import { mkdirSync, writeFileSync } from 'node:fs';
import { join, resolve, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import { THREE, GLTFExporter, BufferGeometryUtils, THREE_VERSION } from '../lemo-three.mjs';
import { bevelBox, xform, jitter, convex, strut, cone, cyl, finish } from '../geo.mjs';

const HERE = dirname(fileURLToPath(import.meta.url)), LAB = resolve(HERE, '..', '..'), PROJECT = resolve(LAB, '..', '..');
const argv = process.argv.slice(2), arg = (k, d) => { const i = argv.indexOf('--' + k); return i >= 0 ? argv[i + 1] : d; };
const OUT = resolve(arg('out', join(PROJECT, 'Assets/_Project/Art/Concepts/Source~/FloodedCrypt')));

// ———————————————————————— gabarito (metros) ————————————————————————
export const ROOM = {
  interior: 16, wallThick: 1, wallHeight: 1.4, module: 2, door: 4, doors: ['N', 'S'],
  ledge: 1, channel: 1, waterY: -.16, bedY: -.42, curbTop: .04, clear: 10, dryCore: 12,
  pillars: { at: 6, plinth: 1.8, height: 1.47 },
  note: 'Room_Normal (DungeonFloorSceneBuilder.NormalInterior 16, paredes 1 m) com parede de 1,4 m (DungeonMvpSceneBuilder.WallHeight); só referência de layout, não colisão',
};

// Paleta da ficha A15 (= cores oficiais em ART_DIRECTION_AND_PROMPTS.md). Variações de piso ±6 % como GA_Stone_Floor_A/B/C.
export const MATERIALS = {
  stone: { color: '#55596B', roughness: .92, note: 'piso oficial' },
  stoneB: { color: '#505465', roughness: .92, note: 'piso −6 %' },
  stoneC: { color: '#5A5E71', roughness: .92, note: 'piso +6 %' },
  wet: { color: '#474B5B', roughness: .55, note: 'piso úmido junto aos canais (mais escuro, menos fosco)' },
  walk: { color: '#60647A', roughness: .9, note: 'lajes da passarela seca porta-centro-porta' },
  stoneDark: { color: '#3E4152', roughness: .95, note: 'paredes e pilares (parede oficial)' },
  cap: { color: '#5E6276', roughness: .9, note: 'capas das paredes e meio-fio do canal' },
  joint: { color: '#2A2C38', roughness: 1, note: 'juntas e boca dos bueiros' },
  bed: { color: '#2C303D', roughness: 1, note: 'fundo do canal' },
  water: { color: '#313E55', roughness: .16, metalness: .05, opacity: .9, note: 'água azul-escura dessaturada (rasa)' },
  ripple: { color: '#5A6C88', roughness: .3, opacity: .8, note: 'reflexos/ondulações na água' },
  moss: { color: '#4C5A44', roughness: 1, note: 'musgo oficial, contido' },
  crack: { color: '#1E1830', roughness: 1, note: 'fendas (sombra oficial)' },
  iron: { color: '#34303A', roughness: .6, metalness: .4, note: 'suporte de tocha' },
  flame: { color: '#FF9A2E', emissive: '#FF9A2E', emissiveIntensity: 2.4, roughness: .5, note: 'chama de tocha (oficial)' },
  rune: { color: '#4FD8FF', emissive: '#4FD8FF', emissiveIntensity: 1.6, roughness: .4, note: 'runas dos nichos, discretas' },
};

// ———————————————————————— construção ————————————————————————
let SEED = 1;
const blk = (w, h, d, pos, o = {}) => xform(jitter(bevelBox(w, h, d, o.c ?? .1, { top: o.top ?? 1, bottom: o.bottom ?? 1, topZ: o.topZ ?? o.top ?? 1, bottomZ: o.bottomZ ?? 1 }), o.j ?? Math.min(w, h, d) * .06, o.seed ?? SEED++), { pos, rot: o.rot ?? [0, 0, 0] });
// placa fina irregular deitada (musgo, poça): hexágono com raios determinísticos
const patch = (cx, y, cz, r, t, seed) => {
  const pts = []; for (let k = 0; k < 7; k++) { const a = k / 7 * Math.PI * 2 + seed, rr = r * (.65 + .35 * Math.abs(Math.sin(seed * 12.9 + k * 3.7))); pts.push([cx + Math.cos(a) * rr, y, cz + Math.sin(a) * rr * .8], [cx + Math.cos(a) * rr, y + t, cz + Math.sin(a) * rr * .8]); }
  return convex(pts);
};
// fio no chão / na parede (fenda, traço de runa)
const line = (pts, r) => pts.slice(1).map((b, i) => strut(pts[i], b, r, r, 4));
// transforma uma geometria do referencial local de um módulo (x ao longo da parede, z = 0 → 1 indo para a parede)
const place = (g, M) => xform(g, M);

class Builder {
  constructor() { this.parts = []; }
  add(group, mat, tag, geo) { if (Array.isArray(geo)) geo.forEach(g => this.add(group, mat, tag, g)); else this.parts.push({ group, mat, tag, geo }); }
  in(group, M) { const b = this; return (mat, tag, geo) => b.add(group, mat, tag, (Array.isArray(geo) ? geo : [geo]).map(g => M ? place(g, M) : g)); }
}

// —— módulo de borda 2×2 m (local: x ∈ [−1, 1] ao longo da parede; z ∈ [−1, 0] calçada seca, z ∈ [0, 1] canal) ——
function edgeModule(add, kind, o = {}) {
  const { waterY, bedY, curbTop } = ROOM;
  const wetMat = o.dry ? 'stoneB' : 'wet';
  if (kind === 'straight') {
    add(wetMat, 'floor', blk(1.96, .3, .9, [0, -.15, -.55], { c: .08, j: .03 }));
    add('cap', 'curb', blk(2.0, curbTop - bedY, .22, [0, (curbTop + bedY) / 2, -.02], { c: .12, j: .015 }));
    add('bed', 'bed', blk(2.0, .12, 1.0, [0, bedY - .06, .5], { c: .1, j: .02 }));
    add('water', 'water', blk(2.0, .03, .92, [0, waterY, .55], { c: .05, j: 0 }));
    add('ripple', 'water', blk(.9 + (o.r ?? 0) * .3, .008, .05, [-.25 + (o.r ?? 0) * .3, waterY + .02, .4], { c: .3, j: 0 }));
    add('ripple', 'water', blk(.5, .008, .04, [.5 - (o.r ?? 0) * .2, waterY + .02, .72], { c: .3, j: 0 }));
    add('ripple', 'water', blk(.35, .008, .035, [-.6 + (o.r ?? 0) * .1, waterY + .02, .86], { c: .3, j: 0 }));
    add('ripple', 'water', blk(1.9, .008, .03, [0, waterY + .02, .16], { c: .3, j: 0 }));   // linha clara junto ao meio-fio
  } else if (kind === 'corner') {   // água em L junto às duas paredes (z > 0 ou x > 0)
    add(wetMat, 'floor', blk(.9, .3, .9, [-.55, -.15, -.55], { c: .08, j: .03 }));
    add('cap', 'curb', blk(1.12, curbTop - bedY, .22, [-.44, (curbTop + bedY) / 2, -.02], { c: .12, j: .015 }));
    add('cap', 'curb', blk(.22, curbTop - bedY, .9, [-.02, (curbTop + bedY) / 2, -.55], { c: .12, j: .015 }));
    add('bed', 'bed', blk(2.0, .12, 1.0, [0, bedY - .06, .5], { c: .1, j: .02 }));
    add('bed', 'bed', blk(1.0, .12, 1.0, [.5, bedY - .06, -.5], { c: .1, j: .02 }));
    add('water', 'water', blk(2.0, .03, .92, [0, waterY, .55], { c: .05, j: 0 }));
    add('water', 'water', blk(.92, .03, 1.0, [.55, waterY, -.5], { c: .05, j: 0 }));
    add('ripple', 'water', blk(.05, .008, .7, [.4, waterY + .02, -.45], { c: .3, j: 0 }));
  } else if (kind === 'bridge') {   // laje seca da passarela sobre o canal; a água passa por baixo (bueiro escuro, sem água visível na laje)
    add('walk', 'bridge', blk(1.96, .34, 1.96, [0, -.17, 0], { c: .08, j: .025 }));
    add('bed', 'bed', blk(2.0, .12, 1.0, [0, bedY - .06, .5], { c: .1, j: .02 }));
    add('joint', 'bed', blk(2.0, .26, .9, [0, (bedY - .34) / 2 + .02, .55], { c: .05, j: 0 }));   // vão escuro sob a laje
    for (const sx of [-1, 1]) add('cap', 'curb', blk(.16, .1, 1.9, [sx * .92, .03, 0], { c: .2, j: .01 }));   // guias laterais baixas
  }
}

// —— módulo de parede 2 m (local: x ∈ [−1, 1]; face interna em z = 0, externa em z = 1; base no fundo do canal) ——
function wallModule(add, kind, o = {}) {
  const H = ROOM.wallHeight, b = ROOM.bedY, len = o.len ?? 2;
  if (kind === 'plain' || kind === 'torch') {
    add('stoneDark', 'wall', blk(len - .04, H - b, 1, [0, (H + b) / 2, .5], { c: .08, j: .05 }));
    add('cap', 'wall', blk(len + .06, .16, 1.1, [0, H + .06, .5], { c: .2, j: .03, top: .96 }));
    if (kind === 'torch') torch(add, [o.tx ?? 0, 0, 0]);
  } else if (kind === 'niche') {   // nicho de runa: abertura 0,96 × 0,7 m, 0,4 m de profundidade, peitoril a 0,35 m
    const y0 = .35, y1 = 1.05, dz = .4;
    for (const sx of [-1, 1]) add('stoneDark', 'wall', blk(.5, H - b, 1, [sx * .74, (H + b) / 2, .5], { c: .08, j: .04 }));
    add('stoneDark', 'wall', blk(1.0, y0 - b, 1, [0, (y0 + b) / 2, .5], { c: .08, j: .03 }));
    add('stoneDark', 'wall', blk(1.0, H - y1, 1, [0, (H + y1) / 2, .5], { c: .1, j: .03 }));
    add('stoneDark', 'wall', blk(1.0, y1 - y0, 1 - dz, [0, (y0 + y1) / 2, dz + (1 - dz) / 2], { c: .08, j: .02 }));
    add('cap', 'niche', blk(1.1, .07, .5, [0, y0 + .03, .18], { c: .25, j: .01 }));                    // peitoril
    add('cap', 'wall', blk(len + .06, .16, 1.1, [0, H + .06, .5], { c: .2, j: .03, top: .96 }));
    // runa: losango vertical com traço central, na parede de fundo (face voltada para a sala)
    const z = dz - .012, cy = (y0 + y1) / 2;
    add('rune', 'rune', line([[0, cy + .24, z], [.14, cy, z], [0, cy - .24, z], [-.14, cy, z], [0, cy + .24, z]], .014));
    add('rune', 'rune', line([[0, cy + .13, z], [0, cy - .13, z]], .012));
    add('moss', 'decor', patch(.25, y0 + .07, .12, .14, .02, 5.1));
  }
}
function torch(add, [x, y, z]) {
  // suporte de ferro na face interna, copo e chama facetada; topo da chama a ~1,32 m
  add('iron', 'torch', blk(.12, .3, .1, [x, .92, z - .05], { c: .2, j: 0 }));
  add('iron', 'torch', strut([x, .9, z - .06], [x, 1.02, z - .26], .03, .03, 4));
  add('iron', 'torch', cyl(.09, .055, .12, 6, { pos: [x, 1.07, z - .28] }));
  add('flame', 'flame', cone(.075, .22, 5, { pos: [x, 1.24, z - .28] }));
}
function pillar(add, x, z) {
  const { plinth } = ROOM.pillars;
  add('stoneDark', 'pillar', blk(plinth, .25, plinth, [x, .125, z], { c: .12, j: .03 }));
  add('stone', 'pillar', blk(1.25, 1.0, 1.25, [x, .74, z], { top: .92, c: .1, j: .05 }));
  add('cap', 'pillar', blk(1.55, .24, 1.55, [x, 1.35, z], { c: .2, j: .03 }));
  add('moss', 'decor', patch(x - .3, 1.47, z + .2, .38, .025, x + z * 3));
  add('moss', 'decor', patch(x + .55, .25, z - .55, .22, .02, x * 2 + z));
  add('crack', 'decor', line([[x - .2, 1.1, z - .63], [x - .05, .9, z - .63], [x - .15, .62, z - .63]], .012));
}
// água caindo de uma bica na parede para dentro do canal
function spout(add, x, z) {
  add('cap', 'spout', blk(.36, .18, .5, [x, .74, z - .1], { c: .2, j: .01 }));
  add('ripple', 'spout', blk(.14, .9, .06, [x, .3, z - .32], { c: .2, j: 0 }));
  add('ripple', 'water', patch(x, ROOM.waterY + .015, z - .4, .22, .01, 2.2));
}

// —— sala completa ——
export const LAYOUT = { torches: [], niches: [], spouts: [], pillars: [], doors: [] };
function room(B) {
  const I = ROOM.interior / 2, H = ROOM.wallHeight, add = B.in('Room');
  // base escura sob o piso seco (aparece nas juntas)
  add('joint', 'floorBase', blk(14, .2, 14, [0, -.14, 0], { c: .02, j: 0 }));
  // piso seco central 12×12 em placas de 2 m; coluna x ∈ [−2, 2] = passarela; placas da borda alternam úmidas
  const grid = [-5, -3, -1, 1, 3, 5];
  for (const x of grid) for (const z of grid) {
    const walk = Math.abs(x) === 1, edge = Math.max(Math.abs(x), Math.abs(z)) === 5;
    const v = (x * 7 + z * 13 + 100) % 3, mat = walk ? 'walk' : edge && (x + z) % 4 === 0 ? 'wet' : ['stone', 'stoneB', 'stoneC'][v];
    add(mat, 'floor', blk(1.95, .3, 1.95, [x, -.15, z], { c: .07, j: .035 }));
  }
  // módulos de borda: lados N/S ao longo de x, L/O ao longo de z; pontes nos vãos das portas (N e S)
  const sides = [
    { id: 'N', rot: 0, at: u => [u, 7] }, { id: 'S', rot: 180, at: u => [-u, -7] },
    { id: 'E', rot: 90, at: u => [7, -u] }, { id: 'W', rot: -90, at: u => [-7, -u] },
  ];
  // rot: girar o local (+z → parede) para a parede do lado. Em xform, rotação Y positiva leva +Z para +X.
  let r = 0;
  for (const s of sides) for (const u of [-5, -3, -1, 1, 3, 5]) {
    const [x, z] = s.at(u), M = { pos: [x, 0, z], rot: [0, s.rot, 0] };
    const door = ROOM.doors.includes(s.id) && Math.abs(u) === 1;
    edgeModule(B.in('Room', M), door ? 'bridge' : 'straight', { r: (r++ % 3) - 1 });
  }
  for (const [x, z, rot] of [[7, 7, 0], [-7, -7, 180], [7, -7, 90], [-7, 7, -90]]) edgeModule(B.in('Room', { pos: [x, 0, z], rot: [0, rot, 0] }), 'corner');
  // soleiras dos vãos (z ∈ [8, 9])
  for (const sz of [-1, 1]) add('walk', 'bridge', blk(3.96, .34, 1.0, [0, -.17, sz * 8.5], { c: .08, j: .02 }));
  // paredes em módulos de 2 m (N/S com vão de 4 m), cantos 1×1 um pouco mais altos, batentes de 2,1 m
  const wallKinds = { N: { '-5': 'niche', '5': 'niche', '-3': 'torch', '3': 'torch' }, S: { '-5': 'torch', '5': 'torch' }, E: { '1': 'niche', '-3': 'torch', '5': 'torch' }, W: { '1': 'niche', '-3': 'torch', '5': 'torch' } };
  for (const s of sides) for (const u of [-7, -5, -3, -1, 1, 3, 5, 7]) {
    if (ROOM.doors.includes(s.id) && Math.abs(u) === 1) continue;
    const [x, z] = s.at(u), base = s.at(0), wx = x + (base[0] === 0 ? 0 : Math.sign(base[0])) * 1, wz = z + (base[1] === 0 ? 0 : Math.sign(base[1])) * 1;
    const kind = wallKinds[s.id][String(u)] || 'plain', tx = s.id === 'N' && Math.abs(u) === 3 ? Math.sign(u) * .45 : 0;   // tocha afastada do batente
    wallModule(B.in('Room', { pos: [wx, 0, wz], rot: [0, s.rot, 0] }), kind, { tx });
    const world = new THREE.Vector3(tx, 0, -.28).applyAxisAngle(new THREE.Vector3(0, 1, 0), s.rot * Math.PI / 180).add(new THREE.Vector3(wx, 1.24, wz));
    if (kind === 'torch') LAYOUT.torches.push({ side: s.id, pos: world.toArray().map(v => +v.toFixed(3)) });
    if (kind === 'niche') LAYOUT.niches.push({ side: s.id, pos: [wx, .7, wz].map(v => +v.toFixed(3)) });
  }
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) {
    add('stoneDark', 'wall', blk(1.0, H + .2 - ROOM.bedY, 1.0, [sx * 8.5, (H + .2 + ROOM.bedY) / 2, sz * 8.5], { c: .1, j: .05 }));
    add('cap', 'wall', blk(1.12, .16, 1.12, [sx * 8.5, H + .26, sz * 8.5], { c: .2, j: .03 }));
    // batentes das portas (lado de dentro do vão, x ∈ [2, 3.1])
    add('stoneDark', 'jamb', blk(1.1, 2.1 - ROOM.bedY, 1.3, [sx * 2.55, (2.1 + ROOM.bedY) / 2, sz * 8.5], { top: .9, c: .1, j: .05 }));
    add('cap', 'jamb', blk(1.24, .16, 1.44, [sx * 2.55, 2.16, sz * 8.5], { c: .2, j: .03 }));
  }
  LAYOUT.doors = ROOM.doors.map(d => ({ side: d, center: [0, d === 'N' ? 8.5 : -8.5], width: ROOM.door }));
  // pilares baixos nos cantos do piso seco
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) { pillar(add, sx * 6, sz * 6); LAYOUT.pillars.push([sx * 6, sz * 6]); }
  // bicas nos cantos do norte (a água entra pelos cantos e corre pelos canais)
  for (const sx of [-1, 1]) { spout(add, sx * 7, 8); LAYOUT.spouts.push([sx * 7, 8]); }
  // musgo contido: topo das paredes, calçadas e cantos; nada no centro
  [[-6.2, .02, 6.6, .4], [5.4, .02, -6.5, .35], [6.6, .02, 3.6, .3], [-6.6, .02, -2.4, .32], [-7.6, 1.48, 4.2, .35], [7.5, 1.48, -5.4, .3],
   [3.8, 1.48, 8.4, .4], [-4.6, 1.48, 8.5, .3], [-8.4, 1.68, -8.4, .3], [8.3, 1.68, 8.5, .28], [-4.6, .01, -5.2, .26], [6.3, .02, 6.3 - 1.2, .22]]
    .forEach(([x, y, z, rr], i) => add('moss', 'decor', patch(x, y, z, rr, .02, i * 1.7 + .3)));
  // poças rasas só nas placas úmidas da borda
  [[-5.7, 4.4, .38], [5.6, -4.2, .35], [5.0, 5.7, .28], [-5.0, -5.6, .32]].forEach(([x, z, rr], i) => add('water', 'puddle', patch(x, .006, z, rr, .006, i * 2.3 + 1)));
  // fendas finas no piso, só nas placas fora da arena (cor de junta, discretas)
  add('joint', 'decor', line([[-5.6, .012, 2.2], [-5.2, .012, 2.6], [-5.4, .012, 3.2]], .012));
  add('joint', 'decor', line([[5.3, .012, -2.9], [5.7, .012, -2.4], [5.5, .012, -1.8]], .012));
  add('joint', 'decor', line([[3.4, .012, 5.6], [3.9, .012, 5.4]], .011));
}

// —— estudo de peças (cada uma na origem; grade de 2 m desenhada pela página) ——
export const KIT = [
  { id: 'Kit_Floor', note: 'placa de piso 2×2 m, variante úmida com poça e musgo' },
  { id: 'Kit_Channel', note: 'módulo de borda reto 2 m: calçada 1 m + meio-fio + canal raso 1 m + parede' },
  { id: 'Kit_Corner', note: 'canto do canal em L com paredes de canto' },
  { id: 'Kit_Crossing', note: 'laje da passarela sobre o canal no vão da porta, batente de 2,1 m' },
  { id: 'Kit_Pillar', note: 'pilar baixo 1,8 × 1,47 × 1,8 m' },
  { id: 'Kit_Niche', note: 'parede 2 m com nicho de runa e calçada' },
];
function kit(B) {
  let add = B.in('Kit_Floor');
  add('joint', 'floorBase', blk(2.1, .16, 2.1, [0, -.2, 0], { c: .02, j: 0 }));
  add('wet', 'floor', blk(1.95, .3, 1.95, [0, -.15, 0], { c: .07, j: .035 }));
  add('water', 'puddle', patch(.35, .006, .3, .4, .006, 1.3)); add('moss', 'decor', patch(-.55, .01, -.5, .3, .02, 4.4));
  add('joint', 'decor', line([[-.2, .012, .6], [.1, .012, .2], [0, .012, -.3]], .012));

  add = B.in('Kit_Channel'); edgeModule(add, 'straight'); wallModule(B.in('Kit_Channel', { pos: [0, 0, 1] }), 'plain');
  add = B.in('Kit_Corner'); edgeModule(add, 'corner');
  wallModule(B.in('Kit_Corner', { pos: [0, 0, 1] }), 'plain'); wallModule(B.in('Kit_Corner', { pos: [1, 0, 0], rot: [0, 90, 0] }), 'plain');
  add('stoneDark', 'wall', blk(1.0, ROOM.wallHeight + .2 - ROOM.bedY, 1.0, [1.5, (ROOM.wallHeight + .2 + ROOM.bedY) / 2, 1.5], { c: .1, j: .05 }));
  add('cap', 'wall', blk(1.12, .16, 1.12, [1.5, ROOM.wallHeight + .26, 1.5], { c: .2, j: .03 }));
  add = B.in('Kit_Crossing'); edgeModule(add, 'bridge');
  add('walk', 'bridge', blk(1.96, .34, 1.0, [0, -.17, 1.5], { c: .08, j: .02 }));
  add('stoneDark', 'jamb', blk(1.1, 2.1 - ROOM.bedY, 1.3, [1.55, (2.1 + ROOM.bedY) / 2, 1.5], { top: .9, c: .1, j: .05 }));
  add('cap', 'jamb', blk(1.24, .16, 1.44, [1.55, 2.16, 1.5], { c: .2, j: .03 }));
  add = B.in('Kit_Pillar'); pillar(add, 0, 0);
  add = B.in('Kit_Niche'); wallModule(B.in('Kit_Niche', { pos: [0, 0, 0] }), 'niche');
  add('stoneB', 'floor', blk(1.96, .3, .9, [0, -.15, -.5], { c: .08, j: .03 }));
}

// ———————————————————————— cena / export ————————————————————————
export function buildScene() {
  SEED = 1; LAYOUT.torches = []; LAYOUT.niches = []; LAYOUT.spouts = []; LAYOUT.pillars = []; LAYOUT.doors = [];
  const B = new Builder(); room(B); kit(B);
  const mats = Object.fromEntries(Object.entries(MATERIALS).map(([k, v]) => {
    const m = new THREE.MeshStandardMaterial({ name: 'crypt_' + k, color: v.color, roughness: v.roughness ?? .9, metalness: v.metalness ?? 0, flatShading: true });
    if (v.emissive) { m.emissive = new THREE.Color(v.emissive); m.emissiveIntensity = v.emissiveIntensity; }
    if (v.opacity != null) { m.transparent = true; m.opacity = v.opacity; m.depthWrite = k !== 'ripple'; }
    return [k, m];
  }));
  // caixas por peça (só da sala) para o validador conferir layout, centro livre e água só nas bordas
  const boxes = B.parts.filter(p => p.group === 'Room').map(p => { p.geo.computeBoundingBox(); const b = p.geo.boundingBox; return { tag: p.tag, mat: p.mat, min: b.min.toArray().map(v => +v.toFixed(3)), max: b.max.toArray().map(v => +v.toFixed(3)) }; });
  const scene = new THREE.Scene(), groups = {};
  const byKey = new Map();
  for (const p of B.parts) { const k = `${p.group}|${p.mat}|${p.tag}`; if (!byKey.has(k)) byKey.set(k, []); byKey.get(k).push(p.geo); }
  let triangles = 0; const perGroup = {};
  for (const [k, geos] of byKey) {
    const [group, mat, tag] = k.split('|');
    if (!groups[group]) { groups[group] = new THREE.Group(); groups[group].name = group; scene.add(groups[group]); }
    const g = finish(BufferGeometryUtils.mergeGeometries(geos.map(g => g.index ? g.toNonIndexed() : g), false));
    const mesh = new THREE.Mesh(g, mats[mat]); mesh.name = `${group}_${tag}_${mat}`; mesh.userData = { tag, mat };
    groups[group].add(mesh);
    const t = g.attributes.position.count / 3; triangles += t; perGroup[group] = (perGroup[group] || 0) + t;
  }
  return { scene, boxes, triangles, perGroup };
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const { scene, boxes, triangles, perGroup } = buildScene();
  mkdirSync(OUT, { recursive: true });
  const glb = Buffer.from(await new GLTFExporter().parseAsync(scene, { binary: true, onlyVisible: false }));
  writeFileSync(join(OUT, 'FloodedCrypt_render.glb'), glb);
  const src = {
    id: 'A15', resource: 'REF-CONCEPT-12', kind: 'fonte de render do conceito 2D de ambiente (não é kit, colisão nem NavMesh)',
    generator: 'Tools/Lemo3DGenerator/src/production/ConceptCrypt.mjs', three: THREE_VERSION,
    frame: 'glTF do gerador: +Y para cima, +Z = norte; sala simétrica em X',
    room: ROOM, palette: Object.fromEntries(Object.entries(MATERIALS).map(([k, v]) => [k, v.color])),
    emissive: Object.fromEntries(Object.entries(MATERIALS).filter(([, v]) => v.emissive).map(([k, v]) => [k, v.emissiveIntensity])),
    layout: LAYOUT, kit: KIT, triangles, trianglesPerGroup: perGroup,
    f14Reference: { room: 'Room_F14_Entry: interior 20 m, coberturas 1,5 × 3 × 3 m em x ±5', note: 'a ficha A15 pede a sala normal 16×16; a cripta não reproduz a sala de primitivas F14 nem define colisão' },
    boxes,
  };
  writeFileSync(join(OUT, 'FloodedCrypt.source.json'), JSON.stringify(src, null, 1));
  console.log(`OK FloodedCrypt (render): ${triangles} tris, ${boxes.length} peças na sala → ${relative(PROJECT, OUT)}`);
}
