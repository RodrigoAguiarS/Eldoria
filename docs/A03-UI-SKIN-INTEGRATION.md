# A03-I — Integração do UI-SKIN nas telas existentes

Esta tarefa é de código. Ela integra a arte produzida no lote A03 (`Assets/_Project/Art/ProductionStaging/UI-SKIN/`, branch `claude/a03-ui-skin`). É acabamento visual: **não muda** layout, alvos de clique, navegação, pilha de menus, pausa, input nem regras de inventário.

- **Executor sugerido:** Codex (implementação, Editor e testes). Claude revisa.
- **Depende de:** merge do PR do A03 na `main`.
- **Branch:** `codex/a03-ui-skin-integration`, criada da `main` atualizada.
- **Contrato da arte:** `ProductionStaging/UI-SKIN/Source~/UI-SKIN.states.json`. Ele diz qual sprite vai em cada estado, as bordas, a posição dos overlays e a regra de `Image.color`. Leia esse arquivo antes; não releia a ficha A03 inteira.

## Pontos de integração encontrados (análise estática)

| Onde | Hoje | Depois |
| --- | --- | --- |
| `InventorySceneBuilder.Button` (`InventorySceneBuilder.cs:361`) | `Image.color = (.8,.68,.45)` e transição padrão ColorTint | Sprite `Button-Normal` Sliced, `Image.color` branco, SpriteSwap |
| `InventorySceneBuilder.Panel` e as `Window` (`InventorySceneBuilder.cs:146-148`, `:179-181`; `GameShellSceneBuilder.cs:77`, `:105`, `:139`) | Cor chapada `(.055,.075,.1)` | `Panel-Window` Sliced. O overlay escuro de tela cheia (`InventoryPanel`, `PausePanel`, `OptionsPanel`) **continua** como está |
| Slots `Slot_00..15` (`InventorySceneBuilder.cs:154-157`) | `Transition.None` e `background.color` tingido em `InventorySlotView.Render` (`InventorySlotView.cs:43`) | Troca de sprite por estado (`Empty`/`Normal`/`Selected`) e `RarityBand` tingida |
| `SelectionFrame` (`InventorySceneBuilder.cs:291-303`) | 4 arestas e um quadrado a 45°, `(1,.82,.45)` | Base `Slot-Selected` e `Marker-Selected`. O GameObject `SelectionFrame` **continua existindo**, porque os testes usam `SelectionFrame.activeSelf` |
| Detalhes/scroll e PotionHud (`TextScaleSceneBuilder.cs:183-186`, `InventorySceneBuilder.cs:315`) | Cores chapadas | `Panel-Inset` Sliced. O handle e a barra de rolagem ficam fora desta tarefa |
| Botões de opções/controles (`GameShellSceneBuilder`, `ControlBindingsSceneBuilder`, `TextScaleSceneBuilder`) | Criados pelo mesmo `Button()` | A passagem de skin cobre todos |
| Cadeia de builders | Todos os builders de UI terminam em `TextScaleSceneBuilder.ConfigureScene(scene)` | Uma chamada no fim dele reaplica a skin, então rodar qualquer builder de novo não apaga a skin |

Cenas afetadas: `ControlBindingsSceneBuilder.Scenes` (`Scenes/MainMenu`, `Scenes/MVP_Arena`, `Scenes/Floor_01` e `Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters`, a cena da release). O Player é prefab (`InventorySceneBuilder.cs:36`), mas o HUD é configurado na cena. **Se algum Image alvo estiver dentro de um prefab ou de uma instância de prefab, pare e relate.**

## Escopo de arquivos (exclusivo desta tarefa)

**Criar**
- `Assets/_Project/Scripts/UI/UiSkin.cs`: `ScriptableObject` (`DungeonCrawler.UI`) só com referências de sprite. Campos: painel janela/inset; botão normal/hover/focus/pressed/disabled; slot normal/hover/selected/empty/rarityBand; markers selected/equipped/empty.
- `Assets/_Project/ScriptableObjects/UI/UiSkin.asset` (+ `.meta`), criado de forma idempotente pelo integrador.
- `Assets/_Project/Scripts/Editor/UiSkinIntegrator.cs`, com dois menus:
  - `Tools/Dungeon Crawler/UI/Apply UI Skin (A03)`;
  - `Tools/Dungeon Crawler/UI/Validate UI Skin (A03)`.

  Ele expõe `public static void ConfigureScene(Scene scene)` e carrega os sprites por caminho constante com `AssetDatabase.LoadAssetAtPath`, sem `FindObjectOfType` nem busca global.
- `Assets/_Project/Tests/EditMode/UiSkinTests.cs` e `Assets/_Project/Tests/PlayMode/PlayerInput/UiSkinSceneTests.cs`.
- `Reports/A03/`: logs, XML e capturas.

