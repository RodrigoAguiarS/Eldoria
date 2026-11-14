#!/usr/bin/env node
// ITEMS-COMBAT — modelos de itens e loot (lote de produção; IDs em Assets/_Project/Art/ProductionSpecs/ASSET_MANIFEST.json).
//
// Fonte de autoria editável dos sete modelos do lote. Usa o mesmo vocabulário low-poly do Hero (geo.mjs/parts.mjs:
// primitivas facetadas, normais de face, jitter leve) e a paleta oficial (aço #B9C4CF, latão #E0B040, couro #7A4B2A,
// ciano #4FD8FF). Nada do herói, inimigos ou ambiente é regenerado.
//
//   MODEL-ITEM-IRON     Item_IronSword      espada no espaço do Socket_Hand_R (lâmina +Z, empunhadura na origem)
//   MODEL-ITEM-WARDEN   Item_WardenBlade    idem
//   MODEL-ITEM-ANCIENT  Item_AncientEdge    idem
//   MODEL-ITEM-LEATHER  Item_LeatherVest    chão (estático, pivô na base) + Hero_LeatherVest (skinned no rig do Hero)
//   MODEL-ITEM-HEALTH   Item_HealthPotion   frasco, pivô na base
//   MODEL-LOOT-GOLD     Loot_Gold           pilha de moedas, pivô na base
//   MODEL-LOOT-XP       Loot_XpOrb          orbe facetado com anel, pivô na base
//
// Saídas (pasta ignorada pelo AssetDatabase, revisão antes da integração):
//   <out>/Models/<Nome>.fbx         FBX 7.4 ASCII (malha estática; o colete vestível é skinned com o esqueleto do Hero)
//   <out>/Review/<Nome>.glb         glTF binário para revisão (não é importado pela Unity)
//   <out>/ItemModels.unity.json     submalhas/materiais/pivôs/orçamentos lidos pelo ProductionVisualIntegrator
//   <out>/ItemModels.render.json    geometria por material para o renderizador de ícones (ItemIcons.mjs)
//   <out>/ItemModels.validation.json
//
// Uso (dentro de Tools/Lemo3DGenerator):
//   node src/production/ItemModels.mjs                 gera em Assets/_Project/Art/ProductionStaging/ITEMS-COMBAT/Source~
//   node src/production/ItemModels.mjs --out <pasta>   outra pasta
//   node src/production/ItemModels.mjs --check         só valida
// Lemo-Opuscar é usado SOMENTE LEITURA (three.js r170 e GLTFExporter); nada é escrito lá.
import { mkdirSync, writeFileSync } from 'node:fs';
import { join, resolve, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import { THREE, GLTFExporter, THREE_VERSION, LEMO_ROOT } from '../lemo-three.mjs';
import { ico, bevelBox, convex, lathe, torus, cyl, xform, jitter, extrude, finish, tris } from '../geo.mjs';
import { torsoWeights } from '../parts.mjs';
import { humanoidSkeleton, makeMaterials, buildSkinnedMesh } from '../rig.mjs';
import { toFBX } from '../export-fbx.mjs';
import * as Hero from '../characters/hero.mjs';

const HERE = dirname(fileURLToPath(import.meta.url)), LAB = resolve(HERE, '..', '..'), PROJECT = resolve(LAB, '..', '..');
const argv = process.argv.slice(2), has = k => argv.includes('--' + k), arg = (k, d) => { const i = argv.indexOf('--' + k); return i >= 0 ? argv[i + 1] : d; };
export const STAGING = 'Assets/_Project/Art/ProductionStaging/ITEMS-COMBAT';
const OUT = resolve(arg('out', join(PROJECT, STAGING, 'Source~')));
const D = Math.PI / 180;

// ———————————————————————— paleta (URP Lit: cor, metallic, smoothness, emissão) ————————————————————————
export const PALETTE = {
  steel: { color: '#B9C4CF', metallic: .6, smoothness: .55 },             // = Hero_metal
  steelDark: { color: '#6F7B88', metallic: .6, smoothness: .45 },         // canal/fio sombreado
  brass: { color: '#E0B040', metallic: .7, smoothness: .5 },              // = Hero_brass
  leatherDark: { color: '#4E3020', smoothness: .25 },                     // = Hero_leatherDark
  wardenSteel: { color: '#A9B6C2', metallic: .65, smoothness: .55 },
  wardenGuard: { color: '#4A5563', metallic: .6, smoothness: .4 },
  cyanInset: { color: '#4FD8FF', smoothness: .6, emissive: '#4FD8FF', emissiveIntensity: .55 },
  ancientSteel: { color: '#97A09F', metallic: .5, smoothness: .35 },
  ancientBrass: { color: '#C99A3B', metallic: .7, smoothness: .42 },
  vestLeather: { color: '#9A6234', smoothness: .28 },                     // couro mais claro que a túnica (#7A4B2A) para ler equipado
  vestSeam: { color: '#3A2418', smoothness: .2 },
  glass: { color: '#D3E2EA', metallic: .1, smoothness: .85 },
  ruby: { color: '#C21D3A', smoothness: .7, emissive: '#7A0F22', emissiveIntensity: .35 },
  cork: { color: '#A8794A', smoothness: .2 },
  highlight: { color: '#FFFFFF', smoothness: .9, emissive: '#FFFFFF', emissiveIntensity: .35 },
  gold: { color: '#F2C14E', metallic: .8, smoothness: .6 },
  xp: { color: '#4FD8FF', smoothness: .6, emissive: '#2FB8E8', emissiveIntensity: .9 },
  xpDark: { color: '#1C5F7A', smoothness: .5, emissive: '#0B2A38', emissiveIntensity: .4 },
  xpRing: { color: '#DFFBFF', smoothness: .8, emissive: '#BFF4FF', emissiveIntensity: 1.2 },
};
const M = makeMaterials('Item', PALETTE);

// ———————————————————————— modelos ————————————————————————
// Espadas: espaço local do Socket_Hand_R do Hero (mesma convenção da Hero_Sword): lâmina ao longo de +Z,
// largura em X, espessura em Y, centro do cabo na origem. Assim o visual equipado usa transform identidade no socket.
const blade = (outline, depth, z0, mat, bevel = .005) => ({ geo: extrude(outline, depth, { bevel, o: { rot: [90, 0, 0], pos: [0, 0, z0] } }), mat });

function ironSword() {
  // Espada de Ferro (Comum, +6): lâmina reta e larga, canal escuro, guarda reta de latão, pomo em disco.
  return [
    blade([[-.045, 0], [.045, 0], [.042, .52], [0, .64], [-.042, .52]], .018, .1, M.steel),
    { geo: xform(bevelBox(.026, .022, .46, .3), { pos: [0, 0, .33] }), mat: M.steelDark },                     // canal (fuller)
    { geo: xform(bevelBox(.26, .034, .05, .3), { pos: [0, 0, .085] }), mat: M.brass },                         // guarda reta
    { geo: xform(bevelBox(.05, .044, .06, .3), { pos: [.13, 0, .085] }), mat: M.brass },
    { geo: xform(bevelBox(.05, .044, .06, .3), { pos: [-.13, 0, .085] }), mat: M.brass },
    { geo: cyl(.024, .024, .15, 6, { rot: [90, 0, 0], pos: [0, 0, -.005] }), mat: M.leatherDark },            // cabo
    { geo: torus(.026, .007, 6, 3, { pos: [0, 0, .035] }), mat: M.brass },                                   // anéis do cabo
    { geo: torus(.026, .007, 6, 3, { pos: [0, 0, -.045] }), mat: M.brass },
    { geo: cyl(.042, .042, .026, 8, { rot: [90, 0, 0], pos: [0, 0, -.095] }), mat: M.brass },                 // pomo em disco
  ];
}

function wardenBlade() {
  // Lâmina do Guardião (Raro, +12): lâmina larga com ponta angulada, guarda pesada em asas, um engaste ciano.
  return [
    blade([[-.058, 0], [.058, 0], [.064, .4], [.05, .56], [-.012, .68], [-.06, .5]], .02, .1, M.wardenSteel),
    { geo: xform(bevelBox(.02, .026, .3, .3), { pos: [-.012, 0, .3] }), mat: M.steelDark },                    // canal
    { geo: extrude([[-.17, -.02], [-.07, -.03], [0, .0], [.07, -.03], [.17, -.02], [.2, .06], [.08, .045], [0, .065], [-.08, .045], [-.2, .06]], .05, { bevel: .006, o: { rot: [90, 0, 0], pos: [0, 0, .07] } }), mat: M.wardenGuard }, // guarda angulada
    { geo: xform(convex([[0, .021, .1], [.03, .021, .14], [0, .021, .18], [-.03, .021, .14], [0, -.021, .1], [.03, -.021, .14], [0, -.021, .18], [-.03, -.021, .14]]), { pos: [-.004, 0, .03] }), mat: M.cyanInset }, // engaste ciano (atravessa a lâmina)
    { geo: cyl(.026, .024, .15, 6, { rot: [90, 0, 0], pos: [0, 0, -.005] }), mat: M.leatherDark },
    { geo: cyl(.04, .03, .045, 6, { rot: [90, 0, 0], pos: [0, 0, -.098] }), mat: M.wardenGuard },             // pomo hexagonal
  ];
}

function ancientEdge() {
  // Fio Antigo (Épico, +18): lâmina larga escalonada com desgaste, guarda em degraus e engaste geométrico de latão.
  return [
    { geo: jitter(blade([[-.06, 0], [.06, 0], [.062, .16], [.05, .19], [.054, .46], [0, .66], [-.054, .46], [-.05, .19], [-.062, .16]], .02, .1, M.ancientSteel).geo, .006, 77), mat: M.ancientSteel },
    { geo: xform(convex([[0, .02, .12], [.044, .02, .19], [0, .02, .26], [-.044, .02, .19], [0, -.02, .12], [.044, -.02, .19], [0, -.02, .26], [-.044, -.02, .19]]), {}), mat: M.ancientBrass },
    { geo: xform(convex([[0, .024, .16], [.02, .024, .19], [0, .024, .22], [-.02, .024, .19], [0, -.024, .16], [.02, -.024, .19], [0, -.024, .22], [-.02, -.024, .19]]), {}), mat: M.steelDark }, // losango
    { geo: xform(bevelBox(.012, .026, .2, .3), { pos: [0, 0, .37] }), mat: M.steelDark },                     // canal curto
    { geo: xform(bevelBox(.28, .03, .036, .3), { pos: [0, 0, .088] }), mat: M.ancientBrass },                 // guarda em degraus
    { geo: xform(bevelBox(.2, .04, .036, .3), { pos: [0, 0, .06] }), mat: M.ancientBrass },
    { geo: xform(bevelBox(.11, .048, .03, .3), { pos: [0, 0, .035] }), mat: M.ancientBrass },
    { geo: cyl(.025, .025, .14, 6, { rot: [90, 0, 0], pos: [0, 0, -.04] }), mat: M.leatherDark },
    { geo: torus(.027, .008, 6, 3, { pos: [0, 0, -.06] }), mat: M.ancientBrass },
    { geo: jitter(ico(.04, 0, { pos: [0, 0, -.125], scale: [1, .8, 1.15] }), .004, 9), mat: M.ancientBrass }, // pomo facetado
  ];
}

// Colete: desenhado no espaço de bind do Hero (mesmas proporções P do hero.mjs), por fora da túnica e da placa peitoral.
function vestParts() {
  const tw = torsoWeights(Hero.P);
  const outer = [[.226, .745], [.214, .8], [.226, .88], [.262, .99], [.274, 1.1], [.26, 1.19], [.222, 1.235]];
  const inner = outer.slice().reverse().map(([r, y]) => [r - .022, y]);
  const profile = [...outer, ...inner, outer[0]];
  const shell = jitter(lathe(profile, 10, { sz: .86 }), .005, 12);
  const out = [{ geo: shell, mat: M.vestLeather, weights: tw }];
  // costura frontal (abertura) + fechos de latão
  out.push({ geo: xform(bevelBox(.026, .44, .02, .3), { pos: [0, .99, .238] }), mat: M.vestSeam, weights: tw });
  for (const y of [.94, 1.08]) out.push({ geo: xform(bevelBox(.07, .034, .026, .3), { pos: [0, y, .248] }), mat: M.brass, bone: y > 1 ? 'Chest' : 'Spine' });
  // costuras laterais e barra
  for (const sx of [-1, 1]) out.push({ geo: xform(bevelBox(.018, .4, .05, .3), { pos: [sx * .268, .97, 0], rot: [0, 0, sx * 3] }), mat: M.vestSeam, weights: tw });
  out.push({ geo: torus(.222, .012, 10, 3, { pos: [0, .755, 0], rot: [90, 0, 0], scale: [1, .86, 1] }), mat: M.vestSeam, bone: 'Hips' });
  // alças sobre os ombros (abaixo das ombreiras, fora da gola da capa)
  for (const sx of [-1, 1]) out.push({ geo: jitter(convex([[sx * .12, 1.19, .2], [sx * .2, 1.19, .2], [sx * .12, 1.29, .06], [sx * .2, 1.29, .06], [sx * .12, 1.29, -.08], [sx * .2, 1.29, -.08], [sx * .12, 1.19, -.2], [sx * .2, 1.19, -.2], [sx * .16, 1.315, -.01]]), .004, 30 + sx), mat: M.vestLeather, bone: 'Chest' });
  return out;
}

function healthPotion() {
  // Frasco atarracado: corpo rubi facetado, ombro/pescoço de vidro, gola de couro, rolha e um brilho.
  const body = lathe([[1e-4, 0], [.08, .004], [.106, .045], [.112, .1], [.098, .155], [.07, .18], [1e-4, .182]], 8, { phase: Math.PI / 8 });
  const neck = lathe([[1e-4, .16], [.084, .165], [.064, .195], [.036, .212], [.034, .255], [.046, .262], [.046, .278], [1e-4, .279]], 8, { phase: Math.PI / 8 });
  return [
    { geo: body, mat: M.ruby },
    { geo: neck, mat: M.glass },
    { geo: torus(.04, .011, 8, 3, { pos: [0, .225, 0], rot: [90, 0, 0] }), mat: M.leatherDark },
    { geo: cyl(.032, .028, .05, 6, { pos: [0, .3, 0] }), mat: M.cork },
    { geo: convex([[-.06, .07, .086], [-.05, .13, .08], [-.07, .1, .07], [-.045, .08, .095], [-.055, .125, .09]]), mat: M.highlight },
  ];
}

function goldCoins() {
  // Pilha de moedas (sem inscrições): três empilhadas + duas soltas.
  const coin = (pos, rot = [0, 0, 0]) => ({ geo: cyl(.085, .085, .022, 10, { pos, rot }), mat: M.gold });
  return [
    coin([0, .011, 0], [0, 10, 0]), coin([.012, .033, -.008], [0, 28, 0]), coin([-.006, .055, .01], [0, 3, 0]),
    coin([.12, .092, .06], [0, 30, 72]), coin([-.1, .011, -.07], [0, 15, 0]),
  ];
}

function xpOrb() {
  // Orbe facetado: facetas superiores ciano, inferiores escuras, anel claro inclinado. Pivô na base (y = 0).
  const g = finish(new THREE.IcosahedronGeometry(.1, 1)); g.translate(0, .1, 0);
  const p = g.attributes.position, up = [], down = [];
  for (let i = 0; i < p.count; i += 3) {
    const cy = (p.getY(i) + p.getY(i + 1) + p.getY(i + 2)) / 3, tri = [];
    for (let k = 0; k < 3; k++) tri.push([p.getX(i + k), p.getY(i + k), p.getZ(i + k)]);
    (cy < .06 ? down : up).push(tri);
  }
  const fromTris = list => { const geo = new THREE.BufferGeometry(); geo.setAttribute('position', new THREE.Float32BufferAttribute(list.flat(2), 3)); return finish(geo); };
  return [
    { geo: fromTris(up), mat: M.xp },
    { geo: fromTris(down), mat: M.xpDark },
    { geo: torus(.135, .009, 12, 3, { pos: [0, .1, 0], rot: [90 - 22, 0, 12] }), mat: M.xpRing },
  ];
}

// Pivôs: espadas no cabo (socket); colete de chão/frasco/moedas/orbe na base.
export const MODELS = [
  { id: 'MODEL-ITEM-IRON', name: 'Item_IronSword', kind: 'sword', parts: ironSword, budget: [300, 800], pivot: 'grip (Socket_Hand_R, lâmina +Z)' },
  { id: 'MODEL-ITEM-WARDEN', name: 'Item_WardenBlade', kind: 'sword', parts: wardenBlade, budget: [300, 800], pivot: 'grip (Socket_Hand_R, lâmina +Z)' },
  { id: 'MODEL-ITEM-ANCIENT', name: 'Item_AncientEdge', kind: 'sword', parts: ancientEdge, budget: [300, 800], pivot: 'grip (Socket_Hand_R, lâmina +Z)' },
  { id: 'MODEL-ITEM-LEATHER', name: 'Item_LeatherVest', kind: 'vest', parts: vestParts, budget: [500, 1200], pivot: 'base', groundFrom: 'bind' },
  { id: 'MODEL-ITEM-HEALTH', name: 'Item_HealthPotion', kind: 'potion', parts: healthPotion, budget: [150, 350], pivot: 'base' },
  { id: 'MODEL-LOOT-GOLD', name: 'Loot_Gold', kind: 'gold', parts: goldCoins, budget: [100, 250], pivot: 'base' },
  { id: 'MODEL-LOOT-XP', name: 'Loot_XpOrb', kind: 'xp', parts: xpOrb, budget: [100, 250], pivot: 'base' },
];

// ———————————————————————— montagem ————————————————————————
// Malha estática multi-material (um grupo por material, na ordem da primeira ocorrência).
export function staticMesh(name, parts) {
  const byMat = new Map();
  for (const p of parts) { if (!byMat.has(p.mat)) byMat.set(p.mat, []); byMat.get(p.mat).push(finish(p.geo.clone())); }
  const pos = [], nrm = [], uv = [], groups = [], mats = []; let start = 0;
  for (const [mat, list] of byMat) {
    let count = 0;
    for (const g of list) { const a = g.attributes; for (let i = 0; i < a.position.count; i++) { pos.push(a.position.getX(i), a.position.getY(i), a.position.getZ(i)); nrm.push(a.normal.getX(i), a.normal.getY(i), a.normal.getZ(i)); uv.push(a.uv.getX(i), a.uv.getY(i)); } count += a.position.count; }
    groups.push({ start, count, materialIndex: mats.length }); mats.push(mat); start += count;
  }
  const geo = new THREE.BufferGeometry();
  geo.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); geo.setAttribute('normal', new THREE.Float32BufferAttribute(nrm, 3)); geo.setAttribute('uv', new THREE.Float32BufferAttribute(uv, 2));
  for (const g of groups) geo.addGroup(g.start, g.count, g.materialIndex);
  geo.computeBoundingBox();
  const mesh = new THREE.Mesh(geo, mats); mesh.name = name;
  return mesh;
}

