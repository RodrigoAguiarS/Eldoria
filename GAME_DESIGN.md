# GAME_DESIGN.md — Eldoria

## Visão
**Eldoria** é um Action-RPG / dungeon crawler isométrico 3D para Windows, inspirado no ritmo de combate e progressão de jogos do gênero, com identidade visual própria. Unity 6 URP, estilo low-poly estilizado, câmera fixa em orientação e inclinada ~45°.

## Loop completo (após MVP)
Entrar na sala → portas fecham → inimigos surgem → derrotar todos → portas abrem → coletar XP/loot → avançar. Um andar tem quatro salas normais e uma arena de Boss.

## Controles
- WASD: movimento relativo à projeção horizontal da câmera, sem deslocamento vertical.
- Mouse: mira via raycast contra plano do chão; o personagem gira horizontalmente para o alvo.
- Botão esquerdo: ataque básico; espaço: dash com cooldown e janela curta de invulnerabilidade; botão direito ou E: especial (pós-MVP inicial).
- Câmera Cinemachine segue suavemente, sem rotação controlada pelo jogador.

F04: **Opções → Controles** permite remapear as quatro direções, ataque, dash, as duas alternativas do giro e da bolsa, poção, pausa/voltar e reinício para teclas ou botões do mouse. Os controles acima são os padrões. Conflitos são recusados e identificam a ação já atribuída; o binding anterior permanece. Escape cancela a captura e sempre permite voltar, mesmo com pausa remapeada; Enter permanece reservado para confirmar na UI. Cancelar, Voltar e Restaurar padrão têm botões próprios. Captura expira em 10 s e cancela ao perder foco ou fechar o painel. Alterações aceitas e restauração são salvas imediatamente por usuário, separadas das opções gerais e da progressão. Rótulos acompanham as ações efetivas. Mira/posição do ponteiro e navegação da UI permanecem fixas; esta entrega não adiciona suporte gamepad completo.

F10: **Opções → Texto e HUD** oferece 100% (padrão), 125% e 150% como faixa
inicial. Aplicar e salvar persiste a preferência; Voltar descarta o rascunho.
Restaurar padrões das opções gerais também exige Aplicar. Dados antigos ou
escala ausente/inválida usam 100%. A fonte e o UGUI permanecem. Botões não
encolhem com o texto; bolsa mantém 16 slots, com equipamento/detalhes roláveis.
Nomes extensos são resumidos com reticências no slot/HUD e completos nos
detalhes da bolsa. Escala não muda combate, inventário ou controles.

## Jogador
Usa CharacterController; atributos HP, recurso (estamina nesta versão), ataque, defesa, velocidade. Ao receber dano, feedback vermelho temporário e shake leve, sem comprometer legibilidade. Morte encerra combate e permite reiniciar.

## Inimigos
Máquina de estados Idle → Chase → Attack → Dead. Melee aproxima e golpeia; ranged mantém distância e dispara; elite tem maior HP/tamanho/dano e concede mais XP. Navegação por NavMeshAgent.

F07: ranged só prepara tiros com visão direta da origem do projétil ao alvo. Parede e porta fechada bloqueiam; ao detectar perda de visão durante a preparação, cancela o tiro e volta a perseguir pelo NavMesh. Reabrir a visão exige um novo ataque e respeita o cooldown já iniciado. Pausa congela os tempos; desativação descarta ataques pendentes. Dano, recuo, drops e Boss mantêm as regras existentes.

F13 (experimental, só na sala de teste fora do build): lanceiro com investida linear. Trava a direção ao se comprometer, mostra a faixa real no chão por pelo menos 1 s, avança pelo NavMesh sem atravessar parede/porta e causa no máximo um dano por investida; esquiva invulnerável consome o contato. Recuperação curta após o golpe. Encontros de release inalterados; valores são propostas de playtest em [LANCER-F13](docs/LANCER-F13.md).

## Boss
Duas fases. Aos 50% HP: transição única, invulnerável por 3 segundos, rugido e mudança visual; segunda fase altera ataques. Golpes pesados mostram telegraph no chão pelo menos 1 segundo antes do dano. HUD de Boss apenas durante luta.

