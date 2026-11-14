# Roadmap — Dungeon Crawler até 1.0

Base: [GAME_DESIGN.md](GAME_DESIGN.md), regras de [AGENTS.md](AGENTS.md), estado real em [docs/MVP-AUDIT.md](docs/MVP-AUDIT.md). Plano incremental; a ordem pode mudar com evidências. Não são promessas de prazo.

## Fronteira proposta de produto

**Base já autorizada:** estabilizar M0–M4 e o andar fixo com quatro salas + Boss, herói/arte/rig Generic atuais, combo de três golpes, melee/ranged/elite inimigos, dash, progressão/loot e morte/reinício/vitória.

**Escopo de 1.0 aprovado pelo usuário em 13/11/2026:** um andar completo e polido, com especial do jogador, recurso e defesa previstos no GDD, **inventário interativo e raridades**, menu inicial/pausa/configurações, som/música, opções de legibilidade/shake/volume, configurações gráficas e distribuição Windows. Detalhes do especial, recurso, defesa e inventário serão especificados concretamente antes do incremento correspondente. Preservar fórmulas/drop rates durante os reparos.

**Expansões opcionais:** múltiplos slots de armadura/afixos complexos, ranged do jogador, novos andares/biomas, geração procedural, save de campanha, quests, coop, lojas e publicação em loja. Inventário e raridades foram promovidos a 1.0 por decisão explícita; isso não os torna requisitos retroativos de M1.

## Entregas e portas de aceite

| Marco | Escopo | Depende de | Aceite verificável | Estado |
| --- | --- | --- | --- | --- |
| R0 — Auditoria e base | Inventário/refs, testes reais de Bootstrap/Arena, docs e Git | Instalação Unity atual | Relatórios com classificações, XMLs, backup, histórico local e nenhum arquivo alheio sobrescrito | Entregue, commit cf3720d |
| R1 — MVP estável | Armas, referências inválidas, builders, ciclo de vida, aquecimento do pool | R0 | Assets reais carregam; GUIDs/valores preservados; regressões pertinentes passam; builds sem os Missing Script diagnosticados | Próxima entrega: armas |
| R2 — Andar completo | Fluxo das quatro salas, ranged/elite, Boss/visual/transição/telegraph, HUD/vitória | R1 | Testes de integração e percurso normal Editor/Windows até vitória; morte/reinício; sem porta prematura/softlock | Pendente |
| R3 — Fechamento de produto | Especial/recurso/defesa, inventário/raridades, menus, áudio, acessibilidade/opções | R2 + especificação dos incrementos | Cada sistema tem dados configuráveis, testes e playtest; controles legíveis, regras fora da UI | Escopo aprovado, planejado |
| R4 — Candidato 1.0 | Balanço fundamentado em playtest, CPU/GPU/frame/GC/memória, build release/distribuição | R3 | Checklist completo; executável reproduzível, logs sem erros, evidência de início–fim e configuração persistente; riscos conhecidos documentados | Pendente |

Não se inventou meta numérica de FPS, duração de campanha ou pacing por nível. Primeiro medir no hardware alvo e apresentar uma proposta. Uma animação mais curta não demonstra otimização.

## Política de execução

Codex implementa/integra/testa. Claude pode revisar design/arte/arquitetura mediante tarefa delimitada; nenhum resultado é presumido. Não há edição concorrente do mesmo arquivo/cena/prefab. Commits locais por entrega verificada; sem publicação/remoto automaticamente.

O jogo deve continuar executável ao fim de cada incremento. Um marco só é fechado com as provas de seu aceite; presença de scripts e uma build criada não substituem jogabilidade. [BACKLOG.md](BACKLOG.md) é a fila operacional; [CHANGELOG.md](CHANGELOG.md) registra resultados, e a auditoria guarda evidências/limites.

## Decisões de produto

| ID | Data | Decisão | Alternativas | Motivo/evidência | Impacto |
| --- | --- | --- | --- | --- | --- |
| D-001 | 13/11/2026 | Incluir inventário interativo e raridades na 1.0 do andar fixo | Manter como expansão | Usuário escolheu explicitamente “Incluir inventário e raridades na 1.0” | R3, RPG-002; ranged do jogador continua opcional |
