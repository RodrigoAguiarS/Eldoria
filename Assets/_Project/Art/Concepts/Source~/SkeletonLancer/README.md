# A13 — Lanceiro corrompido (conceito 2D)

Recurso `REF-CONCEPT-10` · ficha A13 do `PROMPT_CATALOG.md` · condição F13 cumprida (protótipo de primitivas
em `docs/LANCER-F13.md`). Lote de 17/11/2026, branch `claude/a13-concept-lancer`.

**Entrega:** `Assets/_Project/Art/Concepts/SkeletonLancer_Concept_01.png`: PNG RGB 2048×1536, fundo cinza
neutro #2B2C31 com vinheta leve, sem rótulos. `.meta` copiado do `SkeletonMelee_Concept_01.png.meta` (Default,
sRGB, NPOT None, sem mipmaps, máx. 2048), só com GUID novo (`SHA256("Eldoria/A13/SkeletonLancer_Concept_01")`).
É **conceito 2D**: não há FBX, rig, clip, prefab nem sprite de gameplay. Modelagem, rig Generic, clips e integração
continuam sendo tarefas separadas.

## Leitura da folha

| Região | Conteúdo |
|---|---|
| Linha 1 (380 px/m, chão na mesma linha) | frente · lado (perfil, olhando para a esquerda) · costas, ortográficas, pose **Guarda** |
| Linha 2, esquerda | vista isométrica fixa de jogo: elevação 45°, guinada −40° (a mesma das folhas da família), pose Guarda |
| Linha 2, centro | pose **Windup** (lado, 300 px/m): o compromisso da investida reta |
| Linha 2, direita | silhuetas em preto sólido na mesma vista 45°: Guarda e Windup |

- **Guarda** é a postura de aviso durante o telegraph de ≥ 1 s: base larga (pé esquerdo à frente, pé direito
  atrás, os dois no chão), tronco girado ~34° para a direita deixando o ombro blindado à frente, lança grossa
  nivelada com as duas mãos, inclinada 10° para cima e desviada 3,5° da linha do avanço.
- **Windup** usa o mesmo corpo e a mesma escala: 9 % mais baixo, tronco carregado sobre a perna da frente,
  lança recolhida 0,43 m junto ao corpo (ponteira saindo atrás) e ainda na linha reta (desvio de 12°).
- **Identidade com a família:** mesmo `skull()` e `skeletonBody()` do `_skeleton_kit.mjs` (crânio angular
  oversized, costelas, saiote de tiras), mesmas proporções `SKELETON_P` (2 m, cabeça ~1/3), mesmos materiais.
  Paleta da ficha: osso #CFC6A8, ferro #3E4152, ferrugem #8A4A22, olhos #FF2A2A (únicos emissivos).
- **Traços próprios:** ombreira assimétrica só no ombro esquerdo (três lâminas em degrau, aba alta junto ao
  pescoço, um espinho curto voltado para trás, correia diagonal), ombro direito nu; braçadeiras e grevas nos dois
  lados; tassetes largos; lança curta e grossa (1,28 m, haste de 7 cm, ponta em folha de 15 cm com farpas).

## Aceite (medido; `ConceptLancer.validation.json`, 13/13)

- **Identidade entre vistas:** frente/lado/costas com alturas 1,763/1,761/1,763 m e topo do crânio na mesma linha
  (y 164 px); frente e costas com a mesma largura (0,716 m). Mesma malha e pose por construção.
- **Silhueta a 128 px** (câmera 45°, `review_silhouette_128.png`, linha 1 normalizada a 128 px de altura, linha 2
  em escala relativa com o melee Idle = 128 px; ordem: Lanceiro Guarda, Lanceiro Windup, Melee Idle, Melee Attack,
  Elite Idle, Ranged Idle, Hero Idle): IoU máximo 0,54 contra a família (melee 0,47, ataque do melee 0,39, elite
  0,50, ranged 0,49, herói 0,54); proporção L/A 0,91 contra 0,47–0,52 dos outros em idle. Guarda × Windup: IoU 0,49.
