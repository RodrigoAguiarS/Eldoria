# Próximas entregas

Fila única após a auditoria de 14/11/2026. Escopo existente em [MVP](MVP.md), achados em [PROJECT_AUDIT](PROJECT_AUDIT.md), prompts em [PROMPT_CATALOG](PROMPT_CATALOG.md). Prioridades expressam ordem sugerida, não autorização para executar todas as sugestões.

## Ordem recomendada

| Ordem | Entrega | Dependências | Critério de saída |
| --- | --- | --- | --- |
| 1 / P0 | Fechar a QA da revisão pós-release | Trabalho externo aff1895 concluído; preservar alterações locais identificadas | Registrar commit/versão, testar partida normal e diagnóstico separadamente; armas/colete na build, Run/Dash/Hit/Death, vitória e derrota→reinício; aprovação artística explícita ou pendente identificada |
| 2 / P1 | Reproduzir riscos técnicos antes de refatorar | Baseline de hashes e testes isolados | CameraOcclusionHider com >16 obstáculos e modos de sombra diferentes; EnemyCombat desativado no windup; loot reentrante. Corrigir somente regressões reproduzidas, com testes antes/depois |
| 3 / P1 | Comparação de equipamentos — F01 entregue em 15/11/2026 | InventoryModel/Presenter e catálogo visual atuais | 24 EditMode + 26 PlayMode aprovados; captura PlayMode adicional aprovada, texto conferido em 720p/1080p. Atual/selecionado/diferença, baseline, defesa e poções cobertos; [evidências](Reports/F01/README.md) |
| 4 / P1 | Ordenação segura — F02 entregue em 15/11/2026 | F01 opcional, índices de equipamento tratados explicitamente | 32 EditMode + 32 PlayMode aprovados (500 comandos aleatórios, duplicatas, vazios, morte, pausa, zero ataques); captura aprovada em 720p/1080p na cena de release; [evidências](Reports/F02/README.md) |
| 5 / P1 | Feedback de cura/coleta/nível — F05 | Hooks de domínio; A04–A06 opcionais | Efeito só após operação aceita, reutilizado, resetável e compatível com pausa; não reaplicar cura/XP/dano |
| 6 / P1 | Onboarding contextual — F06 | Menus e input existentes | Ensinar WASD/mira/combate/dash/E/I/Q sem prender o jogador nem repetir em toda run |
| 7 / P1 | Ranged com linha de visão — F07 | EnemyBrain/Combat/Navigation, pool e NavMesh | Parede bloqueia tentativa de disparo; decisão temporizada; alvo móvel, pausa, morte e perda de visão testados |
| 8 / P2 | Escolher uma expansão curta | F08 baú **ou** F09 destrutíveis; A11/A12 | Primeiro uma sala fixa; definir regras antes de alterar loot/colisão. Não iniciar todos os sistemas |
| 9 / P2 | Acessibilidade/input e legibilidade | F03, F04, F10; A01–A03 | Opções validadas/persistentes, conflitos resolvidos, layouts em 720p/1080p e tamanhos adicionais documentados |
| 10 / P2–P3 | Conteúdo/progressão de longo prazo | F11–F15 conforme escolha; A13–A15 | Especificação curta aprovada, primeiro incremento isolado; sem geração procedural ou migração Humanoid implícita |

As cinco primeiras funcionalidades sugeridas para escolha são **F01 comparação**, **F02 ordenação**, **F05 feedback**, **F06 onboarding** e **F07 linha de visão**. Dash/giro já existem; seus ícones A01/A02 são polimento. Cinco assets iniciais: A01, A02, A03, A04 e A05. Arte de baú/inimigo/Boss/área depende primeiro da funcionalidade correspondente.

## Melhorias de arquitetura propostas

- Manter domínio puro separado de presenters. InventoryModel depende de índices estáveis: ordenação exige remapeamento explícito; não ordenar apenas o array apresentado na UI.
- Considerar extrair a consulta melee/radial compartilhada somente depois de testes de reentrância, saturação e filtros; as duas consultas têm semânticas distintas. Não buscar redução de linhas às custas da autoridade do combate.
- Builders de 600–770 linhas podem separar importação, composição e validação em entregas próprias. Preservar menus, GUIDs e fontes/cópias instaladas; nenhum movimento de pastas nesta auditoria.
- Antes de pooling de pickups/atores ou atlas de materiais, medir carga normal no Windows e definir orçamento. Pools de projéteis e áudio já estão implementados; não reivindicar esse trabalho como otimização nova.
- Revisar tamanho de build por BuildReport antes de propor remoção de AI Assistant/Inference, Collab/IDE ou módulos de template. Separar ferramentas de Editor, transitivos e runtime. Não remover pacote por ausência de `using`.
- Caso se escolha F11 (save), criar IDs persistentes de definições e migração versionada; GUID de asset e índice de slot não são identificadores universais de instâncias.

## Pendências de testes

Priorizar run normal Windows após mudanças pós-release e equipamentos visíveis; diagnóstico com itens não substitui a progressão normal. Revalidar ultrawide/escala de texto, hardware adicional, leitura de silhueta a 128 px e todos os movimentos equipados. Validar eventual otimização com mesma versão/carga/hardware, aquecimento e controle positivo de alocação; não usar o contador de bytes do thread que falhou nesta máquina. Desempenho da 1.0 é histórico, não baseline automático dos VFX novos.

Os 99 EditMode desta auditoria e inspeções API têm artefatos próprios em `Reports/ProjectAudit`; cobertura/limites e resultado PlayMode constam em PROJECT_AUDIT. Não executar builders só para revisar Markdown. Conferir diff/hashes após testes que os chamam.

## Fluxo de trabalho com IA

1. Escolher um ID e um incremento. Ler AGENTS e seções de GDD/arquitetura/MVP pertinentes; consultar somente o achado/ficha relacionado. Carregar 1–3 skills relevantes, não a biblioteca inteira.
2. Codex registra base/branch, plano curto, arquivos reservados e aceite. Claude revisa design/balanço/arquitetura sem edição concorrente. Agente artístico recebe referências anexadas, pasta exclusiva, formato real e critério de QA.
3. Integrar por APIs do Editor, testar comportamento e revisar visualmente a cena de release. Separar geração de bitmap, modelagem/rig e integração; nenhuma ferramenta recebe capacidades presumidas.
4. Atualizar somente a regra/contrato afetado e uma linha de estado/evidência. Handoff: base, arquivos, decisões, testes, pendência, próximo comando. Evidências detalhadas ficam em Reports; CHANGELOG guarda histórico, não instruções operacionais duplicadas.
5. Rodar o inventário/leitor de links e checker de Unity documentados em ARCHITECTURE. Usar saída nova e benchmarks em pasta exclusiva. Revisar e integrar commits por escopo; não incluir assets desconhecidos numa limpeza documental.

Não há cronograma imposto. O primeiro marco histórico continua pequeno; o produto atual já passou desse marco. Atlas/Humanoid permanecem divergências bloqueadas para a parte afetada, sem impedir polimento compatível com o rig Generic existente.
