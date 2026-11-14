// Primitivas low-poly no grammar do Lemo (styles/lowpoly-island: "primitive assemblies: boxes, prisms,
// low-segment cylinders and cones, icosahedra at detail 0, pinched boxes", flat shading, jitter leve).
// Todas as funções devolvem BufferGeometry NÃO indexada, com normais de face (facetado) e UVs.
import { THREE, BufferGeometryUtils, ConvexGeometry } from './lemo-three.mjs';

const V = (x, y, z) => new THREE.Vector3(x, y, z);
const hash = (x, y, z, s) => { let h = Math.imul((x * 73856093) ^ (y * 19349663) ^ (z * 83492791) ^ (s * 2654435761), 0x45d9f3b); h ^= h >>> 15; return ((h >>> 0) % 10000) / 10000; };

// Normais de face + UV por projeção na face dominante (permite pintar um atlas depois).
// Volume com sinal: < 0 significa triângulos com winding invertido (normais para dentro).
export function signedVolume(g) {
  const p = g.attributes.position; let v = 0;
  for (let i = 0; i < p.count; i += 3) {
    const ax = p.getX(i), ay = p.getY(i), az = p.getZ(i), bx = p.getX(i + 1), by = p.getY(i + 1), bz = p.getZ(i + 1), cx = p.getX(i + 2), cy = p.getY(i + 2), cz = p.getZ(i + 2);
    v += ax * (by * cz - bz * cy) - ay * (bx * cz - bz * cx) + az * (bx * cy - by * cx);
  }
  return v / 6;
}
function orientOutward(g) {
  if (signedVolume(g) >= 0) return g;
  const p = g.attributes.position;
  for (let i = 0; i < p.count; i += 3) { const x = p.getX(i + 1), y = p.getY(i + 1), z = p.getZ(i + 1); p.setXYZ(i + 1, p.getX(i + 2), p.getY(i + 2), p.getZ(i + 2)); p.setXYZ(i + 2, x, y, z); }
  return g;
}

export function finish(g) {
  g = g.index ? g.toNonIndexed() : g;
  orientOutward(g);
  g.deleteAttribute('normal'); g.computeVertexNormals();   // não indexada → normais de face (flat)
  const p = g.attributes.position, n = g.attributes.normal, uv = new Float32Array(p.count * 2);
  for (let i = 0; i < p.count; i++) {
    const ax = Math.abs(n.getX(i)), ay = Math.abs(n.getY(i)), az = Math.abs(n.getZ(i));
    const [u, v] = ax >= ay && ax >= az ? [p.getZ(i), p.getY(i)] : ay >= az ? [p.getX(i), p.getZ(i)] : [p.getX(i), p.getY(i)];
    uv[i * 2] = u * 2 + .5; uv[i * 2 + 1] = v * 2 + .5;
  }
  g.setAttribute('uv', new THREE.BufferAttribute(uv, 2));
  for (const k of Object.keys(g.attributes)) if (!['position', 'normal', 'uv'].includes(k)) g.deleteAttribute(k);
  g.clearGroups();
  return g;
}

// Deslocamento determinístico por posição (mantém a malha fechada: vértices coincidentes andam juntos).
export function jitter(g, amt = .006, seed = 1) {
  const p = g.attributes.position;
  for (let i = 0; i < p.count; i++) {
    const kx = Math.round(p.getX(i) * 1000), ky = Math.round(p.getY(i) * 1000), kz = Math.round(p.getZ(i) * 1000);
    p.setXYZ(i, p.getX(i) + (hash(kx, ky, kz, seed) - .5) * amt, p.getY(i) + (hash(ky, kz, kx, seed + 1) - .5) * amt, p.getZ(i) + (hash(kz, kx, ky, seed + 2) - .5) * amt);
  }
  return g;
}

export function xform(g, { pos = [0, 0, 0], rot = [0, 0, 0], scale = [1, 1, 1] } = {}) {
  const m = new THREE.Matrix4().compose(V(...pos), new THREE.Quaternion().setFromEuler(new THREE.Euler(...rot.map(d => d * Math.PI / 180), 'XYZ')), V(...scale));
  return g.applyMatrix4(m);
}

// —— formas ——
export const convex = pts => finish(new ConvexGeometry(pts.map(p => V(...p))));

// caixa chanfrada (cantos cortados) — o "pinched box" do Lemo com acabamento
export function bevelBox(w, h, d, c = .15, { top = 1, bottom = 1, topZ = 1, bottomZ = 1 } = {}) {
  const pts = [], cw = Math.min(w, d, h) * c;
  for (const sy of [-1, 1]) {
    const k = sy > 0 ? top : bottom, kz = sy > 0 ? topZ : bottomZ, y = sy * h / 2;
    const hw = w / 2 * k, hd = d / 2 * kz;
    for (const [sx, sz] of [[1, 1], [1, -1], [-1, 1], [-1, -1]]) {
      pts.push([sx * hw, y - sy * cw, sz * (hd - cw)], [sx * (hw - cw), y - sy * cw, sz * hd], [sx * (hw - cw), y, sz * (hd - cw)]);
    }
  }
  return convex(pts);
}

