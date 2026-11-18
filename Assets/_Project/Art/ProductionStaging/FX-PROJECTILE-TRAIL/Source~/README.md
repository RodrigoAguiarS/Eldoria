# A09 — FX-PROJECTILE-TRAIL (cauda do projétil hostil)

Uma **textura de faixa** para a cauda do projétil inimigo: uma gota compacta e afilada, com cabeça larga laranja de ameaça (`#FF9A2E`) que escurece até ferrugem (`#8A4A22`) e some em alpha 0. Um centro fino mais quente, no mesmo matiz e sem chegar ao branco, corre pelo eixo na metade da cabeça. As facetas são chapadas e recebem luz de cima. Não tem faísca fora do envelope, explosão, fumaça longa, ciano, texto nem fundo.

É uma **fonte raster estática** para `TrailRenderer`. Não é prefab, `TrailRenderer` configurado, ParticleSystem nem animação. Não altera colisão nem dano do projétil.

| Item | Valor |
| --- | --- |
| Arquivo final | `../FX-PROJECTILE-TRAIL.png`: 128×32 RGBA, com `.meta` de textura de partícula (template de `FX_Spark`, sem mudanças: 128×32 já é potência de 2) |
| Convenção de UV | U ao longo do trajeto: **u = 0 é a cabeça** (junto ao projétil, ponto mais novo) e u = 1 a ponta da cauda. V atravessa a faixa, com centro em v = 0,5. É a mesma convenção do `FX_SlashTrail.png`, do A07 e do `textureMode Stretch` |
| Fonte editável | `Tools/FxProjectileTrailGenerator/generate.py`: `PAL` (cores), `DROP` (forma da gota: raiz, ponto mais largo, afinamento, segmentos), `CORE` (centro fino), `GRAD` (laranja → ferrugem), `ENV` (envelope de alpha), `TRAIL` (proposta de TrailRenderer) e `HEAD_AT_U0` (espelha U). A fonte vetorial `FX-PROJECTILE-TRAIL.source.svg` tem as facetas antes do envelope. Parâmetros e facetas também estão em `FX-PROJECTILE-TRAIL.source.json` |
| Brilho | Alpha máximo 235 (0,92). Cobertura com alpha > 50%: 21,1% da textura. Sem branco opaco |
| Forma | Mais larga na coluna 15 (18 px de 32), afina sem voltar a alargar e chega a 0 px na coluna ~90. Um único componente visível: nada fora da gota. Margem vertical de 7 px |
| Proposta de uso | `time` 0,05 s (ficha: < 0,15 s). Com a maior `projectileSpeed` atual (12 m/s), a cauda tem **0,60 m**; a 11 m/s, 0,55 m. Largura de 0,26 m, que é 65% do orbe de 0,40 m: a cauda nunca é mais larga que a área de perigo real. Nenhuma partícula extra |
| Ferramenta | Python 3.11.9 + Pillow 13.0.0 + NumPy 2.4.6. Polígonos com supersampling 8×, redução BOX com alpha pré-multiplicado, envelope por coluna e RGB zerado onde alpha = 0 |
| Determinismo | Duas execuções seguidas deram o mesmo SHA256 em todos os PNG e no GIF. O hash da textura está em `FxProjectileTrail.validation.json` |

## Reproduzir

```bash
python -m pip install -r .claude/skills/create-game-assets/scripts/requirements.txt numpy
python Tools/FxProjectileTrailGenerator/generate.py
```

O script reescreve a textura, as pranchas de revisão, `preview_projectile.gif`, o SVG, os JSON de fonte e de proposta e `FxProjectileTrail.validation.json`. As `.meta` (textura e pasta) **só são criadas se não existirem**, para preservar o GUID depois que a Unity importar. O script sai com código 1 quando alguma verificação falha. `--out <pasta>` grava a textura e as pranchas fora de `Assets/`, para iterar sem disparar importação. `asset_report.json` e `contact_sheet.png` vêm dos scripts da skill `create-game-assets` (veja "Verificações").

## Direção visual

- **Referências reais:**
  - **Projétil atual:** esfera Unity × 0,4 com `Mat_Projectile` `_BaseColor (1, .55, .1)`, sem emissão.
  - **A08 (`ProductionStaging/FX-PROJECTILE`):** brasa `#FF9A2E` e paredes de fenda ferrugem `#8A4A22`. São as mesmas duas cores da cauda, então ela serve tanto para a esfera quanto para o núcleo A08.
  - **Rastros do Hero, para não confundir:** `FX_SlashTrail.png` (cinza) e `ProductionStaging/FX-DASH` (azul da capa).
  - **Código, só leitura:** `Projectile.cs` (`radius` 0,25, `lifetime` 4 s, guarda de `timeScale`), `ProjectilePool.cs` (16/64) e as `projectileSpeed` dos `EnemyData` (Elite 12, Lancer 12, Ranged 11).
