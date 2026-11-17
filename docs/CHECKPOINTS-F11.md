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
