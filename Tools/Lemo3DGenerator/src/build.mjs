// Monta um personagem a partir da definição: rig → materiais → SkinnedMesh → equipamentos → clips.
import { THREE, GLTFExporter } from './lemo-three.mjs';
import { humanoidSkeleton, makeMaterials, buildSkinnedMesh, attach, stats } from './rig.mjs';
import { bake } from './anim.mjs';

const D = Math.PI / 180;

export async function loadCharacter(id) {
  return import(new URL(`./characters/${id}.mjs`, import.meta.url).href);
}

// def.scale: o personagem é desenhado no tamanho base e escalado uniformemente (geometria, ossos e
// deslocamentos do quadril), p.ex. o elite = esqueleto 1,4× (documento oficial).
export function assemble(mod) {
  const def = mod.definition, k = def.scale || 1;
  const P = Object.fromEntries(Object.entries(mod.P).map(([n, v]) => [n, typeof v === 'number' ? v * k : v]));
  const scaleGeo = list => k === 1 ? list : list.map(p => ({ ...p, geo: p.geo.scale(k, k, k), weights: p.weights ? (x, y, z) => p.weights(x / k, y / k, z / k) : undefined }));
  const extra = (def.extraBones || []).map(([n, par, pos, kind]) => [n, par, pos.map(v => v * k), kind]);
  const rig = humanoidSkeleton(P, extra);
  for (const [name, s] of Object.entries(def.sockets || {})) if (rig.sockets[name] && s.rot) rig.sockets[name].rotation.set(...s.rot.map(v => v * D));
  rig.root.updateMatrixWorld(true);
  const M = makeMaterials(def.name, def.materials);
  const mesh = buildSkinnedMesh(`${def.name}_Body`, scaleGeo(mod.body(M)), rig, M);
  const equipment = [];
  for (const e of (mod.equipment ? mod.equipment(M) : [])) {
    const sock = rig.sockets[e.socket] || rig.bones[e.socket];
    if (!sock) throw new Error(`socket inexistente ${e.socket}`);
    equipment.push({ socket: e.socket, mesh: attach(sock, `${def.name}_${e.name}`, scaleGeo(e.parts)) });
  }
  const root = new THREE.Group(); root.name = def.name; root.add(mesh);
  root.updateMatrixWorld(true);
  const clipDefs = mod.clips();
  const clips = clipDefs.map(c => groundClamp(bake(c, rig, def.basePose || {}, k), mesh, root));
  return { def, P, rig, M, mesh, root, equipment, clips, clipDefs, stats: stats(root) };
}

// Contato com o chão (substitui IK de pés): reproduz o clip com skinning na CPU e, em cada quadro em que
// algum vértice fica abaixo de y=0, ergue o quadril o necessário. Nunca abaixa (saltos/corrida ficam intactos).
function groundClamp(clip, mesh, root) {
  const hipTrack = clip.tracks.find(t => t.name === 'Hips.position');
  const mixer = new THREE.AnimationMixer(root), act = mixer.clipAction(clip); act.play();
  const P = mesh.geometry.attributes.position, v = new THREE.Vector3(), lifts = [];
  for (let k = 0; k < hipTrack.times.length; k++) {
    mixer.setTime(Math.min(hipTrack.times[k], clip.duration - 1e-5)); root.updateMatrixWorld(true);
    let minY = 1e9;
    for (let i = 0; i < P.count; i += 3) { v.fromBufferAttribute(P, i); mesh.applyBoneTransform(i, v); minY = Math.min(minY, v.y); }
    lifts.push(minY < 0 ? -minY : 0);
  }
  mixer.stopAllAction(); mixer.uncacheRoot(root);
  // suaviza o ajuste (evita trancos quando o ponto mais baixo troca de pé)
  const sm = lifts.map((_, k) => Math.max(lifts[k], ((lifts[k - 1] ?? lifts[k]) + 2 * lifts[k] + (lifts[k + 1] ?? lifts[k])) / 4));
  sm.forEach((l, k) => { hipTrack.values[k * 3 + 1] += l; });
  if (clip.userData.loop) { const n = sm.length - 1; hipTrack.values[n * 3 + 1] = hipTrack.values[1]; }
  // repõe a pose de bind
  mesh.skeleton.pose(); root.updateMatrixWorld(true);
  clip.userData.groundLift = +Math.max(...sm).toFixed(3);
  return clip;
}

export async function toGLB(char) {
  const ab = await new GLTFExporter().parseAsync(char.root, { binary: true, animations: char.clips, onlyVisible: false });
  return Buffer.from(ab);
}
