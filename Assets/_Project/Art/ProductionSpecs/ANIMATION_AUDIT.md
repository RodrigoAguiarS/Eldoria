# Auditoria técnica de animações

> Consolidação de 14/11/2026: fichas repetidas foram substituídas por índices por ID; os dados e prompts completos permanecem no manifesto, sem alteração. Ler somente o registro necessário. Lotes ANIM-TORCH e ITEMS-COMBAT já foram produzidos/integrados; o diagnóstico introdutório antigo abaixo é histórico onde divergir desses lotes. Novas sugestões prontas para escolha estão em [PROMPT_CATALOG](../../../../PROMPT_CATALOG.md).

Data: 14/11/2026. **NÃO VALIDADO NO EDITOR nesta auditoria.** Clips/importers/controllers/bridges e provas históricas foram cruzados. FBX e GLB não são contados como duas animações. Os `.anim` em SpriteLab são 2D e não suprem animações 3D.

## Verificação visual ITEMS-COMBAT (14/11/2026)

Observado em PlayMode na cena da release (capturas em `Reports/Leadership/ProductionVisual/Captures`): Attack_01 diagonal, Attack_02 contragolpe à esquerda e Attack_03 finalizador, cada um com rastro próprio seguindo a espada equipada; Special com giro e anel; colete acompanhando o tronco durante os golpes. Nenhum clip foi alterado (`hero.mjs`, Hero.fbx e controller com SHA256 idênticos). Run, Dash, Hit e Death com a arma/colete equipados foram exercitados por testes e pelo stress diagnóstico da build, mas **não foram inspecionados visualmente quadro a quadro nesta sessão**.

## Resultado e contrato

Há 36 clips 3D: Hero 13, melee 5, elite 5, ranged 5 e Warden 8. Nenhum clip crítico da 1.0 está ausente. Existem cinco rigs Generic e controllers correspondentes. Manter raiz in-place, movimento pelo CharacterController.Move/NavMeshAgent e applyRootMotion false. Rig Humanoid/IK/retargeting não são requisitos novos: conversão antiga conflita com ARCHITECTURE/D-002 e está interrompida.

Cada take deve manter hierarquia, bind poses e socket da mão. O Hero usa capa com ossos extras; não trocar por cloth ou root motion. Os nomes de ossos no manifesto não são o número de bones efetivamente pesados. Malha visual, renderers, rig e controllers são recursos existentes, não autorização para regenerar.

## Clips existentes (durações de origem)

| ID | Take | Duração s | Loop | Marcador referencial s | Uso |
| --- | --- | --- | --- | --- | --- |
| ANIM-HERO-IDLE | Idle | 2.4 | Sim | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-WALK | Walk | 1 | Sim | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-RUN | Run | 0.64 | Sim | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-ATTACK | Attack | 0.7 | Não | {"hit": 0.32} | Disponível/importado, legado fora do combo autoritativo; não encomendar novamente. |
| ANIM-HERO-HEAVYATTACK | HeavyAttack | 1.2 | Não | {"hit": 0.7} | Disponível/importado, legado fora do combo autoritativo; não encomendar novamente. |
| ANIM-HERO-ATTACK-01 | Attack_01 | 0.45 | Não | {"hit": 0.16} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-ATTACK-02 | Attack_02 | 0.5 | Não | {"hit": 0.19} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-ATTACK-03 | Attack_03 | 0.72 | Não | {"hit": 0.32} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-SPECIAL | Special | 0.9 | Não | {"hit": 0.36000000000000004} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-DASH | Dash | 0.4 | Não | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-HIT | Hit | 0.45 | Não | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-DEATH | Death | 1.6 | Não | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-VICTORY | Victory | 2 | Não | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-MELEE-IDLE | Idle | 2 | Sim | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-MELEE-CHASE | Chase | 0.7 | Sim | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-MELEE-ATTACK | Attack | 0.95 | Não | {"hit": 0.5} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-MELEE-HIT | Hit | 0.4 | Não | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-MELEE-DEATH | Death | 1.4 | Não | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-ELITE-IDLE | Idle | 2 | Sim | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-ELITE-CHASE | Chase | 0.737 | Sim | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-ELITE-ATTACK | Attack | 1.1 | Não | {"hit": 0.6} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-ELITE-HIT | Hit | 0.4 | Não | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-ELITE-DEATH | Death | 1.4 | Não | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-RANGED-IDLE | Idle | 2.2 | Sim | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-RANGED-CHASE | Chase | 0.8 | Sim | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-RANGED-ATTACK | Attack | 1 | Não | {"fire": 0.5} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-RANGED-HIT | Hit | 0.4 | Não | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-RANGED-DEATH | Death | 1.4 | Não | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-WARDEN-IDLE | Idle | 2.8 | Sim | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-WARDEN-WALK | Walk | 1.3 | Sim | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-WARDEN-SLAMWINDUP | SlamWindup | 1 | Sim | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-WARDEN-SLAM | Slam | 0.9 | Não | {"impact": 0.18} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-WARDEN-VOLLEY | Volley | 1.5 | Não | {"fire": 1.2} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-WARDEN-ROAR | Roar | 2.4 | Não | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-WARDEN-HIT | Hit | 0.5 | Não | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-WARDEN-DEATH | Death | 2.2 | Não | {} | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |

Durações são do manifesto-fonte, confirmadas por nomes nos chunks GLB/importer. Quantização Unity a 30 fps pode resultar em 0,633 em vez de 0,64 ou 0,433 em vez de 0,45; não tratar arredondamento como defeito. Eventos `hit/fire/impact` do manifesto são marcadores de autoria, não callbacks de dano instalados. Teste PlayerSpecialAssetTests exige events.Length=0 no clip Special.

## Mapeamento runtime e sincronia

| Ator/ação | Fonte autoritativa | Representação existente | Verificação posterior |
| --- | --- | --- | --- |
| Hero locomoção | Deslocamento real no XZ | GeneratedCharacterAnimator estima Speed, Locomotion blend tree Idle/Walk/Run; referências 1,6/5 m/s no manifesto | Andar/parar/girar mirando em outra direção, oito direções; avaliar pés e sliding, sem presumir strafe8-way produzido |
| Combo 1 | PlayerCombo.asset duração 0,45/impacto 0,16 s | Attack_01, normalizado 0,35556; ponte corta em 0,035 s e reposiciona pose no impacto | Golpe diagonal, leitura da antecipação/contato/recuperação; não redesenhar movimento sem abrir clip |
| Combo 2 | duração 0,50/impacto 0,19 s | Attack_02, normalizado 0,38 | Diferenciar visualmente do primeiro, manter buffer 0,15 s e janela de cadeia |
| Combo 3 | duração 0,72/impacto 0,32 s, recuperação 0,28 s | Attack_03, normalizado 0,44444 | Finalizador legível; não chamar HeavyAttack legado por engano |
| Especial | duração 0,90/impacto 0,36 s/recuperação 0,15 s | Special, normalizado 0,4, giro>350° comprovado por teste histórico | Raiz imóvel, arma acompanha giro, uma única aplicação radial; morte/dash/pausa cancelam impacto pendente |
| Dash | PlayerDash default 0,18 s, invulnerabilidade 0,25 s | TakeDash 0,4 s, trigger por início; duração do clip é visual | Verificar retorno ao movimento e prioridade sobre combo; diferença de duração não prova bug |
| Dano Hero | Health.Damaged | Hit só fora de combo/especial, flash independente | Sem interromper pose de impacto; flash 0 preserva leitura do corpo |
| Morte Hero | Health.Died | Dead=true/Death1,6 s | Sem golpe pendente, menu de morte/foco; não exigir ragdoll |
| Vitória Hero | Floor.Completed | Victory 2 s por bridge da cena gerada | Uma vez, sem ocultação involuntária pelo overlay; não exigir nova cinemática |
| Melee/elite | EnemyCombat.IsBusy e EnemyData.attackWindup0,4/0,5 s | Attack do manifesto impacta 0,5/0,6 s; offset 0,1 s | Conferir contato contra dano; não alterar windup para acomodar arte |
| Ranged | Windup 0,5 s e disparo pelo pool | Attack 1 s, fire 0,5 s; offset deve ser lido da variante | Cajado/orbe prepara o disparo; projétil nasce no ponto de gameplay, não presumir socket muzzle já conectado |
| Warden aviso | BossBrain.Winding + dimensão do telegraph | SlamWindup loop ou Volley 1,5 s | Bridge distingue Slam pelo diâmetro>=6 m; mudar escala do disco quebra leitura da ação |
| Warden Slam | Winding→Recovering após resolução | Slam 0,9 s, start 0,12 s, marcador impacto 0,18 s | Risco de pequena diferença visual de cerca 0,06 s mais blend/frame; medir antes de diagnosticar erro, sem retimar dano |
| Warden transição | BossPhaseController aos 50%,3 s invulnerável | Roar 2,4 s + tint/flash/pulso | Verificar intervalo restante sem chamar clip ausente; transição única e cor fase 2 permanecem com flash 0 |
| Warden fase 2 | Slam maior/mais rápido + Volley | Mesma malha/controller, nova cor persistente | Sem encomendar segundo Boss ou nova suite por fase |
| Inimigos mortos | Health.Died/corpseLifetime | Death 1,4 s comuns/2,2 s Boss; remoção 2 s/4 s nos dados | Pose final estável até remoção, collider/agent desligados e sem hits |
| Portões | DoorController | Afundamento procedural, collider controlado por regra | Sem AnimationClip obrigatório, não adicionar dobradiça |
| Pickup | Pickup.Update escalado | Bob .15 m e giro 120°/s | Para durante pausa; não exige grab do personagem |

