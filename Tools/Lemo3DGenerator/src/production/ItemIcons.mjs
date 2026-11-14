#!/usr/bin/env node
// ITEMS-COMBAT — ícones ICON-ITEM-* e texturas de FX, renderizados a partir da MESMA geometria de ItemModels.mjs.
//
// Fluxo: ItemModels.render.json → página three.js (Source~/icon-render/index.html) aberta pelo renderizador do Lemo
// (core/render/page.mjs: servidor local + Chromium headless do Playwright, SOMENTE LEITURA) → canvas WebGL com fundo
// transparente, supersampling 2× e contorno escuro por dilatação do alpha → PNG RGBA 512×512 com alpha real
// (toDataURL do canvas, não screenshot com fundo). Conteúdo cabe em 416×416 centrais (margem 48 px).
//
// Saídas em <staging>/Source~:
//   Icons/ICON-ITEM-<ID>.png      cinco ícones 512×512 RGBA (sem texto, número ou moldura de raridade)
//   Icons/review_32_48.png        prancha de revisão (ícones a 32/48 px sobre fundos claro/escuro/pedra) — com fundo, só revisão
//   Icons/models_review.png       prancha dos sete modelos de chão (com fundo, só revisão)
//   FX/FX_SlashTrail.png 256×64, FX/FX_Spark.png 64×64, FX/FX_Ring.png 256×256 (RGBA, brancos; cor aplicada no material)
//   ItemIcons.validation.json     verificação independente: PNG decodificado do disco (dimensão, alpha, margens)
//
// Uso: node src/production/ItemIcons.mjs [--out <pasta>]   (rode antes ItemModels.mjs)
import { mkdirSync, writeFileSync, readFileSync, existsSync } from 'node:fs';
import { join, resolve, dirname, relative } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { deflateSync, inflateSync } from 'node:zlib';
import { LEMO_ROOT, THREE_VERSION } from '../lemo-three.mjs';
import { STAGING } from './ItemModels.mjs';

const HERE = dirname(fileURLToPath(import.meta.url)), LAB = resolve(HERE, '..', '..'), PROJECT = resolve(LAB, '..', '..');
const argv = process.argv.slice(2), arg = (k, d) => { const i = argv.indexOf('--' + k); return i >= 0 ? argv[i + 1] : d; };
const OUT = resolve(arg('out', join(PROJECT, STAGING, 'Source~')));

// ID do ícone → modelo e pose (graus, ordem XYZ aplicada ao modelo; câmera ortográfica frontal levemente elevada).
export const ICONS = [
  { id: 'ICON-ITEM-IRON', model: 'Item_IronSword', pose: { pre: [-90, 0, 0], rot: [0, 28, -42] }, elev: 12 },
  { id: 'ICON-ITEM-WARDEN', model: 'Item_WardenBlade', pose: { pre: [-90, 0, 0], rot: [0, 28, -42] }, elev: 12 },
  { id: 'ICON-ITEM-ANCIENT', model: 'Item_AncientEdge', pose: { pre: [-90, 0, 0], rot: [0, 28, -42] }, elev: 12 },
  { id: 'ICON-ITEM-LEATHER', model: 'Item_LeatherVest', pose: { pre: [0, 0, 0], rot: [0, -24, 0] }, elev: 14 },
  { id: 'ICON-ITEM-HEALTH', model: 'Item_HealthPotion', pose: { pre: [0, 0, 0], rot: [0, 22, 0] }, elev: 14 },
];
const REVIEW_MODELS = ['Item_IronSword', 'Item_WardenBlade', 'Item_AncientEdge', 'Item_LeatherVest', 'Item_HealthPotion', 'Loot_Gold', 'Loot_XpOrb'];
export const SIZE = 512, CONTENT = 416, OUTLINE = 9;

