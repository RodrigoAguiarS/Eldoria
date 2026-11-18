#!/usr/bin/env node
// A08 — FX-PROJECTILE: núcleo visual do projétil hostil (polimento; ID em Assets/_Project/Art/ProductionSpecs/ASSET_MANIFEST.json).
//
// Fonte de autoria editável do modelo. Mesmo vocabulário low-poly do Lemo3DGenerator (geo.mjs: hulls convexos facetados,
// normais de face, jitter determinístico) e a mesma convenção do export-fbx.mjs / ItemModels.mjs: metros, +Y para cima,
// +Z frente, pivô no centro. Substitui SÓ o visual da esfera laranja de Projectile.prefab numa integração futura;
// colisão (SphereCast r = 0,25), velocidade, dano, lifetime e pool não são tocados (nenhum arquivo de domínio é lido/escrito).
//
// Forma: um núcleo de brasa angular (icosaedro d0) dentro de uma casca de ferro escuro partida em quatro placas por duas
// fendas: a fenda principal corre ao longo da trajetória (plano x = 0, contém +Z e +Y; 10 cm) e uma fenda menor cruza a
// frente (plano inclinado em n·p = 0,05; 6 cm), deixando as placas da frente menores (lê a direção). A brasa aparece no
// fundo das fendas; as paredes das fendas recebem laranja-ferrugem com emissão média. Nenhuma ponta sai da esfera de 0,2 m.
// Largura das fendas: a ficha pede uma "pequena fenda", mas com 1,9 cm de meia-largura o ferro #3E4152 some no piso
// (#3E4152/#55596B; contraste médio ~1,0 vs. 3,2 da esfera atual). A largura atual é o mínimo medido que mantém a
// silhueta quente em ≥ 37% de todos os rumos; ver Source~/README.md e FxProjectile.validation.json.
//
// Um material (URP/Lit): as faces apontam para o centro de uma célula de uma paleta 16×16 (BaseMap) e de um mapa de
// emissão 16×16 (EmissionMap). Filtro point, sem mipmaps, sem compressão → cor exata por face.
//
// Saídas:
//   Assets/_Project/Art/ProductionStaging/FX-PROJECTILE/
//     FX-PROJECTILE-Core.fbx (+ .meta)                FBX 7.4 ASCII estático, 1 material, texturas referenciadas
//     FX-PROJECTILE-Core_Base.png / _Emission.png (+ .meta)   paleta 16×16 RGB
//   …/Source~/
//     FX-PROJECTILE-Core.glb                           revisão (cores por vértice; não importado pela Unity)
//     FX-PROJECTILE.source.json                        parâmetros, paleta, material URP proposto, integração proposta
//     FX-PROJECTILE.render.json                        triângulos com cor/emissão por face (para o preview.py)
//     FxProjectile.generator.json                      validação interna (geometria em memória)
//
// Uso (dentro de Tools/Lemo3DGenerator):
//   node src/production/FxProjectileCore.mjs            gera
//   node src/production/FxProjectileCore.mjs --check    só valida
// Depois: python -I Tools/Lemo3DGenerator/src/production/FxProjectileCore_preview.py  (relê o FBX do disco, valida, previews)
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
export const ID = 'FX-PROJECTILE';
export const STAGING = 'Assets/_Project/Art/ProductionStaging/FX-PROJECTILE';
const OUT = join(PROJECT, STAGING), SRC = join(OUT, 'Source~');
export const NAME = 'FX-PROJECTILE-Core';
const FBX_TEMPLATE = join(PROJECT, 'Assets/_Project/Art/Items/Models/Loot_XpOrb.fbx.meta');        // ModelImporter real (Unity 6000.6)
const TEX_TEMPLATE = join(PROJECT, 'Tools/FxHealGenerator/texture.meta.template');                 // cópia de FX_Spark.png.meta

