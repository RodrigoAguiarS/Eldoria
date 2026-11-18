# Seis slots de equipamento — D-006

Expansão explícita pedida pelo usuário em 18/11/2026, sobre o inventário da 1.0 (D-002),
que previa somente um slot de arma e um de armadura. Branch `claude/equipment-six-slots`,
base `main@14ee565`. Este documento é a fonte das regras e dos valores iniciais; os números
são **proposta configurável de playtest**, sem aprovação humana de balanceamento.

## Regras

- Seis categorias independentes: **Arma, Armadura, Capacete, Luvas, Botas e Acessório** (um slot).
  O jogador pode manter as seis equipadas ao mesmo tempo.
- Equipamento continua **na bolsa de 16 espaços**: cada slot é um vínculo a um índice da bolsa,
  nunca armazenamento extra. Equipar, trocar e desequipar não exigem espaço livre e funcionam
  com a bolsa cheia; o item substituído permanece no seu espaço.
- Equipar é manual pela bolsa (**Equipar**). Só a primeira arma coletada continua equipando
  sozinha (D-002). Itens novos de outras categorias chegam desequipados.
- Consultar um slot (clique ou foco) seleciona o item correspondente da bolsa e mostra os
  detalhes; **nunca desequipa**. Desequipar só pelo botão **Desequipar**, que remove apenas o
  vínculo do item selecionado.
- Sem arma da bolsa equipada, o slot Arma mostra **Espada básica** (dano base sem bônus).
- Poções não são equipamento. Jogador morto não equipa, desequipa, coleta ou larga.
- Largar um item equipado remove o vínculo e o bônus; recolhê-lo devolve o item desequipado.
- Ordenar (F02) segue a instância movida nos seis vínculos; duplicatas da mesma definição
  continuam distintas. Ordem por tipo: arma, armadura, capacete, luvas, botas, acessório, poção.
- Raridade continua sem multiplicador oculto. Não existem velocidade de ataque, crítico,
  resistências, conjuntos, afixos ou poderes ligados às novas categorias.

## Atributos e composição

Somente os dois atributos já suportados são reutilizados: **bônus plano de dano** e **defesa**.

- Dano base = (dano base 20 + bônus da arma + bônus plano dos outros equipamentos) × nível
  (+10%/nível) × talento (Lâmina Afiada). Combo e giro multiplicam esse valor por golpe, como
  antes. A ordem dos fatores e a fórmula de D-002 não mudam; o termo novo entra na soma plana.
- Defesa = base + armadura + outros equipamentos + talento (Couraça Firme). Mitigação
  inalterada: max(1, dano × 100/(100 + defesa)).
- Cada slot contribui uma vez, em ordem fixa. Os valores são atribuídos de forma absoluta a
  partir dos vínculos: atualizar a UI, reativar componentes ou restaurar checkpoint não
  acumula bônus. Equipar não altera HP atual nem máximo.
- A comparação usa o mesmo cálculo, sempre dentro do mesmo slot: valor atual, valor após a
  troca e diferença, com os outros cinco slots e os talentos constantes. Luvas nunca são
  comparadas com armadura, nem acessório com arma.

## Valores iniciais (proposta de playtest)

| Item (asset) | Categoria | Raridade | Dano plano | Defesa |
| --- | --- | --- | --- | --- |
| Elmo de ferro (`Gear_IronHelmet`) | Capacete | Comum | 0 | 4 |
| Luvas de couro (`Gear_LeatherGloves`) | Luvas | Comum | 2 | 0 |
| Botas de couro (`Gear_LeatherBoots`) | Botas | Comum | 0 | 3 |
| Amuleto de cobre (`Gear_CopperAmulet`) | Acessório | Raro | 1 | 2 |

Distribuição: defesa nas peças de proteção (capacete/botas), dano nas mãos (luvas) e um
pouco dos dois no acessório. Conjunto novo completo: +3 de dano plano e +9 de defesa — menos
que uma Espada de Ferro (+6) e próximo de um Colete de couro (10). Com as seis peças
iniciais, dano base 29 no nível 1 e defesa 19 (dano recebido ×0,84). Ajustar nos SOs.

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

Schema **v4**: `gearIndices` com exatamente quatro índices (capacete, luvas, botas,
acessório; −1 = vazio), além de `weaponIndex`/`armorIndex`. v1/v2/v3 continuam legíveis com
os quatro slots novos desequipados; um documento v1–v3 com `gearIndices` é malformado e
recusado. Em v4 o campo é obrigatório: campo ausente nunca vira índice zero. Pickups
`Gear` só existem a partir do v4. O documento inteiro é validado antes de qualquer mutação;
talentos, loot no chão, fronteiras seguras, backup e regras de restauração ficam como estavam.
A transferência entre andares normaliza origens antigas para v4 com os slots novos vazios.
Detalhes em [CHECKPOINTS-F11](CHECKPOINTS-F11.md#extensão-d-006--schema-v4).

## Interface

Bolsa à esquerda (16 espaços, inalterada). Coluna direita em três áreas:

1. **EQUIPAMENTOS** — grade 3×2: Capacete | Arma | Acessório / Armadura | Luvas | Botas.
   Cada slot mostra a categoria, o ícone (quando o catálogo tiver), o nome curto na cor da
   raridade, a faixa de raridade (skin), o marcador de equipado e o de vazio. Hover troca a
   base para Slot-Hover; foco do teclado também mostra bordas frias; seleção usa Slot-Selected
   e o marcador de seleção.
2. **ATRIBUTOS** — dano base efetivo, termos (base, arma, outros equipamentos, nível,
   talentos), multiplicadores por golpe (combo/giro), defesa total e seus termos, vida.
   Todos os números vêm do domínio; a UI não recalcula.
3. **DETALHES DO ITEM** — nome completo, raridade, categoria, estado equipado, atributos,
   descrição e a comparação com o slot da mesma categoria.

Atributos e detalhes ficam no ScrollRect existente (rolagem preservada). Abaixo da bolsa,
uma nota explica que equipamento continua na bolsa.

## Arte pendente

- Ícones `ICON-ITEM-*` para as quatro peças (o `ItemVisualCatalog` não tem entrada; os
  slots e detalhes funcionam só com texto e marcadores).
- Modelos de chão: os pickups usam o fallback tingido por `LootKind.Gear`.
- Peças vestíveis no Hero (capacete, luvas, botas, acessório): fora deste incremento; nenhuma
  mudança no rig Generic ou no `PlayerEquipmentVisualPresenter`.
- Segundo andar (F14): recebe os slots e o layout F10, mas continua sem o UI-SKIN (pendência
  já registrada em V04).
