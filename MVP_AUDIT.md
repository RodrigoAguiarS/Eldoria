# Auditoria do MVP — 13/11/2026

## Resultado

**Nem todas as etapas estão concluídas.** M1 e M2 têm os sistemas previstos; M3 tem a lógica principal, com uma lacuna visual na integração dos personagens gerados. M0 está sem Git e M4 permanece parcial. Nenhum aceite completo foi certificado apenas pela leitura do código ou pelos testes de componentes.

Escopo: comparar os 26 itens de `MVP.md` com `AGENTS.md`, `GAME_DESIGN.md`, `ARCHITECTURE.md`, scripts, configurações, assets, checklists e testes disponíveis. Esta auditoria não corrige gameplay nem altera cenas, prefabs, assets de jogo ou ProjectSettings.

Legenda: **presente** = implementação encontrada; **parcial** = parte do requisito falta ou está quebrada. A coluna de validação registra os limites da evidência, mesmo quando a implementação está presente.

## Ambiente e execução

| Componente | Versão/configuração verificada |
| --- | --- |
| Unity | 6000.6.9f1, revisão 8c45277fb9c5 |
| URP | 17.6.0 |
| Input System | 1.20.1; `activeInputHandler: 1` (somente New Input System) |
| Cinemachine | 6.6.0 |
| AI Navigation | 2.0.16 |
| Test Framework | 1.8.0, builtin, resolvido em `Packages/packages-lock.json` |
| Serialização | Force Text; Visible Meta Files |
| Git | `git status --short` retorna `fatal: not a git repository` |

Test Runner executado no Editor real, sem falhas:

| Suíte | Resultado | Evidência |
| --- | --- | --- |
| EditMode | 32 passaram; 0 falharam | [EditMode.xml](Reports/MVP/EditMode.xml), início 21:05:38 UTC / 18:05:38 local |
| PlayMode | 5 passaram; 0 falharam | [PlayMode.xml](Reports/MVP/PlayMode.xml), início 21:06:05 UTC / 18:06:05 local |

EditMode: Boss (6), lógica de dungeon (4), seleção de cenas de build (3), fundação (2), HP/cooldown (6), progressão/loot (11).

PlayMode cobre somente: invulnerabilidade de `Health`, evento único de morte, abrir/fechar collider de porta, reutilização de projétil e telegraph com mínimo de 1 segundo. Não cobre automaticamente movimento, mira, ataque integrado, dash integrado, reinício, navegação nas cinco salas, luta completa do Boss ou coleta das armas serializadas.

Na etapa anterior desta mesma sessão, a cena `Floor_01_GeneratedCharacters` foi aberta e executada em PlayMode; o builder de Floor foi executado duas vezes, e um build Windows foi gerado e aberto com os personagens 3D. Foi observado progresso até 3/5 salas e nível 3 no executável. Isso não comprova vitória, morte/reinício nem o fechamento de todos os aceites. A evidência anterior de idempotência está em `Tools/Lemo3DGenerator/reports/floor_build_idempotence.json`.

## M0 — Fundação

| Item | Implementação | Evidência e validação pendente |
| --- | --- | --- |
| Criar projeto Unity 6 URP e repositório Git | **Parcial** | Projeto abre no Editor e as suítes passam; URP configurado. Não há repositório Git nesta pasta nem nos diretórios ancestrais encontrados pelo Git. Falta o commit inicial reproduzível. |
| Confirmar versões dos pacotes | **Presente** | Versões conferidas em `ProjectVersion.txt`, manifest e lock; testes de `FoundationValidator` passaram. |
| Pastas, documentos, `.gitignore` e `.meta` | **Presente** | Estrutura e documentos existem; `.gitignore` inclui caches/builds; Visible Meta Files configurado. Teste `RequiredFolders_Exist` passou. Versionamento efetivo dos `.meta` depende de criar o Git. |
| Bootstrap simples e PlayMode sem erros | **Presente; validação incompleta** | `BootstrapSceneBuilder.cs`, `BootstrapDiagnostics.cs` e `Assets/_Project/Scenes/Bootstrap.unity` existem. A Bootstrap não foi aberta e executada especificamente nesta auditoria; o PlayMode observado da Floor não substitui esse smoke test. |