// ———————————————————————— PNG (codificação/decodificação mínima, RGBA 8 bits) ————————————————————————
const CRC = (() => { const t = new Uint32Array(256); for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xEDB88320 ^ (c >>> 1) : c >>> 1; t[n] = c >>> 0; } return t; })();
const crc32 = buf => { let c = 0xFFFFFFFF; for (const b of buf) c = CRC[(c ^ b) & 255] ^ (c >>> 8); return (c ^ 0xFFFFFFFF) >>> 0; };
const chunk = (type, data) => { const len = Buffer.alloc(4); len.writeUInt32BE(data.length); const td = Buffer.concat([Buffer.from(type), data]); const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td)); return Buffer.concat([len, td, crc]); };
export function encodePNG(w, h, rgba) {
  const raw = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) { raw[y * (w * 4 + 1)] = 0; rgba.copy ? rgba.copy(raw, y * (w * 4 + 1) + 1, y * w * 4, (y + 1) * w * 4) : raw.set(rgba.subarray(y * w * 4, (y + 1) * w * 4), y * (w * 4 + 1) + 1); }
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 6; ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', deflateSync(raw, { level: 9 })), chunk('IEND', Buffer.alloc(0))]);
}
export function decodePNG(buf) {
  let p = 8, w = 0, h = 0, colorType = 0, depth = 0; const idat = [];
  while (p < buf.length) {
    const len = buf.readUInt32BE(p), type = buf.toString('ascii', p + 4, p + 8), data = buf.subarray(p + 8, p + 8 + len);
    if (type === 'IHDR') { w = data.readUInt32BE(0); h = data.readUInt32BE(4); depth = data[8]; colorType = data[9]; }
    else if (type === 'IDAT') idat.push(data);
    p += 12 + len;
  }
  if (depth !== 8 || colorType !== 6) return { w, h, colorType, depth, rgba: null };
  const raw = inflateSync(Buffer.concat(idat)), stride = w * 4, out = Buffer.alloc(stride * h);
  for (let y = 0; y < h; y++) {
    const f = raw[y * (stride + 1)], line = raw.subarray(y * (stride + 1) + 1, (y + 1) * (stride + 1));
    for (let x = 0; x < stride; x++) {
      const a = x >= 4 ? out[y * stride + x - 4] : 0, b = y > 0 ? out[(y - 1) * stride + x] : 0, c = x >= 4 && y > 0 ? out[(y - 1) * stride + x - 4] : 0;
      let v = line[x];
      if (f === 1) v += a; else if (f === 2) v += b; else if (f === 3) v += (a + b) >> 1;
      else if (f === 4) { const pp = a + b - c, pa = Math.abs(pp - a), pb = Math.abs(pp - b), pc = Math.abs(pp - c); v += pa <= pb && pa <= pc ? a : pb <= pc ? b : c; }
      out[y * stride + x] = v & 255;
    }
  }
  return { w, h, colorType, depth, rgba: out };
}
export function alphaStats({ w, h, rgba }) {
  let transparent = 0, opaque = 0, partial = 0, minX = w, minY = h, maxX = -1, maxY = -1;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const a = rgba[(y * w + x) * 4 + 3];
    if (a === 0) transparent++; else { if (a === 255) opaque++; else partial++; if (x < minX) minX = x; if (y < minY) minY = y; if (x > maxX) maxX = x; if (y > maxY) maxY = y; }
  }
  const n = w * h;
  return { transparentPct: +(100 * transparent / n).toFixed(2), opaquePct: +(100 * opaque / n).toFixed(2), partialPct: +(100 * partial / n).toFixed(2),
    content: maxX < 0 ? null : { x: minX, y: minY, w: maxX - minX + 1, h: maxY - minY + 1 },
    corners: [rgba[3], rgba[(w - 1) * 4 + 3], rgba[((h - 1) * w) * 4 + 3], rgba[(h * w - 1) * 4 + 3]] };
}

// ———————————————————————— texturas de FX (procedurais, brancas; cor e blend no material URP) ————————————————————————
function fxTexture(w, h, fn) {
  const px = Buffer.alloc(w * h * 4);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const a = Math.max(0, Math.min(1, fn((x + .5) / w, (y + .5) / h))), i = (y * w + x) * 4;
    px[i] = px[i + 1] = px[i + 2] = 255; px[i + 3] = Math.round(a * 255);
  }
  return encodePNG(w, h, px);
}
const ss = (a, b, x) => { const t = Math.min(1, Math.max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t); };
export const FX = {
  // TrailRenderer (textureMode Stretch): u = 0 cabeça (lâmina) → 1 cauda; v = largura. Núcleo fino no fio, cauda curta.
  'FX_SlashTrail.png': [256, 64, (u, v) => { const across = Math.pow(Math.sin(Math.PI * v), .6) * (.35 + .65 * ss(.15, .85, v)); return across * Math.pow(1 - u, 1.6) * ss(0, .04, u); }],
  // Faísca/lasca: disco suave.
  'FX_Spark.png': [64, 64, (u, v) => { const d = Math.hypot(u - .5, v - .5) * 2; return Math.pow(1 - ss(0, 1, d), 1.5); }],
  // Anel do giro: borda fina com centro vazio (diferente do disco vermelho de telegraph hostil).
  'FX_Ring.png': [256, 256, (u, v) => { const d = Math.hypot(u - .5, v - .5) * 2; return Math.exp(-Math.pow((d - .9) / .045, 2)) + .25 * Math.exp(-Math.pow((d - .8) / .09, 2)) * (d < .9 ? 1 : 0); }],
};