// ———————————————————————— parâmetros editáveis ————————————————————————
export const SHAPE = {
  envelopeRadius: 0.2,          // = esfera atual (Unity Sphere × 0,4); nada pode passar daqui
  shellRadius: [0.19, 0.2],    // raio das amostras externas da casca (jitter determinístico dentro do envelope)
  shellDirections: 1,           // icosfera de direções (detail 1 → 42 direções)
  rimRadius: 0.194,             // raio dos pontos de borda das fendas
  rimStepDeg: 30,               // amostragem dos arcos de borda
  coreRadius: 0.172,            // brasa: icosaedro detail 0 (20 tris), visível só no fundo das fendas
  coreJitter: 0.008,
  coreRotationDeg: [17, 31, 9],
  // Fendas: plano {p | n·p = d}, meia-largura g. A principal segue a trajetória; a menor cruza a frente.
  fissures: [
    { name: 'spine', normal: [1, 0, 0], d: 0, halfGap: 0.05 },
    { name: 'brow', normal: [0, 0.34, 1], d: 0.05, halfGap: 0.03 },
  ],
  seed: 8,
};

// Paleta (16×16, células 8×8; UV no centro da célula). Emissão = cor linear multiplicada no material por _EmissionColor.
export const PALETTE = {
  iron: { cell: [0, 0], base: '#3E4152', emission: '#000000', note: 'ferro escuro (pedra/ferro oficial #3E4152)' },
  fissureWall: { cell: [1, 0], base: '#8A4A22', emission: '#8A4A22', note: 'parede da fenda: ferrugem #8A4A22 com emissão média (alarga a leitura da fenda)' },
  ember: { cell: [0, 1], base: '#FF9A2E', emission: '#FF9A2E', note: 'brasa (laranja de ameaça oficial #FF9A2E)' },
  spare: { cell: [1, 1], base: '#3E4152', emission: '#000000', note: 'reserva (= ferro)' },
};
export const MATERIAL = {
  name: NAME,
  shader: 'Universal Render Pipeline/Lit',
  surface: 'Opaque',
  baseMap: `${NAME}_Base.png`, baseColor: '#FFFFFF',
  emissionMap: `${NAME}_Emission.png`, emissionColorHDR: [1.0, 1.0, 1.0], keyword: '_EMISSION',
  metallic: 0.2, smoothness: 0.35,
  note: 'Uma instância compartilhada por todos os projéteis do pool (nenhuma cópia por tiro). GI de emissão: None.',
};

// ———————————————————————— geometria ————————————————————————
const V = (x, y, z) => new THREE.Vector3(x, y, z);
const hash = (i, s) => { let h = Math.imul((i * 2654435761) ^ (s * 40503), 0x45d9f3b); h ^= h >>> 15; h = Math.imul(h, 0x2c1b3c6d); h ^= h >>> 12; return ((h >>> 0) % 100000) / 100000; };
const fis = SHAPE.fissures.map(f => ({ ...f, n: V(...f.normal).normalize() }));

function directions() {
  const g = new THREE.IcosahedronGeometry(1, SHAPE.shellDirections), p = g.attributes.position, seen = new Map();
  for (let i = 0; i < p.count; i++) { const v = V(p.getX(i), p.getY(i), p.getZ(i)).normalize(); const k = v.toArray().map(x => x.toFixed(4)).join(); if (!seen.has(k)) seen.set(k, v); }
  return [...seen.values()];
}

