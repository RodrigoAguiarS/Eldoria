# VFX e feedback visual — especificações

Data: 14/11/2026. **NÃO VALIDADO NO EDITOR nesta auditoria.** A release já tem flash, shake, windup, telegraph e mudança de fase; estes são recursos existentes. Os efeitos adicionais são P2 opcionais de apresentação, não correções de regras nem novos sistemas.

## Lote ITEMS-COMBAT — golpes, giro e acerto (14/11/2026)

**Origem do marcador branco confirmada:** `AttackVisual`, cubo 2,2×0,1×1,6 m com `Mat_Attack` criado em `DungeonMvpSceneBuilder.BuildPlayerPrefab` e ativado por `PlayerCombat` no impacto. O `InstallModel` do Hero oculta só Body/Horns/FacingMarker, então o cubo aparecia sob o herói gerado. Ele não é a hitbox (dano = `OverlapSphereNonAlloc` + cone).

- **FX-BASIC:** fica apenas como diagnóstico (`PlayerCombat.showDiagnosticAttackVisual`, desligado por padrão, inclusive em prefabs/cenas reconstruídos). Teste PlayMode confirma que o cubo não aparece durante o golpe.
- **FX-SLASH:** `TrailRenderer` filho do `Socket_Hand_R` (acompanha a lâmina real), material URP Particles/Unlit aditivo sem ZWrite, emissão em torno do impacto de cada passo (0,16/0,19/0,32 s); finalizador mais largo. Capturas `attack_01..03` mostram três arcos distintos.
- **FX-SPECIAL:** rastro no giro + anel fino ciano de centro vazio no impacto (0,36 s), 0,28 s, raio visual ≤ alcance real (3 m).
- **FX-HIT:** `ParticleSystem` world-space com `Emit` (6 lascas + 2 faíscas, sem instanciação), disparado por `PlayerCombat.TargetHit` somente quando `TakeDamage` retorna true. Golpe no ar: zero bursts (teste). O giro especial não emite FX-HIT porque `PlayerSpecial.cs` ficou fora do escopo de arquivos; o flash existente dos inimigos continua.
- Cancelamento/morte/pausa limpam o rastro pendente (testes); VFX usam `System.Random` próprio para não consumir o `UnityEngine.Random` do loot. Dano, cone, cooldown, buffer e deduplicação inalterados (teste: 1 evento de dano por golpe, valor 20).

**Conferência na build Windows (14/11/2026, controle autorizado pelo usuário):** partida normal até a sala 2 em 1920×1080 tela cheia e em 1280×720 janela (opções restauradas para 1920×1080/tela cheia/Alta ao final). Observados: drops reais de ouro (pilha de moedas), XP (orbe com anel) e poção; Largar 1 recria o frasco no chão; pilha ×2 da mesma potência com ícone, "Cura 15" e detalhes com ícone; HUD de poções ×N/Pronto/Sem poções/Indisponível (morto); Q consome uma unidade; layouts de 720p e 1080p sem cortes. No modo diagnóstico `-dcc-stress` da build: rastro na espada, faíscas/lascas em acertos e anel do giro, sem o retângulo branco. Logs do player sem exceções/Missing Script. Capturas em `Reports/Leadership/ProductionVisual/Captures/Build`. **Não observado na build:** espadas e colete equipados (nenhuma arma/armadura dropou até a sala 2; o visual equipado foi verificado no PlayMode da mesma cena).

## Base existente e legibilidade

GeneratedHitFlash tinge cada submesh por MaterialPropertyBlock e restaura base; fase 2 mantém tint próprio. DamageFlash antigo continua como componente, mas nos modelos gerados a ponte é a referência visível. PlayerHitShake usa impulso Cinemachine compatível com pacote6.6.0 instalado; não manipular Transform da MainCamera em conflito com follow. Opções zero precisam realmente remover flash/shake; perigo/estado permanente do Boss seguem legíveis.

