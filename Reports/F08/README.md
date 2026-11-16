# F08 — Baú experimental de recompensa

Base: `2cb7610f6ba5db3a0dca711cf61c6fa1286b120f`, `main` atualizada.
Branch: `codex/f08-room-reward-chest`. Unity **6000.6.9f1**, pacotes preservados.
Regra registrada antes de código em [PROPOSAL](PROPOSAL.md).
Alterações locais anteriores estão separadas no commit `f5cf7a1`,
[PR de preservação](https://github.com/RodrigoAguiarS/Eldoria/pull/5).

## Implementação

- Um baú de primitivas na primeira sala fixa da cena real de release. Não bloqueia
  navegação/combate. F (remapeável) solicita abertura a até 2,5 m; E/direito continuam
  giro. Waiting/Active, morte, pausa e desativação recusam o comando.
- `RoomRewardChest` valida estado atual por referências explícitas. Confirma
  `IsOpened` antes de sortear/instanciar/notificar, impedindo repetição e reentrada.
  Reativação reconcilia a limpeza consultando RoomController; não reseta abertura.
  Reset isolado de HP não renova recompensa. Recarregar a cena cria outra run.
- `RoomRewardDefinition`/`Loot_Chest_F08` são SOs novos: alcance/espaçamento e
  tabela exclusiva com 10–20 ouro e uma espada de ferro. Sorteio usa LootRoller;
  não altera tabelas, bônus, equipamento ou capacidade antigos.
- Recompensas são pickups normais, raízes na mesma cena da run, independentes da
  ativação do baú. A opção `persistUntilCollected` desliga expiração só nessa fonte.
  Recusa por capacidade preserva instância, definição e quantidade, com retry normal.
- `PlayerInputReader.InteractPressed` solicita o comando de domínio;
  `RoomRewardPresenter` só mostra proximidade/bloqueio, binding e tampa.
  Pausa/captura/UI usam a política de input existente.
- `Interact/0` é o 14º slot, após os 13 anteriores, na cópia runtime do input.
  O `.inputactions` não muda. Preferência v1 antiga que já usa F mantém esse
  binding e recebe uma tecla livre para interação; novas colisões são recusadas.
- `RoomRewardSceneBuilder.Integrate` usa APIs do Editor e preserva ajustes/GUIDs
  dos dados existentes. Recompõe os quatro painéis de Controles para incluir o
  novo slot; acrescenta baú apenas à cena de release. Segunda execução deve ser
  idêntica byte a byte. Não implementa loja, chave, procedural ou arte A11.

## Evidências

| Evidência | Resultado e alcance |
| --- | --- |
| [Run01/EditMode.xml](Run01/EditMode.xml) | **161/161** na suíte completa, zero ignorados. |
| [Run03/Composition.xml](Run03/Composition.xml) | **1/1**, após acrescentar inspeção de Missing Script em toda a hierarquia da cena de release. Referências do baú preenchidas, uma única instância, duas integrações idênticas byte a byte e GUIDs/tabelas antigas preservados. |
| [Run02/PlayMode.xml](Run02/PlayMode.xml) | **131 aprovados, zero falhas, 8 explícitos ignorados**. Suíte completa inclui os sete casos funcionais F08 e regressões de controles F04, inventário, combate, salas/andar, Boss, feedback e pools. [Cobertura do baú](Run02/chest-coverage.json). |
| [Visual.xml](Visual.xml), [contexto](Visual/context.txt) | **1/1** em Editor gráfico, cena real de release em **2560×1440**. Três imagens abertas e inspecionadas: [bloqueado](Visual/chest-locked.png), [disponível](Visual/chest-ready.png), [aberto](Visual/chest-opened.png). Texto legível, F correto e tampa aberta; a captura confirma coleta normal após F, sem comprovar run completa. |
| [windows-build.json](windows-build.json), [artefato](windows-artifact.json) | **Succeeded**, zero erros, 496 avisos de shaders conhecidos. Windows x64 PE `0x8664`, BuildOptions.None, tipos F08 presentes no assembly de runtime. Executável local `Builds/F08/Eldoria.exe`. |
| [Smoke/summary.json](Smoke/summary.json) | Executável Windows iniciou a cena de release por flag diagnóstica, permaneceu ativo durante a coleta e fechou por CloseMainWindow; log sem exceções/Missing Script. Sem input de gameplay ou aceite de run completa; sem alegação de desempenho. Samples brutos locais permanecem ignorados pelo Git. |
| [unity-static-final.json](unity-static-final.json) | Zero erros/avisos de metas, GUIDs e scripts. Cenas binárias são também inspecionadas no Editor pelo teste de composição. |

As tentativas permanecem separadas. `Run01/EditMode.xml` passou 161/161.
`Run01/PlayMode.xml` registrou 128 aprovações, 3 falhas de fixtures e 8 ignorados.
`Run01/Chest-verified.xml` documenta a fixture incorreta com UnitySetUp executado
antes do InputTestFixture.Setup; cena recebia o contexto anterior de input.
O carregamento agora ocorre no corpo do teste, após Setup dos dispositivos.
`Run01/Chest-final.xml` passou os sete casos funcionais; captura ignorada sem GPU.
As execuções incompletas têm notas próprias em Run01. Cleanup é restrito à cena
de release para nunca destruir o runner na cena inicial de testes.
`Run01/PlayMode-verified.xml` teve 130 aprovações e uma recusa de sobrescrita do
benchmark F05, por reutilização do diretório da primeira medição. A execução
seguinte usa Run02, preservando todas as medições anteriores.

Os builders antigos também atualizaram um default serializado de opções e a data de
um relatório histórico durante EditMode. Esses efeitos foram inspecionados e
restaurados; a saída nova foi preservada em Run02. Normalizações de EOL em prefabs
foram restauradas somente após igualdade de conteúdo; os dois InitTestScene das
execuções interrompidas foram removidos com hashes registrados. O efeito conhecido
do pacote de inferência no define SENTIS_ANALYTICS_ENABLED foi registrado e o
snapshot de ProjectSettings restaurado. [Registro da limpeza](Run03/cleanup.json).

[Auditoria final](audit-delivery.json): zero cópias instaladas divergentes/ausentes e
zero links locais quebrados. [Delta de assets](asset-delta.json): somente as quatro
cenas planejadas entre assets serializados existentes; prefabs, tabelas antigas,
metas/GUIDs, template input, pacotes, ProjectSettings e relatórios históricos
preservados. Código/Markdown passam `git diff --check`; seis campos vazios gerados
pelo serializador Unity em MainMenu mantêm o espaço final padrão, sem edição textual
manual da cena.

## Reprodução

Editor: **Tools / Dungeon Crawler / Rewards / Integrate Room Chest (F08)**.
Abra `Floor_01_GeneratedCharacters`, entre em PlayMode, aproxime-se do baú na
primeira sala, tente F antes/depois da última morte, pressione/segure repetidamente,
encha a bolsa e tente coletar a espada; libere um slot e tente novamente.
Confirme que E continua giro e reiniciar cria outra sala/baú bloqueado.

Batch, com o projeto fechado, usar o Editor indicado em docs/BUILD-WINDOWS.md:

```powershell
$env:DUNGEON_CRAWLER_TEST_REPORT_DIR='Reports/F08/<nova-execucao>'
# -projectPath 'G:/DungeonCrawler/My project' em cada comando Unity
# Integração: -batchmode -nographics -quit -executeMethod DungeonCrawler.Editor.RoomRewardSceneBuilder.Integrate
# EditMode: -batchmode -nographics -runTests -testPlatform EditMode -testResults <novo.xml> -logFile <novo.log>
# PlayMode: -batchmode -nographics -runTests -testPlatform PlayMode -testResults <novo.xml> -logFile <novo.log>
# Captura gráfica (sem batchmode/nographics): -runTests -testPlatform PlayMode -testFilter DungeonCrawler.Tests.PlayerInput.RoomRewardChestTests.Capture_ReleaseChestLockedReadyAndOpened
# Build: -batchmode -nographics -quit -buildTarget win64 -executeMethod DungeonCrawler.Editor.RoomRewardSceneBuilder.BuildWindowsValidation
```

Cada execução deve usar diretório novo também para benchmarks. Build isolada em
`Builds/F08/Eldoria.exe`; o builder recusa substituir `windows-build.json` existente.
Logs locais `.log` são ignorados pelo Git; XML/JSON/imagens ficam versionados.
Checker estático: `.claude/skills/unity-csharp-scripting/scripts/check_unity_project.py`.
Inventário/cópias/links: `Tools/Audit/audit_project.py --output <relatorio-novo.json>`.

## Limites

Uma sala experimental; sem extensão automática aos demais encontros. Não altera
o avanço das portas: o jogador pode continuar sem abrir o baú. Recompensas somem
ao encerrar/reiniciar a run, como o restante da progressão; não há save de campanha.
Primitivas sem colisão são uma escolha deste experimento, sem arte final ou animação
de abertura temporizada. Captura diagnóstica com morte programática da onda e
jogador invulnerável não certifica partida normal completa nem direção artística.
Merge e aceite humano final dependem da revisão do usuário.
