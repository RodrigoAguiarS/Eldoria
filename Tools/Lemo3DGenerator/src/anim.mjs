// Animação procedural por poses-chave ("poses are data", como no Lemo: brick-toy/actors.js pose()/walkPose()).
// Pose = { Osso: [rx, ry, rz] graus (rotação local, ordem XYZ), $hips: [x, y, z] deslocamento em metros }.
// Convenções (personagem olha +Z, esquerda = +X):
//   braço/perna para frente = X negativo · cotovelo dobra = X negativo · joelho dobra = X positivo
//   inclinar tronco/cabeça para frente = X positivo · abrir braço esquerdo = +Z, direito = −Z · girar para a esquerda = +Y
// Todas as animações são in-place (a Unity move o personagem pelos scripts existentes).
import { THREE } from './lemo-three.mjs';

const D = Math.PI / 180;
export const FPS = 30;

function catmull(p0, p1, p2, p3, t) {
  const t2 = t * t, t3 = t2 * t;
  return .5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3);
}

// valor de um canal (bone, componente) no tempo t
function channel(keys, bone, c, t, loop, dur) {
  const n = keys.length, val = k => (keys[k][1][bone] ? keys[k][1][bone][c] ?? 0 : 0);
  let i = 0; while (i < n - 2 && keys[i + 1][0] <= t) i++;
  const [t1, t2] = [keys[i][0], keys[i + 1][0]], u = t2 > t1 ? Math.min(1, Math.max(0, (t - t1) / (t2 - t1))) : 0;
  // vizinhos: em loops usa o penúltimo/segundo quadro (o último repete o primeiro)
  const prev = i > 0 ? val(i - 1) : loop ? val(n - 2) : val(i);
  const next = i + 2 < n ? val(i + 2) : loop ? val(1) : val(i + 1);
  void dur;
  return catmull(prev, val(i), val(i + 1), next, u);
}

export function evalPose(clip, t, base = {}) {
  const keys = clip.keys, out = {};
  const names = new Set(); for (const [, p] of keys) Object.keys(p).forEach(k => names.add(k));
  if (clip.extra) Object.keys(clip.extra(0) || {}).forEach(k => names.add(k));
  Object.keys(base).forEach(k => names.add(k));
  const ex = clip.extra ? clip.extra(t / clip.duration, t) || {} : {};
  for (const b of names) {
    const v = [0, 1, 2].map(c => channel(keys, b, c, t, clip.loop, clip.duration) + (ex[b]?.[c] ?? 0) + (base[b]?.[c] ?? 0));
    out[b] = v;
  }
  return out;
}

// Gera THREE.AnimationClip amostrado a 30 fps (rotação de todos os ossos citados + posição do quadril).
export function bake(clip, rig, base = {}, scale = 1) {
  const frames = Math.max(2, Math.round(clip.duration * FPS) + 1), times = [];
  for (let f = 0; f < frames; f++) times.push(Math.min(clip.duration, f / FPS));
  const samples = times.map(t => evalPose(clip, t, base));
  samples.forEach(s => Object.keys(s).forEach(k => { if (k !== '$hips' && !rig.bones[k]) throw new Error(`clip ${clip.name}: osso inexistente ${k}`); }));
  // todos os ossos em todos os clips (poses previsíveis ao trocar de estado no Animator)
  const bones = rig.list.map(b => b.name).filter(n => n !== 'Root');
  const tracks = [];
  for (const b of bones) {
    const q = new THREE.Quaternion(), vals = [];
    let prev = null;
    for (const s of samples) {
      const [x, y, z] = s[b] || [0, 0, 0];
      q.setFromEuler(new THREE.Euler(x * D, y * D, z * D, 'XYZ'));
      if (prev && prev.dot(q) < 0) q.set(-q.x, -q.y, -q.z, -q.w);   // continuidade
      vals.push(q.x, q.y, q.z, q.w); prev = q.clone();
    }
    tracks.push(new THREE.QuaternionKeyframeTrack(`${b}.quaternion`, times, vals));
  }
  const hip = rig.bones.Hips.position, hv = [];
  for (const s of samples) { const o = s.$hips || [0, 0, 0]; hv.push(hip.x + o[0] * scale, hip.y + o[1] * scale, hip.z + o[2] * scale); }
  tracks.push(new THREE.VectorKeyframeTrack('Hips.position', times, hv));
  const ac = new THREE.AnimationClip(clip.name, clip.duration, tracks);
  ac.userData = { loop: !!clip.loop, events: clip.events || {} };
  return ac;
}

