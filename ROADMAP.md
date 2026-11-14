# Roadmap — Dungeon Crawler até 1.0

Base: [GAME_DESIGN.md](GAME_DESIGN.md), regras de [AGENTS.md](AGENTS.md), estado real em [docs/MVP-AUDIT.md](docs/MVP-AUDIT.md). Plano incremental; a ordem pode mudar com evidências. Não são promessas de prazo.

## Fronteira proposta de produto

**Base já autorizada:** estabilizar M0–M4 e o andar fixo com quatro salas + Boss, herói/arte/rig Generic atuais, combo de três golpes, melee/ranged/elite inimigos, dash, progressão/loot e morte/reinício/vitória.

**Proposta de 1.0 para aprovação de produto:** um andar completo e polido, com especial do jogador, recurso e defesa previstos no GDD, menu inicial/pausa/configurações, som/música, opções de legibilidade/shake/volume, configurações gráficas e distribuição Windows. Detalhes do especial, gasto/recuperação do recurso e efeito da defesa exigem uma proposta concreta antes de implementar; não serão inventados durante as correções.

**Expansões opcionais propostas:** inventário com gerenciamento de slots, múltiplos equipamentos/afixos/raridades, ranged do jogador, novos andares/biomas, geração procedural, save de campanha, quests, coop, lojas e publicação em loja. Alguns constam da lista ampla do usuário, mas não têm regras no GDD; não entram silenciosamente no MVP. Inventário/raridades podem ser promovidos a 1.0 por decisão explícita.

## Entregas e portas de aceite

| Marco | Escopo | Depende de | Aceite verificável | Estado |
| --- | --- | --- | --- | --- |
| R0 — Auditoria e base | Inventário/refs, testes reais de Bootstrap/Arena, docs e Git | Instalação Unity atual | Relatórios com classificações, XMLs, backup, histórico local e nenhum arquivo alheio sobrescrito | Em andamento |
| R1 — MVP estável | Armas, referências inválidas, builders, ciclo de vida, aquecimento do pool | R0 | Assets reais carregam; GUIDs/valores preservados; regressões pertinentes passam; builds sem os Missing Script diagnosticados | Próxima entrega: armas |
| R2 — Andar completo | Fluxo das quatro salas, ranged/elite, Boss/visual/transição/telegraph, HUD/vitória | R1 | Testes de integração e percurso normal Editor/Windows até vitória; morte/reinício; sem porta prematura/softlock | Pendente |
| R3 — Fechamento de produto | Especial/recurso/defesa e menus, áudio, acessibilidade/opções | R2 + aprovação das regras propostas | Cada sistema tem dados configuráveis, testes e playtest; controles legíveis, regras fora da UI | Planejado, produto a aprovar |
| R4 — Candidato 1.0 | Balanço fundamentado em playtest, CPU/GPU/frame/GC/memória, build release/distribuição | R3 | Checklist completo; executável reproduzível, logs sem erros, evidência de início–fim e configuração persistente; riscos conhecidos documentados | Pendente |

Não se inventou meta numérica de FPS, duração de campanha ou pacing por nível. Primeiro medir no hardware alvo e apresentar uma proposta. Uma animação mais curta não demonstra otimização.

## Política de execução

Codex implementa/integra/testa. Claude pode revisar design/arte/arquitetura mediante tarefa delimitada; nenhum resultado é presumido. Não há edição concorrente do mesmo arquivo/cena/prefab. Commits locais por entrega verificada; sem publicação/remoto automaticamente.

O jogo deve continuar executável ao fim de cada incremento. Um marco só é fechado com as provas de seu aceite; presença de scripts e uma build criada não substituem jogabilidade. [BACKLOG.md](BACKLOG.md) é a fila operacional; [CHANGELOG.md](CHANGELOG.md) registra resultados, e a auditoria guarda evidências/limites.
