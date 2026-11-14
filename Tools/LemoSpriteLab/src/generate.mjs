#!/usr/bin/env node
// LemoSpriteLab — gera frames PNG, spritesheets, metadados JSON e prévias de um personagem.
//   node src/generate.mjs [--character hero] [--out <pasta Assets>] [--preview <pasta>]
import { mkdirSync, writeFileSync, readFileSync } from 'node:fs';
import { join, resolve, relative, dirname } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { loadLemo, mirrorPX } from './lemo-bridge.mjs';
import { encodePNG, encodeAPNG, fromPX, upscale, newImage, blit, fillRect } from './png.mjs';

const HERE = dirname(fileURLToPath(import.meta.url));
const LAB = resolve(HERE, '..');
const PROJECT = resolve(LAB, '..', '..');
const args = Object.fromEntries(process.argv.slice(2).reduce((a, v, i, all) => (v.startsWith('--') ? [...a, [v.slice(2), all[i + 1]]] : a), []));
const charId = args.character || 'hero';
const OUT_ROOT = resolve(args.out || join(PROJECT, 'Assets/_Project/Art/Sprites/Prototypes'));
const PREVIEW_ROOT = resolve(args.preview || join(LAB, 'preview'));

const lemo = await loadLemo();
const mod = await import(pathToFileURL(join(HERE, 'characters', `${charId}.mjs`)).href);
const ch = mod.createHero ? mod.createHero(lemo) : mod.create(lemo);
const { meta, ANIMATIONS, DIRECTIONS } = ch;
const N = meta.logical, K = meta.scale, FS = N * K;   // 64 × 2 = 128

const write = (p, buf) => { mkdirSync(dirname(p), { recursive: true }); writeFileSync(p, buf); };
const pad2 = i => String(i).padStart(2, '0');

function render(dir, pose) {
  const P = new lemo.PX(N, N);
  ch.drawFrame(P, Q => mirrorPX(lemo, Q), dir, pose);
  P.outline(ch.outlineK, ch.outlineTint);   // contorno "selout" do Lemo
  return upscale(fromPX(P), K);
}
function bbox(img) {
  let x0 = 1e9, y0 = 1e9, x1 = -1, y1 = -1;
  for (let y = 0; y < img.height; y++) for (let x = 0; x < img.width; x++) if (img.data[(y * img.width + x) * 4 + 3]) { x0 = Math.min(x0, x); y0 = Math.min(y0, y); x1 = Math.max(x1, x); y1 = Math.max(y1, y); }
  return { x0, y0, x1, y1, w: x1 - x0 + 1, h: y1 - y0 + 1 };
}

// —— render ——
const charDir = join(OUT_ROOT, meta.name);
const frames = {};   // anim → dir → [img]
let maxH = 0;
for (const [anim, A] of Object.entries(ANIMATIONS)) {
  frames[anim] = {};
  for (const dir of DIRECTIONS) {
    frames[anim][dir] = A.frames.map(pose => render(dir, pose));
    for (const f of frames[anim][dir]) maxH = Math.max(maxH, bbox(f).h);
  }
}
const footPx = (meta.footRow + 1) * K;                 // linha (de cima) onde a sola toca o chão
const pivot = { x: 0.5, y: +((FS - footPx) / FS).toFixed(5) };
const ppu = Math.round(maxH / meta.heightMeters);

