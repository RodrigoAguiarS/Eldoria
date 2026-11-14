# Auditoria técnica de animações

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

ANIM-TORCH é o único clip adicional ausente especificado: visual da chama,1,2 s loop,30 fps, envelope pequeno. Não é crítico nem altera iluminação. VFX de slash/giro/dash não substituem os clips 3D existentes. Ficha e prompt em VFX/ASSET_GENERATION_PROMPTS pelo mesmo ID.

## Aceite de animação para futuras entregas

No DCC: raiz/sockets estáveis, poses legíveis, nenhum bone perdido, volumes e capa coerentes, nenhuma curva de deslocamento do root. Registrar duração/fps/takes/impact frame e entregar fonte+FBX; turnaround ou vídeo sozinho não passa. Na Unity: confirmar Generic, estados/motions válidos, loops e transitions, amostrar clips e comparar personagens a 128 px sobre piso real. Executar PlayMode com input real e sequências de cancelamento. Capturar vídeo de combo/dash/Hit/Death, melee/elite/ranged, Slam/Volley/Roar/fase 2 com flash 0/1; observar pés, pop, penetração e autoridade do dano.

As provas anteriores de 36 clips não substituem esse aceite visual futuro. Usar testes existentes pertinentes listados no guia de integração, sem afirmar que foram reexecutados aqui.

## Inventário técnico de personagens, rigs e animações

## CHAR-HERO-MODEL — Hero — modelo

**Categoria:** A — Personagem principal. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Personagem de gameplay

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Capa azul em V, couro, aço e espada curta; 2 m, 2.782 triângulos.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Hero/Hero.fbx](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.fbx) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero.glb](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.glb) — Chunk JSON lido: geometria, materiais e animations.
- [Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Floor release com Hero_Model e encontros Generated.

**Dependências:** CHAR-HERO-RIG, CHAR-HERO-MAT

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.


## CHAR-HERO-RIG — Hero — rig Generic

**Categoria:** A — Personagem principal. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Deformação, hierarquia e sockets

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** 24 transforms/ossos nomeados no manifesto; sockets ['Socket_Hand_L', 'Socket_Hand_R']. Não confundir transforms com ossos efetivamente pesados.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta) — animationType: 2 = Generic.
- [Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/Editor/GeneratedModelPostprocessor.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/Editor/GeneratedModelPostprocessor.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Preservar nomes, pesos, bind poses e sockets; Hero inclui ossos de capa.
- In-place, applyRootMotion false; não migrar para Humanoid.


## CHAR-HERO-MAT — Hero — materiais URP

**Categoria:** A — Personagem principal. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** textura. **Escopo:** aprovado_1_0.

**Finalidade:** Cor, iluminação e flash

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Materiais por parte/cor, 9 entradas no manifesto. GLB sem texturas embutidas. UVs projetadas/sobrepostas; estudo não é atlas. Capa azul em V, couro, aço e espada curta; 2 m, 2.782 triângulos.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Hero/Materials](../../../../Assets/_Project/Art/Models/Generated/Hero/Materials) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero.glb](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.glb) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Concepts/PROVENANCE.md](../../../../Assets/_Project/Art/Concepts/PROVENANCE.md) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Preservar URP/Lit e _BaseColor/PropertyBlock.
- Não acrescentar geometria só para atingir orçamento aproximado; não substituir materiais aprovados.


## ANIM-HERO-IDLE — Hero / Idle

