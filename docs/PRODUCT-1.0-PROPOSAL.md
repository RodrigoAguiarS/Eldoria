# Regras aprovadas da 1.0 — decisão D-002

Detalha as decisões D-001/D-002 de [GAME_DESIGN](../GAME_DESIGN.md#decisões-de-produto). Usuário aprovou a proposta completa em 14/11/2026. Implementação incremental em R3; valores novos são ajustes iniciais em ScriptableObjects, sujeitos a playtest. O nome deste arquivo preserva o caminho apresentado na decisão.

| Sistema | Proposta concreta |
| --- | --- |
| Especial | Giro de espada em 360°, in-place, alcance 3 m, um impacto com 2× o dano calculado de um golpe básico. Botão direito/E. Custo 30 de estamina, cooldown 6 s. Sem dano atrasado após morte, dash ou desativação. |
| Recurso | Estamina máxima 100; regenera 20/s após 0,8 s sem usar especial. Ataque básico, movimento e dash mantêm suas regras/custos atuais. HUD com valor e nome, além da barra. |
| Defesa | Um slot de armadura; defesa base 0. Dano recebido positivo = max(1, dano × 100/(100 + defesa)). A armadura inicial de loot oferece 10 de defesa; sem alteração de HP base, dano inimigo ou invulnerabilidade. |
| Inventário | 16 espaços; armas/armaduras ocupam um espaço cada; poções empilham até 5 por potência de cura. I/Tab abre e pausa a partida offline; clicar nos itens mostra nome, raridade, atributos e ações. Sem arrastar obrigatório. |
| Coleta | XP e ouro continuam imediatos. Armas e poções passam a ser guardadas. Primeira arma pode equipar automaticamente se o slot estiver vazio; trocas seguintes são manuais. Inventário cheio mantém o pickup no chão e informa o motivo. |
| Consumo | Q ou botão Usar consome uma poção guardada; não consome com HP cheio ou jogador morto. Mantém os valores de cura já configurados nos drops (15/20/30/40). |
| Equipamento | Um slot de arma e um de armadura. Equipar/trocar/desequipar não apaga o item. UI solicita comandos ao sistema de inventário; não calcula dano ou cura. |
| Raridades | Comum, Raro e Épico, com nome/cor/dados em ScriptableObjects; sem multiplicador oculto de raridade nem afixos aleatórios. Espada de Ferro: Comum/+6. Lâmina do Guardião: Raro/+12. Uma variante épica pode ter valor explícito, sem alterar as duas armas existentes. |
| Loot adicional | Introduzir armadura simples e uma arma épica nas tabelas por entradas independentes, preservando chances/valores das entradas atuais. Pesos novos configuráveis; sem loot procedural. |
| Progressão | Preservar (dano base + bônus da arma) × multiplicador por nível × multiplicador do golpe. Manter +10% por nível e cura de nível atuais. Novo recurso não altera a fórmula. |
| Menus/opções | Menu inicial, pausa, retorno/reinício, volume de música/efeitos, intensidade de shake/flash e qualidade/resolução/tela cheia. Opções persistem; sem save de campanha/inventário entre runs nesta versão. |
| Apresentação | Preservar herói/rig Generic, materiais e ambiente atuais. Especial usa clip/efeito simples; áudio funcional reutilizado e controlado por volume. Nenhuma dependência da task de imagens. |

Mudanças de comportamento aprovadas: poções deixam de curar automaticamente ao coletar; armas melhores deixam de substituir automaticamente uma arma já equipada; inventário pausa a partida; especial e armadura introduzem novas ferramentas de combate.

Detalhe de gestão de capacidade: o botão Largar 1 devolve uma unidade ao chão, preservando definição/potência; equipamento largado deixa de aplicar bônus. Se não houver posição livre, a operação mantém o item na bolsa. A bolsa pode ser usada inteiramente por botões, sem arrastar itens.

Aceite: testes de custo/regen/cancelamento/defesa, capacidade/stack/identidade/equipamento/consumo, coleta física com inventário cheio, persistência das opções, funcionamento dos menus e percurso normal completo em Editor/Windows. Nenhum marco será declarado pronto apenas pela presença dos scripts.
