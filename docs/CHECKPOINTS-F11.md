# Checkpoint entre salas — F11

Expansão explícita autorizada em 16/11/2026 sobre o produto D-002 sem save.
Base ac6ce43; branch codex/f11-room-checkpoints. Um slot local, sem nuvem.

## Snapshot e fronteira segura

JSON v1 em persistentDataPath/run-checkpoint.json, separado de PlayerPrefs de
opções/bindings. Só gravar com jogador vivo, pelo menos uma sala limpa e nenhuma
sala Active. Coalescer eventos no fim do frame, depois dos drops da última morte.
Coleta, descarte, equipamento, consumo e abertura de baú atualizam o checkpoint
enquanto a fronteira continuar segura. Entrada em combate congela o último save;
sair durante combate retorna a essa fronteira. Não salvar inimigos/projéteis.

Guardar ID/revisão do andar fixo, IDs das salas concluídas, última sala limpa,
HP atual/máximo, nível/XP residual/ouro, slots exatos (ID de definição, quantidade,
potência), índices de equipamento, estamina e cooldown restante do especial/dash.
Índice identifica o slot dentro deste snapshot, nunca uma definição universal.
Retomar no ponto seguro serializado pelo builder da última sala limpa, sem
restaurar movimento, combo, invulnerabilidade ou ataques. Portas de salas
concluídas abrem diretamente; não repetir ondas, morte, XP, cura de nível ou vitória.
Andar 5/5 pode ser retomado para coletar as recompensas restantes.

## Loot e aleatoriedade (decididos antes do código)

Guardar baús abertos e cada pickup pendente com ID de instância, payload já
sorteado, posição e tempo de vida restante (ou persistência até coletar). Bolsa
cheia preserva recompensa e potência. Pickup coletado sai do registro antes do
próximo snapshot; nenhuma recompensa é sorteada durante a carga. Vasos sem
recompensa podem reaparecer inteiros; não participam da progressão persistida.

Um xorshift32 exclusivo da run governa sorteio/scatter de loot e dispersão de
spawn no andar integrado. Persistir seu estado hexadecimal não zero; apresentação
não o consome. Salas futuras repetem a sequência a partir da fronteira; combate
descartado pode repetir resultados. Não se promete anti-save-scumming.

## Validação e arquivo

IDs de definições explícitos nos SOs e catálogo serializado, independentes de
nomes/caminhos em runtime. IDs de salas e revisão do andar são estáveis. Validar
documento inteiro antes de mutar runtime: versão, limites finitos, capacidade,
slots/tipos/potências, equipamento, referências, salas, baús e IDs de pickup únicos.
Duplicatas de uma definição em slots diferentes são legítimas. Rejeitar versão
futura/ID ausente em vez de perder itens silenciosamente; v1 é o primeiro schema,
sem migração de saves anteriores inexistentes. Revisões futuras exigem migração.
Desde a correção de HUD do F12, o texto no andar mostra só falhas (gravação ou
descarte não concluídos, snapshot inválido); status rotineiros não aparecem.
F12 introduz **v2** (talentos `{id, rank}`); v1 é migrado sem talentos, com as
escolhas dos níveis ganhos pendentes. Ver [TALENTS-F12](TALENTS-F12.md).

Escrever temporário no mesmo diretório, flush e troca atômica por File.Replace
com backup anterior. Carga inválida tenta backup validado e avisa na UI. Falha
de disco não trava gameplay; informar que o checkpoint não foi salvo. Limitar
tamanho do arquivo e contagens. Continuar só aparece habilitado para snapshot
inteiramente válido. Nova run/Reiniciar descartam slot e backup; morte encerra
a run e descarta checkpoint. Ao retornar ao menu/sair, finalizar atualização
segura pendente; nunca criar autosave de combate.

## Implementação e aceite

Arquivos: modelos de inventário/XP/HP/recurso (restauração sem recompensar),
FloorController/RoomController (restauração silenciosa), registro de loot/RNG,
serviço de arquivo/validação/orquestração, GameSceneFlow/GameRestarter, presenter
de status e builder idempotente de MainMenu + andar de release. Preservar arte,
GUIDs, opções, bindings e demais cenas diagnósticas.

Testar round-trip com slots vazios/duplicatas/potências/equipamento; truncamento,
versão futura, referência ausente, valores/capacidade inválidos; backup e falha
de escrita; checkpoints repetidos; carga sem XP/loot duplicados; bloqueio durante
combate; Nova run/Reiniciar/morte; menu e ciclo real salvar/sair/retomar. Rodar
EditMode/PlayMode e inspeção gráfica com a Unity 6000.6.9f1 instalada. Evidências
novas em Reports/F11, sem substituir resultados históricos.


## Extensão D-007 — schema v5

Equipamento separado da bolsa ([EQUIPMENT-SLOTS](EQUIPMENT-SLOTS.md)). `slots` passa a conter
só a bolsa e `equipment` guarda exatamente seis IDs persistentes na ordem dos slots (arma,
armadura, capacete, luvas, botas, acessório; `""` = vazio). Em v5, `weaponIndex`/`armorIndex`
valem −1 e `gearIndices` fica vazio; documento v5 sem `equipment`, com tamanho diferente, com
item ausente do catálogo, de outra categoria ou com os campos antigos preenchidos é recusado
antes de qualquer mutação. v1–v4 continuam legíveis: a validação completa ocorre antes e cada
item referenciado pelos índices antigos vai para o seu slot, esvaziando somente o seu espaço
original; os demais itens mantêm posição, nada é removido por ter a mesma definição e a
migração não concede a arma inicial. A chegada ao segundo andar converte a origem para v5.
Continuar restaura exatamente o equipamento salvo, inclusive com o slot de arma vazio.
Talentos, loot no chão, IDs, backup e fronteiras seguras inalterados; nenhum segundo sistema de save.

## Extensão D-006 — schema v4

Seis slots de equipamento ([EQUIPMENT-SLOTS](EQUIPMENT-SLOTS.md)). O documento acrescenta
`gearIndices` com exatamente quatro índices de slot da bolsa, na ordem capacete, luvas,
botas, acessório (−1 = vazio), além de `weaponIndex`/`armorIndex`. Validação antes de qualquer
mutação: cada índice é −1 ou aponta para um item da categoria correspondente; v4 sem o campo
ou com tamanho diferente é recusado, de modo que campo ausente nunca é lido como zero. v1, v2
e v3 continuam legíveis com os quatro slots novos desequipados; um documento anterior ao v4
que traga `gearIndices` é malformado. Pickups `Gear` exigem v4. A chegada ao segundo andar
normaliza uma origem antiga para v4 com os slots novos vazios. Talentos, loot no chão,
fronteiras seguras, backup e regras de restauração inalterados; nenhum segundo sistema de save.

## Extensão F14 — schema v3

O payload continua preservando talentos de v2 e aceita leitura v1/v2. Apenas o catálogo do segundo andar habilita uma fronteira com zero salas limpas (v3), resumeRoom igual ao primeiro ID e âncora fora do trigger. É gravada ao descer, antes da carga, preservando HP/XP/nível/ouro/bolsa/equipamento/talentos/recursos e removendo estado de salas/baús/pickups do andar anterior. Sem cura ou concessão de recompensa. Combate não sobrescreve essa entrada; Continuar retoma a entrada até a primeira limpeza. A seleção de andar valida o principal em todos os catálogos antes de usar backup. Nova run, Reiniciar e morte mantêm descarte de ambos; reinício parte do primeiro andar. Ver [F14](../Reports/F14/PROPOSAL.md).
