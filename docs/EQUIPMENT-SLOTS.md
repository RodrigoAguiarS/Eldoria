# Seis slots de equipamento — D-006 / D-007

D-006 (18/11/2026): expansão explícita pedida pelo usuário sobre o inventário da 1.0 (D-002),
que previa somente um slot de arma e um de armadura. Branch `claude/equipment-six-slots`,
base `main@14ee565`.

D-007 (18/11/2026): por pedido explícito do usuário, **substitui** a regra D-006 de manter
equipamentos dentro dos 16 espaços da bolsa. Os seis slots passam a ser armazenamento próprio
e toda nova run começa com uma arma comum equipada. Branch `claude/equipment-storage`,
empilhada sobre `claude/equipment-six-slots@bb8c704`.

Este documento é a fonte das regras e dos valores iniciais; os números são **proposta
configurável de playtest**, sem aprovação humana de balanceamento.

## Regras

- Seis categorias independentes: **Arma, Armadura, Capacete, Luvas, Botas e Acessório** (um slot).
  O jogador pode manter as seis equipadas ao mesmo tempo.
- **Cada unidade de item existe em um único lugar: bolsa, slot de equipamento ou chão.** Um item
  equipado não ocupa espaço na bolsa de 16 e não aparece nela; o contador da bolsa conta só os
  16 espaços. Duas unidades da mesma definição continuam itens distintos (uma pode estar
  equipada e a outra na bolsa).
- **Equipar** (botão Equipar, com um item da bolsa selecionado) move a unidade da bolsa para o
  slot compatível e libera o espaço. Com o slot ocupado é uma **troca atômica**: a peça anterior
  volta para o espaço liberado pelo item selecionado, por isso a troca funciona com a bolsa cheia.
- **Desequipar** (botão Desequipar, com um slot de equipamento selecionado) move a peça para o
  primeiro espaço livre da bolsa. **Com a bolsa cheia a operação é recusada** com a mensagem
  "Bolsa cheia: libere um espaço para desequipar. Nada foi alterado." — equipamento, bolsa e
  atributos ficam intactos; nada é destruído, descartado ou largado automaticamente.
- Clicar ou focar um slot de equipamento **seleciona a peça equipada** (não um índice da bolsa)
  e mostra seus detalhes; nunca desequipa. A seleção distingue item da bolsa e item equipado e
  acompanha a unidade: depois de equipar, fica no slot; depois de desequipar, no espaço da bolsa.
- **Largar** age só sobre itens da bolsa. Uma peça equipada é desequipada antes de ser largada
  (o botão Largar fica desativado com um slot selecionado), de modo que uma falha de posição no
  mundo nunca remove item nem bônus.
- **Ordenar** (F02) reorganiza somente a bolsa; os equipamentos não mudam. Ordem por tipo:
  arma, armadura, capacete, luvas, botas, acessório, poção.
- Coletar uma arma (ou qualquer peça) **nunca equipa automaticamente**: o item vai para a bolsa.
  O autoequipamento da primeira arma (D-002) foi retirado porque contrariava o equipamento
  manual; a arma inicial cobre o caso de começar armado.
- Poções não são equipamento. Jogador morto não equipa, desequipa, coleta ou larga.
- Raridade continua sem multiplicador oculto. Não existem velocidade de ataque, crítico,
  resistências, conjuntos, afixos ou poderes ligados às novas categorias.

## Arma inicial (nova run)

- `InventoryDefinition.startingWeapon` (referência serializada no SO `PlayerInventory`) aponta
  para a **Espada de Ferro** existente (`Weapon_IronSword`, Comum, +6 de dano plano). O builder
  só preenche o campo quando vazio e recusa uma arma que não seja Comum.
- É colocada **direto no slot de arma**, uma única vez, quando a cena do jogador começa sem uma
  continuação pendente (`RunCheckpointSession.HasPending`): Novo jogo, reinício (R) ou cena
  aberta diretamente. Não cria cópia na bolsa nem no chão, não dispara coleta e não deixa uma
  segunda arma oculta no cálculo (o dano inicial é 20 + 6 = 26 no nível 1).
- Abrir/fechar o inventário, reativar componentes, trocar de andar e continuar um checkpoint
  **não concedem outra arma**. A continuação e a chegada ao andar restauram exatamente o
  equipamento salvo, inclusive com o slot de arma vazio. Um checkpoint recusado não restaura
  nada e a cena segue como nova run (com a arma inicial).
