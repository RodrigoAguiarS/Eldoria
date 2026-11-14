# Auditoria do MVP — 13/11/2026

Responsável técnico/PO: Codex, conforme solicitação do usuário nesta data. As regras de `AGENTS.md`, o produto de `GAME_DESIGN.md` e a arquitetura existente continuam valendo. Este relatório substitui o diagnóstico de estado atual; `../MVP_AUDIT.md` e `../Reports/Task02/` ficam preservados como histórico.

## Método e evidências

- Documentos lidos: AGENTS, CLAUDE, GAME_DESIGN, ARCHITECTURE, MVP, auditoria anterior e briefs de Tasks. Nenhum resultado de Claude foi presumido.
- Snapshot de 944 arquivos antes das alterações: [initial_files.json](../Reports/Leadership/initial_files.json). Backup externo: `G:/DungeonCrawler/LeadershipBackup_20261113_224614`. Nenhum arquivo mudou entre o snapshot e o início dos instrumentos de auditoria.
- Inventário inicial: 93 scripts C# em Assets, 9 cenas, 25 prefabs, 46 assets, 67 materiais, 6 controllers, 8 clips .anim externos; clips FBX também existem. Nenhum WAV/OGG/MP3 encontrado. Os scripts do jogo e as integrações foram lidos; buscas estáticas cobriram todo Assets, incluindo protótipos. Não foram encontradas buscas globais como injeção de dependência nem Input legado no runtime do projeto.
- [asset-audit.json](../Reports/Leadership/asset-audit.json): inspeção de 155 assets pelo Editor, incluindo hierarquia/referências das nove cenas e dos 25 prefabs. Assets binários foram inspecionados pelas APIs, sem edição textual. Referências nulas opcionais não foram classificadas automaticamente como defeitos.
- Versões reais: Unity 6000.6.9f1; URP 17.6.0; Input System 1.20.1; Cinemachine 6.6.0; AI Navigation 2.0.16; Test Framework 1.8.0. Fundação validada pelo Editor; Force Text configurado, embora cenas antigas ainda estejam serializadas em binário.
- [EditMode-baseline.xml](../Reports/Leadership/EditMode-baseline.xml): 45/45 passaram. [PlayMode-baseline.xml](../Reports/Leadership/PlayMode-baseline.xml): 24/27 na primeira execução, com três falhas nos testes novos; duas eram isolamento/expectativa de log e a terceira era processamento de uma borda de input depois do Update do jogo. Corrigidas somente as fixtures, [Arena-baseline.xml](../Reports/Leadership/Arena-baseline.xml) passou 6/6. Os 21 testes anteriores também passaram na execução inicial. Não houve correção de gameplay para obter esse resultado.
- As seis provas novas carregam as cenas salvas MVP_Arena/Bootstrap. Cobrem movimento CharacterController, mira XZ, dash/invulnerabilidade/cooldown, combo que mata o melee, desligamento do agente/collider, navegação/ataque inimigo e morte/reinício da cena. A fase do input é a mesma do player loop; a fixture oficial isola os dispositivos de teste.
- Task 02: três clips/contatos inspecionados em PlayMode e executável iniciado anteriormente; [relatório](../Reports/Task02/README.md). Não substitui a inspeção visual específica da arena nem um percurso completo das cinco salas.
- Skills locais: router → unity-csharp-scripting + unity-scriptableobjects + rpg + game-docs; unity-input-system para os testes de cena e unity-build-pipeline para distribuição. Baseline da biblioteca: Unity 6000.3; APIs verificadas contra a instalação 6000.6 (por exemplo, EntityId em SerializedProperty). Biblioteca já estava instalada em `.claude/skills`; nenhum plugin/pacote foi instalado.

## Classificação dos 26 itens originais

Status descreve o requisito inteiro. Uma evidência parcial não aprova o restante do item.

