# Relatório de qualidade e compatibilidade Unity — Lemo3DGenerator (2026-11-13)

> Registro histórico da execução indicada. Afirmações de ausência de Git/arte/sistemas e pendências descrevem aquela data; consultar [PROJECT_AUDIT](../../PROJECT_AUDIT.md) para estado atual. Medições e limites abaixo foram preservados; não são testes reexecutados por esta revisão documental.

## 1. O que o Lemo-Opuscar oferece de fato (análise)
- **three.js r170** em `node_modules/three` (o mesmo que os estilos 3D importam via importmap), incluindo
  `GLTFExporter`, `GLTFLoader`, `ConvexGeometry`, `BufferGeometryUtils`, `OrbitControls`. **Reutilizado** sem modificação.
- `TECHNIQUE.md §9` e `core/three/post.js`: renderização quadro a quadro para vídeo (GTAO, DOF, bloom). É pós-processo
  de filme — **não** foi usado como substituto de material na Unity (os materiais exportados são PBR simples / URP Lit).
- Estilos 3D (`lowpoly-island`, `brick-toy`): técnica de modelagem por primitivas facetadas (`flatShading`, jitter de
  poucos %, prismas/icosaedros/caixas "pinçadas") e personagens como hierarquia de grupos com `pose()`/`walkPose()`.
  **Reutilizado como linguagem**; o código de personagens deles não serve (sem ossos, sem skinning, sem exportação).
- O Lemo **não tem**: rigging/skinning, clips exportáveis, exportação de arquivos 3D, FBX, integração com engine.
  Tudo isso foi implementado neste gerador independente.
- **Blender não está instalado** (procurado em Program Files, Steam, `%LOCALAPPDATA%` e nos drives C–G). Instalar seria
  um download de terceiros; não foi feito sem autorização.

## 2. Pipeline implementado
definição do personagem (JS) → geometria low-poly real (three, não indexada, normais de face, UVs) → esqueleto humanoide
(nomes compatíveis com Humanoid) → SkinnedMesh multi-material (pesos rígidos por peça; tronco e capa com pesos mistos) →
equipamentos como malhas separadas sob soquetes → clips por poses-chave (Catmull-Rom, 30 fps, todos os ossos, in-place)
→ **ground clamp** (skinning na CPU, ergue o quadril quando algo fica abaixo do chão) → **GLB** (GLTFExporter do Lemo)
+ **FBX 7.4 ASCII** (exportador próprio: hierarquia, geometria, materiais por polígono, Skin/Clusters, BindPose,
um AnimStack por clip) → importação Unity automática → materiais URP Lit, Animator, prefab e cena de teste.

## 3. Validação executada (evidência)
| Verificação | Como | Resultado |
|---|---|---|
| Arquivo válido / 3D real | GLB relido com `GLTFLoader`; bbox com as três dimensões ≥ 0,6 m | 5/5 OK (`reports/validation.json`) |
| Malhas não vazias, materiais | contagem de triângulos por malha; 6–9 materiais por personagem | 5/5 OK |
| Escala e pivô | altura do bbox vs alvo; corpo centrado na base; nada abaixo de y=0 em bind | 5/5 OK (Hero 1,97 m; Elite 2,63; Warden 4,83) |
| Equipamento | nó da arma é descendente do soquete correto, no GLB e na Unity | 5/5 OK |
| Rig e animações | todas as trilhas presentes; cada clip reproduzido (AnimationMixer + skinning CPU) | 36 clips OK |
| Deformação | estiramento de arestas dos triângulos com pesos mistos ≤ 1,8× | OK após correções (máx. medido 1,75×, limite 1,8×) |
| Chão | menor y do corpo por clip ≥ −0,06 m (Death ≥ −0,12) | OK (pior caso −0,036 m) |
| FBX coerente | LimbNodes = ossos do GLB; AnimStacks = clips; Geometry = malhas; sem NaN | 5/5 OK |
| C# Unity | Roslyn do Unity 6000.6.9f1 + 296 referências do projeto; depois o próprio Editor compilou | 0 erros, 0 avisos |
| Importação real | `.fbx.meta`: animationType Generic, escala 1, takes com loop correto | 5/5 OK |
| Unity Editor | `Validate In Editor`: avatar válido, ossos e bindposes, URP Lit em todos os slots, `SampleAnimation`+`BakeMesh` por clip | **5/5 OK** (`reports/unity_validation.json`) |
| PlayMode | cena de teste: os 5 personagens animando com materiais URP e sombras (`preview/captures/unity/`) | visto; Console sem erros |
| Jogo existente | `Floor_01` em PlayMode após instalar os scripts | inicia, Console 0 erros/0 avisos |
| 360° | `preview/captures/turn_*.png` (8 ângulos × 5 personagens), `clips_*.png` (6 instantes por clip) | inspecionado |

