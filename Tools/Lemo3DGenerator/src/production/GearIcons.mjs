#!/usr/bin/env node
// ITEMS-GEAR — ícones ICON-ITEM-HELMET/GLOVES/BOOTS/AMULET, renderizados a partir da MESMA geometria de GearModels.mjs
// com a MESMA página do lote ITEMS-COMBAT (ItemIcons.mjs → PAGE): luz, câmera ortográfica, supersampling 2×, contorno
// escuro por dilatação do alpha, 512×512 RGBA com conteúdo em 416×416 centrais (margem 48 px). Sem texto, número ou
// moldura de raridade (a raridade é apresentada pela UI a partir dos dados do item).
//
// Saídas em <staging>/Source~:
//   Icons/ICON-ITEM-<ID>.png       quatro ícones 512×512 RGBA com alpha real
//   Icons/review_icons.png         prancha: os 9 ícones de item (5 existentes + 4 novos) a 48/32 px sobre fundo claro,
//                                  escuro, pedra e a base do slot da bolsa, e silhuetas em escala de cinza a 32 px
//   Icons/models_review.png        prancha: os quatro modelos novos ao lado da Espada de Ferro e do Colete (escala/estilo)
//   GearIcons.validation.json      verificação independente: PNG decodificado do disco (dimensão, alpha, margens)
//
// Uso: node src/production/GearIcons.mjs [--out <pasta>]   (rode antes GearModels.mjs; ItemModels.mjs para a prancha)
import { mkdirSync, writeFileSync, readFileSync, existsSync } from 'node:fs';
import { join, resolve, dirname, relative } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { LEMO_ROOT, THREE_VERSION } from '../lemo-three.mjs';
import { STAGING } from './GearModels.mjs';
import { STAGING as COMBAT_STAGING } from './ItemModels.mjs';
import { PAGE, SIZE, CONTENT, OUTLINE, decodePNG, alphaStats } from './ItemIcons.mjs';

const HERE = dirname(fileURLToPath(import.meta.url)), LAB = resolve(HERE, '..', '..'), PROJECT = resolve(LAB, '..', '..');
const argv = process.argv.slice(2), arg = (k, d) => { const i = argv.indexOf('--' + k); return i >= 0 ? argv[i + 1] : d; };
const OUT = resolve(arg('out', join(PROJECT, STAGING, 'Source~')));