// Colete vestível: SkinnedMesh no esqueleto do Hero (mesmos ossos/bind pose do Hero.fbx: humanoidSkeleton(P)+extraBones).
export function wearableVest() {
  const def = Hero.definition;
  const rig = humanoidSkeleton(Hero.P, def.extraBones || []);
  for (const [n, s] of Object.entries(def.sockets || {})) if (rig.sockets[n] && s.rot) rig.sockets[n].rotation.set(...s.rot.map(v => v * D));
  rig.root.updateMatrixWorld(true);
  const mesh = buildSkinnedMesh('Hero_LeatherVest', vestParts(), rig, M);
  const root = new THREE.Group(); root.name = 'Hero_LeatherVestRig'; root.add(mesh); root.updateMatrixWorld(true);
  return { root, mesh, rig, clips: [] };
}

export function buildAll() {
  const out = [];
  for (const m of MODELS) {
    let parts = m.parts();
    if (m.groundFrom === 'bind') {   // colete de chão = mesma geometria do vestível, base em y = 0 e centrado
      const box = new THREE.Box3(); for (const p of parts) { p.geo.computeBoundingBox(); box.union(p.geo.boundingBox); }
      const c = box.getCenter(new THREE.Vector3());
      parts = parts.map(p => ({ ...p, geo: p.geo.clone().translate(-c.x, -box.min.y, -c.z) }));
    }
    const mesh = staticMesh(m.name, parts);
    out.push({ ...m, mesh });
  }
  return out;
}

