# F05 — Feedback de cura, coleta e nível

Base `4a1cada` (`main` e `origin/main` atualizadas por fetch em 16/11/2026); branch `codex/f05-feedback`, worktree `G:/DungeonCrawler/f05-feedback`. A worktree foi autorizada pelo usuário para preservar três alterações desconhecidas na pasta original, sem troca de branch ou edição nessa pasta. Unity 6000.6.9f1, URP 17.6.0 e pacotes preservados.

## Comportamento e composição

- Cura efetiva: payload imutável com valor efetivo, HP capturado e origem; `+HP` e motes rubi. HP cheio/morto/quantidade inválida não publicam cura aceita. Reset publica evento próprio e invalida a confirmação ainda pendente em Changed.
- Coleta aceita: novo payload LootDrop conserva quantidade/tipo/definição; a confirmação Collected existente continua alimentando Pickup no pool de áudio. Capacidade recusada mantém o pickup no chão. Sem novo sorteio ou reaplicação do drop.
- Nível: uma confirmação do nível final, incluindo múltiplos níveis numa coleta; cura de nível conserva a fórmula original e não dispara um segundo efeito de cura. LeveledUp usa snapshot antes dos callbacks.
- Três ParticleSystems preconfigurados e reutilizados por jogador: sete motes rubi para cura, quatro glints ciano/latão/marfim para coleta, doze motes num anel dourado para nível. Cada retrigger limpa/reusa seu emissor, com limite de 12 partículas por emissor. Nenhuma alteração no trail/anel/faíscas de combate, dano, XP, cooldowns, sorteio ou hit-stop.
- Fila fixa de quatro avisos pendentes, mais um visível; saturação descarta o pendente mais antigo. String é formatada apenas ao exibir um aviso. Pausa congela apresentação/temporizadores; Q aceito na bolsa fica aguardando retomada. Morte, Health.Configure/reset e desativação limpam avisos/partículas. Reativação não reproduz eventos passados nem acumula inscrições.
- Flash controla a opacidade das partículas; zero as suprime, mantendo mensagens. Sem novo shake ou flash no herói. Cura/nível reutilizam SoundCue.Ui no pool/mixer SFX existente, respeitando mute/prioridade/repetição. Áudio de coleta continua único.
- Consumo e coleta têm guardas de reentrada durante mutação e notificações, com finally para liberar a guarda. Isso impede observadores de repetir consumo/XP/recompensa; chamadas sequenciais continuam normais.

`RewardFeedbackSceneBuilder.Integrate` usa APIs do Editor para compor Player.prefab, MVP_Arena, Floor_01 e Floor_01_GeneratedCharacters. A integração geral de inventário também conhece a composição F05. Não cria arte, não reconstrói salas, não muda dados ou GUIDs de assets existentes. Reusa o material FX_Spark já integrado; A04–A06 são opcionais e não foram produzidos.

## Evidências

| Evidência | Resultado e alcance |
| --- | --- |
| [EditMode-verified.xml](EditMode-verified.xml) | **158/158** na suíte completa, incluindo cura, progressão, dados, assets e builders. |
| [PlayMode-final.xml](PlayMode-final.xml) | **83/83**, zero ignorados. [Seleção por método](playmode-selection-methods.txt); incluindo casos parametrizados, dez casos F05, áudio, inventário/pausa/menus, combo/especial e seis regressões ItemsCombat. |
| [Composition-final.xml](Composition-final.xml) | **1/1** após restaurar assets de testes e integrar somente F05; bytes idênticos ao repetir builder, referências F05 resolvidas nas três cenas, sem Missing Script e VFX da release preservados. |
| [Benchmarks-final-calibrated/reward-feedback.json](Benchmarks-final-calibrated/reward-feedback.json) | 64 operações de aquecimento, 1.000 trios de callbacks/efeitos: **zero alocações**; controle `new byte[4096]` detectado (uma alocação). Três filhos antes/depois. Exibição de 1.000 avisos: 7.000 alocações, registradas separadamente. |
| [Visual-final.xml](Visual-final.xml) | **1/1** no Editor gráfico, com compilação síncrona temporária/restaurada. Seis imagens finais abertas e conferidas: herói/cenário visíveis, motes e textos legíveis em 1280×720 e 1920×1080. |
| [windows-build.json](windows-build.json), [windows-artifact.json](windows-artifact.json) | **Succeeded**, zero erros, 509 avisos de shader; Windows x64/PE `0x8664`, executável em `Builds/F05/Eldoria.exe`. BuildOptions.None, sem Development. |
| [audit-final-normalized.json](audit-final-normalized.json), [asset-delta.json](asset-delta.json) | Cópias instaladas sincronizadas e links locais resolvidos. Só quatro assets de produção mudaram (prefab/3 cenas); dados, pacotes, Tools, ProjectSettings e relatórios históricos preservados. Hashes das três alterações da pasta original conferidos. |
| [unity-static-verified.json](unity-static-verified.json) | Metas/GUIDs/scripts: zero erros/avisos. |

Os XMLs de tentativas anteriores são preservados, inclusive falhas, sem apresentá-los como aprovados. O GC.Alloc Recorder tem unidade TimeNanoseconds para Value; Count registra a quantidade de alocações. Não se reivindica contagem de bytes ou zero GC global.

