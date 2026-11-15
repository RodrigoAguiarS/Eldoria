# Relatório — Teste de sprites com Lemo-Opuscar (2026-11-13)

> Registro histórico da execução indicada. Afirmações de ausência de Git/arte/sistemas e pendências descrevem aquela data; consultar [PROJECT_AUDIT](../../PROJECT_AUDIT.md) para estado atual. Medições e limites abaixo foram preservados; não são testes reexecutados por esta revisão documental.

## 1. Resultado
Herói jogável (Asset 01) em pixel art: **40 frames 128×128** (Idle 4 + Walk 6, × Frente/Costas/Esquerda/Direita),
frames individuais, 3 spritesheets, JSON de metadados, sombra separada, prévias (HTML, APNG, prancha de contato,
silhueta) e exemplo Unity com SpriteRenderer + Animator.

## 2. Análise
**Direção de arte** (`ART_DIRECTION_AND_PROMPTS.md`): três assets — Herói (jogável, escolhido para o teste),
Esqueleto melee e Sala. Nenhuma imagem de conceito existe ainda (o documento diz isso); o sprite foi desenhado
a partir da descrição textual do Asset 01:
- espada curta; armadura de couro escuro; ombreiras e braçadeiras de aço; capa azul real em "V";
- cabeça ~1/3 da altura, mãos e botas robustas, formas redondas (herói) — medido: cabeça ≈ 36 de 110 px;
- paleta: capa #2F6BFF, couro #7A4B2A, metal #B9C4CF, latão #E0B040, pele #E3B58F, sombra #1E1830 (contorno e tinta),
  luz fria #9FB8FF/realce. A validação confirma que os hex oficiais aparecem nos pixels;
- silhueta legível a 128 px (`preview/hero/hero_silhouette.png`).

**Lemo-Opuscar**: biblioteca de *estilos de filme* feitos em código (canvas/WebGL renderizados quadro a quadro
para vídeo). O estilo `hd-2d` é o único com personagens em pixel art. O que ele realmente tem:
- `px.js`: canvas de pixels `PX` (set/rect/ellipse/poly/line/fill), `ramp()` com dither Bayer 4×4, contorno
  "selout" (`outline`), texturas procedurais e billboards three.js;
- `chars.js`: dois personagens do demo (Wren, guardiã do farol) desenhados procedimentalmente, frame 34×50 px,
  **uma única vista 3/4 olhando para a direita** (a esquerda é espelho por `scale.x = -1`), walk de 4 frames,
  idle de 2, poses como tabela de dados;
- `STYLE.md`: regras (sprites "em degraus" a 6–10 fps, dither ≈ .22 em sprites, tinta escura nunca preta,
  um único figurino saturado para o herói).

## 3. Como o Lemo foi reutilizado (sem modificação)
- `src/lemo-bridge.mjs` lê `px.js` do disco, troca **em memória** o `import three` por um stub e simula
  `document.createElement('canvas')`; assim `PX`, `ramp`, `hex`, `mixc`, `outline` rodam no Node. O SHA-256 do
  `px.js` usado vai para o JSON (`lemo.sha256`). Nada é escrito no repositório do Lemo
  (o `git status` dele só mostra pastas não rastreadas pré-existentes, de 06–11/11).
- Adaptações implementadas **no projeto experimental**, porque o Lemo não as tem:
  vistas Frente e Costas; lateral esquerda com a espada na mão correta; walk de 6 frames; idle de 4 frames
  com ação sobreposta; exportação PNG RGBA/APNG (encoder próprio com zlib); spritesheets + JSON; pivô fixo;
  validador; importador e Animator para Unity.

## 4. Validação executada (com evidência)
| Verificação | Como | Resultado |
|---|---|---|
| PNG com alpha real | `validate.mjs` decodifica cada arquivo: colorType 6 (RGBA), cantos alpha 0, sem semitransparência nos frames | OK |
| Dimensões uniformes | 40/40 frames 128×128; sheets conferidas célula a célula com os frames | OK |
| Sem cortes | margem mínima 6 px até a borda em todos os frames | OK |
| Ponto de apoio | sola na linha 120 (pivô 0.5, 0.0625) em todos os frames; Idle sem variação; nenhum pé abaixo da linha | OK |
| Consistência | volume opaco varia ≤ 6,9 % por direção; 41–47 cores compartilhadas por direção | OK |
| Inspeção visual | prancha de contato e tiras ×2 revisadas; prévia HTML animada aberta no navegador | OK |
| C# compila | `check-cs` com Roslyn do Unity 6000.6.9f1 e as 296 referências do csproj; depois compilado pelo próprio Editor | OK, 0 erros/avisos no Console |
| Importação Unity | `.meta` gerados: textureType Sprite, Point, sem mipmap, PPU 56, pivô custom 0.5/0.0625 | OK |
| Animator | menu executado: 8 clips, controller, prefab, cena; PlayMode da cena de demo, Console vazio, frames de Walk mudando entre capturas (`preview/hero/unity/`) | OK |