Parâmetros e estados reais: Speed e Dead em todos; Hero adiciona ComboSpeed e triggers Dash/Hit/Victory/legados. Combo/Special usam CrossFade/Play explícitos na ponte, em vez de depender só de SetTrigger. Não reimplementar o controller a partir de um exemplo genérico da skill. A máscara de upper body e Humanoid IK não são necessárias à implementação atual.

## Movimentos que não devem ser encomendados como lacuna

- Interação manual não existe; E/direito executa giro, pickup é por contato.
- Beber poção via Q/Usar é cura instantânea, inclusive com bolsa pausada. Uma animação corporal obrigatória/tempo de beber mudaria o produto; só VFX opcional, sem lock de input.
- Equipar/desequipar é comando da bolsa pausada. Sem animação de saque, vestir ou paper doll exigida.
- Não há arco, machado, escudo, pulo, ataque elemental, baú ou armadilha aprovados. Não produzir clips desses sistemas.
- Attack/HeavyAttack anteriores são acervo reutilizável; mantê-los não significa que o combo usa apenas um golpe.

## Lote adicional permitido como proposta

ANIM-TORCH já foi produzido e integrado (loop1,2 s/30 fps). Não encomendar novamente. Aprovação artística é separada da existência/importação; VFX de slash/giro/dash não substituem clips corporais 3D. Ficha e evidências pelo mesmo ID no manifesto.

## Aceite de animação para futuras entregas

No DCC: raiz/sockets estáveis, poses legíveis, nenhum bone perdido, volumes e capa coerentes, nenhuma curva de deslocamento do root. Registrar duração/fps/takes/impact frame e entregar fonte+FBX; turnaround ou vídeo sozinho não passa. Na Unity: confirmar Generic, estados/motions válidos, loops e transitions, amostrar clips e comparar personagens a 128 px sobre piso real. Executar PlayMode com input real e sequências de cancelamento. Capturar vídeo de combo/dash/Hit/Death, melee/elite/ranged, Slam/Volley/Roar/fase 2 com flash 0/1; observar pés, pop, penetração e autoridade do dano.