// ———————————————————————— página do renderizador ————————————————————————
const PAGE = String.raw`<!doctype html><html><head><meta charset="utf-8"><style>html,body{margin:0;background:transparent}</style></head><body>
<script type="importmap">{"imports":{"three":"/node_modules/three/build/three.module.js","three/addons/":"/node_modules/three/examples/jsm/"}}</script>
<script type="module">
import * as THREE from 'three';
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js';
const SIZE = __SIZE__, CONTENT = __CONTENT__, OUTLINE = __OUTLINE__, SS = 2;
const data = await (await fetch('./data.json')).json();
const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true, preserveDrawingBuffer: true, premultipliedAlpha: false });
renderer.setPixelRatio(1); renderer.setSize(SIZE * SS, SIZE * SS); renderer.setClearColor(0x000000, 0);
renderer.outputColorSpace = THREE.SRGBColorSpace; renderer.toneMapping = THREE.NoToneMapping;
document.body.appendChild(renderer.domElement);
const pmrem = new THREE.PMREMGenerator(renderer), env = pmrem.fromScene(new RoomEnvironment(), .04).texture;
function scene() {
  const s = new THREE.Scene(); s.environment = env;
  s.add(new THREE.HemisphereLight(0xe6eeff, 0x3a3040, 1.25));
  const key = new THREE.DirectionalLight(0xfff4e0, 2.4); key.position.set(-1.4, 2.2, 2.2); s.add(key);
  const rim = new THREE.DirectionalLight(0x9fe6ff, 1.1); rim.position.set(1.8, 1.2, -1.6); s.add(rim);
  return s;
}
function model(name) {
  const g = new THREE.Group();
  for (const grp of data.models[name].groups) {
    const geo = new THREE.BufferGeometry();
    geo.setAttribute('position', new THREE.Float32BufferAttribute(grp.positions, 3));
    geo.setAttribute('normal', new THREE.Float32BufferAttribute(grp.normals, 3));
    const mat = new THREE.MeshStandardMaterial({ color: grp.color, emissive: grp.emissive, emissiveIntensity: grp.emissiveIntensity,
      metalness: Math.min(grp.metallic, .5), roughness: Math.max(grp.roughness, .35), flatShading: true, envMapIntensity: .9 });
    g.add(new THREE.Mesh(geo, mat));
  }
  return g;
}
const D = Math.PI / 180;
function shot(name, pose, elev, bg) {
  const s = scene(), m = model(name);
  const inner = new THREE.Group(); inner.add(m); m.rotation.set(...pose.pre.map(v => v * D));
  inner.rotation.set(...pose.rot.map(v => v * D)); s.add(inner);
  inner.updateMatrixWorld(true);
  const cam = new THREE.OrthographicCamera(-1, 1, 1, -1, -10, 10);
  cam.position.set(0, Math.sin(elev * D), Math.cos(elev * D)); cam.lookAt(0, 0, 0); cam.updateMatrixWorld(true);
  // enquadra pelos vértices projetados (caixa exata da silhueta)
  const v = new THREE.Vector3(); let x0 = 1e9, x1 = -1e9, y0 = 1e9, y1 = -1e9;
  m.traverse(o => { if (!o.isMesh) return; const p = o.geometry.attributes.position; for (let i = 0; i < p.count; i++) { v.fromBufferAttribute(p, i).applyMatrix4(o.matrixWorld).applyMatrix4(cam.matrixWorldInverse); x0 = Math.min(x0, v.x); x1 = Math.max(x1, v.x); y0 = Math.min(y0, v.y); y1 = Math.max(y1, v.y); } });
  const fit = (CONTENT - 2 * OUTLINE) / SIZE, half = Math.max(x1 - x0, y1 - y0) / fit / 2, cx = (x0 + x1) / 2, cy = (y0 + y1) / 2;
  cam.left = cx - half; cam.right = cx + half; cam.top = cy + half; cam.bottom = cy - half; cam.updateProjectionMatrix();
  renderer.setClearColor(bg ?? 0x000000, bg === undefined ? 0 : 1);
  renderer.render(s, cam);
  // reduz 2× e compõe o contorno (dilatação do alpha) atrás do item
  const c = document.createElement('canvas'); c.width = c.height = SIZE; const x = c.getContext('2d', { willReadFrequently: true });
  x.imageSmoothingEnabled = true; x.imageSmoothingQuality = 'high'; x.drawImage(renderer.domElement, 0, 0, SIZE, SIZE);
  if (bg !== undefined) return c.toDataURL('image/png');
  const img = x.getImageData(0, 0, SIZE, SIZE), a = new Float32Array(SIZE * SIZE);
  for (let i = 0; i < a.length; i++) a[i] = img.data[i * 4 + 3] / 255;
  const dil = new Float32Array(a.length), r = OUTLINE, offs = [];
  for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++) { const d = Math.hypot(dx, dy); if (d <= r + .5) offs.push([dx, dy, Math.min(1, r + .5 - d)]); }
  for (let y = 0; y < SIZE; y++) for (let xx = 0; xx < SIZE; xx++) {
    let m2 = 0; for (const [dx, dy, w] of offs) { const X = xx + dx, Y = y + dy; if (X < 0 || Y < 0 || X >= SIZE || Y >= SIZE) continue; const val = a[Y * SIZE + X] * w; if (val > m2) { m2 = val; if (m2 >= 1) break; } }
    dil[y * SIZE + xx] = m2;
  }
  const OC = [16, 12, 22], OA = .94;
  for (let i = 0; i < a.length; i++) {
    const fa = a[i], oa = dil[i] * OA, outA = fa + oa * (1 - fa);
    if (outA <= 0) { img.data[i * 4 + 3] = 0; continue; }
    for (let k = 0; k < 3; k++) img.data[i * 4 + k] = Math.round((img.data[i * 4 + k] * fa + OC[k] * oa * (1 - fa)) / outA);
    img.data[i * 4 + 3] = Math.round(outA * 255);
  }
  x.putImageData(img, 0, 0);
  return c.toDataURL('image/png');
}
window.icon = (name, pose, elev) => shot(name, pose, elev);
window.still = (name, pose, elev, bg) => shot(name, pose, elev, bg);
window.render = () => {};
window.READY = true;
</script></body></html>`;