| ID | Requisito | Classificação inicial | Evidência e pendência |
| --- | --- | --- | --- |
| M0.1 | Projeto Unity 6 URP e Git | PARCIALMENTE IMPLEMENTADO | Unity/URP funcionam; `git status` informou ausência de repositório. Inicialização/commit previstos nesta entrega. |
| M0.2 | Versões dos pacotes | CONCLUÍDO E VALIDADO | Manifest/lock e FoundationValidator; versões acima, testes da fundação aprovados. |
| M0.3 | Pastas, ignore e metas | CONCLUÍDO E VALIDADO | Estrutura e GUIDs inventariados, sem GUID duplicado; FoundationTests passa. Assets inválidos são tratados separadamente em M4.2. |
| M0.4 | Bootstrap e smoke test | CONCLUÍDO E VALIDADO | `Bootstrap_LoadsAndInitializesWithoutErrors` carrega a cena e exige o diagnóstico real do mapa Gameplay. |
| M1.1 | Arena, luz e câmera isométrica | IMPLEMENTADO, MAS NÃO VALIDADO | Cenas/componentes/referências carregam; provas automatizadas usam a arena. Inspeção visual específica ainda pendente nesta auditoria. |
| M1.2 | WASD relativo à tela/CharacterController | CONCLUÍDO E VALIDADO | `Arena_WasdMovesOnGroundAndMouseAimsInXZ`: deslocamento real no plano e ausência de Rigidbody. |
| M1.3 | Mira por mouse/giro XZ | CONCLUÍDO E VALIDADO | Mesmo teste injeta posição do mouse e verifica direção/rotação do player. |
| M1.4 | Ataque, alcance, dano e cooldown | CONCLUÍDO E VALIDADO | Combo da Task 02; testes de dados, buffer, segurar, multidão, filtros, cancelamento e morte do melee da cena real. |
| M1.5 | Dash e invulnerabilidade | CONCLUÍDO E VALIDADO | Teste real da arena verifica deslocamento, dano bloqueado e recarga que impede segundo dash. |
| M1.6 | Melee com NavMesh e estados | CONCLUÍDO E VALIDADO | Agente na malha, perseguição/ataque que causa dano e morte que desliga agente/collider. FSM Idle/Chase/Attack/Dead preservada. |
| M1.7 | HP, HUD e feedback | IMPLEMENTADO, MAS NÃO VALIDADO | HP/morte e painéis têm provas; cor de dano/shake e legibilidade visual ainda precisam conferência específica. |
| M1.8 | Morte e reinício | CONCLUÍDO E VALIDADO | Teste da arena mata player, verifica componentes/painel e recarrega a cena, restaurando HP e combo. |
| M1.9 | Builder idempotente | PARCIALMENTE IMPLEMENTADO | Builders existem. O builder M1 recria Enemy_Melee com defaults sem recompensas do M4; risco concreto de sobrescrever composição compartilhada. Reexecução/estabilidade de dados precisa prova. |
| M1.10 | Testes e checklist | PARCIALMENTE IMPLEMENTADO | Suítes e checklists existem; cobertura da arena ampliada. Aceite visual integral pendente. |
| M2.1 | Prefabs de sala, portas e spawn | IMPLEMENTADO, MAS NÃO VALIDADO | Inventário/referências resolvidas e teste de porta passa; não houve certificação do percurso completo nesta auditoria. |
| M2.2 | Fechar/spawn/limpar/reabrir | IMPLEMENTADO, MAS NÃO VALIDADO | Tracker/porta testados, fluxo presente. Falta testar sala real com todos os inimigos e impedir abertura antecipada. |
| M2.3 | Ranged e elite | IMPLEMENTADO, MAS NÃO VALIDADO | Dados, prefabs, FSM/recuo e projétil existem; teste comprova reutilização, não ciclo real completo desses inimigos. |
| M2.4 | Quatro salas fixas | IMPLEMENTADO, MAS NÃO VALIDADO | Floor possui quatro salas normais + Boss. Completar todas sem softlock ainda não comprovado nesta auditoria. |
| M3.1 | Sala final/Boss de duas fases | IMPLEMENTADO, MAS NÃO VALIDADO | Dados, prefab e Brain/PhaseModel existem; testes de lógica não equivalem à luta completa. |
| M3.2 | 50%, transição única/3 s/visual | PARCIALMENTE IMPLEMENTADO | Lógica testada. Cor/pulso estão ligados ao placeholder oculto, não ao Warden gerado visível. |
| M3.3 | Telegraph >= 1 s | CONCLUÍDO E VALIDADO | BossTests e BossPlayModeTests verificam limite, tempo e preenchimento; legibilidade na luta completa permanece parte do aceite M3. |
| M3.4 | HUD do Boss e vitória | IMPLEMENTADO, MAS NÃO VALIDADO | Presenters ligados por eventos; falta prova do fluxo real até vitória. |
| M4.1 | XP, cura e +10% de dano | IMPLEMENTADO, MAS NÃO VALIDADO | Fórmula e dano combinado com arma testados. Coleta real, cura por nível e continuidade de uma run ainda não certificados. |
| M4.2 | Ouro, poção e arma por dados | PARCIALMENTE IMPLEMENTADO | Duas armas não carregam; duas tabelas têm referências de arma não resolvidas. Classe WeaponData está dentro de LootTable.cs. |
| M4.3 | Pool de projéteis/efeitos | PARCIALMENTE IMPLEMENTADO | Pool reutiliza após release; não aquece instâncias em Awake. Flash, telegraph e AttackVisual já são reutilizados; nenhum VFX novo foi presumido necessário. |
| M4.4 | Áudio, animação, VFX, acessibilidade/balanço | PARCIALMENTE IMPLEMENTADO | Modelos/clips/feedback existem, combo validado. Não há áudio configurado, opções de acessibilidade, aceite de balanço nem medição completa de frame/GPU em Windows. |

