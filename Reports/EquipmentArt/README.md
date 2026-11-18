# ITEMS-GEAR — Arte de capacete, luvas, botas e amuleto

Branch `claude/equipment-storage` (empilhada sobre D-006 → D-007). Unity 6000.6.9f1, URP 17.6.0.
Commit testado: ver [tested-commit.txt](tested-commit.txt). Procedência:
[PROVENANCE.md](../../Assets/_Project/Art/ProductionStaging/ITEMS-GEAR/Source~/PROVENANCE.md).
Regras das peças em [EQUIPMENT-SLOTS § Arte](../../docs/EQUIPMENT-SLOTS.md). Relatórios anteriores
([EquipmentSlots](../EquipmentSlots/README.md), [EquipmentStorage](../EquipmentStorage/README.md)) não foram alterados.

## Produzido × integrado × validado

| Peça | Produzido (fonte + staging) | Integrado (Unity) | Validado no Editor |
| --- | --- | --- | --- |
| Elmo de ferro | `GearModels.mjs`/`GearIcons.mjs` → `ITEMS-GEAR/Source~` (FBX, GLB, PNG) | `Art/Items/Models/Item_IronHelmet.fbx`, `Icons/ICON-ITEM-HELMET.png`, `Prefabs/Ground_Item_IronHelmet.prefab`, entrada no `ItemVisualCatalog` | Sim: importação, catálogo, bolsa, detalhes, slot, chão (largar/recolher), 720p/1080p |
| Luvas de couro (par = 1 unidade) | idem | `Item_LeatherGloves.fbx`, `ICON-ITEM-GLOVES.png`, `Ground_Item_LeatherGloves.prefab` | Sim (idem) |
| Botas de couro (par = 1 unidade) | idem | `Item_LeatherBoots.fbx`, `ICON-ITEM-BOOTS.png`, `Ground_Item_LeatherBoots.prefab` | Sim (idem) |
| Amuleto de cobre | idem | `Item_CopperAmulet.fbx`, `ICON-ITEM-AMULET.png`, `Ground_Item_CopperAmulet.prefab` | Sim (idem) |
| Aparência equipada no Hero | **não produzida** | — | **NÃO VALIDADO / pendente** |

Materiais: reutilizados `Item_steel`, `Item_steelDark`, `Item_brass`, `Item_leatherDark`, `Item_vestLeather`,
`Item_vestSeam` (não reescritos); novos `Item_copper` e `Item_copperDark` (URP/Lit).
Manifesto: 8 IDs reservados (`ICON-ITEM-HELMET/GLOVES/BOOTS/AMULET`, `MODEL-ITEM-…`) e o lote `ITEMS-GEAR` em
`production_batches`. Atributos das peças inalterados (verificado por `GearStats_AreUnchangedByTheArt`).

## Especificação aplicada

| Modelo | Triângulos | Tamanho (m) | Escala no chão |
| --- | --- | --- | --- |
| Item_IronHelmet | 632 | 0,40 × 0,35 × 0,41 | 1,35 |
| Item_LeatherGloves | 836 | 0,30 × 0,11 × 0,27 | 2,0 (inclinado −28°) |
| Item_LeatherBoots | 632 | 0,29 × 0,32 × 0,30 | 1,75 |
| Item_CopperAmulet | 332 | 0,20 × 0,37 × 0,05 | 2,3 (inclinado −20°) |

FBX 7.4 em metros, Y para cima, +Z frente, pivô na base, normais facetadas, sem colliders, importação escala 1 sem
animação (mesmas regras do ITEMS-COMBAT). Ícones 512×512 RGBA, conteúdo em 416×416 (margem 48 px), Sprite Single,
sRGB, sem mipmaps, bilinear, clamp, PPU 100, sem compressão; mesma página de render, luz e contorno dos ícones
existentes; sem moldura ou cor de raridade pintada (a UI aplica a faixa de raridade a partir dos dados).

## Testes executados

