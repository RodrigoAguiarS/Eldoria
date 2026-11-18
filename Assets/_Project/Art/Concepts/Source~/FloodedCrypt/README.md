# A15 — Cripta inundada (conceito 2D de ambiente)

Recurso `REF-CONCEPT-12` · ficha A15 do `PROMPT_CATALOG.md` · condição do catálogo cumprida (F14 integrada: primeira
sala do segundo andar fixo, `Reports/F14/README.md`). Lote de 17/11/2026, branch `claude/a15-concept-crypt`.

**Entrega:** `Assets/_Project/Art/Concepts/FloodedCrypt_Concept_01.png`: PNG RGB 2048×1536 opaco, sem rótulos.
`.meta` copiado do `SkeletonMelee_Concept_01.png.meta` (Default, sRGB, NPOT None, sem mipmaps, máx. 2048), só com
GUID novo (`SHA256("Eldoria/A15/FloodedCrypt_Concept_01")`). É **ambiente proposto**, não nível aprovado: não há kit
3D, cena, collider, NavMesh, material nem shader de água. A F14 constrói primeiro a geometria jogável por builder.
Malhas, água, materiais, colliders e NavMesh ficam para integração posterior. A imagem não substitui o andar atual.

## Leitura da folha

| Região | Conteúdo |
|---|---|
| Topo (2048×1160) | sala na câmera de jogo (vista do sul, elevação 45°, **ortográfica**, 75 px/m), tochas e runas acesas, encontro F14 de escala: herói de capa azul, 2 esqueletos melee, 2 ranged |
| Faixa inferior (6 células) | estudo de peças a 35°, mesma escala (64 px/m), grade ciano de 2 m: piso úmido · canal reto · canto do canal · travessia da porta · pilar baixo · parede com nicho de runa |

- **Gabarito:** `Room_Normal`: interior 16×16 m, paredes de 1 m de espessura e 1,4 m de altura (a mesma
  `WallHeight` dos builders), vãos de 4 m ao norte e ao sul com batentes de 2,1 m, ritmo de 2 m nas paredes e nas placas.
- **Canais só nas bordas:** cada módulo de borda de 2×2 m tem 1 m de calçada úmida, um meio-fio claro (o limite
  do piso fica legível) e 1 m de canal raso. A água fica 0,20 m abaixo do piso, e a lâmina mede ~0,27 m até o fundo.
  Nos cantos o canal faz L. Duas bicas no norte alimentam a água.
- **Passarela seca:** a coluna x ∈ [−2, 2] usa lajes mais claras de porta a porta. Nos vãos o canal passa por baixo
  de uma laje seca (bueiro escuro). O piso seco central tem 12×12 m em placas de 2 m. Os **10×10 m centrais não
  têm volume nem água**.
