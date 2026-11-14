// Rig humanoide reutilizável: hierarquia de ossos com nomes compatíveis com o Humanoid da Unity,
// montagem de uma SkinnedMesh multi-material (pesos rígidos por peça + pesos mistos na capa),
// soquetes (sockets) para armas/equipamentos e materiais a partir da paleta oficial.
import { THREE } from './lemo-three.mjs';
import { finish, tris } from './geo.mjs';

// Esqueleto a partir de proporções (metros). Personagem olha para +Z, esquerda do personagem = +X.
export function humanoidSkeleton(P, extra = []) {
  const s = P.shoulderY, h = P.hipY;
  const defs = [
    ['Root', null, [0, 0, 0]],
    ['Hips', 'Root', [0, h, 0]],
    ['Spine', 'Hips', [0, P.spineY - h, 0]],
    ['Chest', 'Spine', [0, P.chestY - P.spineY, 0]],
    ['Neck', 'Chest', [0, P.neckY - P.chestY, 0]],
    ['Head', 'Neck', [0, P.headY - P.neckY, 0]],
  ];
  for (const [side, sx] of [['L', 1], ['R', -1]]) {
    defs.push(
      [`Shoulder_${side}`, 'Chest', [sx * P.clavicle, s - P.chestY, 0]],
      [`UpperArm_${side}`, `Shoulder_${side}`, [sx * (P.shoulderX - P.clavicle), 0, 0]],
      [`LowerArm_${side}`, `UpperArm_${side}`, [0, -(s - P.elbowY), 0]],
      [`Hand_${side}`, `LowerArm_${side}`, [0, -(P.elbowY - P.wristY), 0]],
      [`Socket_Hand_${side}`, `Hand_${side}`, [0, -P.gripDrop, P.gripFwd ?? 0], 'socket'],
      [`UpperLeg_${side}`, 'Hips', [sx * P.legX, P.legTopY - h, 0]],
      [`LowerLeg_${side}`, `UpperLeg_${side}`, [0, -(P.legTopY - P.kneeY), 0]],
      [`Foot_${side}`, `LowerLeg_${side}`, [0, -(P.kneeY - P.ankleY), 0]],
      [`Toes_${side}`, `Foot_${side}`, [0, -P.ankleY * .6, P.toeZ]],
    );
  }
  defs.push(...extra);
  const bones = {}, list = [], sockets = {};
  for (const [name, parent, pos, kind] of defs) {
    const b = kind === 'socket' ? new THREE.Object3D() : new THREE.Bone();
    b.name = name; b.position.set(...pos);
    if (kind === 'socket') { sockets[name] = b; b.userData.socket = true; } else list.push(b);
    if (parent) bones[parent].add(b);
    bones[name] = b;
  }
  bones.Root.updateMatrixWorld(true);
  return { root: bones.Root, bones, list, sockets };
}

// Materiais (MeshStandardMaterial → glTF PBR; o FBX recebe cor difusa/emissiva e o Unity cria URP Lit).
export function makeMaterials(prefix, spec) {
  const M = {};
  for (const [key, m] of Object.entries(spec)) {
    const mat = new THREE.MeshStandardMaterial({
      name: `${prefix}_${key}`, color: m.color, flatShading: true,
      metalness: m.metallic ?? 0, roughness: 1 - (m.smoothness ?? .2),
      emissive: m.emissive ? m.emissive : '#000000', emissiveIntensity: m.emissiveIntensity ?? (m.emissive ? 1 : 0),
    });
    mat.userData = { key, ...m };
    M[key] = mat;
  }
  return M;
}