- A arma inicial é um item real: pode ser trocada, desequipada para a bolsa e largada como
  qualquer outra. Armadura e demais peças **não** são equipadas no início.
- Combate sem arma (regra existente): com o slot vazio o golpe usa só o dano base 20, sem bônus
  de arma, e o slot mostra **Espada básica** (a espada fixa do modelo do herói).

## Atributos e composição

Somente os dois atributos já suportados são reutilizados: **bônus plano de dano** e **defesa**.

- Dano base = (dano base 20 + bônus da arma + bônus plano dos outros equipamentos) × nível
  (+10%/nível) × talento (Lâmina Afiada). Combo e giro multiplicam esse valor por golpe, como
  antes. A ordem dos fatores e a fórmula de D-002 não mudam; o termo novo entra na soma plana.
- Defesa = base + armadura + outros equipamentos + talento (Couraça Firme). Mitigação
  inalterada: max(1, dano × 100/(100 + defesa)).
- Cada slot contribui uma vez, em ordem fixa, a partir do conteúdo dos slots. Os valores são
  atribuídos de forma absoluta: atualizar a UI, reativar componentes, trocar ou restaurar
  checkpoint não acumula bônus; trocar/desequipar remove só os bônus daquela peça. Equipar não
  altera HP atual nem máximo. Recusas não produzem alterações parciais nem eventos, e cada
  transferência publica um único evento com o estado final.
- A comparação (item da bolsa × peça equipada do mesmo slot) usa o mesmo cálculo: valor atual,
  valor após a troca e diferença, com os outros cinco slots e os talentos constantes. Luvas
  nunca são comparadas com armadura, nem acessório com arma. Comparar não muda o estado.

## Valores iniciais (proposta de playtest)

| Item (asset) | Categoria | Raridade | Dano plano | Defesa |
| --- | --- | --- | --- | --- |
| Espada de Ferro (`Weapon_IronSword`) — arma inicial | Arma | Comum | 6 | 0 |
| Elmo de ferro (`Gear_IronHelmet`) | Capacete | Comum | 0 | 4 |
| Luvas de couro (`Gear_LeatherGloves`) | Luvas | Comum | 2 | 0 |
| Botas de couro (`Gear_LeatherBoots`) | Botas | Comum | 0 | 3 |
| Amuleto de cobre (`Gear_CopperAmulet`) | Acessório | Raro | 1 | 2 |

Distribuição: defesa nas peças de proteção (capacete/botas), dano nas mãos (luvas) e um
pouco dos dois no acessório. Conjunto novo completo: +3 de dano plano e +9 de defesa — menos
que uma Espada de Ferro (+6) e próximo de um Colete de couro (10). Com as seis peças
iniciais, dano base 29 no nível 1 e defesa 19 (dano recebido ×0,84). Ajustar nos SOs.

Começar com a Espada de Ferro eleva o dano inicial de 20 para 26 (+30%); antes de D-007 a
primeira arma coletada já produzia esse valor. O efeito no ritmo da primeira sala não teve
playtest humano.

Limitações: sem playtest humano; nenhuma curva de nível de item; valores pequenos para não
deslocar o equilíbrio medido em [BALANCE-1.0](BALANCE-1.0.md).

## Disponibilidade e loot

As tabelas de loot e as probabilidades da release **não foram alteradas**. As quatro peças
existem como dados e estão disponíveis no cenário diagnóstico reproduzível
(`-dcc-items` / menu de teste do Editor, `QA_ItemShowcase` no andar de release): entrega uma
de cada categoria pelos comandos normais, equipa todas e cria um pickup de cada no chão.
`LootEntry.gear`/`LootKind.Gear` (valor 5, acrescentado ao fim) permitem incluí-las em tabelas
numa tarefa própria, com chances justificadas.

## Checkpoint

Schema **v5** (D-007): `slots` guarda só a bolsa e `equipment` guarda exatamente seis IDs
persistentes na ordem dos slots (`""` = vazio). Em v5 `weaponIndex`/`armorIndex` valem −1 e
`gearIndices` fica vazio; documento v5 sem `equipment`, com tamanho diferente, com item
desconhecido, de outra categoria ou com campos antigos preenchidos é recusado.

