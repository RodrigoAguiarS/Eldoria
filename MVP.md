# MVP.md — Plano de entregas

## Estado atual — candidato RC2 (14/11/2026)

Candidato RC2: 93/93 EditMode e 69/69 PlayMode passaram. Áudio funcional/mixer/pool, opções gráficas reais com cópia do pipeline, migração das opções e coletores opcionais integrados. Builds release e diagnóstico geradas sem erros; release preserva 496 avisos upstream conhecidos. Comparação Windows em carga sintética equivalente concluída para CPU/GPU/frame/memória; GC em diagnóstico e medição/escuta do áudio seguem em QA. A 1.0 completa ainda não foi declarada pronta.

| Entrega | Evidência corrente | Aceite restante |
| --- | --- | --- |
| Fundação e reparos | Git local, referências reparadas, builders idempotentes, pool aquecido e suítes completas aprovadas | Nenhum bloqueio técnico aberto desta etapa |
| Arena e andar | Movimento/combate/morte/reinício testados; vitória normal 5/5 confirmada em Editor e Windows antes do áudio | Conferência visual final de flash/tremor e ciclo derrota→reinício no Windows atual |
| Inventário e especial | Dados/modelos/UI integrados; vitória com ambos observada no Editor; especial por direito observado no Windows | Percurso Windows atual, tecla física E, equipamento/poções e legibilidade |
| Menus/opções/áudio | Persistência/resolução observadas no Windows; presets reais, mixer/eventos/pool aprovados em testes | Qualidade visual, escuta e medição da mistura Windows |
| Distribuição | RC2 x64 gerado sem erros; medições release e empacotador preparados | GC comparável, QA do candidato, versão final e ZIP verificado |

Cada marco deve produzir estado executável e verificável. Não avançar com erros de compilação ou referências faltantes. Não confundir análise de código com execução no Unity Editor. Planejamento: [ROADMAP.md](ROADMAP.md); execução: [BACKLOG.md](BACKLOG.md); provas e histórico detalhado: [docs/MVP-AUDIT.md](docs/MVP-AUDIT.md).

## Histórico de validações incrementais

Os registros e checklists abaixo preservam o estado de cada etapa na data da execução. Pendências antigas foram resolvidas em entregas posteriores; o quadro acima descreve o estado atual.

Menus/opções integrados: 90 EditMode e idempotência final 2/2; quatro testes de menus revalidados após corrigir tolerância numérica da fixture e o layout de dropdown encontrado na revisão. Windows abriu menu→andar e executou o especial por direito; ajuste 720p/janela persistiu após sair/reabrir, e 1080p/tela cheia foi restaurado. Layouts observados em ambas as resoluções. Controle de tremor/flash passou no teste; áudio e QA final da 1.0 ainda pendentes.

Especial/estamina implementados com 83/83 EditMode e 60/60 PlayMode aprovados, preservando progressão/arma e clips anteriores do Hero. HUD observado no Editor; revisão visual do giro interrompida pelo usuário e build Windows do especial pendentes. Menus completos, áudio e opções seguem abertos.

R3 — inventário integrado aos três cenários de gameplay: 75/75 EditMode e 49/49 PlayMode passaram, incluindo cliques reais New Input System, equipamento, poções, pausa sem dano pendente e descarte que preserva itens. Layout/raridades observados no Editor em PlayMode com conteúdo de preview; a revisão visual antecedeu o botão Largar, cuja interação passou em teste. Especial, menus completos, áudio e opções ainda estão em desenvolvimento; 1.0 não concluída.
Cada marco deve produzir estado executável e verificável. Não avançar com erros de compilação ou referências faltantes. Não confundir análise de código com execução no Unity Editor.

Reparo de renderização FIX-002 validado: referências obsoletas e subassets sem script limpos pelas APIs; 19 componentes de volume preservados. Build Windows abre sem os avisos de pós-processamento diagnosticados. Evidências e limites em [docs/MVP-AUDIT.md](docs/MVP-AUDIT.md).

