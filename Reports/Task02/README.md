# Task 02 — combo do jogador

> Registro histórico da execução indicada. Afirmações de ausência de Git/arte/sistemas e pendências descrevem aquela data; consultar [PROJECT_AUDIT](../../PROJECT_AUDIT.md) para estado atual. Medições e limites abaixo foram preservados; não são testes reexecutados por esta revisão documental.

Implementação em 13/11/2026, Unity 6000.6.9f1, Windows, Ryzen 5 5500 (6 cores/12 threads). O projeto não possui repositório Git. Nenhuma branch ou commit foi criado. Backup dos arquivos envolvidos: `G:/DungeonCrawler/Task02_Backup_20261113_210310`.

## Comportamento

- Até três golpes: diagonal `Attack_01`, contracorte horizontal `Attack_02`, finalizador vertical `Attack_03`.
- Clique inicia um golpe; cliques dentro da janela ou até 0,15 s antes dela permitem encadear. Um clique muito antecipado expira. Uma única solicitação pode ficar pendente; spam não cria uma fila de golpes.
- Segurar repete 01 → 02 → 03; depois da recuperação final, reinicia em 01. Soltar antes de comprometer o próximo golpe completa somente o atual.
- `PlayerComboModel` controla índice, impacto único e recuperação. `PlayerCombat` aplica dano pelo timer e publica eventos C#. Não existe caminho de dano por AnimationEvent.
- Morte, desativação e dash descartam golpe e comando pendente. O combate processa dano em LateUpdate, depois do Update do dash, evitando dependência da ordem dos dois componentes.
- Dano = `(base + bônus da arma) × progressão × multiplicador do golpe`. Sistemas de progressão e loot não foram alterados.
- A ponte recebe início/impacto/fim/cancelamento. A pose de contato é alinhada ao frame autoritativo de dano; a velocidade antes/depois do impacto acompanha a configuração. Receber dano não interrompe visualmente um golpe que continua ativo na lógica.
- Consulta física começa com 16 colliders. Quando retorna capacidade cheia, cresce e consulta novamente antes de aplicar dano; memória é retida. HashSet deduplica o IDamageable. Há alocação pontual ao atingir uma nova densidade, sem alocação recorrente após aquecimento.
- O indicador AttackVisual existente é reutilizado no impacto. Nenhum novo efeito, pool, imagem ou asset de arte foi necessário.

Valores iniciais ajustáveis em `Assets/_Project/ScriptableObjects/Combat/PlayerCombo.asset`, sem caráter canônico:

| Golpe | Duração | Impacto | Janela de encadeamento | Recuperação se terminar | Dano | Alcance | Cone |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 01 | 0,45 s | 0,16 s | 0,24–0,42 s | 0,06 s | ×1,0 | 2,2 | 120° |
| 02 | 0,50 s | 0,19 s | 0,28–0,47 s | 0,08 s | ×1,1 | 2,3 | 145° |
| 03 | 0,72 s | 0,32 s | final | 0,28 s | ×1,5 | 2,5 | 90° |

## Arquivos

- Runtime: `Assets/_Project/Scripts/Player/PlayerCombat.cs`, `PlayerInputReader.cs`, novos `PlayerComboModel.cs` e `PlayerComboDefinition.cs`.
- Dados: `Assets/_Project/ScriptableObjects/Combat/PlayerCombo.asset` e respectivos metadados novos.
- Gerador: `Tools/Lemo3DGenerator/src/characters/hero.mjs` e `src/anim.mjs`; somente os três clips do Hero foram adicionados. Saídas Hero.fbx, Hero.glb e Hero_manifest.json atualizadas.
- Ponte: `Tools/Lemo3DGenerator/unity/GameBridge/GeneratedPlayerAnimationBridge.cs` e cópia instalada em `Assets/_Project/Art/Models/Generated/_Scripts/GameBridge/`.
- Builders: GeneratedCharacterBuilder.cs, GeneratedGameTestBuilder.cs e suas cópias instaladas; `Assets/_Project/Scripts/Editor/DungeonMvpSceneBuilder.cs` e novo `PlayerComboSceneBuilder.cs`.
- Integração por APIs do Editor: Hero.controller, importação Hero.fbx.meta, Player.prefab e Floor_01_GeneratedCharacters.unity. GUIDs existentes preservados; nenhuma edição manual de cena/prefab binário/YAML.
- Testes novos: PlayerComboModelTests.cs (EditMode), PlayerComboCombatTests.cs e PlayerCombatPerformanceTests.cs (PlayMode), PlayerInput/PlayerInputComboTests.cs e asmdef próprio para Input System/TestFramework. Metadados criados pelo Unity.
- Evidências e este relatório em `Reports/Task02/`.

`hero_preservation.json` compara os buffers dos GLBs inicial/final: malhas, materiais e todos os nove clips antigos são idênticos. Os GUIDs existentes do Hero também coincidem. Os inimigos não foram regenerados. Não foram editados arquivos de imagens/Task 01, Packages, loot, Boss, áudio ou acessibilidade; os builders não alteram ProjectSettings.

## Validação executada

