# F04 — Remapeamento de teclado e mouse

16/11/2026. Base `3468060ae3be1d5b21240efc8f16ca026834f752`, branch `codex/f04-control-rebinding`, criada da `main` atualizada. Unity 6000.6.9f1 / Input System 1.20.1; pacotes, GUIDs e identificadores técnicos preservados.

Implementação: `f289917`. [Merge request #2 → main](https://github.com/RodrigoAguiarS/Eldoria/pull/2), aberto para revisão, sem merge. Próximo passo: aceite de teclado físico Windows e revisão do usuário.

## Entrega e contratos

**Opções → Controles** no título e na pausa. Treze bindings editáveis: quatro direções, básico, dash, duas alternativas do giro, duas da bolsa, poção, pausa/voltar e reinício. Captura aceita teclas físicas e cinco botões do mouse. Conflitos indicam a ação ocupante e preservam o binding anterior; não há troca automática. Para trocar duas teclas, use uma tecla livre intermediária.

Escape cancela a captura e permanece como saída imutável dos menus, mesmo com pausa remapeada. Enter/numpadEnter ficam reservados à UI. Cancelar e Voltar são clicáveis durante captura; esses cliques não viram bindings. Há timeout de 10 s, cancelamento ao perder foco/fechar painel e bloqueio até soltar os controles antes de devolver os atalhos. Restaurar padrão e alterações aceitas salvam imediatamente.

`InputBindingSet` clona o template e fornece Gameplay + Shortcuts a todos os donos através da referência `RuntimeInputBindings`. O Restart histórico de Gameplay é removido só da cópia, evitando um segundo dono. O leitor de gameplay agora também é desabilitado na morte; atalhos de UI/reinício continuam separados. Reinício por atalho fica suspenso em overlays, e poção só é encaminhada no gameplay/bolsa. Filtragem por EventSystem e espera pela soltura ao retomar impedem ataque pelo clique de UI.

Persistência: `DungeonCrawler.Bindings.v1`, JSON schema 1 no PlayerPrefs por usuário; IDs estáveis como `Move/up` e `Dash/0`. Validação atômica rejeita versão futura/ausente, IDs repetidos/desconhecidos, caminhos inválidos e duplicatas. Documento recusado não é apagado nem sobrescrito. Reset remove overrides e salva os defaults. O `.inputactions` compartilhado, opções gerais, dados de itens e progressão não são alterados. Ponteiro/mira e navegação UI continuam fixos; não há suporte gamepad completo.

Os rótulos do título, HUD, recurso, bolsa, opções, avisos e terminais consultam ações efetivas. O badge de poção ajusta a largura e fica abaixo do cartão, acomodando MMB e teclas longas. A direção/mira existente do herói não foi alterada; a descrição histórica de mira no GDD não foi reinterpretada como parte desta entrega.

## Arquivos e composição

- Fonte runtime: `Scripts/Systems/InputBindingSet.cs`, `RuntimeInputBindings.cs`; leitores PlayerInputReader/PlayerInventoryInput/GameRestarter/MainMenuController.
- UI: ControlRebindingPresenter, BindingHintPresenter, MenuScreenStack/GameMenuController, PlayerResourcePresenter e InventoryStatusPresenter.
- Editor: ControlBindingsSceneBuilder + pontos de integração em GameShellSceneBuilder/InventorySceneBuilder. O botão Ordenar recompõe seu único callback: a suíte ampla reproduziu acúmulo de dois callbacks durante reconstruções.
- Assets: Player.prefab, MainMenu, MVP_Arena, Floor_01 e Floor_01_GeneratedCharacters, salvos por APIs do Editor. Sem reconstrução de geometria/arte nesta integração específica.
- Testes: InputBindingTests e ControlRebindingInputTests; asmdef EditMode referencia Input System. Contratos em GAME_DESIGN/ARCHITECTURE, estado em MVP/NEXT_STEPS e roteiro em docs/BUILD-WINDOWS.

## Evidência executada

| Evidência | Resultado e alcance |
| --- | --- |
| [EditMode-final.xml](EditMode-final.xml) | **153/153** aprovados na suíte completa: domínio, opções, assets/builders e 27 casos de bindings. |
| [EditMode-layout.xml](EditMode-layout.xml) | **27/27** após o ajuste final do badge; builder repetido mantém bytes, um painel e referências explícitas nas quatro cenas. |
| [PlayMode-final.xml](PlayMode-final.xml) | **35/35** aprovados; [seleção exata](playmode-selection.txt). Oito casos F04 + regressões de menus, pausa/bolsa, arena, combo e especial. |
| [Visual-final.xml](Visual-final.xml) | **1/1**, Editor gráfico em PlayMode: captura/conflito/cancelamento e badge MMB em 1280×720 / 1920×1080. Imagens abertas e conferidas. |
| [windows-build.json](windows-build.json) | Build final release x64 em `Builds/F04/Eldoria.exe`: **Succeeded, zero erros, 496 avisos** de shaders, padrão já presente no histórico. Relatório inicial preservado em [windows-build-initial.json](windows-build-initial.json). |
| [windows-smoke.json](windows-smoke.json) | Executável Windows: menu/opções/controles, captura de MMB para dash, persistência após relançar, rótulos do título/HUD e dash efetivo no andar. Encerramento por Alt+F4 limpo. Ver limites abaixo. |
| [unity-static.json](unity-static.json) | Checker de metas/GUIDs/scripts: zero erros e zero avisos. |
| [audit-final.json](audit-final.json), [asset-delta.json](asset-delta.json) | Fontes/cópias instaladas e links locais conferidos; hashes dos assets preexistentes comparados ao baseline. |
| [preferences-restored.json](preferences-restored.json) | Bindings/opções de player e preferências da build de comparação restaurados dos snapshots locais e conferidos. Snapshots privados `.tmp` não são versionados. |

PlayMode F04 cobre: captura cancelada por Escape/Cancelar/Voltar/desativação/perda de foco; Enter inválido e binding duplicado; teclado e mouse; gamepad ignorado; timeout com `timeScale=0`; reload/reset; direções/dash/básico/especial remapeados; bolsa e poção durante pausa; opções sem consumir; morte e reinício na tecla nova, sem restaurar inventário; clique em Continuar sem ataque, com controle positivo de ataque fora da UI.

Capturas finais: [controles 720p](Visual-final/1280-controls.png), [conflito 720p](Visual-final/1280-conflict.png), [captura 1080p](Visual-final/1920-capture.png), [badge MMB 720p](Visual-final/1280-potion-binding.png), [badge MMB 1080p](Visual-final/1920-potion-binding.png). Capturas iniciais também foram preservadas em `Visual`.

As execuções iniciais não são apresentadas como aprovadas: EditMode-all registrou `anyKey` aceito após normalização e callback duplicado em Ordenar; PlayMode-functional registrou uma expectativa de texto com encoding incorreto e o leitor ainda habilitado na morte. Corrigidos e reexecutados com sucesso. EditMode-retest teve erro de tipo apenas no helper de relatório de build, corrigido antes do reteste. Duas seleções PlayMode iniciais foram interrompidas sem XML ao incluir a captura gráfica em batch; não contam como resultado. Logs locais ficam em Reports/F04 e são ignorados pelo Git.

A suíte ampla regravou a serialização implícita de GameOptions, define de pacote e o relatório histórico de arte. Foram conferidos e restaurados ao baseline; a nova validação de arte foi preservada como [production-visual-validation.json](production-visual-validation.json). Cenas temporárias dos runners interrompidos foram removidas. Nenhuma evidência histórica permanece substituída.

## Limites observados no Windows

O teclado sintetizado pela ferramenta nativa não acionou F/Escape na build F04; Escape também não acionou o menu na build anterior `ReleaseSmoke-1.0.0`, usada como controle. Isso limita o smoke automatizado de teclado no player; não substitui aceite com teclado físico. Os testes PlayMode verificaram callbacks reais do Input System com dispositivos isolados.

Fechar pelo botão da barra via automação provocou crash **nativo após shutdown**, tanto na F04 quanto na build anterior, com frames de UnityPlayer/USER32/uxtheme, sem exceção C# anterior. A F04 encerrou limpa por Alt+F4. Não há evidência de regressão específica da F04; a causa nativa não foi determinada e permanece pendência de QA. Logs completos locais: Windows-player.log, Windows-player-relaunch.log e Windows-baseline-close.log; resumo versionado em windows-smoke.json.

Não houve novo percurso completo até vitória/derrota no executável, teste de ultrawide físico, outro hardware ou aceite artístico. A morte/reinício remapeado está coberta em PlayMode. O smoke funcional do executável antecedeu somente o ajuste visual final do badge; a build final foi recompilada e o badge final validado no Editor. Não houve publicação, empacotamento de release ou merge.

## Reprodução e revisão

1. Abrir com Unity 6000.6.9f1. Menu **Tools / Dungeon Crawler / Input / Integrate Control Remapping (F04)**; ou `-batchmode -nographics -executeMethod DungeonCrawler.Editor.ControlBindingsSceneBuilder.Integrate -quit`.
2. EditMode: toda a suíte ou filtro `InputBindingTests`. PlayMode funcional: nomes em playmode-selection.txt. Usar destinos novos de XML/log e `DUNGEON_CRAWLER_TEST_REPORT_DIR`; não usar `-quit` com `-runTests`.
3. Visual: selecionar somente `DungeonCrawler.Tests.PlayerInput.ControlRebindingInputTests.Capture_Controls720pAnd1080p`, com gráficos, fora de batch/nographics. Definir a variável de relatório para uma pasta nova.
4. Build isolada: `ControlBindingsSceneBuilder.BuildWindowsValidation`, com `-buildTarget win64`. Preservar/renomear o relatório anterior antes de repetir; o helper recusa sobrescrevê-lo. Roteiro manual de regressão em docs/BUILD-WINDOWS.
5. Revisar o merge request para main. Merge somente após aprovação do usuário. Próximo aceite recomendado: teclado físico Windows (captura, Escape, pausa/bolsa e morte→reinício).