**Aceite M0: pendente.** Bloqueio concreto: ausência do repositório/commit. Falta registrar o smoke test da Bootstrap.

## M1 — Arena jogável

Os scripts citados abaixo ficam em `Assets/_Project/Scripts/`, salvo indicação diferente.

| Item | Implementação | Evidência e validação pendente |
| --- | --- | --- |
| Arena, chão, paredes, luz e Cinemachine a ~45° | **Presente** | `Editor/DungeonMvpSceneBuilder.cs` e cena `MVP_Arena.unity`; câmera com rotação de 45°. Cena gerada da Floor vista em execução; aceite específico da arena não repetido nesta auditoria. |
| CharacterController e WASD relativo à tela | **Presente** | `Player/PlayerMovement.cs` projeta vetores da câmera no XZ e chama `CharacterController.Move`; `PlayerInputReader.cs` usa New Input System. Sem teste automatizado de deslocamento integrado. |
| Mira por mouse e rotação XZ | **Presente** | `Player/PlayerAim.cs` lança raio da câmera contra plano horizontal e gira em Y, conforme fallback permitido na arquitetura. Falta checklist manual completo de mira. |
| Ataque com alcance, dano e cooldown | **Presente** | `Player/PlayerCombat.cs`: overlap sem alocação, cone/alcance, janela de acerto e cooldown. HP/cooldown testados; ataque completo via input não automatizado. |
| Dash com cooldown e invulnerabilidade | **Presente** | `Player/PlayerDash.cs` e `Core/Health.cs`. Invulnerabilidade testada em PlayMode; deslocamento e cooldown do dash juntos não testados. |
| Melee com NavMeshAgent e quatro estados | **Presente** | `Enemies/EnemyBrain.cs`, `EnemyNavigation.cs`, `EnemyCombat.cs`, `EnemyData.cs`; decisões temporizadas. Navegação completa não coberta pelas suítes. |
| HP, barras e feedback | **Presente** | `Core/Health.cs`, `HealthModel.cs`, `DamageFlash.cs`, `UI/HealthBarPresenter.cs`, `Player/PlayerHitShake.cs`; HUD visto em execução. HP e morte têm testes. |
| Morte e reinício | **Presente** | `Player/PlayerController.cs`, `Systems/GameRestarter.cs`, `UI/GameOverPresenter.cs`. Evento de morte testado; morrer e reiniciar a cena pelo fluxo real ainda precisa validação. |
| Builder idempotente via menu | **Presente** | `Editor/DungeonMvpSceneBuilder.cs`, `EditorBuildUtility.cs` e `DungeonFloorSceneBuilder.cs`. O builder da Floor foi reexecutado duas vezes nesta sessão; seleção da cena de build tem 3 testes. Isso não testa todos os objetos da arena M1. |
| Testes e checklist | **Presente** | Suítes em `Assets/_Project/Tests/{EditMode,PlayMode}` executadas; `M1_CHECKLIST.md` existe. Faltam testes integrados de input/combate/reinício e concluir o checklist manual. |

**Aceite M1: pendente de confirmação integral.** A implementação existe; os 37 testes não demonstram toda a sequência mover → mirar → atacar → matar → receber dano → morrer → reiniciar sem exceções.

## M2 — Loop de salas