**Categoria:** A — Personagem principal. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 2.4 s; loop; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero.controller](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-HERO-MODEL, CHAR-HERO-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-HERO-WALK — Hero / Walk

**Categoria:** A — Personagem principal. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 1 s; loop; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero.controller](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-HERO-MODEL, CHAR-HERO-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-HERO-RUN — Hero / Run

**Categoria:** A — Personagem principal. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 0.64 s; loop; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero.controller](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-HERO-MODEL, CHAR-HERO-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-HERO-ATTACK — Hero / Attack

**Categoria:** A — Personagem principal. **Status:** EXISTENTE. **Prioridade:** P2. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Disponível/importado, legado fora do combo autoritativo; não encomendar novamente.

**Visual:** 0.7 s; one-shot; marcadores referenciais {'hit': 0.32}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero.controller](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-HERO-MODEL, CHAR-HERO-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-HERO-HEAVYATTACK — Hero / HeavyAttack

**Categoria:** A — Personagem principal. **Status:** EXISTENTE. **Prioridade:** P2. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Disponível/importado, legado fora do combo autoritativo; não encomendar novamente.

**Visual:** 1.2 s; one-shot; marcadores referenciais {'hit': 0.7}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero.controller](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-HERO-MODEL, CHAR-HERO-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-HERO-ATTACK-01 — Hero / Attack_01

**Categoria:** A — Personagem principal. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 0.45 s; one-shot; marcadores referenciais {'hit': 0.16}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero.controller](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-HERO-MODEL, CHAR-HERO-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-HERO-ATTACK-02 — Hero / Attack_02

**Categoria:** A — Personagem principal. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 0.5 s; one-shot; marcadores referenciais {'hit': 0.19}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero.controller](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-HERO-MODEL, CHAR-HERO-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-HERO-ATTACK-03 — Hero / Attack_03

**Categoria:** A — Personagem principal. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 0.72 s; one-shot; marcadores referenciais {'hit': 0.32}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero.controller](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-HERO-MODEL, CHAR-HERO-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-HERO-SPECIAL — Hero / Special

**Categoria:** A — Personagem principal. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 0.9 s; one-shot; marcadores referenciais {'hit': 0.36000000000000004}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero.controller](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-HERO-MODEL, CHAR-HERO-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-HERO-DASH — Hero / Dash

**Categoria:** A — Personagem principal. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 0.4 s; one-shot; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero.controller](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-HERO-MODEL, CHAR-HERO-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-HERO-HIT — Hero / Hit

**Categoria:** A — Personagem principal. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 0.45 s; one-shot; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero.controller](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-HERO-MODEL, CHAR-HERO-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-HERO-DEATH — Hero / Death

**Categoria:** A — Personagem principal. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 1.6 s; one-shot; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero.controller](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-HERO-MODEL, CHAR-HERO-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-HERO-VICTORY — Hero / Victory

**Categoria:** A — Personagem principal. **Status:** EXISTENTE. **Prioridade:** P2. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 2 s; one-shot; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero.controller](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedPlayerAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-HERO-MODEL, CHAR-HERO-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## CHAR-MELEE-MODEL — SkeletonMelee — modelo

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Personagem de gameplay

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Esqueleto curvado, osso marfim, espada enferrujada e olhos vermelhos; ~2 m, 1.998 triângulos.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.fbx](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.fbx) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.glb](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.glb) — Chunk JSON lido: geometria, materiais e animations.
- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Floor release com Hero_Model e encontros Generated.

**Dependências:** CHAR-MELEE-RIG, CHAR-MELEE-MAT

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.


## CHAR-MELEE-RIG — SkeletonMelee — rig Generic

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Deformação, hierarquia e sockets

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** 22 transforms/ossos nomeados no manifesto; sockets ['Socket_Hand_L', 'Socket_Hand_R']. Não confundir transforms com ossos efetivamente pesados.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.fbx.meta) — animationType: 2 = Generic.
- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/Editor/GeneratedModelPostprocessor.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/Editor/GeneratedModelPostprocessor.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Preservar nomes, pesos, bind poses e sockets; Hero inclui ossos de capa.
- In-place, applyRootMotion false; não migrar para Humanoid.


## CHAR-MELEE-MAT — SkeletonMelee — materiais URP

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** textura. **Escopo:** aprovado_1_0.

**Finalidade:** Cor, iluminação e flash

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Materiais por parte/cor, 6 entradas no manifesto. GLB sem texturas embutidas. UVs projetadas/sobrepostas; estudo não é atlas. Esqueleto curvado, osso marfim, espada enferrujada e olhos vermelhos; ~2 m, 1.998 triângulos.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonMelee/Materials](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/Materials) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.glb](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.glb) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Concepts/PROVENANCE.md](../../../../Assets/_Project/Art/Concepts/PROVENANCE.md) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Preservar URP/Lit e _BaseColor/PropertyBlock.
- Não acrescentar geometria só para atingir orçamento aproximado; não substituir materiais aprovados.


