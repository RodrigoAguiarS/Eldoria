#!/usr/bin/env node
// A02 — ICON-ACTION-SPECIAL: ícone de HUD do giro de espada existente (espada curta de aço dentro de um arco ciano).
//
// Mesma família e mesmo renderizador do A01 (DashIcon.mjs) e de ItemIcons.mjs: geometria low-poly facetada, three.js r170
// do Lemo (SOMENTE LEITURA) em Chromium headless, câmera ortográfica 3/4, mesma luz, supersampling 2×, contorno escuro por
// dilatação do alpha, PNG RGBA 512×512 com alpha real (toDataURL, não screenshot). A página de render é a de DashIcon.mjs.
//
// FONTE EDITÁVEL: SWEEP (arco), SWORD (espada) e POSE abaixo. Edite e rode de novo; o script também grava
// ICON-ACTION-SPECIAL.source.json (malha triangulada + parâmetros) e ICON-ACTION-SPECIAL.silhouette.svg em Source~.
//
// Saídas:
//   <staging>/ICON-ACTION-SPECIAL.png                 ícone final 512×512 RGBA
//   <staging>/Source~/review_32_48.png                prancha: A01 × A02 a 48/32 px em claro/escuro/pedra, cor e cinza, família
//   <staging>/Source~/ICON-ACTION-SPECIAL.source.json
//   <staging>/Source~/ICON-ACTION-SPECIAL.silhouette.svg
//   <staging>/Source~/SpinIcon.validation.json        PNG decodificado do disco: dimensão, tipo de cor, alpha, margens, cantos
//
// Uso: node src/production/SpinIcon.mjs [--out <pasta>]
import { mkdirSync, writeFileSync, readFileSync, existsSync } from 'node:fs';
import { join, resolve, dirname, relative } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { LEMO_ROOT, THREE_VERSION } from '../lemo-three.mjs';
import { decodePNG, alphaStats, SIZE, CONTENT, OUTLINE } from './ItemIcons.mjs';
import { PAGE, flatNormals, area2, tri, POSE as DASH_POSE, STAGING as DASH_STAGING } from './DashIcon.mjs';

const HERE = dirname(fileURLToPath(import.meta.url)), LAB = resolve(HERE, '..', '..'), PROJECT = resolve(LAB, '..', '..');
export const STAGING = 'Assets/_Project/Art/ProductionStaging/ICON-ACTION-SPECIAL';
const argv = process.argv.slice(2), arg = (k, d) => { const i = argv.indexOf('--' + k); return i >= 0 ? argv[i + 1] : d; };
const OUT = resolve(arg('out', join(PROJECT, STAGING)));
const ID = 'ICON-ACTION-SPECIAL';

// ———————————————————————— fonte do desenho ————————————————————————
// Plano 2D 0..100 (x → direita, y ↑), z = profundidade (positivo = em direção à câmera). Ângulos em graus, 0 = direita, anti-horário.
// O giro roda no sentido anti-horário: a ponta da espada (tip) lidera e o arco é o rastro que ela deixou para trás.
// A cabeça do arco (larga, corte inclinado) fica sob a ponta; a cauda afina até um fio. O vão entre cauda e cabeça e o centro
// vazado separam a leitura de "giro" do disco fechado (telegraph hostil) e da cunha do dash (A01).
export const SWEEP = {
  center: [50, 50],
  rIn: 32, rOut: 47,          // faixa larga do arco (15 unidades ≈ 60 px a 512)
  head: 52,                   // ângulo da cabeça (sob a ponta da espada)
  headLead: 9,                // borda externa da cabeça adiantada (corte inclinado = sentido do giro)
  span: 292,                  // extensão do rastro, no sentido horário a partir da cabeça
  segments: 18,               // facetas ao longo do arco
  taperFrom: .5,              // fração do arco onde a cauda começa a afinar
  fold: [2.6, -2.2],          // z alternado (crista/vale) das bordas externa/interna → facetas de energia, não disco liso
  // albedo mais escuro que #4FD8FF de propósito (mesma correção do A01): sob a luz da família a média volta a ~#4FD8FF
  colors: { front: 0x22a6cc, back: 0x123848, edge: 0x0f5a74 },
  emissive: .18,
  innerEdge: 2.4,             // filete escuro na borda interna (separa o arco do centro vazado em tons de cinza)
};

