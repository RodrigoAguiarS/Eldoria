# Guia de integração e validação Unity

Data: 14/11/2026. Procedimento para tarefas futuras. **Nenhuma integração, geração, compilação ou execução de PlayMode deste guia foi realizada nesta auditoria. NÃO VALIDADO NO EDITOR.**

## Ambiente e destinos reais

Preservar Unity 6000.6.9f1, URP 17.6.0, Cinemachine 6.6.0, Input System 1.20.1, AI Navigation 2.0.16, UGUI 2.6.0 e Test Framework 1.8.0 do lockfile. A skill Unity tem baseline 6000.3; seus exemplos não autorizam migração, comandos de plugins ausentes ou APIs de outra versão do Cinemachine. Não adicionar VFX Graph, DOTween, importador GLB ou fonte como dependência presumida.

A release inclui duas cenas segundo `MvpReleaseBuild.Build`: `Assets/_Project/Scenes/MainMenu.unity` e `Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity`. Outras cenas habilitadas no EditorBuildSettings não entram automaticamente nesse BuildPlayer. `Floor_01`, `MVP_Arena`, Bootstrap, SpriteLab e Art_Test_Hero são apoio ou experimentos; validar o destino real da release.

Herói, rig, materiais e ambiente atuais devem ser preservados. Em tarefa posterior de produção, receber arquivos em `Assets/_Project/Art/ProductionStaging/<ID>/`, com fonte editável e formatos da ficha. Esta pasta de staging é uma proposta; não foi criada nesta auditoria. Nomear pelo ID ou registrar a correspondência exata no manifesto. Mover assets pelo Editor mantendo `.meta` e GUID. Usar branch própria `codex/...`, escopo explícito de arquivos e coordenação para não editar assets simultaneamente.

## Ordem de integração futura

1. Ler os seis documentos obrigatórios, `git status`, versão, pacotes, manifesto desta auditoria e arquivos envolvidos. Registrar baseline de GUID/bytes e preservar mudanças desconhecidas. A missão atual autoriza somente documentação.
2. Escolher o ID e suas dependências. Resolver conflitos de atlas/UV/Humanoid antes de alterar a parte afetada. Não substituir recursos existentes por serem simples.
3. Receber fonte e asset em staging. Conferir ferramenta, versão, data, prompt, referências, edições e licença. Não inferir direitos desconhecidos das duas imagens antigas do Hero. Fazer QA técnico antes da importação.
4. Importar na instalação real e criar variantes/cena isolada por APIs do Editor. Scripts futuros de construção ficam em `Assets/_Project/Scripts/Editor`, namespace `DungeonCrawler.*`, com menu `Tools/Dungeon Crawler/...` e comportamento idempotente. Não editar `.unity`/`.prefab` textuais às cegas, especialmente as seis cenas binárias da inspeção inicial.
5. Conectar apresentação por referências serializadas, composição e eventos C#, com inscrições no ciclo de vida. Ícone não altera dados de combate; malha não muda hitbox; VFX não aplica dano. Integrar código somente em tarefa posterior delimitada.
6. Rodar testes pertinentes, abrir a cena real em PlayMode e verificar aparência/interação. Testar 1280×720 e 1920×1080. Fazer build Windows quando a mudança entrar na release. Se faltar Editor ou observação, registrar `NÃO VALIDADO NO EDITOR`.
7. Atualizar caminho, status, integração, procedência e evidências por ID. Importação não basta para aprovação. Revisar o diff final para evitar reconstrução acidental de personagens, salas e opções. Não criar commit sem pedido.

## Contratos de importação

