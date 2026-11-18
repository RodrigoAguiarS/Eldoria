# ITEMS-GEAR — procedência

Lote de arte das quatro peças de equipamento de D-006/D-007, produzido em 18/11/2026 a pedido
explícito do usuário (tarefa "Criar e integrar as artes dos novos equipamentos").

| ID do manifesto | Arquivo integrado | Fonte editável |
| --- | --- | --- |
| MODEL-ITEM-HELMET | `Assets/_Project/Art/Items/Models/Item_IronHelmet.fbx` · `Prefabs/Ground_Item_IronHelmet.prefab` | `Tools/Lemo3DGenerator/src/production/GearModels.mjs` (`ironHelmet`) |
| MODEL-ITEM-GLOVES | `Item_LeatherGloves.fbx` · `Ground_Item_LeatherGloves.prefab` (o par é uma unidade) | idem (`glove`, `leatherGloves`) |
| MODEL-ITEM-BOOTS | `Item_LeatherBoots.fbx` · `Ground_Item_LeatherBoots.prefab` (o par é uma unidade) | idem (`boot`, `leatherBoots`) |
| MODEL-ITEM-AMULET | `Item_CopperAmulet.fbx` · `Ground_Item_CopperAmulet.prefab` | idem (`copperAmulet`) |
| ICON-ITEM-HELMET/GLOVES/BOOTS/AMULET | `Assets/_Project/Art/Items/Icons/ICON-ITEM-*.png` (512×512 RGBA) | `Tools/Lemo3DGenerator/src/production/GearIcons.mjs` + mesma geometria |

## Ferramentas e processo

- **Modelagem:** geometria procedural escrita à mão em JavaScript (`GearModels.mjs`), com as primitivas facetadas de
  `Tools/Lemo3DGenerator/src/geo.mjs` e a montagem/exportação de `ItemModels.mjs` (lote ITEMS-COMBAT, que produziu as
  espadas, o colete e a poção). three.js r170 do Lemo-Opuscar (`G:\lemo-opuscar\lemo-opuscar\node_modules\three`), usado
  **somente leitura**; Node 24.18. Blender não está instalado: a fonte editável é o próprio `.mjs` (como no lote
  ITEMS-COMBAT). Saída: FBX 7.4 ASCII (metros, Y para cima, +Z frente, pivô na base, normais facetadas) e GLB de revisão.
- **Ícones:** `GearIcons.mjs` renderiza a mesma geometria na página three.js de `ItemIcons.mjs` (mesma luz, câmera
  ortográfica, supersampling 2×, contorno escuro por dilatação do alpha, conteúdo em 416×416 com margem de 48 px), via
  `core/render/page.mjs` do Lemo (Playwright Chromium headless, somente leitura). PNG gravado a partir do canvas com fundo
  transparente (alpha real, não captura de tela).
- **Paleta/materiais:** reutiliza os materiais URP/Lit já integrados `Item_steel`, `Item_steelDark`, `Item_brass`,
  `Item_leatherDark`, `Item_vestLeather`, `Item_vestSeam`; novos apenas `Item_copper` (#C27A45) e `Item_copperDark` (#6E3F22).
- **Referências:** `Assets/_Project/Art/Concepts/ART_DIRECTION_AND_PROMPTS.md`, `ProductionSpecs/ASSET_MANIFEST.json`
  (entradas ITEMS-COMBAT) e os próprios modelos/ícones integrados (Espada de Ferro, Colete de couro) — ver
  `Icons/models_review.png` e `Icons/review_icons.png`.
- **Escolha do acessório:** amuleto (medalhão de cobre com cordão), porque a definição existente é "Amuleto de cobre" e a
  silhueta circular com cordão é distinta de elmo/luvas/botas a 32 px. Nenhuma subcategoria ou slot novo.

## Licença e fontes externas

Sem imagens, modelos, texturas, fontes tipográficas ou assets de terceiros. Nenhum gerador de imagens por IA foi usado:
toda a geometria e todos os pixels saem dos scripts acima. three.js (MIT) e Playwright (Apache-2.0) são ferramentas de
execução, não conteúdo incorporado. Os arquivos produzidos seguem a licença do projeto.

## Estado

Produzido e integrado (ver `Reports/EquipmentArt/README.md` para a validação efetivamente executada no Editor).
Pendente: aparência equipada no Hero (o `PlayerEquipmentVisualPresenter` cobre só arma e armadura; vestir elmo/luvas/
botas/amuleto exige malhas skinned ou soquetes novos e uma extensão do presenter — trabalho fora deste lote).
Aprovação artística humana pendente.