// ———————————————————— biblioteca de movimentos humanoides ————————————————————
const L = (...k) => k;   // açúcar para listas de chaves
const sin = (u, ph = 0) => Math.sin((u + ph) * Math.PI * 2);

// Caminhada/corrida paramétrica (ciclo de 2 passos, loop perfeito).
export function locomotion(name, { duration, stride, knee, armSwing, elbow, lean, bob, twist = 4, sword = 0, guard = null, hunch = 0 }) {
  return {
    name, duration, loop: true,
    keys: L([0, {}], [duration, {}]),
    extra: (u) => {
      const s = sin(u), c = Math.cos(u * Math.PI * 2);
      const kneeL = knee * Math.max(0, Math.sin(u * Math.PI * 2 + Math.PI * .5)) + knee * .15;
      const kneeR = knee * Math.max(0, Math.sin(u * Math.PI * 2 + Math.PI * 1.5)) + knee * .15;
      const p = {
        UpperLeg_L: [-stride * s, 0, 0], UpperLeg_R: [stride * s, 0, 0],
        LowerLeg_L: [kneeL, 0, 0], LowerLeg_R: [kneeR, 0, 0],
        Foot_L: [stride * s * .5 - kneeL * .35, 0, 0], Foot_R: [-stride * s * .5 - kneeR * .35, 0, 0],
        UpperArm_L: [armSwing * s, 0, 4], UpperArm_R: [-armSwing * s * (sword ? .55 : 1), 0, -4 - sword * 6],
        LowerArm_L: [-elbow - Math.max(0, s) * elbow * .6, 0, 0], LowerArm_R: [-elbow - Math.max(0, -s) * elbow * .6 - sword * 10, 0, 0],
        Spine: [lean * .5, twist * s, 0], Chest: [lean * .5 + hunch, -twist * 1.6 * s, 0], Head: [-lean * .6 - hunch * .5, twist * .8 * s, 0],
        Neck: [0, 0, 0],
        Hips: [0, -twist * s, 0],
        $hips: [0, bob * (Math.abs(c) - .5), 0],   // alto na passagem, baixo no contato
      };
      if (guard) Object.assign(p, guard(u));
      return p;
    },
  };
}

export function idle(name, { duration = 2.4, breath = 2.2, sway = 1.2, armOut = 6, sword = 0, hunch = 0 }) {
  return {
    name, duration, loop: true, keys: L([0, {}], [duration, {}]),
    extra: (u) => {
      const b = sin(u), b2 = sin(u, .25);
      return {
        Spine: [breath * .4 * b + hunch * .5, sway * b2, 0], Chest: [-breath * b + hunch, 0, 0], Neck: [breath * .5 * b, 0, 0], Head: [breath * .3 * b - hunch * .6, sway * 2 * b2, 0],
        Shoulder_L: [0, 0, 1.5 * b], Shoulder_R: [0, 0, -1.5 * b],
        UpperArm_L: [-2 + 2 * b, 0, armOut + 1.5 * b], UpperArm_R: [-4 - sword * 6 + 2 * b, 0, -armOut - 1.5 * b],
        LowerArm_L: [-12, 0, 0], LowerArm_R: [-14 - sword * 14, 0, 0],
        UpperLeg_L: [-3, 0, 2], UpperLeg_R: [3, 0, -2], LowerLeg_L: [5, 0, 0], LowerLeg_R: [5, 0, 0], Foot_L: [-2, 0, 0], Foot_R: [-8, 0, 0],
        $hips: [0, -.012 + .008 * b, 0],
      };
    },
  };
}