| Tipo | Configuração | Verificação |
| --- | --- | --- |
| Ícone | PNG RGBA 512×512, alpha real; Sprite Single, sRGB, mipmaps off, bilinear, clamp, PPU 100; texto/raridade/quantidade separados | Alpha não totalmente opaco, sem checkerboard ou halos; leitura a 32/48 px sobre fundos claros, escuros e pedra |
| Modelo estático | Fonte `.blend` + FBX; 1 unidade = 1 m; Unity +Y acima/+Z frente; transforms aplicadas; pivô conforme ficha; normais facetadas; URP/Lit | Bounds reais, orientação, topologia, materiais e custo medidos; sem collider detalhado substituindo a lógica |
| Roupa/arma equipada | Rig Generic, hierarquia e sockets preservados; uma arma visível; colete vestível precisa de skin e pesos | Amostrar os 13 clips Hero; verificar capa, corpo e mão. Modelo estático de chão não satisfaz roupa vestível |
| Clip 3D | Take nomeado, 30 fps, Generic/in-place, applyRootMotion false, loop conforme ficha; sem AnimationEvent de dano | Motion/controller/Speed/Dead/ComboSpeed, continuidade do loop, poses, cancelamento e raiz estável |
| Atlas condicional | Somente após decisão, em variante isolada com UV apropriada; cor 512 sRGB; dados lineares | Overlay UV, seams, flash `_BaseColor`; estudos de superfície não são atlas |
| UI | UGUI editável, texto em runtime, CanvasScaler 1920×1080/match .5, InputSystemUIInputModule atual | Foco inicial, input somente no topo, disabled, wrap/contraste e dropdown em 720p/1080p |
| VFX | ParticleSystem/mesh/trail simples; materiais URP compatíveis, sem pacote novo; pool/reset | Sem dano/collider novo; limpar trails, respeitar pausa/cancelamento e opções de flash/shake |

Para PNG, usar o checker instalado quando a dependência Python/Pillow estiver disponível:

```text
python .claude/skills/create-game-assets/scripts/asset_report.py <arquivo.png> --expect-size 512x512 --require-alpha --json
```

Esse comando prova somente requisitos raster. `build_preview_sheet.py` da mesma skill pode montar uma contact sheet para revisão. Não impor limite de cores de pixel art aos ícones estilizados. Não rodar geração nesta auditoria.

## Integração por família

**Ícones e bolsa:** criar um mapa visual serializado ItemDefinition→Sprite ou extensão equivalente em escopo aprovado. SlotView atual só possui Button, Text e background; adicionar Image é trabalho de código posterior. Manter nome, raridade, quantidade e Equipado em camadas separadas, sem mudar índices. HUD-POTIONS escuta inventário/HP e exibe soma de quantidades; não cura nem define cooldown. Reutilizar os detalhes fixos existentes. Não adicionar ordenação, arrastar ou slots novos.

**Itens 3D:** um seletor por LootKind/ItemDefinition escolhe o filho visual mantendo trigger/collider. Pickup atual recebe um Renderer e tint; precisa de adaptação explícita para formas diferentes. Potências de poção compartilham forma/ícone. Para equipamento visível, observar Changed e trocar somente visual no socket, ocultando a espada fixa antes de anexar outra. Desequipar não destrói item. Ajustar o colete em cópia isolada do rig antes de solicitar qualquer alteração do corpo aprovado.

**Animações:** preservar os 13 clips Hero. Attack/HeavyAttack legados não substituem Attack_01/02/03. Contatos de 0,16/0,19/0,32 s e Special em 0,36 s pertencem ao domínio. No GeneratedGameTestBuilder, melee/elite usam offset de 0,1 s e ranged usa 0 s. A ponte Boss distingue Slam/Volley pela dimensão do telegraph; não alterar a escala do aviso como decoração. Não migrar para Humanoid, adicionar root motion ou Animator concorrente ao portão procedural.

**VFX:** usar bridges com referências explícitas. AttackImpacted informa tentativa de ataque, sem confirmar alvo atingido. Dano/cura sem origem e posição exigem adapter ou payload em tarefa posterior. Coleta só gera burst após aceitação. Cura visual não pode atrasar HP nem mudar Q durante a pausa da bolsa. FX-SLAM não chama combate. Partículas e trails voltam ao estado inicial no pool; projéteis preservam o pool real. Transições de UI usam tempo não escalado e não atrasam foco/input.

**Ambiente:** reutilizar `_GameArt/Meshes/Materials` e GeneratedArt aplicado. Manter layers, colliders, passagens, centro livre e NavMesh. Decoração permanece ignorada pelo bake com NavMeshModifier. Malhas GA podem compensar a escala do GameObject; medir bounds no mundo antes de redimensionar. Preservar ocultação de paredes e duas luzes realtime por sala, sem confundir com duas luzes globais no andar.

## Menus de Editor e efeitos de execução

Existem `Tools/Dungeon Crawler/Audit/Inspect Project Assets` e `Tools/Dungeon Crawler/Generated Characters/Validate In Editor`. O primeiro abre cenas por API e escreve relatório; seu código não salva cenas/assets. Conferir estado Git e cenas não salvas antes de usá-los. O relatório não prova aparência em movimento.