As provas anteriores de 36 clips não substituem esse aceite visual futuro. Usar testes existentes pertinentes listados no guia de integração, sem afirmar que foram reexecutados aqui.

## Inventário técnico de personagens, rigs e animações

<a id="char-hero-model"></a>

## CHAR-HERO-MODEL — Hero — modelo

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=CHAR-HERO-MODEL]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="char-hero-rig"></a>

## CHAR-HERO-RIG — Hero — rig Generic

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=CHAR-HERO-RIG]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="char-hero-mat"></a>

## CHAR-HERO-MAT — Hero — materiais URP

**Estado atual:** EXISTENTE. **Tipo:** textura. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=CHAR-HERO-MAT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-hero-idle"></a>

## ANIM-HERO-IDLE — Hero / Idle

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-HERO-IDLE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-hero-walk"></a>

## ANIM-HERO-WALK — Hero / Walk

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-HERO-WALK]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-hero-run"></a>

## ANIM-HERO-RUN — Hero / Run

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-HERO-RUN]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-hero-attack"></a>

## ANIM-HERO-ATTACK — Hero / Attack

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-HERO-ATTACK]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-hero-heavyattack"></a>

## ANIM-HERO-HEAVYATTACK — Hero / HeavyAttack

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-HERO-HEAVYATTACK]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-hero-attack-01"></a>

## ANIM-HERO-ATTACK-01 — Hero / Attack_01

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-HERO-ATTACK-01]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-hero-attack-02"></a>

## ANIM-HERO-ATTACK-02 — Hero / Attack_02

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-HERO-ATTACK-02]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-hero-attack-03"></a>

## ANIM-HERO-ATTACK-03 — Hero / Attack_03

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-HERO-ATTACK-03]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-hero-special"></a>

## ANIM-HERO-SPECIAL — Hero / Special

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-HERO-SPECIAL]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-hero-dash"></a>

## ANIM-HERO-DASH — Hero / Dash

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-HERO-DASH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-hero-hit"></a>

## ANIM-HERO-HIT — Hero / Hit

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-HERO-HIT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-hero-death"></a>

## ANIM-HERO-DEATH — Hero / Death

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-HERO-DEATH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-hero-victory"></a>

## ANIM-HERO-VICTORY — Hero / Victory

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-HERO-VICTORY]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="char-melee-model"></a>

## CHAR-MELEE-MODEL — SkeletonMelee — modelo

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=CHAR-MELEE-MODEL]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="char-melee-rig"></a>

## CHAR-MELEE-RIG — SkeletonMelee — rig Generic

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=CHAR-MELEE-RIG]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="char-melee-mat"></a>

## CHAR-MELEE-MAT — SkeletonMelee — materiais URP

**Estado atual:** EXISTENTE. **Tipo:** textura. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=CHAR-MELEE-MAT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-melee-idle"></a>

## ANIM-MELEE-IDLE — SkeletonMelee / Idle

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-MELEE-IDLE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-melee-chase"></a>

## ANIM-MELEE-CHASE — SkeletonMelee / Chase

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-MELEE-CHASE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-melee-attack"></a>

## ANIM-MELEE-ATTACK — SkeletonMelee / Attack

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-MELEE-ATTACK]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-melee-hit"></a>

## ANIM-MELEE-HIT — SkeletonMelee / Hit

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-MELEE-HIT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-melee-death"></a>

## ANIM-MELEE-DEATH — SkeletonMelee / Death

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-MELEE-DEATH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="char-elite-model"></a>

## CHAR-ELITE-MODEL — SkeletonElite — modelo

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=CHAR-ELITE-MODEL]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="char-elite-rig"></a>

## CHAR-ELITE-RIG — SkeletonElite — rig Generic

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=CHAR-ELITE-RIG]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="char-elite-mat"></a>

## CHAR-ELITE-MAT — SkeletonElite — materiais URP

