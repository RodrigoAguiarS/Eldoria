// Elite — ART_DIRECTION_AND_PROMPTS.md, Asset 02: "mesmo modelo escalado 1,4× com ombreiras extras e olhos laranja".
// Enemy_Elite.prefab (Body 1,4×) / EnemyData: windup 0,5 s · alcance 2,2 m · velocidade 3,8 m/s.
import { SKELETON_P, SKELETON_MATERIALS, HUNCH, skeletonBody, skull, spikedPauldron, jaggedSword } from './_skeleton_kit.mjs';
import { enemyClips } from './skeleton_melee.mjs';

export const P = SKELETON_P;
export const definition = {
  id: 'skeleton_elite', name: 'SkeletonElite', role: 'Inimigo elite (Enemy_Elite.prefab)', source: 'ART_DIRECTION_AND_PROMPTS.md — Asset 02 (variante elite)',
  documented: true, heightMeters: 2.8, scale: 1.4, triangleBudget: [3000, 4500],
  materials: { ...SKELETON_MATERIALS, eyes: { color: '#FF9A2E', emissive: '#FF9A2E', emissiveIntensity: 3, smoothness: .5, note: 'olhos laranja da elite (laranja oficial #FF9A2E)' } },
  basePose: HUNCH,
  locomotion: { Idle: 0, Chase: 3.8 },
  sockets: { Socket_Hand_R: { rot: [48, 0, 0] } },
};

export function body(M) {
  const out = skeletonBody(P, M, { bracers: ['L', 'R'], greaves: ['L', 'R'], heavyChest: true });
  out.push(...skull(P, M, { cy: 1.6, r: .27 }));
  // ombreiras extras: as duas, maiores e com três espinhos
  out.push(...spikedPauldron(P, M, 'L', 1, { spikes: 3, k: 1.2 }));
  out.push(...spikedPauldron(P, M, 'R', -1, { spikes: 3, k: 1.2 }));
  return out;
}

export const equipment = M => [{ socket: 'Socket_Hand_R', name: 'RustySword', parts: jaggedSword(M, .7, .095) }];
export const clips = () => enemyClips({ windup: .5, attackDuration: 1.1, chaseSpeed: .95 });
