#!/usr/bin/env node
// Valida os arquivos EXPORTADOS (relidos do disco), não a cena em memória:
//  GLB: relido com o GLTFLoader do three → malhas não vazias, 3D real (volume da caixa), materiais, esqueleto,
//       equipamento preso ao socket, clips; cada clip é reproduzido (AnimationMixer + skinning na CPU) para medir
//       penetração no chão, estiramento de triângulos (deformação), altura e alcance.
//  FBX: estrutura ASCII (cabeçalho 7.4, contagens de Model/LimbNode/Geometry/Cluster/AnimStack) coerente com o GLB.
//   node src/validate.mjs [--dir <pasta Generated>] [--character Hero,...]
import { readFileSync, writeFileSync, readdirSync, existsSync } from 'node:fs';
import { join, resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { THREE, GLTFLoader } from './lemo-three.mjs';

const HERE = dirname(fileURLToPath(import.meta.url)), LAB = resolve(HERE, '..');
const argv = process.argv.slice(2), arg = (k, d) => { const i = argv.indexOf('--' + k); return i >= 0 ? argv[i + 1] : d; };
const DIR = resolve(arg('dir', join(LAB, '..', '..', 'Assets/_Project/Art/Models/Generated')));
const only = arg('character', null)?.split(',');
const names = readdirSync(DIR, { withFileTypes: true }).filter(d => d.isDirectory() && existsSync(join(DIR, d.name, `${d.name}_manifest.json`))).map(d => d.name).filter(n => !only || only.includes(n));

const report = { checkedAt: new Date().toISOString(), dir: DIR, characters: [] };
let failures = 0;
for (const name of names) {
  const man = JSON.parse(readFileSync(join(DIR, name, `${name}_manifest.json`), 'utf8'));
  const errors = [], warnings = [], info = {};
  // ———————— GLB ————————
  const buf = readFileSync(join(DIR, name, `${name}.glb`));
  const ab = buf.buffer.slice(buf.byteOffset, buf.byteOffset + buf.byteLength);
  const gltf = await new Promise((res, rej) => new GLTFLoader().parse(ab, '', res, rej));
  const root = gltf.scene; root.updateMatrixWorld(true);
  const meshes = [], bones = [], mats = new Set();
  root.traverse(o => { if (o.isMesh) meshes.push(o); if (o.isBone) bones.push(o); });
  let tris = 0;
  for (const m of meshes) {
    const n = (m.geometry.index ? m.geometry.index.count : m.geometry.attributes.position.count) / 3; tris += n;
    if (!n) errors.push(`malha vazia: ${m.name}`);
    (Array.isArray(m.material) ? m.material : [m.material]).forEach(x => mats.add(x.name));
  }
  const box = new THREE.Box3().setFromObject(root), size = box.getSize(new THREE.Vector3());
  info.glb = { bytes: buf.length, meshes: meshes.length, skinned: meshes.filter(m => m.isSkinnedMesh).length, triangles: tris, bones: bones.length, materials: [...mats], clips: gltf.animations.map(a => a.name), bbox: size.toArray().map(v => +v.toFixed(3)) };
  if (Math.min(size.x, size.y, size.z) < .15) errors.push(`geometria achatada (bbox ${size.toArray()})`);
  if (Math.abs(size.y - man.heightMeters) / man.heightMeters > .2) warnings.push(`altura ${size.y.toFixed(2)} m vs alvo ${man.heightMeters} m`);
  if (box.min.y < -.02) warnings.push(`geometria abaixo do pivô em bind (${box.min.y.toFixed(3)} m)`);
  const bodyBox = new THREE.Box3(); meshes.filter(m => m.isSkinnedMesh).forEach(m => bodyBox.expandByObject(m));   // sem armas
  if (Math.abs((bodyBox.min.x + bodyBox.max.x) / 2) > .25 || Math.abs((bodyBox.min.z + bodyBox.max.z) / 2) > .35) warnings.push('pivô fora do centro da base do corpo');
  if (!meshes.some(m => m.isSkinnedMesh)) errors.push('sem SkinnedMesh');
  if (mats.size < 3) errors.push('materiais insuficientes');
  for (const e of man.equipment) {
    const node = root.getObjectByName(e.name); if (!node) { errors.push(`equipamento ausente ${e.name}`); continue; }
    let p = node.parent, chain = []; while (p) { chain.push(p.name); p = p.parent; }
    if (!chain.includes(e.socket)) errors.push(`${e.name} não está sob ${e.socket}`);
  }
  for (const c of man.clips) if (!gltf.animations.find(a => a.name === c.name)) errors.push(`clip ausente no GLB: ${c.name}`);
  if (tris > man.triangleBudget[1]) warnings.push(`acima do orçamento (${tris} > ${man.triangleBudget[1]})`);

  // reprodução dos clips com skinning na CPU
  const skinnedList = meshes.filter(m => m.isSkinnedMesh);
  const mixer = new THREE.AnimationMixer(root), v = new THREE.Vector3();
  // triângulos com pesos mistos → medir estiramento de arestas (deformação)
  const SK = skinnedList.map(skinned => {
    const bindPos = skinned.geometry.attributes.position, sw = skinned.geometry.attributes.skinWeight, mixed = [];
    for (let t = 0; t < bindPos.count; t += 3) { let mix = false; for (let k = 0; k < 3; k++) if (sw.getX(t + k) < .98) mix = true; if (mix) mixed.push(t); }
    const bindEdges = mixed.map(t => { const a = new THREE.Vector3().fromBufferAttribute(bindPos, t), b = new THREE.Vector3().fromBufferAttribute(bindPos, t + 1), c = new THREE.Vector3().fromBufferAttribute(bindPos, t + 2); return [a.distanceTo(b), b.distanceTo(c), c.distanceTo(a)]; });
    return { skinned, bindPos, mixed, bindEdges };
  });
  info.clips = [];
  for (const clip of gltf.animations) {
    const act = mixer.clipAction(clip); mixer.stopAllAction(); act.reset().play();
    let minY = 1e9, maxY = -1e9, maxStretch = 1, maxReach = 0, weaponMinY = 1e9;
    const steps = Math.max(6, Math.round(clip.duration * 15));
    for (let s = 0; s <= steps; s++) {
      mixer.setTime(Math.min(clip.duration - 1e-4, clip.duration * s / steps)); root.updateMatrixWorld(true);
      for (const { skinned, bindPos, mixed, bindEdges } of SK) {
      for (let i = 0; i < bindPos.count; i += 2) { v.fromBufferAttribute(bindPos, i); skinned.applyBoneTransform(i, v); v.applyMatrix4(skinned.matrixWorld); minY = Math.min(minY, v.y); maxY = Math.max(maxY, v.y); maxReach = Math.max(maxReach, Math.hypot(v.x, v.z)); }
      mixed.forEach((t, k) => {
        const P = [0, 1, 2].map(j => { const w = new THREE.Vector3().fromBufferAttribute(bindPos, t + j); skinned.applyBoneTransform(t + j, w); return w; });
        const e = [P[0].distanceTo(P[1]), P[1].distanceTo(P[2]), P[2].distanceTo(P[0])];
        e.forEach((x, j) => { if (bindEdges[k][j] > .01) maxStretch = Math.max(maxStretch, x / bindEdges[k][j], bindEdges[k][j] / Math.max(x, 1e-4)); });
      });
      }
      for (const e of man.equipment) root.getObjectByName(e.name).traverse(n => { if (!n.isMesh) return; n.geometry.computeBoundingBox(); const bb = n.geometry.boundingBox.clone().applyMatrix4(n.matrixWorld); weaponMinY = Math.min(weaponMinY, bb.min.y); });
    }
    const isDeath = /Death/.test(clip.name);
    const c = { name: clip.name, duration: +clip.duration.toFixed(2), tracks: clip.tracks.length, minY: +minY.toFixed(3), maxY: +maxY.toFixed(3), maxReach: +maxReach.toFixed(2), maxStretch: +maxStretch.toFixed(2), weaponMinY: man.equipment.length ? +weaponMinY.toFixed(3) : null };
    info.clips.push(c);
    if (minY < (isDeath ? -.12 : -.06)) warnings.push(`${clip.name}: corpo atravessa o chão (${minY.toFixed(3)} m)`);
    if (c.weaponMinY !== null && weaponMinY < (isDeath ? -.15 : -.1)) warnings.push(`${clip.name}: arma atravessa o chão (${weaponMinY.toFixed(3)} m)`);
    if (maxStretch > 1.8) warnings.push(`${clip.name}: estiramento de malha ${maxStretch.toFixed(2)}×`);
    if (maxReach > man.heightMeters * 1.4) errors.push(`${clip.name}: vértices longe demais (${maxReach.toFixed(2)} m) — possível membro desconectado`);
    if (clip.tracks.length < bones.length - 2) errors.push(`${clip.name}: poucas trilhas (${clip.tracks.length})`);
  }

  // ———————— FBX ————————
  const fbxPath = join(DIR, name, `${name}.fbx`);
  if (existsSync(fbxPath)) {
    const t = readFileSync(fbxPath, 'utf8'), count = re => (t.match(re) || []).length;
    info.fbx = {
      bytes: t.length, version7400: /FBXVersion: 7400/.test(t), models: count(/^\tModel: /gm), limbNodes: count(/"LimbNode" \{\n\t\tVersion: 232/g), geometries: count(/^\tGeometry: /gm),
      materials: count(/^\tMaterial: /gm), clusters: count(/"Cluster" \{/g), animStacks: count(/^\tAnimationStack: /gm), curves: count(/^\tAnimationCurve: /gm), bindPose: /"BindPose"/.test(t),
    };
    if (!info.fbx.version7400) errors.push('FBX sem versão 7400');
    if (info.fbx.limbNodes !== bones.length) errors.push(`FBX LimbNodes ${info.fbx.limbNodes} ≠ ossos GLB ${bones.length}`);
    if (info.fbx.animStacks !== gltf.animations.length) errors.push(`FBX AnimStacks ${info.fbx.animStacks} ≠ clips ${gltf.animations.length}`);
    const glbMeshes = gltf.parser.json.meshes.length;   // o GLTFLoader divide malhas multi-material em primitivas
    if (info.fbx.geometries !== glbMeshes) errors.push(`FBX Geometry ${info.fbx.geometries} ≠ malhas GLB ${glbMeshes}`);
    if (/NaN|Infinity/.test(t)) errors.push('FBX contém NaN/Infinity');
  } else errors.push('FBX ausente');

  if (errors.length) failures++;
  report.characters.push({ name, ok: !errors.length, errors, warnings, ...info });
  console.log(`\n== ${name}: ${errors.length ? 'FALHOU' : 'OK'} · ${tris} tris · ${bones.length} ossos · ${gltf.animations.length} clips · bbox ${info.glb.bbox.join('×')} m`);
  console.table(info.clips);
  warnings.forEach(w => console.log('  AVISO', w)); errors.forEach(e => console.log('  ERRO ', e));
}
writeFileSync(join(LAB, 'reports', 'validation.json'), JSON.stringify(report, null, 2));
console.log(failures ? `\n${failures} personagem(ns) com erro` : '\nVALIDAÇÃO OK');
process.exit(failures ? 1 : 0);