// Placa = casca ∩ {s_k (n_k·p − d_k) ≥ g_k} para os sinais s ∈ {±1}². Hull convexo: amostras externas + arcos de borda.
function plate(signs) {
  const inside = p => fis.every((f, k) => signs[k] * (f.n.dot(p) - f.d) >= f.halfGap - 1e-9);
  const [rMin, rMax] = SHAPE.shellRadius, pts = [];
  directions().forEach((dir, i) => { const r = rMin + (rMax - rMin) * hash(i, SHAPE.seed); const p = dir.clone().multiplyScalar(r); if (inside(p)) pts.push(p); });
  const R = SHAPE.rimRadius;
  fis.forEach((f, k) => {
    // círculo {n·p = d + s·g} ∩ esfera R
    const off = f.d + signs[k] * f.halfGap, rc = Math.sqrt(R * R - off * off), c = f.n.clone().multiplyScalar(off);
    const u = Math.abs(f.n.y) < .9 ? V(0, 1, 0).cross(f.n).normalize() : V(1, 0, 0).cross(f.n).normalize(), w = f.n.clone().cross(u);
    for (let a = 0; a < 360; a += SHAPE.rimStepDeg) { const t = a * Math.PI / 180; const p = c.clone().add(u.clone().multiplyScalar(rc * Math.cos(t))).add(w.clone().multiplyScalar(rc * Math.sin(t))); if (inside(p)) pts.push(p); }
  });
  // cantos: interseção dos dois planos de borda com a esfera R
  const [a, b] = fis, oa = a.d + signs[0] * a.halfGap, ob = b.d + signs[1] * b.halfGap;
  const dir = a.n.clone().cross(b.n).normalize(), nab = a.n.dot(b.n), det = 1 - nab * nab;
  const p0 = a.n.clone().multiplyScalar((oa - ob * nab) / det).add(b.n.clone().multiplyScalar((ob - oa * nab) / det));
  const tt = Math.sqrt(Math.max(0, R * R - p0.lengthSq()));
  pts.push(p0.clone().add(dir.clone().multiplyScalar(tt)), p0.clone().add(dir.clone().multiplyScalar(-tt)));
  return finish(new ConvexGeometry(pts));
}

function core() {
  const g = new THREE.IcosahedronGeometry(SHAPE.coreRadius, 0), p = g.attributes.position;
  const r = SHAPE.coreRotationDeg.map(d => d * Math.PI / 180), m = new THREE.Matrix4().makeRotationFromEuler(new THREE.Euler(...r));
  g.applyMatrix4(m);
  const key = new Map();
  for (let i = 0; i < p.count; i++) {     // jitter radial por vértice (vértices coincidentes andam juntos)
    const k = [p.getX(i), p.getY(i), p.getZ(i)].map(x => x.toFixed(4)).join();
    if (!key.has(k)) key.set(k, 1 + (hash(key.size, SHAPE.seed + 7) - .5) * 2 * SHAPE.coreJitter / SHAPE.coreRadius);
    const s = key.get(k); p.setXYZ(i, p.getX(i) * s, p.getY(i) * s, p.getZ(i) * s);
  }
  return finish(g);
}

// Classifica faces de placa: faces sobre um plano de fenda = parede (fissureWall); demais = ferro.
function faceSwatch(a, b, c) {
  const n = V().subVectors(b, a).cross(V().subVectors(c, a)).normalize(), ctr = V().add(a).add(b).add(c).divideScalar(3);
  for (const f of fis) if (Math.abs(Math.abs(n.dot(f.n)) - 1) < 1e-3 && Math.abs(Math.abs(f.n.dot(ctr) - f.d) - f.halfGap) < 2e-3) return 'fissureWall';
  return 'iron';
}

const cellUV = sw => { const [cx, cy] = PALETTE[sw].cell; return [(cx * 8 + 4) / 16, 1 - (cy * 8 + 4) / 16]; };   // V para cima (FBX/Unity)