PERF-001 validado com 30 testes PlayMode: pool aquecido com 16 instâncias, rajada sem novas criações e zero alocações nos disparos aquecidos segundo Profiler com controle positivo. O contador de bytes do thread desta instalação não é usado como prova de GC. Medições em [PoolPerf](Reports/Leadership/PoolPerf/README.md).

R1 técnico entregue: armas/render/builders/pool/ciclo de vida reparados; 53 EditMode e 31 PlayMode passaram. Build Windows atual abre e executa ataque sem Missing Script/exceções/avisos Animator. O aceite completo do MVP/andar, incluindo Boss e validação visual de teclado/reinício Windows, permanece aberto em R2.

Atualização posterior: usuário confirmou WASD/dash/R na build Windows. Apresentação da fase 2 do Warden corrigida e observada no Editor (branco → vermelho, modelo visível); suítes atuais com 54 EditMode/32 PlayMode. Na partida do usuário, observados quatro salas concluídas e Boss em fase 2, sem observar a vitória. Revisão visual da arena confirmou perseguição/HP/morte; botão Reiniciar e flash/shake ainda em validação. R2 continua aberto.

Validação mais recente: 54 EditMode/38 PlayMode; integração das cinco ondas, portas/última morte/vitória única, reativação das salas/andar e ranged pelo pool real. Botão Reiniciar passou com clique New Input System. Usuário confirmou a vitória normal do Editor com 5/5; run Windows atual e conferência visual de flash/shake seguem pendentes. Regras concretas de R3 aprovadas em D-002; não são novos critérios retroativos de M1.

Confirmação posterior do usuário: executável Windows atualizado concluído até vitória/5 de 5. Log registra as cinco ondas e nenhuma exceção/Missing Script; ver Reports/Leadership/windows-normal-run.json. A revisão visual de flash/shake e o ciclo de morte/reinício Windows permanecem na QA final. R3 inicia pelos dados/modelos isolados enquanto essas verificações finais ficam rastreadas.

## Histórico — auditoria e condução técnica em 13/11/2026

Auditoria corrente com classificação por requisito e evidências: [docs/MVP-AUDIT.md](docs/MVP-AUDIT.md). Planejamento: [ROADMAP.md](ROADMAP.md); execução: [BACKLOG.md](BACKLOG.md); alterações: [CHANGELOG.md](CHANGELOG.md).

Compilação validada na Unity 6000.6.9f1; 45 testes EditMode passaram. Os 21 testes PlayMode anteriores e seis novos testes de Bootstrap/MVP_Arena passaram (os novos foram reexecutados após corrigir três falhas das fixtures). Movimento, mira, dash/invulnerabilidade, combo, navegação/dano melee e morte/reinício têm provas na cena real. Ainda faltam conferência visual específica da arena, estabilidade dos builders, reparo das armas, integração visual da fase 2 e percurso completo do andar. **O MVP global continua pendente.**

Atualização FIX-001: armas/tabelas reparadas com GUIDs preservados; **51/51 EditMode e 28/28 PlayMode passaram**, incluindo coleta física de arma/XP, cura e aplicação da progressão. Build Windows gerada e iniciada com **0 Missing Script e 0 exceções** no log; quatro avisos URP permanecem. Git local inicializado em cf3720d. O quadro de auditoria anterior abaixo é histórico; [auditoria corrente](docs/MVP-AUDIT.md) contém os deltas por entrega.

## Auditoria anterior — 13/11/2026 (histórico)

**O MVP ainda não está completo.** A revisão dos 26 itens e suas evidências estão em [MVP_AUDIT.md](MVP_AUDIT.md).

| Marco | Situação verificada |
| --- | --- |
| M0 | Fundação configurada; falta repositório Git/commit inicial e confirmar o smoke test da Bootstrap. |
| M1 | Sistemas implementados; aceite manual completo ainda pendente. |
| M2 | Sistemas implementados; falta comprovar as quatro salas completas sem softlock. |
| M3 | Lógica implementada; visual da fase 2 precisa integração ao Boss gerado e falta comprovar a vitória no andar. |
| M4 | Parcial: progressão e projéteis presentes; armas com script inválido e acabamento/desempenho pendentes. |