| Artefato | Resultado |
| --- | --- |
| [EditMode-2.xml](EditMode-2.xml) | **345/345** (inclui `GearArtTests`: catálogo, prefabs, URP, importação, atributos e reaplicação dos dois integradores sem mudança de bytes) |
| [PlayMode-3.xml](PlayMode-3.xml) | **246: 226 aprovados, 0 falhas**, 20 `[Explicit]` ignorados no batch (inclui `GearArtSceneTests`: ícones na bolsa/detalhes/slots e modelo no chão ao largar e recolher) |
| [PlayMode-graphical.xml](PlayMode-graphical.xml) | 2/2 no Editor com janela: matriz 720p/1080p × texto 100/125/150% do inventário e chão |
| [PlayMode-graphical-ground.xml](PlayMode-graphical-ground.xml) | 1/1: chão após o ajuste de escala |
| [hashes-run4](hashes-run4.txt) = [hashes-run5](hashes-run5.txt) | Integrador reaplicado sem alterar FBX, ícones, prefabs (todos de `Art/Items`), materiais ou catálogo |
| [EditMode-1.xml](EditMode-1.xml), [PlayMode-1.xml](PlayMode-1.xml), [PlayMode-2.xml](PlayMode-2.xml) | Execuções intermediárias (ver Achados) |

Medições de desempenho da suíte redirecionadas para [artifacts/](artifacts/). Efeitos colaterais conhecidos das suítes
restaurados com `git checkout` após cada execução.

## Capturas reais

- [Chão da release](graphical-ground/gear-ground-2560x1440.png): elmo, luvas, botas e amuleto ao lado da Espada de Ferro
  e do Colete de couro (cena `Floor_01_GeneratedCharacters`, PlayMode, Game view 2560×1440).
- [Inventário 720p 100%](graphical/1280x720-100-full-focus-gloves.png) e demais tamanhos em [graphical/](graphical/):
  ícones de elmo, amuleto e botas nos slots equipados. As "Manoplas cerimoniais" da matriz são uma cópia de diagnóstico
  com nome longo, fora do catálogo, e por isso aparecem sem ícone (comportamento esperado).
- [Prancha de ícones](review/review_icons.png): os 9 ícones de item a 48/32 px sobre claro/escuro/pedra/base do slot e
  silhuetas em cinza; [prancha de modelos](review/models_review.png) ao lado da espada e do colete (revisão fora do jogo).

## Achados

1. **Corrigido — teste novo contaminava a suíte:** `GearArtSceneTests` selecionava botões de UI fora de
   `InputTestFixture`; 11 testes de teclado/mouse executados depois falharam (PlayMode-1). Agora segue o padrão das
   outras classes de cena (PlayMode-2/3 verdes).
2. **Corrigido — reaplicar ITEMS-COMBAT apagava entradas:** `ProductionVisualIntegrator.UpsertCatalog` substituía o
   catálogo inteiro; agora preserva entradas de outros lotes (verificado por teste).
3. **Ajustado — legibilidade no chão:** a primeira captura mostrou luvas, botas e amuleto pequenos ao lado da espada e do
   colete; escalas aumentadas (só pose do prefab).
4. **Observação de arte:** o amuleto gira com o pickup e fica fino em parte da rotação, embora inclinado; o ícone não
   tem esse efeito.
5. Os materiais novos são regravados uma única vez após a criação (o URP acrescenta o bloco de versão no primeiro
   recarregamento); a partir daí a reaplicação não muda bytes.

## Pendências

- **Aparência equipada** de capacete, luvas, botas e amuleto no Hero: o `PlayerEquipmentVisualPresenter` só cobre arma
  (soquete) e armadura (malha skinned). Exige malhas skinned no rig Generic do Hero ou soquetes novos e uma extensão do
  presenter — fora deste lote, sem sistema novo criado.
- Aprovação artística humana.
- **NÃO VALIDADO NO EDITOR:** monitores físicos (só Game view emulada); build Windows deste incremento; gamepad.
