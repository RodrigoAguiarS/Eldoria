// Inimigo melee — ART_DIRECTION_AND_PROMPTS.md, Asset 02 (esqueleto corrompido). Enemy_Melee.prefab / EnemyData:
// windup 0,4 s · alcance 1,6 m · velocidade 3,5 m/s. Estados implementados: Idle → Chase → Attack → Dead.
import { SKELETON_P, SKELETON_MATERIALS, HUNCH, skeletonBody, skull, spikedPauldron, jaggedSword, sides } from './_skeleton_kit.mjs';
import * as A from '../anim.mjs';

export const P = SKELETON_P;
export const definition = {
  id: 'skeleton_melee', name: 'SkeletonMelee', role: 'Inimigo melee (Enemy_Melee.prefab)', source: 'ART_DIRECTION_AND_PROMPTS.md — Asset 02',
  documented: true, heightMeters: 2.0, triangleBudget: [3000, 4000],
  materials: SKELETON_MATERIALS, basePose: HUNCH,
  locomotion: { Idle: 0, Chase: 3.5 },
  sockets: { Socket_Hand_R: { rot: [48, 0, 0] } },
};

export function body(M) {
  const out = skeletonBody(P, M, { bracers: ['R'], greaves: ['L'] });
  out.push(...skull(P, M, { cy: 1.6, r: .27 }));
  out.push(...spikedPauldron(P, M, 'L', 1, { spikes: 2 }));
  return out;
}

export const equipment = M => [{ socket: 'Socket_Hand_R', name: 'RustySword', parts: jaggedSword(M) }];

export function enemyClips({ windup, attackDuration, chaseSpeed = 1 }) {
  return [
    A.idle('Idle', { duration: 2.0, breath: 3, sway: 2.5, armOut: 10, sword: 1 }),
    A.locomotion('Chase', { duration: .7 / chaseSpeed, stride: 34, knee: 55, armSwing: 28, elbow: 30, lean: 10, bob: .05, twist: 8, sword: 1 }),
    A.slash('Attack', { duration: attackDuration, windup, hit: windup + .1, heavy: true }),
    A.hit('Hit', .4, 1.2),
    A.death('Death', { duration: 1.4, hipsRest: -.5, back: -.4 }),
  ];
}
export const clips = () => enemyClips({ windup: .4, attackDuration: .95 });
export { sides };