export function build() {
  const pieces = [];
  for (const s of [[1, 1], [1, -1], [-1, 1], [-1, -1]]) pieces.push({ name: `plate_${s[0] > 0 ? 'R' : 'L'}${s[1] > 0 ? 'front' : 'rear'}`, geo: plate(s), kind: 'plate' });
  pieces.push({ name: 'ember_core', geo: core(), kind: 'core' });
  const pos = [], nrm = [], uv = [], col = [], faces = [];
  for (const pc of pieces) {
    const p = pc.geo.attributes.position, n = pc.geo.attributes.normal;
    for (let i = 0; i < p.count; i += 3) {
      const A = V(p.getX(i), p.getY(i), p.getZ(i)), B = V(p.getX(i + 1), p.getY(i + 1), p.getZ(i + 1)), C = V(p.getX(i + 2), p.getY(i + 2), p.getZ(i + 2));
      const sw = pc.kind === 'core' ? 'ember' : faceSwatch(A, B, C), [u, v] = cellUV(sw), c = new THREE.Color(PALETTE[sw].base);
      for (let k = 0; k < 3; k++) { pos.push(p.getX(i + k), p.getY(i + k), p.getZ(i + k)); nrm.push(n.getX(i + k), n.getY(i + k), n.getZ(i + k)); uv.push(u, v); col.push(c.r, c.g, c.b); }
      faces.push({ piece: pc.name, swatch: sw });
    }
  }
  const geo = new THREE.BufferGeometry();
  geo.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); geo.setAttribute('normal', new THREE.Float32BufferAttribute(nrm, 3));
  geo.setAttribute('uv', new THREE.Float32BufferAttribute(uv, 2)); geo.setAttribute('color', new THREE.Float32BufferAttribute(col, 3));
  geo.addGroup(0, pos.length / 3, 0); geo.computeBoundingBox();
  const mat = new THREE.MeshStandardMaterial({ name: NAME, color: 0xffffff, vertexColors: true, metalness: MATERIAL.metallic, roughness: 1 - MATERIAL.smoothness, flatShading: true });
  const mesh = new THREE.Mesh(geo, [mat]); mesh.name = NAME;
  return { mesh, pieces, faces };
}

