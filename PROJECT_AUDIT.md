# Auditoria do projeto — Dungeon Crawler

Data local: **14/11/2026, America/Fortaleza**. Missão: auditoria, limpeza segura e preparação de continuidade; nenhuma funcionalidade nova ou imagem foi produzida. Responsável: Codex. O produto continua regido por AGENTS → GAME_DESIGN → ARCHITECTURE → MVP.

## Resultado e recuperação

Base inicial limpa `5873d7a14538cbfaeaa98c2b6e839868ddfb8bd8`, branch criada `codex/project-audit-cleanup`. O trabalho externo que surgiu durante os testes foi preservado; o usuário pediu aguardar e depois informou “pronto”. A retomada usa também **aff1895**, commit de atalhos/diagnóstico feito pelo outro trabalho. Não houve reset, stash, remoção de funcionalidades, regeneração de arte, mudança de GUID ou remoção de pacote.

**14 Markdown obsoletos removidos por consolidação**, após inventário/hash e revisão de dependências; **5 documentos de produção compactados**, mantendo títulos/âncoras e fichas/prompts integrais no manifesto JSON. Os quatro documentos fundamentais permanecem; somente PROJECT_AUDIT, PROMPT_CATALOG e NEXT_STEPS foram criados como Markdown desta missão. COMO-TESTAR veio do trabalho externo e foi preservado.

Recuperação dos documentos retirados: Git `aff1895` (e base inicial), por exemplo `git show aff1895:ROADMAP.md`; não restaurar todo o working tree para recuperar um arquivo. Candidatos/hashes anteriores em [removal-candidates.json](Reports/ProjectAudit/removal-candidates.json). Evidências brutas continuam em Reports; a consolidação remove instruções operacionais superadas, não substitui resultados históricos por validação nova.

Na retomada havia alterações locais em Player.prefab, Floor_01.unity, MVP_Arena.unity e Reports/Leadership/asset-audit-current.json. Seus hashes estão em [inventory-resumed.json](Reports/ProjectAudit/inventory-resumed.json), junto das mudanças desta auditoria. Foram preservadas byte a byte nesta retomada; não se atribui automaticamente sua origem ao outro agente ou aos testes. Não integrar esses arquivos num commit documental sem revisar a autoria/diff. O relatório API anterior não foi sobrescrito por esta retomada. Ao carregar o Editor, AnalyticsDefineManager do pacote de inferência removeu automaticamente SENTIS_ANALYTICS_ENABLED; a diferença foi isolada, conferida contra o snapshot e restaurada exatamente ao baseline após encerrar o Editor (editor-side-effect.json). Nenhuma preferência de analytics foi alterada.

## Estrutura antes e depois

```text
Antes (48 Markdown; 49 após o handoff externo)
raiz: AGENTS/GAME_DESIGN/ARCHITECTURE/MVP + ROADMAP/BACKLOG/SETUP
      + MVP_AUDIT + M1..M4_CHECKLIST + ART_AND_COMBAT_REQUEST + CLAUDE/CHANGELOG
Tasks/: 3 briefs já superados
docs/: 7 documentos, incluindo outra auditoria e QA separada
Art/ProductionSpecs/: manifesto + 7 documentos, com 145 fichas repetidas
Tools/ e Reports/: ferramentas, guias e evidências

Depois (38 Markdown)
raiz: 4 fundamentais + CLAUDE/CHANGELOG + COMO-TESTAR
      + PROJECT_AUDIT + PROMPT_CATALOG + NEXT_STEPS
docs/: 5 documentos de regras, baseline, build/regressão e aceite histórico
Art/ProductionSpecs/: mesmo manifesto e guias; 5 viraram guias/índices por ID
Tools/Audit/: inventário somente leitura, sem dependências externas
Reports/ProjectAudit/: snapshots, prova de consolidação, XMLs e inspeções API
Assets/Packages/ProjectSettings: estrutura de gameplay preservada
```

