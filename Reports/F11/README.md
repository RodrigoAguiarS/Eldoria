# F11 — checkpoint entre salas

Base `ac6ce43` (main), branch `codex/f11-room-checkpoints`. Unity 6000.6.9f1,
sem troca de pacotes. Regras/limites: [CHECKPOINTS-F11](../../docs/CHECKPOINTS-F11.md);
decisão D-004 em GAME_DESIGN.

Implementação iniciada pelo Codex e concluída pelo Claude (revisão, correções
de teste, regressão completa, documentação e MR).

## Evidências (batchmode `-nographics`)

| Arquivo | Escopo | Resultado |
| --- | --- | --- |
| [editmode.xml](editmode.xml) | `RunCheckpointTests` | **30/30**: round-trip com duplicatas/potências/equipamento/loot no chão, arquivo truncado/`null`/`[]`/versão 999, 18 snapshots inválidos recusados antes de mutar, backup rotativo, `.tmp` abandonado, arquivo travado, RNG independente de `UnityEngine.Random`, restauração sem cura/dano/nível, builder idempotente preservando GUIDs |
| [playmode.xml](playmode.xml) | `RunCheckpointPlayModeTests` (Codex) | 6/7: tolerância do teste de posição estrita demais (ver correções) |
| [playmode-r2.xml](playmode-r2.xml) | `RunCheckpointPlayModeTests` | **7/7** após a correção |
| [editmode-full.xml](editmode-full.xml) | Suíte EditMode completa | **205/206**: falha pré-existente da F10, fora da F11 (ver limitações) |
| [playmode-full.xml](playmode-full.xml) | Suíte PlayMode completa, 1ª execução | 153/166, 2 falhas, 11 capturas gráficas ignoradas: baú intermitente e benchmark sem diretório isolado |
| [playmode-r9](playmode-r9.xml)/[r10](playmode-r10.xml)/[r11](playmode-r11.xml) | `RoomRewardChestTests` + `RunCheckpointPlayModeTests` | **15/15** em 3 execuções seguidas após a correção do baú |
| [playmode-full2.xml](playmode-full2.xml) | Suíte PlayMode completa, `DUNGEON_CRAWLER_TEST_REPORT_DIR=bench-full2` | **155/155** funcionais, 11 capturas gráficas ignoradas por design |
| [unity-static.json](unity-static.json) | `check_unity_project` | 0 erros, 0 avisos, 1244 arquivos |

Os testes PlayMode carregam a cena de release real e retomam pelo botão
**ContinueRun** do MainMenu salvo: ida ao menu, Continuar e conferência de estado.
Cobrem também combate (o arquivo não muda e XP/loot de combate são descartados),
morte da última unidade com XP pendente restaurado uma vez, baú aberto sem novo
sorteio, bolsa cheia preservando a identidade/potência do pickup, Nova run/Reiniciar/morte
apagando o slot e o backup, e arquivo inválido desabilitando Continuar.

## Correções desta etapa

- `RoundTrip_ExactBag...`: a âncora fica 0,1 m acima do piso e a gravidade
  assenta o CharacterController antes de o teste desligar o movimento. Agora o
  teste compara XZ < 0,01 e Y dentro de [âncora − 0,15; âncora + 0,01].
- `RoomRewardChestTests.DisabledDuringClear...` falhou em 2 de 4 execuções.
  O diagnóstico mostrou ouro e arma nascendo a 0,94 m do ponto de abertura, no
  limite do trigger de coleta. Um passo de física no frame seguinte coletava o
  ouro. A gravação do checkpoint (fsync) ao limpar a sala alonga esse frame e
  torna o passo mais provável. O teste agora afasta o player antes de desativar o
  baú; a regra verificada (drops sobrevivem à desativação) não mudou.

## Limitações e pendências

- **Sem sessão interativa no Editor gráfico.** O ciclo salvar → sair → retomar foi
  exercitado nas cenas reais por PlayMode em batchmode, não jogado à mão, e não
  há captura visual do botão Continuar/status. Falta fazer essa verificação manual.
- **Regressão F10 (já na main):** `ItemsCombatAssetTests.IntegrateTwice_IsIdempotentAndPreservesPickupIdentity`
  falha com `Inventory slot missing: 0`. `TextScaleSceneBuilder` (F10) moveu
  `Slot_NN` para dentro de um grid, e `InventorySceneBuilder.ConfigureItemVisuals:277`
  ainda procura em `Window/`. A F11 não toca esse arquivo. Precisa de correção própria.
- Cada mudança segura (coleta, equipamento, poção) grava em disco com
  `Flush(true)` no mesmo frame. Isso é aceitável para o tamanho atual (poucos KB),
  mas pode causar micro-hitch em disco lento. Se aparecer no perfil, uma melhoria
  possível é coalescer as gravações por tempo.
- Suítes completas sem `DUNGEON_CRAWLER_TEST_REPORT_DIR` sobrescrevem relatórios
  históricos rastreados (`Reports/Leadership/*`, `Reports/Task02/*`), e a suíte
  EditMode reserializa prefabs/materiais/ProjectSettings. Esses efeitos colaterais
  ficaram fora do commit, mas ainda estão na árvore de trabalho.