Testes executados no Unity 6000.6.9f1: **32/32 EditMode e 5/5 PlayMode passaram**. Resultados em `Reports/MVP/`. Os testes atuais não cobrem uma partida completa. As caixas abaixo foram preservadas: presença de código e testes de componentes não substituem o aceite de cada marco.

## M0 — Fundação
- [x] Criar projeto Unity 6 URP e repositório Git. (commit inicial cf3720d; histórico local)
- [x] Confirmar versão exata de Unity, Input System, Cinemachine, AI Navigation e Test Framework. (FoundationValidator/EditMode-baseline.xml)
- [x] Criar pastas e documentos; configurar .gitignore e `.meta`. (inventário, validação e exclusões revisadas)
- [x] Criar cena bootstrap simples e validar PlayMode sem erros. (Arena-baseline.xml, Bootstrap_LoadsAndInitializesWithoutErrors)
**Aceite:** Editor abre projeto, Console sem erros, commit inicial reproduzível.

## M1 — Vertical Slice: uma arena jogável
- [ ] Arena com chão, paredes, luz e câmera Cinemachine a ~45°.
- [x] Player CharacterController com WASD relativo à tela. (Arena-baseline.xml, WasdMovesOnGroundAndMouseAimsInXZ)
- [x] Mira pelo mouse via raycast e rotação no plano XZ. (mesmo teste de cena)
- [x] Ataque básico com alcance, dano e cooldown. (testes de combo/Task02 e morte do melee na arena)
- [x] Dash com cooldown e invulnerabilidade breve. (Arena-baseline.xml, DashMovesGrantsInvulnerabilityAndRespectsCooldown)
- [x] Inimigo melee com NavMeshAgent e estados Idle/Chase/Attack/Dead. (testes da arena: navegação, dano e morte/agent/collider)
- [ ] HP do player e inimigo; barra de vida, feedback de dano.
- [x] Morte e reinício da cena. (Arena-baseline.xml, DeathDisablesPlayerAndRestartRestoresSavedScene)
- [ ] Builder de cena idempotente via menu do Editor.
- [ ] Testes EditMode/PlayMode pertinentes e checklist manual.
**Aceite:** entrar em PlayMode, mover, mirar, atacar, matar inimigo, receber dano, morrer e reiniciar sem exceções.

## M2 — Loop de salas
- [ ] Prefab Room, portas e pontos de spawn.
- [ ] Fechar portas ao entrar, spawnar inimigos, reabrir após último morrer.
- [ ] Inimigos ranged e elite.
- [ ] Sequência fixa de quatro salas comuns.
**Aceite:** completar quatro salas sem softlock, portas só abrem quando sala limpa.

## M3 — Boss e andar
- [ ] Sala final e Boss com duas fases.
- [ ] Transição aos 50% HP, invulnerabilidade de 3 s e mudança de padrão.
- [ ] Telegraph visível 1 s antes de ataques pesados.
- [ ] Barra de HP de Boss durante luta e tela de vitória.
**Aceite:** completar andar de cinco salas; fase do Boss ocorre uma única vez.

## M4 — Progressão e acabamento
- [ ] XP, níveis, cura ao subir e +10% de dano.
- [ ] Loot de ouro, poção e arma configurado por ScriptableObjects.
- [ ] Object pooling de projéteis e efeitos repetidos.
- [ ] Áudio, animações, VFX, ajustes de acessibilidade e balanceamento.
**Aceite:** progressão consistente, sem erros de console, desempenho medido em build Windows.

## Modelo de relatório por tarefa
**Marco/tarefa:**
**Arquivos alterados:**
**Critérios atendidos:**
**Validação executada (com evidência):**
**Validação pendente:**
**Riscos e próximo passo:**