- **Forma:** uma gota só. A raiz é chanfrada no lado da cabeça e o ponto mais largo fica a 17% de U. Com a montagem proposta, esse ponto aparece logo atrás da borda do orbe. Daí a gota afina até a ponta.
  - São 7 segmentos, mais curtos perto da cabeça. Cada um tem uma faceta de cima mais clara e uma de baixo mais escura, com tons alternados: é o ritmo facetado simples da ficha.
  - O centro fino tem no máximo 3 px de altura e ocupa 50 colunas.
- **Cor:**
  - média ponderada pelo alpha em matiz 30° e saturação 0,77;
  - cabeça média `(231, 142, 51)`, a 27 de `#FF9A2E`;
  - cauda média `(151, 85, 39)`, a 18 de `#8A4A22`.
- **Não exagera o perigo:**
  - a cauda é mais estreita que o orbe e não tem halo, explosão nem faísca;
  - a área com alpha > 50% cobre 21% da textura;
  - `review_vs_current_sphere.png` mostra a esfera atual sem e com a cauda: o orbe continua sendo o maior elemento.
- **Não parece magia amiga:** não há nenhum pixel ciano/azul (165–265°) nem verde. O matiz fica a 168° do FX-DASH, e a saturação (0,77) é bem diferente da do `FX_SlashTrail` (0,00).

## Verificações executadas (fora da Unity)

- `FxProjectileTrail.validation.json`: 23 checagens no PNG relido do disco, todas ok:
  - 128×32 RGBA; 0 pixels transparentes com RGB não nulo;
  - **bordas com alpha 0:** moldura de 1 px com alpha 0 e margem vertical de 7 px;
  - **fade nas pontas:** nas 3 colunas da cabeça, que ficam dentro do orbe, alpha ≤ 30. Nos últimos 10 px da cauda, alpha ≤ 2;
  - perfil por coluna sem salto maior que 39 (a subida da cabeça fica escondida no orbe);
  - **gota:** largura máxima no primeiro terço e afinando depois sem voltar a alargar; ponta compacta; um único componente com alpha ≥ 8 (sem faíscas soltas);
  - centro quente fino (≤ 4 px) em pelo menos 20% de U;
  - cobertura com alpha > 50% ≤ 35% (real: 21,1%); alpha máximo ≤ 240 (real: 235);
  - 0 px ciano/azul, 0 px verdes, 0 px de branco opaco;
  - lê como laranja hostil, com gradiente de `#FF9A2E` para `#8A4A22`;
  - **legível contra a pedra:** contraste WCAG da cauda composta de 2,96 sobre `#3E4152` e 2,14 sobre `#55596B` (mínimo exigido: 2,0);
  - distinta dos rastros do Hero (FX-DASH e FX_SlashTrail);
  - `time` de 0,05 s < 0,15 s e cauda ≤ 0,60 m na maior velocidade;
  - largura ≤ 75% do orbe; razão do rastro compatível com a textura (2,3:1);
  - `Projectile.cs` e `ProjectilePool.cs` só lidos.
- `asset_report.py --expect-size 128x32 --require-alpha --json` da skill `create-game-assets`: código 0 e `problems: []` (`asset_report.json`). `contact_sheet.png` vem de `build_preview_sheet.py`.
- **Prévias** (todas rotuladas *PREVIEW*, só para revisão, não entregáveis):
  - `review_floor_preview.png` (a prévia de 45° da ficha): a esfera atual a 1 m de altura com a cauda no comprimento máximo, em seis rumos, projetada pela câmera de 45° sobre a captura real do F05. Em cima, na escala de gameplay (~75 px/m a 1080p); embaixo, ampliada 2×. A ordem de desenho imita a Unity: indo para longe da câmera, a cauda transparente passa na frente da borda do orbe;
  - `review_vs_current_sphere.png`: só a esfera × esfera com cauda, 3×;
  - `review_backgrounds.png`: a textura 4× sobre pedra `#3E4152`/`#55596B`, painel `#0E131A`, aço `#B9C4CF`, capa `#2F6BFF` e uma faixa de telegraph;
  - `review_vs_hero_trails.png`: `FX_SlashTrail`, FX-DASH e A09 na mesma largura;
  - `preview_projectile.gif`, 4× mais lento:
    - tiro 1: a cauda cresce até 0,6 m, **congela na pausa** e some junto com o orbe no retorno ao pool;
    - tiro 2: o mesmo objeto do pool, disparado de outro ponto, **começa sem faixa** ligando ao ponto antigo.

  Ajustes feitos depois da revisão:
  - o ponto mais largo passou de 14% para 17% de U e a montagem de 0,10 m para 0,14 m atrás do centro. Antes, a parte larga da gota ficava escondida no orbe e só aparecia um espinho fino;
  - o laranja passou a durar mais (`GRAD` 0,12/0,62 → 0,18/0,66) e as facetas de baixo ficaram menos escuras. Antes, o contraste sobre `#55596B` era de 1,99.

## Integração proposta (tarefa Codex separada)

`FX-PROJECTILE-TRAIL.trail.json` → `unity_integration_proposal` descreve:

- **Material:** **um** `sharedMaterial` `URP/Particles/Unlit`, Transparent/Alpha, cor branca (a cor já vem na textura), sem HDR. Nunca material ou textura por tiro.
- **TrailRenderer:** filho `Trail` do `Projectile.prefab`, já no prefab. Assim as instâncias do pool o herdam e nada é criado por disparo.
  - **posição:** 0,14 m atrás do centro, dentro do orbe. Com a raiz em escala 0,4, `localPosition.z` = −0,35; com a raiz em 1 (opção do A08), −0,14;
  - **tempo e largura:** `time` 0,05 s, largura 0,26 m com curva 1 → 0,8, `minVertexDistance` 0,05;
  - **render:** `textureMode` Stretch, `alignment` View, sem cap/corner vertices, sem sombras, luz ou probes, `autodestruct` desligado e `emitting` falso no prefab.

Regras de reset que a integração precisa manter:

- **`Launch`:** primeiro define `transform.position`/`rotation`, **depois** chama `trail.Clear()` e `emitting = true`. Sem esse `Clear()` depois do teleporte, a faixa liga o ponto onde o objeto saiu do pool ao novo disparo;
- **`OnDespawned`** (colisão, timeout ou `ForceRelease`): `emitting = false` e `Clear()`. A cauda some com o orbe e nenhuma trilha antiga sobrevive ao reuso;
- **`OnEnable`/`OnDisable`** (pool, troca de sala, checkpoint, morte do Hero): `Clear()`;
- **pausa** (`timeScale` 0): `Projectile.Update` já retorna sem mover. Conferir no Editor que a cauda congela em vez de encolher e que não salta ao despausar;
- **dois tiros rápidos do mesmo objeto:** cada `Launch` limpa, então não há emenda entre tiros;
- **só apresentação:** não altera `radius` (SphereCast de 0,25 m), `lifetime`, `hitMask`, velocidade, dano, pool nem colisão. Sem collider no filho.
- **opção de flash:** a cauda é um sinal estável de ameaça, não um flash, e não deveria depender dessa opção. Se a opção de reduzir efeitos for aplicada a ela, o orbe continua visível.

## Importação na Unity

A `.meta` da textura parte do template do A04, que é cópia de `Assets/_Project/Art/Items/VFX/FX_Spark.png.meta`, a textura de partícula já integrada:

- Default, sRGB, `alphaIsTransparency`, bilinear, **clamp** (as pontas com alpha 0 não repetem), mipmaps on;
- padrão 2048, Standalone/WebGL com compressão normal.

128×32 é potência de 2, então nada mudou. O GUID da textura e o da pasta são derivados do nome por SHA256 (`Eldoria/A09/…`): estáveis e novos, sem colisão com nenhuma `.meta` do projeto. A Unity ignora `Source~`.

## Limitações e pendências

- **NÃO VALIDADO NO EDITOR.** Não houve importação, material, TrailRenderer, reset no pool, PlayMode nem captura. A Unity pode reserializar a `.meta` na primeira importação.
- **Confira o sentido de U no Editor.** Se aparecer invertido, gere com `HEAD_AT_U0 = False` ou use tiling X = −1.
- **Sem integração.** Em jogo, o projétil continua sem cauda. O filho `Trail`, o `Clear()` em `Launch`/`OnDespawned` e os testes de pool, pausa e dois tiros são tarefa Codex separada, que mexe em prefab e código.
- **Pausa:** a guarda de `timeScale` do `Projectile` é conhecida, mas o envelhecimento do `TrailRenderer` com `timeScale` 0 precisa ser observado no Editor.
- **Comprimento depende da velocidade:** a 11 m/s a cauda tem 0,55 m. Uma velocidade futura acima de 12 m/s passaria de 0,6 m; nesse caso, reduza `time` (0,6 / velocidade).
- **As prévias não são captura da Unity:** elas **projetam** a faixa sobre a captura do F05, com a esfera desenhada como disco sombreado e sem luz real, bloom ou ordenação de profundidade com o Hero. O núcleo A08 não foi rasterizado nelas; a cauda usa as mesmas cores dele.
- **Prefab não criado:** a ficha do manifesto sugeria `FX-PROJECTILE-TRAIL.prefab`, mas criar prefab exige coordenação (CLAUDE.md), e o A09 pede só a textura e a fonte.
- **Aprovação artística humana pendente.**

## Procedência e direitos

Geometria, cores e render são do projeto Eldoria, gerados pelo script acima com Pillow (licença MIT-CMU/HPND) e NumPy (BSD). Não houve IA generativa de imagem nem asset de terceiros. A captura do F05, o `FX_SlashTrail.png` e o FX-DASH aparecem só nas pranchas de revisão em `Source~`, nunca na textura.

O prompt e o negative prompt da ficha A09 (`PROMPT_CATALOG.md`) serviram de brief e não foram enviados a nenhum gerador. Itens do negative prompt verificados: sem explosão de bola de fogo, magia ciano amiga, texto, fumaça longa opaca, partículas em excesso, xadrez nem marca d'água.