## ANIM-MELEE-IDLE — SkeletonMelee / Idle

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 2 s; loop; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.controller](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-MELEE-MODEL, CHAR-MELEE-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-MELEE-CHASE — SkeletonMelee / Chase

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 0.7 s; loop; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.controller](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-MELEE-MODEL, CHAR-MELEE-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-MELEE-ATTACK — SkeletonMelee / Attack

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 0.95 s; one-shot; marcadores referenciais {'hit': 0.5}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.controller](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-MELEE-MODEL, CHAR-MELEE-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-MELEE-HIT — SkeletonMelee / Hit

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 0.4 s; one-shot; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.controller](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-MELEE-MODEL, CHAR-MELEE-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-MELEE-DEATH — SkeletonMelee / Death

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 1.4 s; one-shot; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.controller](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-MELEE-MODEL, CHAR-MELEE-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## CHAR-ELITE-MODEL — SkeletonElite — modelo

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Personagem de gameplay

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Melee 1,4× com ombreiras espinhosas e olhos laranja; 2.116 triângulos.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.fbx](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.fbx) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.glb](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.glb) — Chunk JSON lido: geometria, materiais e animations.
- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Floor release com Hero_Model e encontros Generated.

**Dependências:** CHAR-ELITE-RIG, CHAR-ELITE-MAT

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.


## CHAR-ELITE-RIG — SkeletonElite — rig Generic

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Deformação, hierarquia e sockets

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** 22 transforms/ossos nomeados no manifesto; sockets ['Socket_Hand_L', 'Socket_Hand_R']. Não confundir transforms com ossos efetivamente pesados.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.fbx.meta) — animationType: 2 = Generic.
- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/Editor/GeneratedModelPostprocessor.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/Editor/GeneratedModelPostprocessor.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Preservar nomes, pesos, bind poses e sockets; Hero inclui ossos de capa.
- In-place, applyRootMotion false; não migrar para Humanoid.


## CHAR-ELITE-MAT — SkeletonElite — materiais URP

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** textura. **Escopo:** aprovado_1_0.

**Finalidade:** Cor, iluminação e flash

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Materiais por parte/cor, 6 entradas no manifesto. GLB sem texturas embutidas. UVs projetadas/sobrepostas; estudo não é atlas. Melee 1,4× com ombreiras espinhosas e olhos laranja; 2.116 triângulos.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonElite/Materials](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/Materials) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.glb](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.glb) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Concepts/PROVENANCE.md](../../../../Assets/_Project/Art/Concepts/PROVENANCE.md) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Preservar URP/Lit e _BaseColor/PropertyBlock.
- Não acrescentar geometria só para atingir orçamento aproximado; não substituir materiais aprovados.


## ANIM-ELITE-IDLE — SkeletonElite / Idle

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 2 s; loop; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.controller](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-ELITE-MODEL, CHAR-ELITE-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-ELITE-CHASE — SkeletonElite / Chase

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 0.737 s; loop; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.controller](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-ELITE-MODEL, CHAR-ELITE-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-ELITE-ATTACK — SkeletonElite / Attack

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 1.1 s; one-shot; marcadores referenciais {'hit': 0.6}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.controller](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-ELITE-MODEL, CHAR-ELITE-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-ELITE-HIT — SkeletonElite / Hit

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 0.4 s; one-shot; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.controller](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-ELITE-MODEL, CHAR-ELITE-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-ELITE-DEATH — SkeletonElite / Death

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 1.4 s; one-shot; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.controller](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonElite/SkeletonElite_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-ELITE-MODEL, CHAR-ELITE-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## CHAR-RANGED-MODEL — SkeletonRanged — modelo

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Personagem de gameplay

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Esqueleto com capuz verde apagado/cajado e orbe vermelho; ~2 m, 2.032 triângulos. Não introduzir arco.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.fbx](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.fbx) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.glb](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.glb) — Chunk JSON lido: geometria, materiais e animations.
- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Floor release com Hero_Model e encontros Generated.

**Dependências:** CHAR-RANGED-RIG, CHAR-RANGED-MAT

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.


## CHAR-RANGED-RIG — SkeletonRanged — rig Generic

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Deformação, hierarquia e sockets

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** 22 transforms/ossos nomeados no manifesto; sockets ['Socket_Hand_L', 'Socket_Hand_R']. Não confundir transforms com ossos efetivamente pesados.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.fbx.meta) — animationType: 2 = Generic.
- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/Editor/GeneratedModelPostprocessor.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/Editor/GeneratedModelPostprocessor.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Preservar nomes, pesos, bind poses e sockets; Hero inclui ossos de capa.
- In-place, applyRootMotion false; não migrar para Humanoid.


