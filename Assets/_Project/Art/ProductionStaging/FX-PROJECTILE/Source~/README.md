# A08 — FX-PROJECTILE (núcleo do projétil hostil)

Um **modelo 3D estático real** para substituir só o visual da esfera laranja de `Prefabs/Projectiles/Projectile.prefab`. É um núcleo de brasa angular dentro de uma casca de ferro escuro (`#3E4152`) partida em quatro placas por duas fendas quentes (`#FF9A2E`). A silhueta continua esférica e não passa do envelope de 0,4 m da esfera atual. Não há pontas, explosão, rig, animação, colisor nem dano.

Nada do domínio foi alterado: `Projectile.prefab`, `Projectile.cs`, `ProjectilePool.cs` e `Mat_Projectile.mat` só foram lidos. A integração é uma tarefa Codex separada.

| Item | Valor |
| --- | --- |
| Malha | `../FX-PROJECTILE-Core.fbx`: FBX 7.4 ASCII estático, **156 triângulos** (alvo ≤ 300), 88 pontos de controle, 5 peças fechadas (4 placas + brasa) |
| Envelope / pivô | Raio máximo 0,1997 m (≤ 0,2). Bounds 0,386 × 0,374 × 0,375 m, centro a ≤ 0,6 mm da origem. Metros, +Y para cima, +Z frente (direção de voo: `Projectile.Launch` usa `LookRotation(direção)`) |
| Material | **Um** material, `FX-PROJECTILE-Core` (URP/Lit). Cada face aponta para o centro de uma célula 8×8 de `../FX-PROJECTILE-Core_Base.png` (BaseMap) e de `../FX-PROJECTILE-Core_Emission.png` (EmissionMap), ambos 16×16. O FBX referencia as duas texturas (DiffuseColor/EmissiveColor) |
| Paleta | ferro `#3E4152` (sem emissão) · parede da fenda `#8A4A22` (emissão `#8A4A22`) · brasa `#FF9A2E` (emissão `#FF9A2E`) · 4ª célula reserva = ferro |
| Material proposto | `_BaseColor` branco, `_BaseMap` = Base, `_EMISSION` ligado, `_EmissionMap` = Emission, `_EmissionColor` HDR 1,0 (faixa útil 1,0–1,6), metallic 0,2, smoothness 0,35, GI de emissão None. **Uma instância compartilhada** pelo pool, sem cópia por tiro |
| Revisão | `FX-PROJECTILE-Core.glb` (glTF 2.0, mesma malha, cor por vértice, sem a emissão; não é importado pela Unity) |
| Fonte editável | `Tools/Lemo3DGenerator/src/production/FxProjectileCore.mjs`: `SHAPE` (envelope, raios, fendas, rotação/jitter da brasa, semente), `PALETTE`, `MATERIAL` |
| Validador + previews | `Tools/Lemo3DGenerator/src/production/FxProjectileCore_preview.py`: relê o FBX e os PNG **do disco** (sem three.js), valida 15 itens, mede a leitura e gera as pranchas |
| Ferramenta | Node 24.18 + three.js r170 do Lemo-Opuscar (somente leitura: `ConvexGeometry`, `GLTFExporter`) e o vocabulário low-poly do `Lemo3DGenerator` (`geo.mjs`: hull convexo, normais de face, jitter determinístico). Python 3.11.9 + NumPy 2.4.6 + Pillow 13.0.0 no validador. Blender não está instalado; o "DCC" é o gerador por código, como no lote ITEMS-COMBAT |
| Determinismo | Duas execuções seguidas dão os mesmos SHA256 em FBX, PNG, `.meta`, GLB e pranchas. Os hashes estão em `FxProjectile.validation.json`. O hash do FBX ASCII vale para o arquivo gerado (LF): com `core.autocrlf`, o checkout pode convertê-lo para CRLF. Regenere para comparar |

## Reproduzir

```bash
cd Tools/Lemo3DGenerator
node src/production/FxProjectileCore.mjs           # malha, FBX, GLB, paletas, .meta, source/render JSON
node src/production/FxProjectileCore.mjs --check   # só valida em memória
cd ../..
python -I Tools/Lemo3DGenerator/src/production/FxProjectileCore_preview.py   # valida do disco + previews
```

O gerador reescreve as `.meta` com **GUIDs estáveis**: SHA256 de `Eldoria/A08/<nome>`, sempre o mesmo valor. São elas:

- **FBX:** cópia exata do ModelImporter de `Art/Items/Models/Loot_XpOrb.fbx.meta`, serializado pela Unity 6000.6. Só o GUID muda.
- **Texturas:** template de `FX_Spark`, com estas mudanças: point, sem mipmaps, sem compressão, sem alpha, tamanho máximo 32.

O validador sai com código 1 se alguma verificação falhar.

## Direção visual

- **Referências reais:**
  - Esfera atual: Unity Sphere × 0,4 com `Mat_Projectile` `_BaseColor (1, .55, .1)`, sem emissão. `Projectile` com `radius` 0,25 e `lifetime` 4.
  - Cajado do Skeleton Ranged: madeira `#5A3A22` em `skeleton_ranged.mjs`.
  - Runas do Warden: `#FF2A2A` emissivo em `warden.mjs`.
  - Pedra e ferro `#3E4152`/`#55596B`.
  - Câmera do jogo: pitch 45°, yaw 0 (`DungeonMvpSceneBuilder.CameraOffset (0, 14, −14)`).
  - Captura real `Reports/F05/Visual-final/1920-collection.png`.
- **Forma:** quatro placas de ferro facetadas (hull convexo de amostras da icosfera com jitter de raio entre 0,19 e 0,20 m, cortadas por dois planos) em volta de uma brasa icosaédrica (r 0,172 m, 20 triângulos). As fendas são:
  - `spine`: plano x = 0. Corre ao longo do voo e contém +Y e +Z. Meia-largura 5 cm, fenda de 10 cm.
  - `brow`: plano com normal (0; 0,34; 1), deslocado 5 cm para a frente. Meia-largura 3 cm. Cruza a frente e deixa as duas placas dianteiras menores, o que dá sentido de direção.

  As paredes das fendas são a ferrugem quente. A brasa aparece no fundo. Nenhum vértice passa de 0,1997 m.
- **Leitura pela câmera do jogo** (24 rumos, `FxProjectile.validation.json`):
  - Fração quente da silhueta (brasa + parede): 37,0–43,8%. Só a brasa: 18,6–29,9%.
  - Área da silhueta: 82–90% da esfera atual. Nunca maior, então não aumenta a leitura de perigo.
  - Contraste WCAG da brasa iluminada com o piso: 6,35 (`#3E4152`) e 4,37 (`#55596B`).
  - Contraste da cor média da silhueta (pior rumo): 2,54 e 1,75. A esfera atual dá 3,22 e 2,22.

## Desvio consciente da ficha: largura da fenda

A ficha pede "casca simples e **pequena** fenda quente". Primeiro foram geradas fendas finas (meia-largura 1,9 cm e 1,1 cm). Medido pela câmera do jogo e visto sobre a captura F05, o ferro `#3E4152` tem **a mesma cor do piso**:

- a cor média da silhueta ficou com contraste ~1,0 contra o piso (a esfera atual dá 3,2);
- só 15% da silhueta era quente;
- o projétil hostil quase sumia.

As fendas atuais (10 cm e 6 cm) são o mínimo testado que devolve a leitura sem tornar a peça uma bola laranja. O validador agora bloqueia uma regressão nisso: a fração quente precisa ser ≥ 0,12 e o contraste médio ≥ 1,5.

Mesmo assim, a esfera atual (laranja chapada) ainda tem **mais contraste médio** que o núcleo A08. Por isso:

- comparar os dois em PlayMode antes de aprovar;
- se faltar leitura, ajustar primeiro no material (`_EmissionColor` até ~1,6 ou ferro um pouco mais claro na paleta), **sem refazer a malha**;
- se a direção de arte preferir fendas finas, mudar `SHAPE.fissures[*].halfGap` no gerador e rodar de novo. O validador mostra o custo de leitura.

## Pranchas (todas rotuladas PREVIEW; rasterizador próprio, não são captura da Unity)

- `preview_ortho.png`: vistas **frontal** (olhando −Z, a frente do voo), **lateral**, **topo** e **câmera do jogo a 45°**, ortográficas. O círculo marca o envelope de 0,4 m.
- `review_headings_vs_current.png`: núcleo A08 e esfera atual pela câmera do jogo nos rumos 0/45/90/135/180/270°.
- `review_floor_preview.png` / `review_floor_zoom.png`: núcleo e esfera atual em escala (~75 px/m) sobre a captura real F05, nos ladrilhos claro e escuro. A sombra e a ordenação de profundidade não são simuladas.
- `contact_sheet.png`: vista a 45° ampliada e as duas paletas.

