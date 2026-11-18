# A14 — Sentinela de Pedra (conceito 2D)

Recurso `REF-CONCEPT-11` · ficha A14 do `PROMPT_CATALOG.md` · condição do catálogo cumprida (F15 prototipada
com primitivas, `docs/STONE-SENTINEL-F15.md`). Lote de 17/11/2026, branch `claude/a14-concept-sentinel`.

**Entrega:** `Assets/_Project/Art/Concepts/StoneSentinel_Concept_01.png`: PNG RGB 2048×1536, fundo cinza
neutro #2B2C31 com vinheta leve, sem rótulos. `.meta` copiado do `SkeletonMelee_Concept_01.png.meta` (Default,
sRGB, NPOT None, sem mipmaps, máx. 2048), só com GUID novo (`SHA256("Eldoria/A14/StoneSentinel_Concept_01")`).
É **conceito 2D**: não há FBX, rig, take, prefab nem sprite de gameplay. A F15 ainda não passou pelo playtest
humano, e o catálogo pede a F15 validada antes da arte final. Malha, rig Generic, takes e integração continuam
sendo tarefas separadas.

## Leitura da folha

| Região | Conteúdo |
|---|---|
| Linha 1 (200 px/m, chão na mesma linha) | frente · lado (perfil, olhando para a esquerda) com o herói de capa azul na mesma escala · costas, ortográficas, pose **Stand** |
| Linha 2, esquerda | vista isométrica fixa de jogo: elevação 45°, guinada −40° (a mesma das folhas da família), pose Stand |
| Linha 2, centro | pose **Windup** (antecipação do leque frontal) na mesma câmera 45°, sobre piso de placas de 2 m, com o setor do protótipo F15 (100°, 6 m: área escura + laranja crescendo até a borda) e o herói dentro dele |
| Linha 2, direita | silhuetas em preto sólido na 45°: Stand em cima, Windup embaixo |

- **Corpo:** guardião original de blocos chanfrados e rachados, 2,69 m de altura e 3,30 m de largura. Torso
  baixo e largo em trapézio (ventre em dois blocos, peito em três, canga de pedra por cima), cabeça pequena
  afundada à frente, abaixo da linha dos ombros, com uma fenda ciano; **sem elmo, chifres nem marreta**.
- **Braços em pilares:** pedra de ombro com musgo, tambor superior, junta escura, tambor inferior mais largo e
  uma base de pilar no lugar da mão, quase no chão. Folga visível entre braço e tronco em todas as vistas.
  Pernas curtas em blocos, pé em laje escura.
- **Núcleo:** fenda vertical ciano estreita (0,11 × 0,80 m) embutida 4 cm atrás de uma moldura escura de
  quatro peças, ecoando o "Core" vertical da primitiva F15. Runas só em dois traços: chevron curto na face
  externa de cada antebraço e um nas costas. Ciano = 0,36 % da silhueta frontal.
- **Costas:** crista dorsal de três pedras e uma runa, então a vista de trás não repete a frente.
- **Rachadura laranja (opcional):** um único fio fino que sai da moldura do núcleo no bloco esquerdo do peito.
  Pode ser retirada sem mudar nada da silhueta.
- **Windup:** os pés continuam plantados na direção da raiz (o setor da F15 é travado nessa direção). Os dois
  pilares se abrem para os lados e um pouco para trás, o tronco recua, a cabeça desce, e o núcleo e a rachadura
  acendem (emissão 2,6 → 5,5 e 1,2 → 3,2 no render). A pose é simétrica como o setor: o golpe que vem depois
  fecha os pilares pela frente. É proposta de leitura, não take.
- **Paleta da ficha** (= cores oficiais do ambiente): pedra #55596B, pedra escura #3E4152, musgo #4C5A44,
  runas e núcleo #4FD8FF, rachadura #FF9A2E, fendas #1E1830. Pedra fosca (smoothness 0,03–0,08, sem metal).

## Aceite (medido; `ConceptSentinel.validation.json`, 13/13)