## CHAR-RANGED-MAT — SkeletonRanged — materiais URP

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** textura. **Escopo:** aprovado_1_0.

**Finalidade:** Cor, iluminação e flash

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Materiais por parte/cor, 8 entradas no manifesto. GLB sem texturas embutidas. UVs projetadas/sobrepostas; estudo não é atlas. Esqueleto com capuz verde apagado/cajado e orbe vermelho; ~2 m, 2.032 triângulos. Não introduzir arco.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonRanged/Materials](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/Materials) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.glb](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.glb) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Concepts/PROVENANCE.md](../../../../Assets/_Project/Art/Concepts/PROVENANCE.md) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Preservar URP/Lit e _BaseColor/PropertyBlock.
- Não acrescentar geometria só para atingir orçamento aproximado; não substituir materiais aprovados.


## ANIM-RANGED-IDLE — SkeletonRanged / Idle

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 2.2 s; loop; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.controller](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-RANGED-MODEL, CHAR-RANGED-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-RANGED-CHASE — SkeletonRanged / Chase

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 0.8 s; loop; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.controller](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-RANGED-MODEL, CHAR-RANGED-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-RANGED-ATTACK — SkeletonRanged / Attack

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 1 s; one-shot; marcadores referenciais {'fire': 0.5}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.controller](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-RANGED-MODEL, CHAR-RANGED-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-RANGED-HIT — SkeletonRanged / Hit

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 0.4 s; one-shot; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.controller](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-RANGED-MODEL, CHAR-RANGED-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-RANGED-DEATH — SkeletonRanged / Death

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 1.4 s; one-shot; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.controller](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-RANGED-MODEL, CHAR-RANGED-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## CHAR-WARDEN-MODEL — Warden — modelo

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Personagem de gameplay

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Guardião esquelético, elmo com chifres e marreta; 4,8 m com chifres, 2.790 triângulos; uma malha para ambas as fases.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Warden/Warden.fbx](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.fbx) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden.glb](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.glb) — Chunk JSON lido: geometria, materiais e animations.
- [Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Floor release com Hero_Model e encontros Generated.

**Dependências:** CHAR-WARDEN-RIG, CHAR-WARDEN-MAT

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.


## CHAR-WARDEN-RIG — Warden — rig Generic

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Deformação, hierarquia e sockets

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** 22 transforms/ossos nomeados no manifesto; sockets ['Socket_Hand_L', 'Socket_Hand_R']. Não confundir transforms com ossos efetivamente pesados.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta) — animationType: 2 = Generic.
- [Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/Editor/GeneratedModelPostprocessor.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/Editor/GeneratedModelPostprocessor.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Preservar nomes, pesos, bind poses e sockets; Hero inclui ossos de capa.
- In-place, applyRootMotion false; não migrar para Humanoid.


## CHAR-WARDEN-MAT — Warden — materiais URP

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** textura. **Escopo:** aprovado_1_0.

**Finalidade:** Cor, iluminação e flash

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Materiais por parte/cor, 8 entradas no manifesto. GLB sem texturas embutidas. UVs projetadas/sobrepostas; estudo não é atlas. Guardião esquelético, elmo com chifres e marreta; 4,8 m com chifres, 2.790 triângulos; uma malha para ambas as fases.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Warden/Materials](../../../../Assets/_Project/Art/Models/Generated/Warden/Materials) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden.glb](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.glb) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Concepts/PROVENANCE.md](../../../../Assets/_Project/Art/Concepts/PROVENANCE.md) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Preservar URP/Lit e _BaseColor/PropertyBlock.
- Não acrescentar geometria só para atingir orçamento aproximado; não substituir materiais aprovados.


