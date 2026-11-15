# Auditoria completa de assets — Dungeon Crawler

> Estado atualizado da auditoria geral: [PROJECT_AUDIT](../../../../PROJECT_AUDIT.md). O manifesto atual registra 133 recursos: 117 EXISTENTES, 13 AUSENTES, 1 INCOMPLETO e 2 NÃO VERIFICADO; zero ausentes P0. O quadro 96/5/30/2 abaixo é a fotografia anterior aos lotes ANIM-TORCH/ITEMS-COMBAT, não backlog atual. Leitura por ID; fichas/prompts completos consolidados no JSON e índices especializados preservados.

Data local: 14/11/2026, America/Fortaleza. Revisão de referência: `2304c64785d595aa86cfafbc987f1c47468fce03`. Escopo: documentação, sem geração de arte e sem alterações em scripts, sistemas, cenas, prefabs, Packages ou ProjectSettings.

## Lote ITEMS-COMBAT (14/11/2026)

Produzidos, importados, integrados e validados no Editor: MODEL-ITEM-IRON/WARDEN/ANCIENT/LEATHER/HEALTH, MODEL-LOOT-GOLD/XP, ICON-ITEM-IRON/WARDEN/ANCIENT/LEATHER/HEALTH, FX-SLASH, FX-SPECIAL, FX-HIT (combo), FX-BASIC (diagnóstico), HUD-POTIONS, UI-SLOTS, UI-INVENTORY e LOOT-SHARED. Build Windows Succeeded. Aprovação artística e conferência visual da build pelo usuário pendentes. Detalhes em ITEMS_AND_EQUIPMENT.md, VFX_ASSET_SPECS.md e `production_batches` do manifesto.

## Resultado e critério de contagem

Foram catalogados **133 recursos lógicos: 96 EXISTENTES, 5 INCOMPLETOS, 30 AUSENTES e 2 NÃO VERIFICADO**. São **36 clips 3D individuais** dentro dos cinco FBX/GLB. Há **0 recursos AUSENTES P0** identificados: a release 1.0 é funcional segundo as provas existentes. P1 aqui completa a apresentação das funcionalidades aprovadas; não reabre o aceite do produto.

Uma entrada representa um recurso ou família explicitamente agrupada (por exemplo, materiais de um personagem). Modelos, rigs, clips e materiais são unidades distintas de produção; subassets e cópias FBX/GLB não são somados duas vezes. Os **461 arquivos físicos em Assets, sem .meta**, têm caminho, tamanho e SHA256 em `file_index` do [manifesto](ASSET_MANIFEST.json); isso inclui scripts, dados, áudio, template e laboratório 2D, portanto não é o total de recursos visuais. `.meta` foi consultado para GUIDs, rig e importação, sem ser contado como arte.

EXISTENTE significa que o recurso foi localizado; `integration_state` distingue integrado, disponível sem uso e referência. INCOMPLETO significa apresentação parcial de uma base utilizável. AUSENTE não significa automaticamente obrigatório: `scope` diferencia complemento da 1.0, sugestão de polimento, futuro condicional e conflito bloqueado. NÃO VERIFICADO mantém a incerteza explícita.

| Categoria | Existentes | Incompletos | Ausentes | Não verificados | IDs ausentes |
| --- | --- | --- | --- | --- | --- |
| A — Personagem principal | 19 | 0 | 1 | 2 | TEX-ATLAS-HERO |
| B — Inimigos e boss | 41 | 0 | 1 | 0 | TEX-ATLAS-MELEE |
| C — Armas e equipamentos | 4 | 0 | 6 | 0 | ICON-ITEM-IRON, MODEL-ITEM-IRON, ICON-ITEM-WARDEN, MODEL-ITEM-WARDEN, ICON-ITEM-ANCIENT, MODEL-ITEM-ANCIENT |
| D — Inventário e itens | 2 | 2 | 2 | 0 | ICON-ITEM-LEATHER, MODEL-ITEM-LEATHER |
| E — Poções e consumíveis | 0 | 0 | 3 | 0 | ICON-ITEM-HEALTH, MODEL-ITEM-HEALTH, HUD-POTIONS |
| F — Menus e interfaces | 6 | 0 | 2 | 0 | UI-SKIN, UI-TRANSITION |
| G — HUD e gameplay | 8 | 0 | 2 | 0 | ICON-ACTION-DASH, ICON-ACTION-SPECIAL |
| H — Loot e recompensas | 0 | 1 | 2 | 0 | MODEL-LOOT-GOLD, MODEL-LOOT-XP |
| I — Cenários e objetos interativos | 12 | 0 | 1 | 0 | MODEL-DESTRUCTIBLE |
| J — VFX e animações adicionais | 4 | 2 | 10 | 0 | ANIM-TORCH, FX-SLASH, FX-SPECIAL, FX-HIT, FX-HEAL, FX-LOOT, FX-LEVEL, FX-SLAM, FX-DASH, FX-PROJECTILE-TRAIL |


