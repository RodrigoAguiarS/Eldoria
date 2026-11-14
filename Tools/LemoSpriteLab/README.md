# LemoSpriteLab — gerador de sprites 2D (prova de conceito)

Gera sprites animados em pixel art para o Dungeon Crawler usando o **motor de pixel do estilo HD-2D do
Lemo-Opuscar** (`styles/hd-2d/demo/px.js`), carregado **somente leitura** a partir de
`G:\lemo-opuscar\lemo-opuscar`. É uma alternativa visual para avaliação: **não substitui** a direção de arte
oficial (low-poly 3D), modelos, prefabs ou cenas do jogo.

## Requisitos
- Node 20+ (testado com 24.18). Sem dependências npm.
- Para `check-cs`: Unity 6000.6.9f1 instalado (usa o Roslyn embutido no Editor).
- Variável opcional `LEMO_ROOT` se o Lemo estiver em outro caminho.

## Comandos (rodar dentro de `Tools/LemoSpriteLab`)
| Comando | O que faz |
|---|---|
| `npm run generate` | Desenha os 40 frames, monta spritesheets, JSON, sombra e prévias |
| `npm run validate` | Relê os PNGs do disco e valida alpha, tamanho, cortes, pivô, paleta e sheets |
| `npm run check-cs` | Compila os scripts C# fora do Unity com as referências do projeto |
| `npm run install-unity` | Copia os scripts C# + asmdefs para `Prototypes/_SpriteLab` |
| `npm run all` | Tudo acima, nesta ordem segura (compila → instala → gera → valida) |

Opções do gerador: `node src/generate.mjs --character hero --out <pasta> --preview <pasta>` (use `--out` para
um diretório temporário enquanto itera, sem disparar importações no Unity aberto).

## Saídas
```
Assets/_Project/Art/Sprites/Prototypes/
  _SpriteLab/Runtime   SpriteLabAnimatorDriver.cs + DungeonCrawler.SpriteLab.asmdef
  _SpriteLab/Editor    importador automático, construtor da demo, manifesto + asmdef (somente Editor)
  Hero/
    Hero_Sprites.json             metadados (frames, fps, direções, rects, PPU, pivô, paleta, proveniência)
    Hero_Shadow.png               sombra de contato separada
    Idle/<Front|Back|Left|Right>/Hero_Idle_<Dir>_00..03.png
    Walk/<Front|Back|Left|Right>/Hero_Walk_<Dir>_00..05.png
    Sheets/Hero_Idle_Sheet.png    512×512  (linhas = direções, colunas = frames)
    Sheets/Hero_Walk_Sheet.png    768×512
    Sheets/Hero_Spritesheet.png   768×1024 (linhas = animação × direção)
    Unity/                        gerado pelo menu do Unity: clips, controller, prefab e cena de demo
Tools/LemoSpriteLab/preview/hero/
  index.html                      prévia animada (abre com duplo clique)
  hero_<Anim>_<Dir>.apng          prévias animadas por direção
  hero_contact_sheet.png          todos os frames com guia de chão (laranja) e eixo central
  hero_silhouette.png             teste de silhueta em preto (regra da direção de arte)
  zoom/*.png                      tiras ampliadas ×2 para inspeção
  unity/playmode_capture_*.jpg    capturas do PlayMode no Unity
```

## Unity
1. Os PNGs são configurados **na primeira importação** por `SpriteLabTextureImporter` (só dentro de
   `Prototypes/`): Sprite Single, Point, sem compressão, sem mipmaps, PPU e pivô (sola dos pés) lidos do JSON.
   Ajustes manuais posteriores são preservados. Para forçar de novo:
   **Tools › Dungeon Crawler › Sprite Lab › Reapply Prototype Import Settings**.
2. **Tools › Dungeon Crawler › Sprite Lab › Build Hero 2D Animator Demo** cria (idempotente) em `Hero/Unity/`:
   8 AnimationClips, `Hero_Sprite.controller` (parâmetros `Direction` int 0..3 = Front/Back/Left/Right e
   `Moving` bool), `Hero_SpriteDemo.prefab` (SpriteRenderer + Animator + `SpriteLabAnimatorDriver`, sprite
   inclinado 45° para a câmera isométrica do jogo, sombra deitada no chão) e `Hero_SpriteDemo.unity`.
   A cena é criada de forma aditiva e fechada: a cena aberta não é trocada.
3. Abra `Hero_SpriteDemo.unity` e dê Play: linha de cima = Idle, linha de baixo = Walk, nas 4 direções.

Para ligar a um personagem real no futuro: chamar `driver.Set(SpriteLabAnimatorDriver.FacingFromScreenDirection(dirTela), velocidade > 0)`.
Isso **não foi** integrado ao Player — fora do escopo do teste.

## Criar um novo personagem
1. Copie `src/characters/hero.mjs` para `src/characters/<id>.mjs`.
2. Ajuste `meta` (`id`, `name`, `heightMeters`, `footRow`), `PALETTE` (hex oficiais do
   `ART_DIRECTION_AND_PROMPTS.md`) e as funções de desenho. Mantenha a convenção: grade lógica 64×64,
   sola na linha 59, luz da tela vinda do alto-esquerda, `L = -1` nas vistas espelhadas.
3. Poses são dados: edite `ANIMATIONS` (cada frame é um objeto de parâmetros; nova pose = nova linha).
   Novas animações (ex.: `Attack`) entram automaticamente nas sheets, no JSON e nos clips.
4. Exporte `createHero` ou `create(lemo)` retornando `{ meta, PALETTE, ANIMATIONS, DIRECTIONS, drawFrame, outlineTint, outlineK }`.
5. `node src/generate.mjs --character <id>` → `node src/validate.mjs --json <...>/<Nome>/<Nome>_Sprites.json`.
6. No Unity, duplique o `MenuItem` de `SpriteLabAnimatorBuilder` (ou chame `Build("<Nome>", true)`).

Itere com `--out` num diretório temporário e só exporte para `Assets/` quando o resultado estiver aprovado.
