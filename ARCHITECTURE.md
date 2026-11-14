# ARCHITECTURE.md — Unity Dungeon Crawler

## Ambiente
Unity 6 LTS disponível na instalação, projeto template **Universal 3D (URP)**, PC Windows. Fixar a versão exata do Editor após criação e commitar `Packages/manifest.json` e `packages-lock.json`. Usar versões compatíveis de Input System, Cinemachine, AI Navigation e Test Framework. Evitar fixar versões sem consultar Package Manager.

## Pastas
`Assets/_Project/{Art,Audio,Animations,Materials,Input,Prefabs/{Player,Enemies,Boss,Rooms},Scenes,ScriptableObjects,Scripts/{Core,Player,Enemies,Boss,Dungeon,Systems,UI,Editor},Settings,Tests/{EditMode,PlayMode}}`.

## Namespaces e componentes
- `DungeonCrawler.Core`: `IDamageable`, eventos e contratos comuns.
- `DungeonCrawler.Player`: `PlayerMovement`, `PlayerAim`, `PlayerCombat`, `PlayerDash`, `PlayerHealth`.
- `DungeonCrawler.Enemies`: `EnemyBrain`, `EnemyNavigation`, `EnemyCombat`, `EnemyHealth`.
- `DungeonCrawler.Boss`: `BossPhaseController`, `BossAttackTelegraph` (fase posterior).
- `DungeonCrawler.Dungeon`: `RoomController`, `DoorController`, `EnemySpawner` (fase posterior).
- `DungeonCrawler.Systems`: `ExperienceSystem`, `LootSystem`, `ProjectilePool` (fases posteriores).
- `DungeonCrawler.UI`: `PlayerHealthPresenter`, `BossHealthPresenter`.
- `DungeonCrawler.Editor`: `DungeonMvpSceneBuilder`.

## Dependências
- Composição por referências serializadas, componentes irmãos (`GetComponent` local em Awake quando necessário) e inicialização explícita.
- Eventos `Action` para alterações de HP, morte, conclusão de sala e nível; evitar eventos estáticos globais quando o ciclo de vida puder causar assinaturas obsoletas.
- UI escuta estado e eventos, não decide dano ou mortes.
- Interfaces definem contratos (`IDamageable` recebe dano e expõe estado; `IPoolable` recebe callbacks de ativação/desativação).
- `ScriptableObject` define configurações; instâncias de runtime guardam HP/cooldown e não alteram o asset compartilhado.

## Input e movimento
Criar asset `.inputactions` com mapa `Gameplay`: `Move` (Vector2 WASD), `Point` (mouse position), `Attack` (left click), `Dash` (space), `Special` (right click/E, ainda não usado). Utilizar callbacks ou leitura de `InputActionReference`; nunca Input legado. Projetar forward/right da câmera no plano XZ e normalizar. Mira: raio da câmera para chão/collider, fallback a plano horizontal quando apropriado.

## Câmera
Cinemachine com follow e offset isométrico; confirmar API conforme versão instalada. Configurar smoothing e orientação fixa; tremor somente via componente/efeito compatível, não manipular transform da câmera principal de forma conflitante.

## Física, dano e IA
Player: CharacterController e gravidade explícita se necessário. Ataque inicial melee por overlap/hitbox com máscara de inimigos e janela de acerto controlada. Inimigo: NavMeshAgent, decisões temporizadas, dano com cooldown, parar ataque ao morrer. Evitar alocação por frame e buscas de cena repetidas. NavMesh bake compatível com AI Navigation.

## Pooling
Projéteis obrigatoriamente pooled; considerar UnityEngine.Pool.ObjectPool<T> com retorno por colisão, timeout e desativação. Não implementar pool antes de haver projéteis.

## Automação de cena
Editor script idempotente com `MenuItem("Tools/Dungeon Crawler/Build MVP Scene")`: criar/atualizar arena, luz, player, inimigo, câmera, HUD e salvar em `Assets/_Project/Scenes/MVP_Arena.unity`. Garantir que reexecutar não duplique objetos. Prefabs devem ser salvos por APIs do Editor, não por texto manual.

## Testes
EditMode: cálculos de dano, clamp HP, cooldown, transição de estado. PlayMode: movimentação, dano, morte, ataque, dash e reinício quando viável. Relatar explicitamente dependências de teste manual (mouse, câmera, NavMesh).

## Git
Ignorar `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, `UserSettings/`, `.vs/` e IDE caches. Versionar `.meta`, `Assets`, `Packages`, `ProjectSettings` e documentação. Unity Asset Serialization: Force Text; Version Control: Visible Meta Files, quando disponível na versão do Editor.
