# A03 — UI-SKIN (kit modular de UI)

Acabamento para as telas **que já existem** (MainMenu, bolsa, pausa, opções), sem redesenhar o fluxo. O kit tem painéis azul-preto com chanfros largos e facetados, linhas discretas de latão fosco e botões no mesmo dourado fosco de hoje, para o texto escuro atual continuar legível. O foco usa cantos geométricos separados. São 16 sprites RGBA separados. Não há texto, contador, nome de raridade, botão funcional, cena ou prefab.

| Item | Valor |
| --- | --- |
| Arquivos finais | `../UI-SKIN-*.png` (16), com `.meta` (Sprite Single, Full Rect, borders medidos) |
| Bases 9-slice | Painel 128×128 com borda 16 (`Window`, `Inset`). Botão 256×80 com borda 12 (`Normal`, `Hover`, `Focus`, `Pressed`, `Disabled`). Slot 192×112 com borda 8 (`Normal`, `Hover`, `Selected`, `Empty`, `RarityBand`) |
| Marcadores | 24×24: `Marker-Selected` (losango), `Marker-Equipped` (escudo), `Marker-Empty` (octógono vazado). Foco: `FocusCorner` 32×32, colchete em L |
| Mapa de estados | `UI-SKIN.states.json`: estado → sprite, borders, SpriteSwap, precedência, posição dos overlays, regra de `Image.color` e o que fica para a integração |
| Fonte editável | `Tools/UiSkinGenerator/generate.py`. Edite `PANEL`, `BUTTON` e `SLOT` (tamanho, borda, chanfro, bevel e linha) e `PAL` (cores). Os parâmetros exportados ficam em `UI-SKIN.source.json` |
| Ferramenta | Python 3.11.9 + Pillow 13.0.0. Polígonos com supersampling 8×, redução com alpha pré-multiplicado e luz fixa de cima/esquerda por faceta |
| Determinismo | Duas execuções seguidas deram o mesmo SHA256 em todos os arquivos gerados pelo script (hashes dos PNG em `UiSkin.validation.json`) |

## Reproduzir

```bash
python -m pip install -r .claude/skills/create-game-assets/scripts/requirements.txt
python Tools/UiSkinGenerator/generate.py
```

O script reescreve os PNG, `review_1080.png`, `review_720.png`, `states_colour_grey_x3.png`, `UI-SKIN.source.json` e `UiSkin.validation.json`. Ele **só cria uma `.meta` quando ela não existe**, para preservar o GUID depois que a Unity importar. Sai com código 1 quando falha alguma verificação. `--out <pasta>` grava só os PNG fora de `Assets/`, para iterar sem disparar importação. `asset_report.json` e `contact_sheet.png` vêm dos scripts da skill `create-game-assets` (veja "Verificações").

## Direção visual

- **Referências reais:** capturas F10 de 1920×1080 a 100% (`Reports/F10/Visual01/*-title.png`, `*-bag.png` e `*-options.png`). Cores lidas dos builders atuais: janela `Color(.055,.075,.1)` = `#0E131A`, igual à ficha. Botão `Color(.8,.68,.45)`. Slot `Color(.10,.13,.17)` e selecionado `Color(.32,.26,.16)`. Destaque `Color(1,.82,.45)`. Texto escuro dos botões `Color(.04,.06,.08)`.
- **Forma:** octógonos com chanfro de 45° (painel 11 px, botão 8 px, slot 5 px), contorno escuro de 1 px e bevel facetado (painel 5 px, botão 4 px, slot 3 px). Cada faceta recebe um tom chapado conforme a normal, como os planos grandes do low-poly do projeto. Não há gradiente ruidoso, textura nem ornamento.
- **Latão:** `#E0B040` dessaturado para `#C9A65A`/`#6F5A30` nas linhas dos painéis e slots. Nos botões, a linha é latão escuro (`#8F7340`/`#6A532A`), para aparecer sobre o dourado.
- **Estados distinguíveis além da cor:**
  - hover: linha de 2 px;
  - foco: capas nos cantos e colchetes `FocusCorner` fora do retângulo;
  - pressionado: chanfro invertido;
  - desabilitado: sem linha de latão;
  - selecionado: moldura de 3 px e losango;
  - vazio: rebaixado, com octógono vazado;
  - equipado: escudo.

  `states_colour_grey_x3.png` mostra tudo em cor e em tons de cinza.
- **Raridade:** `Slot-RarityBand` é branco e recebe `ItemRarity.color` em runtime. O nome da raridade continua no texto. O sprite não tem roxo saturado.