// sólido de revolução com poucos lados (perfil [[raio, y], ...]); sx/sz achatam
export function lathe(profile, seg = 8, { sx = 1, sz = 1, phase = 0 } = {}) {
  const g = new THREE.LatheGeometry(profile.map(([r, y]) => new THREE.Vector2(Math.max(r, 1e-4), y)), seg, phase);
  g.scale(sx, 1, sz);
  return finish(g);
}
export const cyl = (rt, rb, h, seg = 6, o = {}) => xform(finish(new THREE.CylinderGeometry(rt, rb, h, seg, 1, false, o.phase || 0)), o);
export const ico = (r, detail = 0, o = {}) => xform(finish(new THREE.IcosahedronGeometry(r, detail)), o);
export const dodeca = (r, o = {}) => xform(finish(new THREE.DodecahedronGeometry(r, 0)), o);
export const cone = (r, h, seg = 5, o = {}) => xform(finish(new THREE.ConeGeometry(r, h, seg)), o);
export const torus = (r, t, seg = 8, tseg = 4, o = {}) => xform(finish(new THREE.TorusGeometry(r, t, tseg, seg)), o);

// prisma extrudado a partir de um contorno 2D (lâminas, placas, chifres achatados)
export function extrude(points2, depth, { bevel = 0, o = {} } = {}) {
  const s = new THREE.Shape(points2.map(([x, y]) => new THREE.Vector2(x, y)));
  const g = new THREE.ExtrudeGeometry(s, { depth, bevelEnabled: bevel > 0, bevelThickness: bevel, bevelSize: bevel, bevelSegments: 1, steps: 1, curveSegments: 3 });
  g.translate(0, 0, -depth / 2);
  return xform(finish(g), o);
}

// segmento cilíndrico entre dois pontos (ossos, cabos, correntes)
export function strut(a, b, r0, r1 = r0, seg = 6) {
  const A = V(...a), B = V(...b), dir = B.clone().sub(A), len = dir.length();
  const g = finish(new THREE.CylinderGeometry(r1, r0, len, seg));
  g.translate(0, len / 2, 0);
  const q = new THREE.Quaternion().setFromUnitVectors(V(0, 1, 0), dir.normalize());
  g.applyQuaternion(q); g.translate(A.x, A.y, A.z);
  return g;
}

// placa fina de dois lados com curvatura (capa, tecidos): grade cols×rows, pontos fn(u,v)→[x,y,z]
// nrm(u,v) → direção "para fora" do lado s=+1 (normalizada no plano XZ)
export function sheet(cols, rows, fn, thick = .02, nrm = () => [0, 0, -1]) {
  const P = [], push = (a, b, c) => P.push(...a, ...b, ...c);
  const at = (u, v, s) => { const [x, y, z] = fn(u, v); const [nx, ny, nz] = nrm(u, v); return [x + nx * s * thick / 2, y + ny * s * thick / 2, z + nz * s * thick / 2]; };
  for (let j = 0; j < rows; j++) for (let i = 0; i < cols; i++) {
    const u0 = i / cols, u1 = (i + 1) / cols, v0 = j / rows, v1 = (j + 1) / rows;
    for (const s of [1, -1]) {
      const a = at(u0, v0, s), b = at(u1, v0, s), c = at(u1, v1, s), d = at(u0, v1, s);
      if (s > 0) { push(a, b, c); push(a, c, d); } else { push(a, c, b); push(a, d, c); }
    }
  }
  // bordas (fecham a espessura)
  const edge = (pts) => { for (let k = 0; k + 1 < pts.length; k++) { const [u0, v0] = pts[k], [u1, v1] = pts[k + 1]; const a = at(u0, v0, 1), b = at(u1, v1, 1), c = at(u1, v1, -1), d = at(u0, v0, -1); push(a, d, c); push(a, c, b); } };
  const line = (n, f) => Array.from({ length: n + 1 }, (_, k) => f(k / n));
  edge(line(cols, u => [u, 1])); edge(line(cols, u => [1 - u, 0])); edge(line(rows, v => [0, 1 - v])); edge(line(rows, v => [1, v]));
  const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(P, 3));
  return finish(g);
}

export const merge = gs => finish(BufferGeometryUtils.mergeGeometries(gs.map(g => g.index ? g.toNonIndexed() : g), false));
export const tris = g => (g.index ? g.index.count : g.attributes.position.count) / 3;