A pasta Tasks pode ficar vazia no disco, pois somente os três arquivos versionados foram retirados. Não se fez limpeza recursiva de diretórios. `_GameTest` contém a cena real de release apesar do nome; não é pasta temporária descartável. SpriteLab/billboard são experimentos separados, não a direção oficial do jogo.

## Método e cobertura

- Inventário inicial: **1.568 arquivos versionados, 48 Markdown, 189 C# (incluindo Tools), 11 cenas, 36 prefabs, 21 FBX, 14 GLB, 90 materiais, 96 PNG, 9 WAV, 9 controllers e 9 .anim externos**. FBX/GLB contêm clips internos e não são somados como animações distintas. Inventário-resumed inclui o novo commit, arquivos locais e instrumentos desta missão; os totais não são snapshots equivalentes.
- Leitura dos documentos fundamentais, briefs/checklists removidos, guias operacionais, relatórios e registros artísticos. Nos cinco grandes documentos gerados, os campos de cada ficha foram cruzados programaticamente com o JSON; ver prova abaixo. Inventário de todos os Markdown com tamanho, SHA256 e títulos; decisão por arquivo ao final.
- Inspeção estática dos módulos de combate, inventário/progressão, UI, IA, pooling, efeitos/equipamento, pausa/opções, builders, cenas de release, input, testes e scripts dos geradores. Buscas de padrões cobrem Assets/Tools; não se apresenta isso como revisão manual linha a linha de todos os scripts nem como profiling.
- Checker Unity da skill: 1.118 arquivos examinados na execução inicial, **zero erros/avisos**. Inspeção pelas APIs do Editor carregou **207 assets serializados**: 11 cenas, 36 prefabs, 59 .asset, 90 materiais, 9 controllers e 2 inputactions. Cenas binárias foram abertas pelas APIs; nenhuma edição textual de cena/prefab foi feita.
- Versões confirmadas: Unity 6000.6.9f1, URP 17.6.0, Input 1.20.1, Cinemachine 6.6.0, AI Navigation 2.0.16, UGUI 2.6.0, Test Framework 1.8.0 no lock. FoundationValidator confirmou URP, Input novo/legado desabilitado, Force Text, layers/pastas e ações Gameplay.
- Skills awesome-gamedev-agent-skills em `.claude/skills`: unity-csharp-scripting e agent-checks; performance-optimization; game-docs/doc-rules; rpg; create-game-assets; game-ui-ux; unity-animation; game-feel. Baseline 6000.3 não prevalece sobre a instalação 6000.6. Nenhum pacote/plugin foi instalado. Cópias aninhadas e skills de outras engines não foram carregadas como contexto de trabalho.

### Consolidação comprovada de fichas

[spec-consolidation.json](Reports/ProjectAudit/spec-consolidation.json) registra hashes anteriores, contagens e verificações de **145 fichas** em ANIMATION_AUDIT (54), ITEMS_AND_EQUIPMENT (22), UI_UX_ASSET_SPECS (23), VFX_ASSET_SPECS (16) e ASSET_GENERATION_PROMPTS (30). Descrição/finalidade, requisitos/formato/dimensões, evidências, critérios e dependências técnicas foram conferidos contra ASSET_MANIFEST.json; os **30 prompts ingleses embutidos são idênticos** aos do JSON. Uma diferença de espaçamento em FX-TELEGRAPH foi normalizada na comparação.

Status/integração e condições antigas “lote futuro” foram substituídos pelo estado do manifesto/lotes já produzidos. O JSON não foi alterado: identidade, especificações e prompts originais permanecem nele. Redução desses cinco arquivos: **361.222 → 119.953 bytes** antes dos pequenos ajustes de atualização; hashes/tamanhos finais estão no inventário final. Os guias mantêm conteúdo técnico específico, índices e âncoras; os agentes agora abrem apenas `resources[id=...]`, em vez de reler 5 cópias dos mesmos dados.