## Fontes de verdade e preservação

Foram lidos AGENTS.md, GAME_DESIGN.md, ARCHITECTURE.md, MVP.md, ART_DIRECTION_AND_PROMPTS.md e docs/RELEASE-1.0-VALIDATION.md. Regras da 1.0 foram cruzadas com docs/PRODUCT-1.0-PROPOSAL.md, dados ScriptableObject, presenters, builders, testes e evidências de release. ART_AND_COMBAT_REQUEST e os prompts antigos são histórico: não tratá-los como diagnóstico corrente.

O usuário autorizou preservar integralmente três alterações pré-existentes: `ProjectSettings/ProjectSettings.asset`, `Reports/Leadership/asset-audit-current.json`, `Reports/Leadership/windows-build.json`. Nenhuma delas faz parte desta entrega. Não executar builds ou testes de idempotência apenas para revisar arte: esses testes chamam builders e podem salvar assets.

## Skills localizadas e aplicadas

O sistema awesome-gamedev-agent-skills está em `.claude/skills`, com NOTICE e licença Apache 2.0. Router encontrado em `.claude/skills/router/SKILL.md`; a afirmação antiga de router ausente está desatualizada. Foram lidos os SKILL.md raiz abaixo (não presumir instalação no catálogo global Codex):

| Skill em .claude/skills/<nome>/SKILL.md | Aplicação nesta auditoria |
| --- | --- |
| router | Roteamento Unity e fronteiras das disciplinas |
| unity-animation | Controllers, estados, rig, root motion, sincronização e limites do aceite visual |
| create-game-assets | Manifesto, referência de identidade, alpha, fonte/licença e pipeline 3D DCC |
| game-ui-ux | Layout, escala, foco, estados, legibilidade e pausa |
| rpg | Ler dados reais e não inventar classes, atributos ou raridades |
| game-feel | Feedback em eventos, proporcionalidade, reset e opções de flash/shake |
| unity-scriptableobjects | Dados compartilhados, runtime separado e referências explícitas |
| level-design | Preservar métricas/colisão/navegação antes de dressing |
| game-docs | Prova, status e rastreabilidade documental |


Referências adicionais lidas: `create-game-assets/references/three-d-pipeline.md`, `game-ui-ux/references/layout-and-flow.md`, `game-docs/references/doc-rules.md`. `create-game-assets` orienta a produção e QA, mas não fornece um gerador de modelos/rigs 3D. O router declara lacuna em Unity VFX Graph e não contém skill dedicada Unity UGUI: esta auditoria usa UGUI comprovado no projeto, sem inventar capacidades ou instalar plugins. Nenhuma ferramenta de geração foi chamada.

Skills Unity têm baseline 6000.3; o projeto usa **6000.6.9f1/URP 17.6.0**, Cinemachine 6.6.0, Input 1.20.1, AI Navigation2.0.16, UGUI2.6.0 e Test Framework1.8.0 no lockfile. Não migrar versões nem copiar comandos `unity command` de plugin não instalado. O [manual Mecanim 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/AnimationOverview.html) foi consultado para a distinção clips/controllers; o [guia URP 17](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/upgrade-guide-unity-6.html) é referência de compatibilidade, sem executar migração. Contratos específicos vêm dos importers, código e pacotes locais reais.

## Método e limites de evidência

1. Inventário completo de arquivos Assets; leitura dos documentos, catálogos e manifestos de personagens. Leitura de código de domínio, apresentação, integração e testes pertinentes; presença de classe não foi usada como prova de funcionamento.
2. Chunks JSON dos cinco GLB confirmam 36 animações, nomes e materiais; importers FBX confirmam Generic (animationType2). Controllers e bridges foram cruzados com PlayerCombo/PlayerSpecial/EnemyData/BossData. Números do manifesto do gerador são identificados como origem, não novas medições no Editor.
3. Relatório API corrente: `Reports/Leadership/asset-audit-current.json`, timestamp UTC `2026-11-14T15:34:28.9171924Z`, 168 assets serializados e `issues=[]`. É evidência anterior, fornecida pelo projeto e preservada, não novo teste vivo. Hierarquias/dependências compactas estão em `serialized_api_inventory` no manifesto. O auditor desse relatório omite campos de Light/MeshRenderer/RectTransform, por isso não basta para afirmar geometria, luz ou layout correto.
4. As seis cenas binárias foram examinadas por seu registro API/dependências/builders, não interpretadas como YAML. MainMenu, Bootstrap, SampleScene e duas cenas demo possuem texto. Não houve reserialização.
5. Foram vistas as imagens já existentes `Tools/Lemo3DGenerator/preview/captures/unity/ingame_environment_combat.jpg` e `turn_Hero-SkeletonMelee-SkeletonElite-SkeletonRanged-Warden_Idle.png`. Elas sustentam aparência histórica do ambiente/família, sem provar UI atual nem animação em movimento.
6. XMLs finais lidos: 93/93 EditMode e 69/69 PlayMode, ambos Passed; são execuções históricas. Release relata zero referências inválidas e aceite humano RC2 de vitória/derrota/reinício, inventário/Q/E, áudio/feedback. Não converter esse relato em captura automatizada inexistente.

