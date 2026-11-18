# Itens, equipamentos, inventário, poções e loot

> Consolidação de 14/11/2026: fichas repetidas foram substituídas por índices por ID; os dados e prompts completos permanecem no manifesto, sem alteração. Ler somente o registro necessário. Lotes ANIM-TORCH e ITEMS-COMBAT já foram produzidos/integrados; o diagnóstico introdutório antigo abaixo é histórico onde divergir desses lotes. Novas sugestões prontas para escolha estão em [PROMPT_CATALOG](../../../../PROMPT_CATALOG.md).

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

Testar16 slots vazios/cheios, stacks1/5 e potências diferentes, arma inicial equipada/troca/desequipar (D-007: equipamento fora da bolsa; desequipar com bolsa cheia é recusado)/armadura, HP cheio/ferido/morto, Q dentro/fora da bolsa, largar em espaço livre/bloqueado, recusa de pickup e nova coleta após liberar capacidade. Verificar que clique na UI não dispara combo, I/Tab pausa e Esc retorna. Exigir ícones legíveis a 32 px com alpha real e silhuetas distintas de ouro/XP/projétil. Campos `acceptance_criteria` de cada ID complementam esses passos; execução não realizada aqui.

Sem baús, quests, arco/machado, escudos, capacetes/acessórios, mana ou drag-and-drop obrigatório. Sugestões de produto não entram no lote de produção como requisitos.

## Fichas de produção e reuso

<a id="gear-hero-base"></a>

## GEAR-HERO-BASE — Espada visual básica

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=GEAR-HERO-BASE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="gear-enemy-sword"></a>

## GEAR-ENEMY-SWORD — Espada enferrujada melee/elite

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=GEAR-ENEMY-SWORD]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="gear-ranged-staff"></a>

## GEAR-RANGED-STAFF — Cajado corrompido

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=GEAR-RANGED-STAFF]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="gear-warden-maul"></a>

## GEAR-WARDEN-MAUL — Marreta do Guardião

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=GEAR-WARDEN-MAUL]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="icon-item-iron"></a>

## ICON-ITEM-IRON — Espada de Ferro — ícone

**Estado atual:** EXISTENTE. **Tipo:** ícone 2D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ICON-ITEM-IRON]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="model-item-iron"></a>

## MODEL-ITEM-IRON — Espada de Ferro — malha dedicada

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=MODEL-ITEM-IRON]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="icon-item-warden"></a>

## ICON-ITEM-WARDEN — Lâmina do Guardião — ícone

**Estado atual:** EXISTENTE. **Tipo:** ícone 2D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ICON-ITEM-WARDEN]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="model-item-warden"></a>

## MODEL-ITEM-WARDEN — Lâmina do Guardião — malha dedicada

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=MODEL-ITEM-WARDEN]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="icon-item-ancient"></a>

## ICON-ITEM-ANCIENT — Fio Antigo — ícone

**Estado atual:** EXISTENTE. **Tipo:** ícone 2D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ICON-ITEM-ANCIENT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="model-item-ancient"></a>

## MODEL-ITEM-ANCIENT — Fio Antigo — malha dedicada

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=MODEL-ITEM-ANCIENT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="icon-item-leather"></a>

## ICON-ITEM-LEATHER — Colete de couro — ícone

**Estado atual:** EXISTENTE. **Tipo:** ícone 2D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ICON-ITEM-LEATHER]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="model-item-leather"></a>

## MODEL-ITEM-LEATHER — Colete de couro — malha dedicada

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=MODEL-ITEM-LEATHER]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="icon-item-health"></a>

## ICON-ITEM-HEALTH — Poção de vida — ícone

**Estado atual:** EXISTENTE. **Tipo:** ícone 2D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ICON-ITEM-HEALTH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="model-item-health"></a>

## MODEL-ITEM-HEALTH — Poção de vida — malha dedicada

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=MODEL-ITEM-HEALTH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="loot-shared"></a>

## LOOT-SHARED — Pickup compartilhado — esfera e movimento

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=LOOT-SHARED]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="model-loot-gold"></a>

## MODEL-LOOT-GOLD — Ouro — forma dedicada

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=MODEL-LOOT-GOLD]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="model-loot-xp"></a>

## MODEL-LOOT-XP — Orbe de XP — forma dedicada

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=MODEL-LOOT-XP]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="ui-inventory"></a>

## UI-INVENTORY — Inventário

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-INVENTORY]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="ui-slots"></a>

## UI-SLOTS — Slots e estados

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-SLOTS]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="ui-equipment"></a>

## UI-EQUIPMENT — Equipamentos e status

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-EQUIPMENT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="ui-details"></a>

## UI-DETAILS — Detalhes do item selecionado

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-DETAILS]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="hud-potions"></a>

## HUD-POTIONS — Atalho/quantidade de poções no HUD

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=HUD-POTIONS]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.
