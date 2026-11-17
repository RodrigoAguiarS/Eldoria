# ARCHITECTURE.md — Eldoria (Unity)

## Ambiente
Unity **6000.6.9f1**, URP **17.6.0**, PC Windows x64. Manifest/lock versionados: Input System 1.20.1, Cinemachine 6.6.0, AI Navigation 2.0.16, UGUI 2.6.0 e Test Framework 1.8.0 (builtin no lock). Preservar essas versões; consultar instalação/Package Manager antes de mudanças. Não aplicar APIs de Cinemachine 2/3 presumindo compatibilidade.

## Pastas
`Assets/_Project/{Art,Audio,Animations,Materials,Input,Prefabs/{Player,Enemies,Boss,Rooms},Scenes,ScriptableObjects,Scripts/{Core,Player,Enemies,Boss,Dungeon,Systems,UI,Editor},Settings,Tests/{EditMode,PlayMode}}`.

## Namespaces e componentes
- `DungeonCrawler.Core`: `IDamageable`, eventos e contratos comuns.
- `DungeonCrawler.Player`: `PlayerMovement`, `PlayerAim`, `PlayerCombat`, `PlayerDash`, `PlayerSpecial`; vida compartilhada em `DungeonCrawler.Core.Health`/`HealthModel`.
- `DungeonCrawler.Enemies`: `EnemyBrain`, `EnemyNavigation`, `EnemyCombat`, `EnemyData`, `EnemyReward`; usa `Core.Health`.
- `DungeonCrawler.Boss`: `BossBrain`, `BossPhaseModel`, `BossPhaseController`, `BossAttackTelegraph` (implementados).
- `DungeonCrawler.Dungeon`: `RoomController`, `DoorController`, `EnemySpawner`, `FloorController`, `EnemyTracker` (implementados).
- `DungeonCrawler.Systems`: `ExperienceModel`, `PlayerProgression`, `LootTable`/`LootRoller`, `ProjectilePool`, inventário e opções (implementados).
- `DungeonCrawler.UI`: `HealthBarPresenter`, `BossHealthPresenter`, presenters de inventário/menus/recurso.
- `DungeonCrawler.Editor`: `DungeonMvpSceneBuilder`.

## Dependências
- Composição por referências serializadas, componentes irmãos (`GetComponent` local em Awake quando necessário) e inicialização explícita.
- Eventos `Action` para alterações de HP, morte, conclusão de sala e nível; evitar eventos estáticos globais quando o ciclo de vida puder causar assinaturas obsoletas.
- UI escuta estado e eventos, não decide dano ou mortes.
- Interfaces definem contratos (`IDamageable` recebe dano e expõe estado; `IPoolable` recebe callbacks de ativação/desativação).
- `ScriptableObject` define configurações; instâncias de runtime guardam HP/cooldown e não alteram o asset compartilhado.

## Input e movimento
Asset `.inputactions` com mapa `Gameplay`: `Move` (Vector2 WASD), `Point` (mouse position), `Attack` (left click), `Dash` (space), `Special` (right click/E). Utilizar callbacks ou leitura de `InputActionReference`; nunca Input legado. Projetar forward/right da câmera no plano XZ e normalizar. Mira: raio da câmera para chão/collider, fallback a plano horizontal quando apropriado.

F04: cada cena oficial compõe uma única `RuntimeInputBindings`, explicitamente referenciada por PlayerInputReader, PlayerInventoryInput, GameRestarter e menus. `InputBindingSet` clona o template e acrescenta `Shortcuts` (Inventory/UsePotion/Pause/Restart); remove somente da cópia o Restart de Gameplay, anteriormente sem consumidor. O leitor habilita/suspende Gameplay; os demais donos habilitam suas ações de Shortcuts, preservando pausa e reinício após morte. Atores isolados antigos podem criar um set privado; cenas e prefab oficiais têm referências explícitas. O asset compartilhado e o mapa UI não recebem overrides.

Quatorze slots estáveis (`Move/up`, `Dash/0`, etc., com `Interact/0` acrescentado ao fim no F08) formam o schema v1 de `DungeonCrawler.Bindings.v1` no PlayerPrefs. A carga valida o documento inteiro antes de aplicar: versão desconhecida, ID repetido/desconhecido, caminho inválido e colisão entre slots rejeitam a carga sem apagar a preferência. Somente teclas físicas canônicas e cinco botões de mouse são aceitos; agregadores/curingas/eixos/gamepad são recusados. Escape é reservado, com fallback imutável na ação Pause; Enter/numpadEnter ficam na UI. Conflitos não são trocas automáticas. Reset remove overrides e salva defaults; não toca opções gerais nem inventário/progressão.