// ———————————————————————— principal ————————————————————————
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const renderJson = join(OUT, 'ItemModels.render.json');
  if (!existsSync(renderJson)) { console.error('Rode antes: node src/production/ItemModels.mjs'); process.exit(1); }
  const models = JSON.parse(readFileSync(renderJson, 'utf8'));
  const pageDir = join(OUT, 'icon-render'), iconDir = join(OUT, 'Icons'), fxDir = join(OUT, 'FX');
  for (const d of [pageDir, iconDir, fxDir]) mkdirSync(d, { recursive: true });
  writeFileSync(join(pageDir, 'data.json'), JSON.stringify({ models }));
  writeFileSync(join(pageDir, 'index.html'), PAGE.replace('__SIZE__', SIZE).replace('__CONTENT__', CONTENT).replace('__OUTLINE__', OUTLINE));

  const { openDemo, closeServer } = await import(pathToFileURL(join(LEMO_ROOT, 'core', 'render', 'page.mjs')).href);
  const { browser, page } = await openDemo(pageDir, { w: 64, h: 64 });
  const validation = { batch: 'ITEMS-COMBAT', generator: 'Tools/Lemo3DGenerator/src/production/ItemIcons.mjs', lemo: { root: LEMO_ROOT, three: THREE_VERSION, renderer: 'core/render/page.mjs (Playwright Chromium headless), somente leitura' }, size: SIZE, content: CONTENT, outline: OUTLINE, icons: [], fx: [], issues: [] };
  const decode = url => Buffer.from(url.split(',')[1], 'base64');
  for (const ic of ICONS) {
    if (!models[ic.model]) { validation.issues.push(`modelo ausente ${ic.model}`); continue; }
    const png = decode(await page.evaluate(({ m, p, e }) => window.icon(m, p, e), { m: ic.model, p: ic.pose, e: ic.elev }));
    const file = join(iconDir, `${ic.id}.png`); writeFileSync(file, png);
    const dec = decodePNG(readFileSync(file)), st = alphaStats(dec);
    const margin = st.content ? Math.min(st.content.x, st.content.y, SIZE - st.content.x - st.content.w, SIZE - st.content.y - st.content.h) : -1;
    const ok = dec.w === SIZE && dec.h === SIZE && dec.colorType === 6 && st.transparentPct > 20 && st.corners.every(a => a === 0) && margin >= (SIZE - CONTENT) / 2 - 2;
    if (!ok) validation.issues.push(`${ic.id}: falhou (margem ${margin}, ${JSON.stringify(st)})`);
    validation.icons.push({ id: ic.id, model: ic.model, file: relative(PROJECT, file).replace(/\\/g, '/'), width: dec.w, height: dec.h, rgba: dec.colorType === 6, ...st, minMargin: margin, ok, pose: ic.pose });
    console.log(`${ic.id.padEnd(18)} ${dec.w}x${dec.h} RGBA=${dec.colorType === 6} transp ${st.transparentPct}% margem ${margin}px ${ok ? 'OK' : 'FALHOU'}`);
  }
  // pranchas de revisão (com fundo; não são ícones)
  const reviewPage = await browser.newPage({ viewport: { width: 1200, height: 900 } });
  const icons = ICONS.map(ic => ({ id: ic.id, url: 'data:image/png;base64,' + readFileSync(join(iconDir, `${ic.id}.png`)).toString('base64') }));
  const stills = [];
  for (const name of REVIEW_MODELS) {
    const sword = name.includes('Sword') || name.includes('Blade') || name.includes('Edge');
    stills.push({ name, url: await page.evaluate(({ m, p, e }) => window.still(m, p, e, 0x2b2e38), { m: name, p: sword ? { pre: [-90, 0, 0], rot: [0, 28, -42] } : { pre: [0, 0, 0], rot: [0, 25, 0] }, e: sword ? 12 : 30 }) });
  }
  await reviewPage.setContent(`<html><body style="margin:0;font:14px sans-serif;color:#ddd;background:#111">
    <div id="a" style="padding:12px;display:grid;grid-template-columns:repeat(5,1fr);gap:10px">${icons.map(i => `<div>${i.id}<div style="display:flex;gap:6px;margin-top:6px">${['#e8e4dc', '#14161c', '#55596b'].map(bg => `<div style="background:${bg};padding:6px"><img src="${i.url}" width="48" height="48"><img src="${i.url}" width="32" height="32"></div>`).join('')}</div><img src="${i.url}" width="160" style="background:repeating-conic-gradient(#888 0 25%,#666 0 50%) 0/16px 16px;margin-top:6px"></div>`).join('')}</div>
    <div id="b" style="padding:12px;display:grid;grid-template-columns:repeat(4,1fr);gap:10px">${stills.map(s => `<div>${s.name}<br><img src="${s.url}" width="260"></div>`).join('')}</div></body></html>`);
  await reviewPage.waitForTimeout(200);
  await (await reviewPage.$('#a')).screenshot({ path: join(iconDir, 'review_32_48.png') });
  await (await reviewPage.$('#b')).screenshot({ path: join(iconDir, 'models_review.png') });
  await browser.close(); closeServer();

  for (const [name, [w, h, fn]] of Object.entries(FX)) {
    const file = join(fxDir, name); writeFileSync(file, fxTexture(w, h, fn));
    const dec = decodePNG(readFileSync(file)), st = alphaStats(dec);
    validation.fx.push({ file: relative(PROJECT, file).replace(/\\/g, '/'), width: dec.w, height: dec.h, rgba: dec.colorType === 6, transparentPct: st.transparentPct });
    console.log(`${name.padEnd(18)} ${dec.w}x${dec.h} transp ${st.transparentPct}%`);
  }
  validation.ok = validation.issues.length === 0; validation.date = new Date().toISOString();
  writeFileSync(join(OUT, 'ItemIcons.validation.json'), JSON.stringify(validation, null, 2));
  if (!validation.ok) { console.error('FALHOU:\n  ' + validation.issues.join('\n  ')); process.exit(1); }
  console.log('OK →', relative(PROJECT, OUT));
}