## ANIM-WARDEN-IDLE — Warden / Idle

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 2.8 s; loop; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden.controller](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-WARDEN-MODEL, CHAR-WARDEN-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-WARDEN-WALK — Warden / Walk

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 1.3 s; loop; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden.controller](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-WARDEN-MODEL, CHAR-WARDEN-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-WARDEN-SLAMWINDUP — Warden / SlamWindup

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 1 s; loop; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden.controller](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-WARDEN-MODEL, CHAR-WARDEN-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-WARDEN-SLAM — Warden / Slam

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 0.9 s; one-shot; marcadores referenciais {'impact': 0.18}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden.controller](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-WARDEN-MODEL, CHAR-WARDEN-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-WARDEN-VOLLEY — Warden / Volley

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 1.5 s; one-shot; marcadores referenciais {'fire': 1.2}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden.controller](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-WARDEN-MODEL, CHAR-WARDEN-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-WARDEN-ROAR — Warden / Roar

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 2.4 s; one-shot; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden.controller](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-WARDEN-MODEL, CHAR-WARDEN-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-WARDEN-HIT — Warden / Hit

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 0.5 s; one-shot; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden.controller](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-WARDEN-MODEL, CHAR-WARDEN-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.


## ANIM-WARDEN-DEATH — Warden / Death

**Categoria:** B — Inimigos e boss. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Locomoção/combate/reação/morte

**Integração:** Estado/blend tree e ponte presentes; cobertura histórica indicada no guia.

**Visual:** 2.2 s; one-shot; marcadores referenciais {}. Dano não depende desses marcadores.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.fbx.meta) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden.controller](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.controller) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** CHAR-WARDEN-MODEL, CHAR-WARDEN-RIG

**Aceite (pendente de execução para mudanças):**

- Preservar take, duração, loop e raiz in-place.
- Contato visual acompanha timer do domínio; cancelamentos não causam dano tardio.
- Inspecionar loop, pés, capa e arma na câmera real; sem novo aceite visual nesta sessão.

## ANIM-GATE — Portão abrir/fechar

**Categoria:** I — Cenários e objetos interativos. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** animação 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Passagem por conclusão da sala

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Movimento procedural MoveTowards; defaults openDepth 2,7 m/speed 8 m/s. Não precisa AnimationClip nem Animator concorrente.

**Evidências:**

- [Assets/_Project/Scripts/Dungeon/DoorController.cs](../../../../Assets/_Project/Scripts/Dungeon/DoorController.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Prefabs/Rooms/Room_Normal.prefab](../../../../Assets/_Project/Prefabs/Rooms/Room_Normal.prefab) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** ENV-GATE

**Aceite (pendente de execução para mudanças):**

- Conferir Open/Close/SetOpenInstant e collider, sem alterar regra de sala.


## ANIM-TORCH — Loop discreto da chama

**Categoria:** J — VFX e animações adicionais. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** animação 3D. **Escopo:** sugestão_polimento.

**Finalidade:** Polimento de chama estática

**Integração:** Produção/integração visual pendente.

**Visual:** Pequena oscilação só do filho visual, suporte/luzes fixos.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/_Scripts/Editor/GeneratedArtApplier.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/Editor/GeneratedArtApplier.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** ENV-TORCH

**Dimensões/tempo:** Torch_FlameLoop; 1,2 s, loop, 30 fps; escala .98–1.02

**Entrega:** AnimationClip .anim + fonte de autoria; variante visual isolada Support/Flame se necessária, sem rig esquelético

**Contrato Unity:** Animação de Transform de objeto, sem skin, Avatar ou rig esquelético. A tocha atual combina ferro/chama em uma malha com duas submalhas: criar só uma variante isolada com filhos Support/Flame se esse polimento for adotado; preservar GA_Torch original. Entregar fonte e AnimationClip .anim por API Editor, 30 fps, raiz fixa; sem dano, luz animada ou colisão.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#anim-torch) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## PROTO-SPRITELAB — Laboratório de sprites Hero

**Categoria:** A — Personagem principal. **Status:** NÃO VERIFICADO. **Prioridade:** P2. **Tipo:** spritesheet. **Escopo:** protótipo_fora_release.

**Finalidade:** Experimento 2D separado

**Integração:** Protótipo fora das duas cenas de release.

**Visual:** Sprites/8 clips 2D/cenas de teste existem, sem revalidação atual. Não substituir herói 3D.

**Evidências:**

- [Assets/_Project/Art/Sprites/Prototypes/Hero/Sheets/Hero_Spritesheet.png](../../../../Assets/_Project/Art/Sprites/Prototypes/Hero/Sheets/Hero_Spritesheet.png) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Tools/LemoSpriteLab/REPORT.md](../../../../Tools/LemoSpriteLab/REPORT.md) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Sprites/Prototypes/Hero/Unity/Hero_Playtest_Floor.unity](../../../../Assets/_Project/Art/Sprites/Prototypes/Hero/Unity/Hero_Playtest_Floor.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