- **Formato e fundo:** 2048×1536 opaco, publicado em RGB; croma ≤ 6 em 8 amostras (o próprio #2B2C31 tem croma 6).
- **Identidade entre vistas:** frente/lado/costas com 2,69 m e topo na mesma linha (y 126 px); frente e costas
  com 3,30 m. Mesma malha e pose por construção.
- **Escala:** o herói mede 1,97 m na vista lateral (Sentinela 1,37× mais alta). Porte comparado com a primitiva
  F15: altura 2,69 m (colisor 2,7 m), largura 3,30 m (primitiva 3,1 m; colisor 2,5 m), profundidade 1,62 m
  (colisor 1,4 m). `review_scale.png` mostra herói, Sentinela, Warden (4,6 m) e elite na câmera de jogo.
- **Silhueta a 128 px** (câmera 45°, `review_silhouette_128.png`; linha 1 normalizada a 128 px de altura, linha
  2 em escala relativa com o Warden Idle = 128 px; ordem: Sentinela Stand, Sentinela Windup, Warden Idle,
  Warden SlamWindup, Elite, Melee, Herói): IoU máximo **0,37 contra o Warden** (Idle 0,34, SlamWindup 0,37) e
  0,37 contra os demais. Proporção L/A 0,88, contra 0,47–0,62 dos outros. Stand × Windup: IoU 0,49, e na 45° o
  Windup fica 46 % mais largo.
- **Leitura sobre o chão:** no painel do leque o corpo inteiro (403×312 px) e o herói ficam dentro do quadro,
  sobre o piso e o setor. O setor usa só `radius`/`angle` de `Boss_StoneSentinel_Data`; não define área nova.

## Como foi feito (reprodução)

Render 3D em tempo real, mesma técnica das folhas da família em `PROVENANCE.md` (Task 01 e A13), **não** IA de imagem:

```bash
cd Tools/Lemo3DGenerator
node src/production/ConceptSentinel.mjs          # GLB de render + StoneSentinel.source.json (determinístico)
node preview/server.mjs --port 5179 --out "<projeto>/Assets/_Project/Art"
# no navegador, um por vez, esperando o título "DONE <modo>":
#   http://localhost:5179/concept_sentinel.html?mode=sheet
#   http://localhost:5179/concept_sentinel.html?mode=review
#   http://localhost:5179/concept_sentinel.html?mode=scale
#   (?mode=probe = quadro rápido das poses, não publicado)
python src/production/ConceptSentinel_check.py   # mede, valida e publica (PNG RGB + esta pasta)
```

- `ConceptSentinel.mjs` monta a Sentinela pelo `assemble()` do gerador: blocos `bevelBox` com `jitter`
  determinístico, presos rigidamente aos ossos do rig de pose (os nomes humanoides servem só de hierarquia),
  com proporções próprias (ombros em ±1,22 m, pernas curtas). As poses estáticas são `Stand`, `Windup` e `Bind`.
- `preview/concept_sentinel.html` carrega o GLB e os modelos do projeto (herói, Warden, elite, melee) só para
  escala, renderiza cada painel ortográfico com px/m fixo e compõe a folha. A luz é a da família (hemisférica,
  key fria seguindo a câmera, contorno #FF9A2E, sombra projetada), um pouco neutralizada (key #ECEEF6,
  hemisférica #AAB6D6) para a pedra #55596B não virar azul-marinho. O brilho vem só dos emissivos (blur 2D),
  sem bloom, para o cinza do fundo sair exato.
- `StoneSentinel_render.glb` (2424 triângulos) é **só fonte de render**: sem FBX, fora do catálogo do gerador e
  fora da importação da Unity (pasta `Source~`). Não serve como malha de produção nem define orçamento.

Arquivos: `StoneSentinel.source.json` (paleta, emissão, proporções, referência F15, poses), `sheet_measure.json`
(medidas por painel), `review_measure.json` (silhuetas; máscaras omitidas), `scale_measure.json`,
`review_silhouette_128.png`, `review_scale.png`, `ConceptSentinel.validation.json`.

## Limites e achados

- **NÃO VALIDADO NO EDITOR:** a importação do PNG/.meta não foi observada na Unity nesta sessão.
- **Condição F15:** o protótipo existe, mas os números e a leitura espacial ainda esperam playtest humano.
  O conceito não presume fase nova, dano, área ou padrão de ataque. O setor desenhado é o dado atual do protótipo.
- **Escala:** segue a primitiva F15 (2,7 m), então a Sentinela fica menor que o Warden (4,6 m). Se o Boss final
  precisar de presença maior, escalar o conjunto inteiro, sem alongar os membros. A largura de 3,3 m passa do
  colisor de 2,5 m da primitiva: alinhar colisor e malha quando houver malha.
- **Windup simétrico:** escolhido para a antecipação ler como o setor centrado. Se a varredura real for de um
  lado só, refazer só a pose, não o corpo. O rig futuro deve manter os pés plantados na direção travada.
- Na 45°, a Sentinela em pé lê como massa larga e baixa: os ombros cobrem a cabeça. Isso é intencional,
  porque a distinção do Warden vem dessa massa e da falta de chifres e arma.
- Aprovação artística humana pendente.