**NÃO VALIDADO NO EDITOR nesta auditoria.** Unity 6000.6.9f1 estava aberto em Untitled por inventário de processos, mas não havia conexão de automação Unity. Nenhum PlayMode, compilação, teste visual, build ou geração foi executado nesta sessão. Teste de silhueta preta a 128 px, contraste medido, deformação de cada pose e ultrawide físico não foram comprovados. Novos assets devem passar pelo [guia de integração](UNITY_INTEGRATION_GUIDE.md).

## Funcionalidades reais versus aparência

- **A — Herói:** modelo, rig/capa, materiais e 13 clips existem. Attack/HeavyAttack são legados disponíveis; combo usa Attack_01/02/03. Movimento, dash, dano, morte, Victory e Special já têm ponte; não pedir outro herói ou suite básica. Coleta e Q são instantâneos e não exigem clip corporal de interação/consumo.
- **B — Inimigos/Boss:** melee, ranged, elite e Warden reais, com 5/5/5/8 clips. Ranged usa cajado e projétil; Boss usa marreta, Slam/Volley, rugido e mudança de tint. A correção da fase 2 visível já consta nas provas atuais. Aprovação artística individual ranged/Warden continua sem registro específico, embora gameplay da release tenha sido aceito.
- **C — Equipamento:** só três espadas de item (+6/+12/+18) e um colete (+10 defesa). Espada fixa Hero, espadas de esqueletos, cajado e marreta já são meshes. Equipar troca atributo, sem binding de troca visual. Novas malhas visíveis são proposta P2, não bloqueio funcional.
- **D — Inventário:** 16 slots, seleção, detalhes fixos, equipamento manual, desequipar/usar/largar1, capacidade e pausa são reais. Falta apresentação com ícones e estados de foco/hover independentes de cor. Não há campo Sprite em ItemDefinition nem Image de item em SlotView: produzir PNG não integra o recurso. Drag-and-drop/reordenação não fazem parte do contrato.
- **E — Poções:** somente vida, uma definição com potência base15 e stacks de até5 por potência. Drops 15/20/30/40, Q/Usar só vivo e ferido. Quantidade aparece na bolsa, sem contador HUD; não existe cooldown de poção. Frasco 3D dedicado e VFX são polimento opcional.
- **F — Telas:** menu, opções, pausa, inventário, morte e vitória existem. Status/equipamento são painel lateral; especial fixo não implica skill tree. Sem diálogo ou confirmação aprovada. UI é UGUI nativa, fundos simples, botões dourados e fonte builtin; não classificar como ausente só por não ter uma skin ilustrada.
- **G — HUD:** HP/dash/XP/nível/ouro/arma/estamina/giro/salas/Boss/avisos presentes. Quantidade de poção é o complemento P1 proposto. Informação de inimigo aparece na arena de teste; não há prova de barras por inimigo na release. Números de dano/toast positivo são sugestões possíveis, não requisitos; não foram encomendados.
- **H — Loot:** uma esfera compartilhada com cor, bob/giro, coleta/rejeição reais; falta diferenciação por silhueta. Não há baú nem animação obrigatória de coleta. XP/ouro imediatos; recusa por bolsa cheia mantém drop.
- **I — Ambiente:** pisos/paredes/pilares/portões de pedra/ferro, tochas/runas/musgo e luz já aplicados. Floor gerada tem5 GeneratedArt,10 TorchLight,20 runas,70 musgos na prova API. Obstáculo destrutível é futuro previsto sem comportamento atual. Armadilhas/baús não têm escopo.
- **J — Feedback:** flash/shake/windup/tint fase 2 e telegraph funcionais; AttackVisual e projétil são apresentação simples. Trails, impacto, cura, coleta, nível, Slam e chama animada são P2 de apresentação, sem criar combate novo.

## Divergências registradas (sem alterações nas partes afetadas)