| Item | Implementação | Evidência e validação pendente |
| --- | --- | --- |
| Prefabs Room, portas e spawn points | **Presente** | `Editor/DungeonFloorSceneBuilder.cs`, prefabs em `Assets/_Project/Prefabs/Rooms`, `Dungeon/RoomController.cs`, `DoorController.cs`, `EnemySpawner.cs`. |
| Fechar ao entrar, spawnar, reabrir após último morto | **Presente** | `RoomTrigger.cs`, `RoomController.cs`, `EnemyTracker.cs`; acompanhamento por eventos/referências. Testes de tracker e porta passaram; ciclo completo da sala não está automatizado. |
| Ranged e elite | **Presente** | `Enemies/EnemyBrain.cs`, configurações/prefabs próprios e `Systems/ProjectilePool.cs`. Os modelos gerados correspondentes foram vistos no executável nesta sessão. Falta validar sistematicamente todos os estados e ataques. |
| Quatro salas comuns em sequência fixa | **Presente** | Builder e Floor têm `Room_1` até `Room_4`, seguidas por `Room_5_Boss`; `FloorController.cs` acompanha progresso. Observação de 3/5 salas não certifica as quatro completas. |

**Aceite M2: pendente.** Falta registrar uma passagem completa pelas quatro salas, portas abrindo somente após limpeza e ausência de softlock.

## M3 — Boss e andar

| Item | Implementação | Evidência e validação pendente |
| --- | --- | --- |
| Sala final e Boss em duas fases | **Presente** | `Room_5_Boss`, `Boss/BossBrain.cs`, `BossPhaseModel.cs`, `BossPhaseController.cs`, BossData/prefab. Modelos de fase têm testes; luta real completa não validada. |
| 50% HP, invulnerabilidade de 3 s, mudança de padrão | **Presente na lógica; integração visual parcial** | Modelo dispara transição única; controller aplica invulnerabilidade e BossBrain usa parâmetros da fase 2. Mudança visual exigida pelo GAME_DESIGN não alcança corretamente o modelo gerado; detalhes abaixo. |
| Telegraph visível por pelo menos 1 s | **Presente** | `Boss/BossAttackTelegraph.cs` e integração em BossBrain. Teste PlayMode confirma duração mínima e preenchimento; legibilidade durante a luta real ainda pendente. |
| Barra de Boss durante luta e tela de vitória | **Presente** | `UI/BossHealthPresenter.cs`, `VictoryPresenter.cs`, eventos de sala/andar. Visibilidade e vitória não comprovadas por uma partida completa nesta auditoria. |

**Lacuna da arte gerada:** o builder original liga `BossPhaseController.flash` ao renderer `Visual/Body` e `visual` ao objeto `Visual` (`DungeonFloorSceneBuilder.cs`, linhas 479–514). `GeneratedGameTestBuilder.InstallModel` desliga renderers Body/Horn e instala `Warden_Model` diretamente sob a raiz (linhas 269–281). A cor de fase 2 e o pulso de escala continuam aplicados ao placeholder oculto, enquanto `GeneratedBossAnimationBridge` reproduz a animação Roar. O modelo visível não recebe a mudança de cor/escala prevista. É diagnóstico da composição no código; a transição não foi observada visualmente nesta auditoria. O rugido sonoro também depende do áudio ainda ausente.

**Aceite M3: pendente.** Falta completar as cinco salas, observar transição única aos 50%, seus 3 segundos de invulnerabilidade, novo padrão, mudança visual e vitória. Os testes do modelo de fases não substituem esse percurso.

## M4 — Progressão e acabamento

