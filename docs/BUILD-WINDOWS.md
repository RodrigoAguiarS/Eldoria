# Build e reconstrução — Windows 1.0

F15 experimental: builder **Tools / Dungeon Crawler / Boss / Build Stone Sentinel
Prototype (F15)** abre `F15_StoneSentinel_Test.unity`, fora das Build Settings de
release. `DungeonCrawler.Editor.StoneSentinelSceneBuilder.BuildWindowsValidation`
gera uma build só da arena em `Builds/F15/Eldoria.exe`, com relatório em
`Reports/F15/windows-build.json`. Reexecuções geram subpastas `Windows-<timestamp>`
e preservam os artefatos anteriores. Nenhum acesso ao checkpoint da run normal.
[Parâmetros/roteiro](STONE-SENTINEL-F15.md) e [evidências](../Reports/F15/README.md).

Unity **6000.6.9f1**, URP 17.6.0 e pacotes fixados em `Packages/manifest.json`/`packages-lock.json`. Abra o projeto por essa versão; não atualize pacotes para reproduzir esta entrega. Feche PlayMode e qualquer outra instância do Editor antes de executar batchmode no mesmo projeto.

## Abrir o projeto existente

Use Unity Hub → Add project → esta pasta, com o Editor fixado e módulo Windows Build Support. Este repositório já contém jogo, Git, pacotes, metas, cenas e dados; não crie outro projeto nem execute git init para continuar. Input é exclusivamente New Input System; Force Text e metas visíveis já foram configurados. Conferir FoundationValidator e manifest/lock antes de corrigir configuração.

Para jogar a revisão atual e testar itens sem depender de drops aleatórios, siga [COMO-TESTAR](../COMO-TESTAR.md). Os atalhos de aff1895 abrem MainMenu ou a cena real de release; o modo com itens é diagnóstico e deve ser identificado como tal nas evidências. SpriteLab/billboard são experimentos separados. Testar aparência não exige reconstruir o andar.

## Build

