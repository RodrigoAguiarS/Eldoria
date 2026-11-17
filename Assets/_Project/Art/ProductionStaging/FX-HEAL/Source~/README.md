# A04 — FX-HEAL (cura discreta)

Retorno visual curto depois de uma **cura aceita** no F05. Sete motes em losango facetado nascem junto às botas, sobem e passam de rubi a branco quente. Embaixo aparece um anel baixo, aberto e muito tênue, que se expande um pouco e some por completo. Não há poça, nuvem verde, flash amplo nem personagem no efeito. É uma **fonte raster**: não é mecânica de cura, prefab nem ParticleSystem.

| Item | Valor |
| --- | --- |
| Arquivo final | `../FX-HEAL-Sheet.png`: 1024×512 RGBA, grade 4 colunas × 2 linhas de células 256×256, lida linha a linha (quadros 0–3 em cima, 4–7 embaixo). Tem `.meta` de textura de partícula, igual a `FX_Spark` |
| Quadros isolados | `frames/FX-HEAL-F0.png` … `F7.png` (256×256 RGBA). Cada um é idêntico byte a byte à célula correspondente da sheet |
| Timing | `FX-HEAL.timing.json`: 0,4 s, 8 quadros de 0,05 s (20 fps), sem loop. O quadro 7 é totalmente transparente |
| Pivô | Centro dos pés, em (128, 180) px a partir do canto superior esquerdo da célula. Na convenção da Unity (origem embaixo) fica em (0,5; 0,297). É o mesmo em todos os quadros |
| Escala | 160 px/m no plano dos pés, célula de 1,6 m. Anel de 0,30 → 0,47 m e motes a até 0,55 m do centro (limite da ficha: 0,6 m) |
| Câmera | Pitch 45°, offset (0, 10, −10), como `ArtTestSceneBuilder`/`DungeonMvpSceneBuilder`. O anel já vem projetado como elipse 1 : 0,707 |
| Orçamento | 7 motes + 1 anel (≤ 16 partículas da ficha). Alpha máximo 240/255. Cobertura com alpha > 50% abaixo de 6% da célula em todos os quadros |
| Fonte editável | `Tools/FxHealGenerator/generate.py`. Edite `PAL` (cores), `RING` (raio, largura, aberturas, fade por quadro), `MOTES` (posição, altura, atraso, tamanho, deriva), `MOTE`, `PPM`, `PIVOT` e `DURATION`. Os parâmetros exportados ficam em `FX-HEAL.source.json` |
| Ferramenta | Python 3.11.9 + Pillow 13.0.0. Polígonos com supersampling 8×, redução BOX com alpha pré-multiplicado e RGB zerado onde alpha = 0 |
| Determinismo | Duas execuções seguidas deram o mesmo SHA256 em todos os PNG/GIF. Os hashes da sheet e dos quadros ficam em `FxHeal.validation.json` |

## Reproduzir

```bash
python -m pip install -r .claude/skills/create-game-assets/scripts/requirements.txt
python Tools/FxHealGenerator/generate.py
```

O script reescreve a sheet, `frames/`, as pranchas de revisão, `preview_floor.gif`, `FX-HEAL.timing.json`, `FX-HEAL.source.json` e `FxHeal.validation.json`. Ele **só cria a `.meta` quando ela não existe**, para preservar o GUID depois que a Unity importar. Sai com código 1 quando falha alguma verificação. `--out <pasta>` grava só a sheet e os quadros fora de `Assets/`, para iterar sem disparar importação. `asset_report.json` e `contact_sheet.png` vêm dos scripts da skill `create-game-assets` (veja "Verificações").

## Direção visual

- **Referências reais:**
  - Piso e Hero: captura `Reports/F05/Visual-final/1920-healing.png`.
  - Rubi do frasco: `ItemModels.mjs`, `M.ruby` `#C21D3A`.
  - Cor da cura atual do F05: `PlayerRewardFeedback`, `Color(.92,.32,.42)` = `#EB526B`.
  - Câmera: builders citados acima.

  O ícone e o modelo do frasco não foram refeitos.
- **Forma:** os motes são losangos verticais grandes, em 4 facetas chapadas, com luz de cima/esquerda (mesma direção do UI-SKIN e dos ícones). Cada um tem um halo de 6 degraus concêntricos de alpha baixo, sem gradiente ruidoso. O anel é um polígono de 20 lados, com planos largos como o low-poly do projeto.
- **Cor:** os motes vão do rubi `#E04A63` (entre o frasco e o F05) ao branco quente `#FFEBD8`/marfim `#FFF6EA` enquanto sobem. O anel vai de `#F07C8C` a branco quente, com alpha ≤ 0,30. Nenhum pixel visível cai no vermelho de ameaça `#FF2A2A`, no laranja `#FF9A2E` (matiz 0–45° com saturação > 0,55) ou em verde.
- **Leitura de cura, não de perigo:**
  - o anel é **aberto atrás do Hero** (84°) e quebrado em dois pontos na frente. Não forma um círculo fechado de telegraph;
  - a abertura de trás também evita que o billboard desenhe um arco por cima das botas;
  - o anel cresce pouco e desaparece no quadro 6.
- **Posição dos motes:** só à frente e aos lados. Um mote atrás do Hero apareceria, no billboard, desenhado por cima do corpo. As alturas e os atrasos estão desencontrados para os motes não se agruparem em pares.
- **Ritmo:**
  - quadro 0: anel nasce e motes surgem pequenos;
  - quadros 1–3: pico;
  - quadros 4–6: motes claros sobem e somem;
  - quadro 7: vazio.