// Ataque com a mão direita: guarda → preparação → golpe → acompanhamento → retorno
export function slash(name, { duration, windup, hit, heavy = false, events = {} }) {
  const w = windup, h = hit;
  const ready = { UpperArm_R: [-20, 0, -10], LowerArm_R: [-40, 0, 0], UpperArm_L: [-10, 0, 12], LowerArm_L: [-25, 0, 0], UpperLeg_L: [-12, 0, 3], UpperLeg_R: [12, 0, -3], LowerLeg_L: [8, 0, 0], LowerLeg_R: [10, 0, 0], Foot_L: [4, 0, 0], Foot_R: [-12, 0, 0], $hips: [0, -.03, 0] };
  const cock = heavy
    ? { Spine: [-12, 0, 0], Chest: [-14, 15, 0], Head: [10, -10, 0], UpperArm_R: [-145, 0, -30], LowerArm_R: [-45, 0, 0], Hand_R: [-30, 0, 0], UpperArm_L: [-140, 0, 28], LowerArm_L: [-60, 0, 0], UpperLeg_L: [-25, 0, 4], UpperLeg_R: [20, 0, -4], LowerLeg_L: [20, 0, 0], LowerLeg_R: [25, 0, 0], Foot_L: [5, 0, 0], Foot_R: [-20, 0, 0], $hips: [0, -.06, -.04] }
    : { Spine: [0, 25, 0], Chest: [0, 30, 0], Head: [0, -35, 0], UpperArm_R: [-70, -40, -75], LowerArm_R: [-70, 0, 0], Hand_R: [0, 0, 20], UpperArm_L: [-20, 0, 30], LowerArm_L: [-50, 0, 0], UpperLeg_L: [-14, 0, 5], UpperLeg_R: [14, 0, -5], LowerLeg_L: [10, 0, 0], LowerLeg_R: [14, 0, 0], Foot_L: [4, 0, 0], Foot_R: [-14, 0, 0], $hips: [0, -.04, 0] };
  const strike = heavy
    ? { Hips: [10, 0, 0], Spine: [14, 0, 0], Chest: [18, -5, 0], Head: [-20, 4, 0], UpperArm_R: [-82, 0, -8], LowerArm_R: [-12, 0, 0], Hand_R: [0, 0, 0], UpperArm_L: [-76, 0, 10], LowerArm_L: [-15, 0, 0], UpperLeg_L: [-50, 0, 4], UpperLeg_R: [15, 0, -4], LowerLeg_L: [45, 0, 0], LowerLeg_R: [30, 0, 0], Foot_L: [-5, 0, 0], Foot_R: [-25, 0, 0], $hips: [0, -.16, .06] }
    : { Spine: [6, -25, 0], Chest: [8, -35, 0], Head: [-6, 40, 0], UpperArm_R: [-80, 30, -60], LowerArm_R: [-10, 0, 0], Hand_R: [0, 0, -25], UpperArm_L: [-5, 0, 20], LowerArm_L: [-30, 0, 0], UpperLeg_L: [-24, 0, 5], UpperLeg_R: [18, 0, -5], LowerLeg_L: [22, 0, 0], LowerLeg_R: [16, 0, 0], Foot_L: [2, 0, 0], Foot_R: [-18, 0, 0], $hips: [0, -.07, .03] };
  const follow = heavy
    ? { ...strike, Spine: [16, 0, 0], Chest: [20, -5, 0], UpperArm_R: [-70, 0, -8], UpperArm_L: [-66, 0, 10] }
    : { ...strike, Chest: [6, -45, 0], UpperArm_R: [-55, 40, -45], Hand_R: [0, 0, -35] };
  return {
    name, duration, loop: false, events: { hit: h, ...events },
    keys: L([0, ready], [w * .45, { ...ready, ...cock, Chest: cock.Chest.map(v => v * .7) }], [w, cock], [h, strike], [h + (duration - h) * .3, follow], [duration, ready]),
  };
}

// Hero-only reverse horizontal slash; existing character motions are untouched.
export function heroCounterSlash(name, { duration, windup, hit }) {
  const ready = { UpperArm_R: [-55, 40, -45], LowerArm_R: [-25, 0, 0], Chest: [6, -35, 0], UpperArm_L: [-15, 0, 25], LowerArm_L: [-40, 0, 0], $hips: [0, -.04, 0] };
  const cock = { ...ready, Hips: [0, -18, 0], Spine: [3, -22, 0], Chest: [4, -38, 0], Head: [0, 35, 0], UpperArm_R: [-80, 55, -65], LowerArm_R: [-60, 0, 0], Hand_R: [0, 0, -65] };
  const strike = { ...ready, Hips: [0, 12, 0], Spine: [3, 24, 0], Chest: [4, 38, 0], Head: [0, -32, 0], UpperArm_R: [-85, -45, -78], LowerArm_R: [-8, 0, 0], Hand_R: [0, 0, 60], UpperLeg_L: [-18, 0, 5], UpperLeg_R: [14, 0, -5] };
  return { name, duration, loop: false, events: { hit }, keys: L([0, ready], [windup, cock], [hit, strike], [hit + .09, { ...strike, Chest: [4, 48, 0], UpperArm_R: [-65, -55, -65] }], [duration, ready]) };
}

