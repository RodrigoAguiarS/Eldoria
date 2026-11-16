# F07 — Linha de visão do ranged

Base: `850479bbaf610b9b52b48406a8b5cdda6a642556`, `main` atualizada. Branch: `codex/f07-ranged-line-of-sight`. Unity 6000.6.9f1, pacotes preservados. Testes executados numa cópia isolada em `G:/DungeonCrawler/F07Validation`, para preservar a sessão aberta do projeto original.

## Comportamento

- `EnemyData` configura `lineOfSightMask` (Obstacle/Door, 12288) e `lineOfSightInterval` (0,1 s na produção; mínimo runtime de 0,05 s).
- `EnemyCombat` consulta da posição da raiz + `projectileHeight` até a posição atual do alvo + a mesma altura. `ProjectileOrigin` é compartilhada com o disparo do pool: nenhum offset de olho/boca divergente. O rig atual continua sem socket de disparo próprio.
- Consultas normais compartilham cache por Transform do alvo e relógio escalado. O início de cada ataque e o impacto exigem consulta fresca, mesmo se a amostra temporizada ainda estiver válida. Durante windup, perda amostrada cancela permanentemente aquele tiro; abrir a porta novamente exige novo windup. Cooldown já iniciado não é devolvido.
- Colliders sólidos de parede/porta bloqueiam. Triggers e atores nas layers Player/Enemy ficam fora da consulta. Nenhuma busca global ou alocação de array por consulta.
- Brain preserva Idle/Chase/Attack/Dead. Sem visão, persegue via NavMeshAgent; destino e recuo continuam limitados por thinkInterval (0,2 s no asset). Aproximação ranged usa raio do agente como distância de parada, evitando parar no alcance longo atrás da cobertura. Attack mantém parada e a regra de recuo existente.
- Pausa congela windup/cooldown/amostras. Retomada verifica visão antes de disparar. OnDisable do combate/brain descarta pendências; brain para o agente e retoma por Idle. Troca de alvo cancela ataque antigo. Morte durante desativação do brain é reconciliada no OnEnable.
- Pool, dano, tempos de ataque, HP, velocidade, drops e Boss não foram rebalanceados. O ajuste em Stop limpa o caminho antes de estabelecer isStopped.

`RangedLineOfSightBuilder.Integrate` usa AssetDatabase para persistir somente os dois campos novos do asset ranged existente. Não reconstrói salas/prefabs/arte e mantém GUID e referências compartilhadas pelos prefabs primitivo e gerado. A repetição é verificada byte a byte em EditMode.

## Validação e limites

| Evidência | Resultado e alcance |
| --- | --- |
| [EditMode-verified.xml](EditMode-verified.xml) | **159/159** na suíte completa, zero ignorados; incluindo migração idempotente que preserva tuning, GUID e referências dos dois prefabs ranged. |
| [PlayMode-verified.xml](PlayMode-verified.xml) | **32/32**, zero ignorados: 18 casos F07, 5 integração do andar, 3 salas, 2 pausa/input, 2 Health/combate, 1 Boss e 1 benchmark de pool. |
| [Visual.xml](Visual.xml), [contexto](Visual/context.txt) | **1/1** explícito no Editor gráfico, cena real de release em **2560×1440**. As três imagens foram abertas e inspecionadas: cobertura fechada sem windup; porta abrindo com tint de preparo; pose de disparo com pool ativo. O frame de disparo coincide com a saída na origem e não comprova por si só a trajetória completa do projétil. |
| [windows-build.json](windows-build.json), [artefato](windows-artifact.json) | **Succeeded**, zero erros, 496 avisos de shaders (496 linhas de Shader warning, majoritariamente inferência/Sentis). Windows x64, PE `0x8664`, BuildOptions.None. Executável local: `Builds/F07/Eldoria.exe`. |
| [unity-static.json](unity-static.json) | Zero erros/avisos de metas, GUIDs e scripts. |
| [asset-delta.json](asset-delta.json), [delta da cópia](validation-copy-after-editmode.json), [ressincronização](validation-copy-restored.json) | No projeto original, somente Enemy_Ranged.asset mudou entre assets serializados existentes. Dados de balanceamento, prefabs/cenas, GUIDs, pacotes, ProjectSettings e relatórios históricos preservados. Testes de builders mutam a cópia descartável; suas mudanças não são copiadas de volta. |