- **A arma não define hitbox:** o contato da F13 é a distância plana do segmento percorrido até o centro do alvo
  (`hitRadius` 0,75 m, `EnemyLungeAttack.cs`). A ponta fica a 0,92 m do pivô na guarda e 0,68 m no windup. O
  excesso visual de +0,17 m no fim da faixa é menor que o raio do jogador (0,5 m), então a ponta nunca "acerta"
  onde o dano não acerta por uma margem visível. A lança foi encurtada de 1,42 m para 1,28 m por causa disso.
- **Escala:** `review_scale.png` mostra herói, lanceiro (guarda, 1,76 m de altura agachado), melee e elite 1,4× na
  mesma câmera.
- Fundo neutro (croma ≤ 6 em 7 amostras; o próprio #2B2C31 tem croma 6), PNG opaco 2048×1536, olhos visíveis.

## Como foi feito (reprodução)

Render 3D em tempo real, mesma técnica das folhas da família em `PROVENANCE.md` (Task 01), **não** IA de imagem:

```bash
cd Tools/Lemo3DGenerator
node src/production/ConceptLancer.mjs          # GLB de render + SkeletonLancer.source.json (determinístico)
node preview/server.mjs --port 5179 --out "<projeto>/Assets/_Project/Art"
# no navegador, um por vez, esperando o título "DONE <modo>":
#   http://localhost:5179/concept_lancer.html?mode=sheet
#   http://localhost:5179/concept_lancer.html?mode=review
#   http://localhost:5179/concept_lancer.html?mode=scale
python src/production/ConceptLancer_check.py   # mede, valida e publica (PNG RGB + esta pasta)
```

- `ConceptLancer.mjs` monta o lanceiro pelo `assemble()` do gerador (kit da família + lança, ombreira, tassetes)
  com três poses estáticas (`Guard`, `Windup`, `Bind`) somadas ao `HUNCH` da família. Os ângulos dos braços e da
  perna de trás foram resolvidos numericamente por `ConceptLancer_grip.mjs` (descida por coordenadas sobre a
  hierarquia do rig) para as duas
  mãos alcançarem pontos da mesma haste (erro ≤ 3 cm na guarda e ≤ 8 cm no windup) e os dois pés tocarem o chão.
- `preview/concept_lancer.html` carrega o GLB, aplica a pose, orienta a lança da mão direita (trás) para a esquerda
  (frente), renderiza cada painel ortográfico com px/m fixo e compõe a folha. Luz da família: hemisférica
  #9FB8FF/#1E1830, key fria seguindo a câmera, contorno #FF9A2E, sombra projetada. Brilho só dos materiais
  emissivos (blur 2D), sem bloom no fundo, para o cinza neutro sair exato.
- `SkeletonLancer_render.glb` (2130 triângulos) é **só fonte de render**: sem FBX, fora do catálogo do gerador e
  fora da importação da Unity (pasta `Source~`). Não serve como malha de produção nem define orçamento.

Arquivos: `SkeletonLancer.source.json` (paleta, lança, poses), `sheet_measure.json` (medidas por painel e
pegada), `review_measure.json` (silhuetas; máscaras omitidas), `review_silhouette_128.png`, `review_scale.png`,
`ConceptLancer.validation.json`.

## Limites e achados

- **NÃO VALIDADO NO EDITOR:** importação do PNG/.meta não observada na Unity nesta sessão.
- Vista de frente: a lança aponta para o observador e quase some (ortográfica honesta); a leitura da arma está
  no perfil, na 45° e nas silhuetas.
- Na 45° de jogo a lança aponta para baixo e à direita, como a espada do melee e do herói nessa guinada. A distinção
  a 128 px vem da base larga e da massa agachada (L/A 0,91), não da arma.
- O windup desvia 12° da linha reta porque o braço curto da família não alcança mais atrás. Um rig real deve
  travar a lança na direção da faixa (`EnemyLungeAttack` publica `TelegraphStarted`/`DashStarted`/`TargetHit`).
- Para o 3D futuro: manter a ponta perto de 0,75–0,95 m à frente do pivô na guarda, ou alongar o desenho da faixa
  pelo alcance visual. O dano continua no `TargetHit`, sem AnimationEvent.
- Aprovação artística humana pendente. Valores de gameplay da F13 continuam sendo propostas de playtest.