// Espada curta e larga, mesma família da Espada de Ferro (ItemModels.mjs: lâmina reta de aço, canal escuro, guarda reta de
// latão, cabo de couro com anéis, pomo em disco), redesenhada em facetas planas na escala do ícone. Eixo u (comprimento,
// a partir do pomo) e v (largura); colocada na diagonal com o centro em SWEEP.center.
export const SWORD = {
  angle: 52,                  // direção pomo → ponta (alinhada com a cabeça do arco)
  length: 74, centerU: 35,    // u do ponto que fica sobre o centro do arco
  z: 7,                       // acima do arco
  pommel: { u: 4.5, r: 5.2 },
  grip: { u0: 8.5, u1: 20, half: 3.1, rings: [12.2, 16.8] },
  guard: { u0: 20, u1: 25.5, half: 14.5, cap: 2.2 },
  blade: { u0: 25.5, shoulder: 61, tip: 74, half: 7.2, halfShoulder: 6.4, ridge: 3.2 },
  fuller: { u0: 28, u1: 55, half: 1.5 },
  backing: 1.5,               // contorno escuro da espada sobre o arco (o contorno do alpha só pega a borda externa)
  colors: { steel: 0xb9c4cf, steelDark: 0x6f7b88, brass: 0xe0b040, leather: 0x4e3020, backing: 0x100c16 },
};
// mesma pose e elevação do A01 (família ICON-ACTION)
export const POSE = { rot: [...DASH_POSE.rot], elev: DASH_POSE.elev };

// ———————————————————————— malha ————————————————————————
const D = Math.PI / 180;
const lerp = (a, b, t) => a + (b - a) * t;
const smooth = t => { t = Math.min(1, Math.max(0, t)); return t * t * (3 - 2 * t); };
function face(out, a, b, c) { if (area2(a, b, c) < 0) tri(out, a, c, b); else tri(out, a, b, c); }      // frente para +z
function backFace(out, a, b, c) { if (area2(a, b, c) < 0) tri(out, a, b, c); else tri(out, a, c, b); }  // verso
function quad(out, a, b, c, d) { face(out, a, b, c); face(out, a, c, d); }

function buildSweep(s = SWEEP) {
  const [cx, cy] = s.center, front = [], back = [], edge = [], polar = (r, deg, z) => [cx + r * Math.cos(deg * D), cy + r * Math.sin(deg * D), z];
  const outer = [], inner = [], innerLip = [];
  for (let i = 0; i <= s.segments; i++) {
    const t = i / s.segments, ang = s.head - t * s.span, k = smooth((t - s.taperFrom) / (1 - s.taperFrom));
    const ro = lerp(s.rOut, s.rOut - 2.5, k), ri = lerp(s.rIn, ro - .01, k);             // cauda afina pela borda interna
    const zo = (i % 2 ? s.fold[0] : -s.fold[0] * .4) * (1 - .6 * k), zi = (i % 2 ? s.fold[1] * .4 : s.fold[1] * -1) * (1 - .6 * k);
    const lead = i === 0 ? s.headLead : 0;
    outer.push(polar(ro, ang + lead, zo)); inner.push(polar(ri, ang, zi));
    const lip = Math.min(s.innerEdge, (ro - ri) * .35);
    innerLip.push(polar(ri + lip, ang, zi + .35));
  }
  for (let i = 0; i < s.segments; i++) {
    const o0 = outer[i], o1 = outer[i + 1], i0 = innerLip[i], i1 = innerLip[i + 1];
    // diagonal alternada → facetas em zigue-zague
    if (i % 2) { face(front, o0, o1, i1); face(front, o0, i1, i0); } else { face(front, o0, o1, i0); face(front, i0, o1, i1); }
    backFace(back, o0, o1, i1); backFace(back, o0, i1, i0);
    const a = inner[i], b = inner[i + 1];
    if (Math.hypot(b[0] - innerLip[i + 1][0], b[1] - innerLip[i + 1][1]) > .05 || i < s.segments - 1) quad(edge, a, b, innerLip[i + 1], innerLip[i]);
  }
  return { front, back, edge, outline: [...outer, ...inner.slice().reverse()] };
}