| Registro | Divergência | Tratamento |
| --- | --- | --- |
| D-ART-01 | ART_DIRECTION/AGENT_PROMPT antigos pedem Humanoid; ARCHITECTURE/D-002 e importers atuais fixam Generic. | Manter Generic; conversão interrompida. Não produzir Humanoid como requisito. |
| D-ART-02 | Briefing pede atlas 512; UV atual sobreposta e release preserva malha/materiais. | TEX-ATLAS-HERO/MELEE ausentes porém bloqueados; nenhuma aplicação automática. |
| D-ART-03 | ART_DIRECTION diz nenhuma imagem; arquivos/PROVENANCE comprovam conceitos e estudos atuais. | Reutilizar imagens; não duplicar geração nem editar documento antigo nesta tarefa. |
| D-ART-04 | Catálogos/ART_AND_COMBAT_REQUEST antigos dizem9 clips, Boss invisível e inventário incompleto. | Usar13 clips Hero, fase 2 corrigida e aceite1.0 atuais; histórico não é pendência atual. |
| D-ART-05 | Conceito usa paredes2 m, planta16 m; malhas runtime incluem paredes1,4/3 m e Boss26 m. | Preservar geometria/colliders atuais; dimensões antigas não autorizam redimensionar salas. |
| D-ART-06 | Report API registra ForceText, mas seis .unity no disco são binárias. | Não converter cenas; registrar diferença de cobertura e usar evidência API anterior. |


## Lotes e dependências

1. **Lote0 — reuso/provas:** preservar CHAR/ANIM/ENV existentes, completar origem das duas imagens Hero e registrar aprovação individual ranged/Warden; conferir silhueta/contraste no jogo. Não gerar personagens novos.
2. **Lote1 — P1:** cinco `ICON-ITEM-*` como uma família; em seguida tarefa de integração ItemDefinition→Sprite/SlotView/detalhes e `HUD-POTIONS`. Entregar um ícone de poção piloto, validar a 32/48 px e então fechar a família. Sem alterar regras/valores.
3. **Lote2 — P2 opcional:** formas dedicadas dos cinco itens + ouro/XP, reaproveitando Hero_Sword quando compatível. Só depois integrar seletor de pickup e, se escolhido, binding de equipamento nos sockets. Não encomendar colete vestível sem resolver pesos/encaixe na malha atual.
4. **Lote3 — P2 opcional:** FX-SLASH/SPECIAL/HIT e FX-PROJECTILE-TRAIL; depois cura/coleta/nível/Slam/dash. Preparar apresentação e hooks faltantes em tarefas separadas, pool e reset antes de medir custo.
5. **Lote4 — P2 opcional:** ícones de ações, partes de UI, transições e chama discreta. Skin não depende de nova fonte. Destrutível fica após definição de comportamento. Atlas bloqueados ficam fora dos lotes executáveis até decisão.

## Índice completo dos recursos

O manifesto contém evidências, função, dependências, especificação visual e aceite de todas as entradas; fichas especializadas abaixo repetem os mesmos IDs. Prompts só para30 ausentes, nenhum pedido de regeneração de 96 existentes.

### A — Personagem principal

| ID | Recurso | Status | Prioridade | Tipo | Integração/uso |
| --- | --- | --- | --- | --- | --- |
| CHAR-HERO-MODEL | Hero — modelo | EXISTENTE | P0 | modelo 3D | Integrado; preservar e revalidar após mudanças. |
| CHAR-HERO-RIG | Hero — rig Generic | EXISTENTE | P0 | animação 3D | Integrado; preservar e revalidar após mudanças. |
| CHAR-HERO-MAT | Hero — materiais URP | EXISTENTE | P0 | textura | Integrado; preservar e revalidar após mudanças. |
| ANIM-HERO-IDLE | Hero / Idle | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-WALK | Hero / Walk | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-RUN | Hero / Run | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-ATTACK | Hero / Attack | EXISTENTE | P2 | animação 3D | Disponível/importado, legado fora do combo autoritativo; não encomendar novamente. |
| ANIM-HERO-HEAVYATTACK | Hero / HeavyAttack | EXISTENTE | P2 | animação 3D | Disponível/importado, legado fora do combo autoritativo; não encomendar novamente. |
| ANIM-HERO-ATTACK-01 | Hero / Attack_01 | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-ATTACK-02 | Hero / Attack_02 | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-ATTACK-03 | Hero / Attack_03 | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-SPECIAL | Hero / Special | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-DASH | Hero / Dash | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-HIT | Hero / Hit | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-DEATH | Hero / Death | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-HERO-VICTORY | Hero / Victory | EXISTENTE | P2 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| REF-CONCEPT-01 | Hero_Concept_01.png | EXISTENTE | P2 | textura | Conceito importado; não é modelo ou clip 3D. |
| REF-CONCEPT-02 | Hero_Concept_01_Front.png | EXISTENTE | P2 | textura | Conceito importado; não é modelo ou clip 3D. |
| REF-SURFACE-HERO | Hero — estudo de superfície | EXISTENTE | P2 | textura | Referência importada; não é atlas pronto para aplicar. |
| TEX-ATLAS-HERO | Hero — atlas histórico | AUSENTE | P2 | textura | Produção/integração visual pendente. |
| PROTO-SPRITELAB | Laboratório de sprites Hero | NÃO VERIFICADO | P2 | spritesheet | Protótipo fora das duas cenas de release. |
| PROTO-HERO-BILLBOARD | Experimento Hero Concept Billboard | NÃO VERIFICADO | P2 | textura | Experimento fora das duas cenas de release; surgiu em trabalho externo simultâneo, preservar. |