`RuntimeInputBindings` controla `RebindingOperation`, timeout não escalado, cancelamento por Escape/botão/perda de foco/desativação, suspensão dos donos e retomada após um frame neutro. O clique em Cancelar/Voltar não pode virar binding. `GameMenuController` informa o contexto: poção disponível no gameplay/bolsa; reinício por atalho suspenso nos overlays, com botões ainda funcionais. Raycast por EventSystem explicitamente injetado filtra cliques de UI; PlayerInputReader exige soltar botões ao retomar a pausa para evitar ataque pelo clique de Continuar. `BindingHintPresenter` e os presenters de recurso/avisos consultam as ações efetivas. `ControlBindingsSceneBuilder.Integrate` integra prefab e quatro cenas idempotentemente, sem reconstruir arte, salas, dados ou `.inputactions`.

## Câmera
Cinemachine com follow e offset isométrico; confirmar API conforme versão instalada. Configurar smoothing e orientação fixa; tremor somente via componente/efeito compatível, não manipular transform da câmera principal de forma conflitante.

## Física, dano e IA
Player: CharacterController e gravidade explícita se necessário. Ataque inicial melee por overlap/hitbox com máscara de inimigos e janela de acerto controlada. Inimigo: NavMeshAgent, decisões temporizadas, dano com cooldown, parar ataque ao morrer. Evitar alocação por frame e buscas de cena repetidas. NavMesh bake compatível com AI Navigation.

Portas: a layer Door fica fora do bake (Ground|Obstacle), então o vão existe no NavMesh. Cada `DoorController` referencia um `NavMeshObstacle` Box com carving na raiz estática da porta (volume = collider do bloqueio fechado) e o liga/desliga junto com o collider em `Open`/`Close`/`SetOpenInstant`; fechada, nenhum `NavMeshAgent` usa o vão; aberta, não carva. `DoorNavMeshBlockerBuilder.Integrate` (menu `Tools/Dungeon Crawler/Dungeon/...`) aplica isso aos prefabs `Room_*` de forma idempotente (as cenas herdam pelo prefab) e `DungeonFloorSceneBuilder.CreateDoor` mantém o mesmo ao reconstruir. Evidências em [Reports/DoorNavMesh](Reports/DoorNavMesh/README.md).

F07: `EnemyCombat.HasLineOfSight` compartilha cache por alvo com relógio escalado (`EnemyData.lineOfSightInterval`, 0,1 s no ranged; mínimo 0,05 s). `Physics.Linecast` usa máscara explícita Obstacle/Door e ignora triggers; atores ficam fora da máscara. Origem é exatamente `ProjectileOrigin` (posição da raiz + `projectileHeight`), também usada pelo pool; mira usa a posição atual do alvo + a mesma altura. Início e impacto fazem consulta fresca, além das amostras temporizadas durante windup; bloqueio amostrado cancela definitivamente aquele ataque. `EnemyBrain` verifica visão antes de Attack/IsBusy e persegue cobertura somente no thinkInterval existente; `EnemyNavigation.MoveTo` reduz a distância de parada para o raio do agente durante aproximação ranged, evitando parar atrás da parede no alcance de ataque. Não promete achar posição de tiro para alvos inalcançáveis. OnDisable de combate/brain cancela pendências; brain também para a navegação e reinicia em Idle ao reativar. Cooldown iniciado não é reembolsado. `RangedLineOfSightBuilder.Integrate` migra só os campos de visão do asset compartilhado, preservando GUIDs/referências e balanceamento.

## Recompensa experimental por sala (F08)

`RoomRewardDefinition` referencia uma `LootTable` exclusiva e configura alcance/espaçamento. `RoomRewardChest` recebe referências serializadas de `RoomController`, Health/PlayerInputReader, definição, prefab de Pickup e origem dos drops. Consulta o estado atual da sala também após reativação, valida vida/alcance/pausa/habilitação e confirma `IsOpened` antes de sortear/instanciar/notificar; nenhuma reentrada ou toggle reseta o estado. Runtime pertence à instância da cena, nunca ao SO. Pickups são raízes na mesma cena da run, independentes da hierarquia do baú; `Pickup.Setup(..., persistUntilCollected: true)` desliga expiração só para essa fonte. Recusas continuam no fluxo normal de retry, com identidade/quantidade preservadas.