[Auditoria de entrega](audit-delivery.json): cópias instaladas sincronizadas e links locais resolvidos. O snapshot `validation-copy-assets.json` foi colhido durante a build e registra o preloadedAsset temporário do Input System; não foi promovido ao projeto original.

XMLs de tentativas com falhas são preservados; nomes `final` de uma tentativa não significam aprovação. `PlayMode.xml` teve 23/31, `PlayMode-final.xml` 31/32 e `Integration-diagnostic.xml` registrou o diagnóstico do posicionamento. A aprovação é `PlayMode-verified.xml`.

A primeira rodada tinha um contador de eventos da fixture sem reset por caso; corrigido. O teste de parada do agente motivou a ordem ResetPath → isStopped. O diagnóstico confirmou que a linha inicial estava livre após abrir explicitamente a porta. A falha restante vinha do cenário de integração começar no limite de keepDistance, onde a colocação/aproximação no NavMesh põe o ranged na regra de recuo. A fixture agora abre a porta de entrada, exige corredor fisicamente livre e posiciona o ranged no meio da faixa keepDistance/attackRange; as asserções de projétil, dano real e pool permanecem.

Limites: uma obstrução que surge e desaparece inteiramente entre amostras pode não ser detectada; o impacto sempre consulta novamente. A consulta é um segmento fino, não o volume do SphereCast do projétil: bordas estreitas podem liberar visão e ainda interceptar o projétil, que mantém sua colisão autoritativa. Somente colliders nas layers configuradas bloqueiam; transparência visual não muda física. O NavMesh tenta alcançar o alvo; não há busca tática por posições de tiro nem garantia de caminho para alvo inalcançável. O tint de aviso existente pode concluir sua duração original após cancelamento; ele não causa disparo. Sem alegação de zero GC global, benchmark de LOS ou melhoria de FPS.

## Reprodução

1. Abrir com Unity 6000.6.9f1; menu **Tools / Dungeon Crawler / Enemies / Integrate Ranged Line Of Sight (F07)**, ou executar `DungeonCrawler.Editor.RangedLineOfSightBuilder.Integrate` em batch.
2. Rodar EditMode completo. PlayMode: filtro `RangedLineOfSightTests;CombatPlayModeTests;RoomSystemsPlayModeTests;ProjectilePoolPerformanceTests;BossPlayModeTests;InventoryPauseInputTests;FloorIntegrationTests`. Usar destinos novos de XML/log e `DUNGEON_CRAWLER_TEST_REPORT_DIR` próprio; não usar `-quit` junto de `-runTests`.
3. Captura gráfica explícita: `DungeonCrawler.Tests.PlayerInput.RangedLineOfSightReleaseTests.Capture_ReleaseRangedBehindDoorAndFiring`, sem batchmode/nographics. Carrega a cena real e prefab gerado; cria ator/porta diagnósticos e clona os dados apenas para zerar velocidade/keepDistance e manter enquadramento. Dano, windup, cooldown, LOS e pool são os atuais. O teste funcional separado prova contorno da parede; a captura não é uma run normal.
4. Build isolada: `DungeonCrawler.Editor.RangedLineOfSightBuilder.BuildWindowsValidation`, target win64, gera `Builds/F07/Eldoria.exe` e `Reports/F07/windows-build.json`, recusando sobrescrever o relatório.
5. Roteiro humano: na sala ranged, ficar atrás de parede/porta, sair de cobertura, entrar durante windup, pausar/despausar e aproximar para observar recuo. Conferir o ciclo novo de preparo após cancelar. Completar a run normal e derrota/reinício continua sendo aceite humano separado.

APIs consultadas: [Physics.Linecast](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/physics/linecast) e a instalação/pacote local AI Navigation 2.0.16. Fontes de Tools e cópias instaladas permanecem sincronizadas; auditoria e checker estático têm evidências próprias nesta pasta.

As capturas diagnósticas não comprovam vitória 5/5, derrota/reinício, aprovação artística geral ou smoke interativo do executável Windows. Não houve nova run normal no player Windows nesta tarefa. A validação visual ocorreu no PlayMode gráfico da cena de release. Nenhum merge ou publicação foi feito.
