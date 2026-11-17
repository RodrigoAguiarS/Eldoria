# F14 — proposta registrada antes da implementação

Base: main 1d91a36; branch codex/f14-second-fixed-floor. Pedido do usuário: integrar F13 e iniciar F14 por uma sala jogável e transição de teste.

## Incremento e arquivos reservados

Uma sala fixa de primitivas 20×20 no segundo andar, com entrada segura, dois pilares laterais, portas e encontro explícito em SO (2 melee + 2 ranged existentes). Sem regenerar herói/inimigos/arte. Primeiro andar conserva suas cinco salas, encontros, GUIDs e testes. Builder específico cria/atualiza somente assets F14 e acrescenta referências/botão de descida no shell existente. Sistemas: GameSceneFlow, GameRestarter, checkpoint/catalog/session, novos dados de sequência e controlador de transição; testes EditMode/PlayMode; docs afetados.

## Estado e condições

- A passagem ocorre por botão explícito após concluir 5/5 do primeiro andar, vivo e fora da pausa. Nunca no evento da última morte: deixa tempo para coletar XP/loot e abrir o baú. Cliques repetidos são recusados durante a transação.
- HP atual/máximo, nível/XP, ouro, bolsa exata, equipamento, talentos pendentes/escolhidos, estamina e cooldowns permanecem; sem cura, XP, dano ou recompensa artificiais.
- Loot deixado no chão e baús do primeiro andar não acompanham a passagem sem retorno. O botão avisa que recompensas devem ser coletadas antes de descer.
- Checkpoint v3 admite uma fronteira de entrada do segundo andar com zero salas limpas e âncora segura anterior ao trigger; v1/v2 continuam aceitos. Durante combate, Continuar retoma essa entrada ou a última sala limpa, sem salvar combate. Seleção do destino usa floorId e catálogo explícitos, priorizando arquivo principal válido de qualquer andar antes do backup.
- Nova run/reinício em qualquer andar começa no primeiro, zerando estado da run/checkpoint/backup; opções e bindings permanecem. Morte mantém derrota existente e descarta ambos. Concluir a única sala F14 mostra vitória do segundo andar; não inventa outro Boss.
- Referência, catálogo, cena ou dados ausentes recusam a transição com mensagem, sem consumir a run. Falha de escrita também mantém o andar atual.

## Pacing e dificuldade (proposta de playtest, não números aprovados)

Após o Boss há um intervalo voluntário de coleta; entrada segura no novo andar; arena mais larga, duas coberturas laterais e composição mista de quatro inimigos. HP/dano/XP/loot dos inimigos existentes inalterados. Não afirmar dificuldade validada ou balanceamento humano. Uma sala é o primeiro incremento solicitado; extensão para quatro salas + Boss permanece futura.

## Aceite e validação

Travessia das cinco salas preservadas, descida uma vez com estado exato e limpeza da sala F14 somente após última morte. Testar pausa, falha de referência, repetição, entrada/menu/continuar, morte, reinício/nova run; idempotência e hashes do primeiro andar/prefabs/dados protegidos. Executar testes pertinentes, build Windows isolada e abrir cena em PlayMode; percurso normal e qualquer lacuna devem ser reportados separadamente de diagnóstico automatizado.
