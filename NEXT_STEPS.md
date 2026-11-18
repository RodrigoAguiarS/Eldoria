# Próximas entregas

Fila única após a auditoria de 14/11/2026. Escopo existente em [MVP](MVP.md), achados em [PROJECT_AUDIT](PROJECT_AUDIT.md), prompts em [PROMPT_CATALOG](PROMPT_CATALOG.md). Prioridades expressam ordem sugerida, não autorização para executar todas as sugestões.

## Ordem recomendada

| Ordem | Entrega | Dependências | Critério de saída |
| --- | --- | --- | --- |
| 1 / P0 | Fechar a QA da revisão pós-release | Trabalho externo aff1895 concluído; preservar alterações locais identificadas | Registrar commit/versão, testar partida normal e diagnóstico separadamente; armas/colete na build, Run/Dash/Hit/Death, vitória e derrota→reinício; aprovação artística explícita ou pendente identificada |
| 2 / P1 | Reproduzir riscos técnicos antes de refatorar | Baseline de hashes e testes isolados | CameraOcclusionHider com >16 obstáculos e modos de sombra diferentes; EnemyCombat desativado no windup; loot reentrante. Corrigir somente regressões reproduzidas, com testes antes/depois |
| 3 / P1 | Comparação de equipamentos — F01 entregue em 15/11/2026 | InventoryModel/Presenter e catálogo visual atuais | 24 EditMode + 26 PlayMode aprovados; captura PlayMode adicional aprovada, texto conferido em 720p/1080p. Atual/selecionado/diferença, baseline, defesa e poções cobertos; [evidências](Reports/F01/README.md) |
| 4 / P1 | Ordenação segura — F02 entregue em 15/11/2026 | F01 opcional, índices de equipamento tratados explicitamente | 32 EditMode + 32 PlayMode aprovados (500 comandos aleatórios, duplicatas, vazios, morte, pausa, zero ataques); captura aprovada em 720p/1080p na cena de release; [evidências](Reports/F02/README.md) |
| 5 / P1 | Feedback de cura/coleta/nível — F05 implementado em 16/11/2026 | Hooks de confirmação; material existente, A04–A06 opcionais | 158 EditMode, 83 PlayMode, composição idempotente e captura gráfica aprovados; build Windows sem erros. Três efeitos reutilizados, fila limitada, rejeição/reentrada/pausa/reset cobertos; sem reaplicar cura/XP/dano. [Evidências e limites](Reports/F05/README.md); merge depende da revisão do usuário. A04 produzido em staging em 17/11/2026 (`ProductionStaging/FX-HEAL`, sheet 4×2 de 0,4 s); ligação ao emissor de cura do F05 pendente, proposta em `FX-HEAL/Source~/FX-HEAL.timing.json`. A05 produzido em staging em 17/11/2026 (`ProductionStaging/FX-LEVEL`, sheet 4×3 de 0,7 s); ligação a `LeveledUp` pendente, proposta em `FX-LEVEL/Source~/FX-LEVEL.timing.json`. A06 produzido em staging em 17/11/2026 (`ProductionStaging/FX-LOOT`, atlas 384×128 de três glints XP/ouro/guardado); ligação a `CollectionAccepted` pendente, proposta em `FX-LOOT/Source~/FX-LOOT.atlas.json` |
| 6 / P1 | Onboarding contextual — F06 | Menus e input existentes | Ensinar WASD/mira/combate/dash/E/I/Q sem prender o jogador nem repetir em toda run |
| 7 / P1 | Ranged com linha de visão — F07 implementado em 16/11/2026 | EnemyBrain/Combat/Navigation, pool e NavMesh existentes | Parede/porta bloqueiam preparo/impacto; amostra de 0,1 s, novo windup após perda, NavMesh temporizado e pool reutilizado. 18 casos F07 e regressões funcionais aprovados; captura diagnóstica gráfica na cena real de release. [Evidências e limites](Reports/F07/README.md); merge depende da revisão do usuário. A08 produzido em staging em 17/11/2026 (`ProductionStaging/FX-PROJECTILE`, núcleo 3D de 156 triângulos e um material, mesmo envelope de 0,4 m); filho visual no `Projectile.prefab` é tarefa Codex pendente, proposta em `FX-PROJECTILE/Source~/FX-PROJECTILE.source.json`. A09 produzido em staging em 17/11/2026 (`ProductionStaging/FX-PROJECTILE-TRAIL`, textura 128×32 da cauda laranja→ferrugem, 0,6 m/0,05 s); filho Trail e Clear() no reuso do pool são tarefa Codex pendente, proposta em `FX-PROJECTILE-TRAIL/Source~/FX-PROJECTILE-TRAIL.trail.json` |
| 8 / P2 | F08 — um baú em cada uma das cinco salas | Regra registrada antes da implementação; A11 opcional posterior | F próximo após limpar a própria sala, abertura independente, pickups persistentes e coleta após vitória do Boss. Reparo de chamas órfãs e prevenção de alteração global pelo rebuild M1. [Evidências da correção](Reports/F08-AllRooms/README.md); merge depende da revisão do usuário. F09 permanece separado |
| 9 / P2 | Acessibilidade/input e legibilidade | F03 entregue em 15/11/2026; F04 implementado em 16/11/2026; F10; A01–A03 | F03: Primeira/Menor/Maior potência no Q, HUD consistente e JSON v3; [evidências](Reports/F03/README.md). F04: fonte runtime unificada, captura/cancelamento/conflitos/reset, JSON v1 separado e rótulos dinâmicos; 153 EditMode, 35 PlayMode e captura real em 720p/1080p aprovados; [evidências e limites](Reports/F04/README.md). F10: 100/125/150%, JSON v4/fallback, bolsa rolável e targets preservados; 41 EditMode + 13 de reteste, 23 PlayMode e matriz gráfica 1/1 em 720p/1080p/ultrawide emulado. [Evidências/limites](Reports/F10/README.md). Merge depende da revisão do usuário; A01, A02 e A03 produzidos em staging em 17/11/2026 (`ProductionStaging/ICON-ACTION-DASH`, `ProductionStaging/ICON-ACTION-SPECIAL`, `ProductionStaging/UI-SKIN`; integração no HUD/telas pendente, A03 por builder conforme `UI-SKIN/Source~/UI-SKIN.states.json`; tarefa delimitada em [A03-UI-SKIN-INTEGRATION](docs/A03-UI-SKIN-INTEGRATION.md)); A07 produzido em staging em 17/11/2026 (`ProductionStaging/FX-DASH`, textura 256×64 do rastro do dash); ponte de início/fim do dash é tarefa Codex pendente, proposta em `FX-DASH/Source~/FX-DASH.trail.json` |
| 10 / P2–P3 | Conteúdo/progressão de longo prazo | F11–F15 conforme escolha; A13–A15 | Especificação curta aprovada, primeiro incremento isolado; sem geração procedural ou migração Humanoid implícita |

