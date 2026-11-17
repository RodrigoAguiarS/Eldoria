#!/usr/bin/env node
// A01 — ICON-ACTION-DASH: ícone de HUD do dash existente (dobra de capa azul com chevron de espaço negativo).
//
// Mesma família e mesmo renderizador de ItemIcons.mjs (ITEMS-COMBAT): geometria low-poly facetada, three.js r170 do Lemo
// (SOMENTE LEITURA) em Chromium headless, câmera ortográfica 3/4, mesma luz (hemisférica + key quente + rim ciano),
// supersampling 2×, contorno escuro por dilatação do alpha, PNG RGBA 512×512 com alpha real (toDataURL, não screenshot).
//
// FONTE EDITÁVEL: os dados abaixo (CAPE: contorno 2D, furo chevron, profundidade de cada vértice, faixa ciano e fecho)
// são a fonte do ícone. Edite-os e rode de novo; o script também grava ICON-ACTION-DASH.source.json (malha triangulada)
// e ICON-ACTION-DASH.silhouette.svg (desenho 2D plano) em Source~ para revisão/edição externa.
//
// Saídas:
//   <staging>/ICON-ACTION-DASH.png                 ícone final 512×512 RGBA (importado pela Unity com a .meta da ficha)
//   <staging>/Source~/review_32_48.png             prancha: 32/48 px sobre claro/escuro/pedra, tons de cinza, família ITEMS-COMBAT
//   <staging>/Source~/ICON-ACTION-DASH.source.json malha (posições/normais/cores) e parâmetros de câmera
//   <staging>/Source~/ICON-ACTION-DASH.silhouette.svg
//   <staging>/Source~/DashIcon.validation.json     PNG decodificado do disco: dimensão, tipo de cor, alpha, margens, cantos
//
// Uso: node src/production/DashIcon.mjs [--out <pasta>]
import { mkdirSync, writeFileSync, readFileSync, existsSync } from 'node:fs';
import { join, resolve, dirname, relative } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { LEMO_ROOT, THREE, THREE_VERSION } from '../lemo-three.mjs';
import { decodePNG, alphaStats, SIZE, CONTENT, OUTLINE } from './ItemIcons.mjs';

const HERE = dirname(fileURLToPath(import.meta.url)), LAB = resolve(HERE, '..', '..'), PROJECT = resolve(LAB, '..', '..');
export const STAGING = 'Assets/_Project/Art/ProductionStaging/ICON-ACTION-DASH';
const argv = process.argv.slice(2), arg = (k, d) => { const i = argv.indexOf('--' + k); return i >= 0 ? argv[i + 1] : d; };
const OUT = resolve(arg('out', join(PROJECT, STAGING)));
const ID = 'ICON-ACTION-DASH';

