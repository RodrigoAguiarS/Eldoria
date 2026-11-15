# Catálogo de personagens — estado de produção (2026-11-13)

> Registro histórico da execução indicada. Afirmações de ausência de Git/arte/sistemas e pendências descrevem aquela data; consultar [PROJECT_AUDIT](../../PROJECT_AUDIT.md) para estado atual. Medições e limites abaixo foram preservados; não são testes reexecutados por esta revisão documental.

Fontes: `ART_DIRECTION_AND_PROMPTS.md` (visual), `GAME_DESIGN.md`, dados/prefabs existentes (`EnemyData`, `BossData`,
`Player`, `Enemy_*`, `Boss_Warden`). Nenhum personagem fora desses documentos/dados foi criado.

| Personagem | Uso no jogo | Base visual | Altura | Triângulos (total / corpo na Unity) | Ossos | Animações | Estado |
|---|---|---|---|---|---|---|---|
| **Hero** | Player.prefab — mover, atacar, dash, morrer | Asset 01 (documentado) | 2,0 m | 2.782 / 2.614 | 24 (21 com pesos) | Idle, Walk, Run, Attack, HeavyAttack, Dash, Hit, Death, Victory | Gerado · validado (Node + Unity) · PlayMode visto |
| **SkeletonMelee** | Enemy_Melee — Idle/Chase/Attack/Dead, windup 0,4 s | Asset 02 (documentado) | 2,0 m (curvado 1,88) | 1.998 / 1.834 | 22 (18) | Idle, Chase, Attack, Hit, Death | Gerado · validado · PlayMode visto |
| **SkeletonElite** | Enemy_Elite — windup 0,5 s, 1,4× | Asset 02 elite: 1,4×, ombreiras extras, olhos laranja (documentado) | 2,8 m (2,63) | 2.116 / 1.952 | 22 (19) | Idle, Chase, Attack, Hit, Death | Gerado · validado · PlayMode visto |
| **SkeletonRanged** | Enemy_Ranged — projétil, windup 0,5 s | **Suposição**: família esqueleto + capuz (#4C5A44) e cajado com orbe (#FF2A2A) | 2,0 m | 2.032 / 1.868 | 22 (17) | Idle, Chase, Attack (evento `fire` 0,5 s), Hit, Death | Gerado · validado · **visual pendente de aprovação** |
| **Warden** (boss) | Boss_Warden — Slam, Volley, transição de fase | **Suposição**: esqueleto 2,2× com elmo de chifres (chifres do placeholder) e marreta | 4,8 m com chifres | 2.790 / 2.594 | 22 (19) | Idle, Walk, SlamWindup (loop), Slam, Volley, Roar, Hit, Death | Gerado · validado · **visual pendente de aprovação** |

Equipamentos (malhas separadas sob soquetes): Hero → `Hero_Sword` (Socket_Hand_R) · esqueletos → `RustySword` ·
ranged → `CorruptedStaff` · Warden → `Maul`. Todos acompanham as animações (verificado no GLB e na Unity).

Mapeamento estado → clip: Idle→Locomotion(Speed 0) · Chase/Walk→Locomotion(Speed>0) · Attack/Slam/Volley→gatilho homônimo ·
Winding do boss→`SlamWindup` (loop pela duração do telegraph) · Transition→`Roar` · Dead→`Dead=true` (Death).