// ———————————————————————— FBX estático (mesma convenção do export-fbx.mjs: metros, Y para cima, +Z frente) ————————————————————————
const fnum = v => { if (!Number.isFinite(v)) throw new Error('valor não finito no FBX'); const s = (+v.toFixed(6)).toString(); return s === '-0' ? '0' : s; };
const farr = (name, a, fmt = fnum, ind = '\t\t') => `${ind}${name}: *${a.length} {\n${ind}\ta: ${a.map(fmt).join(',')}\n${ind}} \n`;
export function toStaticFBX(mesh) {
  let nextId = 2000000; const id = () => nextId++;
  const objects = [], conns = [], counts = {};
  const add = (type, text) => { objects.push(text); counts[type] = (counts[type] || 0) + 1; };
  const C = (a, b) => conns.push(`\tC: "OO",${a},${b}`);
  const mid = id();
  add('Model', `\tModel: ${mid}, "Model::${mesh.name}", "Mesh" {\n\t\tVersion: 232\n\t\tProperties70:  {\n\t\t\tP: "RotationActive", "bool", "", "",1\n\t\t\tP: "InheritType", "enum", "", "",1\n\t\t\tP: "DefaultAttributeIndex", "int", "Integer", "",0\n\t\t\tP: "Lcl Translation", "Lcl Translation", "", "A",0,0,0\n\t\t\tP: "Lcl Rotation", "Lcl Rotation", "", "A",0,0,0\n\t\t\tP: "Lcl Scaling", "Lcl Scaling", "", "A",1,1,1\n\t\t}\n\t\tShading: Y\n\t\tCulling: "CullingOff"\n\t}\n`);
  C(mid, 0);
  const g = mesh.geometry, P = g.attributes.position, N = g.attributes.normal, T = g.attributes.uv;
  const key = new Map(), cp = [], pvi = [], nrm = [], uv = [], uvi = [], matIdx = [];
  for (let i = 0; i < P.count; i++) {
    const k = `${P.getX(i).toFixed(5)},${P.getY(i).toFixed(5)},${P.getZ(i).toFixed(5)}`;
    let c = key.get(k); if (c === undefined) { c = cp.length / 3; key.set(k, c); cp.push(P.getX(i), P.getY(i), P.getZ(i)); }
    pvi.push(i % 3 === 2 ? -(c + 1) : c); nrm.push(N.getX(i), N.getY(i), N.getZ(i)); uv.push(T.getX(i), T.getY(i)); uvi.push(i);
  }
  for (const gr of g.groups) for (let t = 0; t < gr.count / 3; t++) matIdx.push(gr.materialIndex);
  const gid = id();
  add('Geometry', `\tGeometry: ${gid}, "Geometry::${mesh.name}", "Mesh" {\n` + farr('Vertices', cp) + farr('PolygonVertexIndex', pvi, String) +
    `\t\tGeometryVersion: 124\n\t\tLayerElementNormal: 0 {\n\t\t\tVersion: 101\n\t\t\tName: ""\n\t\t\tMappingInformationType: "ByPolygonVertex"\n\t\t\tReferenceInformationType: "Direct"\n` + farr('Normals', nrm, fnum, '\t\t\t') + `\t\t}\n` +
    `\t\tLayerElementUV: 0 {\n\t\t\tVersion: 101\n\t\t\tName: "UVMap"\n\t\t\tMappingInformationType: "ByPolygonVertex"\n\t\t\tReferenceInformationType: "IndexToDirect"\n` + farr('UV', uv, fnum, '\t\t\t') + farr('UVIndex', uvi, String, '\t\t\t') + `\t\t}\n` +
    `\t\tLayerElementMaterial: 0 {\n\t\t\tVersion: 101\n\t\t\tName: ""\n\t\t\tMappingInformationType: "ByPolygon"\n\t\t\tReferenceInformationType: "IndexToDirect"\n` + farr('Materials', matIdx, String, '\t\t\t') + `\t\t}\n` +
    `\t\tLayer: 0 {\n\t\t\tVersion: 100\n\t\t\tLayerElement:  {\n\t\t\t\tType: "LayerElementNormal"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t\tLayerElement:  {\n\t\t\t\tType: "LayerElementMaterial"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t\tLayerElement:  {\n\t\t\t\tType: "LayerElementUV"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t}\n\t}\n`);
  C(gid, mid);
  for (const m of mesh.material) {
    const matId = id(), c = m.color, e = m.emissive, ud = m.userData || {};
    add('Material', `\tMaterial: ${matId}, "Material::${m.name}", "" {\n\t\tVersion: 102\n\t\tShadingModel: "phong"\n\t\tMultiLayer: 0\n\t\tProperties70:  {\n` +
      `\t\t\tP: "DiffuseColor", "Color", "", "A",${fnum(c.r)},${fnum(c.g)},${fnum(c.b)}\n\t\t\tP: "DiffuseFactor", "Number", "", "A",1\n` +
      `\t\t\tP: "EmissiveColor", "Color", "", "A",${fnum(e.r)},${fnum(e.g)},${fnum(e.b)}\n\t\t\tP: "EmissiveFactor", "Number", "", "A",${fnum(m.emissiveIntensity || 0)}\n` +
      `\t\t\tP: "Shininess", "Number", "", "A",${fnum((ud.smoothness ?? .2) * 100)}\n\t\t\tP: "ReflectionFactor", "Number", "", "A",${fnum(ud.metallic ?? 0)}\n\t\t}\n\t}\n`);
    C(matId, mid);
  }
  const total = Object.values(counts).reduce((a, b) => a + b, 0) + 1;
  const header = `; FBX 7.4.0 project file\n; Gerado por Tools/Lemo3DGenerator/src/production/ItemModels.mjs (three.js do Lemo-Opuscar)\n; ----------------------------------------------------\n\n` +
    `FBXHeaderExtension:  {\n\tFBXHeaderVersion: 1003\n\tFBXVersion: 7400\n\tCreator: "Lemo3DGenerator ItemModels"\n}\n` +
    `GlobalSettings:  {\n\tVersion: 1000\n\tProperties70:  {\n\t\tP: "UpAxis", "int", "Integer", "",1\n\t\tP: "UpAxisSign", "int", "Integer", "",1\n\t\tP: "FrontAxis", "int", "Integer", "",2\n\t\tP: "FrontAxisSign", "int", "Integer", "",1\n\t\tP: "CoordAxis", "int", "Integer", "",0\n\t\tP: "CoordAxisSign", "int", "Integer", "",1\n\t\tP: "OriginalUpAxis", "int", "Integer", "",1\n\t\tP: "OriginalUpAxisSign", "int", "Integer", "",1\n\t\tP: "UnitScaleFactor", "double", "Number", "",100\n\t\tP: "OriginalUnitScaleFactor", "double", "Number", "",100\n\t}\n}\n\n` +
    `Documents:  {\n\tCount: 1\n\tDocument: ${id()}, "Scene", "Scene" {\n\t\tProperties70:  {\n\t\t\tP: "SourceObject", "object", "", ""\n\t\t}\n\t\tRootNode: 0\n\t}\n}\n\nReferences:  {\n}\n\n` +
    `Definitions:  {\n\tVersion: 100\n\tCount: ${total}\n\tObjectType: "GlobalSettings" {\n\t\tCount: 1\n\t}\n` + Object.entries(counts).map(([k, n]) => `\tObjectType: "${k}" {\n\t\tCount: ${n}\n\t}\n`).join('') + `}\n\n`;
  return header + `Objects:  {\n${objects.join('')}}\n\nConnections:  {\n${conns.join('\n')}\n}\n`;
}

