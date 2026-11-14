// Exportador FBX 7.4 ASCII próprio (o Lemo/three.js não tem exportador FBX e o Blender não está instalado).
// Escreve: hierarquia (Null/LimbNode/Mesh), geometria com normais por canto (facetado), UVs, materiais por
// polígono, Skin + Clusters (bind pose), Pose BIND_POSES e um AnimationStack por clip (rotação Euler de todos
// os ossos + translação do quadril), a 30 fps. Unidades: metros (UnitScaleFactor 100), Y para cima, RH.
import { THREE } from './lemo-three.mjs';

const KTIME = 46186158000n;
const R2D = 180 / Math.PI;
const f = v => { if (!Number.isFinite(v)) throw new Error('valor não finito no FBX'); const s = (+v.toFixed(6)).toString(); return s === '-0' ? '0' : s; };
const arr = (name, a, fmt = f, ind = '\t\t') => `${ind}${name}: *${a.length} {\n${ind}\ta: ${a.map(fmt).join(',')}\n${ind}} \n`;

export function toFBX(ch) {
  let nextId = 1000000; const id = () => nextId++;
  const objects = [], conns = [], counts = {};
  const add = (type, text) => { objects.push(text); counts[type] = (counts[type] || 0) + 1; };
  const C = (a, b, prop) => conns.push(prop ? `\tC: "OP",${a},${b},"${prop}"` : `\tC: "OO",${a},${b}`);
  const modelId = new Map();   // Object3D → id

  // —— nós ——
  const root = ch.root, body = ch.mesh, rigRoot = ch.rig.root;
  root.updateMatrixWorld(true);
  const euler = q => new THREE.Euler().setFromQuaternion(q, 'ZYX');
  function model(obj, kind, parentId) {
    const mid = id(); modelId.set(obj, mid);
    const e = euler(obj.quaternion), p = obj.position, s = obj.scale;
    add('Model', `\tModel: ${mid}, "Model::${obj.name}", "${kind}" {\n\t\tVersion: 232\n\t\tProperties70:  {\n` +
      `\t\t\tP: "RotationActive", "bool", "", "",1\n\t\t\tP: "InheritType", "enum", "", "",1\n\t\t\tP: "ScalingMax", "Vector3D", "Vector", "",0,0,0\n\t\t\tP: "DefaultAttributeIndex", "int", "Integer", "",0\n` +
      `\t\t\tP: "Lcl Translation", "Lcl Translation", "", "A",${f(p.x)},${f(p.y)},${f(p.z)}\n\t\t\tP: "Lcl Rotation", "Lcl Rotation", "", "A",${f(e.x * R2D)},${f(e.y * R2D)},${f(e.z * R2D)}\n\t\t\tP: "Lcl Scaling", "Lcl Scaling", "", "A",${f(s.x)},${f(s.y)},${f(s.z)}\n` +
      `\t\t}\n\t\tShading: Y\n\t\tCulling: "CullingOff"\n\t}\n`);
    C(mid, parentId);
    if (kind === 'LimbNode' || kind === 'Null') {
      const aid = id();
      add('NodeAttribute', kind === 'LimbNode'
        ? `\tNodeAttribute: ${aid}, "NodeAttribute::${obj.name}", "LimbNode" {\n\t\tProperties70:  {\n\t\t\tP: "Size", "double", "Number", "",10\n\t\t}\n\t\tTypeFlags: "Skeleton"\n\t}\n`
        : `\tNodeAttribute: ${aid}, "NodeAttribute::${obj.name}", "Null" {\n\t\tTypeFlags: "Null"\n\t}\n`);
      C(aid, mid);
    }
    return mid;
  }

  const materialId = new Map();
  function material(m) {
    if (materialId.has(m)) return materialId.get(m);
    const mid = id(); materialId.set(m, mid);
    const c = m.color, e = m.emissive, ud = m.userData || {};
    add('Material', `\tMaterial: ${mid}, "Material::${m.name}", "" {\n\t\tVersion: 102\n\t\tShadingModel: "phong"\n\t\tMultiLayer: 0\n\t\tProperties70:  {\n` +
      `\t\t\tP: "DiffuseColor", "Color", "", "A",${f(c.r)},${f(c.g)},${f(c.b)}\n\t\t\tP: "DiffuseFactor", "Number", "", "A",1\n` +
      `\t\t\tP: "EmissiveColor", "Color", "", "A",${f(e.r)},${f(e.g)},${f(e.b)}\n\t\t\tP: "EmissiveFactor", "Number", "", "A",${f(m.emissiveIntensity || 0)}\n` +
      `\t\t\tP: "SpecularColor", "Color", "", "A",.2,.2,.2\n\t\t\tP: "Shininess", "Number", "", "A",${f((ud.smoothness ?? .2) * 100)}\n\t\t\tP: "ReflectionFactor", "Number", "", "A",${f(ud.metallic ?? 0)}\n` +
      `\t\t}\n\t}\n`);
    return mid;
  }

  // geometria: pontos de controle deduplicados por (posição, pesos); normais/UV por canto
  function geometry(mesh) {
    const g = mesh.geometry, P = g.attributes.position, N = g.attributes.normal, T = g.attributes.uv, SI = g.attributes.skinIndex, SW = g.attributes.skinWeight;
    const key = new Map(), cp = [], cpSkin = [], pvi = [], nrm = [], uv = [], uvi = [], matIdx = [];
    for (let i = 0; i < P.count; i++) {
      let k = `${P.getX(i).toFixed(5)},${P.getY(i).toFixed(5)},${P.getZ(i).toFixed(5)}`;
      if (SI) k += `|${SI.getX(i)},${SI.getY(i)},${SI.getZ(i)},${SI.getW(i)}|${SW.getX(i).toFixed(4)},${SW.getY(i).toFixed(4)}`;
      let c = key.get(k);
      if (c === undefined) { c = cp.length / 3; key.set(k, c); cp.push(P.getX(i), P.getY(i), P.getZ(i)); if (SI) cpSkin.push([[SI.getX(i), SW.getX(i)], [SI.getY(i), SW.getY(i)], [SI.getZ(i), SW.getZ(i)], [SI.getW(i), SW.getW(i)]]); }
      pvi.push(i % 3 === 2 ? -(c + 1) : c);
      nrm.push(N.getX(i), N.getY(i), N.getZ(i)); uv.push(T.getX(i), T.getY(i)); uvi.push(i);
    }
    const groups = g.groups.length ? g.groups : [{ start: 0, count: P.count, materialIndex: 0 }];
    for (const gr of groups) for (let t = 0; t < gr.count / 3; t++) matIdx.push(gr.materialIndex);
    const gid = id();
    add('Geometry', `\tGeometry: ${gid}, "Geometry::${mesh.name}", "Mesh" {\n` + arr('Vertices', cp) + arr('PolygonVertexIndex', pvi, String) +
      `\t\tGeometryVersion: 124\n\t\tLayerElementNormal: 0 {\n\t\t\tVersion: 101\n\t\t\tName: ""\n\t\t\tMappingInformationType: "ByPolygonVertex"\n\t\t\tReferenceInformationType: "Direct"\n` + arr('Normals', nrm, f, '\t\t\t') + `\t\t}\n` +
      `\t\tLayerElementUV: 0 {\n\t\t\tVersion: 101\n\t\t\tName: "UVMap"\n\t\t\tMappingInformationType: "ByPolygonVertex"\n\t\t\tReferenceInformationType: "IndexToDirect"\n` + arr('UV', uv, f, '\t\t\t') + arr('UVIndex', uvi, String, '\t\t\t') + `\t\t}\n` +
      `\t\tLayerElementMaterial: 0 {\n\t\t\tVersion: 101\n\t\t\tName: ""\n\t\t\tMappingInformationType: "ByPolygon"\n\t\t\tReferenceInformationType: "IndexToDirect"\n` + arr('Materials', matIdx, String, '\t\t\t') + `\t\t}\n` +
      `\t\tLayer: 0 {\n\t\t\tVersion: 100\n\t\t\tLayerElement:  {\n\t\t\t\tType: "LayerElementNormal"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t\tLayerElement:  {\n\t\t\t\tType: "LayerElementMaterial"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t\tLayerElement:  {\n\t\t\t\tType: "LayerElementUV"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t}\n\t}\n`);
    return { gid, cpSkin };
  }

  function meshModel(mesh, parentId) {
    const mid = model(mesh, 'Mesh', parentId);
    const { gid, cpSkin } = geometry(mesh);
    C(gid, mid);
    for (const m of (Array.isArray(mesh.material) ? mesh.material : [mesh.material])) C(material(m), mid);
    return { mid, gid, cpSkin };
  }

  // árvore: Personagem (Null) → Body (Mesh skinned) + Root (LimbNode…) → sockets (Null) → equipamentos (Mesh)
  const charId = model(root, 'Null', 0);
  const bodyInfo = meshModel(body, charId);
  (function walk(obj, parentId) {
    for (const c of obj.children) {
      if (c.isBone) walk(c, model(c, 'LimbNode', parentId));
      else if (c.isMesh) meshModel(c, parentId);
      else walk(c, model(c, 'Null', parentId));   // sockets
    }
  })({ children: [rigRoot] }, charId);

  // —— skin ——
  const bones = body.skeleton.bones, skinId = id();
  add('Deformer', `\tDeformer: ${skinId}, "Deformer::${body.name}_Skin", "Skin" {\n\t\tVersion: 101\n\t\tLink_DeformAcuracy: 50\n\t}\n`);
  C(skinId, bodyInfo.gid);
  const meshWorld = body.matrixWorld.clone();
  bones.forEach((b, bi) => {
    const idx = [], w = [];
    bodyInfo.cpSkin.forEach((sk, c) => { for (const [j, wt] of sk) if (j === bi && wt > 0) { idx.push(c); w.push(wt); } });
    if (!idx.length) return;
    const link = b.matrixWorld.clone(), xf = link.clone().invert().multiply(meshWorld);
    const cid = id();
    add('Deformer', `\tDeformer: ${cid}, "SubDeformer::${b.name}", "Cluster" {\n\t\tVersion: 100\n\t\tUserData: "", ""\n` + arr('Indexes', idx, String) + arr('Weights', w) + arr('Transform', xf.elements) + arr('TransformLink', link.elements) + `\t}\n`);
    C(cid, skinId); C(modelId.get(b), cid);
  });
  // bind pose
  const poseNodes = [[bodyInfo.mid, meshWorld], ...bones.map(b => [modelId.get(b), b.matrixWorld])];
  add('Pose', `\tPose: ${id()}, "Pose::BIND_POSES", "BindPose" {\n\t\tType: "BindPose"\n\t\tVersion: 100\n\t\tNbPoseNodes: ${poseNodes.length}\n` +
    poseNodes.map(([n, m]) => `\t\tPoseNode:  {\n\t\t\tNode: ${n}\n` + arr('Matrix', m.elements, f, '\t\t\t') + `\t\t}\n`).join('') + `\t}\n`);

  // —— animações ——
  const byName = new Map(bones.map(b => [b.name, b]));
  const takes = [];
  for (const clip of ch.clips) {
    const T = BigInt(Math.round(clip.duration * 30)) * KTIME / 30n;
    const sid = id(), lid = id();
    add('AnimationStack', `\tAnimationStack: ${sid}, "AnimStack::${clip.name}", "" {\n\t\tProperties70:  {\n\t\t\tP: "LocalStart", "KTime", "Time", "",0\n\t\t\tP: "LocalStop", "KTime", "Time", "",${T}\n\t\t\tP: "ReferenceStart", "KTime", "Time", "",0\n\t\t\tP: "ReferenceStop", "KTime", "Time", "",${T}\n\t\t}\n\t}\n`);
    add('AnimationLayer', `\tAnimationLayer: ${lid}, "AnimLayer::BaseLayer", "" {\n\t}\n`);
    C(lid, sid);
    takes.push(`\tTake: "${clip.name}" {\n\t\tFileName: "${clip.name}.tak"\n\t\tLocalTime: 0,${T}\n\t\tReferenceTime: 0,${T}\n\t}\n`);
    for (const tr of clip.tracks) {
      const [bn, prop] = tr.name.split('.'), bone = byName.get(bn);
      if (!bone) throw new Error(`track sem osso: ${tr.name}`);
      const times = Array.from(tr.times), kt = times.map(t => BigInt(Math.round(t * 30)) * KTIME / 30n);
      let comps;
      if (prop === 'quaternion') {
        const q = new THREE.Quaternion(), prev = [null, null, null]; comps = [[], [], []];
        for (let i = 0; i < times.length; i++) {
          q.fromArray(tr.values, i * 4); const e = euler(q), v = [e.x * R2D, e.y * R2D, e.z * R2D];
          for (let c = 0; c < 3; c++) { let x = v[c]; if (prev[c] !== null) { while (x - prev[c] > 180) x -= 360; while (x - prev[c] < -180) x += 360; } prev[c] = x; comps[c].push(x); }
        }
      } else if (prop === 'position') {
        comps = [[], [], []]; for (let i = 0; i < times.length; i++) for (let c = 0; c < 3; c++) comps[c].push(tr.values[i * 3 + c]);
      } else continue;
      const nid = id(), label = prop === 'quaternion' ? 'R' : 'T';
      add('AnimationCurveNode', `\tAnimationCurveNode: ${nid}, "AnimCurveNode::${label}", "" {\n\t\tProperties70:  {\n\t\t\tP: "d|X", "Number", "", "A",${f(comps[0][0])}\n\t\t\tP: "d|Y", "Number", "", "A",${f(comps[1][0])}\n\t\t\tP: "d|Z", "Number", "", "A",${f(comps[2][0])}\n\t\t}\n\t}\n`);
      C(nid, lid); C(nid, modelId.get(bone), prop === 'quaternion' ? 'Lcl Rotation' : 'Lcl Translation');
      ['X', 'Y', 'Z'].forEach((ax, c) => {
        const cid = id();
        add('AnimationCurve', `\tAnimationCurve: ${cid}, "AnimCurve::", "" {\n\t\tDefault: ${f(comps[c][0])}\n\t\tKeyVer: 4009\n` + arr('KeyTime', kt, String) + arr('KeyValueFloat', comps[c]) +
          `\t\tKeyAttrFlags: *1 {\n\t\t\ta: 24836\n\t\t} \n\t\tKeyAttrDataFloat: *4 {\n\t\t\ta: 0,0,255790911,0\n\t\t} \n\t\tKeyAttrRefCount: *1 {\n\t\t\ta: ${kt.length}\n\t\t} \n\t}\n`);
        C(cid, nid, `d|${ax}`);
      });
    }
  }

  const total = Object.values(counts).reduce((a, b) => a + b, 0) + 1;
  const now = new Date();
  const header = `; FBX 7.4.0 project file\n; Gerado por Tools/Lemo3DGenerator (three.js do Lemo-Opuscar + exportador FBX ASCII próprio)\n; ----------------------------------------------------\n\n` +
    `FBXHeaderExtension:  {\n\tFBXHeaderVersion: 1003\n\tFBXVersion: 7400\n\tCreationTimeStamp:  {\n\t\tVersion: 1000\n\t\tYear: ${now.getFullYear()}\n\t\tMonth: ${now.getMonth() + 1}\n\t\tDay: ${now.getDate()}\n\t\tHour: 0\n\t\tMinute: 0\n\t\tSecond: 0\n\t\tMillisecond: 0\n\t}\n\tCreator: "Lemo3DGenerator"\n}\n` +
    `GlobalSettings:  {\n\tVersion: 1000\n\tProperties70:  {\n\t\tP: "UpAxis", "int", "Integer", "",1\n\t\tP: "UpAxisSign", "int", "Integer", "",1\n\t\tP: "FrontAxis", "int", "Integer", "",2\n\t\tP: "FrontAxisSign", "int", "Integer", "",1\n\t\tP: "CoordAxis", "int", "Integer", "",0\n\t\tP: "CoordAxisSign", "int", "Integer", "",1\n\t\tP: "OriginalUpAxis", "int", "Integer", "",1\n\t\tP: "OriginalUpAxisSign", "int", "Integer", "",1\n\t\tP: "UnitScaleFactor", "double", "Number", "",100\n\t\tP: "OriginalUnitScaleFactor", "double", "Number", "",100\n\t\tP: "TimeMode", "enum", "", "",6\n\t\tP: "TimeSpanStart", "KTime", "Time", "",0\n\t\tP: "TimeSpanStop", "KTime", "Time", "",${KTIME * 2n}\n\t\tP: "CustomFrameRate", "double", "Number", "",30\n\t}\n}\n\n` +
    `Documents:  {\n\tCount: 1\n\tDocument: ${id()}, "Scene", "Scene" {\n\t\tProperties70:  {\n\t\t\tP: "SourceObject", "object", "", ""\n\t\t\tP: "ActiveAnimStackName", "KString", "", "", "${ch.clips[0]?.name || ''}"\n\t\t}\n\t\tRootNode: 0\n\t}\n}\n\nReferences:  {\n}\n\n` +
    `Definitions:  {\n\tVersion: 100\n\tCount: ${total}\n\tObjectType: "GlobalSettings" {\n\t\tCount: 1\n\t}\n` + Object.entries(counts).map(([k, n]) => `\tObjectType: "${k}" {\n\t\tCount: ${n}\n\t}\n`).join('') + `}\n\n`;
  return header + `Objects:  {\n${objects.join('')}}\n\nConnections:  {\n${conns.join('\n')}\n}\n\nTakes:  {\n\tCurrent: "${ch.clips[0]?.name || ''}"\n${takes.join('')}}\n`;
}