Os menus abaixo salvam ou reconstroem assets/configuração. Não os usar como inspeção nem como requisito para importar cinco ícones:

- `Tools/Dungeon Crawler/Build/MVP Scene (M1)` e `Build/Floor Scene (M2)`.
- `Tools/Dungeon Crawler/Generated Characters/Build Test Assets (all)`, `Build In-Game Test (Floor_01 copy)`, `Apply Generated Art To Game` e `Update Hero Combo Clips`.
- `Tools/Dungeon Crawler/Inventory/Integrate Inventory`, `Combat/Integrate Player Special` e `Menus/Integrate Menus And Options`.
- `Tools/Dungeon Crawler/Build/Windows Release (Current MVP)`.

Os nomes abreviados após o primeiro item de cada linha mantêm o mesmo prefixo de menu. Se reconstrução for necessária, delimitar arquivos, conferir preservação e idempotência. Não foram executados nesta auditoria.

Menus QA existentes abrem a arena/andar em PlayMode e oferecem preview de inventário, especial lento e Boss. Preview contém estado artificial: não prova loot normal, vitória ou run completa. Não salvar conteúdo de preview na cena.

## Matriz de validação futura

| Mudança | Testes existentes pertinentes; ler asserts antes de selecionar | Conferência manual |
| --- | --- | --- |
| Ícones/inventário/poções | InventoryAssetIntegrationTests, InventorySceneUiTests, InventoryPauseInputTests, PlayerInventoryPlayModeTests | 16 slots, raridades/potências, nomes longos, Q/Usar, HP cheio/morto, equipar/desequipar/largar, recusa e nova coleta |
| Hero/rig/clips | PlayerSpecialAssetTests, PlayerSpecialCombatTests, PlayerComboCombatTests, PlayerInputComboTests, MvpBuilderPreservationTests | Oito direções, mira, combo, buffer/segurar, dash, morte/cancelamento, Special; pés/capa/arma em movimento |
| Boss/avisos | GeneratedBossAssetTests, GeneratedBossPresentationTests, BossPlayModeTests, FloorIntegrationTests | Transição a 50% uma vez, 3 s, telegraph ≥1 s, Slam/Volley, flash 0/1, vitória 5/5 |
| Ambiente/equipamento 3D | FloorBuildTests, MvpBuilderPreservationTests, InventoryAssetIntegrationTests conforme escopo | Escala, colliders/sockets, portas e última morte, percurso NavMesh, herói visível |
| Menus/transições | GameShellAssetTests, GameMenusInputTests, GameOptionsTests, GraphicsOptionsPlayModeTests | Foco/camada/retorno, Apply/Back, 720p/1080p, dropdown aberto, run nova |
| VFX/projéteis | ProjectilePoolPerformanceTests, PlayerSpecialCombatTests e lifecycle afetado | Pool aquecido, reset, duas ativações rápidas, pausa/morte, dano autoritativo, piso legível e medições comparáveis |

Testes de idempotência podem salvar assets: executar em tarefa de implementação com baseline, não em auditoria preservativa. Priorizar referências, regras e ciclo de vida; não criar teste que só espelha cores. Após os testes pertinentes passarem, repetir somente diante de mudança ou preocupação nova. Build Windows deve abrir menu/andar, testar controles, inventário, especial, derrota/reinício, vitória 5/5 e opções/áudio, com provas e limites exatos.

## Evidências e critérios de conclusão

XMLs históricos finais registram 93 EditMode e 69 PlayMode aprovados. A release documenta aceite humano da RC2. Nesta sessão a validação é documental: JSON, IDs, dependências, links, caminhos e comparação de hashes. Não atribuir testes anteriores ou ações simultâneas de outro processo a esta auditoria.

O relatório RELEASE-1.0-VALIDATION documenta 496 avisos de shaders no build final; o `windows-build.json` lido durante esta sessão reporta 0 avisos e 0 erros. São evidências diferentes: não inventar causa, substituir o histórico ou alegar compilação atual revalidada. O relatório API também foi atualizado externamente durante a auditoria; o manifesto registra o timestamp e hash efetivamente lidos.

Gate de asset: fonte/arquivo/procedência → QA técnico → importação correta → binding resolvido → PlayMode/legibilidade → aprovação artística quando aplicável. Atualizar status e integração separadamente. Imagem de conceito não satisfaz entrega de modelo/clip funcional.
