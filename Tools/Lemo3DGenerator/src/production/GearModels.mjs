#!/usr/bin/env node
// ITEMS-GEAR — modelos de chão das quatro peças de D-006/D-007 (IDs em Assets/_Project/Art/ProductionSpecs/ASSET_MANIFEST.json).
//
// Fonte de autoria editável. Reutiliza o vocabulário e a montagem de ItemModels.mjs (lote ITEMS-COMBAT): primitivas
// facetadas de geo.mjs, a MESMA paleta (aço #B9C4CF, latão #E0B040, couros do colete/espadas) e os mesmos materiais
// Item_* já importados; só o cobre do amuleto é novo. Nada das armas, do colete, do herói ou do ambiente é regenerado.
//
//   MODEL-ITEM-HELMET   Item_IronHelmet     Elmo de ferro: cúpula facetada, aro de latão, nasal, jugulares, crista
//   MODEL-ITEM-GLOVES   Item_LeatherGloves  Luvas de couro (o PAR é uma unidade de equipamento), reforço nos nós
//   MODEL-ITEM-BOOTS    Item_LeatherBoots   Botas de couro (o PAR é uma unidade), cano alto, sola escura, fivela
//   MODEL-ITEM-AMULET   Item_CopperAmulet   Amuleto de cobre: disco gravado, centro de latão, argola e cordão de couro
//
// Todos com pivô na base (y = 0), metros, Y para cima, +Z frente (convenção do export-fbx.mjs). Sem rig: o visual
// equipado no Hero NÃO faz parte deste lote (o PlayerEquipmentVisualPresenter só cobre arma e armadura).
//
// Saídas (pasta ignorada pelo AssetDatabase, revisão antes da integração):
//   <out>/Models/<Nome>.fbx        FBX 7.4 ASCII estático (toStaticFBX de ItemModels.mjs)
//   <out>/Review/<Nome>.glb        glTF binário para revisão (não é importado pela Unity)
//   <out>/GearModels.unity.json    submalhas/materiais/pivôs/orçamentos lidos pelo GearVisualIntegrator
//   <out>/GearModels.render.json   geometria por material para o renderizador de ícones (GearIcons.mjs)
//   <out>/GearModels.validation.json
//
// Uso (dentro de Tools/Lemo3DGenerator):
//   node src/production/GearModels.mjs                 gera em Assets/_Project/Art/ProductionStaging/ITEMS-GEAR/Source~
//   node src/production/GearModels.mjs --out <pasta>   outra pasta
//   node src/production/GearModels.mjs --check         só valida
// Lemo-Opuscar é usado SOMENTE LEITURA (three.js r170 e GLTFExporter); nada é escrito lá.
import { mkdirSync, writeFileSync } from 'node:fs';
import { join, resolve, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import { THREE, GLTFExporter, THREE_VERSION, LEMO_ROOT } from '../lemo-three.mjs';
import { ico, bevelBox, lathe, torus, cyl, xform, jitter, convex, tris } from '../geo.mjs';
import { makeMaterials } from '../rig.mjs';
import { PALETTE as ITEM_PALETTE, staticMesh, toStaticFBX, renderData } from './ItemModels.mjs';

const HERE = dirname(fileURLToPath(import.meta.url)), LAB = resolve(HERE, '..', '..'), PROJECT = resolve(LAB, '..', '..');
const argv = process.argv.slice(2), has = k => argv.includes('--' + k), arg = (k, d) => { const i = argv.indexOf('--' + k); return i >= 0 ? argv[i + 1] : d; };
export const STAGING = 'Assets/_Project/Art/ProductionStaging/ITEMS-GEAR';
const OUT = resolve(arg('out', join(PROJECT, STAGING, 'Source~')));

// ———————————————————————— paleta ————————————————————————
// Chaves reutilizadas têm EXATAMENTE os valores do lote ITEMS-COMBAT (mesmos materiais Item_* na Unity; o integrador
// não os sobrescreve). Novas: cobre e cobre escuro (gravação), coerentes com o latão #E0B040 e o couro #7A4B2A.
const REUSED = ['steel', 'steelDark', 'brass', 'leatherDark', 'vestLeather', 'vestSeam'];
export const PALETTE = {
  ...Object.fromEntries(REUSED.map(k => [k, ITEM_PALETTE[k]])),
  copper: { color: '#C27A45', metallic: .7, smoothness: .46 },
  copperDark: { color: '#6E3F22', metallic: .55, smoothness: .32 },
};
export const NEW_MATERIALS = ['copper', 'copperDark'];
const M = makeMaterials('Item', PALETTE);

// ———————————————————————— modelos ————————————————————————
const box = (w, h, d, o, mat, c = .3) => ({ geo: xform(bevelBox(w, h, d, c), o), mat });

function ironHelmet() {
  // Elmo de ferro (Capacete, Comum): cúpula facetada de aço levemente alongada, aro de latão com rebites, faixa de reforço,
  // nasal e jugulares; forro de couro visível na borda. Proporção da cabeça do Hero (~0,34 × 0,30 × 0,38 m).
  const RIM = .075, SZ = 1.12;
  const outer = [[1e-4, .335], [.07, .328], [.12, .305], [.152, .262], [.168, .2], [.172, RIM]];
  const inner = outer.slice().reverse().map(([r, y]) => [Math.max(1e-4, r - .016), y + (y === RIM ? 0 : -.01)]);
  const shell = jitter(xform(lathe([...outer, ...inner, outer[0]], 10, { sz: SZ, phase: Math.PI / 10 }), {}), .003, 41);
  const parts = [
    { geo: shell, mat: M.steel },
    { geo: torus(.174, .016, 10, 3, { pos: [0, RIM + .004, 0], rot: [90, 0, 0], scale: [1, SZ, 1] }), mat: M.brass },        // aro
    { geo: torus(.156, .008, 10, 3, { pos: [0, RIM + .012, 0], rot: [90, 0, 0], scale: [1, SZ, 1] }), mat: M.leatherDark },  // forro
    { geo: torus(.166, .01, 10, 3, { pos: [0, .2, 0], rot: [90, 0, 0], scale: [1, SZ, 1] }), mat: M.steelDark },           // faixa de reforço
    { geo: cyl(.03, .036, .018, 6, { pos: [0, .336, 0] }), mat: M.steelDark },                                              // botão do topo
    box(.036, .1, .02, { pos: [0, .045, .196], rot: [-8, 0, 0] }, M.steelDark),                                              // nasal curto
  ];
  // jugulares: placas trapezoidais rentes ao aro (não descem como hastes)
  for (const sx of [-1, 1]) parts.push({ geo: convex([[0, .085, .1], [0, .085, -.02], [0, .03, .07], [0, .03, .0], [-sx * .018, .085, .1], [-sx * .018, .085, -.02], [-sx * .014, .03, .07], [-sx * .014, .03, .0]]
    .map(([x, y, z]) => [x + sx * .175, y, z])), mat: M.steel });
  for (let i = 0; i < 6; i++) {                                                                                             // rebites do aro
    const a = (i + .5) / 6 * Math.PI * 2;
    parts.push({ geo: ico(.012, 0, { pos: [Math.sin(a) * .19, RIM + .006, Math.cos(a) * .19 * SZ] }), mat: M.brass });
  }
  return parts;
}

function glove(side) {
  // Uma luva deitada (palma para baixo, dedos +Z), polegar do lado `side` (+1 direita, -1 esquerda); base em y = 0.
  const parts = [
    { geo: cyl(.05, .046, .075, 7, { rot: [90, 0, 0], pos: [0, .048, -.085] }), mat: M.vestLeather },                        // punho
    { geo: torus(.05, .008, 7, 3, { pos: [0, .048, -.124] }), mat: M.vestSeam },                                            // barra do punho
    box(.1, .05, .1, { pos: [0, .033, .0] }, M.vestLeather, .35),                                                           // dorso
    box(.09, .016, .03, { pos: [0, .062, .038] }, M.leatherDark),                                                           // reforço dos nós
  ];
  // dedos encaixados no dorso (começam dentro dele), levemente curvados para baixo
  const fingers = [[-.034, .07], [-.012, .08], [.012, .078], [.034, .064]];
  for (const [x, len] of fingers) parts.push(box(.021, .028, len, { pos: [x, .026, .03 + len / 2], rot: [6, 0, 0] }, M.vestLeather, .4));
  for (const [x] of fingers.slice(1, 3)) parts.push({ geo: ico(.0085, 0, { pos: [x, .072, .04] }), mat: M.brass });         // tachas de latão
  parts.push(box(.026, .03, .07, { pos: [side * .055, .024, .025], rot: [0, side * -34, 0] }, M.vestLeather, .4));         // polegar (sai do dorso)
  return parts;
}
const place = (parts, o) => parts.map(p => ({ ...p, geo: xform(p.geo, o) }));
function leatherGloves() {
  // O par de luvas é UMA unidade de equipamento; as duas peças existem só na apresentação.
  return [
    ...place(glove(-1), { pos: [-.072, 0, -.01], rot: [0, 14, 0] }),
    ...place(glove(1), { pos: [.072, 0, .014], rot: [0, -10, 0] }),
  ];
}

function boot() {
  // Uma bota em pé (bico +Z): sola e salto escuros, gáspea de couro, cano alto com dobra, tira com fivela de latão.
  return [
    box(.108, .024, .25, { pos: [0, .012, .03] }, M.leatherDark),                                                           // sola
    box(.09, .036, .065, { pos: [0, .03, -.072] }, M.leatherDark),                                                          // salto
    box(.098, .07, .2, { pos: [0, .06, .045] }, M.vestLeather, .42),                                                        // gáspea
    box(.088, .045, .07, { pos: [0, .052, .13] }, M.vestLeather, .5),                                                       // bico
    { geo: cyl(.06, .054, .2, 8, { pos: [0, .19, -.03], phase: Math.PI / 8 }), mat: M.vestLeather },                         // cano
    { geo: cyl(.069, .066, .042, 8, { pos: [0, .296, -.03], phase: Math.PI / 8 }), mat: M.leatherDark },                     // dobra do cano
    { geo: torus(.059, .009, 8, 3, { pos: [0, .14, -.03], rot: [90, 0, 0] }), mat: M.vestSeam },                            // tira
    box(.034, .03, .012, { pos: [0, .14, .03] }, M.brass, .25),                                                             // fivela
  ];
}
function leatherBoots() {
  // O par de botas é UMA unidade de equipamento.
  return [
    ...place(boot(), { pos: [-.074, 0, -.012], rot: [0, -7, 0] }),
    ...place(boot(), { pos: [.074, 0, .018], rot: [0, 5, 0] }),
  ];
}

function copperAmulet() {
  // Amuleto de cobre (Acessório): medalhão pendurado (disco virado para +Z) com anel gravado, quatro marcas, centro de
  // latão facetado, argola e cordão de couro. Silhueta circular, distinta de elmo/luvas/botas mesmo a 32 px.
  const C = .085, parts = [
    { geo: cyl(.078, .078, .02, 10, { rot: [90, 0, 0], pos: [0, C, 0], phase: Math.PI / 10 }), mat: M.copper },            // disco
    { geo: torus(.058, .007, 10, 3, { pos: [0, C, .011] }), mat: M.copperDark },                                           // anel gravado (frente)
    { geo: torus(.058, .007, 10, 3, { pos: [0, C, -.011] }), mat: M.copperDark },                                          // (verso)
    { geo: jitter(ico(.024, 0, { pos: [0, C, .012], scale: [1, 1, .6] }), .002, 5), mat: M.brass },                          // centro
    { geo: torus(.016, .0055, 6, 3, { pos: [0, C + .092, 0], rot: [0, 90, 0] }), mat: M.brass },                             // argola
    { geo: torus(.095, .0065, 14, 3, { pos: [0, C + .2, 0] }), mat: M.leatherDark },                                       // cordão
  ];
  for (let i = 0; i < 4; i++) {                                                                                             // marcas da gravação
    const a = i / 4 * Math.PI * 2 + Math.PI / 4;
    parts.push({ geo: convex([[-.006, -.006, 0], [.006, -.006, 0], [0, .01, 0], [-.006, -.006, .006], [.006, -.006, .006], [0, .01, .006]].map(([x, y, z]) => {
      const r = .036, c = Math.cos(a), s = Math.sin(a);
      return [Math.cos(a - Math.PI / 2) * x - Math.sin(a - Math.PI / 2) * y + c * r, Math.sin(a - Math.PI / 2) * x + Math.cos(a - Math.PI / 2) * y + s * r + C, z + .009];
    })), mat: M.copperDark });
  }
  return parts;
}

export const MODELS = [
  { id: 'MODEL-ITEM-HELMET', name: 'Item_IronHelmet', kind: 'helmet', parts: ironHelmet, budget: [300, 900], pivot: 'base', item: 'Gear_IronHelmet' },
  { id: 'MODEL-ITEM-GLOVES', name: 'Item_LeatherGloves', kind: 'gloves', parts: leatherGloves, budget: [300, 900], pivot: 'base', item: 'Gear_LeatherGloves', pair: true },
  { id: 'MODEL-ITEM-BOOTS', name: 'Item_LeatherBoots', kind: 'boots', parts: leatherBoots, budget: [300, 900], pivot: 'base', item: 'Gear_LeatherBoots', pair: true },
  { id: 'MODEL-ITEM-AMULET', name: 'Item_CopperAmulet', kind: 'amulet', parts: copperAmulet, budget: [150, 500], pivot: 'base', item: 'Gear_CopperAmulet' },
];

// Pivô na base: assenta o menor y em 0 e centraliza X/Z, sem mudar proporções.
export function buildAll() {
  return MODELS.map(m => {
    const parts = m.parts(), b = new THREE.Box3();
    for (const p of parts) { p.geo.computeBoundingBox(); b.union(p.geo.boundingBox); }
    const c = b.getCenter(new THREE.Vector3());
    const mesh = staticMesh(m.name, parts.map(p => ({ ...p, geo: p.geo.clone().translate(-c.x, -b.min.y, -c.z) })));
    return { ...m, mesh };
  });
}

function bounds(mesh) { mesh.geometry.computeBoundingBox(); const b = mesh.geometry.boundingBox; return { min: b.min.toArray().map(v => +v.toFixed(4)), max: b.max.toArray().map(v => +v.toFixed(4)), size: b.getSize(new THREE.Vector3()).toArray().map(v => +v.toFixed(4)) }; }

export function validate(models) {
  const issues = [], report = [];
  for (const m of models) {
    const t = tris(m.mesh.geometry), b = bounds(m.mesh), mats = m.mesh.material.map(x => x.name);
    if (t < m.budget[0] * .5 || t > m.budget[1]) issues.push(`${m.name}: ${t} tris fora do orçamento ${m.budget.join('–')}`);
    for (const v of m.mesh.geometry.attributes.position.array) if (!Number.isFinite(v)) { issues.push(`${m.name}: vértice não finito`); break; }
    if (Math.abs(b.min[1]) > .002) issues.push(`${m.name}: pivô não está na base (minY ${b.min[1]})`);
    if (Math.abs(b.min[0] + b.max[0]) > .004 || Math.abs(b.min[2] + b.max[2]) > .004) issues.push(`${m.name}: não centrado em X/Z`);
    if (Math.max(...b.size) > .5 || Math.min(...b.size) < .015) issues.push(`${m.name}: dimensões fora da escala do Hero ${JSON.stringify(b.size)}`);
    if (mats.length > 6) issues.push(`${m.name}: materiais demais (${mats.length})`);
    report.push({ id: m.id, name: m.name, item: m.item, triangles: t, budget: m.budget, materials: mats, bounds: b, pivot: m.pivot, pairIsOneUnit: !!m.pair });
  }
  return { ok: issues.length === 0, issues, report };
}

async function toGLB(object) { return Buffer.from(await new GLTFExporter().parseAsync(object, { binary: true, onlyVisible: false })); }

// ———————————————————————— principal ————————————————————————
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const models = buildAll(), val = validate(models);
  for (const r of val.report) console.log(`${r.name.padEnd(20)} ${String(r.triangles).padStart(5)} tris  ${r.materials.length} mats  size ${JSON.stringify(r.bounds.size)}`);
  if (!val.ok) { console.error('FALHOU:\n  ' + val.issues.join('\n  ')); process.exit(1); }
  if (has('check')) { console.log('check OK'); process.exit(0); }
  mkdirSync(join(OUT, 'Models'), { recursive: true }); mkdirSync(join(OUT, 'Review'), { recursive: true });
  const unity = { batch: 'ITEMS-GEAR', generator: 'Tools/Lemo3DGenerator/src/production/GearModels.mjs', three: THREE_VERSION, lemoRoot: LEMO_ROOT,
    units: 'metros, Y para cima, +Z frente (convenção do export-fbx.mjs)', reusedMaterials: REUSED.map(k => `Item_${k}`), materials: {}, models: [] };
  for (const [key, m] of Object.entries(PALETTE)) unity.materials[`Item_${key}`] = { color: m.color, metallic: m.metallic ?? 0, smoothness: m.smoothness ?? .2, emissive: m.emissive ?? '#000000', emissiveIntensity: m.emissiveIntensity ?? 0 };
  const render = {};
  for (const m of models) {
    writeFileSync(join(OUT, 'Models', `${m.name}.fbx`), toStaticFBX(m.mesh).replace('ItemModels.mjs', 'GearModels.mjs').replace('Lemo3DGenerator ItemModels', 'Lemo3DGenerator GearModels'));
    writeFileSync(join(OUT, 'Review', `${m.name}.glb`), await toGLB(m.mesh));
    unity.models.push({ id: m.id, name: m.name, kind: m.kind, item: m.item, fbx: `Models/${m.name}.fbx`, submeshMaterials: m.mesh.material.map(x => x.name), triangles: tris(m.mesh.geometry), pivot: m.pivot, pairIsOneUnit: !!m.pair, bounds: bounds(m.mesh) });
    render[m.name] = { kind: m.kind, groups: renderData(m.mesh) };
  }
  writeFileSync(join(OUT, 'GearModels.unity.json'), JSON.stringify(unity, null, 2));
  writeFileSync(join(OUT, 'GearModels.render.json'), JSON.stringify(render));
  writeFileSync(join(OUT, 'GearModels.validation.json'), JSON.stringify({ ...val, date: new Date().toISOString(), node: process.version }, null, 2));
  console.log('OK →', relative(PROJECT, OUT));
}