async function toGLB(object) {
  const ab = await new GLTFExporter().parseAsync(object, { binary: true, onlyVisible: false });
  return Buffer.from(ab);
}

// Geometria por material (posições/normais não indexadas) para o renderizador de ícones.
export function renderData(mesh) {
  const g = mesh.geometry, P = g.attributes.position.array, N = g.attributes.normal.array;
  return g.groups.map(gr => {
    const m = mesh.material[gr.materialIndex];
    return { material: m.name, color: '#' + m.color.getHexString(), emissive: '#' + m.emissive.getHexString(), emissiveIntensity: m.emissiveIntensity || 0,
      metallic: m.metalness, roughness: m.roughness,
      positions: Array.from(P.slice(gr.start * 3, (gr.start + gr.count) * 3), v => +v.toFixed(5)), normals: Array.from(N.slice(gr.start * 3, (gr.start + gr.count) * 3), v => +v.toFixed(4)) };
  });
}

function bounds(mesh) { mesh.geometry.computeBoundingBox(); const b = mesh.geometry.boundingBox; return { min: b.min.toArray().map(v => +v.toFixed(4)), max: b.max.toArray().map(v => +v.toFixed(4)), size: b.getSize(new THREE.Vector3()).toArray().map(v => +v.toFixed(4)) }; }