### B — Inimigos e boss

| ID | Recurso | Status | Prioridade | Tipo | Integração/uso |
| --- | --- | --- | --- | --- | --- |
| CHAR-MELEE-MODEL | SkeletonMelee — modelo | EXISTENTE | P0 | modelo 3D | Integrado; preservar e revalidar após mudanças. |
| CHAR-MELEE-RIG | SkeletonMelee — rig Generic | EXISTENTE | P0 | animação 3D | Integrado; preservar e revalidar após mudanças. |
| CHAR-MELEE-MAT | SkeletonMelee — materiais URP | EXISTENTE | P0 | textura | Integrado; preservar e revalidar após mudanças. |
| ANIM-MELEE-IDLE | SkeletonMelee / Idle | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-MELEE-CHASE | SkeletonMelee / Chase | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-MELEE-ATTACK | SkeletonMelee / Attack | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-MELEE-HIT | SkeletonMelee / Hit | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-MELEE-DEATH | SkeletonMelee / Death | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| CHAR-ELITE-MODEL | SkeletonElite — modelo | EXISTENTE | P0 | modelo 3D | Integrado; preservar e revalidar após mudanças. |
| CHAR-ELITE-RIG | SkeletonElite — rig Generic | EXISTENTE | P0 | animação 3D | Integrado; preservar e revalidar após mudanças. |
| CHAR-ELITE-MAT | SkeletonElite — materiais URP | EXISTENTE | P0 | textura | Integrado; preservar e revalidar após mudanças. |
| ANIM-ELITE-IDLE | SkeletonElite / Idle | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-ELITE-CHASE | SkeletonElite / Chase | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-ELITE-ATTACK | SkeletonElite / Attack | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-ELITE-HIT | SkeletonElite / Hit | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-ELITE-DEATH | SkeletonElite / Death | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| CHAR-RANGED-MODEL | SkeletonRanged — modelo | EXISTENTE | P0 | modelo 3D | Integrado; preservar e revalidar após mudanças. |
| CHAR-RANGED-RIG | SkeletonRanged — rig Generic | EXISTENTE | P0 | animação 3D | Integrado; preservar e revalidar após mudanças. |
| CHAR-RANGED-MAT | SkeletonRanged — materiais URP | EXISTENTE | P0 | textura | Integrado; preservar e revalidar após mudanças. |
| ANIM-RANGED-IDLE | SkeletonRanged / Idle | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-RANGED-CHASE | SkeletonRanged / Chase | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-RANGED-ATTACK | SkeletonRanged / Attack | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-RANGED-HIT | SkeletonRanged / Hit | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-RANGED-DEATH | SkeletonRanged / Death | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| CHAR-WARDEN-MODEL | Warden — modelo | EXISTENTE | P0 | modelo 3D | Integrado; preservar e revalidar após mudanças. |
| CHAR-WARDEN-RIG | Warden — rig Generic | EXISTENTE | P0 | animação 3D | Integrado; preservar e revalidar após mudanças. |
| CHAR-WARDEN-MAT | Warden — materiais URP | EXISTENTE | P0 | textura | Integrado; preservar e revalidar após mudanças. |
| ANIM-WARDEN-IDLE | Warden / Idle | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-WARDEN-WALK | Warden / Walk | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-WARDEN-SLAMWINDUP | Warden / SlamWindup | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-WARDEN-SLAM | Warden / Slam | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-WARDEN-VOLLEY | Warden / Volley | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-WARDEN-ROAR | Warden / Roar | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-WARDEN-HIT | Warden / Hit | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| ANIM-WARDEN-DEATH | Warden / Death | EXISTENTE | P0 | animação 3D | Estado/blend tree e ponte presentes; cobertura histórica indicada no guia. |
| REF-CONCEPT-03 | SkeletonMelee_Concept_01.png | EXISTENTE | P2 | textura | Conceito importado; não é modelo ou clip 3D. |
| REF-CONCEPT-04 | SkeletonElite_Concept_01.png | EXISTENTE | P2 | textura | Conceito importado; não é modelo ou clip 3D. |
| REF-CONCEPT-05 | SkeletonRanged_Concept_01.png | EXISTENTE | P2 | textura | Conceito importado; não é modelo ou clip 3D. |
| REF-CONCEPT-06 | Warden_Concept_01.png | EXISTENTE | P2 | textura | Conceito importado; não é modelo ou clip 3D. |
| REF-CONCEPT-07 | Warden_Phase2_Concept_01.png | EXISTENTE | P2 | textura | Conceito importado; não é modelo ou clip 3D. |
| REF-SURFACE-MELEE | SkeletonMelee — estudo de superfície | EXISTENTE | P2 | textura | Referência importada; não é atlas pronto para aplicar. |
| TEX-ATLAS-MELEE | SkeletonMelee — atlas histórico | AUSENTE | P2 | textura | Produção/integração visual pendente. |