## Verificações executadas (fora da Unity)

- `UiSkin.validation.json` (PNG decodificado do disco): os 16 sprites estão no tamanho exato e em RGBA, com alpha real e 0 pixels transparentes com RGB não nulo.
  - **9-slice:** em todas as bases, cada linha das bordas horizontais e cada coluna das verticais é idêntica pixel a pixel ao longo do eixo esticado, e o centro tem uma única cor. Esticar não deforma, a 720p ou 1080p. O chanfro de cada linha interna cabe na célula de canto: `chanfro + recuo·(√2−1) ≤ borda`.
  - **Contraste** do texto atual sobre o centro de cada base: os 18 pares passam. O branco fica entre 10,8 e 19,4:1. O cinza de "Vazio" sobre `Slot-Empty` dá 5,51:1. O texto escuro dá 8,65:1 no `Button-Normal`, 5,97:1 no `Pressed` e 3,77:1 no `Disabled` (mínimo 3:1 para desabilitado). As raridades sobre o slot selecionado são os casos mais justos: Raro 4,95:1 e Épico 4,71:1 (mínimo 4,5).
  - **Estados:** os 16 pares de estado de botão e de slot diferem em ≥ 291 pixels de borda com Δluminância > 24, ou seja, também em tons de cinza.
- `asset_report.py --expect-size WxH --require-alpha --json` da skill `create-game-assets`, em 5 grupos de tamanho: todos saíram com código 0 e `problems: []` (`asset_report.json`). `contact_sheet.png` vem de `build_preview_sheet.py`.
- **Revisão visual:** `review_1080.png` é uma composição só para revisão, não é entregável. Ela tem os sprites fatiados nos tamanhos medidos da captura F10 (janela 1820×1000, slots 301×149, botões 301×64 e 488×64, MainMenu 600×80) e texto Arial simulando o `LegacyRuntime`. `review_720.png` é a mesma composição reduzida por bilinear: **aproxima** o CanvasScaler e não é captura da Unity. Ajuste aplicado depois da revisão a 720p: as capas de foco do botão ficaram mais grossas.

## Importação na Unity

Cada `.meta` parte do modelo do A01 (`Tools/UiSkinGenerator/sprite.meta.template`):

- Sprite (2D and UI), modo Single, **Full Rect**, `spriteBorder` L/B/R/T medidos (16, 12 ou 8; 0 nos marcadores);
- sRGB, bilinear, clamp, sem mipmaps, PPU 100, `alphaIsTransparency`;
- tamanho máximo 256 no padrão, sem compressão.

Os GUIDs são derivados do nome por SHA256, então são estáveis e novos. A pasta tem `.meta` própria. `Source~` é ignorada pela Unity.

## Limitações e pendências

- **NÃO VALIDADO NO EDITOR.** Não houve importação, Sprite Editor, Canvas real, PlayMode nem captura. A Unity pode reserializar as `.meta` na primeira importação.
- **Sem integração.** Ligar os sprites às telas é uma tarefa de código separada. Ela inclui atribuir os sprites por builder com referência explícita, trocar o tingimento de `InventorySlotView.Render` por troca de sprite e criar hover e foco de slot (hoje `Transition.None`). A lista está em `UI-SKIN.states.json` → `integration_is_separate_code_task`.
- A ficha do manifesto sugeria `UI-SKIN.prefab`. Ele **não foi criado** de propósito: prefab e cena exigem coordenação (CLAUDE.md), e o A03 pede integração por builder.
- Botão desabilitado com texto escuro fica em 3,77:1. Se a integração mantiver a transição ColorTint por cima do sprite, o contraste cai: use SpriteSwap com `Image.color` branco.
- Aprovação artística humana pendente.

## Procedência e direitos

Geometria, cores e render são próprios do projeto Eldoria, gerados pelo script acima com Pillow (licença MIT-CMU/HPND). Não houve IA generativa de imagem nem asset de terceiros. A fonte Arial do Windows aparece só nas pranchas de revisão em `Source~`, nunca nos sprites. O prompt e o negative prompt da ficha A03 (`PROMPT_CATALOG.md` / `ASSET_MANIFEST.json`, `resources[id=UI-SKIN].production.prompt_en`) serviram de brief e não foram enviados a nenhum gerador. Itens do negative prompt verificados por inspeção: sem texto embutido, ornamentos miúdos, molduras de caveira, gradiente roxo saturado, controle falso, marca d'água, xadrez ou metal fotorrealista.