| Item | Implementação | Evidência e validação pendente |
| --- | --- | --- |
| XP, níveis, cura e +10% de dano | **Presente** | `Systems/ExperienceModel.cs`, `PlayerProgression.cs`, `Enemies/EnemyReward.cs`, `UI/ProgressionPresenter.cs`. Multiplicador aditivo `1 + 0,1 × (nível − 1)`; cura configurada em 40% da vida máxima por nível ganho. 11 testes de progressão/loot passaram; nível 3 observado no executável. Cura e dano do fluxo completo ainda precisam confirmação. |
| Ouro, poção e arma por ScriptableObjects | **Parcial; armas com referência inválida** | Tabelas, pickups e coleta existem em `LootTable.cs`, `Pickup.cs`, `PlayerProgression.cs`. Entretanto, os dois assets Weapon têm `m_Script: {fileID: 0}`. Não é possível aceitar o loot de armas como integrado e válido. |
| Pool de projéteis e efeitos repetidos | **Parcial** | `ProjectilePool.cs` usa `ObjectPool<Projectile>`; teste comprova reutilização. Telegraph/flash reutilizam objetos existentes, mas não foi localizada implementação de pool para efeitos repetidos; o próprio `M4_CHECKLIST.md` deixa esse acabamento pendente. Pickups são instanciados/destruídos, uma observação de desempenho separada, sem presumir que sejam projéteis. |
| Áudio, animações, VFX, acessibilidade e balanceamento | **Parcial** | Animações dos personagens gerados e feedback básico (flash, shake, telegraph) existem. Não localizados clips de áudio em Assets nem uso de AudioSource/AudioClip nos scripts de jogo auditados; AudioListener sozinho não produz áudio. Não localizados ajustes de acessibilidade expostos ao jogador. Balanceamento final e medição de desempenho continuam pendentes. |

**Armas:** `WeaponData` é uma classe ScriptableObject declarada dentro de `Assets/_Project/Scripts/Systems/LootTable.cs`, sem arquivo `WeaponData.cs` correspondente. Os assets `Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset` e `Weapon_WardenBlade.asset` têm script nulo (linha 12). As tabelas de elite/Boss referenciam essas armas; `LootRoller` ignora arma nula. Os testes usam objetos criados em memória e não garantem que esses assets carreguem no build. Correção recomendada: separar WeaponData em arquivo próprio, reparar os assets via Editor preservando GUIDs e testar importação/coleta/equipamento no build.

**Runtime:** o `Player.log` do build Windows observado nesta sessão registra duas mensagens `The referenced script on this Behaviour (Game Object '<null>') is missing!`. Trechos preservados em [RuntimeWarnings.txt](Reports/MVP/RuntimeWarnings.txt). Os assets de arma são referências inválidas confirmadas, mas a associação exata entre cada aviso e cada asset ainda precisa rastreamento no Editor. O Console após os testes estava sem warnings/erros; isso não elimina os avisos do executável.

**Aceite M4: não atendido.** Há referências inválidas, acabamento incompleto e ausência de medição registrada de FPS/frame time, CPU/GPU e memória no build Windows.

## Ordem recomendada para concluir

1. Reparar WeaponData/assets no Editor, localizar e eliminar os Missing Script; acrescentar teste que carregue os assets reais.
2. Criar o repositório Git e commit inicial com `.meta`, Packages e ProjectSettings para tornar o estado reproduzível.
3. Integrar feedback visual da fase 2 ao Warden gerado.
4. Registrar smoke test da Bootstrap e checklist completo de movimento/combate/dash/morte/reinício; completar quatro salas e o Boss no Editor e no executável.
5. Finalizar áudio, acessibilidade, VFX/pooling de efeitos aplicáveis e balanceamento; medir desempenho em build Windows e registrar máquina/configurações/resultados. O MVP não define um alvo numérico de FPS, portanto esta auditoria não inventa esse critério.

Os arquivos `M1_CHECKLIST.md` a `M4_CHECKLIST.md` contêm notas históricas e caixas pendentes, inclusive mensagens de “não validado no Editor” anteriores aos testes acima. Foram preservados para não substituir histórico por uma aprovação que não ocorreu. Esta auditoria registra a evidência atual.

## Arquivos desta auditoria

- `MVP.md`: resumo datado e link para esta auditoria; requisitos e caixas originais preservados.
- `MVP_AUDIT.md`: diagnóstico dos 26 itens, critérios ainda abertos e próximos passos.
- `Reports/MVP/EditMode.xml` e `Reports/MVP/PlayMode.xml`: resultados reais do Test Runner.
- `Reports/MVP/RuntimeWarnings.txt`: somente os dois avisos de Missing Script do Player.log, sem copiar o log completo.

Nenhum script de runtime, cena, prefab ou configuração de jogo foi alterado para produzir este diagnóstico.