// Peças: { geo, mat, bone } (rígida) ou { geo, mat, weights:(x,y,z)=>[[bone,w],...] } (mista).
// Monta uma única SkinnedMesh com um grupo (submesh) por material.
export function buildSkinnedMesh(name, parts, rig, materials) {
  const boneIndex = new Map(rig.list.map((b, i) => [b.name, i]));
  const byMat = new Map();
  for (const part of parts) { const k = part.mat.name; if (!byMat.has(k)) byMat.set(k, { mat: part.mat, list: [] }); byMat.get(k).list.push(part); }
  const pos = [], nrm = [], uv = [], si = [], sw = [], groups = [], mats = [];
  let start = 0;
  for (const { mat, list } of byMat.values()) {
    let count = 0;
    for (const part of list) {
      const g = finish(part.geo);
      const p = g.attributes.position, n = g.attributes.normal, t = g.attributes.uv;
      for (let i = 0; i < p.count; i++) {
        pos.push(p.getX(i), p.getY(i), p.getZ(i)); nrm.push(n.getX(i), n.getY(i), n.getZ(i)); uv.push(t.getX(i), t.getY(i));
        let w = part.bone ? [[part.bone, 1]] : part.weights(p.getX(i), p.getY(i), p.getZ(i));
        w = w.filter(([, x]) => x > 1e-4).sort((a, b) => b[1] - a[1]).slice(0, 4);
        const tot = w.reduce((s, [, x]) => s + x, 0);
        for (let k = 0; k < 4; k++) {
          if (!w[k]) { si.push(0); sw.push(0); continue; }
          if (!boneIndex.has(w[k][0])) throw new Error(`osso inexistente: ${w[k][0]}`);
          si.push(boneIndex.get(w[k][0])); sw.push(w[k][1] / tot);
        }
      }
      count += p.count;
    }
    groups.push({ start, count, materialIndex: mats.length }); mats.push(mat); start += count;
  }
  const geo = new THREE.BufferGeometry();
  geo.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3));
  geo.setAttribute('normal', new THREE.Float32BufferAttribute(nrm, 3));
  geo.setAttribute('uv', new THREE.Float32BufferAttribute(uv, 2));
  geo.setAttribute('skinIndex', new THREE.Uint16BufferAttribute(si, 4));
  geo.setAttribute('skinWeight', new THREE.Float32BufferAttribute(sw, 4));
  for (const g of groups) geo.addGroup(g.start, g.count, g.materialIndex);
  geo.computeBoundingBox(); geo.computeBoundingSphere();
  const mesh = new THREE.SkinnedMesh(geo, mats);
  mesh.name = name;
  const skeleton = new THREE.Skeleton(rig.list);
  mesh.add(rig.root);
  mesh.bind(skeleton, new THREE.Matrix4());
  return mesh;
}

// Equipamento rígido preso a um socket (malha própria → objeto separado e trocável na Unity).
export function attach(socket, name, parts) {
  const holder = new THREE.Group(); holder.name = name;
  const byMat = new Map();
  for (const p of parts) { if (!byMat.has(p.mat)) byMat.set(p.mat, []); byMat.get(p.mat).push(finish(p.geo)); }
  const gs = [], mats = []; let start = 0; const pos = [], nrm = [], uv = [];
  for (const [mat, list] of byMat) {
    let count = 0;
    for (const g of list) { const a = g.attributes; for (let i = 0; i < a.position.count; i++) { pos.push(a.position.getX(i), a.position.getY(i), a.position.getZ(i)); nrm.push(a.normal.getX(i), a.normal.getY(i), a.normal.getZ(i)); uv.push(a.uv.getX(i), a.uv.getY(i)); } count += a.position.count; }
    gs.push({ start, count, materialIndex: mats.length }); mats.push(mat); start += count;
  }
  const geo = new THREE.BufferGeometry();
  geo.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); geo.setAttribute('normal', new THREE.Float32BufferAttribute(nrm, 3)); geo.setAttribute('uv', new THREE.Float32BufferAttribute(uv, 2));
  for (const g of gs) geo.addGroup(g.start, g.count, g.materialIndex);
  geo.computeBoundingBox();
  const mesh = new THREE.Mesh(geo, mats); mesh.name = name;
  socket.add(mesh);
  return mesh;
}

export function stats(root) {
  let t = 0, meshes = 0; const mats = new Set();
  root.traverse(o => { if (o.isMesh) { meshes++; t += tris(o.geometry); (Array.isArray(o.material) ? o.material : [o.material]).forEach(m => mats.add(m.name)); } });
  return { triangles: t, meshes, materials: [...mats] };
}