O preview espelha X ao converter FBX → Unity, como a Unity faz na importação, na mesma convenção do lote ITEMS-COMBAT. A casca é simétrica em X; só a rotação interna da brasa muda.

## Verificações (`FxProjectile.validation.json`, 15/15 ok)

- Contagem e envelope: 156 triângulos, todos triângulos, ≤ 300. Raio máximo ≤ 0,2. Centro dos bounds no pivô.
- Cabeçalho: `UnitScaleFactor` 100 (metros), +Y up, +Z front.
- Conteúdo do FBX: só Model/Geometry/Material/Texture/Video. Sem Deformer, Pose, AnimationStack, NodeAttribute, câmera, luz ou colisor. Um Model e um Material. As texturas referenciadas existem ao lado do FBX e estão ligadas a DiffuseColor/EmissiveColor.
- Geometria: 5 peças, cada aresta em exatamente 2 triângulos. Volume com sinal > 0 por peça, ou seja, winding para fora. Normal do FBX = normal geométrica em todas as faces (cos ≥ 0,99999, flat).
- UV e paleta: cada face com UV constante no centro de uma célula (ferro 96, parede 40, brasa 20). Os PNG decodificados batem com a paleta do gerador.
- Leitura: fração quente em todos os rumos, silhueta não maior que a esfera, contraste médio e contraste da brasa contra o piso.

Também foram executados:

- `asset_report.py --expect-size 16x16 --max-colors 4` nas duas paletas: exit 0, `asset_report.json`.
- Releitura do GLB: cabeçalho glTF 2, 1 malha, 1 primitiva, POSITION/NORMAL/TEXCOORD_0/COLOR_0, 156 triângulos.

## Integração proposta (tarefa Codex separada; detalhes em `FX-PROJECTILE.source.json` → `integration_proposal`)

1. Criar um filho `Visual` em `Projectile.prefab` com MeshFilter (`FX-PROJECTILE-Core`) e MeshRenderer (material único compartilhado). A raiz tem `localScale` 0,4. Há duas opções:
   - o filho usa escala 2,5;
   - a raiz passa a 1. `Projectile.cs` não usa a escala: o `SphereCast` usa `radius` 0,25 m em unidades de mundo.
2. Desligar (não remover) o MeshRenderer da esfera da raiz. Não existe Collider e nenhum deve ser criado. Preservar `hitMask`, `radius`, `lifetime`, `Launch`/`ForceRelease`/`OnDespawned` e o pool (capacidade 16, máximo 64, aquecimento).
3. Testar em PlayMode:
   - 16 tiros simultâneos sem `Instantiate` depois do aquecimento;
   - reset por `ForceRelease`, `OnDisable` do pool e pausa (`timeScale` 0);
   - leitura sobre o piso e o telegraph do Warden.
4. Medir a GPU (Frame Debugger/Profiler) antes e depois. Não declarar ganho sem medição: a malha tem 156 triângulos contra 768 da Sphere da Unity, mas acrescenta uma textura e emissão.

## Limitações

- **NÃO VALIDADO NO EDITOR:** importação do FBX, criação/remapeamento do material URP, ligação das texturas pelo FBX, aparência com a luz real, Bloom, `Projectile.prefab`, pool de 16 tiros, reset e GPU não foram observados.
- As `.meta` vêm de templates reais. A Unity pode reserializá-las na primeira importação.
- Com `materialImportMode` 2, a Unity cria um material a partir da descrição do FBX. Conferir se `_BaseMap`/`_EmissionMap` foram ligados. Se não foram, criar o material URP/Lit proposto acima.
- A Unity espelha X na importação. A forma não depende disso, mas a orientação da brasa interna fica espelhada em relação ao GLB.
- Os previews são ortográficos com sombreamento simples (ambiente + difusa + emissão). Não são o URP.
- Aprovação artística humana pendente.

## Procedência e licença

Projeto Eldoria. Geometria própria, gerada por código. three.js r170 (MIT) foi usado somente para leitura a partir do Lemo-Opuscar. Sem IA generativa de imagem nem asset de terceiros.
