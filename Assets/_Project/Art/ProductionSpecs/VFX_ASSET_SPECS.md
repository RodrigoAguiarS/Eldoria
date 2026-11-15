# VFX e feedback visual — especificações

> Consolidação de 14/11/2026: fichas repetidas foram substituídas por índices por ID; os dados e prompts completos permanecem no manifesto, sem alteração. Ler somente o registro necessário. Lotes ANIM-TORCH e ITEMS-COMBAT já foram produzidos/integrados; o diagnóstico introdutório antigo abaixo é histórico onde divergir desses lotes. Novas sugestões prontas para escolha estão em [PROMPT_CATALOG](../../../../PROMPT_CATALOG.md).

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

O projétil já é pooled e visualizado como esfera laranja .4 m. O básico usa trail e FX-HIT por contato aceito; o especial usa trail/anel. AttackVisual permanece só diagnóstico, desligado. ANIM-TORCH anima o visual da chama; não encomendar o loop novamente. FX-HIT do giro ainda exige hook próprio. O quadro de eventos abaixo conserva o diagnóstico original; PlayerCombat.TargetHit(Vector3,Vector3) já supre contato do básico, sem alterar Health.Damaged.

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

<a id="anim-torch"></a>

## ANIM-TORCH — Loop discreto da chama

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-TORCH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-damage"></a>

## FX-DAMAGE — Flash de dano/windup

**Estado atual:** EXISTENTE. **Tipo:** VFX. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-DAMAGE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-shake"></a>

## FX-SHAKE — Impulso de câmera

**Estado atual:** EXISTENTE. **Tipo:** VFX. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-SHAKE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-boss-phase"></a>

## FX-BOSS-PHASE — Transição e cor fase 2

**Estado atual:** EXISTENTE. **Tipo:** VFX. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-BOSS-PHASE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-telegraph"></a>

## FX-TELEGRAPH — Aviso de ataque Boss

**Estado atual:** EXISTENTE. **Tipo:** VFX. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-TELEGRAPH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-basic"></a>

## FX-BASIC — AttackVisual básico

**Estado atual:** EXISTENTE. **Tipo:** VFX. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-BASIC]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-projectile"></a>

## FX-PROJECTILE — Projétil inimigo/Boss

**Estado atual:** INCOMPLETO. **Tipo:** VFX. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-PROJECTILE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-slash"></a>

## FX-SLASH — Rastro do combo

**Estado atual:** EXISTENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-SLASH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-special"></a>

## FX-SPECIAL — Rastro do giro

**Estado atual:** EXISTENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-SPECIAL]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-hit"></a>

## FX-HIT — Impacto de acerto

**Estado atual:** EXISTENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-HIT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-heal"></a>

## FX-HEAL — Recuperação de vida

**Estado atual:** AUSENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-HEAL]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-loot"></a>

## FX-LOOT — Coleta aceita

**Estado atual:** AUSENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-LOOT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-level"></a>

## FX-LEVEL — Subida de nível

**Estado atual:** AUSENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-LEVEL]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-slam"></a>

## FX-SLAM — Impacto Slam

**Estado atual:** AUSENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-SLAM]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-dash"></a>

## FX-DASH — Rastro do dash

**Estado atual:** AUSENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-DASH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-projectile-trail"></a>

## FX-PROJECTILE-TRAIL — Cauda do projétil

**Estado atual:** AUSENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-PROJECTILE-TRAIL]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.
