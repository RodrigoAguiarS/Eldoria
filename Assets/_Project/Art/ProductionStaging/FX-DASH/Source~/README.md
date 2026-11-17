# A07 — FX-DASH (rastro do dash)

Uma **textura de faixa** para o rastro do dash do Hero: três dobras largas e afiladas de pano azul-real (a cor da capa, `#2F6BFF`), em facetas chapadas, com uma borda ciano fina e contida (`#4FD8FF`) só na dobra principal. O alpha zera nas duas pontas de U e nas bordas de V. Não tem personagem, clone, arco de arma, raio, fumaça, texto nem fundo.

É uma **fonte raster estática** para `TrailRenderer`. Não é prefab, `TrailRenderer` configurado, ParticleSystem nem animação.

| Item | Valor |
| --- | --- |
| Arquivo final | `../FX-DASH-Trail.png`: 256×64 RGBA, com `.meta` de textura de partícula (template de `FX_Spark`, sem mudanças: 256×64 já é potência de 2) |
| Convenção de UV | U ao longo do trajeto: **u = 0 é a cabeça** (junto ao Hero, ponto mais novo) e u = 1 a cauda (ponto mais antigo). V atravessa a faixa, centro em v = 0,5. É a mesma convenção do `FX_SlashTrail.png` já integrado e do `textureMode Stretch` do `TrailRenderer` |
| Fonte editável | `Tools/FxDashGenerator/generate.py`: `PAL` (cores), `SWEEPS` (as três dobras: posição, arqueamento, largura, tom, alpha), `EDGE` (borda ciano), `ENV` (envelope de alpha), `TRAIL` (proposta de TrailRenderer) e `HEAD_AT_U0` (espelha U). Fonte vetorial `FX-DASH.source.svg` com as facetas antes do envelope; parâmetros e facetas exportados em `FX-DASH.source.json` |
| Brilho | Alpha máximo 163 (0,64). Cobertura com alpha > 50%: 13,0% da textura. Sem branco opaco |
| Pontas | Moldura de 1 px com alpha 0. Nos 15 px da cabeça o alpha não passa de 29; nas 8 colunas junto ao Hero, de 8. A cauda chega a 0 antes da borda |
| Proposta de uso | `time` 0,16 s (ficha: ≤ 0,20 s), largura 0,60 m, rastro ≈ 2,56 m com os valores atuais do `PlayerDash` (16 m/s, 0,18 s). Razão 4,3:1, próxima da textura 4:1. Nenhuma partícula extra |
| Ferramenta | Python 3.11.9 + Pillow 13.0.0 + NumPy 2.4.6. Polígonos com supersampling 8×, redução BOX com alpha pré-multiplicado, envelope por coluna e RGB zerado onde alpha = 0 |
| Determinismo | Duas execuções seguidas deram o mesmo SHA256 em todos os PNG e no GIF. O hash da textura fica em `FxDash.validation.json` |

## Reproduzir

```bash
python -m pip install -r .claude/skills/create-game-assets/scripts/requirements.txt numpy
python Tools/FxDashGenerator/generate.py
```

O script reescreve a textura, as pranchas de revisão, `preview_dash.gif`, `FX-DASH.source.svg`, `FX-DASH.source.json`, `FX-DASH.trail.json` e `FxDash.validation.json`. Ele **só cria as `.meta` (textura e pasta) quando elas não existem**, para preservar o GUID depois que a Unity importar. O script sai com código 1 quando alguma verificação falha. `--out <pasta>` grava a textura e as pranchas fora de `Assets/`, para iterar sem disparar importação. `asset_report.json` e `contact_sheet.png` vêm dos scripts da skill `create-game-assets` (veja "Verificações").

## Direção visual

- **Referências reais:**
  - Capa do Hero: `#2F6BFF` e acento ciano `#4FD8FF` em `Art/Concepts/ART_DIRECTION_AND_PROMPTS.md`, e o Hero da captura `Reports/F05/Visual-final/1920-collection.png`.
  - Rastro de ataque existente: `Art/Items/VFX/FX_SlashTrail.png` (gradiente cinza suave, claro em u = 0) e sua configuração em `ProductionVisualIntegrator.ConfigureTrail` (Stretch, View, `time` 0,13 s).
  - Dash: `Scripts/Player/PlayerDash.cs` (`dashSpeed` 16, `dashDuration` 0,18 s, `IsDashing` sem evento).
  - Telegraph do Boss: `Mat_TelegraphFill` `(1, .2, .15)`.