[Visual.xml](Visual.xml) passou as asserções, mas suas [imagens iniciais](Visual/1280-healing.png) foram **reprovadas na inspeção visual**: o Editor ainda mostrava o shader ciano de carregamento. Esse [placeholder é documentado pela Unity](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/EditorSettings-asyncShaderCompilation.html). A repetição [Visual-final.xml](Visual-final.xml) passou com compilação síncrona temporária (configuração anterior restaurada) e rejeição de frames dominados pelo placeholder; as seis imagens finais foram abertas e conferidas. Não confundir resultado do NUnit com aceite visual.

Uma seleção PlayMode por classe incluiu Capture_ReleaseFloorEvidence, que aguarda WaitForEndOfFrame e não termina em batch. O runner foi interrompido sem XML; não conta como teste aprovado. A seleção final usa métodos sem parâmetros no filtro para incluir corretamente todos os TestCases e excluir capturas. O benchmark inicial registrou Value do marker com rótulo de bytes; GC.Alloc usa TimeNanoseconds neste Recorder, portanto somente Count é válido como número de alocações. O schema final registra a unidade e não reivindica bytes. Outro reteste contabilizou a formatação do nome da unidade dentro da janela medida; a calibração final exclui esse metadado antes de iniciar a amostra.

A primeira suíte EditMode encontrou a nova validação isActiveAndEnabled indevida no caminho legado de coleta usado por testes isolados e um erro de restauração do setup vazio no teste novo. Corrigidos. A segunda revelou que a arena primitiva não possui PlayerAttackVfxPresenter; o teste foi corrigido para exigir o componente na cena de release e preservar referências em qualquer cena que o possua. Não adicionamos VFX de combate a cenas que não os tinham.

## Arquivos e capturas finais

Domínio: `Core/Health.cs`, `HealthModel.cs`, `HealingAccepted.cs`, `Systems/PlayerProgression.cs` e `Systems/Inventory/PlayerInventory.cs`. Apresentação: `UI/PlayerRewardFeedback.cs` e `Audio/GameplayAudioBridge.cs`. Composição: `Editor/RewardFeedbackSceneBuilder.cs`, hook em `InventorySceneBuilder.cs`, Player.prefab e três cenas de gameplay. Testes: HealingFeedbackTests, RewardFeedbackCompositionTests, RewardFeedbackPlayModeTests, RewardFeedbackVisualTests e um caso adicional no GameAudioPlayModeTests. Contratos/estado: GAME_DESIGN, ARCHITECTURE, MVP, NEXT_STEPS e docs/BUILD-WINDOWS.

- 720p: [cura](Visual-final/1280-healing.png), [coleta](Visual-final/1280-collection.png), [nível](Visual-final/1280-level.png).
- 1080p: [cura](Visual-final/1920-healing.png), [coleta](Visual-final/1920-collection.png), [nível](Visual-final/1920-level.png).

Git diff/check dos fontes não apontou whitespace; Player.prefab mantém o espaço canônico gerado pela Unity no campo vazio `m_Name`, único aviso do diff/check integral.

A leitura visual desta tarefa cobre os efeitos discretos e mensagens da F05, não aprovação artística integral do jogo. O executável foi compilado e verificado como PE x64; não houve novo smoke interativo ou percurso completo no player Windows nesta entrega. O ajuste final depois da build foi somente no teste gráfico, para recusar placeholder; domínio/apresentação/assets de produção da build são os validados.

## Reprodução

1. Abrir esta worktree com Unity 6000.6.9f1. Menu **Tools / Dungeon Crawler / Feedback / Integrate Rewards (F05)**, ou executar `DungeonCrawler.Editor.RewardFeedbackSceneBuilder.Integrate` em batch.
2. Executar EditMode; PlayMode filtra RewardFeedbackPlayModeTests, GameAudioPlayModeTests e regressões de inventário/progressão/combate/menus. Definir `DUNGEON_CRAWLER_TEST_REPORT_DIR` para um destino novo. Não combinar `-quit` e `-runTests`.
3. Teste explícito gráfico: `DungeonCrawler.Tests.PlayerInput.RewardFeedbackVisualTests.Capture_ReleaseFeedback720pAnd1080p`, sem batchmode/nographics, com diretório de relatório novo. Carrega a cena real de release; cura/XP são comandos diagnósticos, não uma run normal.
4. Build isolada: `DungeonCrawler.Editor.RewardFeedbackSceneBuilder.BuildWindowsValidation`, target win64. Saída `Builds/F05/Eldoria.exe`; recusa substituir Reports/F05/windows-build.json. Preservar a evidência e escolher outro destino para repetir.
5. Roteiro humano: [docs/BUILD-WINDOWS](../../docs/BUILD-WINDOWS.md), regressão F05. Revisar o merge request para main; merge somente após aprovação do usuário.

## Limites

Medição de callbacks/partículas é sintética no Editor e separa a formatação de texto; não representa GC total, frame time da build ou custo de uma run normal. O callback de nível é repetido por delegate no benchmark para aquecimento sem adicionar XP fictício à progressão; comportamento real de múltiplos níveis é testado separadamente. A medição usa ProfilerRecorder GC.Alloc com controle positivo; o contador de bytes por thread anteriormente inválido nesta máquina não é usado.

Capturas diagnósticas da cena de release não comprovam vitória 5/5, derrota/reinício com teclado físico, aprovação artística, ultrawide físico ou outro hardware. Não houve merge nem publicação de release. Alterações automáticas de builders em assets/relatórios históricos são conferidas e restauradas, com nova evidência preservada nesta pasta.