// ———————————————————————— FBX estático com 1 material e texturas (Diffuse + Emissive) ————————————————————————
const fnum = v => { if (!Number.isFinite(v)) throw new Error('valor não finito no FBX'); const s = (+v.toFixed(6)).toString(); return s === '-0' ? '0' : s; };
const farr = (name, a, fmt = fnum, ind = '\t\t') => `${ind}${name}: *${a.length} {\n${ind}\ta: ${a.map(fmt).join(',')}\n${ind}} \n`;
export function toFBX(mesh) {
  let nextId = 3000000; const id = () => nextId++;
  const objects = [], conns = [], counts = {};
  const add = (type, text) => { objects.push(text); counts[type] = (counts[type] || 0) + 1; };
  const mid = id();
  add('Model', `\tModel: ${mid}, "Model::${mesh.name}", "Mesh" {\n\t\tVersion: 232\n\t\tProperties70:  {\n\t\t\tP: "RotationActive", "bool", "", "",1\n\t\t\tP: "InheritType", "enum", "", "",1\n\t\t\tP: "DefaultAttributeIndex", "int", "Integer", "",0\n\t\t\tP: "Lcl Translation", "Lcl Translation", "", "A",0,0,0\n\t\t\tP: "Lcl Rotation", "Lcl Rotation", "", "A",0,0,0\n\t\t\tP: "Lcl Scaling", "Lcl Scaling", "", "A",1,1,1\n\t\t}\n\t\tShading: Y\n\t\tCulling: "CullingOff"\n\t}\n`);
  conns.push(`\tC: "OO",${mid},0`);
  const g = mesh.geometry, P = g.attributes.position, N = g.attributes.normal, T = g.attributes.uv;
  const key = new Map(), cp = [], pvi = [], nrm = [], uvk = new Map(), uv = [], uvi = [];
  for (let i = 0; i < P.count; i++) {
    const k = `${P.getX(i).toFixed(5)},${P.getY(i).toFixed(5)},${P.getZ(i).toFixed(5)}`;
    let c = key.get(k); if (c === undefined) { c = cp.length / 3; key.set(k, c); cp.push(P.getX(i), P.getY(i), P.getZ(i)); }
    pvi.push(i % 3 === 2 ? -(c + 1) : c); nrm.push(N.getX(i), N.getY(i), N.getZ(i));
    const tk = `${T.getX(i).toFixed(4)},${T.getY(i).toFixed(4)}`; let t = uvk.get(tk); if (t === undefined) { t = uv.length / 2; uvk.set(tk, t); uv.push(T.getX(i), T.getY(i)); } uvi.push(t);
  }
  const gid = id(), nPoly = P.count / 3;
  add('Geometry', `\tGeometry: ${gid}, "Geometry::${mesh.name}", "Mesh" {\n` + farr('Vertices', cp) + farr('PolygonVertexIndex', pvi, String) +
    `\t\tGeometryVersion: 124\n\t\tLayerElementNormal: 0 {\n\t\t\tVersion: 101\n\t\t\tName: ""\n\t\t\tMappingInformationType: "ByPolygonVertex"\n\t\t\tReferenceInformationType: "Direct"\n` + farr('Normals', nrm, fnum, '\t\t\t') + `\t\t}\n` +
    `\t\tLayerElementUV: 0 {\n\t\t\tVersion: 101\n\t\t\tName: "UVMap"\n\t\t\tMappingInformationType: "ByPolygonVertex"\n\t\t\tReferenceInformationType: "IndexToDirect"\n` + farr('UV', uv, fnum, '\t\t\t') + farr('UVIndex', uvi, String, '\t\t\t') + `\t\t}\n` +
    `\t\tLayerElementMaterial: 0 {\n\t\t\tVersion: 101\n\t\t\tName: ""\n\t\t\tMappingInformationType: "AllSame"\n\t\t\tReferenceInformationType: "IndexToDirect"\n` + farr('Materials', [0], String, '\t\t\t') + `\t\t}\n` +
    `\t\tLayer: 0 {\n\t\t\tVersion: 100\n\t\t\tLayerElement:  {\n\t\t\t\tType: "LayerElementNormal"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t\tLayerElement:  {\n\t\t\t\tType: "LayerElementMaterial"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t\tLayerElement:  {\n\t\t\t\tType: "LayerElementUV"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t}\n\t}\n`);
  conns.push(`\tC: "OO",${gid},${mid}`);
  const matId = id();
  add('Material', `\tMaterial: ${matId}, "Material::${MATERIAL.name}", "" {\n\t\tVersion: 102\n\t\tShadingModel: "phong"\n\t\tMultiLayer: 0\n\t\tProperties70:  {\n` +
    `\t\t\tP: "DiffuseColor", "Color", "", "A",1,1,1\n\t\t\tP: "DiffuseFactor", "Number", "", "A",1\n` +
    `\t\t\tP: "EmissiveColor", "Color", "", "A",1,1,1\n\t\t\tP: "EmissiveFactor", "Number", "", "A",${fnum(MATERIAL.emissionColorHDR[0])}\n` +
    `\t\t\tP: "Shininess", "Number", "", "A",${fnum(MATERIAL.smoothness * 100)}\n\t\t\tP: "ReflectionFactor", "Number", "", "A",${fnum(MATERIAL.metallic)}\n\t\t}\n\t}\n`);
  conns.push(`\tC: "OO",${matId},${mid}`);
  for (const [prop, file] of [['DiffuseColor', MATERIAL.baseMap], ['EmissiveColor', MATERIAL.emissionMap]]) {
    const vid = id(), tid = id(), nm = file.replace(/\.png$/, '');
    add('Video', `\tVideo: ${vid}, "Video::${nm}", "Clip" {\n\t\tType: "Clip"\n\t\tProperties70:  {\n\t\t\tP: "Path", "KString", "XRefUrl", "", "${file}"\n\t\t}\n\t\tUseMipMap: 0\n\t\tFilename: "${file}"\n\t\tRelativeFilename: "${file}"\n\t}\n`);
    add('Texture', `\tTexture: ${tid}, "Texture::${nm}", "" {\n\t\tType: "TextureVideoClip"\n\t\tVersion: 202\n\t\tTextureName: "Texture::${nm}"\n\t\tProperties70:  {\n\t\t\tP: "UseMaterial", "bool", "", "",1\n\t\t}\n\t\tMedia: "Video::${nm}"\n\t\tFileName: "${file}"\n\t\tRelativeFilename: "${file}"\n\t\tModelUVTranslation: 0,0\n\t\tModelUVScaling: 1,1\n\t\tTexture_Alpha_Source: "None"\n\t\tCropping: 0,0,0,0\n\t}\n`);
    conns.push(`\tC: "OO",${vid},${tid}`, `\tC: "OP",${tid},${matId}, "${prop}"`);
  }
  const total = Object.values(counts).reduce((a, b) => a + b, 0) + 1;
  const header = `; FBX 7.4.0 project file\n; Gerado por Tools/Lemo3DGenerator/src/production/FxProjectileCore.mjs (three.js do Lemo-Opuscar)\n; ----------------------------------------------------\n\n` +
    `FBXHeaderExtension:  {\n\tFBXHeaderVersion: 1003\n\tFBXVersion: 7400\n\tCreator: "Lemo3DGenerator FxProjectileCore"\n}\n` +
    `GlobalSettings:  {\n\tVersion: 1000\n\tProperties70:  {\n\t\tP: "UpAxis", "int", "Integer", "",1\n\t\tP: "UpAxisSign", "int", "Integer", "",1\n\t\tP: "FrontAxis", "int", "Integer", "",2\n\t\tP: "FrontAxisSign", "int", "Integer", "",1\n\t\tP: "CoordAxis", "int", "Integer", "",0\n\t\tP: "CoordAxisSign", "int", "Integer", "",1\n\t\tP: "OriginalUpAxis", "int", "Integer", "",1\n\t\tP: "OriginalUpAxisSign", "int", "Integer", "",1\n\t\tP: "UnitScaleFactor", "double", "Number", "",100\n\t\tP: "OriginalUnitScaleFactor", "double", "Number", "",100\n\t}\n}\n\n` +
    `Documents:  {\n\tCount: 1\n\tDocument: ${id()}, "Scene", "Scene" {\n\t\tProperties70:  {\n\t\t\tP: "SourceObject", "object", "", ""\n\t\t}\n\t\tRootNode: 0\n\t}\n}\n\nReferences:  {\n}\n\n` +
    `Definitions:  {\n\tVersion: 100\n\tCount: ${total}\n\tObjectType: "GlobalSettings" {\n\t\tCount: 1\n\t}\n` + Object.entries(counts).map(([k, n]) => `\tObjectType: "${k}" {\n\t\tCount: ${n}\n\t}\n`).join('') + `}\n\n`;
  return { text: header + `Objects:  {\n${objects.join('')}}\n\nConnections:  {\n${conns.join('\n')}\n}\n`, polygons: nPoly, controlPoints: cp.length / 3 };
}

