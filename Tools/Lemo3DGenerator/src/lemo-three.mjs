// Carrega o three.js r170 instalado no Lemo-Opuscar (SOMENTE LEITURA) para uso no Node:
// núcleo + GLTFExporter + GLTFLoader + BufferGeometryUtils. Nada é escrito no Lemo.
// O Lemo usa o mesmo three (importmap "/node_modules/three/...") nos seus estilos 3D.
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { pathToFileURL } from 'node:url';

export const LEMO_ROOT = process.env.LEMO_ROOT || 'G:/lemo-opuscar/lemo-opuscar';
const THREE_DIR = join(LEMO_ROOT, 'node_modules', 'three');
if (!existsSync(join(THREE_DIR, 'build', 'three.module.js'))) throw new Error(`three.js não encontrado em ${THREE_DIR} (defina LEMO_ROOT)`);

// Polyfills mínimos para o GLTFExporter no Node (ele usa FileReader para montar o GLB).
if (typeof globalThis.FileReader === 'undefined') {
  globalThis.FileReader = class {
    readAsArrayBuffer(blob) { blob.arrayBuffer().then(b => { this.result = b; this.onloadend && this.onloadend(); this.onload && this.onload({ target: this }); }); }
    readAsDataURL(blob) { blob.arrayBuffer().then(b => { this.result = 'data:' + (blob.type || 'application/octet-stream') + ';base64,' + Buffer.from(b).toString('base64'); this.onloadend && this.onloadend(); this.onload && this.onload({ target: this }); }); }
  };
}
if (typeof globalThis.self === 'undefined') globalThis.self = globalThis;

const url = rel => pathToFileURL(join(THREE_DIR, rel)).href;
// "three" nos addons resolve pelo node_modules do Lemo → mesmo arquivo build/three.module.js (mesma instância).
export const THREE = await import(url('build/three.module.js'));
export const { GLTFExporter } = await import(url('examples/jsm/exporters/GLTFExporter.js'));
export const { GLTFLoader } = await import(url('examples/jsm/loaders/GLTFLoader.js'));
export const BufferGeometryUtils = await import(url('examples/jsm/utils/BufferGeometryUtils.js'));
export const { ConvexGeometry } = await import(url('examples/jsm/geometries/ConvexGeometry.js'));
export const THREE_VERSION = JSON.parse(readFileSync(join(THREE_DIR, 'package.json'), 'utf8')).version;
export const THREE_URL = url('build/three.module.js');