`PlayerInputReader.InteractPressed` solicita abertura pelo domínio; `RoomRewardPresenter` só apresenta bloqueio/proximidade, binding efetivo e tampa. Usa primitivas sem colisão para não mudar cobertura/NavMesh. O builder `RoomRewardSceneBuilder.Integrate` mantém um baú em cada uma das cinco salas de release pelas APIs do Editor, preserva tabelas antigas e usa o remapeamento F04 já integrado. Cada presenter tem aviso próprio, evitando sobrescrita pelos baús distantes. VictoryPresenter.ContinueExploring fecha a apresentação de vitória e GameMenuController.LeaveTerminal libera a bolsa/pausa sem alterar FloorController.IsCompleted. A composição chama ProductionAnimationIntegrator.RepairScene para restaurar o vínculo das chamas às tochas, sem modificar posição/intensidade das luzes nem gravar relatórios históricos. F08 acrescenta `Interact/0` ao fim dos treze slots F04, sem renumerar IDs/índices anteriores nem trocar a chave/schema v1. Carga de documento anterior que já ocupa F mantém o binding antigo e atribui uma tecla livre à interação; colisões explícitas novas continuam recusadas. O asset `.inputactions` compartilhado permanece intacto, e a ação nasce na cópia runtime.

## Props destrutíveis (F09)

`DestructiblePropDefinition` guarda somente HP máximo; `DestructibleProp`
implementa `IDamageable` com HP por instância e eventos `Damaged`/`Broken`.
Não usa Health de inimigo, EnemyTracker ou loot. Confirma HP/estado terminal
antes de notificar e recusa reentrada durante a transação; toggle não reseta.
Recusa dano inválido, pausa e componente/objeto desativado. Morte do jogador
continua cancelando os ataques pelos contratos existentes.
`DestructiblePropPresenter` assina no ciclo de vida, reconcilia o estado ao
reativar e reutiliza DamageFlash e visuais intacto/destroços pré-compostos.

`DestructiblePropSceneBuilder.Integrate` cria a layer Destructible em slot livre,
sem colisão física com outras layers. Os colliders não são triggers: básico e
giro consultam `enemyMask | destructibleMask`, preservando máscaras de inimigo,
deduplicação por IDamageable e expansão do buffer antes do dano. Visão/projéteis
e NavMesh não incluem a layer. Integra máscaras no prefab/jogadores existentes
e um vaso lateral por sala de release, sem reconstruir salas, luzes ou bake.
Reexecutar preserva GUIDs, definição e posição já existentes. O Editor 6000.6
resserializa DynamicsManager para schema 24 ao persistir a matriz; os pares
anteriores entre outras layers devem permanecer iguais.

## Pooling
Projéteis usam `UnityEngine.Pool.ObjectPool<Projectile>`, aquecimento real de 16 instâncias na configuração atual e callback de retorno reutilizado; colisão, timeout e desativação devolvem ao pool. Pickups/atores ainda são criados por recompensa/onda; isso não prova gargalo e não autoriza pooling indiscriminado.

## Automação de cena
Editor script idempotente com `MenuItem("Tools/Dungeon Crawler/Build/MVP Scene (M1)")`: criar/atualizar arena, luz, player, inimigo, câmera, HUD e salvar em `Assets/_Project/Scenes/MVP_Arena.unity`. Garantir que reexecutar não duplique objetos. Prefabs devem ser salvos por APIs do Editor, não por texto manual.

## Testes
EditMode: cálculos de dano, clamp HP, cooldown, transição de estado. PlayMode: movimentação, dano, morte, ataque, dash e reinício quando viável. Relatar explicitamente dependências de teste manual (mouse, câmera, NavMesh).