// —— frames individuais + spritesheets ——
const json = {
  character: meta.name, id: meta.id, description: meta.description,
  generator: 'Tools/LemoSpriteLab (node src/generate.mjs --character ' + charId + ')',
  lemo: lemo.provenance,
  frameWidth: FS, frameHeight: FS, logicalResolution: N, pixelScale: K,
  pixelsPerUnit: ppu, heightMeters: meta.heightMeters,
  pivot, pivotNote: 'Normalizado (0,0 = canto inferior esquerdo, convenção Unity). Pivô na sola dos pés, centro horizontal.',
  filterMode: 'Point', directions: DIRECTIONS, handedness: meta.handedness, assumptions: meta.assumptions,
  palette: ch.PALETTE,
  coordinateNote: 'x,y = canto superior esquerdo em pixels (origem no topo). unityRect.y usa origem inferior (Unity).',
  animations: [], combinedSheet: null,
};
const combinedCols = Math.max(...Object.values(ANIMATIONS).map(a => a.frames.length));
const combinedRows = Object.keys(ANIMATIONS).length * DIRECTIONS.length;
const combined = newImage(combinedCols * FS, combinedRows * FS);
const combinedFrames = [];
let row = 0;
for (const [anim, A] of Object.entries(ANIMATIONS)) {
  const n = A.frames.length, sheet = newImage(n * FS, DIRECTIONS.length * FS);
  const entry = { name: anim, frameCount: n, fps: A.fps, loop: A.loop, durationSeconds: +(n / A.fps).toFixed(3),
    sheet: `${meta.name}/Sheets/${meta.name}_${anim}_Sheet.png`, sheetWidth: sheet.width, sheetHeight: sheet.height, directions: {} };
  DIRECTIONS.forEach((dir, di) => {
    const list = [];
    frames[anim][dir].forEach((img, i) => {
      const file = `${meta.name}/${anim}/${dir}/${meta.name}_${anim}_${dir}_${pad2(i)}.png`;
      write(join(OUT_ROOT, file), encodePNG(img));
      blit(sheet, img, i * FS, di * FS);
      blit(combined, img, i * FS, row * FS);
      const rect = { x: i * FS, y: di * FS, w: FS, h: FS };
      list.push({ index: i, file, rect, unityRect: { x: rect.x, y: sheet.height - rect.y - FS, w: FS, h: FS } });
      combinedFrames.push({ animation: anim, direction: dir, index: i, rect: { x: i * FS, y: row * FS, w: FS, h: FS },
        unityRect: { x: i * FS, y: combined.height - row * FS - FS, w: FS, h: FS } });
    });
    entry.directions[dir] = { row: di, frames: list };
    row++;
  });
  write(join(OUT_ROOT, entry.sheet), encodePNG(sheet));
  json.animations.push(entry);
}
json.combinedSheet = { file: `${meta.name}/Sheets/${meta.name}_Spritesheet.png`, width: combined.width, height: combined.height,
  layout: 'linhas = animação × direção (Idle Front, Idle Back, Idle Left, Idle Right, Walk ...); colunas = frames; células vazias são transparentes', frames: combinedFrames };
write(join(OUT_ROOT, json.combinedSheet.file), encodePNG(combined));
// lista plana (legível por UnityEngine.JsonUtility, que não lê dicionários)
json.frameList = json.animations.flatMap(a => DIRECTIONS.flatMap(d => a.directions[d].frames.map(f => ({ animation: a.name, direction: d, index: f.index, file: f.file }))));

// sombra de contato separada (o Lemo também usa um blob à parte, não pintado no sprite)
const shadow = newImage(FS, FS / 4);
const [sr, sg, sb] = lemo.hex(ch.PALETTE.shadow);
for (let y = 0; y < shadow.height; y++) for (let x = 0; x < shadow.width; x++) {
  const dx = (x + .5 - FS / 2) / (FS * .22), dy = (y + .5 - shadow.height / 2) / (shadow.height * .32), r = dx * dx + dy * dy;
  if (r <= 1) shadow.data.set([sr, sg, sb, Math.round(150 * (1 - r * .55))], (y * shadow.width + x) * 4);
}
json.shadow = { file: `${meta.name}/${meta.name}_Shadow.png`, width: shadow.width, height: shadow.height, pivot: { x: .5, y: .5 } };
write(join(OUT_ROOT, json.shadow.file), encodePNG(shadow));
write(join(charDir, `${meta.name}_Sprites.json`), JSON.stringify(json, null, 2));

// —— prévias (fora de Assets) ——
const prevDir = join(PREVIEW_ROOT, meta.id);
const BG = [44, 47, 62, 255], BG2 = [52, 56, 73, 255], GUIDE = [255, 154, 46, 255];
function onBackdrop(img, guides = true) {
  const o = newImage(img.width, img.height);
  for (let y = 0; y < o.height; y++) for (let x = 0; x < o.width; x++) o.data.set(((x >> 3) + (y >> 3)) & 1 ? BG : BG2, (y * o.width + x) * 4);
  if (guides) { fillRect(o, 0, footPx, o.width, 1, GUIDE); fillRect(o, o.width / 2, 0, 1, o.height, [255, 154, 46, 110]); }
  blit(o, img, 0, 0); return o;
}
for (const [anim, A] of Object.entries(ANIMATIONS)) for (const dir of DIRECTIONS) {
  write(join(prevDir, `${meta.id}_${anim}_${dir}.apng`), encodeAPNG(frames[anim][dir].map(f => upscale(onBackdrop(f), 2)), 1000 / A.fps));
}
// prancha de contato (fundo xadrez, guia de chão laranja, guia central) e teste de silhueta
const contact = newImage(combined.width, combined.height), sil = newImage(combined.width, combined.height);
row = 0;
for (const [anim, A] of Object.entries(ANIMATIONS)) for (const dir of DIRECTIONS) {
  frames[anim][dir].forEach((f, i) => {
    blit(contact, onBackdrop(f), i * FS, row * FS);
    const s = newImage(FS, FS); fillRect(s, 0, 0, FS, FS, [235, 235, 235, 255]);
    for (let p = 0; p < FS * FS; p++) if (f.data[p * 4 + 3]) s.data.set([0, 0, 0, 255], p * 4);
    blit(sil, s, i * FS, row * FS);
  });
  row++;
}
write(join(prevDir, `${meta.id}_contact_sheet.png`), encodePNG(upscale(contact, 1)));
// tiras ampliadas ×2 por animação/direção (inspeção visual de detalhe)
for (const [anim, A] of Object.entries(ANIMATIONS)) for (const dir of DIRECTIONS) {
  const strip = newImage(A.frames.length * FS, FS);
  frames[anim][dir].forEach((f, i) => blit(strip, onBackdrop(f), i * FS, 0));
  write(join(prevDir, 'zoom', `${meta.id}_${anim}_${dir}_x2.png`), encodePNG(upscale(strip, 2)));
}
write(join(prevDir, `${meta.id}_silhouette.png`), encodePNG(sil));