// ———————————————————————— paleta PNG, metas ————————————————————————
const hex = h => [1, 3, 5].map(i => parseInt(h.slice(i, i + 2), 16));
function palettePNG(channel) {
  const w = 16, h = 16, px = Buffer.alloc(w * h * 4);
  for (const sw of Object.values(PALETTE)) {
    const [r, g, b] = hex(sw[channel]), [cx, cy] = sw.cell;
    for (let y = cy * 8; y < cy * 8 + 8; y++) for (let x = cx * 8; x < cx * 8 + 8; x++) { const o = (y * w + x) * 4; px[o] = r; px[o + 1] = g; px[o + 2] = b; px[o + 3] = 255; }
  }
  return encodePNG(w, h, px);
}
export const stableGuid = key => createHash('sha256').update('Eldoria/A08/' + key).digest('hex').slice(0, 32);
function textureMeta(file, srgb) {
  let t = readFileSync(TEX_TEMPLATE, 'utf8').replace('{{GUID}}', stableGuid(file));
  // [^\r\n]: o template é CRLF; preservar o \r de cada linha alterada
  const set = (k, v) => { const re = () => new RegExp(`(\\n[ \\t]*${k}: )[^\\r\\n]*`, 'g'); if (!re().test(t)) throw new Error(`template sem ${k}`); t = t.replace(re(), `$1${v}`); };
  set('enableMipMap', 0); set('sRGBTexture', srgb ? 1 : 0); set('filterMode', 0); set('alphaUsage', 0); set('alphaIsTransparency', 0); set('textureCompression', 0); set('maxTextureSize', 32);
  return t;
}
function modelMeta() {
  let t = readFileSync(FBX_TEMPLATE, 'utf8');
  t = t.replace(/^guid: [0-9a-f]{32}$/m, `guid: ${stableGuid(NAME + '.fbx')}`);
  if (!t.includes(`guid: ${stableGuid(NAME + '.fbx')}`) || !/addColliders: 0/.test(t) || !/importAnimation: 0/.test(t) || !/animationType: 0/.test(t)) throw new Error('template ModelImporter inesperado');
  return t;
}
const folderMeta = key => `fileFormatVersion: 2\nguid: ${stableGuid(key)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`;

