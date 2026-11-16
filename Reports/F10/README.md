# F10 — escala de texto e HUD

Base `e0860e6`, main atualizada; branch `codex/f10-text-hud-scale`.
Unity 6000.6.9f1 / UGUI 2.6.0 / Input System 1.20.1, sem troca de pacotes.
Escopo inicial em [PLAN](PLAN.md).

**Texto e HUD** oferece 100/125/150%, padrão/fallback 100%. JSON v4 mantém a
chave existente e migra v1–v3 preservando as demais preferências. Aplicar salva;
Voltar descarta. Restaurar padrões das opções gerais altera somente o rascunho
até Aplicar; bindings/progressão permanecem separados.

UiTextScale aplica fontes por baselines serializadas, sem acumulação ou escala
do Canvas/targets. Mantém LegacyRuntime.ttf e UGUI. CanvasScaler reserva
1920×1080 com Expand; containers acomodam 150%. Grid de quatro colunas na bolsa,
equipamento/detalhes roláveis e scrollbar focável; selecionar outro slot retorna
ao topo. Nomes extensos recebem reticências no resumo e ficam integrais nos
detalhes. Dropdowns têm linhas de 64 unidades; HUD separa poções/Boss e amplia
áreas de HP, recurso e mensagens. Builders existentes reaplicam a composição.

## Evidências

- [Edit03/results.xml](Edit03/results.xml): **41/41**, opções/migração/
  persistência/inventário/comparação. Após a correção final do viewport,
  [Edit04/results.xml](Edit04/results.xml): **13/13** F10. Builder byte a byte
  idempotente nas quatro cenas, GUIDs/referências/baselines e SOs/prefabs de
  domínio preservados; zero Missing Script na inspeção pelas APIs do Editor,
  incluindo cenas binárias.
- [Play09/results.xml](Play09/results.xml): **23/23**, New Input System.
  Aplicação repetida sem acumular fonte ou mudar target; Voltar/reset/reload/
  run nova, foco, resolução, blocker sem acionar Aplicar encoberto, bolsa 16/16,
  nome integral, rolagem pelo track e retorno ao topo, equipamento/ordenação/
  poção/pausa/remapeamento/morte. Cliques nos overlays não geram ataques.
  Seleção exata em [play-filter.txt](play-filter.txt).
- [Visual02/results.xml](Visual02/results.xml): **1/1** no Editor gráfico em
  PlayMode, **66 PNGs**. Matriz 1280×720 / 1920×1080 / 2560×1080 **emulado**,
  nas três escalas: título, opções, dropdown, controles, HUD, bolsa cheia e
  rolagem, mais ícones reais em 150%. Altura preferida dos textos, janelas dentro
  do viewport, targets sem sobreposição e separação texto/scrollbar verificados.
  Os sete contatos da matriz foram abertos e inspecionados, junto de imagens
  individuais em 150%: [bolsa 720p](Visual02/1280x720-150-bag.png),
  [ícones 720p](Visual02/1280x720-150-bag-icons.png),
  [HUD 1080p](Visual02/1920x1080-150-hud.png),
  [dropdowns](Visual02/contact-dropdown.jpg), [bolsa](Visual02/contact-bag.jpg).

[Medições](Visual02/measurements.txt): Voltar ao jogo mantém 488×64 unidades,
equivalentes a 325,33×42,67 px em 720p e 488×64 px nas capturas de altura 1080.
Em 150%, conteúdo extenso tem 1305–1320 unidades para 700 de viewport; rolagem
conserva o texto integral. São medidas de geometria, não de contraste ou tinta
de cada glifo.

[Checker final](unity-static-final.json): zero erros/avisos de GUIDs/metas/scripts.
[Auditoria](audit-final.json): fontes/cópias sincronizadas e zero links locais
quebrados. [Resumo](validation-summary.json): 1618 arquivos existentes de conteúdo,
pacotes, ProjectSettings e relatórios preservados por hashes; somente as quatro
cenas e GameOptions mudam entre os assets de conteúdo existentes. Sem erros C#.
Resíduos do runner foram removidos após encerrar as sessões; a alteração do
define SENTIS pelo pacote foi inspecionada e restaurada à baseline. O primeiro
comparador de hashes normalizava blobs históricos CRLF, gerando falsos positivos;
a conferência byte a byte confirmou que esses relatórios não foram modificados.

Visual01 motivou correções no viewport sob a scrollbar e na região de poções
encostando no Boss. Visual02 acrescenta asserção contra a primeira regressão.
Play06 clicava no centro do handle sem arrastar e esperava rolagem; o teste
agora clica no track. Play08 teve 22/23 por uma consulta aos paths antigos de
Details/Slot/Equipment no teste de ordenação; corrigida em Play09. Logs mantidos.

Play01/Play04/Play05 agrupados por classe foram interrompidos sem XML após falta
de avanço; Play02 encontrou a sessão anterior aberta. Não contam como aprovação.
A execução final seleciona os 23 métodos funcionais exatos. A causa do problema
do runner por classe não foi determinada.

## Reprodução

1. **Tools / Dungeon Crawler / UI / Integrate Text And HUD Scale (F10)** ou
   `DungeonCrawler.Editor.TextScaleSceneBuilder.Integrate`. Reexecutar não duplica
   widgets, listeners persistentes ou baselines; não regenera salas/arte/itens.
2. MainMenu/pausa → Opções: alterar, voltar sem aplicar, reabrir; aplicar e
   reiniciar. Restaurar padrões só persiste ao aplicar.
3. Em 150%, abrir bolsa cheia, selecionar/equipar/ordenar e rolar detalhes
   extensos. Navegar por teclado, confirmar no Enter e fechar pelo botão;
   conferir HUD, dropdowns, HP/Boss, mensagens e terminais sem ataque por clique.
4. Diretório novo em `DUNGEON_CRAWLER_TEST_REPORT_DIR`. EditMode:
   `-runTests -testPlatform EditMode -testFilter TextScaleTests`. PlayMode
   funcional usa o conteúdo de `play-filter.txt` em `-testFilter`. XML/log novos;
   não passar `-quit` junto de `-runTests`.
5. Captura gráfica sem `-batchmode`/`-nographics`: `-runTests -testPlatform
   PlayMode -testFilter DungeonCrawler.Tests.PlayerInput.TextScaleInputTests.Capture_LayoutsAtAllScalesAndResolutions`.
   Helper restaura seleção e remove somente seu preset temporário de Game view.

## Limites

Sem nova build Windows ou run normal até vitória nesta tarefa. Cenas reais com
bolsa preenchida, simulação congelada, mensagens e Boss apresentados de forma
diagnóstica; não comprova combate normal com Boss. Ultrawide é Game view emulado,
não monitor físico. Faixa coberta 100–150%, português e fonte atual; sem
certificação de contraste, acessibilidade completa, leitor de tela, gamepad,
outros idiomas/fontes, hardware adicional ou 200%. Merge depende do usuário.