### C — Armas e equipamentos

| ID | Recurso | Status | Prioridade | Tipo | Integração/uso |
| --- | --- | --- | --- | --- | --- |
| GEAR-HERO-BASE | Espada visual básica | EXISTENTE | P0 | modelo 3D | Integrado ao modelo; não há binding de troca por ItemDefinition. |
| GEAR-ENEMY-SWORD | Espada enferrujada melee/elite | EXISTENTE | P0 | modelo 3D | Integrado ao modelo; não há binding de troca por ItemDefinition. |
| GEAR-RANGED-STAFF | Cajado corrompido | EXISTENTE | P0 | modelo 3D | Integrado ao modelo; não há binding de troca por ItemDefinition. |
| GEAR-WARDEN-MAUL | Marreta do Guardião | EXISTENTE | P0 | modelo 3D | Integrado ao modelo; não há binding de troca por ItemDefinition. |
| ICON-ITEM-IRON | Espada de Ferro — ícone | AUSENTE | P1 | ícone 2D | Produção/integração visual pendente. |
| MODEL-ITEM-IRON | Espada de Ferro — malha dedicada | AUSENTE | P2 | modelo 3D | Sugestão; pickup usa esfera e equipar aplica atributos sem trocar malha. |
| ICON-ITEM-WARDEN | Lâmina do Guardião — ícone | AUSENTE | P1 | ícone 2D | Produção/integração visual pendente. |
| MODEL-ITEM-WARDEN | Lâmina do Guardião — malha dedicada | AUSENTE | P2 | modelo 3D | Sugestão; pickup usa esfera e equipar aplica atributos sem trocar malha. |
| ICON-ITEM-ANCIENT | Fio Antigo — ícone | AUSENTE | P1 | ícone 2D | Produção/integração visual pendente. |
| MODEL-ITEM-ANCIENT | Fio Antigo — malha dedicada | AUSENTE | P2 | modelo 3D | Sugestão; pickup usa esfera e equipar aplica atributos sem trocar malha. |

### D — Inventário e itens

| ID | Recurso | Status | Prioridade | Tipo | Integração/uso |
| --- | --- | --- | --- | --- | --- |
| ICON-ITEM-LEATHER | Colete de couro — ícone | AUSENTE | P1 | ícone 2D | Produção/integração visual pendente. |
| MODEL-ITEM-LEATHER | Colete de couro — malha dedicada | AUSENTE | P2 | modelo 3D | Sugestão; pickup usa esfera e equipar aplica atributos sem trocar malha. |
| UI-INVENTORY | Inventário | INCOMPLETO | P1 | interface | Integrado e funcional; apresentação parcial. |
| UI-SLOTS | Slots e estados | INCOMPLETO | P1 | interface | Integrado e funcional; apresentação parcial. |
| UI-EQUIPMENT | Equipamentos e status | EXISTENTE | P0 | interface | Integrado; preservar e revalidar após mudanças. |
| UI-DETAILS | Detalhes do item selecionado | EXISTENTE | P0 | interface | Integrado; preservar e revalidar após mudanças. |

### E — Poções e consumíveis

| ID | Recurso | Status | Prioridade | Tipo | Integração/uso |
| --- | --- | --- | --- | --- | --- |
| ICON-ITEM-HEALTH | Poção de vida — ícone | AUSENTE | P1 | ícone 2D | Produção/integração visual pendente. |
| MODEL-ITEM-HEALTH | Poção de vida — malha dedicada | AUSENTE | P2 | modelo 3D | Sugestão; pickup usa esfera e equipar aplica atributos sem trocar malha. |
| HUD-POTIONS | Atalho/quantidade de poções no HUD | AUSENTE | P1 | interface | Produção/integração visual pendente. |

### F — Menus e interfaces

| ID | Recurso | Status | Prioridade | Tipo | Integração/uso |
| --- | --- | --- | --- | --- | --- |
| UI-MAIN | Menu principal | EXISTENTE | P0 | interface | Integrado; preservar e revalidar após mudanças. |
| UI-OPTIONS | Configurações | EXISTENTE | P0 | interface | Integrado; preservar e revalidar após mudanças. |
| UI-PAUSE | Pausa | EXISTENTE | P0 | interface | Integrado; preservar e revalidar após mudanças. |
| UI-DEATH | Tela de morte | EXISTENTE | P0 | interface | Integrado; preservar e revalidar após mudanças. |
| UI-VICTORY | Tela de vitória | EXISTENTE | P0 | interface | Integrado; preservar e revalidar após mudanças. |
| UI-TYPE | Tipografia e widgets atuais | EXISTENTE | P0 | interface | Integrado; preservar e revalidar após mudanças. |
| UI-SKIN | Partes modulares da UI | AUSENTE | P2 | interface | Produção/integração visual pendente. |
| UI-TRANSITION | Transições discretas | AUSENTE | P2 | interface | Produção/integração visual pendente. |

