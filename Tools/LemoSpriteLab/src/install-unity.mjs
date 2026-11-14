#!/usr/bin/env node
// Copia os scripts Unity do Sprite Lab (unity/Runtime e unity/Editor + asmdefs) para
// Assets/_Project/Art/Sprites/Prototypes/_SpriteLab. Rode `npm run check-cs` antes (compilação fora do Unity).
import { cpSync, mkdirSync, readdirSync } from 'node:fs';
import { join, resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const LAB = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const DEST = resolve(LAB, '..', '..', 'Assets/_Project/Art/Sprites/Prototypes/_SpriteLab');
for (const part of ['Runtime', 'Editor']) {
  const src = join(LAB, 'unity', part), dst = join(DEST, part);
  mkdirSync(dst, { recursive: true });
  for (const f of readdirSync(src)) if (/\.(cs|asmdef)$/.test(f)) { cpSync(join(src, f), join(dst, f)); console.log('→', part + '/' + f); }
}
console.log('Scripts instalados em', DEST);
