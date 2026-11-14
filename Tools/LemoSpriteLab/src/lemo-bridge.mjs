// Ponte SOMENTE LEITURA para o motor de pixel do estilo HD-2D do Lemo-Opuscar.
//
// O Lemo desenha seus personagens no navegador (canvas + three.js). Aqui carregamos o arquivo
// original `styles/hd-2d/demo/px.js` sem modificá-lo no disco: o texto é lido, a linha
// `import * as THREE from 'three'` é trocada em memória por um stub (as funções de pixel não usam
// THREE; só billboard/texOf/glow usam e essas não são chamadas aqui) e um `document` mínimo é
// simulado para que `new PX(w, h)` funcione no Node. Nenhum arquivo do Lemo é escrito.
import { readFileSync, existsSync } from 'node:fs';
import { join } from 'node:path';
import { createHash } from 'node:crypto';

export const LEMO_ROOT = process.env.LEMO_ROOT || 'G:/lemo-opuscar/lemo-opuscar';
export const LEMO_PX_REL = 'styles/hd-2d/demo/px.js';

function fakeCanvas() {
  const c = {
    width: 0, height: 0,
    getContext() {
      return {
        createImageData: (w, h) => ({ width: w, height: h, data: new Uint8ClampedArray(w * h * 4) }),
        putImageData() { /* os pixels ficam em PX.d.data; nada a fazer */ },
      };
    },
  };
  return c;
}

export async function loadLemo(root = LEMO_ROOT) {
  const path = join(root, LEMO_PX_REL);
  if (!existsSync(path)) throw new Error(`Lemo px.js não encontrado em ${path}. Defina LEMO_ROOT.`);
  const src = readFileSync(path, 'utf8');
  const importLine = /^import \* as THREE from 'three';\s*$/m;
  if (!importLine.test(src)) throw new Error('px.js mudou: a linha de import do three não foi encontrada; revise a ponte.');
  const patched = src.replace(importLine,
    "const THREE = new Proxy({}, { get(_, k) { throw new Error('THREE.' + String(k) + ' indisponível no Node (LemoSpriteLab usa apenas as primitivas de pixel)'); } });");

  if (!globalThis.document) globalThis.document = { createElement: () => fakeCanvas() };

  const url = 'data:text/javascript;base64,' + Buffer.from(patched, 'utf8').toString('base64');
  const mod = await import(url);
  for (const k of ['PX', 'ramp', 'hex', 'mixc', 'bayer', 'h2']) if (!mod[k]) throw new Error(`px.js não exporta ${k}`);
  return {
    ...mod,
    provenance: {
      library: 'Lemo-Opuscar (MIT) — github.com/lemomo-ai/lemo-opuscar',
      file: LEMO_PX_REL,
      sha256: createHash('sha256').update(src).digest('hex'),
      usedExports: ['PX (set/rect/ellipse/poly/line/outline)', 'ramp (Bayer 4x4)', 'hex', 'mixc'],
    },
  };
}

// Visão espelhada de um PX: mesma API, coordenadas X invertidas. Os métodos de desenho do Lemo
// (rect/ellipse/poly/line/fill) só chamam this.set e leem this.w/this.h, então são reaproveitados.
export function mirrorPX(lemo, P) {
  const M = Object.create(lemo.PX.prototype);
  M.w = P.w; M.h = P.h;
  M.set = (x, y, col, a) => P.set(P.w - 1 - (x | 0), y, col, a);
  M.get = (x, y) => P.get(P.w - 1 - x, y);
  return M;
}
