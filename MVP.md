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

A entrega inicial F08 acrescentou um baú experimental à primeira sala fixa de release: F próximo após limpar, abertura/sorteio únicos e pickups sem expiração, com recusa por capacidade e identidade preservadas. E continua giro; reativação/reset de HP não renovam recompensa, e reinício cria outra run. Dados exclusivos em SOs e builder idempotente; novo slot de interação mantém bindings v1 anteriores. Evidências: 161/161 EditMode, inspeção adicional de composição/Missing Script 1/1, 131 PlayMode aprovados (8 explícitos ignorados) e captura gráfica 1/1 em 2560×1440. Build Windows isolada sem erros (496 avisos de shader). Reprodução e limites em [Reports/F08](Reports/F08/README.md). Merge depende da revisão do usuário; diagnóstico não certifica partida normal completa.

Revisão F08 solicitada pelo usuário: um baú independente em cada uma das cinco salas, incluindo Boss. Coletar recompensas na vitória devolve acesso à sala/bolsa sem desfazer 5/5. Trinta chamas órfãs foram reparadas mantendo exatamente os transforms das tochas e as onze luzes existentes. Rebuild M1 agora aplica arte somente na arena e é coberto contra alterações nas salas/release. Evidências atuais: 161/161 EditMode e 132 PlayMode aprovados, zero falhas (9 explícitos ignorados); retestes finais 3 EditMode + 20 PlayMode, captura gráfica 1/1 e build Windows sem erros (496 avisos de shader). Evidências e limites em [Reports/F08-AllRooms](Reports/F08-AllRooms/README.md).

F09 acrescenta um tipo de vaso de 40 HP em SO, um por sala de release. Básico/giro
deduplicam os dois colliders; quebra única, destroços estáticos, sem drops/XP ou
dependência da limpeza. Layer própria sem bloqueio físico, centro/portas/spawns e
NavMesh preservados. Evidência: 163/163 EditMode e 146 PlayMode aprovados, zero
falhas (10 capturas explícitas ignoradas no batch), mais captura gráfica 1/1
inspecionada em 2560×1440. Reprodução e limites em
[Reports/F09](Reports/F09/README.md); merge depende de revisão do usuário.

F10 acrescenta escala de texto/HUD 100/125/150%, fallback 100 e JSON v4,
com rascunho/Aplicar/Voltar/reset e bolsa rolável. 41 EditMode pertinentes
aprovados, mais reteste final de 13 F10; 23 PlayMode funcionais e matriz gráfica
1/1 com 66 capturas em 720p/1080p/ultrawide emulado. Geometria/alvos medidos;
sem certificação de contraste ou acessibilidade completa. [Evidências e limites](Reports/F10/README.md);
merge depende da revisão do usuário.

F12 (D-005) acrescenta uma escolha de talento por nível em Pausa → Talentos: três opções fixas em SO, termo próprio que preserva +10%/nível, cura de nível e armas, checkpoint v2 com migração de v1. [Regras](docs/TALENTS-F12.md) e [evidências/limites](Reports/F12/README.md); balanceamento sem playtest humano e merge dependem da revisão do usuário.

Correção de portas (achado da F13): portas fechadas das salas de release agora carvam o NavMesh (`NavMeshObstacle` na raiz da porta, ligado só enquanto fechada), então nenhum inimigo atravessa o vão; portas abertas mantêm as salas conectadas. PlayMode 165/176 (0 falhas, 11 capturas explícitas), EditMode 229/230 (falha pré-existente da F10); sem captura gráfica ou build. [Evidências](Reports/DoorNavMesh/README.md); merge depende da revisão do usuário.

F13 (experimental) acrescenta o lanceiro de primitivas numa sala de teste fora do build: aviso linear ≥1 s com direção travada, investida pelo NavMeshAgent recortada por parede/porta e um dano por investida. Encontros de release inalterados. 244/245 EditMode (falha pré-existente F10), 14/14 PlayMode F13 e captura gráfica 1/1; sem playtest humano, build ou A13. [Regras](docs/LANCER-F13.md) e [evidências/achados](Reports/F13/README.md); merge depende da revisão do usuário.

Uma run nova reinicia progressão/inventário; opções/bindings persistem e, desde F11 (D-004), um checkpoint local entre salas pode ser retomado por **Continuar run** até Nova run, Reiniciar ou morte ([regras](docs/CHECKPOINTS-F11.md), [evidências](Reports/F11/README.md)). Não há campanha salva, lojas, quests, coop, geração procedural ou publicação em loja no produto aprovado. Novidades são propostas do [PROMPT_CATALOG](PROMPT_CATALOG.md), priorizadas em [NEXT_STEPS](NEXT_STEPS.md); não reimplementar combo, inventário, especial, áudio ou arte de itens como ausentes.