## Achados e tratamento

| ID / prioridade | Evidência concreta | Tratamento nesta missão |
| --- | --- | --- |
| DOC-01 / P1 | SETUP dizia que não havia jogo; Tasks diziam que não havia Git/combos; quatro checklists continham regras anteriores a D-002; MVP misturava aceite final com caixas antigas abertas | Consolidado em MVP, GDD/decisões, BUILD-WINDOWS, NEXT_STEPS e este relatório; histórico permanece no Git/Reports/CHANGELOG |
| DOC-02 / P1 | Fichas/índices pediam ícones, modelos, binding e chama já presentes; manifesto registra117 existentes,13 ausentes,1 incompleto,2 não verificados | Índices atualizados e aviso de fotografia histórica; novos prompts não repetem os lotes concluídos |
| DOC-03 / P1 | ART_DIRECTION dizia “nenhuma imagem”, skills ausentes e rig Humanoid; D-002/arquitetura e importadores usam Generic | Corrigido status e preservada direção/paleta. Brief antigo marcado histórico. Migração de rig/atlas continua bloqueada na parte afetada, sem mudança de modelo/material |
| CODE-01 / P2 | CameraOcclusionHider: buffer fixo16 em SphereCastNonAlloc; restaura shadowCastingMode como On em vez de guardar valor anterior | Risco estático, sem reprodução de falha nesta missão. Propor teste com >16 obstáculos e renderer originalmente Off/TwoSided antes de corrigir |
| CODE-02 / P2 | EnemyCombat mantém windingUp/target/hitAt e não cancela em OnDisable; EnemyBrain apenas remove assinatura em OnDisable | Possível impacto antigo após reativação; a pausa congela tempo e possui cancelamento explícito, portanto não declarar defeito de pausa sem reprodução. Teste de ciclo de vida recomendado |
| CODE-03 / P2 | EnemyReward usa lista estática Buffer durante foreach de drops; callback reentrante pode modificar armazenamento | Risco condicionado a reentrância, sem falha observada no fluxo atual. Não refatorado sem teste de reprodução |
| CODE-04 / P2 | PlayerCombat e PlayerSpecial repetem crescimento de buffer/HashSet/dano, mas básico filtra cone e especial é radial | Refatoração maior proposta; manter autoridades/cancelamentos. Redução de duplicação não é ganho medido de CPU |
| PERF-01 / P2 | Pickups/visuais/atores usam Instantiate em recompensa/onda; projéteis e áudio já são reutilizados; VFX reutilizam componentes | Não há perfil novo de frame que justifique pools adicionais. Preservado comportamento; proposta de medição em NEXT_STEPS |
| PERF-02 / P2 | Espadas/frasco do lote usam3–5 materiais contra meta proposta1–2; gerados usam vários materiais e UV projetada | Não é prova de gargalo ou defeito funcional. Avaliar drawcalls/GPU com Frame Debugger antes de atlas/material merge; não impor migração UV |
| ORG-01 / P2 | 44 grupos de bytes idênticos;23 fontes C#/asmdef têm cópias instaladas idênticas;16 recursos finais também estão no Source~ | Preservados: são fonte/instalação, staging reproduzível ou evidência, não redundância descartável. Detalhes em inventory-before/resumed |
| ORG-02 / P2 | Scene/paths _GameTest/_GameArt e scripts Editor de600–770 linhas | Nome enganoso e concentração de responsabilidades; não mover/repartir nesta limpeza, pois builders/testes e destino da release dependem dos caminhos |
| DEP-01 / P2 | Manifest inclui AI Assistant/Inference/Collab/IDE e módulos de template; locks têm transitivos/builtin | Sem demonstração de remoção segura por BuildReport/Package Manager. Nenhum pacote removido; ausência de using não prova desuso de ferramenta ou serviço |
| QA-01 / P1 | Aceite 1.0 anterior ao lote visual e aff1895; teste de captura é Explicit; braços/roupa na build ainda sem cobertura visual integral | Testes atuais separados do aceite histórico. QA humana/equipamentos/ultrawide e perfil pós-VFX continuam pendentes |
| RIGHTS-01 / P2 | PROVENANCE registra origem/licença desconhecida dos dois conceitos antigos do Hero | Preservar registro; não inferir licença comercial nem regenerar identidade. Completar origem real quando disponível |