// página de prévia (abre com duplo clique; anima as spritesheets reais exportadas)
// spritesheets embutidas (data URI) para a página abrir em qualquer lugar; são os mesmos bytes exportados
const relSheet = f => 'data:image/png;base64,' + readFileSync(join(OUT_ROOT, f)).toString('base64');
const html = `<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>${meta.name} Sprite Preview</title>
<style>:root{--bg:#16131f;--fg:#e8eeff;--mut:#9aa3c0;--card:#221d30}body{margin:0;background:var(--bg);color:var(--fg);font:14px/1.4 system-ui,sans-serif;padding:16px}
h1{font-size:18px;margin:0 0 4px}p{color:var(--mut);margin:0 0 16px}.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(150px,1fr));gap:12px}
.cell{background:var(--card);border-radius:8px;padding:8px;text-align:center}canvas{image-rendering:pixelated;width:128px;height:128px;background:repeating-conic-gradient(#2c2f3e 0 25%,#343849 0 50%) 0 0/16px 16px}
label{display:block;color:var(--mut);font-size:12px;margin-top:4px}.ctl{margin:0 0 16px}button{background:#2f6bff;color:#fff;border:0;border-radius:6px;padding:6px 10px;margin-right:6px;cursor:pointer}</style></head>
<body><h1>${meta.name} — prévia Idle/Walk</h1><p>Lê as spritesheets exportadas em Assets. Linha laranja = ponto de apoio (pivô) em y=${footPx}px.</p>
<div class="ctl"><button id="g">Guias on/off</button><button id="z">Zoom 1×/2×</button></div><div class="grid" id="grid"></div>
<script>const D=${JSON.stringify(json.animations.map(a => ({ name: a.name, fps: a.fps, n: a.frameCount, sheet: relSheet(a.sheet) })))},DIRS=${JSON.stringify(DIRECTIONS)},FS=${FS},FOOT=${footPx};
let guides=true,zoom=1;const cells=[];
for(const a of D){const img=new Image();img.src=a.sheet;DIRS.forEach((d,r)=>{const c=document.createElement('div');c.className='cell';const cv=document.createElement('canvas');cv.width=FS;cv.height=FS;c.append(cv);const l=document.createElement('label');l.textContent=a.name+' · '+d+' · '+a.fps+' fps';c.append(l);grid.append(c);cells.push({a,img,r,cv})})}
function tick(t){for(const c of cells){const g=c.cv.getContext('2d');g.imageSmoothingEnabled=false;g.clearRect(0,0,FS,FS);const f=Math.floor(t/1000*c.a.fps)%c.a.n;if(c.img.complete)g.drawImage(c.img,f*FS,c.r*FS,FS,FS,0,0,FS,FS);if(guides){g.fillStyle='rgba(255,154,46,.9)';g.fillRect(0,FOOT,FS,1);g.fillStyle='rgba(255,154,46,.4)';g.fillRect(FS/2,0,1,FS)}c.cv.style.width=c.cv.style.height=(FS*zoom)+'px'}requestAnimationFrame(tick)}
requestAnimationFrame(tick);document.getElementById('g').onclick=()=>guides=!guides;document.getElementById('z').onclick=()=>zoom=zoom===1?2:1;</script></body></html>`;
write(join(prevDir, 'index.html'), html);

const total = Object.values(frames).reduce((s, a) => s + Object.values(a).reduce((t, l) => t + l.length, 0), 0);
console.log(`OK ${meta.name}: ${total} frames ${FS}x${FS}, PPU ${ppu}, pivô ${pivot.x},${pivot.y}`);
console.log('  assets  →', relative(PROJECT, charDir));
console.log('  prévias →', relative(PROJECT, prevDir));
