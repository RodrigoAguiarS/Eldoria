# F08 — cinco baús e correção das tochas

Base: `77e4b13`, `main` atualizada. Branch: `codex/f08-all-rooms-light-fix`.
Escopo solicitado pelo usuário em [PLAN](PLAN.md). Unity 6000.6.9f1, pacotes
preservados. Validação em `G:/DungeonCrawler/F08AllRoomsValidation` porque o Editor
do projeto original estava aberto. A sessão original foi apenas inspecionada;
nenhuma cena do usuário foi reconstruída para rodar testes.

## Resultado

Um baú independente por sala, nas quatro salas normais e na sala do Boss. Cada um
responde a F somente após a própria sala limpar e mantém o sorteio único, alcance,
pausa/morte/reinício, persistência de pickups e retry de capacidade da entrega F08.
E continua giro. Os cinco presenters usam labels distintas: um baú distante não
apaga o aviso de outro. Os avisos ficam ocultos durante a tela terminal.

**Coletar recompensas** fecha a tela de vitória e libera sala/bolsa para o loot do
Boss; `FloorController` continua concluído em 5/5. O botão fica entre Reiniciar e
Voltar ao menu, com teste de não sobreposição. Essa apresentação não concede
itens nem altera combate/progressão.

As chamas voltam às tochas das paredes. O reparo usa a lógica existente de
`ProductionAnimationIntegrator`, exposta para a cena recebida sem gravar relatórios
históricos. Não altera os transforms das tochas nem intensidade/posição/cor das
luzes. `RoomRewardSceneBuilder` compõe apenas a cena de release e reaproveita os
Controles já integrados; não reconstrói prefabs de sala ou outras cenas.

## Causa da reincidência

A inspeção anterior ao reparo encontrou **30 chamas órfãs**, todas como filhas da
raiz da sala em posição local zero. As dez luzes pontuais estavam nos objetos das
tochas nas paredes; a luz direcional também permaneceu igual. Não era necessário
recriar iluminação, materiais ou cenário.

`DungeonMvpSceneBuilder.Build` chamava `GeneratedArtApplier.ApplyEnvironment`, que
recriava a decoração de todos os prefabs de sala e regravava a cena gerada de
release. O teste de preservação restaurava os prefabs, mas não incluía essa cena
gerada no snapshot. Os fileIDs dos pais ficavam incompatíveis com os filhos
adicionados na cena, deixando as chamas no centro.

Agora o builder M1 chama `ApplySceneEnvironment` somente para sua arena. A fonte em
Tools e a cópia instalada estão sincronizadas. O teste verifica bytes da cena de
release e dos prefabs de sala durante cada uma das duas reconstruções, e inclui
cena gerada/meta no snapshot restaurado como proteção adicional.

## Evidências

