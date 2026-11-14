# Itens, equipamentos, inventário, poções e loot

Data: 14/11/2026. Auditoria estática com provas anteriores da 1.0. **NÃO VALIDADO NO EDITOR nesta sessão.** Layout/estados da bolsa em UI_UX_ASSET_SPECS; prompts em ASSET_GENERATION_PROMPTS e campos estruturados no manifesto.

## Lote ITEMS-COMBAT — estado em 14/11/2026 (produção, importação, integração e validação)

Esta seção atualiza as fichas abaixo; o texto original das fichas é preservado como histórico da auditoria estática.

| ID | Produzido | Importado | Integrado | Validado |
| --- | --- | --- | --- | --- |
| MODEL-ITEM-IRON / WARDEN / ANCIENT | Sim: `ItemModels.mjs` (224–348 tris), FBX + GLB de revisão | `Art/Items/Models` | Pickup por catálogo; equipada no `Socket_Hand_R`, `Hero_Sword` oculta | Editor + testes; build |
| MODEL-ITEM-LEATHER | Sim: colete de chão e variante skinned (`Hero_LeatherVest`, 588 tris, Hips/Spine/Chest) | Sim | Pickup; `Hero_Model/Equip_LeatherVest` nos ossos do Hero | Editor + testes; build |
| MODEL-ITEM-HEALTH | Sim (286 tris) | Sim | Pickup de todas as potências (um modelo) | Editor + testes; build |
| MODEL-LOOT-GOLD / XP | Sim (200 / 152 tris) | Sim | Pickup por `LootKind` | Editor + testes; build |
| ICON-ITEM-IRON / WARDEN / ANCIENT / LEATHER / HEALTH | Sim: render dos mesmos modelos pela página/Playwright do Lemo, PNG RGBA 512, alpha real | Sprite em `Art/Items/Icons` | Slots, detalhes, equipamento, HUD de poções | Editor + testes; build |
| HUD-POTIONS, UI-SLOTS, UI-INVENTORY, LOOT-SHARED | Código/UGUI | — | Cena da release | Editor + testes |

Associação explícita: `ScriptableObjects/Items/ItemVisualCatalog.asset` (ItemDefinition/LootKind → modelo de chão → visual equipado → ícone), lida por `PickupVisualPresenter`, `InventoryPresenter`/`InventorySlotView`, `PotionHudPresenter` e pelo integrador que monta `PlayerEquipmentVisualPresenter`. Nenhuma busca global; nenhum atributo mudou (+6/+12/+18, defesa +10, pilhas de 5 por potência, 16 slots, Q).

Integração idempotente: **Tools/Dungeon Crawler/Production Visual/Items & Combat/Run 1-2-3** (importar staging → integrar → validar 67/67). `Build/Floor Scene (M2)` reaplica a integração na cena da release e o pickup reconstruído recebe o presenter.

Evidências: `Reports/Leadership/ProductionVisual/validation.json`, `EditMode-items-combat.xml` (99/99), `PlayMode-items-combat.xml` (74/74), `PlayMode-items-combat-rerun.xml` (5/5 após o último ajuste), `Captures/*.png` (PlayMode na cena real), `windows-build.log` e `windows-player-stress.log`. Staging/fontes: `Art/ProductionStaging/ITEMS-COMBAT/Source~`.

Limites: aprovação artística humana pendente; espadas/colete equipados não observados na build (só no PlayMode); espadas e frasco usam 3–5 materiais (meta da ficha: 1–2); fonte editável em `.mjs`, sem `.blend`.

**Conferência na build Windows (14/11/2026, controle autorizado pelo usuário):** partida normal até a sala 2 em 1920×1080 tela cheia e em 1280×720 janela (opções restauradas para 1920×1080/tela cheia/Alta ao final). Observados: drops reais de ouro (pilha de moedas), XP (orbe com anel) e poção; Largar 1 recria o frasco no chão; pilha ×2 da mesma potência com ícone, "Cura 15" e detalhes com ícone; HUD de poções ×N/Pronto/Sem poções/Indisponível (morto); Q consome uma unidade; layouts de 720p e 1080p sem cortes. No modo diagnóstico `-dcc-stress` da build: rastro na espada, faíscas/lascas em acertos e anel do giro, sem o retângulo branco. Logs do player sem exceções/Missing Script. Capturas em `Reports/Leadership/ProductionVisual/Captures/Build`. **Não observado na build:** espadas e colete equipados (nenhuma arma/armadura dropou até a sala 2; o visual equipado foi verificado no PlayMode da mesma cena).

## Catálogo real e regras preservadas

