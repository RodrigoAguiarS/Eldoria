#!/usr/bin/env node
// ANIM-TORCH — Torch_FlameLoop (lote de produção, ver Assets/_Project/Art/ProductionSpecs/ASSET_MANIFEST.json).
//
// Fonte de autoria editável da animação discreta da chama da tocha. NÃO cria nem altera GA_Torch:
//  1. Reproduz a geometria de StoneMeshFactory.Torch() (Unity, espaço da tocha, metros, frente +Z) e a separa em
//     Support (submalha 0, ferro) e Flame (submalha 1, chama) com o pivô da chama na base (centro do anel).
//     A geometria final na Unity é COPIADA das submalhas do GA_Torch original pelo ProductionAnimationIntegrator;
//     estes vértices servem para a prévia e para a conferência cruzada (o integrador compara os dois).
//  2. Define as curvas do clip (harmônicos inteiros de 1,2 s → loop sem emenda, tangentes iguais no início/fim)
//     e faz o bake a 30 fps (37 chaves, 0…1,2 s) com tangentes analíticas/numéricas no formato do AnimationCurve.
//  3. Valida: emenda, faixa de escala .98–1.02, passo máximo por quadro, deslocamento do topo, assentamento da base,
//     suporte/raiz não animados.
//  4. Escreve em <staging>/Source~ (pasta ignorada pelo AssetDatabase): ANIM-TORCH.source.json (fonte legível,
//     completa), ANIM-TORCH.unity.json (forma achatada lida pelo integrador), Torch_FlameLoop_preview.glb (intercâmbio/DCC, coordenadas glTF), preview/ (página do renderizador
//     do Lemo: window.READY + window.render(t)) e validation.json.
//
// Uso (dentro de Tools/Lemo3DGenerator):
//   node src/production/ANIM-TORCH.mjs                 gera e valida em Assets/_Project/Art/ProductionStaging/ANIM-TORCH/Source~
//   node src/production/ANIM-TORCH.mjs --out <pasta>   outra pasta de saída (iteração)
//   node src/production/ANIM-TORCH.mjs --render        também renderiza quadros com core/render/still.mjs do Lemo
//   node src/production/ANIM-TORCH.mjs --check         só valida (não escreve arquivos)
// Lemo-Opuscar é usado SOMENTE LEITURA (three.js r170 e o renderizador de stills); nada é escrito lá.
import { mkdirSync, writeFileSync, readFileSync, existsSync } from 'node:fs';
import { join, resolve, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import { THREE, GLTFExporter, GLTFLoader, THREE_VERSION, LEMO_ROOT } from '../lemo-three.mjs';

const HERE = dirname(fileURLToPath(import.meta.url)), LAB = resolve(HERE, '..', '..'), PROJECT = resolve(LAB, '..', '..');
const argv = process.argv.slice(2), has = k => argv.includes('--' + k), arg = (k, d) => { const i = argv.indexOf('--' + k); return i >= 0 ? argv[i + 1] : d; };
const STAGING = 'Assets/_Project/Art/ProductionStaging/ANIM-TORCH';
const OUT = resolve(arg('out', join(PROJECT, STAGING, 'Source~')));

// ———————————————————————— parâmetros editáveis (fonte de autoria) ————————————————————————
export const ID = 'ANIM-TORCH';
export const CLIP = {
  name: 'Torch_FlameLoop',
  duration: 1.2,          // s (ficha)
  fps: 30,                // ficha
  loop: true,
  target: 'Flame',        // caminho do filho animado relativo à raiz da variante (Animator na raiz)
};
// Base da chama = centro do anel em StoneMeshFactory.Torch (c = (0, .18, .34)); pivô de escala/inclinação.
export const FLAME_PIVOT = [0, 0.18, 0.34];
// Canais (graus / fator). Todos usam harmônicos INTEIROS do período → valor e derivada idênticos em 0 e 1,2 s.
//  roll  (eixo Z local, frente da tocha): inclinação esquerda → direita, 1 ciclo por loop.
//  pitch (eixo X local): respiro frente/trás discreto, 2 ciclos, defasado.
//  sy    (altura da chama): 2 ciclos + tremor de 5 ciclos, sempre dentro de .98–1.02.
//  sxz   (largura): contra-fase leve do sy (aparência de volume), dentro de .99–1.01.
export const CHANNELS = {
  roll: [{ amp: 3.5, k: 1, phase: 0 }, { amp: 0.6, k: 3, phase: 1.1 }],
  pitch: [{ amp: 1.2, k: 2, phase: Math.PI / 3 }],
  sy: { base: 1, terms: [{ amp: 0.015, k: 2, phase: -Math.PI / 2 }, { amp: 0.004, k: 5, phase: 0.4 }] },
  sxz: { base: 1, terms: [{ amp: -0.008, k: 2, phase: -Math.PI / 2 }] },
};
export const LIMITS = { scaleMin: 0.98, scaleMax: 1.02, maxRollDeg: 5, maxStepDegPerFrame: 1.0, maxTopDisplacementM: 0.04, seamEps: 1e-6 };
// Fases sugeridas para tochas vizinhas (cycleOffset normalizado; o integrador cria um controller por fase).
export const PHASES = [0, 0.37, 0.71];

// ———————————————————————— geometria (porta exata de StoneMeshFactory.Torch, jitter 0) ————————————————————————
// Unity: mão esquerda, faces frontais em ordem horária vistas de fora. Submalha 0 = ferro, 1 = chama.
function torchGeometry() {
  const subs = [[], []];
  const tri = (s, a, b, c) => subs[s].push([a, b, c]);
  const quad = (s, a, b, c, d) => { tri(s, a, b, c); tri(s, a, c, d); };
  const block = (s, min, max, bevel) => {
    const b = bevel;
    const b0 = [min[0], min[1], min[2]], b1 = [max[0], min[1], min[2]], b2 = [max[0], min[1], max[2]], b3 = [min[0], min[1], max[2]];
    const t0 = [min[0] + b, max[1], min[2] + b], t1 = [max[0] - b, max[1], min[2] + b], t2 = [max[0] - b, max[1], max[2] - b], t3 = [min[0] + b, max[1], max[2] - b];
    quad(s, t0, t3, t2, t1); quad(s, t0, t1, b1, b0); quad(s, t1, t2, b2, b1); quad(s, t2, t3, b3, b2); quad(s, t3, t0, b0, b3); quad(s, b1, b2, b3, b0);
  };
  block(0, [-0.08, -0.25, 0], [0.08, 0.1, 0.1], 0.02);
  block(0, [-0.04, 0.0, 0.1], [0.04, 0.06, 0.32], 0.01);
  block(0, [-0.12, 0.06, 0.24], [0.12, 0.18, 0.44], 0.04);
  const c = FLAME_PIVOT, top = [0.01, 0.58, 0.35];
  const ring = [[-0.1, 0, -0.08], [0.1, 0, -0.08], [0.12, 0, 0.06], [0, 0, 0.12], [-0.12, 0, 0.06]].map(o => [c[0] + o[0], c[1] + o[1], c[2] + o[2]]);
  for (let i = 0; i < ring.length; i++) tri(1, ring[i], top, ring[(i + 1) % ring.length]);
  const flat = s => subs[s].flat();   // ordem dos vértices = ordem do GA_Torch (Tri adiciona 3 vértices por triângulo)
  return { support: flat(0), flame: flat(1), bowl: { min: [-0.12, 0.06, 0.24], max: [0.12, 0.18, 0.44] } };
}

// ———————————————————————— matemática (convenções da Unity) ————————————————————————
const D2R = Math.PI / 180, TAU = Math.PI * 2;
const qmul = (a, b) => [a[3] * b[0] + a[0] * b[3] + a[1] * b[2] - a[2] * b[1], a[3] * b[1] - a[0] * b[2] + a[1] * b[3] + a[2] * b[0], a[3] * b[2] + a[0] * b[1] - a[1] * b[0] + a[2] * b[3], a[3] * b[3] - a[0] * b[0] - a[1] * b[1] - a[2] * b[2]];
const axisQ = (ax, deg) => { const h = deg * D2R / 2, s = Math.sin(h); return [ax[0] * s, ax[1] * s, ax[2] * s, Math.cos(h)]; };
// Quaternion.Euler(x, y, z): gira z em Z, depois x em X, depois y em Y → q = qy * qx * qz
const eulerQ = (x, y, z) => qmul(qmul(axisQ([0, 1, 0], y), axisQ([1, 0, 0], x)), axisQ([0, 0, 1], z));
const qrot = (q, v) => { const [x, y, z, w] = q, ix = w * v[0] + y * v[2] - z * v[1], iy = w * v[1] + z * v[0] - x * v[2], iz = w * v[2] + x * v[1] - y * v[0], iw = -x * v[0] - y * v[1] - z * v[2]; return [ix * w + iw * -x + iy * -z - iz * -y, iy * w + iw * -y + iz * -x - ix * -z, iz * w + iw * -z + ix * -y - iy * -x]; };
const sum = (terms, t, T) => terms.reduce((a, p) => a + p.amp * Math.sin(TAU * p.k * t / T + p.phase), 0);
const dsum = (terms, t, T) => terms.reduce((a, p) => a + p.amp * (TAU * p.k / T) * Math.cos(TAU * p.k * t / T + p.phase), 0);

export function pose(t) {
  const T = CLIP.duration;
  const roll = sum(CHANNELS.roll, t, T), pitch = sum(CHANNELS.pitch, t, T);
  const sy = CHANNELS.sy.base + sum(CHANNELS.sy.terms, t, T), sxz = CHANNELS.sxz.base + sum(CHANNELS.sxz.terms, t, T);
  return { euler: [pitch, 0, roll], q: eulerQ(pitch, 0, roll), s: [sxz, sy, sxz] };
}
function poseDerivative(t) {
  const T = CLIP.duration, h = 1e-5;
  const a = pose(t - h).q, b = pose(t + h).q;
  return { dq: a.map((v, i) => (b[i] - v) / (2 * h)), ds: [dsum(CHANNELS.sxz.terms, t, T), dsum(CHANNELS.sy.terms, t, T), dsum(CHANNELS.sxz.terms, t, T)] };
}

// ———————————————————————— bake 30 fps no formato AnimationCurve (Unity) ————————————————————————
const PROPS = ['m_LocalRotation.x', 'm_LocalRotation.y', 'm_LocalRotation.z', 'm_LocalRotation.w', 'm_LocalScale.x', 'm_LocalScale.y', 'm_LocalScale.z'];
export function bakeClip() {
  const frames = Math.round(CLIP.duration * CLIP.fps), curves = PROPS.map(p => ({ path: CLIP.target, type: 'Transform', property: p, keys: [] }));
  for (let f = 0; f <= frames; f++) {
    const t = f / CLIP.fps, P = pose(t), dP = poseDerivative(t);
    const vals = [...P.q, ...P.s], tans = [...dP.dq, ...dP.ds];
    vals.forEach((v, i) => curves[i].keys.push({ frame: f, time: +t.toFixed(6), value: +v.toFixed(7), inTangent: +tans[i].toFixed(7), outTangent: +tans[i].toFixed(7) }));
  }
  // emenda: a periodicidade garante igualdade numérica; força bit a bit após verificar
  let seamError = 0;
  for (const c of curves) {
    const a = c.keys[0], z = c.keys[c.keys.length - 1];
    seamError = Math.max(seamError, Math.abs(a.value - z.value), Math.abs(a.outTangent - z.inTangent));
    z.value = a.value; z.inTangent = a.inTangent; z.outTangent = a.outTangent;
  }
  return { frames, curves, seamError };
}

// Avaliação Hermite igual ao AnimationCurve não ponderado (usada na validação e na prévia).
export function evalCurve(keys, t) {
  if (t <= keys[0].time) return keys[0].value;
  for (let i = 0; i < keys.length - 1; i++) {
    const a = keys[i], b = keys[i + 1];
    if (t > b.time) continue;
    const dt = b.time - a.time, s = (t - a.time) / dt, s2 = s * s, s3 = s2 * s;
    return (2 * s3 - 3 * s2 + 1) * a.value + (s3 - 2 * s2 + s) * dt * a.outTangent + (-2 * s3 + 3 * s2) * b.value + (s3 - s2) * dt * b.inTangent;
  }
  return keys[keys.length - 1].value;
}
function sampleBaked(baked, t) {
  const v = baked.curves.map(c => evalCurve(c.keys, t));
  const q = v.slice(0, 4), n = Math.hypot(...q);
  return { q: q.map(x => x / n), s: v.slice(4) };
}

// ———————————————————————— validação ————————————————————————
function validate(geo, baked) {
  const errors = [], warnings = [], T = CLIP.duration, piv = FLAME_PIVOT;
  const flameLocal = geo.flame.map(v => [v[0] - piv[0], v[1] - piv[1], v[2] - piv[2]]);
  const topIndex = flameLocal.reduce((bi, v, i, a) => (v[1] > a[bi][1] ? i : bi), 0);
  const ringIdx = flameLocal.map((v, i) => (Math.abs(v[1]) < 1e-9 ? i : -1)).filter(i => i >= 0);
  let sMin = 9, sMax = -9, maxRoll = 0, maxStep = 0, maxTop = 0, minBaseY = 9, maxBaseY = -9, maxFlameDev = 0;
  const steps = 1200;   // amostragem densa (1 ms) entre as chaves
  for (let k = 0; k <= steps; k++) {
    const t = (k / steps) * T, S = sampleBaked(baked, t), ref = pose(t);
    sMin = Math.min(sMin, ...S.s); sMax = Math.max(sMax, ...S.s);
    maxRoll = Math.max(maxRoll, Math.abs(ref.euler[2]));
    const world = flameLocal.map(v => { const r = qrot(S.q, [v[0] * S.s[0], v[1] * S.s[1], v[2] * S.s[2]]); return [r[0] + piv[0], r[1] + piv[1], r[2] + piv[2]]; });
    const rest = geo.flame;
    maxTop = Math.max(maxTop, Math.hypot(world[topIndex][0] - rest[topIndex][0], world[topIndex][1] - rest[topIndex][1], world[topIndex][2] - rest[topIndex][2]));
    for (const i of ringIdx) { minBaseY = Math.min(minBaseY, world[i][1]); maxBaseY = Math.max(maxBaseY, world[i][1]); }
    // desvio do bake (Hermite) em relação à função contínua
    const R = flameLocal.map(v => { const r = qrot(ref.q, [v[0] * ref.s[0], v[1] * ref.s[1], v[2] * ref.s[2]]); return [r[0] + piv[0], r[1] + piv[1], r[2] + piv[2]]; });
    world.forEach((w, i) => { maxFlameDev = Math.max(maxFlameDev, Math.hypot(w[0] - R[i][0], w[1] - R[i][1], w[2] - R[i][2])); });
  }
  for (let f = 0; f < baked.frames; f++) {
    const a = pose(f / CLIP.fps).q, b = pose((f + 1) / CLIP.fps).q;
    const dot = Math.min(1, Math.abs(a[0] * b[0] + a[1] * b[1] + a[2] * b[2] + a[3] * b[3]));
    maxStep = Math.max(maxStep, 2 * Math.acos(dot) / D2R);
  }
  const bowlTop = geo.bowl.max[1];
  const result = {
    keysPerCurve: baked.frames + 1, curves: baked.curves.map(c => `${c.path}:${c.property}`), seamError: baked.seamError,
    scaleRange: [+sMin.toFixed(5), +sMax.toFixed(5)], maxRollDeg: +maxRoll.toFixed(3), maxStepDegPerFrame: +maxStep.toFixed(4),
    maxTopDisplacementM: +maxTop.toFixed(4), flameBaseYRangeM: [+minBaseY.toFixed(4), +maxBaseY.toFixed(4)], bowlTopYM: bowlTop,
    baseSeatDeviationM: +Math.max(bowlTop - minBaseY, maxBaseY - bowlTop).toFixed(4), hermiteVsContinuousMaxErrorM: +maxFlameDev.toExponential(2),
    animatedPaths: [...new Set(baked.curves.map(c => c.path))], rootAnimated: baked.curves.some(c => c.path === ''), supportAnimated: baked.curves.some(c => c.path === 'Support'),
    supportVertices: geo.support.length, flameVertices: geo.flame.length,
  };
  if (baked.seamError > LIMITS.seamEps) errors.push(`emenda do loop ${baked.seamError} > ${LIMITS.seamEps}`);
  if (sMin < LIMITS.scaleMin - 1e-6 || sMax > LIMITS.scaleMax + 1e-6) errors.push(`escala fora de ${LIMITS.scaleMin}–${LIMITS.scaleMax}: ${result.scaleRange}`);
  if (maxRoll > LIMITS.maxRollDeg) errors.push(`inclinação ${maxRoll}° > ${LIMITS.maxRollDeg}°`);
  if (maxStep > LIMITS.maxStepDegPerFrame) errors.push(`passo por quadro ${maxStep}° > ${LIMITS.maxStepDegPerFrame}° (estroboscópico)`);
  if (maxTop > LIMITS.maxTopDisplacementM) errors.push(`topo desloca ${maxTop} m > ${LIMITS.maxTopDisplacementM} m`);
  if (result.rootAnimated || result.supportAnimated) errors.push('raiz/suporte animados');
  if (result.animatedPaths.length !== 1 || result.animatedPaths[0] !== CLIP.target) errors.push(`caminhos animados inesperados: ${result.animatedPaths}`);
  if (result.baseSeatDeviationM > 0.01) warnings.push(`base da chama sai ${result.baseSeatDeviationM} m do topo da cuba`);
  if (minBaseY < geo.bowl.min[1]) errors.push('base da chama atravessa o fundo da cuba');
  return { ok: errors.length === 0, errors, warnings, result };
}

// ———————————————————————— GLB (intercâmbio; glTF mão direita: x → −x e winding invertido) ————————————————————————
const toGl = v => [-v[0], v[1], v[2]];
const qToGl = q => [q[0], -q[1], -q[2], q[3]];
function glGeometry(vertsUnity, offset = [0, 0, 0]) {
  const pos = [];
  for (let i = 0; i < vertsUnity.length; i += 3) for (const j of [0, 2, 1]) { const v = vertsUnity[i + j]; pos.push(...toGl([v[0] - offset[0], v[1] - offset[1], v[2] - offset[2]])); }
  const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); g.computeVertexNormals();
  return g;
}
function buildThreeTorch(geo, baked) {
  const iron = new THREE.MeshStandardMaterial({ name: 'GA_Iron', color: 0x2a2a33, metalness: 0.5, roughness: 0.65, flatShading: true });
  const flameM = new THREE.MeshStandardMaterial({ name: 'GA_Torch_Flame', color: 0xff9a2e, emissive: 0xff9a2e, emissiveIntensity: 3, roughness: 0.5, flatShading: true });
  const root = new THREE.Group(); root.name = 'Torch_FlameAnimated';
  const support = new THREE.Mesh(glGeometry(geo.support), iron); support.name = 'Support';
  const flame = new THREE.Mesh(glGeometry(geo.flame, FLAME_PIVOT), flameM); flame.name = CLIP.target; flame.position.set(...toGl(FLAME_PIVOT));
  root.add(support, flame);
  const times = baked.curves[0].keys.map(k => k.time), qv = [], sv = [];
  for (let i = 0; i < times.length; i++) {
    const q = qToGl([0, 1, 2, 3].map(c => baked.curves[c].keys[i].value)); qv.push(...q);
    sv.push(...[4, 5, 6].map(c => baked.curves[c].keys[i].value));
  }
  const clip = new THREE.AnimationClip(CLIP.name, CLIP.duration, [new THREE.QuaternionKeyframeTrack(`${CLIP.target}.quaternion`, times, qv), new THREE.VectorKeyframeTrack(`${CLIP.target}.scale`, times, sv)]);
  return { root, clip };
}
async function writeGlb(geo, baked, file) {
  const { root, clip } = buildThreeTorch(geo, baked);
  const ab = await new GLTFExporter().parseAsync(root, { binary: true, animations: [clip] });
  writeFileSync(file, Buffer.from(ab));
  // releitura do disco: confere nós, clip e pose final = inicial
  const buf = readFileSync(file), gltf = await new Promise((res, rej) => new GLTFLoader().parse(buf.buffer.slice(buf.byteOffset, buf.byteOffset + buf.byteLength), '', res, rej));
  const names = []; gltf.scene.traverse(o => names.push(o.name));
  const a = gltf.animations[0], mixer = new THREE.AnimationMixer(gltf.scene), act = mixer.clipAction(a); act.play();
  const fl = gltf.scene.getObjectByName(CLIP.target);
  mixer.setTime(0); const q0 = fl.quaternion.clone(), s0 = fl.scale.clone();
  mixer.setTime(a.duration - 1e-6); const q1 = fl.quaternion.clone(), s1 = fl.scale.clone();
  return { bytes: buf.length, nodes: names.filter(Boolean), clip: a.name, duration: +a.duration.toFixed(4), tracks: a.tracks.map(t => t.name), loopPoseDelta: +Math.max(q0.angleTo(q1), s0.distanceTo(s1)).toExponential(2) };
}