## Verificações executadas (fora da Unity)

- `FxHeal.validation.json`, com 14 checagens no PNG decodificado do disco, todas ok:
  - sheet 1024×512 RGBA e 8 quadros 256×256 RGBA, com célula k igual ao quadro k;
  - 0 pixels transparentes com RGB não nulo;
  - margem mínima de 20 px até a borda da célula, ou seja, nenhum corte;
  - quadro 7 com alpha máximo 0, e quadros 0–6 visíveis;
  - quadro 6 abaixo de 35% do pico;
  - pivô coerente: em todos os quadros o anel fica centrado em x = 128 e sua base fica onde o raio do quadro manda, com tolerância de 1,5 px;
  - 0 px em matiz verde e 0 px saturados em matiz de ameaça;
  - sem flash amplo;
  - raio ≤ 0,6 m e ≤ 16 partículas.
- `asset_report.py --expect-size … --require-alpha --json` da skill `create-game-assets`: a sheet (1024×512) e os 8 quadros (256×256) saíram com código 0 e `problems: []` (`asset_report.json`). `contact_sheet.png` vem de `build_preview_sheet.py`.
- **Revisão visual** (composições só para revisão, não entregáveis):
  - `review_floor.png`: os 8 quadros sobre a captura real do F05, na escala estimada de gameplay (~75 px/m a 1080p, pelo ladrilho de 2 m ≈ 150 px), ampliados 2×. Na linha de cima com o Hero, na de baixo sobre piso limpo;
  - `review_backgrounds.png`: os quadros sobre pedra `#3E4152`/`#55596B`, painel `#0E131A` e aço `#B9C4CF`, com uma cruz no pivô;
  - `preview_floor.gif`: animação a 0,05 s por quadro sobre o recorte do Hero.

  Ajustes feitos depois da revisão:
  - pivô subido de y = 196 para 180, porque o anel encostava na borda;
  - anel clareado, porque lia como sombra escura;
  - abertura de trás reduzida de 118° para 84°;
  - motes aumentados;
  - dois pares de motes laterais separados.

## Integração proposta (tarefa de código separada)

`FX-HEAL.timing.json` → `unity_integration_proposal` descreve:

- **Material:** `URP/Particles/Unlit`, Transparent/Alpha, com a opacidade multiplicada pela opção de flash do F05.
- **ParticleSystem:** 1 partícula, burst único, `startLifetime` 0,4 s, `startSize` 1,6 m, simulação em World.
- **Texture Sheet Animation:** Grid 4×2, Whole Sheet, linear, 1 ciclo.
- **Renderer:** Billboard com `pivot` (0; 0,203; 0) para o pivô dos pés coincidir com a partícula. **Confira o sinal no Editor.**

Regras do F05 que continuam valendo:

- dispara só em `HealingAccepted` com origem diferente de `LevelUp`;
- não atrasa a cura;
- a pausa congela o efeito;
- morte, reset e desativação limpam;
- o retrigger reusa o emissor sem instanciar.

## Importação na Unity

A `.meta` da sheet é cópia de `Assets/_Project/Art/Items/VFX/FX_Spark.png.meta`, a textura de partícula já integrada (`Tools/FxHealGenerator/texture.meta.template`):

- Default, sRGB, `alphaIsTransparency`, bilinear, clamp, mipmaps on;
- padrão 2048, Standalone/WebGL com compressão normal.

As margens de ≥ 20 px por célula toleram os mips usados a ~0,47× sem vazar entre quadros. O GUID é derivado do nome por SHA256, então é estável e novo. A pasta tem `.meta` própria. `Source~` é ignorada pela Unity, e os quadros isolados não são importados.

## Limitações e pendências

- **NÃO VALIDADO NO EDITOR.** Não houve importação, material, ParticleSystem, PlayMode nem captura. A Unity pode reserializar a `.meta` na primeira importação.
- **Sem integração.** O F05 continua com os 7 motes procedurais (`FX_Spark`). Trocar pelo sheet é uma tarefa de código separada, com builder e testes de pausa/reset/opções.
- `review_floor.png` usa a captura do F05, que **já contém os motes procedurais atuais** perto das botas (pontos vermelhos pequenos). Eles não fazem parte desta sheet.
- A escala in-game é uma estimativa a partir da captura. A leitura final depende do PlayMode.
- A ficha do manifesto sugeria `FX-HEAL.prefab`. Ele **não foi criado** de propósito: prefab exige coordenação (CLAUDE.md), e o A04 pede só a fonte raster.
- Aprovação artística humana pendente.

## Procedência e direitos

Geometria, cores e render são próprios do projeto Eldoria, gerados pelo script acima com Pillow (licença MIT-CMU/HPND). Não houve IA generativa de imagem nem asset de terceiros. A captura do F05 aparece só nas pranchas de revisão em `Source~`, nunca na sheet.

O prompt e o negative prompt da ficha A04 (`PROMPT_CATALOG.md`) serviram de brief e não foram enviados a nenhum gerador. Itens do negative prompt verificados: sem personagem, texto, fundo opaco, xadrez, poça de veneno, nuvem verde de dano, flash de tela cheia, câmera diferente por quadro ou marca d'água.