// ID do ícone → modelo e pose (graus; câmera ortográfica frontal elevada `elev`, como em ItemIcons.mjs).
export const ICONS = [
  { id: 'ICON-ITEM-HELMET', model: 'Item_IronHelmet', pose: { pre: [0, 0, 0], rot: [0, -30, 0] }, elev: 18 },
  { id: 'ICON-ITEM-GLOVES', model: 'Item_LeatherGloves', pose: { pre: [0, 0, 0], rot: [0, 18, 0] }, elev: 42 },
  { id: 'ICON-ITEM-BOOTS', model: 'Item_LeatherBoots', pose: { pre: [0, 0, 0], rot: [0, -34, 0] }, elev: 16 },
  { id: 'ICON-ITEM-AMULET', model: 'Item_CopperAmulet', pose: { pre: [0, 0, 0], rot: [0, 16, 0] }, elev: 6 },
];
const EXISTING_ICONS = ['ICON-ITEM-IRON', 'ICON-ITEM-WARDEN', 'ICON-ITEM-ANCIENT', 'ICON-ITEM-LEATHER', 'ICON-ITEM-HEALTH'];
const SLOT_BASE = join(PROJECT, 'Assets/_Project/Art/ProductionStaging/UI-SKIN');
const SLOT_FILE = 'UI-SKIN-Slot-Normal.png';

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const renderJson = join(OUT, 'GearModels.render.json');
  if (!existsSync(renderJson)) { console.error('Rode antes: node src/production/GearModels.mjs'); process.exit(1); }
  const models = JSON.parse(readFileSync(renderJson, 'utf8'));
  const combatJson = join(PROJECT, COMBAT_STAGING, 'Source~', 'ItemModels.render.json');
  const combat = existsSync(combatJson) ? JSON.parse(readFileSync(combatJson, 'utf8')) : {};
  const pageDir = join(OUT, 'icon-render'), iconDir = join(OUT, 'Icons');
  for (const d of [pageDir, iconDir]) mkdirSync(d, { recursive: true });
  writeFileSync(join(pageDir, 'data.json'), JSON.stringify({ models: { ...combat, ...models } }));
  writeFileSync(join(pageDir, 'index.html'), PAGE.replace('__SIZE__', SIZE).replace('__CONTENT__', CONTENT).replace('__OUTLINE__', OUTLINE));

  const { openDemo, closeServer } = await import(pathToFileURL(join(LEMO_ROOT, 'core', 'render', 'page.mjs')).href);
  const { browser, page } = await openDemo(pageDir, { w: 64, h: 64 });
  const validation = { batch: 'ITEMS-GEAR', generator: 'Tools/Lemo3DGenerator/src/production/GearIcons.mjs', page: 'ItemIcons.mjs PAGE (mesma luz/câmera/contorno do lote ITEMS-COMBAT)',
    lemo: { root: LEMO_ROOT, three: THREE_VERSION, renderer: 'core/render/page.mjs (Playwright Chromium headless), somente leitura' }, size: SIZE, content: CONTENT, outline: OUTLINE, icons: [], issues: [] };
  const decode = url => Buffer.from(url.split(',')[1], 'base64');
  for (const ic of ICONS) {
    if (!models[ic.model]) { validation.issues.push(`modelo ausente ${ic.model}`); continue; }
    const png = decode(await page.evaluate(({ m, p, e }) => window.icon(m, p, e), { m: ic.model, p: ic.pose, e: ic.elev }));
    const file = join(iconDir, `${ic.id}.png`); writeFileSync(file, png);
    const dec = decodePNG(readFileSync(file)), st = alphaStats(dec);
    const margin = st.content ? Math.min(st.content.x, st.content.y, SIZE - st.content.x - st.content.w, SIZE - st.content.y - st.content.h) : -1;
    const ok = dec.w === SIZE && dec.h === SIZE && dec.colorType === 6 && st.transparentPct > 20 && st.corners.every(a => a === 0) && margin >= (SIZE - CONTENT) / 2 - 2;
    if (!ok) validation.issues.push(`${ic.id}: falhou (margem ${margin}, ${JSON.stringify(st)})`);
    validation.icons.push({ id: ic.id, model: ic.model, file: relative(PROJECT, file).replace(/\\/g, '/'), width: dec.w, height: dec.h, rgba: dec.colorType === 6, ...st, minMargin: margin, ok, pose: ic.pose, elev: ic.elev });
    console.log(`${ic.id.padEnd(18)} ${dec.w}x${dec.h} RGBA=${dec.colorType === 6} transp ${st.transparentPct}% margem ${margin}px ${ok ? 'OK' : 'FALHOU'}`);
  }

  // pranchas de revisão (com fundo; NÃO são ícones)
  const url = file => 'data:image/png;base64,' + readFileSync(file).toString('base64');
  const all = [...EXISTING_ICONS.map(id => ({ id, url: url(join(PROJECT, 'Assets/_Project/Art/Items/Icons', id + '.png')), isNew: false })),
    ...ICONS.map(ic => ({ id: ic.id, url: url(join(iconDir, ic.id + '.png')), isNew: true }))];
  const slot = existsSync(join(SLOT_BASE, SLOT_FILE)) ? url(join(SLOT_BASE, SLOT_FILE)) : null;
  const backgrounds = ['#e8e4dc', '#14161c', '#55596b'];
  const stills = [];
  for (const name of ['Item_IronSword', 'Item_LeatherVest', 'Item_IronHelmet', 'Item_LeatherGloves', 'Item_LeatherBoots', 'Item_CopperAmulet']) {
    if (!combat[name] && !models[name]) continue;
    const sword = name.includes('Sword');
    stills.push({ name, url: await page.evaluate(({ m, p, e }) => window.still(m, p, e, 0x2b2e38), { m: name, p: sword ? { pre: [-90, 0, 0], rot: [0, 28, -42] } : { pre: [0, 0, 0], rot: [0, 25, 0] }, e: sword ? 12 : 30 }) });
  }
  const review = await browser.newPage({ viewport: { width: 1500, height: 900 } });
  await review.setContent(`<html><body style="margin:0;font:13px sans-serif;color:#ddd;background:#111">
    <div id="a" style="padding:12px;display:grid;grid-template-columns:repeat(3,1fr);gap:12px">${all.map(i => `<div style="${i.isNew ? 'outline:1px solid #4fd8ff;' : ''}padding:4px">${i.id}${i.isNew ? ' (novo)' : ''}
      <div style="display:flex;gap:6px;margin-top:6px">${backgrounds.map(bg => `<div style="background:${bg};padding:6px"><img src="${i.url}" width="48" height="48"><img src="${i.url}" width="32" height="32"></div>`).join('')}
      ${slot ? `<div style="background:url(${slot}) center/100% 100%;padding:8px"><img src="${i.url}" width="48" height="48"></div>` : ''}
      <div style="background:#e8e4dc;padding:6px"><img src="${i.url}" width="32" height="32" style="filter:grayscale(1) brightness(.2)"></div></div></div>`).join('')}</div>
    <div id="b" style="padding:12px;display:grid;grid-template-columns:repeat(3,1fr);gap:10px">${stills.map(s => `<div>${s.name}<br><img src="${s.url}" width="300"></div>`).join('')}</div></body></html>`);
  await review.waitForTimeout(300);
  await (await review.$('#a')).screenshot({ path: join(iconDir, 'review_icons.png') });
  await (await review.$('#b')).screenshot({ path: join(iconDir, 'models_review.png') });
  await browser.close(); closeServer();

  validation.ok = validation.issues.length === 0; validation.date = new Date().toISOString();
  writeFileSync(join(OUT, 'GearIcons.validation.json'), JSON.stringify(validation, null, 2));
  if (!validation.ok) { console.error('FALHOU:\n  ' + validation.issues.join('\n  ')); process.exit(1); }
  console.log('OK →', relative(PROJECT, OUT));
}