function buildSword(w = SWORD) {
  const [cx, cy] = SWEEP.center, ca = Math.cos(w.angle * D), sa = Math.sin(w.angle * D);
  const P = (u, v, z = 0) => { const uu = u - w.centerU; return [cx + uu * ca - v * sa, cy + uu * sa + v * ca, w.z + z]; };
  const steel = [], dark = [], brass = [], leather = [], backing = [];
  const B = w.blade, G = w.guard, H = w.grip, F = w.fuller;
  // lâmina: seção em losango (aresta central elevada), ombro e ponta
  const ridge = [P(B.u0, 0, B.ridge), P(B.shoulder, 0, B.ridge), P(B.tip, 0, .6)];
  for (const side of [1, -1]) {
    const e0 = P(B.u0, side * B.half, 0), e1 = P(B.shoulder, side * B.halfShoulder, 0);
    quad(steel, e0, e1, ridge[1], ridge[0]); face(steel, e1, ridge[2], ridge[1]);
  }
  // canal escuro sobre a aresta (levemente à frente para não brigar com a lâmina)
  const fz = B.ridge + .25, fuller = [P(F.u0, -F.half * .6, fz), P(F.u0, F.half * .6, fz), P(F.u1, F.half, fz), P(F.u1 + 3, 0, fz), P(F.u1, -F.half, fz)];
  face(dark, fuller[0], fuller[1], fuller[2]); face(dark, fuller[0], fuller[2], fuller[4]); face(dark, fuller[4], fuller[2], fuller[3]);
  // guarda reta com pontas um pouco mais grossas; aresta central elevada
  for (const side of [1, -1]) {
    const um = (G.u0 + G.u1) / 2, inner = side * (B.half + .5), outer = side * G.half, cap = side * (G.half - G.cap * 2.2);
    const a = P(G.u0, 0, 2.2), b = P(G.u1, 0, 2.2), c = P(G.u1, cap, 1.4), d = P(G.u1 + .9, outer, .8), e = P(um, outer + side * .6, 1.2), f = P(G.u0 - .9, outer, .8), g = P(G.u0, cap, 1.4);
    quad(brass, a, b, c, g); face(brass, g, c, e); face(brass, c, d, e); face(brass, g, e, f);
    void inner;
  }
  // cabo: dois planos com aresta central; anéis de latão
  for (const side of [1, -1]) quad(leather, P(H.u0, side * H.half * .85, 0), P(H.u1, side * H.half, 0), P(H.u1, 0, 1.6), P(H.u0, 0, 1.6));
  for (const u of H.rings) for (const side of [1, -1]) quad(brass, P(u - .9, side * (H.half + .5), .4), P(u + .9, side * (H.half + .5), .4), P(u + .9, 0, 2.1), P(u - .9, 0, 2.1));
  // pomo: hexágono em pirâmide baixa
  const pc = P(w.pommel.u, 0, 2.6), ring = Array.from({ length: 6 }, (_, i) => { const a = (i * 60 + 30) * D; return P(w.pommel.u + Math.cos(a) * w.pommel.r, Math.sin(a) * w.pommel.r, .4); });
  for (let i = 0; i < 6; i++) face(brass, ring[i], ring[(i + 1) % 6], pc);
  // contorno da espada: cada peça (pomo, cabo, guarda, lâmina) vira um polígono convexo deslocado para fora, logo atrás da
  // espada; a união segue a silhueta real sem preencher os vãos entre guarda e cabo.
  const o = w.backing, bz = -1.2, um = (G.u0 + G.u1) / 2;
  const pieces = [
    Array.from({ length: 6 }, (_, i) => { const a = (i * 60 + 30) * D; return [w.pommel.u + Math.cos(a) * (w.pommel.r + o), Math.sin(a) * (w.pommel.r + o)]; }),
    [[H.u0 - o, -H.half - o], [G.u0, -H.half - o], [G.u0, H.half + o], [H.u0 - o, H.half + o]],
    [[G.u0 - .9 - o, -G.half - o * .6], [um, -G.half - .6 - o], [G.u1 + .9 + o, -G.half - o * .6], [G.u1 + o, -B.half - o], [G.u1 + o, B.half + o],
      [G.u1 + .9 + o, G.half + o * .6], [um, G.half + .6 + o], [G.u0 - .9 - o, G.half + o * .6]],
    [[G.u1, -B.half - o], [B.shoulder, -B.halfShoulder - o], [B.tip + o * 1.6, 0], [B.shoulder, B.halfShoulder + o], [G.u1, B.half + o]],
  ];
  for (const poly of pieces) { const c = poly.reduce((s, p) => [s[0] + p[0] / poly.length, s[1] + p[1] / poly.length], [0, 0]); for (let i = 0; i < poly.length; i++) face(backing, P(...poly[i], bz), P(...poly[(i + 1) % poly.length], bz), P(...c, bz)); }
  const hull = pieces.map(poly => poly.map(p => P(...p, bz)));
  return { steel, dark, brass, leather, backing, hull, tip: P(B.tip, 0) };
}