No PowerShell, a partir de `G:\DungeonCrawler\My project`:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'G:/DungeonCrawler/My project' -buildTarget win64 -executeMethod DungeonCrawler.Editor.MvpReleaseBuild.BuildWindows -quit -logFile 'G:/DungeonCrawler/My project/Reports/Leadership/Windows-rebuild.log'
```

A chamada pode retornar antes de o processo Unity terminar. Aguarde o processo e confira `Reports/Leadership/windows-build.json`: versão 1.0.0, `Succeeded`, zero erros e `development=false`. Saída: `Builds/WindowsMVP/Eldoria.exe` (antes de D-003: `DungeonCrawler.exe`), com MainMenu e Floor_01_GeneratedCharacters. O menu equivalente é **Tools / Dungeon Crawler / Build / Windows Release (Current MVP)**.

`BuildWindowsProfile` gera somente a build Development de diagnóstico em `Builds/WindowsProfile`; não a distribua. Flags, carga e limites das medições: `Tools/AudioGenerator/DIAGNOSTICS.md` e `Reports/Leadership/WindowsPerf/README.md`.

## Testes

Execute separadamente, aguardando uma suíte terminar antes de iniciar a outra. Não use `-quit` com `-runTests` nesta instalação. Para execuções novas, escolha diretório e nomes novos de XML/log, preservando EditMode-final/PlayMode-final históricos. Alguns testes EditMode de idempotência salvam assets; registre hashes/diff antes/depois e não restaure mudanças de terceiros automaticamente.

```powershell
$env:DUNGEON_CRAWLER_TEST_REPORT_DIR='Reports/Leadership/FinalPerf'
& 'C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'G:/DungeonCrawler/My project' -runTests -testPlatform EditMode -testResults 'G:/DungeonCrawler/My project/Reports/Leadership/EditMode-final.xml' -logFile 'G:/DungeonCrawler/My project/Reports/Leadership/EditMode-final.log'
```

Repita com `-testPlatform PlayMode`, resultado `PlayMode-final.xml` e log `PlayMode-final.log`. A variável separa os relatórios novos dos históricos da Task 02. Os resultados da entrega ficam em `Reports/Leadership`; a matriz de cobertura e aceite manual está em `RELEASE-1.0-VALIDATION.md`.

O exemplo acima reconstrói os caminhos da entrega histórica: para auditoria nova, troque **todos** os destinos por uma pasta exclusiva (por exemplo Reports/ProjectAudit/<execucao>) antes de executar. Confira o NUnit XML (passed/failed/skipped), não só o código do processo. Um teste Explicit de captura pode ser ignorado em batchmode; não apresentá-lo como revisão visual concluída.

## Roteiro de regressão

Este roteiro consolida M1–M4 e a QA do candidato. Registrar commit, build/versão, cena, resolução, origem da evidência e resultado; não marcar etapas por presença de código. Valores atuais vêm de SOs e [BALANCE-1.0](BALANCE-1.0.md), não dos checklists antigos.

1. **Fundação e arena:** Console sem erros, referências resolvidas; Bootstrap inicia com ações Gameplay; na MVP_Arena conferir chão/obstáculos/luz, câmera isométrica fixa seguindo suavemente, WASD relativo à tela no XZ e mira horizontal pelo mouse. Inimigo contorna pilares com NavMesh; player não usa Rigidbody.
2. **Combate e sobrevivência:** clique/segurar executa combo01/02/03 com impactos únicos, buffer limitado e retorno à locomoção; dash em movimento/parado respeita cooldown/invulnerabilidade. HP/HUD/flash/shake respondem; morte cancela controles/dano pendente, R e botão reiniciam. Registrar câmera/pose/impacto observados, não só valores.
3. **Salas:** percorrer quatro salas normais; entrada fecha, onda inicia, saída fica bloqueada enquanto um inimigo vive e abre somente após a última morte. Conferir ranged/elite, recuo/tiros, colisão em parede/porta e pool reutilizado; sem atravessar portas ou softlock. O quinto encontro é o Boss: não esperar vitória após sala4.
4. **Boss:** HUD aparece só durante a luta, Slam/Volley avisam antes do dano; transição única a50% com3s invulnerável, visual persistente no Warden e novo padrão. Telegraph nunca abaixo1s; sair da área/dash evita dano segundo as regras. Derrotar até5/5 e observar vitória; depois testar derrota→reinício. Preview de fase/diagnóstico não é prova de vitória normal.
5. **Progressão/bolsa:** XP/ouro imediatos; nível/dano/cura coerentes; armas/poções/armadura entram na bolsa. Testar16slots, stack5 e potências distintas, arma inicial equipada e troca manual (D-007), desequipar com espaço livre e recusa com bolsa cheia, defesa, Q/Usar ferido/cheio/morto, Largar1 em espaço livre/bloqueado. Coleta recusada preserva pickup e permite nova tentativa após liberar espaço. I/Tab pausa; Esc volta; UI não dispara ataque.
6. **Visual pós-release:** modelos próprios de itens no chão, ícones/quantidade/estado de poções, uma espada equipada e colete acompanhando Run/Dash/Hit/Death/combos/especial. Trails/faíscas só após acerto aceito; giro com anel, sem cubo branco na apresentação normal; chama animada. Use diagnóstico de itens para cobertura visual e partida normal separada para loot/progressão.
7. **Menus/opções:** MainMenu→Jogar inicia run nova; pausa/bolsa/opções/foco/retorno corretos; terminais fecham overlays e focam Reiniciar. Aplicar persiste, Voltar descarta rascunho. Testar1280×720 janela e1920×1080 tela cheia, dropdowns/textos longos, presets Leve/Padrão sem rosa; fechar/reabrir preserva opções e restaurar preferências anteriores. Ultrawide emulado não comprova monitor físico.
8. **Áudio/legibilidade/build:** música/SFX em0 silenciam categorias; flash/tremor em0 removem efeitos opcionais sem ocultar perigo/fase2. Ouvir ataque/acerto/dano/coleta/morte/rugido/vitória/música, anotar sons ausentes/desconforto/distorção. No Windows normal, concluir andar e derrota/reinício; triar log para exceções/Missing Script/avisos Animator. Métricas de perfil, diagnóstico sintético e escuta humana são evidências distintas; repetir com mesma configuração antes de alegar melhoria.

Aceite humano complementa testes automatizados. Se não abriu a cena e observou PlayMode, a revisão visual permanece `NÃO VALIDADO NO EDITOR`.

### Regressão F04 — Controles

Em Opções → Controles, testar captura de tecla e botão do mouse, conflito (por exemplo dash em Q), Escape/Cancelar/Voltar, timeout de 10 s com partida pausada e perda de foco. Binding rejeitado/cancelado preserva o anterior. Remapear bolsa, poção, pausa e reinício; Escape continua voltando, poção funciona na bolsa e os cliques nos menus não atacam nem consomem. Conferir rótulos do título/HUD/bolsa/opções/derrota, morte→reinício com a tecla nova, run nova sem inventário e overrides preservados após relançar. Restaurar padrão deve persistir após outro relançamento. Navegação UI/mira continuam fixas; sem suporte gamepad completo.

Builder específico: `DungeonCrawler.Editor.ControlBindingsSceneBuilder.Integrate`, menu **Tools / Dungeon Crawler / Input / Integrate Control Remapping (F04)**. Build isolada de validação: `ControlBindingsSceneBuilder.BuildWindowsValidation` gera `Builds/F04/Eldoria.exe` e `Reports/F04/windows-build.json`, recusando sobrescrever esse relatório; não modifica o pacote histórico. Para execuções posteriores, escolher novos destinos. Testes, comandos e limites da entrega em [Reports/F04](../Reports/F04/README.md).

### Regressão F05 — Cura, coleta e nível

Na cena `Floor_01_GeneratedCharacters`, ferir e usar Q/Usar: somente cura efetiva produz `+HP`, motes rubi e o tom discreto existente. Com HP cheio ou morto, não consumir nem confirmar cura. Coletar XP/ouro/poção/arma/armadura; conferir mensagem/glint apenas após aceite, e pickup mantido no chão com bolsa cheia. Uma coleta que sobe vários níveis deve curar e atualizar dano uma vez pela regra existente e apresentar o nível final. Abrir bolsa, consumir durante pausa e retomar; efeitos/tempo dos avisos retomam sem acúmulo ilimitado. Morte/reinício/desativação não deixam partículas ou mensagens antigas. Flash zero mantém mensagens e remove partículas; SFX zero respeita o mixer. Revalidar trail de combo, anel do giro, faíscas de acerto e flash/shake de dano.

Builder: **Tools / Dungeon Crawler / Feedback / Integrate Rewards (F05)**, ou `DungeonCrawler.Editor.RewardFeedbackSceneBuilder.Integrate`. Build isolada: `RewardFeedbackSceneBuilder.BuildWindowsValidation`, com saída `Builds/F05/Eldoria.exe` e relatório novo `Reports/F05/windows-build.json`; recusa substituir evidência existente. Testes visuais explícitos exigem Editor gráfico; XML automatizado e captura diagnóstica não substituem uma run normal até vitória/derrota. Evidências e comandos desta revisão em [Reports/F05](../Reports/F05/README.md).

### Regressão F07 — Linha de visão do ranged

Usar **Tools / Dungeon Crawler / Enemies / Integrate Ranged Line Of Sight (F07)** (`RangedLineOfSightBuilder.Integrate`) para persistir máscara Obstacle/Door e intervalo de 0,1 s no asset compartilhado, sem reconstruir salas ou prefabs. Cobertura deve impedir novo preparo; entrar em cobertura durante windup deve cancelar o tiro, e sair exige preparo novo com cooldown preservado. Conferir porta fechada/abrindo, alvo móvel/morto, desativação/reativação e pausa. O agente persegue pelo NavMesh em thinkInterval, inclusive quando já está no alcance de ataque atrás da parede.

Build isolada: `RangedLineOfSightBuilder.BuildWindowsValidation`, saída `Builds/F07/Eldoria.exe` e relatório novo `Reports/F07/windows-build.json`. Captura explícita gráfica: `RangedLineOfSightReleaseTests.Capture_ReleaseRangedBehindDoorAndFiring`, na cena real de release, com ator/porta diagnósticos. Evidências, reprodução e limites em [Reports/F07](../Reports/F07/README.md); essa captura não substitui uma run normal Windows.

### Regressão F08 — Baú de sala fixa

Builder: **Tools / Dungeon Crawler / Rewards / Integrate Room Chest (F08)**,
`DungeonCrawler.Editor.RoomRewardSceneBuilder.Integrate`. Integra um baú independente em
cada uma das cinco salas de release e usa Interagir com baú nos Controles já
integrados. Repara as chamas órfãs sem reposicionar tochas ou luzes. Preserva tabelas de inimigos; os dados exclusivos ficam em
`RoomReward_F08.asset`/`Loot_Chest_F08.asset`. Reexecutar preserva ajustes nesses SOs.

Tentar F perto antes da limpeza: bloqueado. Depois da última morte: aviso de
abertura, F solta ouro/espada uma única vez e abre a tampa. E continua giro.
Conferir distância de 2,5 m, pausa/bolsa/captura de input, repetição/segurar F,
desativação/reativação e morte. Com bolsa cheia, tocar na espada mantém o mesmo
pickup; liberar um espaço permite coleta sem reroll. Drops do baú não expiram;
reinício cria run nova com sala/baú bloqueados. No menu Controles, F deve conflitar
com outra atribuição; remapear Interagir atualiza o aviso. Preferência v1 antiga
que já usa F preserva esse controle e atribui tecla livre à interação.

Após a vitória, **Coletar recompensas** fecha o painel, permitindo acessar a
bolsa e o baú do Boss. Testar todas as salas e confirmar cinco aberturas independentes.

Build isolada da correção: `RoomRewardSceneBuilder.BuildAllRoomsValidation`, saída
`Builds/F08-AllRooms/Eldoria.exe`, relatório novo `Reports/F08-AllRooms/windows-build.json`
(recusa sobrescrita). O método antigo BuildWindowsValidation mantém o destino histórico F08. Testes: `RoomRewardAssetTests`, `InputBindingTests`,
`RoomRewardChestTests` e regressões de controles/salas/inventário. Captura explícita
gráfica da correção: `RoomRewardChestTests.Capture_AllRoomsAndTorchPositions`; requer
Editor sem `-batchmode`/`-nographics`. XML e captura diagnóstica são distintos do
aceite de uma run normal completa. Evidências atuais em [Reports/F08-AllRooms](../Reports/F08-AllRooms/README.md); a primeira entrega permanece em [Reports/F08](../Reports/F08/README.md).

### Regressão F09 — Vasos destrutíveis

Builder: **Tools / Dungeon Crawler / Environment / Integrate Destructible Vases
(F09)**, `DungeonCrawler.Editor.DestructiblePropSceneBuilder.Integrate`.
Um vaso de 40 HP por sala de release; básico de 20 causa dois acertos, giro de
40 quebra em um no estado inicial. Equipamento/nível/combo mantêm o cálculo
existente. Conferir destroços estáticos, sem drops e sem impedir saída/vitória
com vasos intactos. Portas, centro e spawns ficam livres; os vasos não bloqueiam
movimento, projéteis ou visão ranged. Toggle não restaura HP e reinício cria novos.

Testes: `DestructiblePropAssetTests`, `DestructiblePropTests` e
`FloorIntegrationTests.SavedFloor_AllWavesCloseDoorsAndOnlyLastDeathCompletesVictoryOnce`.
Captura gráfica explícita: `DestructiblePropTests.Capture_ReleaseVaseBeforeAndAfterActualAttacks`,
sem `-batchmode`/`-nographics`, com `DUNGEON_CRAWLER_TEST_REPORT_DIR` exclusivo.
As imagens usam dano pelos impactos reais; posicionamento/invulnerabilidade são
diagnósticos, distintos do aceite de uma run normal. Evidências em
[Reports/F09](../Reports/F09/README.md).

### Regressão F10 — Escala de texto/HUD

Builder: **Tools / Dungeon Crawler / UI / Integrate Text And HUD Scale (F10)**,
`DungeonCrawler.Editor.TextScaleSceneBuilder.Integrate`. Atualiza as quatro
cenas oficiais; não exige regenerar personagens, salas ou dados de itens.

Em Opções, escolher 100/125/150%: o rascunho não altera o HUD até **Aplicar e
salvar**. Voltar descarta; **Restaurar padrões** também é rascunho até Aplicar.
Fechar/reabrir e reiniciar mantêm a preferência aplicada. JSON v1–v3 ou escala
inválida/ausente voltam a 100%; documento futuro usa os defaults.

Conferir bolsa 16/16, poções com quantidade, comparação, equipamento e nomes
extensos na coluna rolável; reticências nos resumos não alteram o nome integral.
Rolar com roda/barra, focar a barra e selecionar outro slot deve retornar ao topo.
Abrir os dropdowns de qualidade/resolução/poção/escala e selecionar com mouse e
teclado; clique no fundo do dropdown deve fechar a lista sem acionar o botão
encoberto. Opções/bolsa/pausa não devem atacar nem deixar foco em painel oculto.
Conferir HP, Boss, recursos, avisos e terminais na maior escala.

Captura explícita: `TextScaleInputTests.Capture_LayoutsAtAllScalesAndResolutions`,
Editor gráfico sem `-batchmode`/`-nographics`, com
`DUNGEON_CRAWLER_TEST_REPORT_DIR` exclusivo. Verifica 1280×720, 1920×1080 e
2560×1080 **emulado**, nas três escalas; mede encaixe/alvos e gera imagens para
inspeção. Não comprova monitor ultrawide físico, contraste, gamepad completo,
leitor de tela, outras fontes/idiomas ou escalas acima de 150%. Evidências e
limites em [Reports/F10](../Reports/F10/README.md).

## Reconstrução da composição

Os assets necessários já estão versionados. Não é necessário regenerar personagens, cenário ou imagens para construir o jogo.

- **Tools / Dungeon Crawler / Combo / Integrate Hero Combo** configura combo e referências preservando o SO existente.
- **Tools / Dungeon Crawler / Audio / Integrate Functional Audio** integra áudio, shell e diagnóstico pelas APIs do Editor, reaproveitando definições existentes. O gerador original `python Tools/AudioGenerator/generate.py` só é necessário para reproduzir os WAVs; documentado junto à ferramenta.
- O builder do andar integra a cópia gerada e o shell ao terminar. O build final usa lista explícita MainMenu → Floor_01_GeneratedCharacters; cenas de teste continuam disponíveis no Editor. Não regenere inimigos como parte de ajustes de animação do Hero.
- GUIDs e ajustes de dados existentes devem ser preservados. Não substitua cenas/prefabs por edição textual. Builders recusam assets existentes que não carregam, em vez de sobrescrevê-los.

O pacote Unity de inferência pode adicionar `SENTIS_ANALYTICS_ENABLED` ao carregar o Editor. Isso foi identificado como efeito do pacote; não é requisito do gameplay. Confira o diff e preserve alterações desconhecidas. Os avisos conhecidos de shaders ficam documentados no relatório da entrega.

## Pacote local

Após build final e testes aprovados:

```powershell
python Tools/Release/package_windows.py
```

Gera `Builds/Release/<produto>-<versão>-windows-x64.zip` (nome do executável da build; ex.: `Eldoria-…`; o pacote histórico 1.0.0 é `DungeonCrawler-1.0.0-windows-x64.zip`), README, BUILD-INFO e manifesto SHA256. Verifica PE x64, CRC e hashes; exclui pastas de backup/debug e recusa sobrescrever um pacote existente. Não publica nem envia arquivos. Para outra revisão, preserve o pacote anterior e versione explicitamente o novo artefato.


## F14 — validação isolada de dois andares

`DungeonCrawler.Editor.SecondFloorSceneBuilder.Integrate` / **Tools / Dungeon Crawler / Dungeon / Integrate Second Fixed Floor (F14)** acrescenta uma sala de primitivas e referências explícitas sem reconstruir personagens/primeiro andar. `SecondFloorSceneBuilder.BuildWindowsValidation` gera `Builds/F14/Eldoria.exe` com MainMenu, Floor_01_GeneratedCharacters e Floor_02_Fixed; recusa substituir `Reports/F14/windows-build.json`. O builder de release corrente também inclui as três cenas.

Percorrer primeiro andar normalmente, coletar antes da descida sem retorno, descer pelo painel e conferir estado exato; entrar na sala F14, fechar portas, usar coberturas e limpar somente na última morte. Testar menu/Continuar antes e durante combate, morte, Reiniciar/nova run no primeiro andar, pausa e repetição da transição. Retomada usa fronteira segura e não concede XP/loot outra vez. Diagnósticos com teleporte/dano sintético não equivalem a esse percurso normal. Evidências/limites em [Reports/F14](../Reports/F14/README.md).
