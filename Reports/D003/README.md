# D-003 — Nome do jogo: Eldoria · fluxo branch + merge request

Data: 15/11/2026. Base: `main` em `a86d2cd` (avançada por fast-forward com todo o trabalho F01/F02 e anterior). Branch: `claude/eldoria-rename`. Unity 6000.6.9f1.

## Entrega

- Nome visível ao jogador: `PlayerSettings.productName` = **Eldoria** (antes `My project`), executável `Builds/WindowsMVP/Eldoria.exe` (e `Eldoria_Data`) e título **ELDORIA** no MainMenu.
- `GameIdentity` (Editor) concentra os nomes. `MvpReleaseBuild` reaplica o nome do produto a cada build. **Tools / Dungeon Crawler / Menus / Apply Game Name (Eldoria)** atualiza só o título salvo do MainMenu, de forma idempotente. `GameShellSceneBuilder` usa o mesmo texto.
- `Jogar-Build.bat` e `Testar-Itens-Build.bat` abrem `Eldoria.exe`. Se só existir a build antiga, abrem `DungeonCrawler.exe` e avisam para gerar uma build nova. `Tools/Release/package_windows.py` passa a derivar executável e nome do pacote do relatório da build.
- Mantidos de propósito: namespaces, assemblies, menus `Tools/Dungeon Crawler`, chave de PlayerPrefs, variáveis `DUNGEON_CRAWLER_*` e pastas.
- Fluxo dos agentes: `AGENTS.md` (regra + Fluxo por tarefa), `CLAUDE.md` e `NEXT_STEPS.md` agora exigem branch nova a partir da `main` atualizada e merge request para a `main` ao concluir. O merge só acontece com aprovação do usuário. Sem remoto/CLI, a branch fica commitada e o usuário é informado.

## Validação executada

| Evidência | Resultado |
| --- | --- |
| `hashes-apply.txt` | `GameIdentity.Apply` executado duas vezes: mesmo hash do MainMenu |
| [EditMode-retry.xml](EditMode-retry.xml) | 4/4: produto, caminho do executável e título salvo = Eldoria; Apply idempotente; reintegração do shell continua com um TitlePanel/OptionsPanel. Uma 1ª tentativa terminou durante a inicialização do Unity sem gerar XML (`EditMode.log`); o reteste passou |
| [PlayMode.xml](PlayMode.xml) | 5/5: menus/entrada e opções gráficas |
| `final-inventory.json` | 0 cópias instaladas divergentes, 0 links Markdown quebrados |

Efeitos colaterais revertidos e fora do commit: a reintegração do shell no teste regravou cenas de gameplay e `Player.prefab` com a deriva antiga de `PlayerAim`, e o Editor removeu o define `SENTIS_ANALYTICS_ENABLED`. Os arquivos foram restaurados ao HEAD; MainMenu e ProjectSettings voltaram ao estado deixado pelo Apply.

## Limites

- Não foi gerada build nova: a janela e o `.exe` com o nome Eldoria só aparecem após **▶ 3. Gerar build Windows e abrir**. Esse passo regrava `Reports/Leadership/windows-build.json`.
- PlayerPrefs no Windows usa o nome do produto, então as opções salvas pela build anterior voltam ao padrão uma vez.
- `companyName` (`DefaultCompany`) e `applicationIdentifier` não foram alterados.
- Captura visual do título não foi feita; o texto foi verificado por teste na cena salva.
