# A01 — ICON-ACTION-DASH (ícone de dash)

Ícone de HUD para o **dash que já existe**. Uma dobra larga de capa azul com um chevron `>` de espaço negativo apontando no sentido do dash. Sem arma, sem texto, sem moldura. Atalho e cooldown continuam sendo UI de runtime.

| Item | Valor |
| --- | --- |
| Arquivo final | `../ICON-ACTION-DASH.png`: PNG RGBA 512×512 com alpha real |
| Conteúdo | caixa 416×334 em (48, 89); margem mínima de 48 px; cantos com alpha 0 |
| Fonte editável | `Tools/Lemo3DGenerator/src/production/DashIcon.mjs` (`CAPE`: vértices x/y/z nomeados, facetas, faixa, fecho, cores; `POSE`) |
| Fontes exportadas | `ICON-ACTION-DASH.source.json` (malha triangulada + parâmetros), `ICON-ACTION-DASH.silhouette.svg` (desenho 2D plano com as arestas das facetas) |
| Renderizador | página three.js r170 (`icon-render/`) servida por `core/render/page.mjs` do Lemo-Opuscar (Chromium headless, somente leitura); mesma luz, supersampling 2× e contorno de 9 px de `ItemIcons.mjs` |
| Determinismo | duas execuções produziram o mesmo SHA256 `a9b045fc6c2b54f066ed7de34db6d3512593c5c3360e3a50f65cbcf793d819a1` |

## Reproduzir

```bash
cd Tools/Lemo3DGenerator
node src/production/DashIcon.mjs
```

O script exige o three.js do Lemo (`LEMO_ROOT`, padrão `G:/lemo-opuscar/lemo-opuscar`) e reescreve o PNG, `icon-render/`, as fontes exportadas, `review_32_48.png` e `DashIcon.validation.json`. Ele termina com código 1 quando falha qualquer verificação de dimensão, tipo de cor, transparência, cantos, margem ou franja.

## Direção visual

- **Referências:** `Art/Concepts/Hero_Concept_01.png` (capa azul com barra em ponta, fecho no ombro) e os ícones de `Art/Items/Icons` (mesma câmera, luz e contorno).
- **Forma:** a capa se prende num fecho facetado à direita e se abre para trás em três pontas, como a barra da capa do Hero. As dobras alternam crista e vale (z ±), num total de 18 facetas de tecido.
- **Cores:** albedo do tecido `#2350D8`. Sob a luz da família, a média renderizada das facetas fica em `#4778EC` (`clothMeanHex`); na primeira iteração, o albedo `#2F6BFF` puro saiu com média `#569BFB`, claro e pastel demais. Avesso em `#1E1830`. Faixa de luz `#4FD8FF` na borda superior. Fecho em aço `#8A93A0`. Material fosco (roughness 0,9).
- **Leitura:** silhueta em cunha apontando para a direita, com o chevron vazado. Revisado a 48 e 32 px sobre fundo claro, escuro e pedra, em cor e em tons de cinza: `review_32_48.png` (redução do navegador) e `native_32_48_x4.png` (redução bilinear do Pillow ampliada 4× com vizinho mais próximo).

## Verificações executadas (fora da Unity)

- `DashIcon.validation.json`: PNG decodificado do disco; 512×512, RGBA, 71,32% transparente, cantos com alpha 0, margem de 48 px, 0 pixels transparentes com RGB não nulo (sem halo em filtragem bilinear).
- `asset_report.py --expect-size 512x512 --require-alpha --json` da skill `create-game-assets`: saída 0, `problems: []` (`asset_report.json`). Prancha da skill: `contact_sheet.png`.

## Importação na Unity

A `.meta` foi escrita à mão a partir de `Art/Items/Icons/ICON-ITEM-IRON.png.meta`, com GUID novo e sem a entrada de sprite herdada. Configuração: Sprite (2D and UI), modo Single, sRGB, bilinear, clamp, mipmaps desligados, PPU 100, `alphaIsTransparency`. Tamanho máximo 512 no padrão e 2048 no Standalone, como os ícones de item.

## Limitações e pendências

- **NÃO VALIDADO NO EDITOR.** Não houve importação, inspeção no HUD nem PlayMode. A Unity pode reserializar a `.meta` na primeira importação.
- **Sem integração:** ligar o ícone ao `DashCooldownPresenter`/builder é uma tarefa separada, sem mudar a regra do dash.
- **Comparação com o giro:** a distinção em 32/48 px depende de A02 (ICON-ACTION-SPECIAL), que ainda não existe.
- **Aprovação artística humana pendente.**

## Procedência e direitos

Geometria e render próprios do projeto Eldoria, gerados pelo script acima. three.js tem licença MIT. Não houve IA generativa de imagem nem uso de asset de terceiros. Prompt de referência: ficha A01 em `PROMPT_CATALOG.md` / `ASSET_MANIFEST.json` (`resources[id=ICON-ACTION-DASH].production.prompt_en`). O prompt serviu de brief; ele não foi enviado a nenhum gerador.