export function validate(models, vest) {
  const issues = [], report = [];
  for (const m of models) {
    const t = tris(m.mesh.geometry), b = bounds(m.mesh), mats = m.mesh.material.map(x => x.name);
    const inBudget = t >= m.budget[0] * .5 && t <= m.budget[1];
    if (!inBudget) issues.push(`${m.name}: ${t} tris fora do orçamento ${m.budget.join('–')}`);
    const p = m.mesh.geometry.attributes.position.array; for (const v of p) if (!Number.isFinite(v)) { issues.push(`${m.name}: vértice não finito`); break; }
    if (m.pivot === 'base' && Math.abs(b.min[1]) > .002) issues.push(`${m.name}: pivô não está na base (minY ${b.min[1]})`);
    if (m.kind === 'sword' && (b.min[2] > -.08 || b.max[2] < .6)) issues.push(`${m.name}: espada fora da convenção do socket`);
    report.push({ id: m.id, name: m.name, triangles: t, budget: m.budget, materials: mats, bounds: b, pivot: m.pivot });
  }
  const vt = tris(vest.mesh.geometry), si = vest.mesh.geometry.attributes.skinIndex, sw = vest.mesh.geometry.attributes.skinWeight;
  let maxErr = 0; for (let i = 0; i < sw.count; i++) maxErr = Math.max(maxErr, Math.abs(sw.getX(i) + sw.getY(i) + sw.getZ(i) + sw.getW(i) - 1));
  if (maxErr > 1e-3) issues.push(`Hero_LeatherVest: pesos não normalizados (${maxErr})`);
  const used = new Set(); for (let i = 0; i < si.count; i++) [[si.getX(i), sw.getX(i)], [si.getY(i), sw.getY(i)], [si.getZ(i), sw.getZ(i)], [si.getW(i), sw.getW(i)]].forEach(([c, w]) => { if (w > 0) used.add(vest.rig.list[c].name); });
  report.push({ id: 'MODEL-ITEM-LEATHER', name: 'Hero_LeatherVest', variant: 'wearable', triangles: vt, bones: vest.rig.list.map(b => b.name), weightedBones: [...used].filter(n => n), bounds: bounds(vest.mesh) });
  return { ok: issues.length === 0, issues, report };
}