// ———————————————————————— página de prévia (renderizador de stills do Lemo) ————————————————————————
function previewHtml() {
  return `<!doctype html><html><head><meta charset="utf-8"><title>ANIM-TORCH preview</title>
<style>html,body{margin:0;height:100%;background:#15131f;overflow:hidden;font:600 15px/1.3 system-ui,sans-serif;color:#d8d6e6}
#hud{position:absolute;left:14px;top:10px;white-space:pre}#lbl{position:absolute;bottom:10px;width:100%;display:flex;justify-content:space-around;color:#9a98ad}</style>
<script type="importmap">{"imports":{"three":"/node_modules/three/build/three.module.js"}}</script></head>
<body><div id="hud"></div><div id="lbl"><span>câmera de jogo aprox. (45°, ~11 m)</span><span>close-up 3/4</span><span>close-up frontal</span></div>
<script type="module">
import * as THREE from 'three';
const data = await (await fetch('./data.json')).json();
const W = innerWidth, H = innerHeight;
const renderer = new THREE.WebGLRenderer({ antialias: true, preserveDrawingBuffer: true });
renderer.setPixelRatio(1); renderer.setSize(W, H); renderer.setScissorTest(true); document.body.prepend(renderer.domElement);
const scene = new THREE.Scene(); scene.background = new THREE.Color(0x15131f);
scene.add(new THREE.HemisphereLight(0x9fb8ff, 0x1e1830, 0.9));
const moon = new THREE.DirectionalLight(0x9fb8ff, 1.2); moon.position.set(-4, 8, -6); scene.add(moon);
const toGl = v => [-v[0], v[1], v[2]];
function geom(verts, off) { const p = []; for (let i = 0; i < verts.length; i += 3) for (const j of [0, 2, 1]) { const v = verts[i + j]; p.push(...toGl([v[0] - off[0], v[1] - off[1], v[2] - off[2]])); }
  const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(p, 3)); g.computeVertexNormals(); return g; }
const iron = new THREE.MeshStandardMaterial({ color: 0x2a2a33, metalness: .5, roughness: .65, flatShading: true });
const fire = new THREE.MeshStandardMaterial({ color: 0xff9a2e, emissive: 0xff9a2e, emissiveIntensity: 2.2, flatShading: true });
const wallM = new THREE.MeshStandardMaterial({ color: 0x3e4152, roughness: .95, flatShading: true }), floorM = new THREE.MeshStandardMaterial({ color: 0x55596b, roughness: .95, flatShading: true });
// parede (−z da tocha), piso e a tocha a 0,868 m (altura real das salas: wallH 1,4 × 0,62)
const wall = new THREE.Mesh(new THREE.BoxGeometry(6, 1.4, 1), wallM); wall.position.set(0, .7, -.5); scene.add(wall);
const floor = new THREE.Mesh(new THREE.BoxGeometry(10, .2, 8), floorM); floor.position.set(0, -.1, 3); scene.add(floor);
const torch = new THREE.Group(); torch.position.set(0, .868, 0); scene.add(torch);
torch.add(new THREE.Mesh(geom(data.geometry.support, [0, 0, 0]), iron));
const flame = new THREE.Mesh(geom(data.geometry.flame, data.geometry.flamePivot), fire); flame.position.set(...toGl(data.geometry.flamePivot)); torch.add(flame);
const ghost = new THREE.Mesh(flame.geometry, new THREE.MeshBasicMaterial({ color: 0xffffff, wireframe: true, transparent: true, opacity: .25 })); ghost.position.copy(flame.position); torch.add(ghost);
const pl = new THREE.PointLight(0xff9a2e, 4, 7, 2); pl.position.set(0, 1.318, .6); scene.add(pl);   // luz fixa (não animada), como TorchLight
function hermite(keys, t) { if (t <= keys[0].time) return keys[0].value;
  for (let i = 0; i < keys.length - 1; i++) { const a = keys[i], b = keys[i + 1]; if (t > b.time) continue; const dt = b.time - a.time, s = (t - a.time) / dt, s2 = s * s, s3 = s2 * s;
    return (2*s3-3*s2+1)*a.value + (s3-2*s2+s)*dt*a.outTangent + (-2*s3+3*s2)*b.value + (s3-s2)*dt*b.inTangent; } return keys[keys.length - 1].value; }
const cams = [
  (() => { const c = new THREE.PerspectiveCamera(40, (W/3)/H, .1, 100); c.position.set(0, .868 + 7.8, 7.8); c.lookAt(0, .868, 0); return c; })(),
  (() => { const c = new THREE.PerspectiveCamera(30, (W/3)/H, .05, 50); c.position.set(-1.1, 1.75, 1.6); c.lookAt(0, 1.2, .3); return c; })(),
  (() => { const c = new THREE.PerspectiveCamera(30, (W/3)/H, .05, 50); c.position.set(0, 1.25, 2.2); c.lookAt(0, 1.2, .3); return c; })(),
];
const hud = document.getElementById('hud'), T = data.clip.duration;
window.render = t => {
  const tt = ((t % T) + T) % T, v = data.clip.curves.map(c => hermite(c.keys, tt));
  const n = Math.hypot(v[0], v[1], v[2], v[3]); flame.quaternion.set(v[0] / n, -v[1] / n, -v[2] / n, v[3] / n); flame.scale.set(v[4], v[5], v[6]);
  hud.textContent = data.clip.name + '  t=' + tt.toFixed(3) + ' s  quadro ' + (tt * data.clip.fps).toFixed(1) + '/' + Math.round(T * data.clip.fps) + '\\nescala y ' + v[5].toFixed(4) + '   wireframe = pose de repouso (GA_Torch)';
  cams.forEach((c, i) => { renderer.setViewport(i * W / 3, 0, W / 3, H); renderer.setScissor(i * W / 3, 0, W / 3, H); renderer.render(scene, c); });
};
window.render(0); window.READY = true;
</script></body></html>`;
}