**MVP global: ainda não aprovado.** M0 exige histórico reproduzível; M1 tem validações visuais/builders abertas; M2/M3 exigem percurso e Boss; M4 tem assets inválidos e acabamento ausente.

## Defeitos e riscos priorizados

1. **P0 — Armas inválidas.** `ScriptableObjects/Items/Weapon_IronSword.asset` e `Weapon_WardenBlade.asset` têm m_Script nulo; Loot_Elite/Loot_Boss apontam para elas. Corrigir classe/asset pelo Editor, preservando GUID, nome, bônus 6/12 e referências; acrescentar provas com assets reais, equipamento e build.
2. **P0 — Sem versionamento.** Criar Git local, exclusões verificadas e commits por incremento; não adicionar remoto/publicar. Backups não substituem histórico reproduzível.
3. **P1 — Integração da fase 2 do Warden.** `BossPhaseController` usa Visual/Body oculto, enquanto o modelo está em Warden_Model. Corrigir ponte/composição e observar transição única, 3 s e persistência da cor.
4. **P1 — Renderização/recursos obsoletos.** O checker de texto encontra quatro GUIDs órfãos de componentes de teste no DefaultVolumeProfile; a API carrega 19 componentes válidos. PC_Renderer contém sete referências não resolvidas de debug de probe volumes. O executável registra shaders de DoF/Panini indisponíveis. Não confundir tudo com os dois avisos de armas; rastrear separadamente antes de reparar recursos que o jogo utiliza.
5. **P1 — Builders compartilhados.** M1 e Floor reconstroem o mesmo Enemy_Melee/Player. Reexecutar M1 não pode apagar recompensa/composição do M4. Validar idempotência com snapshots/Editor APIs antes de aceitar.
6. **P1 — Pool frio.** ObjectPool.defaultCapacity aloca armazenamento, não instâncias. Aquecer explicitamente e provar contagens/reuso sob rajada, mantendo limites configuráveis.
7. **P1 — Ausência de prova integrada de salas/Boss.** Adicionar testes que atravessam a composição real e depois playtest, sem teleportes/invulnerabilidade como evidência de vitória normal.
8. **P2 — Aviso ao desativar player animado.** Observado `Game object with animator is inactive` na saída/reinício. Verificar guards da ponte ao cancelar; nenhum erro de compilação foi observado no runtime inicial.
9. **P2 — Limites e ciclo de vida.** CameraOcclusionHider usa buffer fixo de 16; RoomController perde inscrições ao reativar; EnemyCombat não cancela explicitamente no OnDisable; lista estática de drops pode sofrer reentrância. São riscos de código, ainda sem reprodução de falha no percurso padrão; não chamar correção de ganho de desempenho sem medir.

## Além dos marcos originais

Mana/estamina, defesa, especial, menus iniciais/pause/configurações, áudio, inventário interativo, raridades e ataque ranged do jogador não estão implementados. O ranged existente é do inimigo. A prioridade é fechar M0–M4 antes de ampliar esses sistemas. [ROADMAP](../ROADMAP.md) propõe a fronteira de 1.0; [BACKLOG](../BACKLOG.md) controla execução e aprovação de produto.

## Regra de atualização

O quadro acima guarda a classificação da auditoria inicial. Cada entrega abaixo acrescenta correção, evidência nova e estado resultante; MVP.md resume o estado corrente, sem apagar o histórico.

| Entrega | Resultado e evidência |
| --- | --- |
| AUD-001 | Auditoria inicial, snapshot de assets, 45 testes EditMode e 21 anteriores + 6 novos testes PlayMode aprovados após corrigir fixtures. Nenhuma nova funcionalidade de gameplay foi adicionada. |
