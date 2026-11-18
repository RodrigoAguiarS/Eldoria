# Como ver e testar a versão atual

Use só estes caminhos. Eles sempre abrem as cenas oficiais do jogo
(`MainMenu` → `Floor_01_GeneratedCharacters`), onde ficam itens, ícones, equipamento e efeitos de golpe.

## No Unity (menu **Tools › Dungeon Crawler**, primeiras opções)

| Opção | O que faz |
| --- | --- |
| **▶ 1. Jogar versão atual (menu principal)** | Abre o menu principal e dá Play. É o jogo como o jogador vê. |
| **▶ 2. Testar itens e combate (já com itens)** | Dá Play direto no andar e entrega 3 espadas, colete e poções, equipa a Lâmina mais forte e o colete e põe um pickup de cada tipo à sua frente (espadas, colete, poção, ouro, XP). |
| **▶ 3. Gerar build Windows e abrir** | Gera a build oficial (`Builds/WindowsMVP/Eldoria.exe`) e abre o jogo. |
| **▶ 4. Gerar build Windows e abrir já com itens** | Igual ao 3, mas o jogo abre direto no andar com os itens de teste. |
| **▶ 5. Abrir última build (sem gerar)** | Abre o .exe já existente. |

Clique de novo em ▶ 1 ou ▶ 2 durante o Play para parar.

## Sem abrir o Unity (duplo clique na pasta do projeto)

- `Jogar-Build.bat`: abre a última build no menu principal.
- `Testar-Itens-Build.bat`: abre a última build direto no andar, com os itens de teste.

Os `.bat` usam a última build gerada. Depois de mudar algo no Unity, gere de novo com ▶ 3.

## O que conferir

- **Itens no chão:** cada um tem modelo próprio (3 espadas, colete, frasco, moedas, orbe de XP).
- **Seis slots (D-006):** os itens de teste também trazem elmo, luvas, botas e amuleto, já equipados, e uma segunda fileira de pickups dessas peças (ainda sem ícone/modelo próprio: aparecem com o pickup tingido padrão).
- **Inventário (I ou Tab):** ícones nos slots, quantidade (×N), "Cura N" nas poções, moldura de seleção e ícone nos detalhes.
- **HUD de poções** (canto superior esquerdo): ícone, Q, total e estado. Q bebe uma poção.
- **Personagem:** a espada equipada aparece na mão e o colete no corpo. Desequipar volta à espada básica.
- **Golpes** (botão esquerdo): rastro na espada nos 3 golpes, faíscas só quando acerta. Giro (E ou botão direito): anel ciano. Não deve aparecer retângulo branco.

- **Baús:** um em cada uma das cinco salas. F próximo abre após limpar a própria sala; E continua giro. Na vitória, **Coletar recompensas** volta à sala para abrir o baú do Boss e usar a bolsa. Uma coleta recusada fica no chão.
- **Tochas:** chamas nas tochas das paredes, sem tochas soltas no centro das salas. O rebuild da arena M1 deve preservar as salas do andar.

## O que NÃO usar para ver o jogo

- **Tools › Dungeon Crawler › Sprite Lab › Build Hero Playtest...** não gera o jogo. Esse menu cria um
  **protótipo do herói 2D** (`Art/Sprites/Prototypes/Hero/Unity/Hero_Playtest_*.unity`) a partir da cena antiga
  `Floor_01`, sem itens, ícones nem efeitos. É por isso que aparecia a imagem 2D.
- **Play com uma cena qualquer aberta:** o Play do Unity roda a cena que estiver aberta. Use ▶ 1 ou ▶ 2.
- Os itens de teste são um atalho e só aparecem com ▶ 2/▶ 4 ou `Testar-Itens-Build.bat`. No jogo normal, os itens vêm dos drops dos inimigos.