// ———————————————————————— principal ————————————————————————
const geo = torchGeometry(), baked = bakeClip(), check = validate(geo, baked);
console.log(`${ID} ${CLIP.name}: ${baked.frames + 1} chaves × ${baked.curves.length} curvas, ${CLIP.duration} s @ ${CLIP.fps} fps`);
console.log(JSON.stringify(check.result));
check.warnings.forEach(w => console.warn('AVISO', w));
if (!check.ok) { check.errors.forEach(e => console.error('ERRO', e)); process.exit(1); }
if (has('check')) process.exit(0);

mkdirSync(join(OUT, 'preview'), { recursive: true });
const generatedAt = new Date().toISOString();
const source = {
  id: ID, schema: 1, generatedAt,
  generator: { file: 'Tools/Lemo3DGenerator/src/production/ANIM-TORCH.mjs', node: process.version, lemoRoot: LEMO_ROOT, three: THREE_VERSION, lemoUsage: 'three.js r170 (núcleo, GLTFExporter, GLTFLoader) e core/render/still.mjs, somente leitura' },
  basis: { mesh: 'Assets/_Project/Art/Models/Generated/_GameArt/Meshes/GA_Torch.asset', factory: 'Assets/_Project/Art/Models/Generated/_Scripts/Editor/StoneMeshFactory.cs#Torch', submeshes: { 0: 'Support (GA_Iron)', 1: 'Flame (GA_Torch_Flame)' }, materials: ['Assets/_Project/Art/Models/Generated/_GameArt/Materials/GA_Iron.mat', 'Assets/_Project/Art/Models/Generated/_GameArt/Materials/GA_Torch_Flame.mat'] },
  units: 'Unity: metros, mão esquerda, Y acima, frente da tocha +Z, raiz da tocha no suporte de parede',
  geometry: { support: geo.support, flame: geo.flame, flamePivot: FLAME_PIVOT, bowl: geo.bowl, note: 'Vértices em espaço da tocha, 3 por triângulo, na ordem do GA_Torch. A Unity usa as submalhas do GA_Torch; este bloco é conferência cruzada.' },
  clip: { ...CLIP, frames: baked.frames, channels: CHANNELS, phases: PHASES, curves: baked.curves },
  validation: check,
};
writeFileSync(join(OUT, `${ID}.source.json`), JSON.stringify(source, null, 1));
// Forma achatada para o JsonUtility da Unity (sem arrays aninhados), consumida pelo ProductionAnimationIntegrator.
const unity = {
  id: ID, generatedAt, clipName: CLIP.name, target: CLIP.target, duration: CLIP.duration, fps: CLIP.fps, loop: CLIP.loop, phases: PHASES,
  flamePivot: { x: FLAME_PIVOT[0], y: FLAME_PIVOT[1], z: FLAME_PIVOT[2] },
  supportVertices: geo.support.flat(), flameVertices: geo.flame.flat(),
  curves: baked.curves.map(c => ({ path: c.path, property: c.property, times: c.keys.map(k => k.time), values: c.keys.map(k => k.value), inTangents: c.keys.map(k => k.inTangent), outTangents: c.keys.map(k => k.outTangent) })),
  limits: LIMITS,
};
writeFileSync(join(OUT, `${ID}.unity.json`), JSON.stringify(unity));
writeFileSync(join(OUT, 'preview', 'data.json'), JSON.stringify({ geometry: source.geometry, clip: source.clip }));
writeFileSync(join(OUT, 'preview', 'index.html'), previewHtml());
const glb = await writeGlb(geo, baked, join(OUT, `${CLIP.name}_preview.glb`));
const validation = { id: ID, checkedAt: generatedAt, ok: check.ok, errors: check.errors, warnings: check.warnings, ...check.result, glb };
writeFileSync(join(OUT, 'validation.json'), JSON.stringify(validation, null, 2));
console.log(`OK → ${relative(PROJECT, OUT)} (source.json, ${CLIP.name}_preview.glb ${glb.bytes} B, preview/, validation.json)`);

if (has('render')) {
  const still = join(LEMO_ROOT, 'core', 'render', 'still.mjs'), outDir = join(OUT, 'preview', 'stills');
  if (!existsSync(still)) { console.error('renderizador do Lemo não encontrado:', still); process.exit(1); }
  const times = Array.from({ length: 9 }, (_, i) => (i * CLIP.duration / 8).toFixed(3));
  const r = spawnSync(process.execPath, [still, join(OUT, 'preview'), ...times, '--out', outDir, '--size', '1500x500', '--prefix', 'torch_t'], { stdio: 'inherit', cwd: LEMO_ROOT });
  if (r.status !== 0) process.exit(r.status ?? 1);
  console.log('stills →', relative(PROJECT, outDir));
}