BossAttackTelegraph mantém dois objetos fixos, borda externa da área e preenchimento crescente. Não substituir o aviso por partículas sem geometria precisa. Dados atuais: Slam 3,5 m/1,4 s fase 1; fase 2 raio 4,55 m/1 s; Volley raio 2,5 m/1,2 s. Nunca reduzir aviso abaixo1 s. Diferenciar efeito de impacto (cosmético, depois do timer) de aviso (antes do dano); área desenhada deve seguir dados reais, sem inferir alcance pela arte.

O projétil já é pooled e visualizado como esfera laranja .4 m. Ataque básico ativa AttackVisual no impacto até fim/cancelamento, sem trail da espada. Especial já tem animação/timing e feedback sonoro, mas não foi localizado componente visual específico de partículas/trail. Tochas usam malha emissiva estática, não ParticleSystem. Ausência de ParticleSystem não significa ausência de feedback funcional.

## Contratos de eventos e lacunas de integração

| Evento/sistema | Informação disponível | Consequência para produção |
| --- | --- | --- |
| PlayerCombat.AttackStarted/Impacted/Finished/Cancelled | Índice e PlayerComboStep | Trail/arco podem representar golpe; Impacted ocorre na tentativa, não confirma acerto de alvo |
| PlayerSpecial mesmos eventos | Step, impacto 0,36 s | VFX giro acompanha um impacto radial, não múltiplos hits pelo trail |
| Health.Damaged(float) | Dano aceito, sem posição/normal do contato | FX-HIT localizado exige ponte futura; não prometer spawn exato só com PNG/prefab |
| Health.Changed | HPcurrent/max, origem de cura ausente | FX-HEAL não pode presumir PotionUsed; definir ponte de consumo aceito separada, sem alterar cura |
| PlayerProgression.Collected(LootKind) | Categoria após coleta aceita | FX-LOOT simples possível; toast com nome/quantidade/posição precisa dados extras |
| PlayerProgression.LeveledUp(int) | Novo nível | Efeito de nível não aplica cura nem bônus |
| PlayerInventory.ItemDropped(Pickup) | Pickup criado | Feedback de largar usa posição, sem inventar animação de coleta |
| BossBrain Winding/Recovering | Estado/telegraph, impacto sem evento público dedicado | FX-SLAM necessita adapter/tarefa futura; visual jamais chama ResolveSlam |
| BossPhaseController eventos | TransitionStarted/Phase2Started | Preservar3 s invulnerável e tint permanente, sem segunda malha |
| PlayerDash | IsDashing/CooldownFraction | Já há ponte por borda; novo subscriber é tarefa futura, não evento que a skill cria |
| Projectile/ProjectilePool | Pooled lifetime e retorno | Cauda deve limpar ao reciclar; manter colisão/hitMask/lifetime existentes |

## Política visual e orçamentos propostos

Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

Feedback regular discreto, poucos canais e duração curta; Boss mais amplo só no alcance real. Sem hit-stop novo: pausa/invulnerabilidade/combate atuais não podem ser alterados por decoração. Sem gore ou relâmpago/explosão/superfície de dano novos. Cores hostis não podem ser confundidas com XP/cura do jogador.

Os limites de partículas/duração nas fichas são **metas propostas para produção**, não medições nem critérios retroativos da 1.0. Preferir mesh/trail simples e texturas128/256 quando úteis. Não adicionar VFX Graph, ShaderGraph/custom shader ou materiais translucentes caros sem necessidade demonstrada. O contrato URP pode ser atendido pelos materiais existentes quando apropriados; sem supor nome de shader de pacote diferente.

Efeitos repetidos devem ser reutilizados: aquecer pool, reiniciar sementes/timers, limpar partículas/trails, remover inscrições ao desativar. `OnEnable` não deve reaproveitar segmentos do tiro anterior. Pausa congela efeitos de gameplay; UI/transições usam tempo não escalado. Em cancelamento/morte, não deixar trailing arc fantasma; efeitos independentes já emitidos podem completar apenas se não sugerirem novo dano. Não usar Instantiate/Destroy de projéteis por tiro.

## QA de efeitos

