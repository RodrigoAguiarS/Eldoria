// PNG/APNG RGBA 8-bit sem dependências (zlib nativo do Node).
import { deflateSync, inflateSync } from 'node:zlib';

const CRC = new Uint32Array(256);
for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; CRC[n] = c >>> 0; }
function crc32(buf) { let c = 0xffffffff; for (let i = 0; i < buf.length; i++) c = CRC[(c ^ buf[i]) & 255] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; }

function chunk(type, data) {
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const td = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td));
  return Buffer.concat([len, td, crc]);
}
const SIG = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]);

function ihdr(w, h) {
  const b = Buffer.alloc(13); b.writeUInt32BE(w, 0); b.writeUInt32BE(h, 4);
  b[8] = 8; b[9] = 6; b[10] = 0; b[11] = 0; b[12] = 0;   // 8 bits, RGBA, sem entrelaçamento
  return chunk('IHDR', b);
}
function raw(img) {
  const { width: w, height: h, data } = img, out = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) { out[y * (w * 4 + 1)] = 0; Buffer.from(data.buffer, data.byteOffset + y * w * 4, w * 4).copy(out, y * (w * 4 + 1) + 1); }
  return deflateSync(out, { level: 9 });
}

export function encodePNG(img) {
  return Buffer.concat([SIG, ihdr(img.width, img.height), chunk('IDAT', raw(img)), chunk('IEND', Buffer.alloc(0))]);
}

// APNG (prévia animada; abre em navegadores). delay em ms.
export function encodeAPNG(frames, delayMs, loops = 0) {
  const { width: w, height: h } = frames[0];
  const parts = [SIG, ihdr(w, h)];
  const actl = Buffer.alloc(8); actl.writeUInt32BE(frames.length, 0); actl.writeUInt32BE(loops, 4); parts.push(chunk('acTL', actl));
  let seq = 0;
  frames.forEach((f, i) => {
    const fc = Buffer.alloc(26);
    fc.writeUInt32BE(seq++, 0); fc.writeUInt32BE(w, 4); fc.writeUInt32BE(h, 8); fc.writeUInt32BE(0, 12); fc.writeUInt32BE(0, 16);
    fc.writeUInt16BE(Math.round(delayMs), 20); fc.writeUInt16BE(1000, 22); fc[24] = 1; /* dispose: background */ fc[25] = 0; /* blend: source */
    parts.push(chunk('fcTL', fc));
    const z = raw(f);
    if (i === 0) parts.push(chunk('IDAT', z));
    else { const s = Buffer.alloc(4); s.writeUInt32BE(seq++); parts.push(chunk('fdAT', Buffer.concat([s, z]))); }
  });
  parts.push(chunk('IEND', Buffer.alloc(0)));
  return Buffer.concat(parts);
}

// Decodificador mínimo (apenas o que este gerador escreve: RGBA 8-bit, filtros 0..4) para a validação.
export function decodePNG(buf) {
  if (!buf.subarray(0, 8).equals(SIG)) throw new Error('não é PNG');
  let off = 8, w = 0, h = 0, ct = 0, bd = 0; const idat = []; const types = [];
  while (off < buf.length) {
    const len = buf.readUInt32BE(off), type = buf.toString('ascii', off + 4, off + 8), data = buf.subarray(off + 8, off + 8 + len);
    types.push(type);
    if (type === 'IHDR') { w = data.readUInt32BE(0); h = data.readUInt32BE(4); bd = data[8]; ct = data[9]; }
    if (type === 'IDAT') idat.push(data);
    off += 12 + len;
  }
  if (bd !== 8 || ct !== 6) return { width: w, height: h, colorType: ct, bitDepth: bd, chunks: types, data: null };
  const z = inflateSync(Buffer.concat(idat)), stride = w * 4, out = new Uint8ClampedArray(w * h * 4);
  let prev = new Uint8Array(stride);
  for (let y = 0; y < h; y++) {
    const ft = z[y * (stride + 1)], line = z.subarray(y * (stride + 1) + 1, (y + 1) * (stride + 1)), cur = new Uint8Array(stride);
    for (let i = 0; i < stride; i++) {
      const a = i >= 4 ? cur[i - 4] : 0, b = prev[i], c = i >= 4 ? prev[i - 4] : 0;
      let v = line[i];
      if (ft === 1) v += a; else if (ft === 2) v += b; else if (ft === 3) v += (a + b) >> 1;
      else if (ft === 4) { const p = a + b - c, pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c); v += pa <= pb && pa <= pc ? a : pb <= pc ? b : c; }
      cur[i] = v & 255;
    }
    out.set(cur, y * stride); prev = cur;
  }
  return { width: w, height: h, colorType: ct, bitDepth: bd, chunks: types, data: out };
}

// —— utilidades de imagem ——
export const newImage = (w, h) => ({ width: w, height: h, data: new Uint8ClampedArray(w * h * 4) });
export function fromPX(P) { return { width: P.w, height: P.h, data: new Uint8ClampedArray(P.d.data) }; }
export function upscale(img, k) {
  const o = newImage(img.width * k, img.height * k);
  for (let y = 0; y < o.height; y++) for (let x = 0; x < o.width; x++) {
    const s = (((y / k) | 0) * img.width + ((x / k) | 0)) * 4, d = (y * o.width + x) * 4;
    o.data[d] = img.data[s]; o.data[d + 1] = img.data[s + 1]; o.data[d + 2] = img.data[s + 2]; o.data[d + 3] = img.data[s + 3];
  }
  return o;
}
export function blit(dst, src, dx, dy) {
  for (let y = 0; y < src.height; y++) for (let x = 0; x < src.width; x++) {
    const X = dx + x, Y = dy + y; if (X < 0 || Y < 0 || X >= dst.width || Y >= dst.height) continue;
    const s = (y * src.width + x) * 4, d = (Y * dst.width + X) * 4, a = src.data[s + 3] / 255;
    if (a === 0) continue;
    for (let c = 0; c < 3; c++) dst.data[d + c] = Math.round(src.data[s + c] * a + dst.data[d + c] * (1 - a));
    dst.data[d + 3] = Math.max(dst.data[d + 3], src.data[s + 3]);
  }
}
export function fillRect(img, x0, y0, w, h, rgba) {
  for (let y = y0; y < y0 + h; y++) for (let x = x0; x < x0 + w; x++) {
    if (x < 0 || y < 0 || x >= img.width || y >= img.height) continue;
    const d = (y * img.width + x) * 4; img.data.set(rgba, d);
  }
}
