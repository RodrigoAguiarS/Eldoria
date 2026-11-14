# MVP.md — Plano de entregas

## Princípio
Cada marco deve produzir estado executável e verificável. Não avançar com erros de compilação ou referências faltantes. Não confundir análise de código com execução no Unity Editor.

## Estado corrente — auditoria e condução técnica em 13/11/2026

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