- **Forma:** três dobras de pano, de trás para a frente:
  - `lower_fold` e `upper_fold`: dobras estreitas, curtas, abertas para baixo e para cima;
  - `main_sweep`: a dobra principal, larga, que chega mais longe na cauda.

  Cada dobra tem raiz larga e chanfrada no lado da cabeça e afina até uma ponta na cauda. Ela é dividida em segmentos com duas facetas chapadas: a de cima mais clara (luz de cima, como ícones, UI-SKIN e FX-HEAL/LEVEL/LOOT) e a de baixo mais escura. Os segmentos alternam o tom, o que dá a leitura de dobras largas de capa.
- **Cor:** média ponderada pelo alpha em matiz 222°, saturação 0,79, praticamente o matiz da capa (223°). A borda ciano ocupa 2,4% dos pixels visíveis.
- **Diferente do arco de ataque:** o `FX_SlashTrail` é cinza (saturação 0,00) e é um gradiente liso. O FX-DASH é azul saturado, facetado, com três pontas na cauda (diferença média de alpha 38). `review_vs_slash.png` mostra os dois na mesma escala.
- **Não sugere dano:** não há vermelho, laranja, verde, raio, faísca nem lâmina. A cabeça quase transparente evita uma cópia opaca do Hero.

## Verificações executadas (fora da Unity)

- `FxDash.validation.json`: 21 checagens no PNG decodificado, todas ok:
  - 256×64 RGBA; 0 pixels transparentes com RGB não nulo;
  - **bordas transparentes:** moldura de 1 px com alpha 0, margem vertical de 6 px;
  - **fade nas duas pontas:** nos 15 px de cada ponta, alpha ≤ 25% do pico (29 na cabeça, 0 na cauda); 8 colunas junto ao Hero com alpha ≤ 8; perfil de alpha por coluna sem saltos maiores que 6;
  - **não esconde chão nem telegraph:** alpha máximo 163 (0,64), cobertura com alpha > 50% de 13,0%; no pixel mais opaco, 36% da diferença entre telegraph e piso continua visível;
  - 0 px verdes, 0 px saturados em matiz de ameaça (0–36°), 0 px de branco opaco;
  - lê como azul da capa; borda ciano entre 2% e 20%; duas ou três dobras;
  - **legível sobre a pedra:** distância RGB da faixa composta à pedra de pelo menos 80,4. O contraste de luminância (WCAG) é baixo, 1,26 sobre `#55596B` e 1,58 sobre `#3E4152`: a leitura vem do matiz, por design discreto;
  - não copia o `FX_SlashTrail`;
  - cauda proposta de 0,16 s ≤ 0,20 s; razão do rastro proposto compatível com a textura;
  - `PlayerDash.cs` só é lido (velocidade, duração, invulnerabilidade e recarga registradas, nada é alterado).
- `asset_report.py --expect-size 256x64 --require-alpha --json` da skill `create-game-assets`: código 0 e `problems: []` (`asset_report.json`). `contact_sheet.png` vem de `build_preview_sheet.py`.
- **Prévias** (todas rotuladas *PREVIEW*, só para revisão, não entregáveis):
  - `review_floor_preview.png`: o rastro proposto no comprimento máximo, projetado pela câmera de 45° sobre a captura real do F05, como faixa alinhada à vista (`alignment View`) na altura da capa. Em cima, na escala de gameplay (~75 px/m a 1080p). Embaixo, ampliado 2×;
  - `review_telegraph_preview.png`: o mesmo rastro atravessando um telegraph vermelho de Boss desenhado no piso, ao lado do quadro sem rastro;
  - `review_backgrounds.png`: a textura 2× sobre pedra `#3E4152`/`#55596B`, painel `#0E131A`, aço `#B9C4CF` e uma faixa de telegraph;
  - `review_vs_slash.png`: `FX_SlashTrail.png` acima e FX-DASH abaixo;
  - `preview_dash.gif`: o rastro cresce durante o dash (0,18 s) e some pela cauda em 0,16 s, 4× mais lento; o último quadro mostra o estado após reset, sem rastro.

  Ajustes feitos depois da revisão:
  - as raízes das dobras foram puxadas para perto de u = 0 e a subida do envelope foi de 13% para 20% de U. Antes, a raiz da dobra principal criava um degrau de alpha de 44 entre colunas e a cabeça chegava a 57;
  - a borda ciano subiu de alpha 0,55 para 0,85. Antes, misturada ao azul, ela não lia como ciano (0% dos pixels);
  - as dobras de cima e de baixo foram afastadas da principal para as três lerem separadas.