// ———————————————————————— fonte do desenho ————————————————————————
// Plano 2D 0..100 (x → direita = sentido do dash, y ↑). z = profundidade da dobra (positivo = em direção à câmera).
// A capa nasce presa no fecho (direita) e se abre para trás (esquerda) com três pontas na barra, como a capa do Hero.
// As dobras irradiam do fecho: pontas da barra são cristas (z+), reentrâncias são vales (z−). O chevron ">" é furo.
export const CAPE = {
  vertices: {
    K1: [84, 72, 3],   K2: [90, 60, -3],                       // fecho (topo/base)
    T1: [64, 85, 18],  T2: [40, 89, -8],  tipA: [5, 81, 14],    // borda superior → ponta superior
    N1: [31, 65, -16], tipB: [0, 52, 20], N2: [29, 38, -16],   // barra: reentrância, ponta do meio, reentrância
    tipC: [5, 13, 12], B1: [34, 17, 14],  B2: [58, 29, -10], B3: [77, 45, 9], // ponta inferior → barra inferior
    // chevron (espaço negativo): ponta externa O, braço superior Uo/Ui, vértice interno I, braço inferior Li/Lo
    O: [74, 58, 8], Uo: [47, 80, 6], Ui: [33, 72, -9], I: [51, 58, 0], Li: [33, 44, -6], Lo: [47, 36, 9],
  },
  // facetas (ordem livre; o script orienta para a câmera)
  faces: [
    ['K1', 'T1', 'O'], ['T1', 'Uo', 'O'], ['T1', 'T2', 'Uo'], ['T2', 'tipA', 'Uo'], ['tipA', 'Ui', 'Uo'], ['tipA', 'N1', 'Ui'],
    ['Ui', 'N1', 'I'], ['N1', 'tipB', 'I'], ['tipB', 'Li', 'I'], ['tipB', 'N2', 'Li'],
    ['N2', 'tipC', 'Li'], ['tipC', 'B1', 'Li'], ['B1', 'Lo', 'Li'], ['B1', 'B2', 'Lo'], ['B2', 'B3', 'Lo'], ['B3', 'O', 'Lo'],
    ['B3', 'K2', 'O'], ['K2', 'K1', 'O'],
  ],
  outline: ['K1', 'T1', 'T2', 'tipA', 'N1', 'tipB', 'N2', 'tipC', 'B1', 'B2', 'B3', 'K2'],
  hole: ['O', 'Uo', 'Ui', 'I', 'Li', 'Lo'],
  // faixa de luz ciano ao longo da borda superior (do fecho para trás) e largura
  trim: { path: ['K1', 'T1', 'T2', 'tipA'], width: 4.5, lift: .4 },
  // fecho facetado (prende a capa — leitura de "capa", não de asa)
  clasp: { at: [86, 66], r: 4.2, z: 3 },
  // albedo do tecido mais escuro que #2F6BFF de propósito: sob a luz compartilhada com ITEMS-COMBAT, a média das facetas
  // iluminadas volta a ~#2F6BFF (medido em DashIcon.validation.json → clothMeanRGB); sombra puxada para #1E1830.
  colors: { cloth: 0x2350d8, inner: 0x1e1830, trim: 0x4fd8ff, clasp: 0x8a93a0 },
};
// pose do grupo (graus XYZ) e elevação da câmera ortográfica (mesma convenção dos ícones de item)
export const POSE = { rot: [-8, -18, -4], elev: 12 };

// ———————————————————————— malha ————————————————————————
export function tri(out, a, b, c) { out.push(...a, ...b, ...c); }
export function flatNormals(pos) {
  const n = [];
  for (let i = 0; i < pos.length; i += 9) {
    const ax = pos[i + 3] - pos[i], ay = pos[i + 4] - pos[i + 1], az = pos[i + 5] - pos[i + 2];
    const bx = pos[i + 6] - pos[i], by = pos[i + 7] - pos[i + 1], bz = pos[i + 8] - pos[i + 2];
    let x = ay * bz - az * by, y = az * bx - ax * bz, z = ax * by - ay * bx; const l = Math.hypot(x, y, z) || 1;
    x /= l; y /= l; z /= l; n.push(x, y, z, x, y, z, x, y, z);
  }
  return n;
}
export const area2 = (a, b, c) => (b[0] - a[0]) * (c[1] - a[1]) - (c[0] - a[0]) * (b[1] - a[1]);
export function buildMesh(src = CAPE) {
  const P = src.vertices, front = [], back = [];
  for (const f of src.faces) {
    let [a, b, c] = f.map(k => { if (!P[k]) throw new Error(`vértice desconhecido ${k}`); return P[k]; });
    if (area2(a, b, c) < 0) [b, c] = [c, b];   // anti-horário no plano = face para +z (câmera)
    tri(front, a, b, c); tri(back, a, c, b);
  }
  // faixa ciano: polilinha deslocada para dentro (à esquerda do percurso anti-horário) e levemente à frente
  const t = src.trim, line = t.path.map(k => P[k]), trim = [];
  const inward = line.map((p, i) => {
    const prev = line[Math.max(0, i - 1)], next = line[Math.min(line.length - 1, i + 1)];
    let dx = next[0] - prev[0], dy = next[1] - prev[1]; const l = Math.hypot(dx, dy) || 1; dx /= l; dy /= l;
    return [-dy, dx];
  });
  for (let i = 0; i < line.length - 1; i++) {
    const p0 = line[i], p1 = line[i + 1], n0 = inward[i], n1 = inward[i + 1];
    const w0 = i === 0 ? t.width * .4 : t.width, w1 = i + 1 === line.length - 1 ? t.width * .2 : t.width;
    const a = [p0[0], p0[1], p0[2] + t.lift], b = [p1[0], p1[1], p1[2] + t.lift];
    const c = [p1[0] + n1[0] * w1, p1[1] + n1[1] * w1, p1[2] + t.lift], d = [p0[0] + n0[0] * w0, p0[1] + n0[1] * w0, p0[2] + t.lift];
    for (const [x, y, z] of [[a, c, b], [a, d, c]]) { if (area2(x, y, z) < 0) tri(trim, x, z, y); else tri(trim, x, y, z); }
  }
  // fecho: hexágono facetado em pirâmide baixa
  const k = src.clasp, clasp = [], top = [k.at[0], k.at[1], k.z + 1.6];
  const ring = Array.from({ length: 6 }, (_, i) => { const a = Math.PI / 6 + i * Math.PI / 3; return [k.at[0] + Math.cos(a) * k.r, k.at[1] + Math.sin(a) * k.r, k.z]; });
  for (let i = 0; i < 6; i++) tri(clasp, ring[i], ring[(i + 1) % 6], top);

  const group = (positions, color, emissive = 0x000000, emissiveIntensity = 0, metallic = 0, roughness = .9) =>
    ({ positions, normals: flatNormals(positions), color, emissive, emissiveIntensity, metallic, roughness });
  return {
    groups: [
      group(front, src.colors.cloth),
      group(back, src.colors.inner),
      group(trim, src.colors.trim, src.colors.trim, .15),
      group(clasp, src.colors.clasp, 0, 0, .3, .55),
    ],
    triangles: { cloth: src.faces.length, trim: trim.length / 9, clasp: 6 },
  };
}