export function buildMesh() {
  const sw = buildSweep(), sd = buildSword(), S = SWEEP.colors, C = SWORD.colors;
  const group = (positions, color, emissive = 0x000000, emissiveIntensity = 0, metallic = 0, roughness = .9) =>
    ({ positions, normals: flatNormals(positions), color, emissive, emissiveIntensity, metallic, roughness });
  return {
    groups: [
      group(sw.front, S.front, S.front, SWEEP.emissive, 0, .7),
      group(sw.back, S.back),
      group(sw.edge, S.edge, 0, 0, 0, .8),
      group(sd.backing, C.backing, 0, 0, 0, 1),
      group(sd.steel, C.steel, 0, 0, .6, .45),
      group(sd.dark, C.steelDark, 0, 0, .6, .55),
      group(sd.brass, C.brass, 0, 0, .7, .5),
      group(sd.leather, C.leather, 0, 0, 0, .75),
    ],
    triangles: Object.fromEntries(Object.entries({ sweepFront: sw.front, sweepBack: sw.back, sweepEdge: sw.edge, backing: sd.backing, steel: sd.steel, fuller: sd.dark, brass: sd.brass, leather: sd.leather }).map(([k, v]) => [k, v.length / 9])),
    outline: { sweep: sw.outline, sword: sd.hull },
  };
}

function silhouetteSVG(mesh) {
  const pts = a => a.map(p => `${p[0].toFixed(2)},${(100 - p[1]).toFixed(2)}`).join(' L');
  const tris = (pos, fill) => { let s = ''; for (let i = 0; i < pos.length; i += 9) s += `<path d="M${pos[i].toFixed(2)},${(100 - pos[i + 1]).toFixed(2)} L${pos[i + 3].toFixed(2)},${(100 - pos[i + 4]).toFixed(2)} L${pos[i + 6].toFixed(2)},${(100 - pos[i + 7]).toFixed(2)} Z"/>`; return `<g fill="${fill}" stroke="#1E1830" stroke-width=".12">${s}</g>`; };
  const g = mesh.groups, hex = c => '#' + c.toString(16).padStart(6, '0').toUpperCase();
  return `<?xml version="1.0" encoding="UTF-8"?>
<!-- ${ID}: desenho 2D plano da fonte (arco + espada, com as arestas das facetas). Profundidade z, luz e contorno ficam em SpinIcon.mjs. -->
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="512" height="512">
  <path fill="#4FD8FF" d="M${pts(mesh.outline.sweep)} Z"/>
  ${tris(g[0].positions, '#4FD8FF')}
  ${tris(g[2].positions, hex(g[2].color))}
  ${mesh.outline.sword.map(p => `<path fill="#100C16" d="M${pts(p)} Z"/>`).join('')}
  ${[4, 5, 6, 7].map(i => tris(g[i].positions, hex(g[i].color))).join('\n  ')}
</svg>
`;
}

