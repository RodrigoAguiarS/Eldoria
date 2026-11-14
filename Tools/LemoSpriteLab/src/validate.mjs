#!/usr/bin/env node
// Valida os PNGs exportados (lidos de volta do disco): tamanho, canal alpha real, cortes,
// ponto de apoio, centralização, consistência de paleta/volume entre frames e as spritesheets.
//   node src/validate.mjs [--json <caminho do *_Sprites.json>]
import { readFileSync, writeFileSync } from 'node:fs';
import { join, resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { decodePNG } from './png.mjs';

const HERE = dirname(fileURLToPath(import.meta.url));
const PROJECT = resolve(HERE, '..', '..', '..');
const argv = process.argv.slice(2), ai = argv.indexOf('--json');
const jsonPath = resolve(ai >= 0 ? argv[ai + 1] : join(PROJECT, 'Assets/_Project/Art/Sprites/Prototypes/Hero/Hero_Sprites.json'));
const J = JSON.parse(readFileSync(jsonPath, 'utf8'));
const ROOT = resolve(dirname(jsonPath), '..');
const FS = J.frameWidth, footRow = Math.round(FS - J.pivot.y * FS);   // primeira linha abaixo da sola
const errors = [], warns = [], rows = [];
const hexOf = (r, g, b) => '#' + [r, g, b].map(v => v.toString(16).padStart(2, '0')).join('').toUpperCase();

function analyse(img) {
  let x0 = 1e9, y0 = 1e9, x1 = -1, y1 = -1, opaque = 0, partial = 0; const cols = new Map();
  for (let y = 0; y < img.height; y++) for (let x = 0; x < img.width; x++) {
    const i = (y * img.width + x) * 4, a = img.data[i + 3];
    if (a === 0) continue;
    if (a !== 255) partial++;
    opaque++; x0 = Math.min(x0, x); y0 = Math.min(y0, y); x1 = Math.max(x1, x); y1 = Math.max(y1, y);
    const k = hexOf(img.data[i], img.data[i + 1], img.data[i + 2]); cols.set(k, (cols.get(k) || 0) + 1);
  }
  // sola: linha mais baixa com pixels opacos na metade inferior que não sejam contorno lateral
  return { x0, y0, x1, y1, w: x1 - x0 + 1, h: y1 - y0 + 1, opaque, partial, cols, cx: (x0 + x1 + 1) / 2 };
}

const official = Object.fromEntries(Object.entries(J.palette).map(([k, v]) => [v.toUpperCase(), k]));
const usedOfficial = new Set();
let total = 0;
for (const A of J.animations) {
  for (const dir of J.directions) {
    const list = A.directions[dir].frames, stats = [];
    for (const fr of list) {
      const buf = readFileSync(join(ROOT, fr.file)), img = decodePNG(buf); total++;
      const tag = `${A.name}/${dir}/${fr.index}`;
      if (img.width !== FS || img.height !== FS) errors.push(`${tag}: tamanho ${img.width}x${img.height}`);
      if (img.colorType !== 6) { errors.push(`${tag}: PNG sem canal alpha (colorType ${img.colorType})`); continue; }
      const s = analyse(img);
      const corners = [0, FS - 1, FS * (FS - 1), FS * FS - 1].map(p => img.data[p * 4 + 3]);
      if (corners.some(a => a !== 0)) errors.push(`${tag}: cantos não transparentes`);
      if (s.partial) warns.push(`${tag}: ${s.partial} pixels semitransparentes`);
      const margin = Math.min(s.x0, s.y0, FS - 1 - s.x1, FS - 1 - s.y1);
      if (margin < 2) errors.push(`${tag}: personagem encosta na borda (margem ${margin}px) — possível corte`);
      for (const k of s.cols.keys()) if (official[k]) usedOfficial.add(official[k]);
      stats.push({ ...s, margin, tag });
    }
    // consistência dentro da direção
    const H = stats.map(s => s.h), O = stats.map(s => s.opaque), CX = stats.map(s => s.cx), BOT = stats.map(s => s.y1);
    const span = a => Math.max(...a) - Math.min(...a);
    const pal = stats.map(s => new Set(s.cols.keys()));
    const inter = [...pal[0]].filter(c => pal.every(p => p.has(c))).length, uni = new Set(pal.flatMap(p => [...p])).size;
    rows.push({ anim: A.name, dir, frames: stats.length, heightPx: `${Math.min(...H)}–${Math.max(...H)}`, opaquePx: `${Math.min(...O)}–${Math.max(...O)}`,
      opaqueVar: +(span(O) / Math.max(...O) * 100).toFixed(1), centerX: `${Math.min(...CX)}–${Math.max(...CX)}`, lowestRow: `${Math.min(...BOT)}–${Math.max(...BOT)}`,
      minMargin: Math.min(...stats.map(s => s.margin)), sharedColors: `${inter}/${uni}` });
    if (span(O) / Math.max(...O) > .12) warns.push(`${A.name}/${dir}: volume opaco varia ${(span(O) / Math.max(...O) * 100).toFixed(1)}%`);
    if (Math.max(...BOT) > footRow + 2) errors.push(`${A.name}/${dir}: pixels abaixo do pivô (${Math.max(...BOT)} > ${footRow + 2})`);
    if (A.name === 'Idle' && span(BOT) > 0) errors.push(`${A.name}/${dir}: pés mudam de altura no Idle`);
    if (span(CX) > 6) warns.push(`${A.name}/${dir}: centro horizontal oscila ${span(CX)}px`);
  }
  // spritesheet da animação confere com os frames
  const sheet = decodePNG(readFileSync(join(ROOT, A.sheet)));
  if (sheet.width !== A.sheetWidth || sheet.height !== A.sheetHeight) errors.push(`${A.sheet}: tamanho divergente do JSON`);
  for (const dir of J.directions) for (const fr of A.directions[dir].frames) {
    const img = decodePNG(readFileSync(join(ROOT, fr.file)));
    for (let y = 0; y < FS; y += 3) for (let x = 0; x < FS; x += 3) {
      const a = (y * FS + x) * 4, b = ((fr.rect.y + y) * sheet.width + fr.rect.x + x) * 4;
      if (img.data[a + 3] !== sheet.data[b + 3] || (img.data[a + 3] && img.data[a] !== sheet.data[b])) { errors.push(`${A.sheet}: célula ${dir}/${fr.index} difere do frame`); y = FS; break; }
    }
  }
}
const comb = decodePNG(readFileSync(join(ROOT, J.combinedSheet.file)));
if (comb.width !== J.combinedSheet.width || comb.height !== J.combinedSheet.height) errors.push('spritesheet combinada: tamanho divergente');
const expected = J.animations.reduce((s, a) => s + a.frameCount * J.directions.length, 0);
if (total !== expected) errors.push(`total de frames ${total} ≠ ${expected}`);
const missingOfficial = ['cape', 'leather', 'metal', 'brass', 'skin'].filter(k => !usedOfficial.has(k));
if (missingOfficial.length) warns.push('cores oficiais ausentes nos pixels: ' + missingOfficial.join(', '));

console.table(rows);
console.log(`frames: ${total} · cores oficiais presentes: ${[...usedOfficial].join(', ')}`);
for (const w of warns) console.log('AVISO ', w);
for (const e of errors) console.log('ERRO  ', e);
const report = { json: jsonPath, frames: total, expected, pivotRow: footRow, usedOfficial: [...usedOfficial], rows, warnings: warns, errors, ok: errors.length === 0, checkedAt: new Date().toISOString() };
writeFileSync(join(HERE, '..', 'preview', `${J.id}_validation.json`), JSON.stringify(report, null, 2));
console.log(errors.length ? `FALHOU (${errors.length} erros)` : 'VALIDAÇÃO OK');
process.exit(errors.length ? 1 : 0);