## 4. Problemas encontrados e correções
1. Pés atravessando o chão em clips agachados (sem IK) → *ground clamp* no bake (medido no skinning real).
2. Estiramento de 2,26× no tronco (Dash/HeavyAttack) → zonas de mistura de pesos mais largas + curvatura distribuída entre quadril/coluna/peito.
3. Pernas para cima no fim da Death → ângulo das coxas corrigido em relação ao quadril deitado.
4. Braço atravessando a cabeça grande na Victory → braço mais aberto.
5. Espada atravessando o chão no HeavyAttack; marreta do Warden longa demais (atravessava o chão em todos os clips) → ângulos e comprimento ajustados.
6. **Olhos dos esqueletos invisíveis** (dentro do crânio) → detalhes do rosto assentados na superfície do elipsoide.
7. Validador: o `GLTFLoader` divide malhas multi-material em primitivas → contagens ajustadas; JSON da Unity com vírgula decimal (cultura pt-BR) → `InvariantCulture`.

## 5. Limitações e pendências reais
- Lemo intacto: `git status` do Lemo só lista pastas não rastreadas preexistentes; nenhum arquivo dele foi alterado.
- **Sem `.blend`**: Blender ausente. Fonte editável = definições JS; o GLB abre no Blender se ele for instalado.
- **GLB na Unity**: exige o pacote glTFast (alteraria `Packages/`, não autorizado). O caminho Unity é o **FBX**, validado.
- **FBX ASCII próprio**: validado pela importação do Unity 6000.6.9f1; não testado em Maya/Blender/3ds Max.
- **Rig Generic**, não Humanoid: os nomes seguem o padrão Humanoid, mas o avatar Humanoid (retarget) não foi configurado/testado.
- Esqueletos com ~2 mil triângulos, **abaixo** da meta de 3–4 mil do documento (metas, não limites); o herói tem 2,8 mil (meta 3–5 mil).
  Há margem para mais detalhe sem sair do orçamento.
- Peças rígidas por osso (estilo low-poly segmentado): sem deformação de pele; juntas escondidas por esferas/ombreiras.
- Capa animada por 2 ossos com movimento procedural (sem simulação de tecido).
- **SkeletonRanged e Warden não têm descrição no documento de arte**: visuais derivados e marcados como suposições.
- Não houve integração aos prefabs oficiais (por regra): `GeneratedCharacterAnimator` está pronto (Speed por variação de
  posição + gatilhos), mas ligar Attack/Dash/Hit/Death aos eventos de `PlayerCombat`/`EnemyBrain`/`BossBrain` é a próxima etapa,
  numa branch própria. O `DamageFlash` atual usa o renderer `Body` dos prefabs; com o modelo novo ele precisará apontar para o SkinnedMeshRenderer.
- Testes de PlayMode automatizados (Test Runner) não foram criados; a verificação foi pelo menu de validação e inspeção visual.
- Ocorrências de ambiente: durante a verificação a cena aberta foi trocada para a de teste e depois reaberta (`Floor_01` sem
  alterações pendentes); a busca do Project usou a área de transferência; um clique caiu no Explorer quando ele tomou o foco
  (apenas navegou até `E:\`). Prefabs/cenas oficiais mantêm a data 09:50, anterior a este trabalho.

## 6. Teste dentro do jogo (2026-11-13, 11:24)
- Menu **Build In-Game Test (Floor_01 copy)**: 4 variantes de prefab, 5 encounters copiados, 5 salas religadas, cópia da cena
  com o herói no Player. Prefabs, encounters e cenas oficiais mantiveram a data 11:20 (anterior à execução).
- PlayMode em Floor_01_GeneratedCharacters.unity: herói gerado andando (locomoção pela velocidade real); Room_1 instanciou
  2 SkeletonMelee pelo EnemySpawner (Console: "2 inimigo(s) instanciado(s) do encounter 'Encounter_Room1_Generated'");
  os esqueletos perseguiram e atacaram, o herói recebeu dano e morreu (tela "VOCÊ MORREU" + Death); **R** reiniciou a cena.
  Console sem erros nem avisos. Capturas: preview/captures/unity/ingame_*.jpg.
- **Não exercitado em jogo** (o controle remoto é lento demais para lutar): salas 2–5, elite, ranged e Warden; o tempo exato
  de Attack/Dash do herói. Eles usam as mesmas pontes, já compiladas e montadas; falta a jogada manual.
- Limitações da ponte: o tint de dano/windup original (DamageFlash) agia na cápsula, agora oculta — substituído por um flash
  próprio no modelo; a cor de fase 2 do boss (SetBaseColor) e o pulso de escala do rugido (no objeto Visual) não aparecem
  no modelo; HeavyAttack do herói não tem gatilho porque o jogo ainda não tem ataque forte.