F15: Sentinela de Pedra experimental, em arena opcional separada da release e
dos andares. Uma fase com leque frontal travado, aviso ≥1 s, impacto único e
recuperação explícita. Morte/desativação/pausa cancelam o aviso; retomar exige
novo aviso completo. Sem loot/XP/checkpoint, novos padrões ou A14. Reinício
local não toca a run salva. Valores propostos em [STONE-SENTINEL-F15](docs/STONE-SENTINEL-F15.md).
As duas fases/50%/3 s acima continuam sendo o contrato do Guardião existente.

## Dungeon
Salas como prefabs, com pontos de entrada/saída, portas e pontos de spawn definidos. Inicialmente sequência fixa; aleatoriedade só após estabilização. Inimigos da sala são rastreados por referências/eventos, não por busca global.

F08 — o experimento inicial está registrado em [PROPOSAL](Reports/F08/PROPOSAL.md). Por revisão solicitada pelo usuário, cada uma das cinco salas fixas da cena de release, incluindo Boss, tem um baú de primitivas independente. F próximo (2,5 m por padrão) abre somente após limpar a sala; E/direito continuam giro. Jogador morto, pausa, sala ou baú desativado impedem abertura. Abre/sorteia uma vez por instância da run: repetição, desativação/reativação e reset isolado de HP não renovam a recompensa. Reiniciar carrega sala e baú novos. Tabela exclusiva do baú concede 10–20 ouro e uma espada de ferro pelos pickups normais; não muda tabelas de inimigos. Drops do baú não expiram; bolsa cheia preserva a mesma recompensa até coleta aceita ou fim da run. Após a vitória, **Coletar recompensas** fecha a tela para acessar o baú do Boss e liberar espaço na bolsa, mantendo o andar concluído. Sem chaves, loja ou geração procedural. F pode ser remapeado em Controles; uma preferência anterior que já usava F é preservada, com tecla livre atribuída à interação e indicada no aviso.

F09: um tipo de vaso destrutível de primitivas, com 40 HP configuráveis em SO e
uma instância lateral em cada sala de release. Básico e giro causam dano uma vez
por alvo em cada impacto, mesmo com vários colliders. Quebra uma única vez e
mostra destroços estáticos; desativar/reativar não restaura HP. Reiniciar cria
vasos novos. Sem drops/XP, física de estilhaços ou participação na limpeza da
sala. São alvos de ambiente sem bloqueio físico/cobertura, fora do centro,
portas e spawns; não alteram o NavMesh. Recompensas futuras exigem dados próprios.

## Progressão
Inimigos concedem orbes de XP; ao subir de nível, cura configurável e +10% de dano (multiplicador por nível definido em código de progressão e testado). Drops possíveis: ouro, poção e arma. Itens, raridades e valores são ScriptableObjects.

F12 (D-005): cada nível ganho concede uma escolha de talento, sem substituir a regra acima. Três opções fixas em SO, até 3 ranks: Lâmina Afiada (+5% de dano por rank, fator próprio), Couraça Firme (+5 de defesa) e Giro Ágil (−0,5 s no cooldown do giro). Subir de nível não pausa; o HUD avisa e **Pausa → Talentos** abre a escolha; Voltar/Esc não gasta. Vários níveis ficam enfileirados e cada clique aceito gasta uma escolha. Morto não escolhe; nova run começa sem talentos. Sem RNG, árvore, respec ou slots. Valores e limites em [TALENTS-F12](docs/TALENTS-F12.md); balanceamento ainda sem playtest humano.