Versões **v1–v4** (equipamento como índice da bolsa) continuam legíveis e são migradas: o
snapshot inteiro é validado antes; cada unidade referenciada vai para o seu slot e **só o seu
espaço original** é esvaziado; os demais itens ficam nas mesmas posições; nada é removido por
ter a mesma definição de um equipamento; a migração não duplica itens nem concede a arma
inicial (um v1–v4 sem arma vinculada chega com o slot vazio). v4 continua exigindo
`gearIndices` com quatro índices (v1–v3 sem o campo); pickups `Gear` só a partir do v4.
A chegada ao segundo andar converte qualquer origem para v5. Talentos, loot no chão, IDs
persistentes, backup e fronteiras seguras ficam como estavam.
Detalhes em [CHECKPOINTS-F11](CHECKPOINTS-F11.md#extensão-d-007--schema-v5).

## Interface

Bolsa à esquerda (16 espaços; mostra só o que está guardado). Coluna direita em três áreas:

1. **EQUIPAMENTOS** — grade 3×2: Capacete | Arma | Acessório / Armadura | Luvas | Botas.
   Cada slot mostra a categoria, o ícone (quando o catálogo tiver), o nome curto na cor da
   raridade, a faixa de raridade (skin), o marcador de equipado e o de vazio. Hover troca a
   base para Slot-Hover; foco do teclado também mostra bordas frias; seleção usa Slot-Selected
   e o marcador de seleção.
2. **ATRIBUTOS** — dano base efetivo, termos (base, arma, outros equipamentos, nível,
   talentos), multiplicadores por golpe (combo/giro), defesa total e seus termos, vida.
   Todos os números vêm do domínio; a UI não recalcula.
3. **DETALHES DO ITEM** — nome completo, raridade, categoria, estado equipado ("Fora da bolsa"
   para peças equipadas), atributos, descrição, a comparação com a peça do mesmo slot (para
   itens da bolsa) e a mensagem de bolsa cheia quando um desequipar é recusado.

Botões: Equipar (item equipável da bolsa), Desequipar (slot ocupado), Usar (poção da bolsa),
Largar (item da bolsa). Atributos e detalhes ficam no ScrollRect existente (rolagem preservada).
Abaixo da bolsa, uma nota explica que equipar move o item para o slot e que desequipar devolve
à bolsa e precisa de um espaço livre.

## Arte (lote ITEMS-GEAR)

Produzida e integrada em 18/11/2026 no mesmo estilo low-poly do lote ITEMS-COMBAT (espadas,
colete, poção), com a mesma paleta e os mesmos materiais `Item_*`; só o cobre do amuleto é novo.

| Peça | Ícone | Modelo de chão | Fonte |
| --- | --- | --- | --- |
| Elmo de ferro | `Art/Items/Icons/ICON-ITEM-HELMET.png` | `Art/Items/Models/Item_IronHelmet.fbx` → `Prefabs/Ground_Item_IronHelmet.prefab` | `Tools/Lemo3DGenerator/src/production/GearModels.mjs` / `GearIcons.mjs` |
| Luvas de couro (par = 1 unidade) | `ICON-ITEM-GLOVES.png` | `Item_LeatherGloves.fbx` → `Ground_Item_LeatherGloves.prefab` | idem |
| Botas de couro (par = 1 unidade) | `ICON-ITEM-BOOTS.png` | `Item_LeatherBoots.fbx` → `Ground_Item_LeatherBoots.prefab` | idem |
| Amuleto de cobre | `ICON-ITEM-AMULET.png` | `Item_CopperAmulet.fbx` → `Ground_Item_CopperAmulet.prefab` | idem |

Acessório inicial: **amuleto** (medalhão de cobre com cordão), porque a definição existente é
"Amuleto de cobre" e a silhueta circular se distingue das outras três categorias a 32 px.
Integração idempotente pelo menu `Tools/Dungeon Crawler/Production Visual/Gear (Helmet, Gloves,
Boots, Amulet)`; o lote ITEMS-COMBAT preserva estas entradas ao ser reaplicado. Procedência em
`Art/ProductionStaging/ITEMS-GEAR/Source~/PROVENANCE.md`; evidências em
[Reports/EquipmentArt](../Reports/EquipmentArt/README.md).

Pendências:
- **Aparência equipada no Hero** (capacete, luvas, botas, amuleto): o `PlayerEquipmentVisualPresenter`
  só cobre arma (soquete) e armadura (malha skinned). Vestir as quatro peças exige malhas skinned no
  rig Generic do Hero ou soquetes novos e uma extensão do presenter — trabalho próprio, fora deste lote.
- Aprovação artística humana.
- Segundo andar (F14): recebe os slots e o layout F10, mas continua sem o UI-SKIN (pendência
  já registrada em V04).