## 5. Problemas encontrados e soluções
1. **`px.js` depende de three.js e do DOM** → ponte com stub de THREE e canvas falso; falha explícita se o arquivo mudar.
2. **Lemo só desenha uma vista** → vistas Frente/Costas/Lateral escritas do zero com as primitivas do Lemo.
3. **Espelhar inverte a luz e troca a mão da espada** → `mirrorPX` + fator `L` mantém a luz no alto-esquerda;
   na vista Esquerda a espada fica na mão direita (lado oposto à câmera, parcialmente oculta).
4. **Pé avançado descia 2 px abaixo do pivô na vista frontal** (pego pela validação) → o pé de trás sobe; nenhum desce.
5. **Cor oficial do couro não aparecia** (rampa só escurecia) → rampa recentrada no #7A4B2A.
6. **Vista lateral fina e ombreira lendo como cachecol** → tronco mais largo, ombreira menor e mais baixa, perna/braço distantes mais escuros.
7. **Dither na pele parecia barba** → dither da pele reduzido (.12).
8. **Linha de comando longa no csc** → arquivo de resposta (`.rsp`).
9. **Prévia HTML sem imagens no painel do navegador** → spritesheets embutidas como data URI.

## 6. Limitações reais
- O Lemo **não é um gerador de sprites**: não tem exportação de PNG/spritesheet, metadados, múltiplas
  direções nem integração com engine. Reutilizável de fato: as primitivas de pixel, a rampa com dither, o
  contorno e as regras do estilo. Todo o resto é código deste protótipo.
- Desenho procedural: cada personagem novo exige escrever funções de desenho (≈ 250 linhas para o herói).
  Não há geração por IA nem a partir de imagem de conceito.
- Resolução efetiva 64×64 ampliada ×2 (pixels de 2×2): fiel ao grammar HD-2D (personagens de 30–60 px),
  mas com menos detalhe que um sprite nativo de 128 px. A respiração anda em passos de 2 px.
- Sem diagonais (4 direções). Para um ARPG isométrico com movimento livre, 8 direções seriam o próximo passo.
- Vista "Back": a capa esconde quase todo o corpo e as pernas — leitura correta para a capa em V, mas o walk
  de costas é transmitido sobretudo pelas botas e pelo balanço da capa.
- Iluminação "pintada": o sprite não recebe as luzes da cena como o modelo 3D. O HD-2D do Lemo resolve isso com
  billboards Lambert em three.js; no Unity seria preciso um material Sprite-Lit e ajuste de luzes (não testado).
- Cabelo castanho escuro (#4A2C1A) e ausência de capacete são **suposições**: a direção de arte não define.
- Ainda não existe imagem de conceito aprovada; quando existir, o sprite deve ser revisto contra ela.

## 7. Ocorrências no ambiente (não causadas pelo gerador)
- Às **02:53:36** `Assets/_Project/Prefabs/Rooms/Room_Normal.prefab` e `Assets/_Project/Scenes/Floor_01.unity`
  foram regravados juntos — exatamente o par que o menu existente **Build/Floor Scene (M2)** produz. Isso ocorreu
  ~1 min após o Unity recompilar com os novos scripts e **antes** de eu executar qualquer menu (02:54:26). Os
  scripts do Sprite Lab só escrevem em `Prototypes/` (ver `SpriteLabAnimatorBuilder`). Provável ação paralela
  (usuário ou outro agente; também há `Art/Concepts/AGENT_PROMPT_CHARACTER_ASSETS.md` editado às 02:32).
  O projeto não é repositório git, então não foi possível comparar o conteúdo — **recomenda-se conferir**.
- Para trazer o Unity à frente, uma segunda instância do Editor foi aberta por engano e encerrou sozinha
  (código 0); ela sobrescreveu `%LOCALAPPDATA%\Unity\Editor\Editor.log` da sessão em curso.
- Durante a verificação, a cena aberta foi trocada para `Hero_SpriteDemo` (para o PlayMode) e depois reaberta em
  `Floor_01`, sem alterações pendentes. A digitação no campo de busca do Unity usou a área de transferência.

## 8. Recomendação
Como **alternativa visual**, o resultado é viável e coerente com a paleta e as proporções oficiais, e o pipeline
(gerar → validar → importar → Animator) funciona de ponta a ponta. Antes de considerar o caminho 2D:
aprovar um conceito do herói, testar o sprite na câmera real do jogo ao lado do esqueleto e do cenário
low-poly (legibilidade e iluminação), e decidir entre 4 e 8 direções.
