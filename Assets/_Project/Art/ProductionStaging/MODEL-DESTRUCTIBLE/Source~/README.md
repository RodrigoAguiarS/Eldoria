# A12 — MODEL-DESTRUCTIBLE (vaso de barro e caixa de madeira, intactos e quebrados)

Quatro **modelos 3D estáticos reais** para substituir só o visual do vaso destrutível F09 de primitivas (`DestructibleVase_F09`: `Intact/Body` esfera + `Intact/Rim` cilindro, `Rubble/Shard_0..2` cubos, `Mat_Vase_F09`). Duas famílias da ficha A12: um **vaso de barro baixo** e uma **caixa de madeira reforçada**, cada uma com variante **intacta** e **quebrada**. Formas grossas, rachaduras discretas, sem gore. Não há física, collider, loot, burst, animação nem marca de recompensa.

A condição da ficha ("F09 validado com primitivas antes") está cumprida: F09 está integrado na `main` (PR #8; `Reports/F09/README.md`: 163 EditMode, 146 PlayMode, captura gráfica 1/1). Nada do domínio foi alterado: `DestructibleProp.cs`, `DestructiblePropPresenter.cs`, `DestructiblePropDefinition.cs`, `DestructiblePropSceneBuilder.cs`, `Prop_Vase_F09.asset`, `Mat_Vase_F09`, cenas e prefabs só foram lidos. A integração é uma tarefa Codex separada.

| Item | Valor |
| --- | --- |
| Arquivos | `../MODEL-DESTRUCTIBLE_Jar_Intact.fbx`, `_Jar_Broken.fbx`, `_Crate_Intact.fbx`, `_Crate_Broken.fbx`: FBX 7.4 ASCII estáticos (sem Takes) |
| Triângulos (alvo ≤ 600 por prop) | vaso intacto **344**, vaso quebrado **344**, caixa intacta **360**, caixa quebrada **180** |
| Nós (nomes da ficha) | `Jar_Intact` (um nó); `Jar_Broken_Base`, `Jar_Broken_Rim`, `Jar_Broken_Shard_0..3`; `Crate_Intact` (um nó); `Crate_Broken_Base`, `Crate_Broken_Plank_0..1`. Todos são Mesh filhos da raiz. O intacto é **um único nó** de propósito: o `DamageFlash` mira um só renderer |
| Dimensões (L × A × P) | vaso intacto 0,780 × 0,680 × 0,743 m; quebrado 0,766 × 0,431 × 0,719. Caixa intacta 0,720 × 0,620 × 0,720; quebrada 0,700 × 0,380 × 0,710 |
| Pivôs / eixos | Metros, +Y para cima, **+Z frente**. Origem de cada FBX = **base central do prop** (fundo em y 0, centro XZ ≤ 1 mm). `*_Intact` e `*_Broken_Base` na origem; cada fragmento com pivô na base central da própria peça, sem rotação nem escala |
| Footprint | As quatro cabem no vaso F09 atual (±0,45 m em X/Z, 1,04 m de altura). A quebrada nunca sai dos bounds X/Z nem da altura da intacta da mesma família (**bounds estáveis**, mesmo pivô) |
| Material | **Um**: `MODEL-DESTRUCTIBLE_Prop` (URP/Lit fosco, metallic 0, smoothness 0,15), com o mesmo nome nos quatro FBX. `../MODEL-DESTRUCTIBLE_Palette.png` (16×16, células 4×4 px) como BaseMap; cada face aponta para o centro de uma célula |
| Paleta | barro `#7B4E32` (ferrugem oficial `#8A4A22` dessaturada 30%) · barro claro `#915C3B` · barro escuro `#593824` · interior `#2A1D16` · rachadura `#3A2519` · madeira `#7A4B2A` (oficial) · madeira escura `#53331D` · madeira partida `#9B7048` · musgo `#4C5A44` (oficial). Derivadas registradas em `MODEL-DESTRUCTIBLE.source.json` |
| Revisão | `MODEL-DESTRUCTIBLE.glb` (glTF 2.0, as quatro variantes lado a lado em X a cada 1,1 m, cor por vértice; não é importado pela Unity, serve para DCC/revisão) |
| Fonte editável | `Tools/Lemo3DGenerator/src/production/ModelDestructible.mjs`: `FOOTPRINT_F09`, `SHAPE.jar` (anéis do perfil, boca, rachaduras, musgo, corte irregular, vão, aro caído, cacos), `SHAPE.crate` (montantes, tábuas, tampo, diagonais, rachadura, musgo, montantes partidos, tábuas caídas), `PALETTE`, `MATERIAL` |
| Validador + previews | `Tools/Lemo3DGenerator/src/production/ModelDestructible_preview.py`: relê FBX/PNG **do disco** (sem three.js), valida 16 itens, mede a leitura a 128 px e gera as pranchas |
| Ferramenta | Node 24 + three.js r170 do Lemo-Opuscar (somente leitura: `ConvexGeometry`, `GLTFExporter`, `GLTFLoader`) e o vocabulário low-poly do `Lemo3DGenerator` (`geo.mjs`). Python 3.11 + NumPy + Pillow no validador. Blender não está instalado; o "DCC" é o gerador por código, como em A08/A11 |
| Determinismo | Duas execuções seguidas dão os mesmos SHA256 em FBX, `.meta`, PNG, GLB, `source.json` e `generator.json`. Hashes em `ModelDestructible.validation.json` (FBX ASCII gerado com LF; com `core.autocrlf` o checkout pode virar CRLF, então regenere para comparar) |

## Reproduzir

```bash
cd Tools/Lemo3DGenerator
node src/production/ModelDestructible.mjs           # malhas, 4 FBX, GLB, paleta, .meta, source/generator JSON
node src/production/ModelDestructible.mjs --check   # só valida em memória
cd ../..
python -I Tools/Lemo3DGenerator/src/production/ModelDestructible_preview.py   # valida do disco + previews
python -I .claude/skills/create-game-assets/scripts/asset_report.py --expect-size 16x16 --max-colors 16 --json Assets/_Project/Art/ProductionStaging/MODEL-DESTRUCTIBLE/MODEL-DESTRUCTIBLE_Palette.png
```

As `.meta` têm **GUIDs estáveis** (SHA256 de `Eldoria/A12/<nome>`):

- **FBX:** ModelImporter de `Art/Items/Models/Loot_XpOrb.fbx.meta` (Unity 6000.6, serializedVersion 24600) mudando **só o GUID**: estático (`importAnimation 0`, `animationType 0`), escala 1, `useFileUnits`, sem colliders, normais importadas, `materialImportMode 2`. Com `materialLocation 1`, a Unity cria uma cópia embutida do material em cada FBX; o remapeamento para um `.mat` único fica para a integração.
- **Paleta:** template de `FX_Spark` com point, sem mipmaps, sem compressão, sem alpha, sRGB, tamanho máximo 32.

O validador sai com código 1 se alguma verificação falhar.

## Direção visual

- **Referências reais:** captura `Reports/F09/Visual02/vase-intact.png` (vaso laranja de primitivas ao lado do Hero, ~111 px/m em 2560×1440); geometria do vaso em `DestructiblePropSceneBuilder.ConfigureRelease`; paleta oficial da direção de arte (ferrugem `#8A4A22`, couro/madeira `#7A4B2A`, musgo `#4C5A44`, pisos `#55596B`/`#3E4152`); vocabulário de A11 (hulls convexos facetados, paleta por célula); câmera do jogo com pitch 45° em −Z (`DungeonMvpSceneBuilder.CameraOffset (0, 14, −14)`).
- **Vaso intacto:** decágono com uma face plana na frente. Pé escuro, barriga larga (0,78 m), ombro claro (gradiente pintado), gargalo estreito, aro claro com **boca escura** (disco de 0,12 m). Duas rachaduras finas em zigue-zague (lascas escuras rentes à parede, 15 mm de largura) na frente e na frente-esquerda. Musgo no pé, à esquerda.
- **Vaso quebrado:** pé e base maciça até y 0,14 (topo escuro = fundo interno). Por cima, nove placas finas de parede (3,5 cm) partidas em alturas irregulares (0,18–0,34 m), com topo claro (barro exposto) e **vão na frente**, que mostra o interior para a câmera. O aro com gargalo caiu dentro do corpo, inclinado com a boca para a frente. Quatro cacos planos ficam nos cantos livres do quadrado do footprint (fora do círculo do vaso). Mesmo musgo do intacto.
- **Caixa intacta:** núcleo escuro (visível nas frestas), quatro montantes escuros, três tábuas por lado, tampo de três tábuas entre duas travessas, uma diagonal escura por lado (sentidos alternados). Uma tábua da frente rachada; musgo no pé da frente-esquerda.
- **Caixa quebrada:** fundo escuro, montantes partidos (0,16–0,38 m) com topo inclinado de madeira clara, primeira fileira de tábuas inteira, tocos da segunda fileira atrás e à esquerda com ponta clara, diagonal de trás encurtada e duas tábuas caídas para dentro. Mesmo musgo.
- **Sem acentos de perigo ou loot:** nada vermelho, emissivo, dourado ou com símbolo. A única cor clara é a madeira/barro partido, que sinaliza o estado quebrado.

## Leitura (validador, câmera do jogo, prop girado 180° para a frente encarar a câmera, rumos −30…+30°)

Tiles de **128 px reais**, a 120 px/m. A caixa intacta, a maior projeção a 45°, ocupa ~114 px de altura.

| Dupla | IoU silhueta (máx) | Razão de área (máx) | Diferença de aparência (mín) |
| --- | --- | --- | --- |
| vaso intacto × caixa intacta | **0,668** | 0,686 | 1,000 |
| vaso quebrado × caixa quebrada | 0,727 | 0,752 | 0,848 |
| vaso intacto × vaso quebrado | 0,756 | **0,843** | **0,801** |
| caixa intacta × caixa quebrada | 0,740 | 0,740 | 0,725 |

- **Critério:** IoU ≤ 0,8 em todas as duplas. Intacto e quebrado dividem o footprint por exigência da ficha ("bounds estáveis", "sem aumentar footprint"), então a silhueta a 45° fica parecida por construção. Para essas duplas, o validador exige também razão de área ≤ 0,85 (o quebrado é mais baixo) e diferença de aparência ≥ 0,4: fração da união em que a célula da paleta muda (interior escuro, madeira partida, cacos). O vaso fica perto do limite de área (0,843); a leitura dele vem principalmente da aparência (0,801).
- **Interior escuro visível:** vaso quebrado ≥ 47,9%, caixa quebrada ≥ 18,1%, contra 6,9% no vaso intacto (só a boca) e 0% na caixa intacta.
- **Contra o piso:** ΔE76 da cor média contra os pisos `#55596B`/`#3E4152` fica entre 30,2 e 37,5 (barro e madeira quentes sobre pedra fria). O contraste WCAG de luminância é baixo (1,01–1,51): a separação vem do matiz e do sombreamento, não do brilho. Ver limitações.

## Aceite da ficha

| Critério | Evidência |
| --- | --- |
| Formas distintas a 128 px | `formas_distintas_128px` (tabela acima) + `preview_128.png` (128 px reais, cor e silhueta) |
| Bounds estáveis entre variantes | `bounds_estaveis`: vaso quebrado X [−0,390; 0,376] Z [−0,367; 0,351] A 0,431 ⊆ intacto X ±0,390, Z [−0,371; 0,372], A 0,680; caixa quebrada X ±0,350, Z [−0,360; 0,350], A 0,380 ⊆ intacta ±0,360, A 0,620 |
| Sem aumentar footprint | `footprint_f09`: as quatro dentro de ±0,45 m / 1,04 m (vaso F09 atual); vaso 0,78 m e caixa 0,72 m de largura |
| ≤ 600 tris/prop, 1 material, nomes intact/broken, pivô no chão | `triangulos`, `material_unico`, `objetos_nomes`, `pivos`, `unidade_eixos` (16/16 em `ModelDestructible.validation.json`) |
| Previews frente/lado/45° | `preview_ortho.png` (frente, lado, topo e 45°, quatro variantes) |
| F09 validado com primitivas antes | F09 integrado (PR #8; `Reports/F09/README.md`, `GAME_DESIGN.md` F09, `MVP.md`) |

## Pranchas (todas rotuladas PREVIEW)

Rasterizador próprio em NumPy, ortográfico, luz ambiente + difusa. Não são captura da Unity.

- `preview_ortho.png`: frente, lado, topo e câmera do jogo 45°, nas quatro variantes.
- `preview_128.png`: as quatro em 128 px reais pela câmera do jogo (cor sobre o piso e silhueta).
- `review_f09_scale.png`: as quatro em escala (~111 px/m) sobre a captura real `Reports/F09/Visual02/vase-intact.png`, ao lado do vaso de primitivas e do Hero.
- `contact_sheet.png`: 45° das quatro + paleta.

## Integração proposta (tarefa Codex separada)

Detalhes em `MODEL-DESTRUCTIBLE.source.json` → `integration_proposal`.

- **Troca de estado:** `DestructiblePropPresenter` já alterna os GameObjects `intact` e `rubble`. Instanciar `*_Intact.fbx` como filho de `Intact` e `*_Broken.fbx` como filho de `Rubble`, por builder idempotente (`DestructiblePropSceneBuilder` ou variante nova). Nenhum código de domínio muda.
- **Flash:** apontar `DamageFlash.targetRenderer` para o MeshRenderer do nó `Jar_Intact` (ou `Crate_Intact`). O tint em `_BaseColor` via MaterialPropertyBlock multiplica a paleta.
- **Colliders/layer:** continuam do domínio. Sugestão: manter os colliders atuais do vaso (Sphere do `Body` + Capsule do `Rim`) como volumes de acerto e desligar só os MeshRenderers de `Body`/`Rim`/`Shard_*`; o vaso A12 cabe dentro deles. Para a caixa, a tarefa de domínio define um BoxCollider de ~0,70 × 0,62 × 0,70 m. A layer Destructible fica fora da NavMesh, da visão e dos projéteis, como hoje.
- **Colocação:** footprint ≤ o do vaso F09, então a posição lateral atual (sala + (−5,5; 0; −2)) e as distâncias de portas, spawns e centro continuam válidas. Girar Y 180° deixa a frente (rachaduras, musgo, vão do vaso quebrado) para a câmera.
- **Família por sala:** vaso, caixa ou alternado é decisão de design da integração. Ambos usam a mesma definição de 40 HP, sem regra nova. Não registrar como inimigo da sala nem participar da limpeza.
- **Material:** um `.mat` URP/Lit `MODEL-DESTRUCTIBLE_Prop` (metallic 0, smoothness 0,15, `_BaseMap` = paleta) compartilhado pelos quatro FBX, por Search and Remap ou `externalObjects` via builder. Não reutilizar `Mat_Vase_F09`, que é cor sólida sem paleta.
- **Burst:** o F09 atual não tem burst (troca estática). Se for adicionado depois, reutilizar um efeito existente disparado por `Broken`; fora deste lote.
- **Testes:** quebra única Intact → Rubble sem dano ou drops repetidos; flash visível no renderer A12 respeitando a opção de flash; desativar/reativar mantém o estado e reiniciar volta intacto; NavMesh e portas inalterados; leitura nas cinco salas, ao lado do Hero.

## Divergência registrada

A ficha do manifesto `MODEL-DESTRUCTIBLE` (auditoria anterior) propunha "bloco de alvenaria 1×1×1 m + três fragmentos, ≤ 600 tris no total, fonte .blend". Este lote segue a ficha **A12 do `PROMPT_CATALOG.md`** (vaso de barro + caixa de madeira, intacto/quebrado, ≤ 600 tris por prop), que é o ID selecionado e é mais recente. O F09 atual também é um vaso, não um bloco de pedra. Não há `.blend`: a fonte editável é o gerador por código, porque Blender não está instalado, como em A08/A11. A ficha do manifesto foi atualizada para refletir isso.

## Limitações

- **NÃO VALIDADO NO EDITOR.** Não foram observados: importação estática, materiais criados pela MaterialDescription (conferir `_BaseMap`, metallic/smoothness), remapeamento para o material único, URP real, cena F09, presenter, `DamageFlash` e PlayMode.
- **Escala e brilho ao lado do Hero.** Os props A12 são menores que o vaso de primitivas (vaso 0,78 × 0,68 m contra a esfera de 0,9 × 1,04 m) e mais sóbrios (barro dessaturado contra o laranja `#9E3D18` de `Mat_Vase_F09`), como pede a ficha. Pela prévia em escala continuam legíveis, mas a luz real das salas pode escurecê-los. Se faltar leitura em PlayMode, os ajustes preferidos são escala uniforme de até ~1,15× (ainda dentro do footprint: vaso 0,90 m) ou clarear as células `clay`/`wood` da paleta, sem refazer a malha.
- **Luminância.** O contraste WCAG com o piso é baixo (1,01–1,51). A separação vem do matiz (ΔE 30–37), do sombreamento das faces e do interior escuro. Conferir nas cinco salas.
- O vaso quebrado tem razão de área 0,843 contra o intacto, perto do limite de 0,85. A diferença de aparência (0,801) é o sinal principal.
- Espelho de X na importação: a Unity espelha X, então rachaduras, musgo e cacos assimétricos aparecem do lado oposto ao das pranchas (que já aplicam o espelho). Os previews mostram o resultado esperado na Unity.
- Previews feitos com o rasterizador próprio (ortográfico), não com a Unity. A captura F09 de fundo é perspectiva, então a escala é aproximada.
- Aprovação artística humana pendente.