// ———————————————————————— principal ————————————————————————
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const models = buildAll(), vest = wearableVest();
  const val = validate(models, vest);
  for (const r of val.report) console.log(`${r.name.padEnd(20)} ${String(r.triangles).padStart(5)} tris  ${r.materials ? r.materials.length + ' mats' : r.weightedBones.join(',')}  size ${JSON.stringify(r.bounds.size)}`);
  if (!val.ok) { console.error('FALHOU:\n  ' + val.issues.join('\n  ')); process.exit(1); }
  if (has('check')) { console.log('check OK'); process.exit(0); }
  mkdirSync(join(OUT, 'Models'), { recursive: true }); mkdirSync(join(OUT, 'Review'), { recursive: true });
  const unity = { batch: 'ITEMS-COMBAT', generator: 'Tools/Lemo3DGenerator/src/production/ItemModels.mjs', three: THREE_VERSION, lemoRoot: LEMO_ROOT,
    units: 'metros, Y para cima, +Z frente (convenção do export-fbx.mjs)', materials: {}, models: [] };
  for (const [key, m] of Object.entries(PALETTE)) unity.materials[`Item_${key}`] = { color: m.color, metallic: m.metallic ?? 0, smoothness: m.smoothness ?? .2, emissive: m.emissive ?? '#000000', emissiveIntensity: m.emissiveIntensity ?? 0 };
  const render = {};
  for (const m of models) {
    writeFileSync(join(OUT, 'Models', `${m.name}.fbx`), toStaticFBX(m.mesh));
    writeFileSync(join(OUT, 'Review', `${m.name}.glb`), await toGLB(m.mesh));
    unity.models.push({ id: m.id, name: m.name, kind: m.kind, fbx: `Models/${m.name}.fbx`, submeshMaterials: m.mesh.material.map(x => x.name), triangles: tris(m.mesh.geometry), pivot: m.pivot, bounds: bounds(m.mesh) });
    render[m.name] = { kind: m.kind, groups: renderData(m.mesh) };
  }
  writeFileSync(join(OUT, 'Models', 'Hero_LeatherVest.fbx'), toFBX(vest));
  writeFileSync(join(OUT, 'Review', 'Hero_LeatherVest.glb'), await toGLB(vest.root));
  unity.models.push({ id: 'MODEL-ITEM-LEATHER', name: 'Hero_LeatherVest', kind: 'wearable', fbx: 'Models/Hero_LeatherVest.fbx', skinned: true, skeleton: 'Hero (humanoidSkeleton(P) + Cape_1/Cape_2, sockets)', submeshMaterials: vest.mesh.material.map(x => x.name), triangles: tris(vest.mesh.geometry), bounds: bounds(vest.mesh) });
  writeFileSync(join(OUT, 'ItemModels.unity.json'), JSON.stringify(unity, null, 2));
  writeFileSync(join(OUT, 'ItemModels.render.json'), JSON.stringify(render));
  writeFileSync(join(OUT, 'ItemModels.validation.json'), JSON.stringify({ ...val, date: new Date().toISOString(), node: process.version }, null, 2));
  console.log('OK →', relative(PROJECT, OUT));
}