Validar dois disparos30ms separados, pool aquecido sem criação extra, retorno/reativação sem cauda antiga, morte/dash durante windup, abrir bolsa no frame de impacto, qualidadeLeve/Padrão e flash/shake 0/1. Conferir piso/telegraph/ator sempre visíveis, máximo três flashes amplos por segundo como diretriz conservadora de acessibilidade, sem certificar limite medido nesta auditoria. Contadores de partículas/materiais/drawcalls/CPU/GPU/memória devem ser comparados na mesma carga e hardware; nenhuma promessa de ganho deFPS por arte ou velocidade de animação.

Som funcional já existe (9 WAV e mixer); reutilizar roteamento/pool, sem encomendar áudio nesta missão visual. Não afirmar que um novo VFX foi integrado só por aparecer noProject.

## Fichas detalhadas de reuso e produção

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


## FX-DAMAGE — Flash de dano/windup

**Categoria:** J — VFX e animações adicionais. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** VFX. **Escopo:** aprovado_1_0.

**Finalidade:** Feedback implementado

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Vermelho no dano, laranja no windup; PropertyBlock e intensidade configurável.

**Evidências:**

- [Assets/_Project/Scripts/Core/DamageFlash.cs](../../../../Assets/_Project/Scripts/Core/DamageFlash.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedHitFlash.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedHitFlash.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedEnemyAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Arte apenas observa domínio; preservar dano/colliders/timers.
- Pool real de projéteis e discos fixos mantidos.
- Flash/shake 0 não ocultam área do perigo nem cor persistente da fase.


## FX-SHAKE — Impulso de câmera

**Categoria:** J — VFX e animações adicionais. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** VFX. **Escopo:** aprovado_1_0.

**Finalidade:** Feedback implementado

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Impulso Cinemachine com intensidade configurável; versão real 6.6.0, sem copiar API 3.1 da skill.

**Evidências:**

- [Assets/_Project/Scripts/Player/PlayerHitShake.cs](../../../../Assets/_Project/Scripts/Player/PlayerHitShake.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Packages/manifest.json](../../../../Packages/manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Arte apenas observa domínio; preservar dano/colliders/timers.
- Pool real de projéteis e discos fixos mantidos.
- Flash/shake 0 não ocultam área do perigo nem cor persistente da fase.


## FX-BOSS-PHASE — Transição e cor fase 2

**Categoria:** J — VFX e animações adicionais. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** VFX. **Escopo:** aprovado_1_0.

**Finalidade:** Feedback implementado

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Transição única, branco e vermelho persistente no modelo visível; fase 2 continua legível com flash 0, não exige segunda malha.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs](../../../../Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/GeneratedBossAnimationBridge.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Boss/BossPhaseController.cs](../../../../Assets/_Project/Scripts/Boss/BossPhaseController.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/boss-visual-review.json](../../../../Reports/Leadership/boss-visual-review.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Arte apenas observa domínio; preservar dano/colliders/timers.
- Pool real de projéteis e discos fixos mantidos.
- Flash/shake 0 não ocultam área do perigo nem cor persistente da fase.


## FX-TELEGRAPH — Aviso de ataque Boss

**Categoria:** J — VFX e animações adicionais. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** VFX. **Escopo:** aprovado_1_0.

**Finalidade:** Feedback implementado

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Dois discos fixos reutilizados; externo área real, interno cresce; Slam raio 3,5/4,55 m fase 2, Volley 2,5 m, aviso >=1 s.

**Evidências:**

- [Assets/_Project/Scripts/Boss/BossAttackTelegraph.cs](../../../../Assets/_Project/Scripts/Boss/BossAttackTelegraph.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Materials/Mat_TelegraphOuter.mat](../../../../Assets/_Project/Materials/Mat_TelegraphOuter.mat) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Materials/Mat_TelegraphFill.mat](../../../../Assets/_Project/Materials/Mat_TelegraphFill.mat) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Arte apenas observa domínio; preservar dano/colliders/timers.
- Pool real de projéteis e discos fixos mantidos.
- Flash/shake 0 não ocultam área do perigo nem cor persistente da fase.


## FX-BASIC — AttackVisual básico

**Categoria:** J — VFX e animações adicionais. **Status:** INCOMPLETO. **Prioridade:** P2. **Tipo:** VFX. **Escopo:** aprovado_1_0.

**Finalidade:** Feedback implementado

**Integração:** Produção/integração visual pendente.

**Visual:** Marcador simples temporário no impacto; não é trail na trajetória da espada.

**Evidências:**

- [Assets/_Project/Scripts/Player/PlayerCombat.cs](../../../../Assets/_Project/Scripts/Player/PlayerCombat.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Prefabs/Player/Player.prefab](../../../../Assets/_Project/Prefabs/Player/Player.prefab) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Arte apenas observa domínio; preservar dano/colliders/timers.
- Pool real de projéteis e discos fixos mantidos.
- Flash/shake 0 não ocultam área do perigo nem cor persistente da fase.


## FX-PROJECTILE — Projétil inimigo/Boss

**Categoria:** J — VFX e animações adicionais. **Status:** INCOMPLETO. **Prioridade:** P2. **Tipo:** VFX. **Escopo:** aprovado_1_0.

**Finalidade:** Feedback implementado

**Integração:** Produção/integração visual pendente.

**Visual:** Esfera laranja .4 m integrada ao pool; sem cauda própria. Não precisa arco/flecha.

**Evidências:**

- [Assets/_Project/Prefabs/Projectiles/Projectile.prefab](../../../../Assets/_Project/Prefabs/Projectiles/Projectile.prefab) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/ProjectilePool.cs](../../../../Assets/_Project/Scripts/Systems/ProjectilePool.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/Projectile.cs](../../../../Assets/_Project/Scripts/Systems/Projectile.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Arte apenas observa domínio; preservar dano/colliders/timers.
- Pool real de projéteis e discos fixos mantidos.
- Flash/shake 0 não ocultam área do perigo nem cor persistente da fase.


## FX-SLASH — Rastro do combo

**Categoria:** J — VFX e animações adicionais. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** VFX. **Escopo:** sugestão_polimento.

**Finalidade:** Polimento opcional de ação existente

**Integração:** Produção/integração visual pendente.

**Visual:** Three concise steel/cyan sword arcs: right diagonal, left diagonal and compact downward finisher. No broad opaque glow.

**Evidências:**

- [Assets/_Project/Scripts/Player/PlayerCombat.cs](../../../../Assets/_Project/Scripts/Player/PlayerCombat.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Player/PlayerSpecial.cs](../../../../Assets/_Project/Scripts/Player/PlayerSpecial.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/PlayerProgression.cs](../../../../Assets/_Project/Scripts/Systems/PlayerProgression.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/Projectile.cs](../../../../Assets/_Project/Scripts/Systems/Projectile.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** ANIM-HERO-ATTACK-01, ANIM-HERO-ATTACK-02, ANIM-HERO-ATTACK-03

**Integração técnica pendente:**

- PlayerCombat.AttackStarted/AttackImpacted/AttackCancelled; sem hit-confirm/posição por alvo.

**Dimensões/tempo:** 0.12–0.18 s; <=12 particles per impact; radius never exceeds actual combo range

**Entrega:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Contrato Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#fx-slash) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## FX-SPECIAL — Rastro do giro

**Categoria:** J — VFX e animações adicionais. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** VFX. **Escopo:** sugestão_polimento.

**Finalidade:** Polimento opcional de ação existente

**Integração:** Produção/integração visual pendente.

**Visual:** One thin cyan circular sword sweep, empty centre, restrained pale edge, clearly distinct from a red hostile ground telegraph.

**Evidências:**

- [Assets/_Project/Scripts/Player/PlayerCombat.cs](../../../../Assets/_Project/Scripts/Player/PlayerCombat.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Player/PlayerSpecial.cs](../../../../Assets/_Project/Scripts/Player/PlayerSpecial.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/PlayerProgression.cs](../../../../Assets/_Project/Scripts/Systems/PlayerProgression.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/Projectile.cs](../../../../Assets/_Project/Scripts/Systems/Projectile.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** ANIM-HERO-SPECIAL

**Integração técnica pendente:**

- PlayerSpecial.AttackImpacted .36 s; não há VFX específico inscrito.

**Dimensões/tempo:** 0.18–0.30 s; radius <=3 m; <=24 particles

**Entrega:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Contrato Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#fx-special) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## FX-HIT — Impacto de acerto

**Categoria:** J — VFX e animações adicionais. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** VFX. **Escopo:** sugestão_polimento.

**Finalidade:** Polimento opcional de ação existente

**Integração:** Produção/integração visual pendente.

**Visual:** Small directional ivory chips and two orange sparks for bone/metal contact; no blood, large flash or persistent debris.

**Evidências:**

- [Assets/_Project/Scripts/Player/PlayerCombat.cs](../../../../Assets/_Project/Scripts/Player/PlayerCombat.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Player/PlayerSpecial.cs](../../../../Assets/_Project/Scripts/Player/PlayerSpecial.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/PlayerProgression.cs](../../../../Assets/_Project/Scripts/Systems/PlayerProgression.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/Projectile.cs](../../../../Assets/_Project/Scripts/Systems/Projectile.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** FX-DAMAGE

**Integração técnica pendente:**

- Health.Damaged só fornece quantidade; posição/direção precisariam ponte futura.

**Dimensões/tempo:** 0.15–0.25 s; radius <=0.3 m; <=10 particles

**Entrega:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Contrato Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#fx-hit) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## FX-HEAL — Recuperação de vida

**Categoria:** J — VFX e animações adicionais. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** VFX. **Escopo:** sugestão_polimento.

**Finalidade:** Polimento opcional de ação existente

**Integração:** Produção/integração visual pendente.

**Visual:** Brief soft ruby-to-warm-white upward motes close to the hero and a low-opacity foot ring; no poison-like effect.

**Evidências:**

- [Assets/_Project/Scripts/Player/PlayerCombat.cs](../../../../Assets/_Project/Scripts/Player/PlayerCombat.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Player/PlayerSpecial.cs](../../../../Assets/_Project/Scripts/Player/PlayerSpecial.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/PlayerProgression.cs](../../../../Assets/_Project/Scripts/Systems/PlayerProgression.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/Projectile.cs](../../../../Assets/_Project/Scripts/Systems/Projectile.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** ICON-ITEM-HEALTH

**Integração técnica pendente:**

- Health.Changed genérico; não há PotionUsed. Distinguir consumo aceito de cura de nível.

**Dimensões/tempo:** 0.35–0.60 s; radius <=0.6 m; <=16 particles

**Entrega:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Contrato Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#fx-heal) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## FX-LOOT — Coleta aceita

**Categoria:** J — VFX e animações adicionais. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** VFX. **Escopo:** sugestão_polimento.

**Finalidade:** Polimento opcional de ação existente

**Integração:** Produção/integração visual pendente.

**Visual:** Small cyan glint for XP, brass for gold, neutral spark for stored gear/potions; restrained size and brightness.

**Evidências:**

- [Assets/_Project/Scripts/Player/PlayerCombat.cs](../../../../Assets/_Project/Scripts/Player/PlayerCombat.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Player/PlayerSpecial.cs](../../../../Assets/_Project/Scripts/Player/PlayerSpecial.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/PlayerProgression.cs](../../../../Assets/_Project/Scripts/Systems/PlayerProgression.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/Projectile.cs](../../../../Assets/_Project/Scripts/Systems/Projectile.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** LOOT-SHARED

**Integração técnica pendente:**

- PlayerProgression.Collected(LootKind) existe, sem quantidade/posição/item completo.

**Dimensões/tempo:** 0.20–0.35 s; radius <=0.35 m; <=8 particles

**Entrega:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Contrato Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#fx-loot) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## FX-LEVEL — Subida de nível

**Categoria:** J — VFX e animações adicionais. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** VFX. **Escopo:** sugestão_polimento.

**Finalidade:** Polimento opcional de ação existente

**Integração:** Produção/integração visual pendente.

**Visual:** One thin cyan ring rising boots-to-waist, sparse brass motes; combat floor remains visible.

**Evidências:**

- [Assets/_Project/Scripts/Player/PlayerCombat.cs](../../../../Assets/_Project/Scripts/Player/PlayerCombat.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Player/PlayerSpecial.cs](../../../../Assets/_Project/Scripts/Player/PlayerSpecial.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/PlayerProgression.cs](../../../../Assets/_Project/Scripts/Systems/PlayerProgression.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/Projectile.cs](../../../../Assets/_Project/Scripts/Systems/Projectile.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** HUD-PROGRESSION

**Integração técnica pendente:**

- PlayerProgression.LeveledUp(int), sem mudar cura ou fórmula.

**Dimensões/tempo:** 0.60–0.80 s; radius <=0.8 m; <=20 particles

**Entrega:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Contrato Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#fx-level) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## FX-SLAM — Impacto Slam

**Categoria:** J — VFX e animações adicionais. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** VFX. **Escopo:** sugestão_polimento.

**Finalidade:** Polimento opcional de ação existente

**Integração:** Produção/integração visual pendente.

**Visual:** Low thin shock ring and a few angular stone chips with muted orange edge; no persistent damaging patch.

**Evidências:**

- [Assets/_Project/Scripts/Player/PlayerCombat.cs](../../../../Assets/_Project/Scripts/Player/PlayerCombat.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Player/PlayerSpecial.cs](../../../../Assets/_Project/Scripts/Player/PlayerSpecial.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/PlayerProgression.cs](../../../../Assets/_Project/Scripts/Systems/PlayerProgression.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/Projectile.cs](../../../../Assets/_Project/Scripts/Systems/Projectile.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** FX-TELEGRAPH

**Integração técnica pendente:**

- BossBrain aplica Slam, sem evento público de impacto dedicado.

**Dimensões/tempo:** 0.25–0.45 s; radius 3.5 or 4.55 m from actual phase data; <=24 particles

**Entrega:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Contrato Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#fx-slam) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## FX-DASH — Rastro do dash

**Categoria:** J — VFX e animações adicionais. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** VFX. **Escopo:** sugestão_polimento.

**Finalidade:** Polimento opcional de ação existente

**Integração:** Produção/integração visual pendente.

**Visual:** Two or three faint blue cape sweeps or broad dust puffs; never opaque full-character duplicates.

**Evidências:**

- [Assets/_Project/Scripts/Player/PlayerCombat.cs](../../../../Assets/_Project/Scripts/Player/PlayerCombat.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Player/PlayerSpecial.cs](../../../../Assets/_Project/Scripts/Player/PlayerSpecial.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/PlayerProgression.cs](../../../../Assets/_Project/Scripts/Systems/PlayerProgression.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/Projectile.cs](../../../../Assets/_Project/Scripts/Systems/Projectile.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** ANIM-HERO-DASH

**Integração técnica pendente:**

- PlayerDash expõe IsDashing, sem evento; ponte atual detecta início por borda.

**Dimensões/tempo:** tail <=0.20 s; <=8 particles; clear on cancellation

**Entrega:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Contrato Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#fx-dash) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## FX-PROJECTILE-TRAIL — Cauda do projétil

**Categoria:** J — VFX e animações adicionais. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** VFX. **Escopo:** sugestão_polimento.

**Finalidade:** Polimento opcional de ação existente

**Integração:** Produção/integração visual pendente.

**Visual:** Compact tapered orange tail on existing hostile orb, clear against stone, no explosion or larger danger radius.

**Evidências:**

- [Assets/_Project/Scripts/Player/PlayerCombat.cs](../../../../Assets/_Project/Scripts/Player/PlayerCombat.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Player/PlayerSpecial.cs](../../../../Assets/_Project/Scripts/Player/PlayerSpecial.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/PlayerProgression.cs](../../../../Assets/_Project/Scripts/Systems/PlayerProgression.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/Projectile.cs](../../../../Assets/_Project/Scripts/Systems/Projectile.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** FX-PROJECTILE

**Integração técnica pendente:**

- Reset OnEnable/retorno do ProjectilePool; não criar nova instância a cada tiro.

**Dimensões/tempo:** tail <=0.15 s and 0.6 m; <=6 particles per projectile

**Entrega:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Contrato Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#fx-projectile-trail) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.
