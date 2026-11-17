# A06 — FX-LOOT (coleta confirmada)

Família de três **glints de coleta aceita** (`PlayerProgression.CollectionAccepted`). A forma é a mesma nas três imagens: uma estrela compacta de quatro pontas largas, com núcleo em losango e quatro pontas diagonais curtas. O canal alpha é idêntico byte a byte entre elas. Só a cor muda:

- **ciano** para XP;
- **latão contido** para ouro;
- **marfim quente** para poção, arma e armadura guardadas.

É uma **fonte raster** (atlas 2D de três variantes estáticas). Não é spritesheet de animação, AnimationClip corporal, prefab nem ParticleSystem.

| Item | Valor |
| --- | --- |
| Arquivo final | `../FX-LOOT-Atlas.png`: 384×128 RGBA, três células 128×128 lado a lado: 0 = XP, 1 = ouro, 2 = guardado. Tem `.meta` de textura de partícula (template de `FX_Spark`) com `nPOTScale: 0` |
| Glints isolados | `glints/FX-LOOT-xp.png`, `FX-LOOT-gold.png`, `FX-LOOT-stored.png` (128×128 RGBA). Cada um é idêntico byte a byte à célula correspondente do atlas |
| Mapa de rects | `FX-LOOT.atlas.json`: rect em px (origem em cima) e em UV da Unity (origem embaixo) por célula, `loot_kind_to_index` (`Xp`→0, `Gold`→1, `Potion`/`Weapon`/`Armor`→2) e a proposta de integração |
| Pivô | Centro do quad (0,5; 0,5). O centroide do alpha fica em (63,5; 61,8) px: a ponta de cima é um pouco mais longa |
| Tamanho | Forma visível de 87×91 px na célula (margem mínima de 14 px). Com o quad proposto de 0,20 m, o glint ocupa ~0,15 m e aparece com ~15 px perto do Hero a 1080p |
| Orçamento proposto | 5 partículas, vida de 0,28 s, círculo fixo de 0,20 m → alcance de 0,30 m. Limites da ficha: ≤ 8 partículas, 0,20–0,35 s, ≤ 0,35 m |
| Brilho | Alpha máximo 199 (0,78). Cobertura com alpha > 50% de 10,5% da célula. Não há branco opaco |
| Fonte editável | `Tools/FxLootGenerator/generate.py`. Edite `VARIANTS` (cores por tipo e mapeamento de `LootKind`), `SHAPE` (comprimento das pontas, base, núcleo, diagonais, halo, alpha) e `EMIT` (orçamento proposto). Os parâmetros e as facetas exportadas ficam em `FX-LOOT.source.json` |
| Ferramenta | Python 3.11.9 + Pillow 13.0.0. Polígonos com supersampling 8×, redução BOX com alpha pré-multiplicado e RGB zerado onde alpha = 0 |
| Determinismo | Duas execuções seguidas deram o mesmo SHA256 em todos os PNG/GIF. Os hashes do atlas e dos glints ficam em `FxLoot.validation.json` |

## Reproduzir

```bash
python -m pip install -r .claude/skills/create-game-assets/scripts/requirements.txt
python Tools/FxLootGenerator/generate.py
```

O script reescreve o atlas, `glints/`, as pranchas de revisão, `preview_floor.gif`, `FX-LOOT.atlas.json`, `FX-LOOT.source.json` e `FxLoot.validation.json`. Ele **só cria a `.meta` quando ela não existe**, para preservar o GUID depois que a Unity importar. O script sai com código 1 quando alguma verificação falha. `--out <pasta>` grava atlas, glints e pranchas fora de `Assets/`, para iterar sem disparar importação. `asset_report.json` e `contact_sheet.png` vêm dos scripts da skill `create-game-assets` (veja "Verificações").

## Direção visual

- **Referências reais:**
  - Piso e Hero: captura `Reports/F05/Visual-final/1920-collection.png`. A coleta acontece nas botas do Hero.
  - Orbe de XP: `ItemModels.mjs`, `M.xp` `#4FD8FF`, emissivo `#2FB8E8` e anel `M.xpRing` `#DFFBFF`. O glint de XP usa sombra `#2A9CC8`, meio `#4FD8FF` e realce `#C8F4FF`.
  - Moedas: `M.gold` `#F2C14E`. O glint de ouro é **mais contido** (`#A8822E` / `#D4A846` / `#EED38A`) para não parecer outra moeda.
  - Ícones de item: `Art/Items/Icons/ICON-ITEM-HEALTH.png` e `ICON-ITEM-IRON.png`. O marfim (`#A8977A` / `#D9CCAC` / `#EADCBA`) é neutro: não sugere vermelho de poção nem aço de arma, e serve a qualquer item guardado.
  - Burst atual do F05: `PlayerRewardFeedback.OnCollected`, 4 partículas tingidas nas mesmas três famílias de cor.
  - Telegraph do Boss: `Mat_TelegraphFill` `(1, .2, .15)`.