- EditMode: **45/45 passaram**, incluindo 13 casos do modelo de combo. Evidência: `EditMode.xml`.
- PlayMode: **17/17 passaram**, incluindo integração da Floor com os três estados, cone/alcance/layer/trigger, dano único, progressão/arma, reset e cancelamentos. Evidência: `PlayMode.xml`.
- Input real com fixture isolada oficial do New Input System: **4/4 passaram** (um/dois/três cliques e segurar com recuperação). Evidência: `InputPlayMode.xml`. Press/release no mesmo frame também atravessam os callbacks reais.
- Saturação: 16, 18, 64 e 300 colliders (8, 9, 32 e 150 alvos), cada alvo recebe exatamente um dano. Cena sem Animator também é coberta.
- Integração executada duas vezes sem reconstruir o andar/inimigos: `integration.txt`, `IntegrationUnity.log`.
- Benchmark final: **1/1 passou**, com as duas rotinas sem GC recorrente; `Performance.xml`.
- Build Windows de desenvolvimento gerado e iniciado. A execução mostrou a cena, mas foi encerrada ao aparecer uma solicitação de firewall; nenhuma permissão foi alterada. Log preservado em `WindowsDevelopmentPlayer.log`.
- Build Windows final sem Development: **Succeeded**, 136.969.911 bytes, 44,43 s; `build.txt` e `FinalEditor.log`. Executável em `Builds/Task02/DungeonCrawler.exe`.
- **Conferência visual executada no Editor:** após o usuário cancelar a solicitação de firewall, `Floor_01_GeneratedCharacters` foi aberta em PlayMode. Foram reproduzidos os três golpes e inspecionadas suas poses de contato na câmera do jogo. O alvo temporário recebeu uma aplicação de 20, 22 e 30 de dano, terminando em 928/1000 HP. Depois do finalizador, o indicador de impacto foi desligado e o Hero retornou à locomoção sem novo golpe. Evidências: `visual_impacts.txt` e mensagens `[Task02 Preview]` em `FinalEditor.log`. PlayMode foi encerrado; alvo temporário e alterações de teste não foram salvos na cena.
- **Smoke test do executável final executado:** a cena carregou com Hero e cenário atuais; um clique de mouse iniciou o ataque animado e houve retorno à pose de locomoção. Aplicação encerrada normalmente, sem exceções no log `WindowsPlayer.log`. Os três golpes e os casos de buffer/segurar/cancelamento foram testados no Editor; este smoke test Windows não certifica todas essas sequências novamente no executável nem uma partida completa.
- Encontrado e removido um conflito do atalho de preview com `Assets/Copy Path`. O comando permanece disponível pelo menu, sem ocupar um atalho existente. A alteração afeta somente o helper de Editor e recompilou com zero erros; não altera o runtime já incluído no executável final.

## CPU e GC após aquecimento

Carga igual: 32 alvos estáticos, dois colliders por alvo (64), mesma posição, layer, cone de 120°, raio 2,2, 200 consultas de aquecimento e 10.000 consultas medidas. Editor em batchmode/nographics, sem Deep Profile. Medição por Stopwatch e GC.GetAllocatedBytesForCurrentThread; captura binária do Profiler em `CombatProfiler.raw`. Não equivale ao frame time/FPS de uma partida.

| Medição | 10.000 consultas | Por consulta | GC gerenciado |
| --- | --- | --- | --- |
| Código original, antes de editar | 124,0594 ms | 12,41 µs | 0 B |
| Algoritmo original reproduzido na sessão final do Profiler | 110,6391 ms | 11,06 µs | 0 B |
| Combo, mesma sessão final do Profiler | 402,7585 ms | 40,28 µs | 0 B |

Arquivos: `baseline.json`, `legacy_profiled.json`, `after_profiled.json`. `after.json` registra a primeira medição intermediária (371,0268 ms); a comparação final usa a mesma sessão instrumentada para ambos os algoritmos. O trecho antigo foi retido apenas no teste como referência, nunca no runtime.

**Não houve ganho de CPU nessa carga saturada.** A nova rotina lê todos os 64 colliders, deduplica e alcança 32 alvos; a anterior podia truncar em 16 colliders e aplicar dano duplicado. A correção custa trabalho adicional; animação mais rápida não foi usada como evidência de desempenho. Marcadores runtime: `DungeonCrawler.PlayerCombat.Tick` e `DungeonCrawler.PlayerCombat.ApplyHits`.

## Limitações e reprodução

- O executável emitiu dois avisos de Missing Script já descritos no MVP_AUDIT, além de avisos de shaders de pós-processamento URP removidos/não suportados. Esses sistemas estão fora do escopo autorizado desta task. O MVP global não foi certificado como concluído.
- Não foi feito aceite completo das cinco salas, Boss ou progressão de uma partida; a validação desta task se limita ao combo e integrações descritas.
- Menu idempotente: `Tools/Dungeon Crawler/Combo/Integrate Hero Combo`. Não reconstrói inimigos nem cenário e preserva ajustes existentes do asset de combo.
- Menu `Tools/Dungeon Crawler/Combo/Preview Next Impact`: abre a Floor em PlayMode, cria somente um alvo temporário e pausa no contato de cada golpe; repetir o comando pelo menu encadeia o próximo. O alvo e as alterações de teste desaparecem ao sair do PlayMode. `visual_impacts.txt` registra o dano observado.
- Menu `Build Windows`: gera `Builds/Task02/DungeonCrawler.exe` sem modificar a lista compartilhada de cenas do projeto.
- Próximo passo: playtest de sensibilidade das janelas/recuperação e correção separada dos avisos de assets/URP existentes.
