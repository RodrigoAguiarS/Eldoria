# F08 — proposta registrada antes da implementação

Base: `2cb7610f6ba5db3a0dca711cf61c6fa1286b120f` (`main` atualizada).
Branch: `codex/f08-room-reward-chest`.
Alterações locais anteriores preservadas separadamente em `f5cf7a1`.

## Regra experimental solicitada

Um baú de primitivas na primeira sala fixa da cena de release. Enquanto a sala
estiver Waiting/Active, o baú permanece bloqueado. Após RoomController.Cleared,
o jogador vivo, próximo (2,5 m), solicita abertura com F. E/direito continuam giro.
Pausa, morte e desativação impedem interação. Abertura e sorteio são únicos por
instância da run; desativar/reativar nunca reseta o estado. Reinício carrega uma
nova cena, com baú fechado e sala ainda bloqueada.

Definição e tabela novas em ScriptableObjects: ouro (10–20) e uma espada de ferro,
ambos garantidos, usando LootRoller e Pickup existentes. Não altera tabelas de
inimigos nem inventário/equipamento. Pickups do baú permanecem até coleta aceita
ou fim da run; recusa por capacidade mantém a mesma definição/quantidade no mundo.
Não há reroll por repetição, recusa, toggle, morte ou reset isolado de HP.

F é o padrão da nova ação Interact, integrada à fonte runtime/remapeamento F04.
Slots anteriores conservam IDs/ordem/schema. Se um documento anterior já usava F,
preservar o controle antigo e escolher uma tecla livre para Interact, exibida no
aviso e no menu; padrão novo continua F.

## Plano e arquivos reservados

- Dungeon: RoomRewardChest e definição; referências explícitas a sala/jogador,
  saída de pickups fora da hierarquia do baú e estado autoritativo por instância.
- PlayerInputReader/InputBindingSet: nova solicitação de interação e rótulo.
- UI: presenter de proximidade/estado e tampa, sem autoridade sobre recompensa.
- Pickup: opção explícita de persistência, preservando o default dos drops antigos.
- Editor: builder idempotente da cena de release e dados novos; build isolada F08.
- Testes EditMode/PlayMode/input: bloqueio, alcance, spam/reentrada, toggles,
  pausa/morte/reinício, capacidade/identidade, compatibilidade e idempotência.
- Documentação: somente regra, contrato, estado e evidências desta entrega.

## Aceite

Compilar; suíte pertinente aprovada; builder duas vezes sem duplicatas ou mudanças
na segunda execução; GUIDs/dados antigos preservados; cena real aberta em PlayMode
gráfico e capturada; build Windows isolada sem erros. Captura diagnóstica não
certifica uma partida normal completa. Sem loja, chaves, procedural ou arte A11.
