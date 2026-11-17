# A02 — ICON-ACTION-SPECIAL (ícone do giro de espada)

Ícone de HUD para o **giro de 360° que já existe**. Uma espada curta e larga de aço na diagonal, dentro de um único arco ciano facetado com o centro aberto. A ponta da espada fica sobre a cabeça larga do arco, e o arco afina até uma cauda fina. Isso indica o giro no sentido anti-horário. Não tem texto, números, timer ou moldura. Atalho e cooldown continuam sendo UI de runtime, e o PNG não define custo, dano nem tempo.

| Item | Valor |
| --- | --- |
| Arquivo final | `../ICON-ACTION-SPECIAL.png`: PNG RGBA 512×512 com alpha real |
| Conteúdo | caixa 416×416 em (48, 48); margem mínima de 48 px; cantos com alpha 0 |
| Fonte editável | `Tools/Lemo3DGenerator/src/production/SpinIcon.mjs` (`SWEEP`: raio, extensão, facetas, afinamento, cores; `SWORD`: pomo, cabo, guarda, lâmina, canal, contorno; `POSE`) |
| Fontes exportadas | `ICON-ACTION-SPECIAL.source.json` (malha triangulada + parâmetros), `ICON-ACTION-SPECIAL.silhouette.svg` (desenho 2D plano com as facetas) |
| Renderizador | a mesma página three.js r170 do A01 (`PAGE` exportada de `DashIcon.mjs`), servida por `core/render/page.mjs` do Lemo-Opuscar (Chromium headless, somente leitura); mesma luz, supersampling 2× e contorno de 9 px |
| Determinismo | duas execuções produziram o mesmo SHA256 `aa0666fbd649f78f6a9e6711c2716d97794cf6f7d311c86f8d20c60df6bc7e2c` |

## Reproduzir

```bash
cd Tools/Lemo3DGenerator
node src/production/SpinIcon.mjs
```

O script exige o three.js do Lemo (`LEMO_ROOT`, padrão `G:/lemo-opuscar/lemo-opuscar`). Ele reescreve o PNG, `icon-render/`, as fontes exportadas, `review_32_48.png` e `SpinIcon.validation.json`, e sai com código 1 quando falha alguma verificação de dimensão, tipo de cor, transparência, cantos, margem ou franja.

`native_32_48_x4.png`, `contact_sheet.png` e `asset_report.json` vêm dos scripts Python (Pillow 13) descritos em "Verificações".

## Direção visual

- **Referências:** A01 (`ICON-ACTION-DASH`: mesma pose `rot [-8, -18, -4]`, elevação 12°, luz e contorno), `Art/Items/Icons/ICON-ITEM-IRON.png` e `MODEL-ITEM-IRON` em `ItemModels.mjs` (lâmina reta de aço, canal escuro, guarda reta de latão, cabo de couro com anéis, pomo de latão). A espada não é elemental: o ciano está só no arco, não na lâmina.
- **Forma:**
  - O arco cobre 292° em 18 segmentos com cristas e vales em z alternado. A cabeça tem a borda externa adiantada 9°, num corte inclinado. A partir da metade, a borda interna sobe até a externa e forma uma cauda fina.
  - O vão entre a cauda e a cabeça, junto com o centro vazado, separa este ícone de um disco fechado e do telegraph hostil.
  - A espada mede 74 unidades: lâmina com seção em losango, canal escuro, guarda reta, cabo com dois anéis e pomo hexagonal.
  - Um contorno escuro por peça (`SWORD.backing`) separa a espada do arco onde os dois se sobrepõem.
- **Cores:** o albedo do arco é `#22A6CC`, mais escuro de propósito, como a correção de cor do A01. Sob a luz da família, a média renderizada fica em `#52E2F4`, perto de `#4FD8FF`. Aço `#B9C4CF`, que renderiza `#CBD5DE`. Latão `#E0B040`, que renderiza `#F6CF56` (mesmo albedo e luz do ícone da Espada de Ferro). Couro `#4E3020`. As médias estão em `SpinIcon.validation.json` → `renderedMeans`.
- **Leitura:** a silhueta é circular com uma diagonal, contra a cunha em chevron do A01. A revisão foi feita a 48 e 32 px sobre fundo claro, escuro e pedra, em cor e em tons de cinza, lado a lado com o A01:
  - `review_32_48.png`: redução do navegador, mais a família ITEMS-COMBAT;
  - `native_32_48_x4.png`: redução bilinear do Pillow, ampliada 4× com vizinho mais próximo. Linhas: cor (claro/escuro/pedra) e depois cinza; cada fundo mostra A01 e A02 a 48 e depois a 32 px.

## Verificações executadas (fora da Unity)

- `SpinIcon.validation.json`: PNG decodificado do disco, 512×512, RGBA, 64,49% transparente, cantos com alpha 0, margem de 48 px e 0 pixels transparentes com RGB não nulo.
- `asset_report.py --expect-size 512x512 --require-alpha --json` da skill `create-game-assets`: saída 0, `problems: []` (`asset_report.json`). A prancha `contact_sheet.png` (A01 + A02) vem de `build_preview_sheet.py`.
- **Sem regressão no A01:** `DashIcon.mjs` passou a exportar `PAGE`, `tri`, `flatNormals` e `area2` para este script. Re-renderizado depois da mudança, o A01 manteve o SHA256 `a9b045fc…819a1`. Os arquivos de `Source~` do A01 regravados nessa execução foram descartados.

## Importação na Unity

`.meta` copiada de `ICON-ACTION-DASH.png.meta` (A01), com `guid` e `spriteID` novos. Configuração: Sprite (2D and UI), modo Single, sRGB, bilinear, clamp, mipmaps desligados, PPU 100 e `alphaIsTransparency`. Tamanho máximo 512 no padrão e 2048 no Standalone/WebGL. A pasta tem `.meta` própria. `Source~` é ignorada pela Unity.

## Limitações e pendências

- **NÃO VALIDADO NO EDITOR.** Não houve importação, inspeção no HUD nem PlayMode. A Unity pode reserializar a `.meta` na primeira importação.
- **Sem integração:** ligar o ícone ao HUD por referência explícita (presenter/builder do especial) é uma tarefa separada, sem mudar a regra, o custo, o dano ou o tempo do giro.
- **Cinza sobre fundo claro:** arco e lâmina ficam com valores próximos e se separam pelo contorno escuro. Se o HUD final usar fundo claro, vale escurecer um pouco o albedo do arco em `SWEEP.colors.front`.
- **Aprovação artística humana pendente.**

## Procedência e direitos

Geometria e render são próprios do projeto Eldoria, gerados pelo script acima. three.js tem licença MIT. Não houve IA generativa de imagem nem uso de asset de terceiros. O prompt e o negative prompt da ficha A02 (`PROMPT_CATALOG.md` / `ASSET_MANIFEST.json`, `resources[id=ICON-ACTION-SPECIAL].production.prompt_en`) serviram de brief e não foram enviados a nenhum gerador. Itens do negative prompt verificados por inspeção: sem texto, números, runas, espadas extras, raios, arma de fogo, disco fechado, fotorrealismo, xadrez ou marca d'água.