Inventário interativo e raridades integram o escopo da versão 1.0 por aprovação do usuário; ver [decisões de produto](#decisões-de-produto). A implementação preserva a fórmula de progressão e as regras de coleta, capacidade e equipamento aprovadas em D-002.

Regras aprovadas para a implementação da 1.0 (D-002): inventário com 16 espaços, equipamento manual em um slot de arma e um de armadura (ampliado para seis categorias por D-006, abaixo), poções guardadas para consumo com Q e pilhas de até 5 por potência, XP/ouro imediatos e pickup mantido no chão se não houver capacidade. I/Tab abre o inventário e pausa a partida offline. Raridades Comum/Raro/Épico não aplicam multiplicadores ocultos; as armas existentes mantêm bônus +6/+12. Detalhes, valores iniciais e aceite em [regras da 1.0](docs/PRODUCT-1.0-PROPOSAL.md).

O especial aprovado é um giro de espada em 360° (botão direito/E), alcance inicial 3 m, dano 2× o básico calculado, custo 30 de estamina e cooldown 6 s. Estamina inicial máxima 100, regeneração 20/s após 0,8 s sem especial; básico/dash mantêm suas regras. Defesa base 0, fornecida por armadura; dano recebido positivo = max(1, dano × 100/(100 + defesa)). Esses valores novos são configuráveis e sujeitos a playtest, sem alterar a progressão por nível.

F03: a opção **Poção rápida (Q)** escolhe **Primeira** (padrão, menor índice), **Menor potência** ou **Maior potência**, com desempate pelo menor índice atual da bolsa. O HUD mostra a potência escolhida; HP cheio ou jogador morto não consomem. Q permanece disponível na bolsa pausada; **Usar** continua consumindo o item selecionado. Somente a preferência persiste entre runs.

F05: cura efetiva mostra `+HP` e motes rubi; coleta aceita mostra mensagem e glints ciano (XP), latão (ouro) ou marfim (itens); subida de nível mostra o nível final e um anel baixo dourado. Múltiplos níveis numa coleta mantêm a cura e o dano existentes, com uma confirmação do nível final. HP cheio/morto ou bolsa cheia não geram confirmação de cura/coleta rejeitada. Até quatro mensagens aguardam exibição; saturação descarta a pendente mais antiga. Efeitos e mensagens congelam na pausa, inclusive quando Q é aceito na bolsa, e retomam ao continuar. Morte, reset de HP e desativação limpam a apresentação. Flash em zero remove as partículas opcionais; mensagens continuam. Sem hit-stop ou novo shake.

## Direção visual
Low-poly estilizado, silhuetas legíveis na câmera isométrica, cores vivas e contraste adequado entre personagem, inimigos, perigos e cenário. Pedra, tochas e obstáculos destrutíveis no escopo posterior. Não copiar assets ou personagens de jogos existentes.

## Escopo do primeiro marco
Uma arena com jogador, câmera, WASD, mira, ataque básico, dash, um inimigo melee, HP, HUD e reinício. Sem Boss, loot, geração de salas, habilidades especiais ou arte definitiva.

## Parâmetros iniciais ajustáveis (não definitivos)
Vida do jogador 100; inimigo 40; dano do ataque 20; velocidade do jogador 5 unidades/s; cooldown do dash 2 s. Ajustar por playtest, não tratar como valores canônicos.

## Decisões de produto

| ID | Data | Decisão | Alternativa | Motivo e impacto |
| --- | --- | --- | --- | --- |
| D-001 | 13/11/2026 | Inventário interativo e raridades entram na 1.0 | Adiar para expansão | Escolha explícita do usuário; não muda retroativamente o primeiro marco. |
| D-002 | 14/11/2026 | Aprovar integralmente as [regras concretas da 1.0](docs/PRODUCT-1.0-PROPOSAL.md) | Ajustar proposta antes de implementar | Usuário aprovou inventário/pausa/consumo/equipamento/defesa/especial/estamina/menus/opções. Preservar fórmula de dano e armas +6/+12; sem save de campanha nesta versão. |
| D-003 | 15/11/2026 | Nome do jogo passa a ser **Eldoria** | Manter "Dungeon Crawler" | Pedido do usuário. Muda nome do produto/janela, executável `Eldoria.exe` e título do menu. Identificadores técnicos `DungeonCrawler` permanecem. Como PlayerPrefs no Windows usa o nome do produto, opções salvas pela build anterior voltam ao padrão uma vez. |
| D-005 | 16/11/2026 | F12: talento por nível como expansão explícita | Manter só +10%/nível | Pedido do usuário. Três talentos fixos com ranks; termo próprio que preserva +10%/nível, cura de nível e armas. Persistência apenas pelo checkpoint F11 (schema v2). Regras em [TALENTS-F12](docs/TALENTS-F12.md). |
| D-004 | 16/11/2026 | F11: checkpoint local entre salas como expansão explícita | Manter a 1.0 sem save | Pedido do usuário. Um slot local; grava somente com sala limpa e nenhuma sala em combate. Não é save de campanha, autosave de combate nem nuvem. Regras e limites em [CHECKPOINTS-F11](docs/CHECKPOINTS-F11.md). |
| D-006 | 18/11/2026 | Seis categorias de equipamento (arma, armadura, capacete, luvas, botas, acessório) como expansão explícita | Manter só arma e armadura (D-002) | Pedido do usuário. Slots são vínculos a itens da bolsa de 16, sem armazenamento extra; só bônus plano de dano e defesa; sem afixos/conjuntos/crítico. Checkpoint v4. Valores são proposta de playtest. Regras em [EQUIPMENT-SLOTS](docs/EQUIPMENT-SLOTS.md). |

F11 (D-004): ao limpar uma sala, o andar grava HP, nível/XP/ouro, bolsa exata (definição, quantidade, potência), equipamento, estamina/cooldowns, salas concluídas, baús abertos e loot no chão. **Continuar run** só fica habilitado com checkpoint válido e retoma na última sala limpa, sem repetir ondas, XP, cura ou loot. Sair durante combate retorna à última fronteira segura. Nova run, Reiniciar e morte descartam o checkpoint e o backup.

O loop do andar fixo já está implementado; “após MVP” e “primeiro marco” acima identificam a sequência original de construção, não pendências atuais. Combo de três golpes e segurar para repetir, inventário de 16 slots, três raridades, especial e opções fazem parte do produto existente. Estado/evidências em [MVP](MVP.md). Valores atuais inspecionados e limites do playtest em [BALANCE-1.0](docs/BALANCE-1.0.md).

Novos andares/biomas, arma ranged do jogador, afixos, slots além dos seis de D-006, geração procedural, save de campanha (além do checkpoint F11 entre salas), quests, coop, lojas e publicação em loja continuam expansões. O [catálogo](PROMPT_CATALOG.md) contém sugestões para escolha futura; esta auditoria não aprova sua implementação nem muda a identidade low-poly/rig Generic da 1.0.


D-006: a bolsa mostra seis slots — Capacete | Arma | Acessório / Armadura | Luvas | Botas — com atributos e detalhes do item abaixo. Os seis podem ficar equipados ao mesmo tempo; cada um soma uma vez bônus plano de dano (antes de nível/talento) ou defesa. Equipar/trocar/desequipar não usa espaço extra e funciona com a bolsa cheia. Consultar um slot seleciona o item e nunca desequipa; Desequipar é o botão explícito. Sem arma da bolsa, o slot mostra a espada básica. Tabelas de loot da release inalteradas; as quatro peças novas estão no cenário diagnóstico. Arte/ícones/modelos vestíveis pendentes. Valores em [EQUIPMENT-SLOTS](docs/EQUIPMENT-SLOTS.md).

## Segundo andar fixo — F14 (primeiro incremento)

Após limpar as cinco salas do primeiro andar, o painel oferece **Descer ao segundo andar**. A decisão é voluntária para permitir coleta e baú antes da passagem sem retorno; loot no chão fica para trás. Jogador vivo, andar concluído e ausência de pausa são exigidos. HP, nível/XP, ouro, bolsa/equipamento, talentos, estamina e cooldowns permanecem sem cura ou recompensa extra. Uma sala fixa 20×20 de primitivas, dois pilares e 2 melee + 2 ranged existentes validam entrada, navegação, portas e combate; seus números permanecem propostas de playtest. A vitória desse incremento ocorre ao limpar essa sala. Não constitui um segundo andar completo de quatro salas e Boss.

F14 estende F11 com checkpoint v3 na entrada segura do segundo andar, antes de iniciar combate; v1/v2 continuam legíveis. Continuar run resolve o andar por ID explícito e retorna à última fronteira segura. Nova run ou Reiniciar em qualquer andar começa no primeiro; morte descarta checkpoint/backup. Sem retorno entre andares, novo Boss, geração procedural ou arte final. Proposta prévia e evidências em [Reports/F14](Reports/F14/PROPOSAL.md).