- **Pilares baixos** (1,8 × 1,47 × 1,8 m) em (±6, ±6), nos cantos do piso seco, fora do quadrado 10×10.
- **Nichos de runa:** 4 (dois na parede norte, um a leste, um a oeste), com losango ciano discreto ao fundo.
  **Tochas:** 8 (#FF9A2E). **Musgo** contido nas capas, calçadas e pilares. Poças rasas só nas placas da borda.
- **Paleta da ficha:** pedra #55596B (±6 % nas placas, como GA_Stone_Floor_A/B/C), parede #3E4152, musgo #4C5A44,
  água #313E55 (azul-escuro dessaturado), tochas #FF9A2E, runas #4FD8FF, fendas/juntas escuras.

## Aceite (medido; `ConceptCrypt.validation.json`, 14/14)

Medido a partir do PNG, das máscaras do render e das **caixas por peça** do `FloodedCrypt.source.json`, que saem da
mesma geometria renderizada. Não é colisão nem navegação.

- **Formato:** 2048×1536 opaco, publicado em RGB; paleta da ficha nos materiais.
- **Água azul-escura dessaturada:** material #313E55 (matiz 218°, S 0,42, V 0,33). No render a média é
  rgb(43, 43, 64): a luz quente das tochas puxa um pouco para o violeta.
- **Gabarito:** faces internas a 7,93 m (o jitter dos blocos tira ~7 cm dos 8 m), externa a 9,07 m; vãos de
  3,97 m entre batentes; blocos de parede só de 2 m e cantos de 1 m.
- **Centro 10×10 livre:** 0 peças acima de 0,06 m e 0 peças de água no quadrado. A planta de cima tem 0 px de água
  nele. Na câmera de jogo o quadrado fica **100 % visível**, sem oclusão de paredes nem pilares.
- **Água só nas bordas:** as 114 peças de água ficam na faixa |x| ou |z| ≥ 7,17 m, com topo abaixo do meio-fio.
- **Rota seca porta-centro-porta:** nenhuma peça de água na faixa x ∈ [−2, 2] e 0 px de água na passarela na
  planta; lajes secas cobrem z ∈ [6, 9] e [−9, −6].
- **Leitura do piso:** o piso do centro fica em L* 35,9, rgb(76, 83, 117), perto do #55596B sob luz fria. Os
  personagens ficam a ΔE76 21,6 do piso.
- **Acentos contidos:** laranja de tocha em 0,13 % e ciano em 0,03 % da vista principal.
- **Estudo de peças:** as 6 peças ficam inteiras dentro das células.

`review_layout.png` mostra a planta de cima. Ela é evidência e não entra na folha: quadrado 10×10 tracejado em
laranja, passarela tracejada em branco e grade de 2 m em ciano.

## Como foi feito (reprodução)

Render 3D em tempo real, mesma técnica de A13/A14 (`PROVENANCE.md`), **não** IA de imagem:

```bash
cd Tools/Lemo3DGenerator
node src/production/ConceptCrypt.mjs           # GLB de render + FloodedCrypt.source.json (determinístico)
node preview/server.mjs --port 5179 --out "<projeto>/Assets/_Project/Art"
# no navegador, um por vez, esperando o título "DONE <modo>":
#   http://localhost:5179/concept_crypt.html?mode=sheet    (folha + máscaras de zona/água/personagens)
#   http://localhost:5179/concept_crypt.html?mode=layout   (planta de cima + água no centro/passarela)
#   (?mode=probe = quadro rápido da sala, não publicado)
python src/production/ConceptCrypt_check.py    # mede, valida e publica (PNG RGB + esta pasta)
```

- `ConceptCrypt.mjs` monta a sala e as 6 peças com os helpers do gerador (`bevelBox` + `jitter` determinístico).
  Cada módulo de borda, de parede, nicho, pilar e tocha é uma função em coordenadas locais. As malhas saem
  agrupadas por grupo/material/papel, e as caixas de cada peça da sala vão para o `source.json`.
- `preview/concept_crypt.html` carrega o GLB, posiciona as luzes a partir do `source.json` (tochas e nichos) e
  carrega os modelos do projeto (herói, melee, ranged) só para escala. Depois renderiza e compõe a folha e grava as
  máscaras usadas pelo validador.
- `FloodedCrypt_render.glb` (20 040 triângulos) é **só fonte de render**: sem FBX, fora da importação da Unity
  (pasta `Source~`). Não é kit modular nem define orçamento.
- Referencial: glTF do gerador (+Y para cima, +Z = norte). A sala é simétrica em X, então o espelhamento de eixo da
  importação na Unity não muda o layout.

## Limites e achados

- **NÃO VALIDADO NO EDITOR:** a importação do PNG/.meta não foi observada na Unity nesta sessão.
- **Divergência com a sala F14:** a ficha pede a sala normal 16×16 com centro 10×10, e a cripta segue a ficha. A
  sala de primitivas `Room_F14_Entry` tem 20 m e duas coberturas de 1,5 × 3 × 3 m em x = ±5. Elas ocupam
  x ∈ [4,25; 5,75] e entram no quadrado 10×10. Se a F14 adotar esta identidade, decidir lá se as coberturas ficam
  (como pilares baixos fora do centro) ou se a sala de 20 m ganha uma faixa de borda mais larga. O conceito não altera
  a sala atual.
- **Altura das paredes:** 1,4 m (builders), não os 2 m do texto histórico da direção de arte. Isso mantém a leitura
  na câmera 45°.
- **Câmera ortográfica:** "sem perspectiva dramática". A câmera real do jogo tem perspectiva, então a parede sul
  esconde um pouco mais do piso perto dela.
- **Água:** só aparência (cor, opacidade, traços claros). Não promete reflexo, ondas, interação, lentidão nem dano.
  Atravessar o canal é decisão de gameplay e de colisão para a F14.
- **Luz do render:** mais forte e neutra que a da Task 01 (lua #D8DFF2, hemisférica #B4BACE), para a pedra não
  virar azul-marinho. A primeira versão, com lua #9FB8FF fraca, deixava o piso em L* 17.
- Personagens só para escala; as posições não são spawn nem composição de encontro aprovada.
- Aprovação artística humana pendente.