### G — HUD e gameplay

| ID | Recurso | Status | Prioridade | Tipo | Integração/uso |
| --- | --- | --- | --- | --- | --- |
| HUD-HP | Vida do jogador | EXISTENTE | P0 | interface | Integrado; preservar e revalidar após mudanças. |
| HUD-DASH | Cooldown do dash | EXISTENTE | P0 | interface | Integrado; preservar e revalidar após mudanças. |
| HUD-STAMINA | Estamina e giro | EXISTENTE | P0 | interface | Integrado; preservar e revalidar após mudanças. |
| HUD-PROGRESSION | XP, nível, ouro e arma | EXISTENTE | P0 | interface | Integrado; preservar e revalidar após mudanças. |
| HUD-FLOOR | Progresso das salas | EXISTENTE | P0 | interface | Integrado; preservar e revalidar após mudanças. |
| HUD-BOSS | Vida do Guardião | EXISTENTE | P0 | interface | Integrado; preservar e revalidar após mudanças. |
| HUD-ENEMY | HP inimigo na arena de teste | EXISTENTE | P2 | interface | Integrado; preservar e revalidar após mudanças. |
| HUD-NOTICE | Avisos de inventário | EXISTENTE | P0 | interface | Integrado; preservar e revalidar após mudanças. |
| ICON-ACTION-DASH | Dash — ícone | AUSENTE | P2 | ícone 2D | Produção/integração visual pendente. |
| ICON-ACTION-SPECIAL | Giro de espada — ícone | AUSENTE | P2 | ícone 2D | Produção/integração visual pendente. |

### H — Loot e recompensas

| ID | Recurso | Status | Prioridade | Tipo | Integração/uso |
| --- | --- | --- | --- | --- | --- |
| LOOT-SHARED | Pickup compartilhado — esfera e movimento | INCOMPLETO | P2 | modelo 3D | Integrado e utilizável como placeholder. |
| MODEL-LOOT-GOLD | Ouro — forma dedicada | AUSENTE | P2 | modelo 3D | Produção/integração visual pendente. |
| MODEL-LOOT-XP | Orbe de XP — forma dedicada | AUSENTE | P2 | modelo 3D | Produção/integração visual pendente. |

### I — Cenários e objetos interativos

| ID | Recurso | Status | Prioridade | Tipo | Integração/uso |
| --- | --- | --- | --- | --- | --- |
| REF-CONCEPT-08 | DungeonRoom_Concept_01.png | EXISTENTE | P2 | textura | Conceito importado; não é modelo ou clip 3D. |
| REF-CONCEPT-09 | DungeonRoom_ModularKit_01.png | EXISTENTE | P2 | textura | Conceito importado; não é modelo ou clip 3D. |
| ENV-FLOOR | Pisos de pedra | EXISTENTE | P0 | modelo 3D | Integrado; preservar e revalidar após mudanças. |
| ENV-WALL | Paredes e pilares | EXISTENTE | P0 | modelo 3D | Integrado; preservar e revalidar após mudanças. |
| ENV-GATE | Portão de ferro | EXISTENTE | P0 | modelo 3D | Integrado; preservar e revalidar após mudanças. |
| ENV-TORCH | Tocha | EXISTENTE | P1 | modelo 3D | Integrado; preservar e revalidar após mudanças. |
| ENV-RUNE | Runas | EXISTENTE | P1 | modelo 3D | Integrado; preservar e revalidar após mudanças. |
| ENV-MOSS | Musgo | EXISTENTE | P1 | modelo 3D | Integrado; preservar e revalidar após mudanças. |
| ENV-ROOMS | Salas e arena Boss | EXISTENTE | P0 | modelo 3D | Integrado; preservar e revalidar após mudanças. |
| ENV-LIGHT | Iluminação e materiais de ambiente | EXISTENTE | P0 | textura | Integrado; preservar e revalidar após mudanças. |
| ENV-OCCLUSION | Ocultação de paredes | EXISTENTE | P0 | interface | Integrado; preservar e revalidar após mudanças. |
| ANIM-GATE | Portão abrir/fechar | EXISTENTE | P0 | animação 3D | Integrado; preservar e revalidar após mudanças. |
| MODEL-DESTRUCTIBLE | Obstáculo destrutível futuro | AUSENTE | P2 | modelo 3D | Produção/integração visual pendente. |

### J — VFX e animações adicionais