## Integração proposta (tarefa Codex separada)

`FX-DASH.trail.json` → `unity_integration_proposal` descreve:

- **Material:** `URP/Particles/Unlit`, Transparent/Alpha, cor branca (a cor já vem na textura), com a opacidade multiplicada pela opção de flash existente (0 desliga).
- **TrailRenderer** reutilizado, filho do Hero a 0,80 m (altura da capa), atrás do corpo: `time` 0,16 s, largura 0,60 m com curva 1 → 0,85 → 0,55, `minVertexDistance` 0,06, `textureMode` Stretch, `alignment` View, sem sombras, luz ou probes, `autodestruct` desligado e `emitting` falso por padrão.
- **Ponte de início/fim:** `PlayerDash` só expõe `IsDashing`, sem evento. Na borda false→true: `Clear()` e `emitting = true`. Na borda true→false: `emitting = false`, e a cauda some sozinha em ≤ 0,16 s.

Regras que a integração precisa manter:

- reset/checkpoint (`RestoreCheckpoint`), morte, `OnDisable`/reativação e volta ao menu: `emitting = false` e `Clear()`. **Nenhuma trilha antiga sobrevive**;
- teleporte ou troca de sala durante ou logo após o dash: `Clear()` antes do novo ponto, sem faixa atravessando a sala;
- pausa (`timeScale` 0): não emitir pontos novos; ao despausar, continuar ou limpar, sem salto;
- dois dashes seguidos: o segundo início chama `Clear()` e não emenda com a cauda anterior;
- é só apresentação: não altera `dashSpeed`, `dashDuration`, `invulnerabilityDuration`, `cooldown`, colisão, dano nem input. Sem collider.

## Importação na Unity

A `.meta` da textura parte do template do A04, que é cópia de `Assets/_Project/Art/Items/VFX/FX_Spark.png.meta`, a textura de partícula já integrada:

- Default, sRGB, `alphaIsTransparency`, bilinear, **clamp** (as pontas com alpha 0 não repetem), mipmaps on;
- padrão 2048, Standalone/WebGL com compressão normal.

256×64 é potência de 2, então nenhuma mudança foi necessária. O GUID da textura e o da pasta são derivados do nome por SHA256: estáveis e novos (não colidem com nenhuma `.meta` do projeto). `Source~` é ignorada pela Unity.

## Limitações e pendências

- **NÃO VALIDADO NO EDITOR.** Não houve importação, material, TrailRenderer, ponte, PlayMode nem captura. A Unity pode reserializar a `.meta` na primeira importação.
- **Confira o sentido de U no Editor.** A convenção u = 0 na cabeça segue o `FX_SlashTrail` e o Stretch do TrailRenderer. Se aparecer invertida, gere com `HEAD_AT_U0 = False` ou use tiling X = −1 no material.
- **Sem integração.** O dash continua sem rastro. A ponte é tarefa Codex separada, com testes de reset, morte, pausa, reativação, teleporte e dois dashes seguidos.
- As prévias **projetam** a faixa sobre a captura do F05, em que o Hero está parado e não há ordenação de profundidade com o corpo. Não são captura da Unity.
- O contraste de luminância com a pedra é baixo por design. Confira em PlayMode sobre piso claro e perto de tochas; se faltar leitura, suba `ENV.peak` (limite atual de alpha 165) ou clareie `PAL.light`.
- A ficha do manifesto sugeria `FX-DASH.prefab`. Ele **não foi criado** de propósito: prefab exige coordenação (CLAUDE.md), e o A07 pede só a textura e a fonte.
- Aprovação artística humana pendente.

## Procedência e direitos

Geometria, cores e render são próprios do projeto Eldoria, gerados pelo script acima com Pillow (licença MIT-CMU/HPND) e NumPy (BSD). Não houve IA generativa de imagem nem asset de terceiros. A captura do F05 e o `FX_SlashTrail.png` aparecem só nas pranchas de revisão em `Source~`, nunca na textura.

O prompt e o negative prompt da ficha A07 (`PROMPT_CATALOG.md`) serviram de brief e não foram enviados a nenhum gerador. Itens do negative prompt verificados: sem clones do personagem, texto, arco de arma, raio, parede de fumaça, fundo opaco, xadrez ou marca d'água.