As cinco primeiras funcionalidades sugeridas para escolha são **F01 comparação**, **F02 ordenação**, **F05 feedback**, **F06 onboarding** e **F07 linha de visão**. Dash/giro já existem; seus ícones A01/A02 são polimento. Cinco assets iniciais: A01, A02, A03, A04 e A05. Arte de baú/inimigo/Boss/área depende primeiro da funcionalidade correspondente.

F09 implementado em 16/11/2026: vasos destrutíveis simples, sem drops, um por sala
de release, quebra única e sem interferir na limpeza/NavMesh. 163 EditMode e
146 PlayMode aprovados; [evidências e limites](Reports/F09/README.md). Integração
na main aguarda revisão do usuário; A12 continua opcional e não foi executado.

F11 implementado em 16/11/2026 (D-004): checkpoint local entre salas, JSON v1
com escrita atômica e backup, Continuar run no menu e restauração sem XP/loot
duplicados. [Regras](docs/CHECKPOINTS-F11.md) e [evidências/limites](Reports/F11/README.md).
Integração na main aguarda revisão do usuário.

F12 implementado em 16/11/2026 (D-005): uma escolha de talento por nível em
Pausa → Talentos, três opções fixas em SO com ranks, checkpoint v2 com migração
de v1. [Regras](docs/TALENTS-F12.md) e [evidências/limites](Reports/F12/README.md).
Balanceamento sem playtest humano; integração na main aguarda revisão do usuário.

F13 implementado em 16/11/2026 como experimento isolado: lanceiro com investida
linear numa sala de teste fora do build. [Regras](docs/LANCER-F13.md) e
[evidências](Reports/F13/README.md). Pendências: playtest dos valores, A13/rig
Generic. O achado de portas sem bloqueio foi corrigido na main; ver Reports/DoorNavMesh.