## Git
Ignorar `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, `UserSettings/`, `.vs/` e IDE caches. Versionar `.meta`, `Assets`, `Packages`, `ProjectSettings` e documentação. Unity Asset Serialization: Force Text; Version Control: Visible Meta Files, quando disponível na versão do Editor.

## Inventário e equipamento (1.0 / D-002)

ItemDefinition/WeaponData/ArmorData/PotionData e ItemRarityDefinition são dados compartilhados; raridade não aplica multiplicador implícito. InventoryDefinition configura capacidade e a poção usada pelos drops. InventoryModel mantém slots fixos, pilhas por potência e índices de equipamento, com adição atômica; não controla HP ou UI.

PlayerInventory valida comandos com Health e aplica equipamento via PlayerProgression/Health. PlayerProgression preserva fórmula de dano e XP/cura de nível; jogadores sem inventário mantêm compatibilidade com o MVP anterior. Pickup só se remove após TryCollect retornar sucesso; rejeição preserva o item e permite retry controlado. Largar devolve uma unidade ao mundo, verifica espaço por raycasts e mantém o item na bolsa se não houver posição válida.

InventoryPresenter/SlotView apresentam estado e solicitam comandos; assinaturas acompanham OnEnable/OnDisable. Builders configuram os três cenários de gameplay e o prefab compartilhado pelas APIs do Editor, sem regenerar arte ou salas.

F01: `PlayerInventory.TryGetEquipmentComparison` consulta o equipamento sem mutar slots ou emitir eventos; poção/slot vazio não têm comparação. O snapshot separa bônus plano da arma do dano base no nível atual, antes dos multiplicadores de combo/especial. `PlayerProgression.GetBaseDamage` usa o mesmo cálculo de `PlayerCombat.CurrentDamage`; defesa usa a mesma composição de baseline e armadura aplicada ao Health. O presenter só formata atual → selecionado e diferença. `InventorySceneBuilder.IntegrateEquipmentComparison` atualiza somente a área correspondente nas três cenas existentes, preservando comandos e referências.

F02: `InventoryModel.TrySort` reordena pilhas inteiras por tipo (arma, armadura, poção), rank de raridade decrescente (raridade ausente abaixo de Comum) e nome sem diferenciar maiúsculas; empate mantém a ordem original e vazios ficam no fim. Não funde, divide nem altera pilhas, capacidade ou auto-equip. Os índices de arma/armadura seguem a instância movida (duplicatas da mesma definição continuam distintas), com um único `Changed`; bolsa já ordenada não publica evento. `newIndexOf[antigo]` permite ao presenter manter a seleção no mesmo item. `PlayerInventory.TrySort` recusa comando com jogador morto. Botão Ordenar no cabeçalho criado por `InventorySceneBuilder.IntegrateInventorySort`/`ConfigureInventorySort`, também chamado pela integração geral. Na política Primeira (padrão), Q usa a primeira poção na ordem atual dos slots; F03 acrescenta as alternativas abaixo.

F03: `InventoryModel.GetPotionIndex(QuickPotionPolicy)` seleciona por ordem/potência, com empate pelo índice, sem mutação. `PlayerInventory.QuickPotionIndex` consulta a opção compartilhada por referência explícita e `TryUseQuickPotion` usa o mesmo índice, preservando validação de HP e vida; `TryUsePotion(index)` continua independente. Opções aplicadas notificam o inventário/HUD com assinaturas no ciclo de vida. JSON v3 migra v1/v2 para Primeira (v1 também migra qualidade); política ausente/inválida usa Primeira e versão ausente/inválida/futura rejeita o documento e usa defaults. A chave existente é preservada e não há migração de progressão. `GameShellSceneBuilder.IntegrateQuickPotionPolicy` integra o seletor nas quatro cenas e a referência no prefab sem reconstruir salas/arte/dados.

## Pausa e atalhos (1.0 / D-002)

GameplayPauseController suspende o mapa Gameplay, cancela controles explicitamente referenciados e congela Time.timeScale. Ao sair, restaura apenas controles previamente habilitados e jogador vivo; destruir/desativar o controlador restaura o tempo. Timers de combate/IA/spawn/projéteis respeitam a pausa, inclusive na janela de impacto. PlayerInventoryInput usa as ações de Shortcuts da fonte runtime (padrões I/Tab/Q/Esc), disponíveis conforme contexto durante o menu e suspensas na captura. GameMenuController apresenta os painéis e solicita pausa; não decide dano, cura ou progressão.

## Especial e estamina (1.0 / D-002)

PlayerSpecialDefinition configura recurso, custo, regeneração, cooldown e movimento. PlayerSpecialModel usa relógio escalado e emite início/impacto/fim/cancelamento; custo e cooldown não são devolvidos ao cancelar. PlayerSpecial aplica um hit radial autoritativo, deduplica IDamageable e amplia o buffer antes de causar dano quando saturado. Morte, dash, desativação e pausa impedem impacto pendente; básico e especial respeitam a recuperação um do outro. E/direito não repetem ao segurar. A ponte do Hero representa os mesmos eventos C#, alinhando a pose de impacto sem AnimationEvent de dano; o rig continua Generic/in-place.

## Menus e opções (1.0 / D-002)

MainMenu e o andar são destinos explícitos de GameSceneFlow; iniciar/reiniciar cria uma run nova. MenuScreenStack oculta a tela anterior e restaura o foco ao voltar, mantendo o menu superior como superfície interativa. GameMenuController solicita pausa ao domínio; telas de vitória/derrota encerram overlays e selecionam o botão de reinício. GameShellSceneBuilder configura telas/referências pelas APIs e coloca MainMenu antes do andar no build, preservando as demais cenas de teste no Editor. `GameIdentity` (Editor) concentra o nome do produto (D-003): `Eldoria` em PlayerSettings, `Eldoria.exe`/`Eldoria_Data` e título do MainMenu; `MvpReleaseBuild` reaplica o nome a cada build e `GameIdentity.Apply` atualiza só o título salvo, de forma idempotente.

GameOptionsDefinition compartilha defaults serializados e estado de sessão não serializado; os ajustes ficam em JSON versionado no PlayerPrefs por usuário, separados da progressão. Dados inválidos/versões futuras não são aplicados; valores são limitados antes de uso. GameOptionsPresenter edita uma cópia e só persiste ao aplicar. GameOptionsApplier usa as APIs de qualidade/tela no player, preservando o seletor de resolução do Game view no Editor. Referências explícitas de opções ajustam flash e impulso de câmera, incluindo o rugido do Boss; a cor persistente da fase 2 permanece legível com flash desligado.

Apenas PC está incluído nos níveis nativos do build Windows; por isso as escolhas Leve/Equilibrada/Alta usam presets de escala/sombras no SO, além de Padrão. GameOptionsApplier altera uma cópia temporária do pipeline URP e restaura a referência ao sair, sem editar o asset do projeto. Opções v1 migram para v2 preservando volumes/legibilidade/tela e trocando o índice nativo dependente da plataforma por Padrão.

F10: opções gerais usam JSON **v4**, mantendo `DungeonCrawler.Options.v1`.
`textScalePercent` aceita 100/125/150, com fallback 100; v1–v3 migram para 100
sem descartar as demais migrações. `GameOptionsPresenter` mantém o rascunho,
inclusive ao restaurar padrões; somente Aplicar persiste e notifica.
`UiTextScale` recebe opções e baselines de Text explicitamente serializadas
pelo builder, assina/desassina no ciclo de vida e aplica tamanho absoluto,
sem acumulação ou alteração de transform/targets. Templates de Dropdown
herdam a fonte atual e usam linhas de 64 unidades.

`TextScaleSceneBuilder` integra as quatro cenas sem reconstruir domínio/arte.
Janelas reservam espaço para 150%, slots usam GridLayoutGroup; a coluna de
equipamento/detalhes usa ScrollRect, VerticalLayoutGroup e ContentSizeFitter,
com barra de rolagem focável. Selecionar outro slot retorna ao topo. Os nomes
completos ficam nos detalhes; resumo visual não altera dados. O CanvasScaler
reserva a referência 1920×1080 e expande o espaço disponível. A preferência
altera somente fontes; HUD usa containers e âncoras próprios. Builders de
shell/controles/inventário/avisos reaplicam a composição, preservando baselines.
Faixa inicial limitada a 150%; não implica suporte gamepad completo, medição
de contraste ou cobertura de monitores físicos ultrawide.

## Áudio e diagnóstico (1.0)

GameAudioDefinition contém clips e roteamento Music/SFX. GameAudioPool reutiliza oito vozes pré-configuradas, limita prioridade/repetição e aplica ganhos das categorias em dB; mute em zero é explícito. GameplayAudioBridge e EnemyAudioBridge escutam eventos C# com inscrições no ciclo de vida. EnemySpawner injeta o pool nos atores instanciados; progressão emite Collected apenas após coleta aceita. Variação sonora não altera o gerador aleatório do loot.

Sons e loop originais são reproduzíveis em Tools/AudioGenerator. Clips são PCM/precarregados; novas fontes não são criadas durante golpes. Diagnóstico opcional no player registra contadores e pode capturar o DSP do próprio jogo; sem flags, coletores/cenário ficam desativados e o filtro de captura não é instanciado. Limites, flags e fontes das APIs em Tools/AudioGenerator/DIAGNOSTICS.md.

## Apresentação de itens e combate (pós-release)

`ItemVisualCatalog` mapeia definição/LootKind para ícone, prefab de chão e equipamento. `PickupVisualPresenter`, `PlayerEquipmentVisualPresenter` e presenters UGUI só representam estado. Espadas/colete são pré-configurados no Hero; troca visual não muda bônus. `PlayerAttackVfxPresenter` reutiliza trail/anel/ParticleSystem; `PlayerCombat.TargetHit` confirma dano aceito do básico. Especial ainda não publica contato por alvo; não inferir que FX-HIT do básico cobre o giro. Efeitos não causam dano nem consomem o RNG do loot.

Personagens usam rig Generic/in-place e FBX; GLB é fonte/prévia sem importador Unity instalado. Fontes de integração em `Tools/Lemo3DGenerator/unity` e cópias em `Art/Models/Generated/_Scripts` têm funções distintas e devem ficar sincronizadas. SpriteLab e billboard são experimentos preservados fora da release. Os caminhos `_GameTest`/`_GameArt` são legados, mas contêm a cena/arte de produção: não mover por nome sem migrar builders/testes/referências.

## Feedback de recompensas (F05)

`HealthModel.Healed`/`Health.Healed` publicam `HealingAccepted` com quantidade efetiva, HP atual/máximo capturados e `HealingSource` (Other/Potion/LevelUp), depois de `Changed`. Reset não emite cura e cancela confirmação pendente; `ResetPerformed` limpa apresentação. `PlayerInventory` protege consumo contra reentrada durante Changed/Healed. `PlayerProgression.CollectionAccepted` conserva o `LootDrop` aceito, sem outro sorteio; `Collected` continua sendo a única confirmação de coleta usada pelo áudio. Uma chamada reentrante de coleta no mesmo jogador é recusada durante a transação e suas notificações. `LeveledUp` mantém uma notificação com snapshot do nível final, sem alterar fórmula/quantidade de cura.

`PlayerRewardFeedback` observa esses eventos, sem inferir cura por HP Changed, com três ParticleSystems previamente compostos e reutilizados, sem fontes/objetos novos por operação ou uso do RNG do loot. Cura de nível usa somente a apresentação de nível. Fila fixa de quatro avisos pendentes mais um visível; texto/quantidades são formatados somente ao exibir, com alocação de string limitada a essa troca. Temporizadores e partículas usam tempo escalado; morte/reset/desativação limpam fila/texto/partículas e OnEnable/OnDisable gerem inscrições. Opção flash controla as partículas, sem alterar o flash de dano ou o shake. GameplayAudioBridge reutiliza o tom Ui para cura/nível pelo pool/mixer SFX e conserva Pickup para coleta.

`RewardFeedbackSceneBuilder.Integrate` integra somente o prefab Player e as três cenas de gameplay pelas APIs do Editor. A integração geral de inventário também recompõe essas referências. Preserva VFX de combo/giro/acerto, áudio e dados existentes. Evidência e limites em [Reports/F05](Reports/F05/README.md).

A reconstrução M1 aplica arte somente à própria arena por `GeneratedArtApplier.ApplySceneEnvironment`, mantendo prefabs de salas e a cena de release intactos. O teste de preservação verifica esses bytes durante cada rebuild e inclui a cena gerada/meta no snapshot restaurado. Isso evita que a recriação global da decoração mude fileIDs dos pais e deixe as chamas adicionadas à cena órfãs no centro das salas. Fonte Tools e cópia instalada permanecem sincronizadas.

## Checkpoint entre salas (F11 / D-004)

Especificação e limites em [CHECKPOINTS-F11](docs/CHECKPOINTS-F11.md). `RunCheckpointData` é o schema JSON **v1** em `persistentDataPath/run-checkpoint.json` (`DUNGEON_CRAWLER_CHECKPOINT_DIR` isola testes), separado de PlayerPrefs de opções/bindings. `RunCheckpointCatalog` (SO) resolve IDs explícitos de `ItemDefinition.PersistentId` e `RoomController.PersistentId`, guarda a âncora de retomada por sala e valida o documento inteiro (versão, finitos, limites, capacidade, tipos/potências, equipamento, salas, baús, pickups únicos) antes de qualquer mutação. Versão futura ou ID ausente rejeitam a carga.

`RunCheckpointFile` limita o tamanho a 1 MiB, escreve `.tmp` com flush e troca por `File.Replace` com `.bak`; um primário inválido nunca substitui um backup válido. `RunCheckpointStore` tenta primário e backup com o mesmo validador. `RunCheckpointSession` transporta um único pedido de Continuar até a cena do andar; Nova run/Reiniciar apagam slot e backup.

`RunCheckpointController` (ordem 100) grava no `LateUpdate` quando há mudança e a fronteira é segura: jogador vivo, ≥1 sala limpa e nenhuma sala Active. Retomar usa APIs `RestoreCheckpoint`/`TryRestore` que atribuem estado sem passar por coleta, cura, ganho de XP ou eventos de nível; salas limpas abrem portas sem spawn nem `Cleared`. `RunLootRegistry` registra pickups injetados (ID de instância, payload já sorteado, posição, tempo restante) e mantém o xorshift32 da run que sorteia loot de baús/inimigos e dispersão de spawn; o estado é persistido em hexadecimal. Morte apaga o checkpoint. `RunCheckpointSceneBuilder` (menu `Tools/Dungeon Crawler/Checkpoints/...`) compõe catálogo, referências do andar de release e o botão **ContinueRun**/status no MainMenu de forma idempotente; `GameShellSceneBuilder` reaplica o menu.

## Talento por nível (F12 / D-005)

Regras e valores em [TALENTS-F12](docs/TALENTS-F12.md). `TalentDefinition` (ID persistente, efeito, valor por rank, máximo) e `TalentCatalog` (ordem fixa) são SOs somente leitura. `TalentModel` é puro: escolhas disponíveis = `(nível − 1) − escolhidos`, limitadas à capacidade restante; não existe contador separado. `TryRestore` valida o snapshot inteiro antes de atribuir.

`PlayerTalents` assina `PlayerProgression.Changed` no ciclo de vida e escreve **valores absolutos** dos próprios termos: `PlayerCombat.TalentDamageMultiplier` (fator separado de `DamageMultiplier`, que continua sendo só o nível), `PlayerInventory.SetTalentDefenseBonus` (base + armadura + talento, também usado pela comparação F01) e `PlayerSpecial.SetCooldownReduction` (aplicada por `PlayerSpecialModel.EffectiveCooldown` ao iniciar o giro). Reativação/refresh não acumulam e nenhum SO compartilhado é editado. `TryChoose` recusa morto/desabilitado e reentrada durante `Chosen`/`Changed`.

`MenuScreen.Talents` (acrescentado ao fim do enum) abre sobre Pausa via `GameMenuController.OpenTalents`, compartilhando a pausa; Esc/Voltar retornam à Pausa. `TalentChoicePresenter` só formata e solicita; trava 0,3 s em tempo real entre escolhas aceitas e volta à Pausa após a última. `TalentNoticePresenter` mostra o aviso no HUD e a contagem no botão, com a tecla efetiva de pausa. `TalentSceneBuilder.Integrate` (menu `Tools/Dungeon Crawler/Progression/...`) cria os SOs sem sobrescrever valores ajustados, compõe prefab e as três cenas de gameplay e é chamado também por `InventorySceneBuilder.ConfigurePlayer/ConfigureHud`, porque `GameShellSceneBuilder` redefine a lista de telas. `TextScaleSceneBuilder` inclui o botão no layout da Pausa (janela cresce) e o aviso abaixo do HUD de poções.

Checkpoint **v2**: `RunCheckpointData.talents` (`{id, rank}`), resolvidos por `RunCheckpointCatalog.talents`. v1 continua válido sem talentos e migra com as escolhas dos níveis já ganhos pendentes; v1 com talentos, ID desconhecido/duplicado, rank fora de 1..máx ou soma maior que `nível − 1` rejeitam a carga. Restauração ocorre depois do nível e não emite `Chosen`.

## Auditoria reproduzível

`python Tools/Audit/audit_project.py --output <relatorio-novo.json>` inventaria hashes, duplicatas, cópias instaladas e caminhos de links Markdown sem alterar assets. Recusa sobrescrever o relatório. `MvpProjectAudit.InspectCleanupBaseline`/menu Audit abre cenas/prefabs pelas APIs e escreve em `Reports/ProjectAudit/editor-assets.json`; não salva assets. Esses dados não comprovam aceite visual ou ausência de uso dinâmico. Comandos de teste e regressão em `docs/BUILD-WINDOWS.md`.