function silhouetteSVG(src = CAPE) {
  const P = src.vertices, pts = ks => ks.map(k => `${P[k][0]},${100 - P[k][1]}`).join(' L');
  return `<?xml version="1.0" encoding="UTF-8"?>
<!-- ${ID}: desenho 2D plano da fonte (contorno + furo chevron + arestas das facetas). Profundidade z e luz ficam em DashIcon.mjs. -->
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="512" height="512">
  <path fill="#2F6BFF" fill-rule="evenodd" d="M${pts(src.outline)} Z M${pts(src.hole)} Z"/>
  <g fill="none" stroke="#1E1830" stroke-width=".3">${src.faces.map(f => `<path d="M${pts(f)} Z"/>`).join('')}</g>
  <path fill="none" stroke="#4FD8FF" stroke-width="1.6" d="M${pts(src.trim.path)}"/>
  <circle cx="${src.clasp.at[0]}" cy="${100 - src.clasp.at[1]}" r="${src.clasp.r}" fill="#8A93A0"/>
</svg>
`;
}

// ———————————————————————— página do renderizador (luz/contorno idênticos a ItemIcons.mjs) ————————————————————————
export const PAGE = String.raw`<!doctype html><html><head><meta charset="utf-8"><style>html,body{margin:0;background:transparent}</style></head><body>
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
function model() {
  const g = new THREE.Group();
  for (const grp of data.mesh.groups) {
    const geo = new THREE.BufferGeometry();
    geo.setAttribute('position', new THREE.Float32BufferAttribute(grp.positions, 3));
    geo.setAttribute('normal', new THREE.Float32BufferAttribute(grp.normals, 3));
    const mat = new THREE.MeshStandardMaterial({ color: grp.color, emissive: grp.emissive, emissiveIntensity: grp.emissiveIntensity,
      metalness: Math.min(grp.metallic, .5), roughness: Math.max(grp.roughness, .35), flatShading: true, envMapIntensity: .9 });
    g.add(new THREE.Mesh(geo, mat));
  }
  // centraliza no plano e reduz para ~2 unidades (só para a câmera; proporções intactas)
  g.position.set(-50, -50, 0); const wrap = new THREE.Group(); wrap.add(g); wrap.scale.setScalar(.02);
  return wrap;
}
const D = Math.PI / 180;
function shot(pose, elev, bg) {
  const s = scene(), m = model();
  const inner = new THREE.Group(); inner.add(m); inner.rotation.set(...pose.rot.map(v => v * D)); s.add(inner);
  inner.updateMatrixWorld(true);
  const cam = new THREE.OrthographicCamera(-1, 1, 1, -1, -10, 10);
  cam.position.set(0, Math.sin(elev * D), Math.cos(elev * D)); cam.lookAt(0, 0, 0); cam.updateMatrixWorld(true);
  const v = new THREE.Vector3(); let x0 = 1e9, x1 = -1e9, y0 = 1e9, y1 = -1e9;
  m.traverse(o => { if (!o.isMesh) return; const p = o.geometry.attributes.position; for (let i = 0; i < p.count; i++) { v.fromBufferAttribute(p, i).applyMatrix4(o.matrixWorld).applyMatrix4(cam.matrixWorldInverse); x0 = Math.min(x0, v.x); x1 = Math.max(x1, v.x); y0 = Math.min(y0, v.y); y1 = Math.max(y1, v.y); } });
  const fit = (CONTENT - 2 * OUTLINE) / SIZE, half = Math.max(x1 - x0, y1 - y0) / fit / 2, cx = (x0 + x1) / 2, cy = (y0 + y1) / 2;
  cam.left = cx - half; cam.right = cx + half; cam.top = cy + half; cam.bottom = cy - half; cam.updateProjectionMatrix();
  renderer.setClearColor(bg ?? 0x000000, bg === undefined ? 0 : 1);
  renderer.render(s, cam);
  const c = document.createElement('canvas'); c.width = c.height = SIZE; const x = c.getContext('2d', { willReadFrequently: true });
  x.imageSmoothingEnabled = true; x.imageSmoothingQuality = 'high'; x.drawImage(renderer.domElement, 0, 0, SIZE, SIZE);
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
    if (outA <= 0) { img.data[i * 4] = img.data[i * 4 + 1] = img.data[i * 4 + 2] = img.data[i * 4 + 3] = 0; continue; }
    for (let k = 0; k < 3; k++) img.data[i * 4 + k] = Math.round((img.data[i * 4 + k] * fa + OC[k] * oa * (1 - fa)) / outA);
    img.data[i * 4 + 3] = Math.round(outA * 255);
  }
  x.putImageData(img, 0, 0);
  return c.toDataURL('image/png');
}
window.icon = (pose, elev) => shot(pose, elev);
window.render = () => {};
window.READY = true;
</script></body></html>`;