F15 implementada como protótipo de uma fase em arena separada, sem integrar
F14/release ou alterar loot/Guardião. Leque com aviso ≥1 s, dano único,
recuperação e cancelamentos; [regras](docs/STONE-SENTINEL-F15.md) e
[evidências/limites](Reports/F15/README.md). Próximo passo deste incremento é
revisão/playtest humano dos números e leitura espacial; A14 e novos padrões
dependem dessa avaliação e de tarefa própria. Merge depende da aprovação do usuário.

## Melhorias de arquitetura propostas

- Manter domínio puro separado de presenters. InventoryModel depende de índices estáveis: ordenação exige remapeamento explícito; não ordenar apenas o array apresentado na UI.
- Considerar extrair a consulta melee/radial compartilhada somente depois de testes de reentrância, saturação e filtros; as duas consultas têm semânticas distintas. Não buscar redução de linhas às custas da autoridade do combate.
- Builders de 600–770 linhas podem separar importação, composição e validação em entregas próprias. Preservar menus, GUIDs e fontes/cópias instaladas; nenhum movimento de pastas nesta auditoria.
- Antes de pooling de pickups/atores ou atlas de materiais, medir carga normal no Windows e definir orçamento. Pools de projéteis e áudio já estão implementados; não reivindicar esse trabalho como otimização nova.
- Revisar tamanho de build por BuildReport antes de propor remoção de AI Assistant/Inference, Collab/IDE ou módulos de template. Separar ferramentas de Editor, transitivos e runtime. Não remover pacote por ausência de `using`.
- F11 usa IDs persistentes explícitos em ItemDefinition/RoomController. O schema v3 (F14, entrada do segundo andar) preserva leitura v1/v2 e talentos F12, com testes; versões futuras devem seguir o mesmo padrão. GUID de asset e índice de slot não são identificadores universais de instâncias.

## Pendências de testes

Priorizar run normal Windows após mudanças pós-release e equipamentos visíveis; diagnóstico com itens não substitui a progressão normal. Revalidar ultrawide/escala de texto, hardware adicional, leitura de silhueta a 128 px e todos os movimentos equipados. Validar eventual otimização com mesma versão/carga/hardware, aquecimento e controle positivo de alocação; não usar o contador de bytes do thread que falhou nesta máquina. Desempenho da 1.0 é histórico, não baseline automático dos VFX novos.

Os 99 EditMode desta auditoria e inspeções API têm artefatos próprios em `Reports/ProjectAudit`; cobertura/limites e resultado PlayMode constam em PROJECT_AUDIT. Não executar builders só para revisar Markdown. Conferir diff/hashes após testes que os chamam.

## Fluxo de trabalho com IA

1. Escolher um ID e um incremento. Ler AGENTS e seções de GDD/arquitetura/MVP pertinentes; consultar somente o achado/ficha relacionado. Carregar 1–3 skills relevantes, não a biblioteca inteira.
2. O agente cria a branch da tarefa a partir da `main` atualizada e registra base/branch, plano curto, arquivos reservados e aceite; ao concluir, abre merge request para a `main`. Claude revisa design/balanço/arquitetura sem edição concorrente. Agente artístico recebe referências anexadas, pasta exclusiva, formato real e critério de QA.
3. Integrar por APIs do Editor, testar comportamento e revisar visualmente a cena de release. Separar geração de bitmap, modelagem/rig e integração; nenhuma ferramenta recebe capacidades presumidas.
4. Atualizar somente a regra/contrato afetado e uma linha de estado/evidência. Handoff: base, arquivos, decisões, testes, pendência, próximo comando. Evidências detalhadas ficam em Reports; CHANGELOG guarda histórico, não instruções operacionais duplicadas.
5. Rodar o inventário/leitor de links e checker de Unity documentados em ARCHITECTURE. Usar saída nova e benchmarks em pasta exclusiva. Revisar e integrar commits por escopo; não incluir assets desconhecidos numa limpeza documental.

Não há cronograma imposto. O primeiro marco histórico continua pequeno; o produto atual já passou desse marco. Atlas/Humanoid permanecem divergências bloqueadas para a parte afetada, sem impedir polimento compatível com o rig Generic existente.


F14 iniciada em 17/11/2026 por solicitação do usuário: uma sala jogável fixa e transição a partir do primeiro andar, com persistência explícita de jogador e fronteira segura de checkpoint v3. [Proposta anterior ao código](Reports/F14/PROPOSAL.md) e [evidências/limites](Reports/F14/README.md). Expansão para demais salas/Boss e A15 continuam separadas; números exigem playtest. Merge F13 e correção de portas integrados na main pelo PR #13; merge F14 depende da revisão do usuário.
