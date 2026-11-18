#!/usr/bin/env node
// A11 — MODEL-CHEST: baú de pedra e latão (visual do baú F08; ID em Assets/_Project/Art/ProductionSpecs/ASSET_MANIFEST.json).
//
// Fonte de autoria editável do modelo e do take mecânico. Mesmo vocabulário low-poly do Lemo3DGenerator (geo.mjs: caixas
// chanfradas, hulls convexos, normais de face) e a mesma convenção do export-fbx.mjs / FxProjectileCore.mjs: metros, +Y para
// cima, +Z frente. Só visual: nenhum arquivo de domínio é lido ou escrito (RoomRewardChest, RoomRewardPresenter, builders,
// cenas e prefabs ficam intactos). Sem collider, recompensa, loot, moeda nova ou rig humanoide.
//
// Forma: plinto de pedra escura, quatro paredes de blocos chanfrados (frente/trás em três blocos, lados em um) com interior
// escuro e fundo, duas cintas de latão na frente e duas atrás, espelho de fechadura; tampa em sólido facetado (faixa vertical +
// quatro águas) com aro de latão na frente/lados, duas cintas que passam por cima e descem até a dobradiça, e uma pega larga.
// Dobradiça traseira: eixo X em (y 0,54, z −0,342), atrás de toda a pedra da tampa; nós de dobradiça da base e da tampa são
// cilindros coaxiais intercalados em X (sem sobreposição). Toda geometria da tampa fica à frente do eixo, então ao abrir
// nada dela entra na base (verificado por SAT no validador ao longo do take).
//
// FBX: dois nós Mesh de topo, "Base" (pivô na base central, origem) e "Lid" (pivô no eixo da dobradiça, rotação zero em
// repouso = fechado). Dois materiais (pedra e latão), ambos com a mesma paleta 16×16 como BaseMap; UV por face no centro de
// uma célula. Um take "Open" de 0,6 s a 30 fps só na rotação X do Lid (in-place; Base nunca anima).
//
// Saídas:
//   Assets/_Project/Art/ProductionStaging/MODEL-CHEST/
//     MODEL-CHEST.fbx (+ .meta)                FBX 7.4 ASCII: Base + Lid, 2 materiais, take Open
//     MODEL-CHEST_Palette.png (+ .meta)        paleta 16×16 RGB (células 4×4 px)
//   …/Source~/
//     MODEL-CHEST.glb                          revisão/intercâmbio DCC (cores por vértice + clip Open; não importado pela Unity)
//     MODEL-CHEST.source.json                  parâmetros, paleta, materiais, clip, integração proposta
//     ModelChest.generator.json                validação interna (geometria em memória)
//
// Uso (dentro de Tools/Lemo3DGenerator):
//   node src/production/ModelChest.mjs            gera
//   node src/production/ModelChest.mjs --check    só valida
// Depois: python -I Tools/Lemo3DGenerator/src/production/ModelChest_preview.py  (relê o FBX do disco, valida, previews)
// Lemo-Opuscar é usado SOMENTE LEITURA (three.js r170, ConvexGeometry, GLTFExporter).
import { mkdirSync, writeFileSync, readFileSync } from 'node:fs';
import { join, resolve, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
import { THREE, GLTFExporter, ConvexGeometry, THREE_VERSION, LEMO_ROOT } from '../lemo-three.mjs';
import { finish, tris, signedVolume, bevelBox } from '../geo.mjs';
import { encodePNG } from './ItemIcons.mjs';

const HERE = dirname(fileURLToPath(import.meta.url)), LAB = resolve(HERE, '..', '..'), PROJECT = resolve(LAB, '..', '..');
const has = k => process.argv.includes('--' + k);
export const ID = 'MODEL-CHEST';
export const STAGING = 'Assets/_Project/Art/ProductionStaging/MODEL-CHEST';
const OUT = join(PROJECT, STAGING), SRC = join(OUT, 'Source~');
export const NAME = 'MODEL-CHEST';
const FBX_TEMPLATE = join(PROJECT, 'Assets/_Project/Art/Items/Models/Loot_XpOrb.fbx.meta');        // ModelImporter real (Unity 6000.6)
const TEX_TEMPLATE = join(PROJECT, 'Tools/FxHealGenerator/texture.meta.template');                 // cópia de FX_Spark.png.meta

// ———————————————————————— parâmetros editáveis ————————————————————————
export const DIM = { width: 1.2, depth: 0.7, height: 0.8, note: 'ficha A11: ~1,2 × 0,7 × 0,8 m (L × P × A)' };
export const SHAPE = {
  plinth: { w: 1.2, d: 0.7, h: 0.08, chamfer: 0.25 },
  walls: { w: 1.12, d: 0.62, y0: 0.08, y1: 0.54, thick: 0.07, frontBlocks: 3, chamfer: 0.15 },
  floorTop: 0.14,
  hinge: { y: 0.54, z: -0.342, knuckleRadius: 0.021, segments: 6 },
  lid: { halfW: 0.575, zRear: -0.315, zFront: 0.32, slabTop: 0.64, top: 0.8, topHalfW: 0.45, topZ: [-0.15, 0.16] },
  band: { y: [0.545, 0.6], protrude: 0.015 },
  straps: { x: 0.36, width: 0.07, protrude: 0.012, baseY: [0.09, 0.53] },
  lock: { halfW: 0.075, y: [0.4, 0.525], keyhole: { halfW: 0.012, y: [0.44, 0.48] } },
  handle: { halfW: 0.2, y: [0.575, 0.62], z: [0.35, 0.368], posts: { x: [0.15, 0.19], y: [0.58, 0.615], z: [0.33, 0.352] } },
};
export const BUDGET = { triangles: 1200, materials: 2 };

// Paleta 16×16 com células 4×4 px (UV no centro da célula). Cores oficiais da ficha; interior e latão gasto derivados.
export const PALETTE = {
  stoneLight: { cell: [0, 0], hex: '#55596B', note: 'pedra clara oficial (paredes, águas e topo da tampa)' },
  stoneDark: { cell: [1, 0], hex: '#3E4152', note: 'pedra escura oficial (plinto, faixa vertical da tampa)' },
  interior: { cell: [2, 0], hex: '#25262F', note: 'interior/fundo/face de baixo da tampa e buraco da fechadura: derivado escurecido de #3E4152 (lê "aberto" pela câmera)' },
  brass: { cell: [0, 1], hex: '#E0B040', note: 'latão oficial (faces de cima/frente das cintas, aro e pega)' },
  brassWorn: { cell: [1, 1], hex: '#9C7A30', note: 'latão gasto: derivado de #E0B040 (laterais, traseira, dobradiças)' },
};
export const MATERIALS = [
  { index: 0, name: `${NAME}_Stone`, shader: 'Universal Render Pipeline/Lit', surface: 'Opaque', baseMap: `${NAME}_Palette.png`, baseColor: '#FFFFFF', metallic: 0, smoothness: 0.12, note: 'fosco, como GA_Stone_Floor/Wall (smoothness 0,10–0,15)' },
  { index: 1, name: `${NAME}_Brass`, shader: 'Universal Render Pipeline/Lit', surface: 'Opaque', baseMap: `${NAME}_Palette.png`, baseColor: '#FFFFFF', metallic: 0.45, smoothness: 0.3, note: 'latão fosco gasto (GA_Iron usa 0,5/0,35); sem emissão' },
];
// Take mecânico: destrava (−4°), gira com aceleração/desaceleração, passa do ponto (−111°), volta (−103,5°) e assenta em −105°
// (mesmo ângulo aberto que RoomRewardPresenter aplica hoje ao LidPivot). Cada segmento é smoothstep (velocidade zero nas chaves).
export const CLIP = {
  name: 'Open', duration: 0.6, fps: 30, loop: false, target: 'Lid', channel: 'Lcl Rotation X (graus)',
  keys: [[0, 0], [0.07, -4], [0.43, -111], [0.52, -103.5], [0.6, -105]],
  openDeg: -105,
  note: 'Rotação X negativa ergue a frente (+Z) da tampa nas três convenções (three.js, FBX, Unity após o espelho de X). Base e raiz sem curvas (in-place).',
};

// ———————————————————————— geometria ————————————————————————
const V = (x, y, z) => new THREE.Vector3(x, y, z);
const H = SHAPE.hinge;
export const HINGE = [0, H.y, H.z];
const box = (x0, x1, y0, y1, z0, z1) => { const g = new THREE.BoxGeometry(x1 - x0, y1 - y0, z1 - z0); g.translate((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2); return finish(g); };
const bbox = (x0, x1, y0, y1, z0, z1, c) => { const g = bevelBox(x1 - x0, y1 - y0, z1 - z0, c); g.translate((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2); return g; };
const hull = pts => finish(new ConvexGeometry(pts.map(p => V(...p))));
function knuckle(x0, x1) {   // cilindro ao longo de X, coaxial com a dobradiça
  const g = new THREE.CylinderGeometry(H.knuckleRadius, H.knuckleRadius, x1 - x0, H.segments, 1, false, Math.PI / H.segments);
  g.rotateZ(Math.PI / 2); g.translate((x0 + x1) / 2, H.y, H.z); return finish(g);
}
// offset para fora de um polígono convexo (z, y) em sentido anti-horário
function offsetPolygon(pts, o) {
  const n = pts.length, out = [];
  for (let i = 0; i < n; i++) {
    const p = pts[i], a = pts[(i - 1 + n) % n], b = pts[(i + 1) % n];
    const e1 = [p[0] - a[0], p[1] - a[1]], e2 = [b[0] - p[0], b[1] - p[1]];
    const n1 = [e1[1], -e1[0]].map(v => v / Math.hypot(...e1)), n2 = [e2[1], -e2[0]].map(v => v / Math.hypot(...e2));
    const m = [n1[0] + n2[0], n1[1] + n2[1]], ml = Math.hypot(...m), k = o / ((m[0] / ml) * n1[0] + (m[1] / ml) * n1[1]);
    out.push([p[0] + m[0] / ml * k, p[1] + m[1] / ml * k]);
  }
  return out;
}

// Classificação por face (normal n e centróide c, espaço do modelo) → amostra da paleta
const stoneShell = (n) => n.y < -0.9 ? 'interior' : Math.abs(n.y) < 0.2 ? 'stoneDark' : 'stoneLight';
const brassFace = (n) => (n.y > 0.3 || n.z > 0.6) ? 'brass' : 'brassWorn';
const wallFace = axis => (n, c) => (n[axis] * Math.sign(c[axis]) < -0.3) ? 'interior' : n.y < -0.5 ? 'stoneDark' : 'stoneLight';

export function parts() {
  const S = SHAPE, W = S.walls, L = S.lid, P = [];
  const add = (name, part, mat, geo, swatch) => P.push({ name, part, mat, geo, swatch: typeof swatch === 'function' ? swatch : () => swatch });
  // —— Base ——
  add('plinth', 'Base', 0, bbox(-S.plinth.w / 2, S.plinth.w / 2, 0, S.plinth.h, -S.plinth.d / 2, S.plinth.d / 2, S.plinth.chamfer), 'stoneDark');
  const bw = W.w / W.frontBlocks;
  for (const [side, z0, z1] of [['front', W.d / 2 - W.thick, W.d / 2], ['back', -W.d / 2, -W.d / 2 + W.thick]])
    for (let i = 0; i < W.frontBlocks; i++)
      add(`wall_${side}_${i}`, 'Base', 0, bbox(-W.w / 2 + i * bw, -W.w / 2 + (i + 1) * bw, W.y0, W.y1, z0, z1, W.chamfer), wallFace('z'));
  for (const s of [-1, 1]) add(`wall_${s > 0 ? 'right' : 'left'}`, 'Base', 0, bbox(s > 0 ? W.w / 2 - W.thick : -W.w / 2, s > 0 ? W.w / 2 : -W.w / 2 + W.thick, W.y0, W.y1, -W.d / 2 + W.thick, W.d / 2 - W.thick, W.chamfer), wallFace('x'));
  add('floor', 'Base', 0, box(-W.w / 2 + W.thick, W.w / 2 - W.thick, W.y0, S.floorTop, -W.d / 2 + W.thick, W.d / 2 - W.thick), 'interior');
  const st = S.straps;
  for (const s of [-1, 1]) {
    const x0 = s * st.x - st.width / 2, x1 = s * st.x + st.width / 2;
    add(`strap_front_${s > 0 ? 'R' : 'L'}`, 'Base', 1, box(x0, x1, st.baseY[0], st.baseY[1], W.d / 2 - 0.01, W.d / 2 + st.protrude + 0.003), brassFace);
    // cinta traseira termina abaixo do nó da tampa (mesmo X): topo ≤ eixo − raio − 8 mm, sem tocar o cilindro
    add(`strap_back_${s > 0 ? 'R' : 'L'}`, 'Base', 1, box(x0, x1, st.baseY[0], Math.min(st.baseY[1], H.y - H.knuckleRadius - 0.008), -W.d / 2 - st.protrude - 0.003, -W.d / 2 + 0.01), brassFace);
    // nós da base flanqueiam o nó da tampa (x da cinta) com folga de 6 mm; folha liga o nó à parede traseira, abaixo da junta
    for (const [k0, k1, tag] of [[st.x + 0.038, st.x + 0.098, 'out'], [st.x - 0.098, st.x - 0.038, 'in']]) {
      const a = s > 0 ? k0 : -k1, b = s > 0 ? k1 : -k0;
      add(`hinge_base_${s > 0 ? 'R' : 'L'}_${tag}`, 'Base', 1, knuckle(a, b), 'brassWorn');
      add(`leaf_base_${s > 0 ? 'R' : 'L'}_${tag}`, 'Base', 1, box(a, b, H.y - 0.06, H.y - 0.005, H.z, -W.d / 2 + 0.005), 'brassWorn');
    }
  }
  const lk = S.lock;
  add('lock_plate', 'Base', 1, box(-lk.halfW, lk.halfW, lk.y[0], lk.y[1], W.d / 2 - 0.01, W.d / 2 + 0.012), brassFace);
  add('keyhole', 'Base', 0, box(-lk.keyhole.halfW, lk.keyhole.halfW, lk.keyhole.y[0], lk.keyhole.y[1], W.d / 2 + 0.008, W.d / 2 + 0.016), 'interior');
  // —— Lid (espaço do modelo; convertido para o pivô da dobradiça na exportação) ——
  const [tz0, tz1] = L.topZ, sh = [];
  for (const sx of [-1, 1]) for (const z of [L.zRear, L.zFront]) sh.push([sx * L.halfW, H.y, z], [sx * L.halfW, L.slabTop, z]);
  for (const sx of [-1, 1]) for (const z of [tz0, tz1]) sh.push([sx * L.topHalfW, L.top, z]);
  add('lid_shell', 'Lid', 0, hull(sh), stoneShell);
  const bd = S.band;
  add('band_front', 'Lid', 1, box(-L.halfW - bd.protrude, L.halfW + bd.protrude, bd.y[0], bd.y[1], L.zFront - 0.002, L.zFront + bd.protrude), brassFace);
  for (const s of [-1, 1]) add(`band_${s > 0 ? 'right' : 'left'}`, 'Lid', 1, box(s > 0 ? L.halfW - 0.002 : -L.halfW - bd.protrude, s > 0 ? L.halfW + bd.protrude : -L.halfW + 0.002, bd.y[0], bd.y[1], L.zRear, L.zFront + bd.protrude), brassFace);
  // cintas da tampa: perfil lateral da tampa com offset para fora; base elevada para ficar escondida dentro da tampa
  const profile = [[L.zRear, H.y], [L.zFront, H.y], [L.zFront, L.slabTop], [tz1, L.top], [tz0, L.top], [L.zRear, L.slabTop]];
  const off = offsetPolygon(profile, st.protrude).map(([z, y], i) => [z, i < 2 ? H.y + 0.008 : y]);   // perfil anti-horário em (z, y)
  for (const s of [-1, 1]) {
    const x0 = s * st.x - st.width / 2, x1 = s * st.x + st.width / 2, pts = [];
    for (const x of [x0, x1]) for (const [z, y] of off) pts.push([x, y, z]);
    add(`strap_lid_${s > 0 ? 'R' : 'L'}`, 'Lid', 1, hull(pts), brassFace);
    add(`hinge_lid_${s > 0 ? 'R' : 'L'}`, 'Lid', 1, knuckle(x0 + 0.003, x1 - 0.003), 'brassWorn');
    add(`leaf_lid_${s > 0 ? 'R' : 'L'}`, 'Lid', 1, box(x0 + 0.005, x1 - 0.005, H.y, H.y + 0.045, H.z, L.zRear + 0.004), 'brassWorn');
  }
  const hd = S.handle, po = hd.posts;
  add('handle_bar', 'Lid', 1, box(-hd.halfW, hd.halfW, hd.y[0], hd.y[1], hd.z[0], hd.z[1]), brassFace);
  for (const s of [-1, 1]) add(`handle_post_${s > 0 ? 'R' : 'L'}`, 'Lid', 1, box(s > 0 ? po.x[0] : -po.x[1], s > 0 ? po.x[1] : -po.x[0], po.y[0], po.y[1], po.z[0], po.z[1]), 'brassWorn');
  return P;
}

const cellUV = sw => { const [cx, cy] = PALETTE[sw].cell; return [(cx * 4 + 2) / 16, 1 - (cy * 4 + 2) / 16]; };   // V para cima (FBX/Unity)

// Monta as duas malhas (Base na origem; Lid relativa ao eixo da dobradiça) com UV de paleta, cor por vértice (GLB) e material por face.
export function build() {
  const P = parts(), meshes = {};
  for (const part of ['Base', 'Lid']) {
    const origin = part === 'Lid' ? V(...HINGE) : V(0, 0, 0);
    const pos = [], nrm = [], uv = [], col = [], faces = [], groups = [[], []], pieces = [];
    for (const pc of P.filter(p => p.part === part)) {
      const p = pc.geo.attributes.position, n = pc.geo.attributes.normal;
      for (let i = 0; i < p.count; i += 3) {
        const A = V(p.getX(i), p.getY(i), p.getZ(i)), B = V(p.getX(i + 1), p.getY(i + 1), p.getZ(i + 1)), C = V(p.getX(i + 2), p.getY(i + 2), p.getZ(i + 2));
        const fn = V().subVectors(B, A).cross(V().subVectors(C, A)).normalize(), ctr = V().add(A).add(B).add(C).divideScalar(3);
        const sw = pc.swatch(fn, ctr);
        if (!PALETTE[sw]) throw new Error(`${pc.name}: amostra ${sw}`);
        groups[pc.mat].push({ verts: [A, B, C].map(v => v.clone().sub(origin)), n: [0, 1, 2].map(k => [n.getX(i + k), n.getY(i + k), n.getZ(i + k)]), sw, piece: pc.name });
      }
      pieces.push({ name: pc.name, material: MATERIALS[pc.mat].name, triangles: tris(pc.geo), signedVolume: +signedVolume(pc.geo).toFixed(7) });
    }
    const geo = new THREE.BufferGeometry();
    let start = 0;
    for (const m of [0, 1]) {
      for (const f of groups[m]) {
        const [u, v] = cellUV(f.sw), c = new THREE.Color(PALETTE[f.sw].hex);
        f.verts.forEach((vv, k) => { pos.push(vv.x, vv.y, vv.z); nrm.push(...f.n[k]); uv.push(u, v); col.push(c.r, c.g, c.b); });
        faces.push({ material: m, swatch: f.sw, piece: f.piece });
      }
      geo.addGroup(start * 3, groups[m].length * 3, m); start += groups[m].length;
    }
    geo.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); geo.setAttribute('normal', new THREE.Float32BufferAttribute(nrm, 3));
    geo.setAttribute('uv', new THREE.Float32BufferAttribute(uv, 2)); geo.setAttribute('color', new THREE.Float32BufferAttribute(col, 3));
    geo.computeBoundingBox();
    const mats = MATERIALS.map(m => new THREE.MeshStandardMaterial({ name: m.name, color: 0xffffff, vertexColors: true, metalness: m.metallic, roughness: 1 - m.smoothness, flatShading: true }));
    const mesh = new THREE.Mesh(geo, mats); mesh.name = part; mesh.position.copy(origin);
    meshes[part] = { mesh, faces, pieces, origin: origin.toArray() };
  }
  return meshes;
}

// ———————————————————————— take Open ————————————————————————
const smooth = t => t * t * (3 - 2 * t);
export function lidAngle(t) {
  const k = CLIP.keys; if (t <= k[0][0]) return k[0][1]; if (t >= k[k.length - 1][0]) return k[k.length - 1][1];
  for (let i = 0; i + 1 < k.length; i++) if (t <= k[i + 1][0]) { const u = (t - k[i][0]) / (k[i + 1][0] - k[i][0]); return k[i][1] + (k[i + 1][1] - k[i][1]) * smooth(u); }
}
export function bakeClip() {
  const n = Math.round(CLIP.duration * CLIP.fps), frames = [];
  for (let f = 0; f <= n; f++) frames.push({ frame: f, t: +(f / CLIP.fps).toFixed(6), deg: +lidAngle(f / CLIP.fps).toFixed(4) });
  return frames;
}

// ———————————————————————— FBX 7.4 ASCII: 2 Mesh, 2 materiais, 1 textura, take Open ————————————————————————
const KTIME = 46186158000n;
const fnum = v => { if (!Number.isFinite(v)) throw new Error('valor não finito no FBX'); const s = (+v.toFixed(6)).toString(); return s === '-0' ? '0' : s; };
const farr = (name, a, fmt = fnum, ind = '\t\t') => `${ind}${name}: *${a.length} {\n${ind}\ta: ${a.map(fmt).join(',')}\n${ind}} \n`;
export function toFBX(meshes, frames) {
  let nextId = 4000000; const id = () => nextId++;
  const objects = [], conns = [], counts = {}, info = {};
  const add = (type, text) => { objects.push(text); counts[type] = (counts[type] || 0) + 1; };
  const matIds = MATERIALS.map(m => {
    const mid = id();
    add('Material', `\tMaterial: ${mid}, "Material::${m.name}", "" {\n\t\tVersion: 102\n\t\tShadingModel: "phong"\n\t\tMultiLayer: 0\n\t\tProperties70:  {\n` +
      `\t\t\tP: "DiffuseColor", "Color", "", "A",1,1,1\n\t\t\tP: "DiffuseFactor", "Number", "", "A",1\n\t\t\tP: "EmissiveColor", "Color", "", "A",0,0,0\n\t\t\tP: "EmissiveFactor", "Number", "", "A",0\n` +
      `\t\t\tP: "Shininess", "Number", "", "A",${fnum(m.smoothness * 100)}\n\t\t\tP: "ReflectionFactor", "Number", "", "A",${fnum(m.metallic)}\n\t\t}\n\t}\n`);
    return mid;
  });
  const file = MATERIALS[0].baseMap, nm = file.replace(/\.png$/, ''), vid = id(), tid = id();
  add('Video', `\tVideo: ${vid}, "Video::${nm}", "Clip" {\n\t\tType: "Clip"\n\t\tProperties70:  {\n\t\t\tP: "Path", "KString", "XRefUrl", "", "${file}"\n\t\t}\n\t\tUseMipMap: 0\n\t\tFilename: "${file}"\n\t\tRelativeFilename: "${file}"\n\t}\n`);
  add('Texture', `\tTexture: ${tid}, "Texture::${nm}", "" {\n\t\tType: "TextureVideoClip"\n\t\tVersion: 202\n\t\tTextureName: "Texture::${nm}"\n\t\tProperties70:  {\n\t\t\tP: "UseMaterial", "bool", "", "",1\n\t\t}\n\t\tMedia: "Video::${nm}"\n\t\tFileName: "${file}"\n\t\tRelativeFilename: "${file}"\n\t\tModelUVTranslation: 0,0\n\t\tModelUVScaling: 1,1\n\t\tTexture_Alpha_Source: "None"\n\t\tCropping: 0,0,0,0\n\t}\n`);
  conns.push(`\tC: "OO",${vid},${tid}`);
  for (const mid of matIds) conns.push(`\tC: "OP",${tid},${mid}, "DiffuseColor"`);

  const modelIds = {};
  for (const part of ['Base', 'Lid']) {
    const { mesh, faces } = meshes[part], o = meshes[part].origin, mid = id(); modelIds[part] = mid;
    add('Model', `\tModel: ${mid}, "Model::${part}", "Mesh" {\n\t\tVersion: 232\n\t\tProperties70:  {\n\t\t\tP: "RotationActive", "bool", "", "",1\n\t\t\tP: "InheritType", "enum", "", "",1\n\t\t\tP: "ScalingMax", "Vector3D", "Vector", "",0,0,0\n\t\t\tP: "DefaultAttributeIndex", "int", "Integer", "",0\n` +
      `\t\t\tP: "Lcl Translation", "Lcl Translation", "", "A",${o.map(fnum).join(',')}\n\t\t\tP: "Lcl Rotation", "Lcl Rotation", "", "A",0,0,0\n\t\t\tP: "Lcl Scaling", "Lcl Scaling", "", "A",1,1,1\n\t\t}\n\t\tShading: Y\n\t\tCulling: "CullingOff"\n\t}\n`);
    conns.push(`\tC: "OO",${mid},0`);
    // pontos de controle deduplicados POR PEÇA (blocos encostados não se fundem; cada peça segue fechada no arquivo)
    const g = mesh.geometry, Pp = g.attributes.position, N = g.attributes.normal, T = g.attributes.uv;
    const key = new Map(), cp = [], pvi = [], nrm = [], uvk = new Map(), uv = [], uvi = [], matIdx = [];
    for (let i = 0; i < Pp.count; i++) {
      const k = `${faces[Math.floor(i / 3)].piece}|${Pp.getX(i).toFixed(5)},${Pp.getY(i).toFixed(5)},${Pp.getZ(i).toFixed(5)}`;
      let c = key.get(k); if (c === undefined) { c = cp.length / 3; key.set(k, c); cp.push(Pp.getX(i), Pp.getY(i), Pp.getZ(i)); }
      pvi.push(i % 3 === 2 ? -(c + 1) : c); nrm.push(N.getX(i), N.getY(i), N.getZ(i));
      const tk = `${T.getX(i).toFixed(4)},${T.getY(i).toFixed(4)}`; let t = uvk.get(tk); if (t === undefined) { t = uv.length / 2; uvk.set(tk, t); uv.push(T.getX(i), T.getY(i)); } uvi.push(t);
      if (i % 3 === 0) matIdx.push(faces[i / 3].material);
    }
    const gid = id();
    add('Geometry', `\tGeometry: ${gid}, "Geometry::${part}", "Mesh" {\n` + farr('Vertices', cp) + farr('PolygonVertexIndex', pvi, String) +
      `\t\tGeometryVersion: 124\n\t\tLayerElementNormal: 0 {\n\t\t\tVersion: 101\n\t\t\tName: ""\n\t\t\tMappingInformationType: "ByPolygonVertex"\n\t\t\tReferenceInformationType: "Direct"\n` + farr('Normals', nrm, fnum, '\t\t\t') + `\t\t}\n` +
      `\t\tLayerElementUV: 0 {\n\t\t\tVersion: 101\n\t\t\tName: "UVMap"\n\t\t\tMappingInformationType: "ByPolygonVertex"\n\t\t\tReferenceInformationType: "IndexToDirect"\n` + farr('UV', uv, fnum, '\t\t\t') + farr('UVIndex', uvi, String, '\t\t\t') + `\t\t}\n` +
      `\t\tLayerElementMaterial: 0 {\n\t\t\tVersion: 101\n\t\t\tName: ""\n\t\t\tMappingInformationType: "ByPolygon"\n\t\t\tReferenceInformationType: "IndexToDirect"\n` + farr('Materials', matIdx, String, '\t\t\t') + `\t\t}\n` +
      `\t\tLayer: 0 {\n\t\t\tVersion: 100\n\t\t\tLayerElement:  {\n\t\t\t\tType: "LayerElementNormal"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t\tLayerElement:  {\n\t\t\t\tType: "LayerElementMaterial"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t\tLayerElement:  {\n\t\t\t\tType: "LayerElementUV"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t}\n\t}\n`);
    conns.push(`\tC: "OO",${gid},${mid}`);
    for (const m of matIds) conns.push(`\tC: "OO",${m},${mid}`);   // ordem = índice do material por polígono (0 pedra, 1 latão)
    info[part] = { polygons: Pp.count / 3, controlPoints: cp.length / 3 };
  }

  // take Open: só Lcl Rotation do Lid (X animado; Y/Z constantes 0)
  const T = BigInt(frames.length - 1) * KTIME / BigInt(CLIP.fps);
  const sid = id(), lid = id();
  add('AnimationStack', `\tAnimationStack: ${sid}, "AnimStack::${CLIP.name}", "" {\n\t\tProperties70:  {\n\t\t\tP: "LocalStart", "KTime", "Time", "",0\n\t\t\tP: "LocalStop", "KTime", "Time", "",${T}\n\t\t\tP: "ReferenceStart", "KTime", "Time", "",0\n\t\t\tP: "ReferenceStop", "KTime", "Time", "",${T}\n\t\t}\n\t}\n`);
  add('AnimationLayer', `\tAnimationLayer: ${lid}, "AnimLayer::BaseLayer", "" {\n\t}\n`);
  conns.push(`\tC: "OO",${lid},${sid}`);
  const kt = frames.map(f => BigInt(f.frame) * KTIME / BigInt(CLIP.fps)), comps = [frames.map(f => f.deg), frames.map(() => 0), frames.map(() => 0)];
  const nid = id();
  add('AnimationCurveNode', `\tAnimationCurveNode: ${nid}, "AnimCurveNode::R", "" {\n\t\tProperties70:  {\n\t\t\tP: "d|X", "Number", "", "A",0\n\t\t\tP: "d|Y", "Number", "", "A",0\n\t\t\tP: "d|Z", "Number", "", "A",0\n\t\t}\n\t}\n`);
  conns.push(`\tC: "OO",${nid},${lid}`, `\tC: "OP",${nid},${modelIds.Lid}, "Lcl Rotation"`);
  ['X', 'Y', 'Z'].forEach((ax, c) => {
    const cid = id();
    add('AnimationCurve', `\tAnimationCurve: ${cid}, "AnimCurve::", "" {\n\t\tDefault: ${fnum(comps[c][0])}\n\t\tKeyVer: 4009\n` + farr('KeyTime', kt, String) + farr('KeyValueFloat', comps[c]) +
      `\t\tKeyAttrFlags: *1 {\n\t\t\ta: 24836\n\t\t} \n\t\tKeyAttrDataFloat: *4 {\n\t\t\ta: 0,0,255790911,0\n\t\t} \n\t\tKeyAttrRefCount: *1 {\n\t\t\ta: ${kt.length}\n\t\t} \n\t}\n`);
    conns.push(`\tC: "OP",${cid},${nid}, "d|${ax}"`);
  });

  const total = Object.values(counts).reduce((a, b) => a + b, 0) + 1;
  const header = `; FBX 7.4.0 project file\n; Gerado por Tools/Lemo3DGenerator/src/production/ModelChest.mjs (three.js do Lemo-Opuscar)\n; ----------------------------------------------------\n\n` +
    `FBXHeaderExtension:  {\n\tFBXHeaderVersion: 1003\n\tFBXVersion: 7400\n\tCreator: "Lemo3DGenerator ModelChest"\n}\n` +
    `GlobalSettings:  {\n\tVersion: 1000\n\tProperties70:  {\n\t\tP: "UpAxis", "int", "Integer", "",1\n\t\tP: "UpAxisSign", "int", "Integer", "",1\n\t\tP: "FrontAxis", "int", "Integer", "",2\n\t\tP: "FrontAxisSign", "int", "Integer", "",1\n\t\tP: "CoordAxis", "int", "Integer", "",0\n\t\tP: "CoordAxisSign", "int", "Integer", "",1\n\t\tP: "OriginalUpAxis", "int", "Integer", "",1\n\t\tP: "OriginalUpAxisSign", "int", "Integer", "",1\n\t\tP: "UnitScaleFactor", "double", "Number", "",100\n\t\tP: "OriginalUnitScaleFactor", "double", "Number", "",100\n` +
    `\t\tP: "TimeMode", "enum", "", "",6\n\t\tP: "TimeSpanStart", "KTime", "Time", "",0\n\t\tP: "TimeSpanStop", "KTime", "Time", "",${T}\n\t\tP: "CustomFrameRate", "double", "Number", "",${CLIP.fps}\n\t}\n}\n\n` +
    `Documents:  {\n\tCount: 1\n\tDocument: ${id()}, "Scene", "Scene" {\n\t\tProperties70:  {\n\t\t\tP: "SourceObject", "object", "", ""\n\t\t\tP: "ActiveAnimStackName", "KString", "", "", "${CLIP.name}"\n\t\t}\n\t\tRootNode: 0\n\t}\n}\n\nReferences:  {\n}\n\n` +
    `Definitions:  {\n\tVersion: 100\n\tCount: ${total}\n\tObjectType: "GlobalSettings" {\n\t\tCount: 1\n\t}\n` + Object.entries(counts).map(([k, n]) => `\tObjectType: "${k}" {\n\t\tCount: ${n}\n\t}\n`).join('') + `}\n\n`;
  const takes = `Takes:  {\n\tCurrent: "${CLIP.name}"\n\tTake: "${CLIP.name}" {\n\t\tFileName: "${CLIP.name}.tak"\n\t\tLocalTime: 0,${T}\n\t\tReferenceTime: 0,${T}\n\t}\n}\n`;
  return { text: header + `Objects:  {\n${objects.join('')}}\n\nConnections:  {\n${conns.join('\n')}\n}\n\n` + takes, info, ktimeStop: T.toString() };
}

// ———————————————————————— paleta PNG, metas ————————————————————————
const hex = h => [1, 3, 5].map(i => parseInt(h.slice(i, i + 2), 16));
function palettePNG() {
  const w = 16, h = 16, px = Buffer.alloc(w * h * 4), fill = hex(PALETTE.stoneDark.hex);
  for (let i = 0; i < w * h; i++) { px[i * 4] = fill[0]; px[i * 4 + 1] = fill[1]; px[i * 4 + 2] = fill[2]; px[i * 4 + 3] = 255; }   // células livres = pedra escura
  for (const sw of Object.values(PALETTE)) {
    const [r, g, b] = hex(sw.hex), [cx, cy] = sw.cell;
    for (let y = cy * 4; y < cy * 4 + 4; y++) for (let x = cx * 4; x < cx * 4 + 4; x++) { const o = (y * w + x) * 4; px[o] = r; px[o + 1] = g; px[o + 2] = b; }
  }
  return encodePNG(w, h, px);
}
export const stableGuid = key => createHash('sha256').update('Eldoria/A11/' + key).digest('hex').slice(0, 32);
function textureMeta(file) {
  let t = readFileSync(TEX_TEMPLATE, 'utf8').replace('{{GUID}}', stableGuid(file));
  const set = (k, v) => { const re = () => new RegExp(`(\\n[ \\t]*${k}: )[^\\r\\n]*`, 'g'); if (!re().test(t)) throw new Error(`template sem ${k}`); t = t.replace(re(), `$1${v}`); };
  set('enableMipMap', 0); set('sRGBTexture', 1); set('filterMode', 0); set('alphaUsage', 0); set('alphaIsTransparency', 0); set('textureCompression', 0); set('maxTextureSize', 32);
  return t;
}
// ModelImporter do Loot_XpOrb com animação ligada: rig Generic (como os personagens gerados), avatar criado do próprio modelo,
// take importado como clip padrão "Open" (sem loop). Sem colliders, escala 1, unidades do arquivo.
function modelMeta() {
  let t = readFileSync(FBX_TEMPLATE, 'utf8');
  const rep = (re, v) => { if (!re.test(t)) throw new Error(`template ModelImporter sem ${re}`); t = t.replace(re, v); };
  rep(/^guid: [0-9a-f]{32}$/m, `guid: ${stableGuid(NAME + '.fbx')}`);
  rep(/^(  importAnimation: )0\r?$/m, '$11');
  rep(/^(  animationType: )0\r?$/m, '$12');
  rep(/^(  avatarSetup: )0\r?$/m, '$11');
  if (!/addColliders: 0/.test(t) || !/globalScale: 1/.test(t) || !/clipAnimations: \[\]/.test(t)) throw new Error('template ModelImporter inesperado');
  return t;
}
const folderMeta = key => `fileFormatVersion: 2\nguid: ${stableGuid(key)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`;

// ———————————————————————— validação interna ————————————————————————
export function validate(meshes, frames) {
  const issues = [], out = { parts: {} };
  let total = 0; const all = new THREE.Box3();
  for (const part of ['Base', 'Lid']) {
    const { mesh, pieces, faces } = meshes[part], g = mesh.geometry, t = tris(g); total += t;
    const b = g.boundingBox.clone().translate(V(...meshes[part].origin)); all.union(b);
    for (const pc of pieces) if (pc.signedVolume <= 0) issues.push(`${part}/${pc.name}: volume ≤ 0`);
    const bySwatch = {}; for (const f of faces) bySwatch[f.swatch] = (bySwatch[f.swatch] || 0) + 1;
    out.parts[part] = { triangles: t, pieces: pieces.length, origin: meshes[part].origin, boundsLocal: { min: g.boundingBox.min.toArray().map(v => +v.toFixed(4)), max: g.boundingBox.max.toArray().map(v => +v.toFixed(4)) }, facesBySwatch: bySwatch, pieceList: pieces };
  }
  const size = all.getSize(V()), ctr = all.getCenter(V());
  if (total > BUDGET.triangles) issues.push(`${total} tris > ${BUDGET.triangles}`);
  if (Math.abs(all.min.y) > 1e-4) issues.push(`base não apoia em y = 0 (${all.min.y})`);
  if (Math.abs(ctr.x) > 0.01 || Math.abs(ctr.z) > 0.02) issues.push(`pivô longe do centro da base: ${ctr.toArray()}`);
  if (Math.abs(size.x - DIM.width) > 0.03 || Math.abs(size.y - DIM.height) > 0.02 || Math.abs(size.z - DIM.depth) > 0.05) issues.push(`dimensões ${size.toArray()} fora da ficha`);
  // tampa à frente do eixo (exceto os nós coaxiais) → nada dela entra na base ao abrir
  const lp = meshes.Lid.mesh.geometry.attributes.position, lf = meshes.Lid.faces; let minZ = Infinity, minY = Infinity;
  for (let i = 0; i < lp.count; i++) { const pc = lf[Math.floor(i / 3)].piece; if (pc.startsWith('hinge_lid')) continue; minZ = Math.min(minZ, lp.getZ(i)); minY = Math.min(minY, lp.getY(i)); }
  if (minZ < -1e-6) issues.push(`geometria da tampa atrás do eixo: z local ${minZ}`);
  if (Math.abs(minY) > 1e-6) issues.push(`fundo da tampa fora do eixo: y local ${minY}`);
  const last = frames[frames.length - 1];
  if (Math.abs(last.deg - CLIP.openDeg) > 1e-3 || frames[0].deg !== 0) issues.push('take não vai de 0° a −105°');
  let maxStep = 0; for (let i = 1; i < frames.length; i++) maxStep = Math.max(maxStep, Math.abs(frames[i].deg - frames[i - 1].deg));
  return { ok: !issues.length, issues, triangles: total, materials: MATERIALS.length, bounds: { min: all.min.toArray().map(v => +v.toFixed(4)), max: all.max.toArray().map(v => +v.toFixed(4)), size: size.toArray().map(v => +v.toFixed(4)), center: ctr.toArray().map(v => +v.toFixed(4)) }, lidLocalMinZExceptKnuckles: +minZ.toFixed(5), maxStepDegPerFrame: +maxStep.toFixed(3), ...out };
}

// ———————————————————————— principal ————————————————————————
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const meshes = build(), frames = bakeClip(), val = validate(meshes, frames);
  console.log(`${NAME}: ${val.triangles} tris (Base ${val.parts.Base.triangles}, Lid ${val.parts.Lid.triangles}), size ${JSON.stringify(val.bounds.size)}, centro ${JSON.stringify(val.bounds.center)}, passo máx ${val.maxStepDegPerFrame}°/quadro`);
  if (!val.ok) { console.error('FALHOU:\n  ' + val.issues.join('\n  ')); process.exit(1); }
  if (has('check')) { console.log('check OK'); process.exit(0); }
  mkdirSync(SRC, { recursive: true });
  const fbx = toFBX(meshes, frames);
  writeFileSync(join(OUT, `${NAME}.fbx`), fbx.text);
  writeFileSync(join(OUT, `${NAME}.fbx.meta`), modelMeta());
  writeFileSync(join(OUT, MATERIALS[0].baseMap), palettePNG());
  writeFileSync(join(OUT, MATERIALS[0].baseMap + '.meta'), textureMeta(MATERIALS[0].baseMap));
  writeFileSync(join(PROJECT, STAGING + '.meta'), folderMeta('folder/' + ID));
  // GLB de revisão com o clip Open (quaternion do Lid)
  const scene = new THREE.Group(); scene.name = NAME; scene.add(meshes.Base.mesh, meshes.Lid.mesh);
  const q = new THREE.Quaternion(), qv = [];
  for (const f of frames) { q.setFromEuler(new THREE.Euler(f.deg * Math.PI / 180, 0, 0)); qv.push(q.x, q.y, q.z, q.w); }
  const clip = new THREE.AnimationClip(CLIP.name, CLIP.duration, [new THREE.QuaternionKeyframeTrack('Lid.quaternion', frames.map(f => f.t), qv)]);
  const glb = Buffer.from(await new GLTFExporter().parseAsync(scene, { binary: true, onlyVisible: false, animations: [clip] }));
  writeFileSync(join(SRC, `${NAME}.glb`), glb);
  const source = {
    id: ID, batch: 'A11', name: NAME, generator: 'Tools/Lemo3DGenerator/src/production/ModelChest.mjs', three: THREE_VERSION, lemoRoot: LEMO_ROOT, node: process.version,
    units: 'metros, +Y para cima, +Z frente (ficha A11); Base com pivô na base central (origem); Lid com pivô no eixo da dobradiça traseira',
    dimensions: DIM, shape: SHAPE, hinge: HINGE, palette: PALETTE, materials: MATERIALS, clip: { ...CLIP, frames },
    parts: { Base: val.parts.Base, Lid: val.parts.Lid }, triangles: val.triangles, bounds: val.bounds,
    fbx: { file: `${NAME}.fbx`, models: ['Base', 'Lid'], info: fbx.info, materials: MATERIALS.map(m => m.name), textures: [MATERIALS[0].baseMap], takes: [CLIP.name], ktimeStop: fbx.ktimeStop },
    guids: { folder: stableGuid('folder/' + ID), fbx: stableGuid(NAME + '.fbx'), palette: stableGuid(MATERIALS[0].baseMap) },
    import: {
      model: 'ModelImporter copiado de Assets/_Project/Art/Items/Models/Loot_XpOrb.fbx.meta (Unity 6000.6, serializedVersion 24600): globalScale 1, useFileUnits, addColliders 0, normais importadas, materialImportMode 2; alterados só GUID, importAnimation 1, animationType 2 (Generic, como Art/Models/Generated/*) e avatarSetup 1. clipAnimations vazio: a Unity cria o clip padrão a partir do take "Open" (0–18 quadros, sem loop).',
      texture: 'Template FX_Spark (Tools/FxHealGenerator/texture.meta.template) com point, sem mipmaps, sem compressão, sem alpha, max 32, sRGB.',
    },
    integration_proposal: {
      status: 'NÃO INTEGRADO — tarefa Codex separada. RoomRewardChest.cs, RoomRewardPresenter.cs, RoomRewardSceneBuilder.cs, cenas, prefabs e materiais Mat_Chest_F08_* não foram alterados.',
      orientation: 'O baú F08 atual tem a frente em −Z local (Latch em z −0,46; dropOrigin em z −1,2; câmera em (0, 14, −14) olha para +Z). O modelo segue a ficha (+Z frente): o filho Visual deve ter localRotation Y = 180° sob a raiz do baú para a frente encarar a câmera e os pickups.',
      lid: 'Apontar RoomRewardPresenter.lid para o nó importado "Lid" (pivô no eixo, rotação de repouso 0). No espaço do modelo, Euler(−105, 0, 0) abre a frente para cima — é exatamente o valor que o presenter já aplica, então o estado visual binário funciona sem mudar código. O take Open (0,6 s) é opcional: Animator/clip sem eventos, disparado pelo evento Opened, nunca entregando recompensa; ao restaurar checkpoint (RestoreCheckpoint) aplicar a pose final direto, sem tocar o take.',
      replace: 'Desligar (não remover sem coordenação) Body/LidPivot/Latch de primitivas, ou criar variante/prefab novo pelo Editor via builder idempotente. Materiais URP Lit MODEL-CHEST_Stone (metallic 0, smoothness 0,12) e MODEL-CHEST_Brass (0,45/0,3), ambos com MODEL-CHEST_Palette.png em _BaseMap; uma instância compartilhada pelos cinco baús.',
      collider: 'Collider simples separado (BoxCollider ~1,2 × 0,8 × 0,7 m centrado em y 0,4) na raiz, se a regra de interação/obstrução pedir; o FBX não traz colisor. Manter fora de spawns, portas e NavMesh crítico (o baú atual está em sala + (3,5, 0, 2)).',
      tests: ['F perto/longe, sala bloqueada/limpa, pausa e jogador morto: só a apresentação muda', 'reabrir/reativar/reset de HP: tampa continua aberta sem novo sorteio', 'RestoreCheckpoint(true) → pose aberta sem take', 'pickups em dropOrigin não colidem com a tampa aberta (tampa recua até z ≈ −0,47 m do modelo, ou +0,47 m no espaço do baú atual)', 'leitura aberto/fechado na câmera real das cinco salas e no Boss'],
    },
    licence: 'Projeto Eldoria; geometria e animação próprias geradas por código (three.js r170 MIT, somente leitura no Lemo-Opuscar). Sem IA generativa nem asset de terceiros; desenho genérico, sem referência comercial.',
  };
  writeFileSync(join(SRC, `${NAME}.source.json`), JSON.stringify(source, null, 2));
  writeFileSync(join(SRC, 'ModelChest.generator.json'), JSON.stringify({ ...val, glbBytes: glb.length, fbxBytes: Buffer.byteLength(fbx.text) }, null, 2));
  console.log('OK →', relative(PROJECT, OUT));
}
