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

Treze slots estáveis (`Move/up`, `Dash/0`, etc.) formam o schema v1 de `DungeonCrawler.Bindings.v1` no PlayerPrefs. A carga valida o documento inteiro antes de aplicar: versão desconhecida, ID repetido/desconhecido, caminho inválido e colisão entre slots rejeitam a carga sem apagar a preferência. Somente teclas físicas canônicas e cinco botões de mouse são aceitos; agregadores/curingas/eixos/gamepad são recusados. Escape é reservado, com fallback imutável na ação Pause; Enter/numpadEnter ficam na UI. Conflitos não são trocas automáticas. Reset remove overrides e salva defaults; não toca opções gerais nem inventário/progressão.

`RuntimeInputBindings` controla `RebindingOperation`, timeout não escalado, cancelamento por Escape/botão/perda de foco/desativação, suspensão dos donos e retomada após um frame neutro. O clique em Cancelar/Voltar não pode virar binding. `GameMenuController` informa o contexto: poção disponível no gameplay/bolsa; reinício por atalho suspenso nos overlays, com botões ainda funcionais. Raycast por EventSystem explicitamente injetado filtra cliques de UI; PlayerInputReader exige soltar botões ao retomar a pausa para evitar ataque pelo clique de Continuar. `BindingHintPresenter` e os presenters de recurso/avisos consultam as ações efetivas. `ControlBindingsSceneBuilder.Integrate` integra prefab e quatro cenas idempotentemente, sem reconstruir arte, salas, dados ou `.inputactions`.

## Câmera
Cinemachine com follow e offset isométrico; confirmar API conforme versão instalada. Configurar smoothing e orientação fixa; tremor somente via componente/efeito compatível, não manipular transform da câmera principal de forma conflitante.

## Física, dano e IA
Player: CharacterController e gravidade explícita se necessário. Ataque inicial melee por overlap/hitbox com máscara de inimigos e janela de acerto controlada. Inimigo: NavMeshAgent, decisões temporizadas, dano com cooldown, parar ataque ao morrer. Evitar alocação por frame e buscas de cena repetidas. NavMesh bake compatível com AI Navigation.

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

## Áudio e diagnóstico (1.0)

GameAudioDefinition contém clips e roteamento Music/SFX. GameAudioPool reutiliza oito vozes pré-configuradas, limita prioridade/repetição e aplica ganhos das categorias em dB; mute em zero é explícito. GameplayAudioBridge e EnemyAudioBridge escutam eventos C# com inscrições no ciclo de vida. EnemySpawner injeta o pool nos atores instanciados; progressão emite Collected apenas após coleta aceita. Variação sonora não altera o gerador aleatório do loot.

Sons e loop originais são reproduzíveis em Tools/AudioGenerator. Clips são PCM/precarregados; novas fontes não são criadas durante golpes. Diagnóstico opcional no player registra contadores e pode capturar o DSP do próprio jogo; sem flags, coletores/cenário ficam desativados e o filtro de captura não é instanciado. Limites, flags e fontes das APIs em Tools/AudioGenerator/DIAGNOSTICS.md.

## Apresentação de itens e combate (pós-release)

`ItemVisualCatalog` mapeia definição/LootKind para ícone, prefab de chão e equipamento. `PickupVisualPresenter`, `PlayerEquipmentVisualPresenter` e presenters UGUI só representam estado. Espadas/colete são pré-configurados no Hero; troca visual não muda bônus. `PlayerAttackVfxPresenter` reutiliza trail/anel/ParticleSystem; `PlayerCombat.TargetHit` confirma dano aceito do básico. Especial ainda não publica contato por alvo; não inferir que FX-HIT do básico cobre o giro. Efeitos não causam dano nem consomem o RNG do loot.

Personagens usam rig Generic/in-place e FBX; GLB é fonte/prévia sem importador Unity instalado. Fontes de integração em `Tools/Lemo3DGenerator/unity` e cópias em `Art/Models/Generated/_Scripts` têm funções distintas e devem ficar sincronizadas. SpriteLab e billboard são experimentos preservados fora da release. Os caminhos `_GameTest`/`_GameArt` são legados, mas contêm a cena/arte de produção: não mover por nome sem migrar builders/testes/referências.

## Auditoria reproduzível

`python Tools/Audit/audit_project.py --output <relatorio-novo.json>` inventaria hashes, duplicatas, cópias instaladas e caminhos de links Markdown sem alterar assets. Recusa sobrescrever o relatório. `MvpProjectAudit.InspectCleanupBaseline`/menu Audit abre cenas/prefabs pelas APIs e escreve em `Reports/ProjectAudit/editor-assets.json`; não salva assets. Esses dados não comprovam aceite visual ou ausência de uso dinâmico. Comandos de teste e regressão em `docs/BUILD-WINDOWS.md`.