Histórico incremental permanece em [CHANGELOG](CHANGELOG.md), XML/JSON de `Reports` e Git. Checklists antigos foram consolidados no [roteiro de regressão](docs/BUILD-WINDOWS.md#roteiro-de-regressão); remover caixas históricas contraditórias não amplia o aceite comprovado.

F15: protótipo isolado da Sentinela de Pedra com uma fase, leque frontal
anunciado, recuperação e cancelamento, arena própria fora da release e reinício
local sem checkpoint. Guardião/loot global preservados. Valores continuam
propostas; A14 e integração em andares ficam para outra tarefa.
33 EditMode, 20 PlayMode funcionais/de regressão, 2 provas gráficas no Editor e
14 testes no WindowsPlayer aprovados; luta com movimento/combo/giro reais e
build jogável isolada com zero erros. Sem aprovação humana de balanceamento.
[Regras](docs/STONE-SENTINEL-F15.md) e [evidências/limites](Reports/F15/README.md).

V01 (18/11/2026, estático, NÃO VALIDADO NO EDITOR): F01–F05, F07–F15 e A01–A15 estão na main (PRs #1–#31; F01/F02 por commit direto). As notas acima de "merge depende da revisão" estão superadas. F06 não foi implementado. Candidato base proposto: sem F06, com F13/F15 isolados e A13–A15 como conceito. [Matriz e limites](Reports/Validation/V01/20261118-21b702c/README.md). V02 (18/11/2026, batchmode): EditMode 269/269 e PlayMode 199/0/15 Explicit. A regressão `IntegrateTwice_…` da F10 foi corrigida e os integradores estão idempotentes. [Evidências](Reports/Validation/V02/20261118-2e78c61/README.md). V03 (18/11/2026): ícones de dash/giro (A01/A02) integrados ao HUD das quatro cenas de gameplay, incluindo F14, sem mudar regras. EditMode 281/281, PlayMode 201/0/16 Explicit e captura 720p/1080p × 100%/150%. Sem aprovação artística humana. [Evidências](Reports/Validation/V03/20261118-cc391c8/README.md). V05 (18/11/2026): sheets/atlas de cura, nível e coleta (A04–A06) ligados aos emissores do F05 sem mudar regras; rejeições não emitem. EditMode 291/291, PlayMode 212/0/18 Explicit e captura 720p/1080p. Sem aprovação artística humana. [Evidências](Reports/Validation/V05/20261118-2f9e9d6/README.md).

D-006 (18/11/2026, branch `claude/equipment-six-slots`): seis categorias de equipamento ativas ao mesmo tempo, vinculadas à bolsa de 16 (sem armazenamento extra), com grade Capacete | Arma | Acessório / Armadura | Luvas | Botas, ATRIBUTOS do domínio e DETALHES com comparação do mesmo slot. Bônus plano de dano e defesa compostos uma vez; checkpoint v4 com v1–v3 legíveis; transferência entre andares preservada. Tabelas de loot da release inalteradas; peças novas no cenário diagnóstico. Valores sem playtest humano; arte das peças pendente. [Regras](docs/EQUIPMENT-SLOTS.md) e [evidências](Reports/EquipmentSlots/README.md).

D-007 (18/11/2026, branch `claude/equipment-storage`, empilhada sobre D-006): equipamento passa a ter armazenamento próprio — cada unidade em um só lugar (bolsa, slot ou chão), equipar/trocar/desequipar movem itens, desequipar com bolsa cheia é recusado sem alteração, ordenar/largar agem só na bolsa e coletar nunca equipa. Nova run começa com a Espada de Ferro (Comum) equipada uma única vez. Checkpoint v5 com migração v1–v4 sem duplicação. [Regras](docs/EQUIPMENT-SLOTS.md) e [evidências](Reports/EquipmentStorage/README.md).

## Relatório por tarefa

Informar objetivo, base/branch, arquivos alterados, comportamento, critérios atendidos com evidência, testes executados, validação pendente, riscos e próximo passo. Se não houve execução na Unity, escrever `NÃO VALIDADO NO EDITOR`. Uma cena jogável, testada e reproduzível precede expansão de escopo.


F14 — primeiro incremento: uma sala fixa de primitivas no segundo andar e descida voluntária após as cinco salas preservadas do primeiro. Estado do jogador segue integralmente; checkpoint v3 na entrada segura, retomada por floorId e reinício/nova run no primeiro andar. Não declara um segundo andar completo de cinco salas nem balanceamento humano aprovado. 66 EditMode e 35 PlayMode pertinentes aprovados, mais reteste F14 9/9 e percurso gráfico completo com movimento/combate reais automatizados. Evidências/limitações em [Reports/F14](Reports/F14/README.md).