| Evidência | Resultado e alcance |
| --- | --- |
| `Before/scene.json`, `After/scene.json`, `scene-comparison.json` | Antes: 1 baú e 30 chamas órfãs. Depois: 5 baús, 30 chamas nas tochas e zero órfãs. Os transforms das 30 tochas e as configurações das 11 luzes são idênticos antes/depois. |
| `Edit01/results.xml` | **161/161** na suíte completa, zero falhas/ignorados. Composição verifica cinco salas únicas, referências, avisos individuais, chamas corretas, luzes/prefabs preservados e integração idempotente byte a byte. Rebuild M1 preserva salas e release. |
| `Edit02/results.xml` | **3/3** no reteste de composição/preservação após o ajuste visual final do botão e da ocultação do aviso durante vitória. |
| `Play01/results.xml` | **132 aprovados, zero falhas, 9 explícitos ignorados**. Suíte completa, incluindo os oito casos funcionais de baús, controles, coleta recusada/identidade e fluxo completo pelas cinco salas com vitória → voltar → bolsa → baú do Boss. |
| [Play03/results.xml](Play03/results.xml) | **20/20**, zero ignorados: reteste funcional de baús/menus/remapeamento após os ajustes finais, com asserções de labels ocultas na vitória e botões sem sobreposição. Play02 foi interrompido por seleção de uma captura explícita legada em batch; motivo registrado em Play02/incomplete.txt, sem aprovação atribuída a essa tentativa. |
| [Visual02/results.xml](Visual02/results.xml), [contexto](Visual02/context.txt) | **1/1** no Editor gráfico em **2560×1440**. As cinco salas têm baús disponíveis; chamas visíveis nas paredes. Tela de vitória inspecionada com três botões legíveis, aviso F oculto; retorno à sala e baú do Boss aberto confirmados. A captura anterior também foi inspecionada e motivou a correção de layout. |
| [windows-build.json](windows-build.json), [artefato](windows-artifact.json) | Build final Windows x64 **Succeeded**, zero erros, 496 avisos conhecidos de shader. DLL final contém o controle de visibilidade terminal; cena validada transferida com SHA256 conferido. Executável local `Builds/F08-AllRooms/Eldoria.exe`. |
| [Smoke/summary.json](Smoke/summary.json) | Build Windows executada com início diagnóstico no andar, amostra de 30 s em Direct3D12 e fechamento por CloseMainWindow. Log sem exceções/Missing Script; não representa aceite de run normal completa. |
| [transfer.json](transfer.json) | Somente a cena de release transferida entre assets serializados. Prefabs conferidos iguais à cópia validada; todos os outros assets da baseline original preservados. |

A primeira captura (`Visual.xml`/`Visual`) passou as asserções funcionais, mas a
inspeção das imagens encontrou o botão novo sobreposto ao Voltar ao menu e o aviso
F pintando sobre a vitória. Foi corrigido antes da entrega; novos testes verificam
ausência de sobreposição e aviso oculto. A primeira build (`Build01`) é provisória,
anterior a esse ajuste, e não é o artefato final de entrega.

[Auditoria final](audit-final.json): fontes/cópias sincronizadas e zero links locais
quebrados. [Checker final](unity-static-final.json): zero erros/avisos. O primeiro
checker apenas avisou sobre a meta do novo auditor, ainda não importado no projeto
original; a meta gerada na cópia validada foi transferida e o checker repetido.

Logs `.log`, amostras brutas e caches locais não são versionados. XML/JSON e
capturas pertencem somente a esta revisão; evidências antigas F08 não foram
substituídas.

## Reprodução e limites

1. No projeto original atualizado, iniciar por **Tools / Dungeon Crawler / ▶ 1.
   Jogar versão atual (menu principal)**. Verificar um baú em cada sala, chamas nas
   paredes e aviso F somente próximo. A cena oficial é Floor_01_GeneratedCharacters.
2. Limpar cada sala, abrir com F e repetir a tecla. Cada baú abre só uma vez;
   recusa por bolsa cheia deixa o mesmo pickup no chão. E permanece giro.
3. Derrotar o Boss, clicar **Coletar recompensas**, liberar espaço na bolsa se
   necessário e abrir o quinto baú. O andar continua concluído.
4. Builder: **Tools / Dungeon Crawler / Rewards / Integrate Room Chest (F08)**.
   Reexecutar não duplica baús/chamas nem reposiciona luzes.
5. Build isolada: `DungeonCrawler.Editor.RoomRewardSceneBuilder.BuildAllRoomsValidation`,
   destino `Builds/F08-AllRooms/Eldoria.exe`, relatório novo
   `Reports/F08-AllRooms/windows-build.json` (recusa sobrescrita).

Captura explícita: `RoomRewardChestTests.Capture_AllRoomsAndTorchPositions`, em
Editor gráfico sem batchmode/nographics. Usar destinos novos e
`DUNGEON_CRAWLER_TEST_REPORT_DIR` exclusivo para cada execução. O diagnóstico mata
a onda por dano programático e mantém o jogador invulnerável; não é aceite humano
de uma run normal nem revisão artística geral. Os baús continuam sendo primitivas,
sem colisão, usando a mesma tabela já aprovada no F08. Sem loja/chaves/procedural.