// ———————————————————————— principal ————————————————————————
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const src = join(OUT, 'Source~'), pageDir = join(src, 'icon-render');
  for (const d of [OUT, src, pageDir]) mkdirSync(d, { recursive: true });
  const mesh = buildMesh();
  writeFileSync(join(pageDir, 'data.json'), JSON.stringify({ mesh: { groups: mesh.groups } }));
  writeFileSync(join(pageDir, 'index.html'), PAGE.replace('__SIZE__', SIZE).replace('__CONTENT__', CONTENT).replace('__OUTLINE__', OUTLINE));
  writeFileSync(join(src, `${ID}.source.json`), JSON.stringify({ id: ID, generator: 'Tools/Lemo3DGenerator/src/production/SpinIcon.mjs', units: 'plano 0..100, z = profundidade', sweep: SWEEP, sword: SWORD, pose: POSE, mesh }, null, 1));
  writeFileSync(join(src, `${ID}.silhouette.svg`), silhouetteSVG(mesh));

  const { openDemo, closeServer } = await import(pathToFileURL(join(LEMO_ROOT, 'core', 'render', 'page.mjs')).href);
  const { browser, page } = await openDemo(pageDir, { w: 64, h: 64 });
  const png = Buffer.from((await page.evaluate(({ p, e }) => window.icon(p, e), { p: POSE, e: POSE.elev })).split(',')[1], 'base64');
  const file = join(OUT, `${ID}.png`); writeFileSync(file, png);

  const dec = decodePNG(readFileSync(file)), st = alphaStats(dec);
  const margin = st.content ? Math.min(st.content.x, st.content.y, SIZE - st.content.x - st.content.w, SIZE - st.content.y - st.content.h) : -1;
  let dirtyTransparent = 0; for (let i = 0; i < dec.rgba.length; i += 4) if (dec.rgba[i + 3] === 0 && (dec.rgba[i] | dec.rgba[i + 1] | dec.rgba[i + 2])) dirtyTransparent++;
  // médias renderizadas por material (pixels opacos classificados por matiz) — conferência das cores da ficha
  const acc = { cyan: [0, 0, 0, 0], steel: [0, 0, 0, 0], brass: [0, 0, 0, 0] };
  for (let i = 0; i < dec.rgba.length; i += 4) {
    const [r, g, b, a] = dec.rgba.subarray(i, i + 4); if (a !== 255) continue;
    const mx = Math.max(r, g, b), mn = Math.min(r, g, b);
    const k = (b > 120 && g > 100 && b > r + 60 && g > r + 40) ? 'cyan' : (mx > 110 && mx - mn < 40 && b >= r) ? 'steel' : (r > 140 && r > b + 70 && g > b + 30) ? 'brass' : null;
    if (k) { acc[k][0] += r; acc[k][1] += g; acc[k][2] += b; acc[k][3]++; }
  }
  const hex = c => '#' + c.map(v => v.toString(16).padStart(2, '0')).join('').toUpperCase();
  const means = Object.fromEntries(Object.entries(acc).map(([k, [r, g, b, n]]) => { const c = [r, g, b].map(v => Math.round(v / Math.max(1, n))); return [k, { rgb: c, hex: hex(c), pixels: n }]; }));
  // abertura central: o centro do arco deve ficar transparente fora da espada (amostra num anel interno, longe da lâmina)
  const issues = [];
  if (dec.w !== SIZE || dec.h !== SIZE) issues.push(`dimensão ${dec.w}x${dec.h}`);
  if (dec.colorType !== 6) issues.push(`tipo de cor ${dec.colorType} (esperado RGBA)`);
  if (!(st.transparentPct > 20)) issues.push(`transparência ${st.transparentPct}%`);
  if (!st.corners.every(a => a === 0)) issues.push(`cantos não transparentes ${st.corners}`);
  if (margin < (SIZE - CONTENT) / 2 - 2) issues.push(`margem ${margin}px < ${(SIZE - CONTENT) / 2 - 2}`);
  if (dirtyTransparent) issues.push(`${dirtyTransparent} pixels transparentes com RGB não nulo`);

  // prancha de revisão (com fundo; não é ícone): A01 × A02 lado a lado
  const url = 'data:image/png;base64,' + png.toString('base64');
  const dashFile = join(PROJECT, DASH_STAGING, 'ICON-ACTION-DASH.png');
  const dash = existsSync(dashFile) ? 'data:image/png;base64,' + readFileSync(dashFile).toString('base64') : null;
  const fam = ['ICON-ITEM-IRON', 'ICON-ITEM-LEATHER', 'ICON-ITEM-HEALTH'].map(id => {
    const f = join(PROJECT, 'Assets/_Project/Art/Items/Icons', `${id}.png`);
    return existsSync(f) ? 'data:image/png;base64,' + readFileSync(f).toString('base64') : null;
  }).filter(Boolean);
  const bgs = ['#e8e4dc', '#14161c', '#55596b'];
  const pair = grey => bgs.map(bg => `<div style="background:${bg};padding:6px;display:flex;gap:6px;align-items:end">${[dash, url].filter(Boolean).map(u => `<img src="${u}" width="48" height="48" style="${grey ? 'filter:grayscale(1)' : ''}"><img src="${u}" width="32" height="32" style="${grey ? 'filter:grayscale(1)' : ''}">`).join('<span style="width:6px"></span>')}</div>`).join('');
  const review = await browser.newPage({ viewport: { width: 1200, height: 760 } });
  await review.setContent(`<html><body style="margin:0;font:13px sans-serif;color:#ddd;background:#111"><div id="r" style="padding:12px;display:flex;flex-direction:column;gap:10px">
    <div style="display:flex;gap:16px;align-items:start">
      <div>A01 dash × A02 giro — cor (48 / 32 px)<div style="display:flex;gap:6px;margin-top:6px">${pair(false)}</div>
      <div style="margin-top:8px">tons de cinza</div><div style="display:flex;gap:6px;margin-top:6px">${pair(true)}</div></div>
      <div>${ID} 512 px sobre xadrez (só revisão)<br><img src="${url}" width="220" style="margin-top:6px;background:repeating-conic-gradient(#888 0 25%,#666 0 50%) 0/16px 16px"></div>
    </div>
    <div>família ICON-ACTION + ITEMS-COMBAT a 48 px (escuro / pedra)<div style="display:flex;gap:6px;margin-top:6px">${['#14161c', '#55596b'].map(bg => `<div style="background:${bg};padding:6px;display:flex;gap:8px">${[dash, url, ...fam].filter(Boolean).map(u => `<img src="${u}" width="48">`).join('')}</div>`).join('')}</div></div>
  </div></body></html>`);
  await review.waitForTimeout(200);
  await (await review.$('#r')).screenshot({ path: join(src, 'review_32_48.png') });
  await browser.close(); closeServer();

  const validation = {
    id: ID, generator: 'Tools/Lemo3DGenerator/src/production/SpinIcon.mjs',
    lemo: { root: LEMO_ROOT, three: THREE_VERSION, renderer: 'core/render/page.mjs (Playwright Chromium headless), somente leitura; página de DashIcon.mjs' },
    file: relative(PROJECT, file).replace(/\\/g, '/'), width: dec.w, height: dec.h, rgba: dec.colorType === 6, ...st, minMargin: margin,
    dirtyTransparentPixels: dirtyTransparent, renderedMeans: means, triangles: mesh.triangles, pose: POSE, size: SIZE, contentArea: CONTENT, outline: OUTLINE,
    issues, ok: issues.length === 0, date: new Date().toISOString(),
  };
  writeFileSync(join(src, 'SpinIcon.validation.json'), JSON.stringify(validation, null, 2));
  console.log(`${ID} ${dec.w}x${dec.h} RGBA=${dec.colorType === 6} transp ${st.transparentPct}% conteúdo ${JSON.stringify(st.content)} margem ${margin}px médias ${JSON.stringify(Object.fromEntries(Object.entries(means).map(([k, v]) => [k, v.hex])))} ${validation.ok ? 'OK' : 'FALHOU'}`);
  if (!validation.ok) { console.error('FALHOU:\n  ' + issues.join('\n  ')); process.exit(1); }
}