**Estado atual:** EXISTENTE. **Tipo:** textura. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=CHAR-ELITE-MAT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-elite-idle"></a>

## ANIM-ELITE-IDLE — SkeletonElite / Idle

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-ELITE-IDLE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-elite-chase"></a>

## ANIM-ELITE-CHASE — SkeletonElite / Chase

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-ELITE-CHASE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-elite-attack"></a>

## ANIM-ELITE-ATTACK — SkeletonElite / Attack

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-ELITE-ATTACK]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-elite-hit"></a>

## ANIM-ELITE-HIT — SkeletonElite / Hit

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-ELITE-HIT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-elite-death"></a>

## ANIM-ELITE-DEATH — SkeletonElite / Death

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-ELITE-DEATH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="char-ranged-model"></a>

## CHAR-RANGED-MODEL — SkeletonRanged — modelo

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=CHAR-RANGED-MODEL]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="char-ranged-rig"></a>

## CHAR-RANGED-RIG — SkeletonRanged — rig Generic

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=CHAR-RANGED-RIG]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="char-ranged-mat"></a>

## CHAR-RANGED-MAT — SkeletonRanged — materiais URP

**Estado atual:** EXISTENTE. **Tipo:** textura. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=CHAR-RANGED-MAT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-ranged-idle"></a>

## ANIM-RANGED-IDLE — SkeletonRanged / Idle

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-RANGED-IDLE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-ranged-chase"></a>

## ANIM-RANGED-CHASE — SkeletonRanged / Chase

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-RANGED-CHASE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-ranged-attack"></a>

## ANIM-RANGED-ATTACK — SkeletonRanged / Attack

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-RANGED-ATTACK]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-ranged-hit"></a>

## ANIM-RANGED-HIT — SkeletonRanged / Hit

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-RANGED-HIT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-ranged-death"></a>

## ANIM-RANGED-DEATH — SkeletonRanged / Death

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-RANGED-DEATH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="char-warden-model"></a>

## CHAR-WARDEN-MODEL — Warden — modelo

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=CHAR-WARDEN-MODEL]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="char-warden-rig"></a>

## CHAR-WARDEN-RIG — Warden — rig Generic

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=CHAR-WARDEN-RIG]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="char-warden-mat"></a>

## CHAR-WARDEN-MAT — Warden — materiais URP

**Estado atual:** EXISTENTE. **Tipo:** textura. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=CHAR-WARDEN-MAT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-warden-idle"></a>

## ANIM-WARDEN-IDLE — Warden / Idle

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-WARDEN-IDLE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-warden-walk"></a>

## ANIM-WARDEN-WALK — Warden / Walk

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-WARDEN-WALK]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-warden-slamwindup"></a>

## ANIM-WARDEN-SLAMWINDUP — Warden / SlamWindup

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-WARDEN-SLAMWINDUP]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-warden-slam"></a>

## ANIM-WARDEN-SLAM — Warden / Slam

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-WARDEN-SLAM]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-warden-volley"></a>

## ANIM-WARDEN-VOLLEY — Warden / Volley

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-WARDEN-VOLLEY]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-warden-roar"></a>

## ANIM-WARDEN-ROAR — Warden / Roar

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-WARDEN-ROAR]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-warden-hit"></a>

## ANIM-WARDEN-HIT — Warden / Hit

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-WARDEN-HIT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-warden-death"></a>

## ANIM-WARDEN-DEATH — Warden / Death

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-WARDEN-DEATH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-gate"></a>

## ANIM-GATE — Portão abrir/fechar

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-GATE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="anim-torch"></a>

## ANIM-TORCH — Loop discreto da chama

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-TORCH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="proto-spritelab"></a>

## PROTO-SPRITELAB — Laboratório de sprites Hero

**Estado atual:** NÃO VERIFICADO. **Tipo:** spritesheet. **Escopo:** protótipo_fora_release.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=PROTO-SPRITELAB]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.
