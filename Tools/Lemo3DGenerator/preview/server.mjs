#!/usr/bin/env node
// Servidor local da prévia 360°: /lemo/* → biblioteca Lemo (somente leitura, para o three.js),
// /out/* → pasta de modelos exportados, /* → esta ferramenta. POST /save?name=x.png grava capturas em preview/captures.
//   node preview/server.mjs [--port 5178] [--out <pasta de modelos>]
import { createServer } from 'node:http';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { join, resolve, extname, normalize, dirname, basename } from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = dirname(fileURLToPath(import.meta.url)), LAB = resolve(HERE, '..');
const argv = process.argv.slice(2), arg = (k, d) => { const i = argv.indexOf('--' + k); return i >= 0 ? argv[i + 1] : d; };
const PORT = +arg('port', 5178);
const OUT = resolve(arg('out', join(LAB, '..', '..', 'Assets/_Project/Art/Models/Generated')));
const LEMO = process.env.LEMO_ROOT || 'G:/lemo-opuscar/lemo-opuscar';
const TYPES = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.mjs': 'text/javascript', '.json': 'application/json', '.glb': 'model/gltf-binary', '.png': 'image/png', '.jpg': 'image/jpeg' };

createServer(async (req, res) => {
  try {
    const u = new URL(req.url, 'http://x');
    if (req.method === 'POST' && u.pathname === '/save') {
      const chunks = []; for await (const c of req) chunks.push(c);
      const name = basename(u.searchParams.get('name') || 'capture.png').replace(/[^\w.-]/g, '_');
      const dir = join(HERE, 'captures'); await mkdir(dir, { recursive: true });
      const body = Buffer.concat(chunks).toString(); const b64 = body.slice(body.indexOf(',') + 1);
      await writeFile(join(dir, name), Buffer.from(b64, 'base64'));
      res.writeHead(200).end('ok'); console.log('salvo', name); return;
    }
    let base = HERE, p = decodeURIComponent(u.pathname);
    if (p.startsWith('/lemo/')) { base = LEMO; p = p.slice(5); }
    else if (p.startsWith('/out/')) { base = OUT; p = p.slice(4); }
    else if (p === '/') p = '/index.html';
    const file = normalize(join(base, p));
    if (!file.startsWith(normalize(base))) { res.writeHead(403).end(); return; }
    const data = await readFile(file);
    res.writeHead(200, { 'Content-Type': TYPES[extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store' }).end(data);
  } catch (e) { res.writeHead(404).end(String(e.message)); }
}).listen(PORT, () => console.log(`prévia em http://localhost:${PORT}/ (modelos: ${OUT})`));
