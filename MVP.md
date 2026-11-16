# MVP — escopo, aceite e estado

## Estado e evidências

A release **Windows 1.0.0 de 14/11/2026** entregou o andar fixo de quatro salas + Guardião, combo/dash/especial, estamina/defesa, progressão/loot, inventário/raridades, menus/opções e áudio. Aceite humano da RC2: vitória 5/5, derrota→reinício, atalhos e áudio/feedback. Evidências e limites em [RELEASE-1.0-VALIDATION](docs/RELEASE-1.0-VALIDATION.md): 93/93 EditMode, 69/69 PlayMode, build sem erros, ZIP extraído e 208 hashes conferidos. Esses resultados pertencem à release, não certificam automaticamente revisões posteriores.

Depois da release entraram chama animada, modelos/ícones de itens, equipamento visível, HUD de poções, VFX de combo/giro/acerto e atalhos de teste (`aff1895`). Testes históricos ITEMS-COMBAT: 99 EditMode; PlayMode com 74 aprovados e 1 caso visual explícito ignorado, mais retestes específicos. A revisão artística humana e o visual das armas/colete na build não estão integralmente aprovados. [COMO-TESTAR](COMO-TESTAR.md) identifica o jogo normal e o cenário diagnóstico com itens; diagnóstico não prova vitória normal.

Nesta auditoria, 99/99 EditMode passaram antes da interrupção por trabalho externo. Inspeção real de 207 assets via Editor não encontrou referências inválidas; a retomada tem relatório separado. Resultados atuais, alterações preservadas e cobertura exata em [PROJECT_AUDIT](PROJECT_AUDIT.md). Não confundir inspeção estática, Test Runner, revisão visual e confirmação humana.

## Marcos e critérios permanentes

| Marco | Escopo e aceite verificável | Estado da 1.0 / evidência |
| --- | --- | --- |
| M0 — Fundação | Unity/URP/pacotes e layers corretos; Git com metas e exclusões; Bootstrap inicia sem erros | Concluído na release; FoundationValidator/Tests, Bootstrap_LoadsAndInitializesWithoutErrors e histórico Git cf3720d |
| M1 — Arena | Chão/paredes/luz/câmera fixa ~45°; WASD relativo à câmera com CharacterController.Move; mira mouse em XZ; ataque com alcance/dano/tempo; dash/cooldown/invulnerabilidade; melee NavMeshAgent Idle/Chase/Attack/Dead; HP/HUD/flash/shake; morte e reinício; builder idempotente e testes | Implementado e validado no escopo da release; MvpSceneAcceptanceTests, PlayerInputComboTests, PlayerComboCombatTests, MvpBuilderPreservationTests e aceite humano. Regressão visual futura segue o roteiro de build |
| M2 — Salas | Prefabs/portas/spawn points; entrar fecha, gera onda, última morte abre; ranged/elite e quatro salas fixas sem softlock | Concluído na release; RoomSystemsPlayModeTests/FloorIntegrationTests e percurso normal. Reativação reconcilia mortes e progresso por referências/eventos |
| M3 — Boss | Quinta sala, duas fases, transição única a 50%, 3 s invulnerável, padrão diferente, telegraph ≥1 s, HUD só durante luta e vitória 5/5 | Concluído na release; BossTests/BossPlayModeTests/GeneratedBossPresentationTests e confirmação humana Windows. Não atribuir vitória à automação que só observou parte da run |
| M4 — Progressão/apresentação | XP/nível/cura/+10% de dano; ouro/poção/arma por SO; projéteis pooled e efeitos reutilizados; animação/áudio/legibilidade/balanço; desempenho medido em Windows | Concluído conforme D-001/D-002; Progression/Inventory/Special/GameAudio/GameOptions tests e medições específicas. Não se exige zero GC global nem meta de FPS inventada |
| R3 — Produto 1.0 | Bolsa 16 slots, equipamento manual, raridades, Q/pilhas por potência/pausa/descarte; especial/recurso/defesa; menu/pausa/opções persistentes | Concluído conforme [regras aprovadas](docs/PRODUCT-1.0-PROPOSAL.md); fórmulas em GAME_DESIGN e baseline em docs/BALANCE-1.0 |
| R4 — Distribuição | Build x64 reproduzível, logs/limites, medição comparável, pacote CRC/SHA256 e smoke extraído | Concluído para o pacote original; reconstruções posteriores não equivalem ao ZIP antigo. Ver docs/BUILD-WINDOWS |

## Limites e próximas entregas

F04 acrescenta remapeamento de teclado/botões do mouse em Opções → Controles, com conflitos recusados, cancelamento/retorno seguro por Escape, reset, rótulos derivados das ações e persistência própria de bindings. Fonte runtime explícita cobre gameplay, bolsa/poção, menus e reinício sem editar o `.inputactions` compartilhado. Evidência desta revisão: 153/153 EditMode, 35/35 PlayMode funcionais/de regressão e uma captura PlayMode explícita em 720p/1080p; build/smoke e limites em [Reports/F04](Reports/F04/README.md). Esses resultados não substituem o aceite da run normal ou a revisão artística pós-release.

F05 confirma cura efetiva, coleta aceita e nível final com três efeitos reutilizados, fila limitada e áudio no mixer existente. Rejeições não geram confirmação; pausa/morte/reset/desativação e reentrada são cobertos. Evidência desta revisão: 158/158 EditMode, 83/83 PlayMode, composição idempotente 1/1, captura gráfica 1/1 em 720p/1080p e build Windows com zero erros (509 avisos de shader). Callbacks/partículas aquecidos tiveram zero alocações com controle positivo; texto é medido separadamente. Fontes, relatórios e limites em [Reports/F05](Reports/F05/README.md). O aceite de uma run normal completa e a revisão artística geral continuam separados.

F07 acrescenta visão temporizada ao ranged a partir da origem real do projétil, bloqueada por Obstacle/Door. Perda amostrada no windup cancela o tiro; novo preparo respeita cooldown, pausa e reativação. NavMesh reposiciona com destinos limitados pelo thinkInterval. Evidências: 159/159 EditMode, 32/32 PlayMode funcionais, 1/1 captura gráfica em 2560×1440 e build Windows sem erros (496 avisos de shader). Composição idempotente, reprodução e limites em [Reports/F07](Reports/F07/README.md); a captura diagnóstica na cena de release é distinta de uma run normal.

Uma run nova reinicia progressão/inventário; somente opções persistem. Não há campanha salva, lojas, quests, coop, geração procedural ou publicação em loja no produto aprovado. Novidades são propostas do [PROMPT_CATALOG](PROMPT_CATALOG.md), priorizadas em [NEXT_STEPS](NEXT_STEPS.md); não reimplementar combo, inventário, especial, áudio ou arte de itens como ausentes.

Histórico incremental permanece em [CHANGELOG](CHANGELOG.md), XML/JSON de `Reports` e Git. Checklists antigos foram consolidados no [roteiro de regressão](docs/BUILD-WINDOWS.md#roteiro-de-regressão); remover caixas históricas contraditórias não amplia o aceite comprovado.

## Relatório por tarefa

Informar objetivo, base/branch, arquivos alterados, comportamento, critérios atendidos com evidência, testes executados, validação pendente, riscos e próximo passo. Se não houve execução na Unity, escrever `NÃO VALIDADO NO EDITOR`. Uma cena jogável, testada e reproduzível precede expansão de escopo.