| ID | Recurso | Status | Prioridade | Tipo | Integração/uso |
| --- | --- | --- | --- | --- | --- |
| ANIM-TORCH | Loop discreto da chama | AUSENTE | P2 | animação 3D | Produção/integração visual pendente. |
| FX-DAMAGE | Flash de dano/windup | EXISTENTE | P0 | VFX | Integrado; preservar e revalidar após mudanças. |
| FX-SHAKE | Impulso de câmera | EXISTENTE | P0 | VFX | Integrado; preservar e revalidar após mudanças. |
| FX-BOSS-PHASE | Transição e cor fase 2 | EXISTENTE | P0 | VFX | Integrado; preservar e revalidar após mudanças. |
| FX-TELEGRAPH | Aviso de ataque Boss | EXISTENTE | P0 | VFX | Integrado; preservar e revalidar após mudanças. |
| FX-BASIC | AttackVisual básico | INCOMPLETO | P2 | VFX | Produção/integração visual pendente. |
| FX-PROJECTILE | Projétil inimigo/Boss | INCOMPLETO | P2 | VFX | Produção/integração visual pendente. |
| FX-SLASH | Rastro do combo | AUSENTE | P2 | VFX | Produção/integração visual pendente. |
| FX-SPECIAL | Rastro do giro | AUSENTE | P2 | VFX | Produção/integração visual pendente. |
| FX-HIT | Impacto de acerto | AUSENTE | P2 | VFX | Produção/integração visual pendente. |
| FX-HEAL | Recuperação de vida | AUSENTE | P2 | VFX | Produção/integração visual pendente. |
| FX-LOOT | Coleta aceita | AUSENTE | P2 | VFX | Produção/integração visual pendente. |
| FX-LEVEL | Subida de nível | AUSENTE | P2 | VFX | Produção/integração visual pendente. |
| FX-SLAM | Impacto Slam | AUSENTE | P2 | VFX | Produção/integração visual pendente. |
| FX-DASH | Rastro do dash | AUSENTE | P2 | VFX | Produção/integração visual pendente. |
| FX-PROJECTILE-TRAIL | Cauda do projétil | AUSENTE | P2 | VFX | Produção/integração visual pendente. |

## Fora do escopo

| Recurso consultado | Motivo para não produzir |
| --- | --- |
| Machados, arcos, novas classes de arma | Sem dados ou comportamento aprovado. Cajado e marreta são props de inimigos. |
| Capacetes, escudos, acessórios e slots adicionais | Aprovação: uma arma e uma armadura. Elmo do Warden não é item. |
| Poções de mana/estamina e cooldown de poção | Somente Potion_Health; estamina regenera; poção não tem cooldown. |
| Quests/itens de missão/NPCs/diálogos | Sem sistema/escopo aprovado encontrado. |
| Baús, armadilhas e explosões | Não previstos nas fontes aprovadas; não encomendar. |
| Skill tree/tela de habilidades/status separado | Um especial fixo; status no inventário/HUD, sem pontos distribuíveis. |
| Drag-and-drop, ordenar/mover slots | Não obrigatório nem implementado. Selecionar/largar não é reordenar. |
| Confirmação de saída/reinício | Sem modal aprovado/implementado; melhoria de produto, não requisito visual. |
| Animações corporais de consumir/interagir/coletar | Q cura instantaneamente; coleta por trigger. Clip obrigatório alteraria regra. Não há interação manual E (E é giro). |
| Carregamento, minimapa, save campanha, mobile/gamepad | Não ampliar escopo Windows teclado/mouse 1.0. |

## Atualizações externas e verificação documental final

Durante a inspeção surgiram `Mat_Hero_Concept_Billboard.mat`, `Art_Test_Hero.unity` e seus meta, além de mudanças nos dois meta de conceitos Hero e atualizações dos relatórios correntes. Nenhum desses arquivos foi escrito por esta auditoria. O experimento foi incluído como PROTO-HERO-BILLBOARD, NÃO VERIFICADO e fora da release; imagem em billboard não substitui herói skinned. O índice físico final inclui 461 arquivos sem meta, excluindo os próprios entregáveis ProductionSpecs.

A escrita desta tarefa ficou restrita aos oito documentos autorizados. Foram comparados hashes de 1.323 arquivos versionados da inspeção inicial: quatro diferiram durante trabalho externo simultâneo (dois meta Hero e os dois relatórios já citados). Essa observação não é uma alegação de projeto globalmente imutável; nenhum script, prefab, cena, material ou configuração foi editado por esta auditoria.

Checagens documentais: JSON válido, IDs únicos, dependências existentes e sem ciclos, evidências com caminhos existentes, links/âncoras locais válidos e todos os 30 ausentes com ficha e prompt correspondente. O checker game-docs passou com 0 erros/0 avisos; somente nota informativa sobre o GDD original não usar índice de features. Não houve testes novos da Unity.

O relatório de release registra 496 avisos de shaders, enquanto o windows-build.json corrente reporta 0 avisos/0 erros; são registros distintos, sem causa presumida ou compilação nova nesta auditoria. O manifesto registra o timestamp/hash do relatório API efetivamente lido.

Confirmação do usuário em 14/11/2026: “Sim, preservar e concluir os documentos”. As alterações externas permanecem preservadas; o experimento PROTO-HERO-BILLBOARD continua NÃO VERIFICADO, sem nova validação no Editor.