export function dash(name, duration = .4) {
  const tuck = { Hips: [12, 0, 0], Spine: [14, 0, 0], Chest: [14, 0, 0], Head: [-22, 0, 0], UpperArm_L: [45, 0, 15], UpperArm_R: [40, 0, -18], LowerArm_L: [-20, 0, 0], LowerArm_R: [-25, 0, 0], UpperLeg_L: [-67, 0, 4], UpperLeg_R: [23, 0, -4], LowerLeg_L: [60, 0, 0], LowerLeg_R: [40, 0, 0], Foot_L: [-5, 0, 0], Foot_R: [-20, 0, 0], $hips: [0, -.18, 0] };
  return { name, duration, loop: false, keys: L([0, {}], [duration * .25, tuck], [duration * .7, { ...tuck, $hips: [0, -.14, 0] }], [duration, {}]) };
}

export function hit(name, duration = .45, k = 1) {
  const recoil = { Spine: [-12 * k, 8, 0], Chest: [-14 * k, 10, 0], Head: [-18 * k, -12, 6], UpperArm_L: [-30, 0, 30], UpperArm_R: [-20, 0, -35], LowerArm_L: [-40, 0, 0], LowerArm_R: [-30, 0, 0], UpperLeg_L: [6, 0, 0], LowerLeg_L: [10, 0, 0], LowerLeg_R: [14, 0, 0], $hips: [0, -.04, -.06] };
  return { name, duration, loop: false, keys: L([0, {}], [duration * .2, recoil], [duration * .45, { ...recoil, Head: [-10 * k, -6, 3] }], [duration, {}]) };
}

// Queda para trás terminando deitado no chão (último quadro mantido pelo Animator).
export function death(name, { duration = 1.5, hipsRest = -.5, back = -.4 } = {}) {
  const stagger = { Spine: [-10, 10, 0], Chest: [-15, 10, 0], Head: [-25, 0, 0], UpperArm_L: [-40, 0, 40], UpperArm_R: [-30, 0, -45], LowerArm_L: [-30, 0, 0], LowerArm_R: [-30, 0, 0], UpperLeg_L: [-10, 0, 0], LowerLeg_L: [30, 0, 0], LowerLeg_R: [20, 0, 0], $hips: [0, -.08, -.08] };
  const knees = { Hips: [-20, 0, 0], Spine: [-15, 0, 0], Chest: [-10, 0, 0], Head: [-20, 0, 0], UpperArm_L: [-20, 0, 60], UpperArm_R: [-20, 0, -60], LowerArm_L: [-20, 0, 0], LowerArm_R: [-20, 0, 0], UpperLeg_L: [-50, 0, 6], UpperLeg_R: [-40, 0, -6], LowerLeg_L: [90, 0, 0], LowerLeg_R: [80, 0, 0], Foot_L: [-30, 0, 0], Foot_R: [-30, 0, 0], $hips: [0, -.32, back * .4] };
  const down = { Hips: [-82, 0, 0], Spine: [-4, 0, 0], Chest: [-2, 0, 0], Head: [-6, 25, 0], UpperArm_L: [-10, 0, 75], UpperArm_R: [-10, 0, -70], LowerArm_L: [-15, 0, 0], LowerArm_R: [-20, 0, 0], UpperLeg_L: [-14, 0, 9], UpperLeg_R: [-4, 0, -11], LowerLeg_L: [28, 0, 0], LowerLeg_R: [14, 0, 0], Foot_L: [20, 0, 0], Foot_R: [25, 0, 0], $hips: [0, hipsRest, back] };
  return { name, duration, loop: false, keys: L([0, {}], [duration * .18, stagger], [duration * .45, knees], [duration * .8, { ...down, $hips: [0, hipsRest + .03, back] }], [duration, down]) };
}

export function victory(name, duration = 2) {
  const raise = { Spine: [-6, 0, 0], Chest: [-10, 0, 0], Head: [-15, 0, 0], UpperArm_R: [-150, 0, -38], LowerArm_R: [-15, 0, 0], Hand_R: [-35, 0, 0], UpperArm_L: [-20, 0, 35], LowerArm_L: [-95, 0, 0], Hand_L: [0, 0, 20], UpperLeg_L: [-6, 0, 6], UpperLeg_R: [4, 0, -6], LowerLeg_L: [4, 0, 0], LowerLeg_R: [4, 0, 0] };
  return { name, duration, loop: false, keys: L([0, {}], [duration * .3, { ...raise, $hips: [0, -.05, 0] }], [duration * .45, { ...raise, UpperArm_R: [-158, 0, -34], $hips: [0, .02, 0] }], [duration, { ...raise, $hips: [0, 0, 0] }]) };
}