| Definição | Nome | Raridade | Atributo | Recurso visual atual |
| --- | --- | --- | --- | --- |
| Weapon_IronSword | Espada de Ferro | Comum | +6 dano | Nome na bolsa; sem ícone/modelo próprio ligado; Hero mantém espada fixa |
| Weapon_WardenBlade | Lâmina do Guardião | Raro | +12 dano | Mesmo limite de apresentação |
| Weapon_AncientEdge | Fio Antigo | Épico | +18 dano | Mesmo limite; épico não aplica bônus oculto |
| Armor_Leather | Colete de couro | Comum | +10 defesa | Nome/status; malha do corpo não troca |
| Potion_Health | Poção de vida | Comum | Default15 HP, stack 5 por potência | Nome/quantidade selecionada; esfera rosa no chão |

Fontes: Assets/_Project/ScriptableObjects/Items/*.asset, Loot/*.asset e docs/PRODUCT-1.0-PROPOSAL.md. ItemDefinition só conhece Weapon/Armor/Potion. Armas/armadura ocupam um slot cada. Há 16 posições fixas, poções com mesma definição+potência empilham até5; potências diferentes não se fundem. Ouro/XP são imediatos, não ocupam bolsa. Uma arma pode equipar automaticamente somente se slot de arma vazio; demais trocas são manuais. Equipamento permanece na bolsa, identificado pelo índice; não duplicar item ao criar ícone da área de equipamento.

Dano usa `(base+bônus arma)×multiplicador de nível×multiplicador do golpe`, progressão+10% por nível preservada. Defesa positiva usa `max(1, dano×100/(100+defesa))`. A raridade só apresenta nome/cor/rank, sem multiplicador oculto. Comum RGB(.78,.82,.87), Raro(.3,.72,1), Épico(.85,.55,1); manter cores dos SO e sempre nome/forma adicional para leitura sem cor.

## Poções e drops

| Tabela | Conteúdo relevante atual |
| --- | --- |
| Melee | Ouro1–3 chance .5; poção15 HP chance .1; colete chance .1 |
| Ranged | Ouro2–4 chance 1; poção20 HP chance .15; colete chance .1 |
| Elite | Ouro5–10 chance 1; poção30 HP chance .4; Espada de Ferro chance .25; Fio Antigo chance .1 |
| Boss | Ouro25–40 chance 1; poção40 HP chance 1; Lâmina do Guardião chance 1 |

XP é concedido pelo sistema de recompensa de inimigo; não confundir a entrada Gold do LootTable com XP. As potências15/20/30/40 não são quatro definições ou quatro tipos de frasco: **um ícone e um modelo de vida bastam**, com valor dinâmico nos detalhes. O consumoQ usa a primeira poção elegível segundo o modelo atual; não prometer escolha de maior/menor potência. HP cheio/morto não consomem, cura não é adiada por clip e não há cooldown de poção. Não produzir mana/estamina: estamina já regenera após o especial.

## Pipeline real de visual

`LootDrop → Pickup.Setup → tint no Renderer → trigger → PlayerProgression.TryCollect → PlayerInventory.TryStore`. Só depois de sucesso o pickup é removido. Rejeição por capacidade preserva o drop e permite retry; `CollectionDenied` mostra aviso. `PlayerInventory.Changed` aplica bônus da arma/armadura e refresca UI, sem substituir mesh.

O pickup usa uma esfera compartilhada, bob .15 m e giro 120°/s; arma/armadura recebem cor de raridade, outros tipos cor própria. `Collected(LootKind)` não contém identidade, quantidade e posição completa do item, portanto um futuro toast rico exige payload/binding em tarefa de código. `ItemDropped(Pickup)` existe para feedback de largar. Largar 1 verifica oito direções por raycast e mantém item se não houver espaço; não é arrastar e não move um slot para outro.

## Trabalho necessário para usar novos ícones

1. Produzir os cinco ICON-ITEM como família; preservar nomes, SOs e atributos existentes.
2. Em tarefa separada, criar mapa visual serializado ItemDefinition→Sprite, ou extensão equivalente explicitamente delimitada; ItemDefinition atual não possui campo icon.
3. SlotView precisa de Image de item além de label/background. Atualizar por estado do stack/seleção, manter quantidade e Equipado como texto/símbolo separados. A arte não calcula capacidade nem atributos.
4. Detalhes usam o mesmo ícone. Aplicar backgrounds de raridade fora do PNG, sem criar variantes de imagem por qualidade. Garantir vazio/indisponível/selecionado/equipado com indicadores independentes.
5. HUD-POTIONS exibe total, Q e indisponibilidade; somar stacks sem fundi-los e sem mudar consumo.

## Modelos e equipamento visível (P2 opcional)

GEAR-HERO-BASE é uma espada3D já existente; verificar encaixe/reuso antes de modelar a Espada de Ferro. Cajado/marreta/espadas de inimigos são props existentes, não itens coletáveis. Três espadas dedicadas, colete, frasco, ouro e XP são propostas de apresentação; o modelo de chão e o visual equipado podem compartilhar malha quando adequado.

Para trocar a espada no Hero, um integrador precisa ocultar a espada fixa e anexar só uma malha ao Socket_Hand_R, com escala/pivô testados nos 13 clips. Para colete visível, usar variante skinned no esqueleto atual, preservar capa e roupa base, validar clipping em Run/Attack_03/Special/Death. Não habilitar Optimize Game Objects sem preservar sockets necessários. Um colete estático para chão não satisfaz a variante vestível. Nenhuma mudança de malha, rig ou materiais aprovados foi autorizada nesta auditoria.

## Aceite de inventário e recompensas

Testar16 slots vazios/cheios, stacks1/5 e potências diferentes, primeira arma/troca/desequipar/armadura, HP cheio/ferido/morto, Q dentro/fora da bolsa, largar em espaço livre/bloqueado, recusa de pickup e nova coleta após liberar capacidade. Verificar que clique na UI não dispara combo, I/Tab pausa e Esc retorna. Exigir ícones legíveis a 32 px com alpha real e silhuetas distintas de ouro/XP/projétil. Campos `acceptance_criteria` de cada ID complementam esses passos; execução não realizada aqui.

Sem baús, quests, arco/machado, escudos, capacetes/acessórios, mana ou drag-and-drop obrigatório. Sugestões de produto não entram no lote de produção como requisitos.

## Fichas de produção e reuso

## GEAR-HERO-BASE — Espada visual básica

**Categoria:** C — Armas e equipamentos. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Prop do personagem, não definição de item

**Integração:** Integrado ao modelo; não há binding de troca por ItemDefinition.

**Visual:** Malha Hero_Sword em socket da mão; acompanha animações.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Hero/Hero.fbx](../../../../Assets/_Project/Art/Models/Generated/Hero/Hero.fbx) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Arma acompanha a mão sem deslizar/sumir.
- Cajado/marreta não criam classes jogáveis nem slots extras.


## GEAR-ENEMY-SWORD — Espada enferrujada melee/elite

**Categoria:** C — Armas e equipamentos. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Prop do personagem, não definição de item

**Integração:** Integrado ao modelo; não há binding de troca por ItemDefinition.

**Visual:** Malha RustySword em socket da mão; acompanha animações.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.fbx](../../../../Assets/_Project/Art/Models/Generated/SkeletonMelee/SkeletonMelee.fbx) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Arma acompanha a mão sem deslizar/sumir.
- Cajado/marreta não criam classes jogáveis nem slots extras.


## GEAR-RANGED-STAFF — Cajado corrompido

**Categoria:** C — Armas e equipamentos. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Prop do personagem, não definição de item

**Integração:** Integrado ao modelo; não há binding de troca por ItemDefinition.

**Visual:** Malha CorruptedStaff em socket da mão; acompanha animações.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged_manifest.json](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.fbx](../../../../Assets/_Project/Art/Models/Generated/SkeletonRanged/SkeletonRanged.fbx) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Arma acompanha a mão sem deslizar/sumir.
- Cajado/marreta não criam classes jogáveis nem slots extras.


## GEAR-WARDEN-MAUL — Marreta do Guardião

**Categoria:** C — Armas e equipamentos. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Prop do personagem, não definição de item

**Integração:** Integrado ao modelo; não há binding de troca por ItemDefinition.

**Visual:** Malha Maul em socket da mão; acompanha animações.

**Evidências:**

- [Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden_manifest.json) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Art/Models/Generated/Warden/Warden.fbx](../../../../Assets/_Project/Art/Models/Generated/Warden/Warden.fbx) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Arma acompanha a mão sem deslizar/sumir.
- Cajado/marreta não criam classes jogáveis nem slots extras.


## ICON-ITEM-IRON — Espada de Ferro — ícone

**Categoria:** C — Armas e equipamentos. **Status:** AUSENTE. **Prioridade:** P1. **Tipo:** ícone 2D. **Escopo:** aprovado_1_0.

**Finalidade:** Identidade visual do item em slots e detalhes

**Integração:** Produção/integração visual pendente.

**Visual:** Short straight steel sword, simple brass crossguard, dark leather grip, broad readable blade; match the existing Hero_Sword family, no magical glow.

**Evidências:**

- [Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset](../../../../Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset) — Item real: Comum, valor explícito 6.
- [Assets/_Project/Scripts/Systems/Inventory/ItemDefinition.cs](../../../../Assets/_Project/Scripts/Systems/Inventory/ItemDefinition.cs) — Sem campo Sprite/ícone.
- [Assets/_Project/Scripts/UI/InventorySlotView.cs](../../../../Assets/_Project/Scripts/UI/InventorySlotView.cs) — Render textual, sem Image de item.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Integração técnica pendente:**

- Tarefa futura: mapa serializado ItemDefinition→Sprite, Image dedicado no SlotView/detalhes. Arte não adiciona esse binding sozinha.

**Dimensões/tempo:** 512×512; leitura 48 e 32 px

**Entrega:** PNG RGBA + fonte editável

**Contrato Unity:** PNG RGBA 512×512 + fonte; alpha real, conteúdo 416×416 central/margem 48 px. Sprite Single, sRGB, mipmaps off, bilinear, clamp, PPU 100; testar a 48/32 px. Sem texto, números ou moldura de raridade baked.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#icon-item-iron) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## MODEL-ITEM-IRON — Espada de Ferro — malha dedicada

**Categoria:** C — Armas e equipamentos. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

**Finalidade:** Forma no chão; equipamento visível é extensão visual opcional

**Integração:** Sugestão; pickup usa esfera e equipar aplica atributos sem trocar malha.

**Visual:** Short straight steel sword, simple brass crossguard, dark leather grip, broad readable blade; match the existing Hero_Sword family, no magical glow.

**Evidências:**

- [Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset](../../../../Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Prefabs/Items/Pickup.prefab](../../../../Assets/_Project/Prefabs/Items/Pickup.prefab) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/Inventory/PlayerInventory.cs](../../../../Assets/_Project/Scripts/Systems/Inventory/PlayerInventory.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** ICON-ITEM-IRON

**Integração técnica pendente:**

- Seletor visual futuro por LootKind/ItemDefinition; Pickup atual recebe um Renderer/tint.
- Troca visual futura por referências/eventos nos sockets; não duplicar Hero_Sword fixa.

**Dimensões/tempo:** 0,85×0,16×0,06 m (proposta a ajustar ao Hero); pivô na empunhadura para espada, base para demais

**Entrega:** FBX + .blend; espadas 300–800 tris, colete 500–1200, frasco 150–350; 1–2 materiais

**Contrato Unity:** Fonte .blend + FBX real; GLB opcional para revisão, sem dependência de importador novo. Metros, Unity +Y cima/+Z frente, transforms aplicadas, pivô conforme ficha, normais facetadas, URP/Lit, sem colliders de gameplay no visual.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#model-item-iron) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## ICON-ITEM-WARDEN — Lâmina do Guardião — ícone

**Categoria:** C — Armas e equipamentos. **Status:** AUSENTE. **Prioridade:** P1. **Tipo:** ícone 2D. **Escopo:** aprovado_1_0.

**Finalidade:** Identidade visual do item em slots e detalhes

**Integração:** Produção/integração visual pendente.

**Visual:** Short broad steel sword, heavier angular guard, one restrained cyan inset. Keep it one-handed, distinct from the boss maul.

**Evidências:**

- [Assets/_Project/ScriptableObjects/Items/Weapon_WardenBlade.asset](../../../../Assets/_Project/ScriptableObjects/Items/Weapon_WardenBlade.asset) — Item real: Raro, valor explícito 12.
- [Assets/_Project/Scripts/Systems/Inventory/ItemDefinition.cs](../../../../Assets/_Project/Scripts/Systems/Inventory/ItemDefinition.cs) — Sem campo Sprite/ícone.
- [Assets/_Project/Scripts/UI/InventorySlotView.cs](../../../../Assets/_Project/Scripts/UI/InventorySlotView.cs) — Render textual, sem Image de item.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Integração técnica pendente:**

- Tarefa futura: mapa serializado ItemDefinition→Sprite, Image dedicado no SlotView/detalhes. Arte não adiciona esse binding sozinha.

**Dimensões/tempo:** 512×512; leitura 48 e 32 px

**Entrega:** PNG RGBA + fonte editável

**Contrato Unity:** PNG RGBA 512×512 + fonte; alpha real, conteúdo 416×416 central/margem 48 px. Sprite Single, sRGB, mipmaps off, bilinear, clamp, PPU 100; testar a 48/32 px. Sem texto, números ou moldura de raridade baked.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#icon-item-warden) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## MODEL-ITEM-WARDEN — Lâmina do Guardião — malha dedicada

**Categoria:** C — Armas e equipamentos. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

**Finalidade:** Forma no chão; equipamento visível é extensão visual opcional

**Integração:** Sugestão; pickup usa esfera e equipar aplica atributos sem trocar malha.

**Visual:** Short broad steel sword, heavier angular guard, one restrained cyan inset. Keep it one-handed, distinct from the boss maul.

**Evidências:**

- [Assets/_Project/ScriptableObjects/Items/Weapon_WardenBlade.asset](../../../../Assets/_Project/ScriptableObjects/Items/Weapon_WardenBlade.asset) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Prefabs/Items/Pickup.prefab](../../../../Assets/_Project/Prefabs/Items/Pickup.prefab) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/Inventory/PlayerInventory.cs](../../../../Assets/_Project/Scripts/Systems/Inventory/PlayerInventory.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** ICON-ITEM-WARDEN

**Integração técnica pendente:**

- Seletor visual futuro por LootKind/ItemDefinition; Pickup atual recebe um Renderer/tint.
- Troca visual futura por referências/eventos nos sockets; não duplicar Hero_Sword fixa.

**Dimensões/tempo:** 0,95×0,22×0,07 m (proposta a ajustar ao Hero); pivô na empunhadura para espada, base para demais

**Entrega:** FBX + .blend; espadas 300–800 tris, colete 500–1200, frasco 150–350; 1–2 materiais

**Contrato Unity:** Fonte .blend + FBX real; GLB opcional para revisão, sem dependência de importador novo. Metros, Unity +Y cima/+Z frente, transforms aplicadas, pivô conforme ficha, normais facetadas, URP/Lit, sem colliders de gameplay no visual.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#model-item-warden) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## ICON-ITEM-ANCIENT — Fio Antigo — ícone

**Categoria:** C — Armas e equipamentos. **Status:** AUSENTE. **Prioridade:** P1. **Tipo:** ícone 2D. **Escopo:** aprovado_1_0.

**Finalidade:** Identidade visual do item em slots e detalhes

**Integração:** Produção/integração visual pendente.

**Visual:** Short broad ancient steel sword, stepped angular guard, one brass geometric inset and subtle wear. No new element or damage type.

**Evidências:**

- [Assets/_Project/ScriptableObjects/Items/Weapon_AncientEdge.asset](../../../../Assets/_Project/ScriptableObjects/Items/Weapon_AncientEdge.asset) — Item real: Épico, valor explícito 18.
- [Assets/_Project/Scripts/Systems/Inventory/ItemDefinition.cs](../../../../Assets/_Project/Scripts/Systems/Inventory/ItemDefinition.cs) — Sem campo Sprite/ícone.
- [Assets/_Project/Scripts/UI/InventorySlotView.cs](../../../../Assets/_Project/Scripts/UI/InventorySlotView.cs) — Render textual, sem Image de item.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Integração técnica pendente:**

- Tarefa futura: mapa serializado ItemDefinition→Sprite, Image dedicado no SlotView/detalhes. Arte não adiciona esse binding sozinha.

**Dimensões/tempo:** 512×512; leitura 48 e 32 px

**Entrega:** PNG RGBA + fonte editável

**Contrato Unity:** PNG RGBA 512×512 + fonte; alpha real, conteúdo 416×416 central/margem 48 px. Sprite Single, sRGB, mipmaps off, bilinear, clamp, PPU 100; testar a 48/32 px. Sem texto, números ou moldura de raridade baked.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#icon-item-ancient) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## MODEL-ITEM-ANCIENT — Fio Antigo — malha dedicada

**Categoria:** C — Armas e equipamentos. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

**Finalidade:** Forma no chão; equipamento visível é extensão visual opcional

**Integração:** Sugestão; pickup usa esfera e equipar aplica atributos sem trocar malha.

**Visual:** Short broad ancient steel sword, stepped angular guard, one brass geometric inset and subtle wear. No new element or damage type.

**Evidências:**

- [Assets/_Project/ScriptableObjects/Items/Weapon_AncientEdge.asset](../../../../Assets/_Project/ScriptableObjects/Items/Weapon_AncientEdge.asset) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Prefabs/Items/Pickup.prefab](../../../../Assets/_Project/Prefabs/Items/Pickup.prefab) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/Inventory/PlayerInventory.cs](../../../../Assets/_Project/Scripts/Systems/Inventory/PlayerInventory.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** ICON-ITEM-ANCIENT

**Integração técnica pendente:**

- Seletor visual futuro por LootKind/ItemDefinition; Pickup atual recebe um Renderer/tint.
- Troca visual futura por referências/eventos nos sockets; não duplicar Hero_Sword fixa.

**Dimensões/tempo:** 0,95×0,24×0,07 m (proposta a ajustar ao Hero); pivô na empunhadura para espada, base para demais

**Entrega:** FBX + .blend; espadas 300–800 tris, colete 500–1200, frasco 150–350; 1–2 materiais

**Contrato Unity:** Fonte .blend + FBX real; GLB opcional para revisão, sem dependência de importador novo. Metros, Unity +Y cima/+Z frente, transforms aplicadas, pivô conforme ficha, normais facetadas, URP/Lit, sem colliders de gameplay no visual.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#model-item-ancient) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## ICON-ITEM-LEATHER — Colete de couro — ícone

**Categoria:** D — Inventário e itens. **Status:** AUSENTE. **Prioridade:** P1. **Tipo:** ícone 2D. **Escopo:** aprovado_1_0.

**Finalidade:** Identidade visual do item em slots e detalhes

**Integração:** Produção/integração visual pendente.

**Visual:** Sleeveless chunky brown leather vest, broad plain panels, dark seams and two brass fasteners; no helmet, shield or accessory set.

**Evidências:**

- [Assets/_Project/ScriptableObjects/Items/Armor_Leather.asset](../../../../Assets/_Project/ScriptableObjects/Items/Armor_Leather.asset) — Item real: Comum, valor explícito 10.
- [Assets/_Project/Scripts/Systems/Inventory/ItemDefinition.cs](../../../../Assets/_Project/Scripts/Systems/Inventory/ItemDefinition.cs) — Sem campo Sprite/ícone.
- [Assets/_Project/Scripts/UI/InventorySlotView.cs](../../../../Assets/_Project/Scripts/UI/InventorySlotView.cs) — Render textual, sem Image de item.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Integração técnica pendente:**

- Tarefa futura: mapa serializado ItemDefinition→Sprite, Image dedicado no SlotView/detalhes. Arte não adiciona esse binding sozinha.

**Dimensões/tempo:** 512×512; leitura 48 e 32 px

**Entrega:** PNG RGBA + fonte editável

**Contrato Unity:** PNG RGBA 512×512 + fonte; alpha real, conteúdo 416×416 central/margem 48 px. Sprite Single, sRGB, mipmaps off, bilinear, clamp, PPU 100; testar a 48/32 px. Sem texto, números ou moldura de raridade baked.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#icon-item-leather) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## MODEL-ITEM-LEATHER — Colete de couro — malha dedicada

**Categoria:** D — Inventário e itens. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

**Finalidade:** Forma no chão; equipamento visível é extensão visual opcional

**Integração:** Sugestão; pickup usa esfera e equipar aplica atributos sem trocar malha.

**Visual:** Sleeveless chunky brown leather vest, broad plain panels, dark seams and two brass fasteners; no helmet, shield or accessory set.

**Evidências:**

- [Assets/_Project/ScriptableObjects/Items/Armor_Leather.asset](../../../../Assets/_Project/ScriptableObjects/Items/Armor_Leather.asset) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Prefabs/Items/Pickup.prefab](../../../../Assets/_Project/Prefabs/Items/Pickup.prefab) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/Inventory/PlayerInventory.cs](../../../../Assets/_Project/Scripts/Systems/Inventory/PlayerInventory.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** ICON-ITEM-LEATHER

**Integração técnica pendente:**

- Seletor visual futuro por LootKind/ItemDefinition; Pickup atual recebe um Renderer/tint.
- Troca visual futura por referências/eventos nos sockets; não duplicar Hero_Sword fixa.

**Dimensões/tempo:** 0,60×0,65×0,25 m (proposta a ajustar ao Hero); pivô na empunhadura para espada, base para demais

**Entrega:** FBX + .blend; espadas 300–800 tris, colete 500–1200, frasco 150–350; 1–2 materiais

**Contrato Unity:** Fonte .blend + FBX real; GLB opcional para revisão, sem dependência de importador novo. Metros, Unity +Y cima/+Z frente, transforms aplicadas, pivô conforme ficha, normais facetadas, URP/Lit, sem colliders de gameplay no visual.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#model-item-leather) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## ICON-ITEM-HEALTH — Poção de vida — ícone

**Categoria:** E — Poções e consumíveis. **Status:** AUSENTE. **Prioridade:** P1. **Tipo:** ícone 2D. **Escopo:** aprovado_1_0.

**Finalidade:** Identidade visual do item em slots e detalhes

**Integração:** Produção/integração visual pendente.

**Visual:** Squat faceted glass flask, broad round body, short neck, cork and dark leather collar; ruby-red liquid, clear highlight, chunky silhouette.

**Evidências:**

- [Assets/_Project/ScriptableObjects/Items/Potion_Health.asset](../../../../Assets/_Project/ScriptableObjects/Items/Potion_Health.asset) — Item real: Comum, valor explícito 15.
- [Assets/_Project/Scripts/Systems/Inventory/ItemDefinition.cs](../../../../Assets/_Project/Scripts/Systems/Inventory/ItemDefinition.cs) — Sem campo Sprite/ícone.
- [Assets/_Project/Scripts/UI/InventorySlotView.cs](../../../../Assets/_Project/Scripts/UI/InventorySlotView.cs) — Render textual, sem Image de item.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Integração técnica pendente:**

- Tarefa futura: mapa serializado ItemDefinition→Sprite, Image dedicado no SlotView/detalhes. Arte não adiciona esse binding sozinha.

**Dimensões/tempo:** 512×512; leitura 48 e 32 px

**Entrega:** PNG RGBA + fonte editável

**Contrato Unity:** PNG RGBA 512×512 + fonte; alpha real, conteúdo 416×416 central/margem 48 px. Sprite Single, sRGB, mipmaps off, bilinear, clamp, PPU 100; testar a 48/32 px. Sem texto, números ou moldura de raridade baked.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#icon-item-health) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## MODEL-ITEM-HEALTH — Poção de vida — malha dedicada

**Categoria:** E — Poções e consumíveis. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

**Finalidade:** Forma no chão; equipamento visível é extensão visual opcional

**Integração:** Sugestão; pickup usa esfera e equipar aplica atributos sem trocar malha.

**Visual:** Squat faceted glass flask, broad round body, short neck, cork and dark leather collar; ruby-red liquid, clear highlight, chunky silhouette.

**Evidências:**

- [Assets/_Project/ScriptableObjects/Items/Potion_Health.asset](../../../../Assets/_Project/ScriptableObjects/Items/Potion_Health.asset) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Prefabs/Items/Pickup.prefab](../../../../Assets/_Project/Prefabs/Items/Pickup.prefab) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/Inventory/PlayerInventory.cs](../../../../Assets/_Project/Scripts/Systems/Inventory/PlayerInventory.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** ICON-ITEM-HEALTH

**Integração técnica pendente:**

- Seletor visual futuro por LootKind/ItemDefinition; Pickup atual recebe um Renderer/tint.
- Troca visual futura por referências/eventos nos sockets; não duplicar Hero_Sword fixa.

**Dimensões/tempo:** 0,22×0,32×0,22 m (proposta a ajustar ao Hero); pivô na empunhadura para espada, base para demais

**Entrega:** FBX + .blend; espadas 300–800 tris, colete 500–1200, frasco 150–350; 1–2 materiais

**Contrato Unity:** Fonte .blend + FBX real; GLB opcional para revisão, sem dependência de importador novo. Metros, Unity +Y cima/+Z frente, transforms aplicadas, pivô conforme ficha, normais facetadas, URP/Lit, sem colliders de gameplay no visual.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#model-item-health) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## LOOT-SHARED — Pickup compartilhado — esfera e movimento

**Categoria:** H — Loot e recompensas. **Status:** INCOMPLETO. **Prioridade:** P2. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

**Finalidade:** Coleta por trigger, rejeição mantém drop

**Integração:** Integrado e utilizável como placeholder.

**Visual:** Esfera visual .4 m: cor por tipo/raridade, bob .15 m, giro 120°/s. Lifetime 45 s; rejeição remove expiração. Funcional, sem silhueta por item.

**Evidências:**

- [Assets/_Project/Prefabs/Items/Pickup.prefab](../../../../Assets/_Project/Prefabs/Items/Pickup.prefab) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/Pickup.cs](../../../../Assets/_Project/Scripts/Systems/Pickup.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/PlayerProgression.cs](../../../../Assets/_Project/Scripts/Systems/PlayerProgression.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Preservar trigger e retry .25 s; item rejeitado permanece no chão.
- Cor não deve ser a única distinção em futuro acabamento.


## MODEL-LOOT-GOLD — Ouro — forma dedicada

**Categoria:** H — Loot e recompensas. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

**Finalidade:** Leitura de recompensa imediata

**Integração:** Produção/integração visual pendente.

**Visual:** A small chunky stack of three brass-gold coins, plain broad bevels, bright upper edge, dark underside, no lettering.

**Evidências:**

- [Assets/_Project/Scripts/Systems/Pickup.cs](../../../../Assets/_Project/Scripts/Systems/Pickup.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/PlayerProgression.cs](../../../../Assets/_Project/Scripts/Systems/PlayerProgression.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** LOOT-SHARED

**Dimensões/tempo:** 0.30 x 0.14 x 0.30 m; pivô base

**Entrega:** FBX + .blend; 100–250 tris, 1 material

**Contrato Unity:** Fonte .blend + FBX real; GLB opcional para revisão, sem dependência de importador novo. Metros, Unity +Y cima/+Z frente, transforms aplicadas, pivô conforme ficha, normais facetadas, URP/Lit, sem colliders de gameplay no visual.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#model-loot-gold) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## MODEL-LOOT-XP — Orbe de XP — forma dedicada

**Categoria:** H — Loot e recompensas. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

**Finalidade:** Leitura de recompensa imediata

**Integração:** Produção/integração visual pendente.

**Visual:** A simple faceted cyan orb, dark lower facet and restrained bright core, clearly distinct from an orange hostile projectile.

**Evidências:**

- [Assets/_Project/Scripts/Systems/Pickup.cs](../../../../Assets/_Project/Scripts/Systems/Pickup.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Systems/PlayerProgression.cs](../../../../Assets/_Project/Scripts/Systems/PlayerProgression.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** LOOT-SHARED

**Dimensões/tempo:** 0.25 m diameter; pivô base

**Entrega:** FBX + .blend; 100–250 tris, 1 material

**Contrato Unity:** Fonte .blend + FBX real; GLB opcional para revisão, sem dependência de importador novo. Metros, Unity +Y cima/+Z frente, transforms aplicadas, pivô conforme ficha, normais facetadas, URP/Lit, sem colliders de gameplay no visual.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#model-loot-xp) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## UI-INVENTORY — Inventário

**Categoria:** D — Inventário e itens. **Status:** INCOMPLETO. **Prioridade:** P1. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado e funcional; apresentação parcial.

**Visual:** Window central 1180×780, título/capacidade, grade 4×4, lateral equipamento/detalhes; ações inferiores. Funcional textual; ícones ausentes.

**Evidências:**

- [Assets/_Project/Scripts/UI/InventoryPresenter.cs](../../../../Assets/_Project/Scripts/UI/InventoryPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE, ICON-ITEM-IRON, ICON-ITEM-WARDEN, ICON-ITEM-ANCIENT, ICON-ITEM-LEATHER, ICON-ITEM-HEALTH

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## UI-SLOTS — Slots e estados

**Categoria:** D — Inventário e itens. **Status:** INCOMPLETO. **Prioridade:** P1. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado e funcional; apresentação parcial.

**Visual:** 16 slots 188×104, passo 200×116; nome/raridade/quantidade/Equipado; vazio com travessão. Seleção por fundo; Transition.None sem hover/focus distinto.

**Evidências:**

- [Assets/_Project/Scripts/UI/InventorySlotView.cs](../../../../Assets/_Project/Scripts/UI/InventorySlotView.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE, ICON-ITEM-IRON, ICON-ITEM-WARDEN, ICON-ITEM-ANCIENT, ICON-ITEM-LEATHER, ICON-ITEM-HEALTH

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## UI-EQUIPMENT — Equipamentos e status

**Categoria:** D — Inventário e itens. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Resumo lateral textual: arma/armadura/dano/defesa/HP; não há paper doll nem tela própria.

**Evidências:**

- [Assets/_Project/Scripts/UI/InventoryPresenter.cs](../../../../Assets/_Project/Scripts/UI/InventoryPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## UI-DETAILS — Detalhes do item selecionado

**Categoria:** D — Inventário e itens. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Nome, raridade, atributo, quantidade e ajuda. Painel fixo, não tooltip hover.

**Evidências:**

- [Assets/_Project/Scripts/UI/InventoryPresenter.cs](../../../../Assets/_Project/Scripts/UI/InventoryPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## HUD-POTIONS — Atalho/quantidade de poções no HUD

**Categoria:** E — Poções e consumíveis. **Status:** AUSENTE. **Prioridade:** P1. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Leitura de Q sem abrir inventário

**Integração:** Produção/integração visual pendente.

**Visual:** Proposta: ícone frasco 48×48, Q e total; zero/HP cheio/morto indisponível, sem sugerir cooldown.

**Evidências:**

- [Assets/_Project/Scripts/UI/InventoryPresenter.cs](../../../../Assets/_Project/Scripts/UI/InventoryPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Player/PlayerInventoryInput.cs](../../../../Assets/_Project/Scripts/Player/PlayerInventoryInput.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/UI/PlayerResourcePresenter.cs](../../../../Assets/_Project/Scripts/UI/PlayerResourcePresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** ICON-ITEM-HEALTH, HUD-STAMINA

**Integração técnica pendente:**

- Presenter futuro somente leitura em Inventory.Changed/Health.Changed; não mudar escolha de Q nem capacidade.

**Dimensões/tempo:** Componente 160×64 no canvas 1920×1080; âncora superior esquerda/margem 24, proposta y=-180

**Entrega:** UGUI editável; ícone referenciado; textos runtime

**Contrato Unity:** UGUI editável; sprites e texto separados, referência 1920×1080/CanvasScaler match .5. Preservar Text/LegacyRuntime.ttf, InputSystemUIInputModule, pilha de menus e presenters atuais; imagem achatada não é interface funcional.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#hud-potions) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.