// ———————————————————————— principal ————————————————————————
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const src = join(OUT, 'Source~'), pageDir = join(src, 'icon-render');
  for (const d of [OUT, src, pageDir]) mkdirSync(d, { recursive: true });
  const mesh = buildMesh();
  writeFileSync(join(pageDir, 'data.json'), JSON.stringify({ mesh }));
  writeFileSync(join(pageDir, 'index.html'), PAGE.replace('__SIZE__', SIZE).replace('__CONTENT__', CONTENT).replace('__OUTLINE__', OUTLINE));
  writeFileSync(join(src, `${ID}.source.json`), JSON.stringify({ id: ID, generator: 'Tools/Lemo3DGenerator/src/production/DashIcon.mjs', units: 'plano 0..100, z = profundidade da dobra', cape: CAPE, pose: POSE, mesh }, null, 1));
  writeFileSync(join(src, `${ID}.silhouette.svg`), silhouetteSVG());

  const { openDemo, closeServer } = await import(pathToFileURL(join(LEMO_ROOT, 'core', 'render', 'page.mjs')).href);
  const { browser, page } = await openDemo(pageDir, { w: 64, h: 64 });
  const png = Buffer.from((await page.evaluate(({ p, e }) => window.icon(p, e), { p: POSE, e: POSE.elev })).split(',')[1], 'base64');
  const file = join(OUT, `${ID}.png`); writeFileSync(file, png);

  const dec = decodePNG(readFileSync(file)), st = alphaStats(dec);
  const margin = st.content ? Math.min(st.content.x, st.content.y, SIZE - st.content.x - st.content.w, SIZE - st.content.y - st.content.h) : -1;
  // franja: pixels totalmente transparentes devem ter RGB zerado (sem cor de fundo vazando em filtragem bilinear)
  let dirtyTransparent = 0; for (let i = 0; i < dec.rgba.length; i += 4) if (dec.rgba[i + 3] === 0 && (dec.rgba[i] | dec.rgba[i + 1] | dec.rgba[i + 2])) dirtyTransparent++;
  // cor média do tecido renderizado (pixels opacos azuis, fora do contorno escuro)
  const cm = [0, 0, 0]; let cn = 0;
  for (let i = 0; i < dec.rgba.length; i += 4) { const [r, g, b, a] = dec.rgba.subarray(i, i + 4); if (a === 255 && b > 90 && b > r + 40) { cm[0] += r; cm[1] += g; cm[2] += b; cn++; } }
  const clothMeanRGB = cm.map(v => Math.round(v / Math.max(1, cn)));
  const issues = [];
  if (dec.w !== SIZE || dec.h !== SIZE) issues.push(`dimensão ${dec.w}x${dec.h}`);
  if (dec.colorType !== 6) issues.push(`tipo de cor ${dec.colorType} (esperado RGBA)`);
  if (!(st.transparentPct > 20)) issues.push(`transparência ${st.transparentPct}%`);
  if (!st.corners.every(a => a === 0)) issues.push(`cantos não transparentes ${st.corners}`);
  if (margin < (SIZE - CONTENT) / 2 - 2) issues.push(`margem ${margin}px < ${(SIZE - CONTENT) / 2 - 2}`);
  if (dirtyTransparent) issues.push(`${dirtyTransparent} pixels transparentes com RGB não nulo`);

  // prancha de revisão (com fundo; não é ícone)
  const url = 'data:image/png;base64,' + png.toString('base64');
  const fam = ['ICON-ITEM-IRON', 'ICON-ITEM-LEATHER', 'ICON-ITEM-HEALTH'].map(id => {
    const f = join(PROJECT, 'Assets/_Project/Art/Items/Icons', `${id}.png`);
    return existsSync(f) ? { id, url: 'data:image/png;base64,' + readFileSync(f).toString('base64') } : null;
  }).filter(Boolean);
  const bgs = ['#e8e4dc', '#14161c', '#55596b'];
  const cell = (u, grey) => bgs.map(bg => `<div style="background:${bg};padding:6px;display:flex;gap:6px;align-items:end"><img src="${u}" width="48" height="48" style="${grey ? 'filter:grayscale(1)' : ''}"><img src="${u}" width="32" height="32" style="${grey ? 'filter:grayscale(1)' : ''}"></div>`).join('');
  const review = await browser.newPage({ viewport: { width: 1100, height: 700 } });
  await review.setContent(`<html><body style="margin:0;font:13px sans-serif;color:#ddd;background:#111"><div id="r" style="padding:12px;display:flex;flex-direction:column;gap:10px">
    <div style="display:flex;gap:16px;align-items:start">
      <div>${ID} — cor<div style="display:flex;gap:6px;margin-top:6px">${cell(url, false)}</div>
      <div style="margin-top:8px">tons de cinza</div><div style="display:flex;gap:6px;margin-top:6px">${cell(url, true)}</div></div>
      <div>512 px sobre xadrez (só revisão)<br><img src="${url}" width="200" style="margin-top:6px;background:repeating-conic-gradient(#888 0 25%,#666 0 50%) 0/16px 16px"></div>
    </div>
    <div>família ITEMS-COMBAT a 48 px (escuro / pedra)<div style="display:flex;gap:6px;margin-top:6px">${['#14161c', '#55596b'].map(bg => `<div style="background:${bg};padding:6px;display:flex;gap:8px"><img src="${url}" width="48">${fam.map(f => `<img src="${f.url}" width="48">`).join('')}</div>`).join('')}</div></div>
  </div></body></html>`);
  await review.waitForTimeout(200);
  await (await review.$('#r')).screenshot({ path: join(src, 'review_32_48.png') });
  await browser.close(); closeServer();

  const validation = {
    id: ID, generator: 'Tools/Lemo3DGenerator/src/production/DashIcon.mjs',
    lemo: { root: LEMO_ROOT, three: THREE_VERSION, renderer: 'core/render/page.mjs (Playwright Chromium headless), somente leitura' },
    file: relative(PROJECT, file).replace(/\\/g, '/'), width: dec.w, height: dec.h, rgba: dec.colorType === 6, ...st, minMargin: margin,
    dirtyTransparentPixels: dirtyTransparent, clothMeanRGB, clothMeanHex: '#' + clothMeanRGB.map(v => v.toString(16).padStart(2, '0')).join('').toUpperCase(), triangles: mesh.triangles, pose: POSE, size: SIZE, contentArea: CONTENT, outline: OUTLINE,
    issues, ok: issues.length === 0, date: new Date().toISOString(),
  };
  writeFileSync(join(src, 'DashIcon.validation.json'), JSON.stringify(validation, null, 2));
  console.log(`${ID} ${dec.w}x${dec.h} RGBA=${dec.colorType === 6} transp ${st.transparentPct}% conteúdo ${JSON.stringify(st.content)} margem ${margin}px ${validation.ok ? 'OK' : 'FALHOU'}`);
  if (!validation.ok) { console.error('FALHOU:\n  ' + issues.join('\n  ')); process.exit(1); }
}