**Alterar (mínimo)**
- `Assets/_Project/Scripts/UI/InventorySlotView.cs`: campos opcionais `[SerializeField] UiSkin skin` e `Image rarityBand`, `equippedMarker`, `emptyMarker`. `Render` troca o sprite por estado quando `skin != null`. **Quando `skin == null`, o comportamento atual continua igual** (tingimento), para manter a compatibilidade de testes e cenas antigas.
- `Assets/_Project/Scripts/Editor/TextScaleSceneBuilder.cs`: uma linha no fim de `ConfigureScene`, `UiSkinIntegrator.ConfigureScene(scene);`, protegida por "skin asset existe".
- As 4 cenas acima, **somente** pela execução do integrador. Nada de editar YAML à mão.
- `Assets/_Project/Art/ProductionSpecs/ASSET_MANIFEST.json`: `UI-SKIN.integration_state`, `validation` e um novo item em `production_batches`. Também `NEXT_STEPS.md`.
- Opcional, se o usuário aprovar a arte antes: mover `ProductionStaging/UI-SKIN/*.png` para `Assets/_Project/Art/UI/Skin/` com `AssetDatabase.MoveAsset`, que preserva os GUIDs.

**Não tocar:** `Packages/`, `ProjectSettings/`, prefabs, presenters além de `InventorySlotView`, `GameMenuController`, input e navegação, os outros builders (exceto a linha acima), regras de inventário e as fontes de arte em `Source~`.

## Regras de aplicação

1. Em todo Image alvo: `type = Sliced`, `fillCenter = true`, `pixelsPerUnitMultiplier = 1`, **`color = Color.white`**. Não mexer em `raycastTarget`, `RectTransform`, `Navigation` nem `onClick`.
2. **Botões** (todo `Button` cujo `targetGraphic` é o Image criado por `Button()`; não inclui slots, dropdown, toggle nem slider):
   - `transition = SpriteSwap`;
   - `spriteState`: highlighted = Hover, pressed = Pressed, selected = Focus, disabled = Disabled;
   - o texto do Label continua `#0A0F14`.

   Hoje os botões desabilitados aparecem pelo tingimento (captura F10: Equipar/Usar). Com SpriteSwap, quem marca o desabilitado é o sprite `Disabled`. Confirme que nenhum código escreve `Image.color` ou `colors` desses botões em runtime.
3. **Slots:** `Render(stack, isSelected, equipped, icon)` escolhe a base com a precedência `selected > filled/empty`.
   - `RarityBand` fica ativa só com item, com `color = rarity.color`.
   - `equippedMarker.SetActive(equipped)` e `emptyMarker.SetActive(stack.IsEmpty)`.
   - `SelectionFrame` continua ativo quando selecionado. As 4 arestas antigas são desativadas pelo integrador, e o `Marker` antigo dá lugar ao sprite `Marker-Selected` (24×24, posição (-2, 2), sem rotação).
   - Fora desta tarefa: hover de slot (hoje `Transition.None`) e `FocusCorner`. No slot, `OnSelect` já seleciona o item, então foco é igual a seleção. Registre isso como possível A03-I2.
4. **Painéis:** `Window` recebe `Panel-Window`. Os detalhes roláveis e o PotionHud recebem `Panel-Inset`.
5. **Idempotência:** rodar o integrador 2× gera um diff vazio na segunda vez. Rodar qualquer builder antigo e depois o integrador (ou só o builder, via `TextScaleSceneBuilder`) deixa o mesmo resultado.

## Critérios de aceite

- [ ] Compila sem erros ou avisos novos. As cenas abrem sem Missing Script nem referência faltando. `Validate UI Skin (A03)` passa nas 4 cenas.
- [ ] **Alvos preservados:** `RectTransform` (anchors, pivot, posição, tamanho), `raycastTarget` e `Navigation` de todos os Selectables idênticos antes e depois. Teste EditMode comparando um snapshot.
- [ ] **Estados visíveis:** em PlayMode, um botão em foco mostra o sprite Focus e um desabilitado mostra Disabled. Slot selecionado: base Selected, `SelectionFrame.activeSelf` e marcador. Equipado: escudo. Vazio: base Empty e octógono. Raridade: faixa com `ItemRarity.color`.
- [ ] **Regressão:** todos os EditMode e PlayMode existentes passam, em especial `InventorySceneUiTests`, `ItemsCombatSceneTests`, `TextScaleTests`, `TextScaleInputTests`, `InputBindingTests` e `EquipmentComparisonTests`.
- [ ] **Capturas reais** em `Reports/A03/` (title, bag, bag-scrolled, options, controls, hud) a 1280×720 e 1920×1080, com texto em 100% e 150%, no mesmo enquadramento de `Reports/F10/Visual01`. As bordas esticam sem deformar e o texto continua legível.
- [ ] Teclado e gamepad: a navegação e o foco inicial de cada tela são iguais aos de antes (F04/F10).
- [ ] O relatório separa o que foi executado do que é análise estática. Sem Editor disponível, registrar `NÃO VALIDADO NO EDITOR`.

## Handoff

- **Base:** `main` depois do merge do A03.
- **Branch:** `codex/a03-ui-skin-integration`.
- **Próximo comando:** `Tools/Dungeon Crawler/UI/Apply UI Skin (A03)`, depois a suíte EditMode e PlayMode e as capturas da matriz F10.
- **Ao concluir:** PR para a `main` com objetivo, arquivos, testes executados, capturas, limitações e o diff de cenas gerado só pelo integrador. Nada de merge sem aprovação do usuário.