Runtime: não foram encontrados Input legado ou buscas globais usadas como injeção de dependências nos módulos de gameplay examinados. FindAnyObjectByType do novo PlaytestShortcuts é ferramenta de Editor, não DI de runtime. Player usa CharacterController.Move; inimigos usam NavMeshAgent com thinkInterval. UI solicita comandos e representa estado, sem decidir dano/cura/progressão. Não se removeu componente pelo simples fato de ficar oculto: Body/AttackVisual/flash de compatibilidade ainda têm referências e diagnósticos.

### Candidatos preservados e motivos

| Grupo | Verificação / motivo para preservar |
| --- | --- |
| FBX + GLB dos personagens/itens | FBX importa no Unity; GLB é fonte de revisão/validação do gerador. Não são bytes ou funções equivalentes |
| Source~ e outputs finais idênticos | Staging guarda fonte/procedência/reprodução; integradores consultam esses caminhos. Source~ é deliberadamente ignorado pelo importador, não lixo sem uso |
| Tools/*/unity e _Scripts/_SpriteLab | 23 pares exatos; Tools é fonte instalável, Assets compila na Unity. Há bridges instaladas adicionais próprias do protótipo; não apagá-las por não terem par |
| SpriteLab, billboard e cenas de teste | Experimentos fora das duas cenas de release, com builders/tests/previews próprios; uso fora do player ainda é uso |
| SampleScene, Readme/TutorialInfo e assets Mobile | SampleScene ainda habilitada na lista compartilhada; build de release seleciona somente MainMenu/andar explicitamente. Volume/render/default input também aparecem em configurações. Sem deleção sem grafo completo e validação específica |
| Reports com JSONs de mesmo hash | Controles de alocação e snapshots pertencem a sessões/cargas diferentes. Mesmo valor não elimina função de evidência |
| Library/Logs/Builds/UserSettings/csproj/slnx/caches | .gitignore já exclui; builds e capturas locais são úteis ao usuário. Não houve limpeza de disco nem promessa de espaço recuperado |
| .claude/skills | Biblioteca local ignorada, inclui duplicatas aninhadas; sem recuperação Git e possível consumo pelo outro agente. Melhorar roteamento, preservar LICENSE/NOTICE e arquivos |

## Estado real das funcionalidades

| Sistema | Evidência / estado |
| --- | --- |
| Movimento/mira/dash/HP/reinício | Implementados; testes MvpSceneAcceptance/Combat/Input e aceite histórico de partida. Não reexecutado visualmente nesta auditoria |
| Combo de três golpes | Implementado; modelo/consultas/input/cancelamentos/deduplicação/saturação testados. Timer do domínio aplica dano, Animator apresenta |
| Especial/estamina/defesa | Implementados, dados SO e testes de custo/regen/impacto/cancelamento; sem repetição ao segurar E/direito |
| Melee/ranged/elite, salas e Boss | Implementados; testes de portas/reativação/pool/fases e integração. Vitória normal da 1.0 é confirmação humana, não simulação diagnóstica |
| XP/ouro/loot | Implementados; fórmula de nível aditiva, cura configurada, tabelas independentes e coleta aceita. Dados atuais prevalecem sobre checklists antigos |
| Inventário/raridades/equipamento/poções | Implementados;16 slots, stack 5 por potência, auto-equip inicial, trocas manuais, armadura, Q/Usar, largar/recusar/recoletar. Ordenação/comparação/política de Q não estão prontas |
| Menus/pausa/opções/áudio | Implementados e testados; opções JSON/PlayerPrefs, cópia runtime URP e pool 8 vozes. Não há save de campanha |
| Itens/ícones/equipamento visível/HUD | Produzidos/integrados, testes de dados/cena. Aprovação artística e inspeção visual completa na build continuam parciais |
| Chama/VFX de combate | ANIM-TORCH, slash/giro/FX-HIT básico existem. FX-HIT do especial e cura/coleta/nível/dash/cauda/Slam são complementos pendentes |
| Run com itens de teste | Implementada por aff1895 e coberta no PlayMode atual; off por padrão, flag -dcc-items. Não é loot normal nem nova progressão |
| Save, talentos, novos inimigos/Boss/andar/baú/destrutíveis | Ausentes ou futuros condicionais, sugestões F08–F15; não iniciados nesta missão |
| Atlas/Humanoid | Premissas antigas em conflito com preservação D-002; parte afetada interrompida, sem migração disfarçada |

## Otimizações efetivamente realizadas

1. Leitura seletiva por tarefa/ID em AGENTS e CLAUDE; fila única NEXT_STEPS e estado único MVP. Regras técnicas e decisões fundamentais mantidas.
2. Consolidação de 14 documentos, compressão de 5 índices e retirada de status superados; fórmulas, paleta, identidade, evidências e prompts antigos preservados nos destinos/JSON/Git apropriados.
3. `Tools/Audit/audit_project.py`: inventário/hash/duplicatas/cópias/links em um comando, stdlib, somente leitura dos arquivos de entrada, recusa sobrescrita de saída. Links cobrem paths inline; anchors/external URLs/bare paths exigem checker complementar.
4. MvpProjectAudit ganhou menu/entrada de baseline, saída selecionável por DUNGEON_CRAWLER_AUDIT_REPORT e proteção contra sobrescrever essa saída; criação do diretório acompanha o caminho escolhido. Inspeção continua sem salvar cenas/prefabs.

**Nenhuma otimização de runtime ou ganho de FPS/CPU/GC foi implementado ou reivindicado.** Os riscos de ciclo de vida/oclusão exigem reprodução antes de refatoração; esta missão não cria requisitos novos nem remove comportamento para simplificar.

## Verificações executadas e limitações

| Execução desta missão | Resultado / artefato |
| --- | --- |
| Git e recuperação | Estado inicial limpo, branch isolada; interrupção por edição externa e retomada autorizada; snapshots before/resumed/final |
| Importação/compilação Unity | Batchmode 6000.6.9f1 concluído com código 0 nas inspeções; sem erros CS. Avisos de pacotes/Editor são registrados no log; não se promete Console sem qualquer aviso |
| Inspeção API inicial | 207 assets, issues=[]; [editor-assets.json](Reports/ProjectAudit/editor-assets.json) |
| Inspeção API após retomada | 207 assets, issues=[]; [editor-assets-resumed.json](Reports/ProjectAudit/editor-assets-resumed.json); fundação aprovada |
| EditMode inicial | **99/99**,0 falhas/0 skip; [EditMode.xml](Reports/ProjectAudit/EditMode.xml). Execução anterior ao commit externo aff1895; não afirmar cobertura de mudanças que entraram depois |
| PlayMode após retomada | **76 casos:75 aprovados,0 falhas,1 ignorado**; [PlayMode.xml](Reports/ProjectAudit/PlayMode.xml). O caso ignorado é captura visual Explicit que requer Game view renderizado, não falha de gameplay |
| Consolidação | 145 fichas verificadas;30 prompts embutidos exatos contra JSON inalterado; prova e hashes em spec-consolidation.json |
| Checker estático/links/diff/hashes finais | Checker Unity final: 1.122 arquivos, zero erros/avisos; checker documental: 38 Markdown, zero erros/avisos. 23 pares fonte/cópia idênticos, links locais válidos e 1.214 arquivos não documentais de Assets/Packages/ProjectSettings preservados contra o snapshot da retomada |

Não houve novo BuildPlayer, reconstrução de ZIP, playthrough manual, captura visual, escuta ou medição de GPU/Frame Debugger nesta missão. Os testes PlayMode rodam headless na Unity real: validam asserts, não aparência. **Revisão visual desta missão: NÃO VALIDADO NO EDITOR.** Essa indicação não nega que o Test Runner e as APIs tenham sido executados. Medições publicadas de 1.0 usam carga/hardware delimitados; não garantem FPS de todos os encontros ou hardware atual.

## Próximas escolhas e prompts prontos

Cinco funcionalidades recomendadas: **F01 comparação de equipamento; F02 ordenação segura; F05 feedback de cura/coleta/nível; F06 tutorial contextual; F07 linha de visão ranged**. Cinco assets prioritários: **A01 ícone dash; A02 ícone giro; A03 peças de UI; A04 cura; A05 nível**. Todos têm prompt completo, dependências e aceite em [PROMPT_CATALOG](PROMPT_CATALOG.md); ordem e pendências em [NEXT_STEPS](NEXT_STEPS.md). Não executar os30 prompts de uma vez.

## Fechamento da auditoria

- **Documentação:** 49 → 38 Markdown em relação à retomada (48 originais + COMO-TESTAR externo); 14 removidos por consolidação, 3 novos entregáveis e 5 índices compactados. Aproximadamente 36% menos bytes UTF-8 de Markdown, já incluindo o catálogo; não é estimativa exata de tokens nem redução do tamanho do jogo.
- **Checks finais:** [docs-final.json](Reports/ProjectAudit/docs-final.json), [static-final.json](Reports/ProjectAudit/static-final.json), [final-validation.json](Reports/ProjectAudit/final-validation.json) e [inventory-final.json](Reports/ProjectAudit/inventory-final.json). Checker documental: 0 erros/0 avisos, uma nota esperada de GDD sem índice de features. Conferidos 15prompts de funcionalidades e 15 de arte com os campos pedidos; zero mudanças inesperadas nos 1.214 arquivos preservados.
- **Entrega:** alterações desta missão permanecem revisáveis no working tree da branch codex/project-audit-cleanup; sem commit abrangendo trabalho alheio, push ou publicação. A recuperação dos 14 documentos retirados continua disponível em aff1895.
- **Pendente:** QA visual/humana pós-release, armas/colete na build, ultrawide/hardware adicional, origem dos conceitos antigos e reprodução dos riscos estáticos de oclusão/ciclo de vida/reentrância. Nenhuma dessas pendências foi mascarada como corrigida ou testada visualmente.

## Decisão por documento existente

O inventário abaixo inclui os48 iniciais e COMO-TESTAR do handoff. Documentos de evidência datada continuam históricos; não são lidos como backlog corrente.

| Documento | Finalidade | Decisão / destino |
| --- | --- | --- |
| `AGENTS.md` | Política e mapa de leitura | Atualizado; leitura por escopo, skills/handoff e proteção de evidências |
| `ARCHITECTURE.md` | Contratos técnicos | Atualizado; versões/classes reais, pools e apresentação atuais |
| `ART_AND_COMBAT_REQUEST.md` | Brief antigo de arte/combate | Removido; estado em MVP/auditoria, direção preservada em Concepts, sugestões no catálogo |
| `Assets/_Project/Art/Concepts/AGENT_PROMPT_CHARACTER_ASSETS.md` | Brief original de personagens | Preservado com aviso histórico; contém ajustes únicos de identidade, não executar migração antiga |
| `Assets/_Project/Art/Concepts/ART_DIRECTION_AND_PROMPTS.md` | Identidade/paleta/métricas | Atualizado; status real/Generic, parâmetros e identidade preservados |
| `Assets/_Project/Art/Concepts/PROVENANCE.md` | Origem/licença/limites das imagens | Mantido sem alteração; registro canônico, inclui origem desconhecida |
| `Assets/_Project/Art/ProductionSpecs/ANIMATION_AUDIT.md` | Contratos de animação e 54 fichas | Compactado; fichas no manifesto, diagnóstico de chama atualizado |
| `Assets/_Project/Art/ProductionSpecs/ASSET_GENERATION_PROMPTS.md` | 30 briefs artísticos originais | Compactado em índice;30 prompts exatos permanecem no JSON; próximos15 no catálogo |
| `Assets/_Project/Art/ProductionSpecs/FULL_ASSET_AUDIT.md` | Inventário lógico de recursos | Mantido; contagem117/13/1/2 atualizada no topo e fotografia antiga rotulada |
| `Assets/_Project/Art/ProductionSpecs/ITEMS_AND_EQUIPMENT.md` | Regras visuais e22 fichas de itens | Compactado; fonte única no manifesto, lote integrado preservado |
| `Assets/_Project/Art/ProductionSpecs/UI_UX_ASSET_SPECS.md` | Layout/estados e23 fichas | Compactado; binding/ícones/HUD atuais diferenciados de fotografia antiga |
| `Assets/_Project/Art/ProductionSpecs/UNITY_INTEGRATION_GUIDE.md` | Importação/integração/QA | Mantido com atualização de bindings já implementados; contratos únicos preservados |
| `Assets/_Project/Art/ProductionSpecs/VFX_ASSET_SPECS.md` | Eventos/legibilidade e16 fichas | Compactado; slash/giro/hit/chama atuais, próximos efeitos separados |
| `BACKLOG.md` | Fila e histórico da 1.0 | Removido; resultados em CHANGELOG/Reports, próximas entregas em NEXT_STEPS |
| `CHANGELOG.md` | Histórico de entregas | Mantido; referências redirecionadas e entrada desta auditoria |
| `CLAUDE.md` | Papel e coordenação do revisor | Atualizado; encaminha ao mapa AGENTS sem duplicar leitura integral |
| `COMO-TESTAR.md` | Atalhos do jogo/diagnóstico | Mantido sem edição; criado pelo outro trabalho em aff1895 |
| `docs/BALANCE-1.0.md` | Valores e evidência de balanço RC2 | Mantido; link de regressão corrigido, valores não alterados |
| `docs/BUILD-WINDOWS.md` | Build/testes/reconstrução/distribuição | Atualizado; setup existente e regressão M1–M4/QA consolidados |
| `docs/MVP-AUDIT.md` | Segunda auditoria e deltas | Removido; diagnóstico aqui, resultados em CHANGELOG/Reports/relatório de release |
| `docs/PRODUCT-1.0-PROPOSAL.md` | Regras aprovadas D-002 | Mantido; link de decisão corrigido. Não é proposta pendente |
| `docs/RELEASE-1.0-VALIDATION.md` | Matriz de aceite e limites da release | Mantido sem alteração; prova histórica indispensável |
| `docs/RELEASE-CANDIDATE-QA.md` | Checklist do candidato | Removido; passos incorporados a BUILD-WINDOWS/roteiro-de-regressão |
| `GAME_DESIGN.md` | Regras e decisões de produto | Atualizado; D-001/D-002 recebidos do ROADMAP, expansões separadas |
| `M1_CHECKLIST.md` | Checklist histórico M1 | Removido; critérios em MVP, passos em BUILD-WINDOWS, valores atuais em BALANCE-1.0 |
| `M2_CHECKLIST.md` | Checklist histórico M2 | Removido; critérios em MVP, passos em BUILD-WINDOWS, valores atuais em BALANCE-1.0 |
| `M3_CHECKLIST.md` | Checklist histórico M3 | Removido; critérios em MVP, passos em BUILD-WINDOWS, valores atuais em BALANCE-1.0 |
| `M4_CHECKLIST.md` | Checklist histórico M4 | Removido; critérios em MVP, passos em BUILD-WINDOWS, valores atuais em BALANCE-1.0 |
| `MVP.md` | Critérios e estado por marco | Consolidado; caixas históricas substituídas por aceite/evidência delimitados |
| `MVP_AUDIT.md` | Auditoria inicial de 26critérios | Removido; síntese/histórico/riscos neste relatório, critérios em MVP, provas em Reports/MVP |
| `Reports/Leadership/AudioPerf/README.md` | Medição/execução e limites | Mantido; mesma carga/hardware e limites preservados, pendências antigas rotuladas |
| `Reports/Leadership/PoolPerf/README.md` | Medição/execução e limites | Mantido; mesma carga/hardware e limites preservados, pendências antigas rotuladas |
| `Reports/Leadership/SpecialPerf/README.md` | Medição/execução e limites | Mantido; mesma carga/hardware e limites preservados, pendências antigas rotuladas |
| `Reports/Leadership/WindowsPerf/README.md` | Medição/execução e limites | Mantido; mesma carga/hardware e limites preservados, pendências antigas rotuladas |
| `Reports/Task02/README.md` | Medição/execução e limites | Mantido; mesma carga/hardware e limites preservados, pendências antigas rotuladas |
| `ROADMAP.md` | Fronteira/decisões/entregas1.0 | Removido; decisões em GDD, marcos em MVP e fila em NEXT_STEPS |
| `SETUP.md` | Inicialização de kit vazio | Removido; abertura do projeto existente em docs/BUILD-WINDOWS |
| `Tasks/01_IMAGES_AND_UNITY_IMPORT.md` | Task preparada antes dos conceitos atuais | Removido; proveniência/identidade em Concepts, atlas conflitante registrado, novas produções no catálogo |
| `Tasks/02_PLAYER_COMBOS.md` | Task de combo já entregue | Removido; comportamento/testes/medidas em Reports/Task02 e arquitetura atual |
| `Tasks/03_REMAINING_TASKS.md` | Fila anterior aos reparos e 1.0 | Removido; resultados em CHANGELOG/Reports, riscos remanescentes aqui e fila em NEXT_STEPS |
| `Tools/AudioGenerator/DIAGNOSTICS.md` | Catálogo/relato técnico histórico | Mantido; comandos/fontes/limitações únicos. Relatos datados rotulados quando desatualizados |
| `Tools/AudioGenerator/README.md` | Guia operacional da ferramenta | Mantido; comandos/fontes/limitações únicos. Relatos datados rotulados quando desatualizados |
| `Tools/Lemo3DGenerator/BUILD_TROUBLESHOOTING.md` | Catálogo/relato técnico histórico | Mantido; comandos/fontes/limitações únicos. Relatos datados rotulados quando desatualizados |
| `Tools/Lemo3DGenerator/CATALOG.md` | Catálogo/relato técnico histórico | Mantido; comandos/fontes/limitações únicos. Relatos datados rotulados quando desatualizados |
| `Tools/Lemo3DGenerator/FLOOR_BUILD_WORKFLOW.md` | Catálogo/relato técnico histórico | Mantido; comandos/fontes/limitações únicos. Relatos datados rotulados quando desatualizados |
| `Tools/Lemo3DGenerator/README.md` | Guia operacional da ferramenta | Mantido; comandos/fontes/limitações únicos. Relatos datados rotulados quando desatualizados |
| `Tools/Lemo3DGenerator/REPORT.md` | Catálogo/relato técnico histórico | Mantido; comandos/fontes/limitações únicos. Relatos datados rotulados quando desatualizados |
| `Tools/LemoSpriteLab/README.md` | Guia operacional da ferramenta | Mantido; comandos/fontes/limitações únicos. Relatos datados rotulados quando desatualizados |
| `Tools/LemoSpriteLab/REPORT.md` | Catálogo/relato técnico histórico | Mantido; comandos/fontes/limitações únicos. Relatos datados rotulados quando desatualizados |