// ———————————————————————— validação interna ————————————————————————
export function validate(m) {
  const g = m.mesh.geometry, p = g.attributes.position, issues = [], t = tris(g);
  let rMax = 0; for (let i = 0; i < p.count; i++) rMax = Math.max(rMax, Math.hypot(p.getX(i), p.getY(i), p.getZ(i)));
  const b = g.boundingBox, ctr = b.getCenter(V()), size = b.getSize(V());
  if (t > 300) issues.push(`${t} tris > 300`);
  if (rMax > SHAPE.envelopeRadius + 1e-6) issues.push(`raio máx ${rMax} > envelope ${SHAPE.envelopeRadius}`);
  if (ctr.length() > .01) issues.push(`centro dos bounds ${ctr.toArray()} longe do pivô`);
  const pieces = m.pieces.map(pc => ({ name: pc.name, triangles: tris(pc.geo), signedVolume: +signedVolume(pc.geo).toFixed(7) }));
  for (const pc of pieces) if (pc.signedVolume <= 0) issues.push(`${pc.name}: volume ≤ 0 (normais para dentro)`);
  const bySwatch = {}; for (const f of m.faces) bySwatch[f.swatch] = (bySwatch[f.swatch] || 0) + 1;
  if (!bySwatch.fissureWall || !bySwatch.ember || !bySwatch.iron) issues.push('faltam faces de ferro/parede/brasa');
  return { ok: !issues.length, issues, triangles: t, maxRadius: +rMax.toFixed(5), bounds: { min: b.min.toArray().map(v => +v.toFixed(4)), max: b.max.toArray().map(v => +v.toFixed(4)), size: size.toArray().map(v => +v.toFixed(4)), center: ctr.toArray().map(v => +v.toFixed(4)) }, pieces, facesBySwatch: bySwatch, materials: 1 };
}

function renderData(m) {
  const g = m.mesh.geometry, p = g.attributes.position, n = g.attributes.normal, out = [];
  for (let i = 0, f = 0; i < p.count; i += 3, f++) out.push({ s: m.faces[f].swatch, p: [0, 1, 2].map(k => [p.getX(i + k), p.getY(i + k), p.getZ(i + k)].map(v => +v.toFixed(5))), n: [n.getX(i), n.getY(i), n.getZ(i)].map(v => +v.toFixed(4)) });
  return out;
}