- **Forma:** a mesma estrela compacta nas três imagens.
  - Quatro pontas largas (topo 50 px, lados 43 px, base 40 px). A base mais curta faz o glint "pousar" sobre o pickup visto pela câmera de 45°, em vez de virar uma cruz simétrica.
  - Cada ponta tem duas facetas chapadas que partem de um vértice interno deslocado para a luz.
  - Núcleo em losango claro e quatro pontas diagonais curtas a 50% de opacidade, atrás.
  - Halo curto de 6 degraus.
  - Luz de cima/esquerda, a mesma dos ícones, do UI-SKIN, do FX-HEAL e do FX-LEVEL.
- **Distinção entre os tipos** (médias ponderadas pelo alpha, em `FxLoot.validation.json`):
  - XP: matiz 194°, saturação 0,58;
  - ouro: matiz 42°, saturação 0,61;
  - guardado: matiz 41°, saturação 0,22.

  Ouro e marfim têm matiz parecido, então a separação vem da saturação (diferença de 0,38) e da distância RGB entre as médias (mínima de 83,6).
- **Sucesso, não perigo:** nenhum pixel entra no matiz de ameaça de 0–36° com saturação alta. As faces escuras de latão ficam com matiz ≥ 40°, longe do laranja `#FF9A2E` (32°). Não há verde nem branco opaco. `review_danger.png` põe os três glints ao lado de um telegraph vermelho e de um losango laranja de ameaça.
- **Sem recompensa nova:** não há moeda, frasco, símbolo, texto nem número. Poção, arma e armadura usam a mesma célula marfim.

## Verificações executadas (fora da Unity)

- `FxLoot.validation.json`: 21 checagens no PNG decodificado, todas ok:
  - atlas 384×128 RGBA e três glints 128×128 RGBA, com célula i igual ao glint i;
  - **mesma forma:** canal alpha idêntico nas três variantes;
  - 0 pixels transparentes com RGB não nulo;
  - margem mínima de 14 px até a borda da célula (limite 10 px);
  - centroide do alpha a ≤ 2,2 px do centro;
  - alpha máximo 199 e cobertura com alpha > 50% de 10,5% (limites 205 e 22%);
  - 0 px verdes, 0 px saturados em matiz de ameaça, 0 px de branco opaco;
  - XP lê como ciano, ouro como latão e guardado como marfim, e as três médias são distintas;
  - **legível sobre a pedra:** contraste (WCAG) da forma composta de pelo menos 2,42 sobre `#55596B` e 3,27 sobre `#3E4152`;
  - envelope proposto: 0,30 m ≤ 0,35 m, 5 ≤ 8 partículas, 0,28 s dentro de 0,20–0,35 s.
- `asset_report.py --expect-size … --require-alpha --json` da skill `create-game-assets`: o atlas (384×128) e os três glints (128×128) saíram com código 0 e `problems: []` (`asset_report.json`). `contact_sheet.png` vem de `build_preview_sheet.py`.
- **Revisão visual** (composições só para revisão, não entregáveis):
  - `review_floor.png`: o burst proposto (5 glints no pico, t = 0,10 s) sobre a captura real do F05 de coleta. Em cima, na escala estimada de gameplay (~75 px/m a 1080p). Embaixo, ampliado 3×;
  - `review_backgrounds.png`: os três glints sobre pedra `#3E4152`/`#55596B`, painel `#0E131A` e aço `#B9C4CF`;
  - `review_icons.png`: cada glint abaixo da referência do seu tipo (amostra da cor da orbe de XP, amostra da cor das moedas, ícone de poção, ícone de espada);
  - `review_danger.png`: telegraph vermelho e losango laranja de ameaça ao lado dos glints;
  - `preview_floor.gif`: o burst proposto das três variantes em sequência, a 40 ms por passo, sobre o recorte do Hero.

  Ajustes feitos depois da revisão:
  - as facetas passaram a ser compostas numa camada própria e só depois multiplicadas pela opacidade. Antes, a estrela sobre o halo e as diagonais chegava a alpha 252;
  - o realce do marfim desceu de `#F1E7CF` para `#EADCBA` e a opacidade de pico de 0,78 para 0,74, porque o núcleo gerava 59 px de branco opaco.

## Integração proposta (tarefa de código separada)

`FX-LOOT.atlas.json` → `unity_integration_proposal` descreve:

- **Emissor:** reutilizar o `collectionEffect` existente do `PlayerRewardFeedback`. Nada é instanciado por coleta.
- **Material:** `URP/Particles/Unlit`, Transparent/Alpha, cor branca (a cor já vem na textura), com a opacidade multiplicada pela opção de flash do F05 (0 desliga).
- **ParticleSystem:** 5 partículas em posições fixas num círculo de 0,20 m, a 0,15 m do piso, subindo 0,40 m/s. `startLifetime` 0,28 s, `startSize` 0,20 m. Alpha sobe em 25% da vida e cai a partir de 45%.
- **Texture Sheet Animation:** Grid 3×1, `frameOverTime` constante 0 e `startFrame` = `loot_kind_to_index[drop.Kind]`, definido no módulo antes do Emit. **Confira no Editor se `startFrame` espera índice de quadro ou fração.** Alternativa: três materiais com offset/tiling fixos.
- **Renderer:** Billboard, pivô no centro.

Regras que a integração precisa manter:

- emitir **só depois de `CollectionAccepted`**. Pickup recusado (bolsa cheia, Hero morto, componente desativado) não emite;
- é só apresentação: não lê nem altera `LootTable`, quantidades, chances ou o RNG de loot. As posições são fixas, sem `UnityEngine.Random`;
- coletas seguidas reiniciam o mesmo emissor (Clear + Emit), com no máximo 5 partículas vivas;
- pausa congela; morte, reset e desativação limpam;
- não cria tipo novo de recompensa.

O burst atual de 4 partículas tingidas pode ser substituído pela integração, mas isso é decisão da tarefa de código.

## Importação na Unity

A `.meta` do atlas parte do template do A04, que é cópia de `Assets/_Project/Art/Items/VFX/FX_Spark.png.meta`, a textura de partícula já integrada:

- Default, sRGB, `alphaIsTransparency`, bilinear, clamp, mipmaps on;
- padrão 2048, Standalone/WebGL com compressão normal.

A única diferença é `nPOTScale: 0` (None). O atlas tem 384×128, e 384 não é potência de 2. Com `ToNearest`, a Unity o reescalaria para 512×128 e as células deixariam de cair em pixel inteiro. Com None, as células 128 ficam exatas. Como 384 e 128 são múltiplos de 4, a compressão em blocos continua possível. **Confira compressão e mips no Editor.** As margens de ≥ 14 px seguram os mips até 16×16 por célula sem vazar entre tipos.

O GUID do atlas e o da pasta são derivados do nome por SHA256: estáveis e novos. `Source~` é ignorada pela Unity, e os glints isolados não são importados.

## Limitações e pendências

- **NÃO VALIDADO NO EDITOR.** Não houve importação, material, ParticleSystem, PlayMode nem captura. A Unity pode reserializar a `.meta` na primeira importação. Alpha, seleção da célula por tipo, reset e flash reduzido precisam ser conferidos em PlayMode.
- **Sem integração.** `CollectionAccepted` continua com o burst procedural do F05. Usar o atlas é uma tarefa de código separada, com testes de coleta recusada, coleta cheia, pausa, reset e opções.
- `review_floor.png` e `preview_floor.gif` **simulam** o burst sobre a captura do F05, que já contém o burst procedural atual. Não são captura da Unity.
- Sobre a capa azul do Hero, o glint ciano tem menos contraste que sobre a pedra. Confira a ordenação e a leitura em PlayMode. Se preciso, suba `EMIT.height_m` ou afaste as partículas para os lados.
- Não há render dos pickups 3D no repositório. A comparação usa as cores dos materiais em `ItemModels.mjs` e os ícones de item.
- A escala in-game é uma estimativa a partir da captura. A leitura final depende do PlayMode.
- A ficha do manifesto sugeria `FX-LOOT.prefab`. Ele **não foi criado** de propósito: prefab exige coordenação (CLAUDE.md), e o A06 pede só a fonte raster.
- Aprovação artística humana pendente.

## Procedência e direitos

Geometria, cores e render são próprios do projeto Eldoria, gerados pelo script acima com Pillow (licença MIT-CMU/HPND). Não houve IA generativa de imagem nem asset de terceiros. A captura do F05 e os ícones de item aparecem só nas pranchas de revisão em `Source~`, nunca no atlas.

O prompt e o negative prompt da ficha A06 (`PROMPT_CATALOG.md`) serviram de brief e não foram enviados a nenhum gerador. Itens do negative prompt verificados: sem moedas ou poções duplicadas, texto, símbolo de moeda nova, explosão grande, fundo, xadrez ou marca d'água.