// ———————————————————————— principal ————————————————————————
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const m = build(), val = validate(m);
  console.log(`${NAME}: ${val.triangles} tris, raio máx ${val.maxRadius} m, size ${JSON.stringify(val.bounds.size)}, faces ${JSON.stringify(val.facesBySwatch)}`);
  if (!val.ok) { console.error('FALHOU:\n  ' + val.issues.join('\n  ')); process.exit(1); }
  if (has('check')) { console.log('check OK'); process.exit(0); }
  mkdirSync(SRC, { recursive: true });
  const fbx = toFBX(m.mesh);
  writeFileSync(join(OUT, `${NAME}.fbx`), fbx.text);
  writeFileSync(join(OUT, `${NAME}.fbx.meta`), modelMeta());
  writeFileSync(join(OUT, MATERIAL.baseMap), palettePNG('base'));
  writeFileSync(join(OUT, MATERIAL.baseMap + '.meta'), textureMeta(MATERIAL.baseMap, true));
  writeFileSync(join(OUT, MATERIAL.emissionMap), palettePNG('emission'));
  writeFileSync(join(OUT, MATERIAL.emissionMap + '.meta'), textureMeta(MATERIAL.emissionMap, true));
  writeFileSync(join(PROJECT, STAGING + '.meta'), folderMeta('folder/' + ID));
  const glb = Buffer.from(await new GLTFExporter().parseAsync(m.mesh, { binary: true, onlyVisible: false }));
  writeFileSync(join(SRC, `${NAME}.glb`), glb);
  writeFileSync(join(SRC, 'FX-PROJECTILE.render.json'), JSON.stringify({ palette: PALETTE, emissionHDR: MATERIAL.emissionColorHDR[0], faces: renderData(m) }));
  const source = {
    id: ID, batch: 'A08', name: NAME, generator: 'Tools/Lemo3DGenerator/src/production/FxProjectileCore.mjs', three: THREE_VERSION, lemoRoot: LEMO_ROOT, node: process.version,
    units: 'metros, +Y para cima, +Z frente (direção de voo: Projectile.Launch usa LookRotation(direção)), pivô no centro',
    shape: SHAPE, palette: PALETTE, material: MATERIAL,
    pieces: val.pieces, triangles: val.triangles, fbx: { file: `${NAME}.fbx`, polygons: fbx.polygons, controlPoints: fbx.controlPoints, materials: 1, textures: [MATERIAL.baseMap, MATERIAL.emissionMap] },
    guids: { folder: stableGuid('folder/' + ID), fbx: stableGuid(NAME + '.fbx'), base: stableGuid(MATERIAL.baseMap), emission: stableGuid(MATERIAL.emissionMap) },
    import: { model: 'ModelImporter copiado de Assets/_Project/Art/Items/Models/Loot_XpOrb.fbx.meta (Unity 6000.6, serializedVersion 24600): globalScale 1, useFileUnits, addColliders 0, importAnimation 0, animationType 0 (None), normais importadas, materialImportMode 2; só o GUID muda.',
      textures: 'Template FX_Spark (Tools/FxHealGenerator/texture.meta.template) com point, sem mipmaps, sem compressão, sem alpha, max 32; sRGB nos dois (a cor de emissão também é sRGB).' },
    integration_proposal: {
      status: 'NÃO INTEGRADO — tarefa Codex separada; Projectile.prefab, Projectile.cs, ProjectilePool.cs e Mat_Projectile não foram alterados.',
      child: `Filho "Visual" de Projectile.prefab com MeshFilter(${NAME}) + MeshRenderer(material ${NAME}, compartilhado). A raiz tem localScale 0,4 (a esfera Unity de 1 m vira 0,4 m): ou o filho usa localScale 2,5, ou a raiz passa para escala 1 — Projectile.cs não usa a escala (SphereCast com radius = 0,25 m em unidades de mundo).`,
      root: 'Desligar (não remover) o MeshRenderer da esfera da raiz, ou trocar a malha; nenhum Collider existe nem deve ser criado.',
      preserve: ['hitMask', 'radius 0,25', 'lifetime 4', 'Launch/ForceRelease/OnDespawned', 'ProjectilePool defaultCapacity 16 / maxSize 64 e aquecimento'],
      renderer: 'Cast Shadows: opcional (a esfera atual projeta sombra); Light Probes/Reflection Probes como a esfera; sem luz pontual por projétil.',
      tests: ['16 tiros simultâneos (capacidade padrão do pool) sem Instantiate após aquecimento', 'reset: ForceRelease / OnDisable do pool / pausa (timeScale 0) — visual some/volta com o objeto', 'medir GPU (Frame Debugger/Profiler) antes e depois; não declarar ganho sem medição', 'legibilidade sobre o piso #3E4152/#55596B e telegraph do Warden'],
    },
    licence: 'Projeto Eldoria; geometria própria gerada por código (three.js r170 MIT, somente leitura no Lemo-Opuscar). Sem IA generativa nem asset de terceiros.',
  };
  writeFileSync(join(SRC, 'FX-PROJECTILE.source.json'), JSON.stringify(source, null, 2));
  writeFileSync(join(SRC, 'FxProjectile.generator.json'), JSON.stringify({ ...val, glbBytes: glb.length, fbxBytes: Buffer.byteLength(fbx.text) }, null, 2));
  console.log('OK →', relative(PROJECT, OUT));
}
