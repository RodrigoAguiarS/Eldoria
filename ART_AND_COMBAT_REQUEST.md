# Arte e combos — diagnóstico e pedido para outro modelo

Revisão em 13/11/2026. Fonte principal: `Assets/_Project/Art/Concepts/ART_DIRECTION_AND_PROMPTS.md`, comparada aos assets gerados, catálogo e código atuais. Este arquivo é um briefing; não implementa o novo combate.

## O que existe e o que falta

| Parte | Estado encontrado |
| --- | --- |
| Herói | Conceitos `Hero_Concept_01.png` e `Hero_Concept_01_Front.png` existem; modelo FBX/GLB com capa, espada, rig e 9 clips também. O status “NENHUMA IMAGEM FOI GERADA” do documento é histórico e está desatualizado. |
| Melee e elite | Modelos FBX/GLB existentes, materiais e animações. Não localizada a imagem `SkeletonMelee_Concept_01.png` pedida no documento. |
| Ranged e Warden | Modelos existentes, mas o ART_DIRECTION não define suas fichas visuais. Seus manifests e `Tools/Lemo3DGenerator/CATALOG.md` registram que os desenhos foram derivados como suposições. Falta formalizar/aprovar a direção desses dois personagens, incluindo fase 2 do Boss. |
| Sala da masmorra | Não localizada `DungeonRoom_Concept_01.png`. A Floor ainda é montada com blocos e materiais temporários; falta o acabamento do kit modular de pedra, piso rachado, musgo, tochas e runas previsto no conceito. A presença de paredes/portas funcionais não equivale a esse acabamento. |
| Texturas | Modelos gerados usam materiais de cores por parte. Não localizados os atlas pintados 512×512 de herói/esqueleto previstos na direção de arte. O Hero tem 9 materiais; isso precisa ser considerado ao avaliar renderização, sem presumir que triângulos sejam o único custo. |
| Rig | Pipeline atual importa Generic. O documento pede humanoide e o prompt antigo pede Avatar Humanoid: falta configurar/validar retargeting se esse requisito for mantido. Generic já permite as animações próprias atuais. |
| Evidência visual e procedência | Há capturas de turnarounds/animações no gerador, mas não localizados `Art/Concepts/PROVENANCE.md` nem evidência específica do teste de silhueta preta a 128 px e aprovação sobre o piso real. Não declarar esse aceite concluído. |
| Boss fase 2 | A cor/pulso de escala originais afetam o placeholder oculto, não o Warden visível; requer integração ao modelo gerado. Diagnóstico detalhado em `MVP_AUDIT.md`. |
| Ataque | Há `Attack` e `HeavyAttack` no FBX do Hero. Só Attack é acionado pela ponte de gameplay; não há sequência de combos implementada. Combos são uma expansão solicitada agora, não um requisito esquecido do documento de arte. |

Ter menos triângulos que a meta aproximada não torna um asset incompleto: o Hero tem 2.782, abaixo da meta de 3–5 mil; esqueletos têm cerca de 2 mil. Julgar legibilidade/deformação e medir custo antes de adicionar geometria.

## Situação do ataque atual

- `Assets/_Project/Scripts/Player/PlayerCombat.cs`: um ataque repetido ao segurar o botão; cooldown 0,5 s, dano após 0,1 s, alcance 2,2 m e cone de 120°.
- Usa `OverlapSphereNonAlloc` com buffer reutilizado de 16 colliders. Isso já evita a alocação de um array de resultados por consulta. Há oportunidade de verificar saturação e deduplicar um mesmo inimigo com vários colliders.
- `GeneratedPlayerAnimationBridge` observa o GameObject AttackVisual e inicia o mesmo clip Attack a partir de 0,22 s. O manifesto marca impacto a 0,32 s, portanto o deslocamento atual foi escolhido para coincidir com o hitDelay de 0,1 s. Não chamar essa compensação de erro de sincronização sem medir.
- `HeavyAttack` existe (1,2 s, impacto em 0,7 s) mas não tem acionamento no gameplay. Pode ser base para um terceiro golpe, ajustado à cadência pretendida.
- Tornar o golpe mais responsivo é diferente de reduzir CPU/GC. Ganho de desempenho só pode ser declarado com comparação no Profiler/build.

## Tasks separadas

O pedido conjunto anterior foi substituído por escopos independentes. Use somente o arquivo da task que será executada:

| Task | Arquivo | Entrega |
| --- | --- | --- |
| 01 — Imagens e importação | [01_IMAGES_AND_UNITY_IMPORT.md](Tasks/01_IMAGES_AND_UNITY_IMPORT.md) | Somente imagens/texturas e importação desses arquivos no Unity. Sem código, gameplay ou combos. |
| 02 — Combos | [02_PLAYER_COMBOS.md](Tasks/02_PLAYER_COMBOS.md) | Implementação do ataque, clips, integração, testes e medição específica do combate. Sem geração de imagens. |
| Demais tasks | [03_REMAINING_TASKS.md](Tasks/03_REMAINING_TASKS.md) | Armas/Missing Script, Boss, integração visual 3D, áudio, acessibilidade, VFX/desempenho, Git e aceite final. |

A Task 01 não precisa da implementação do combo. A Task 02 usa o modelo existente e não depende das imagens novas. Importar conceitos/texturas no Project não equivale a criar ou substituir modelos 3D no cenário; essa integração fica em task própria.

Os arquivos são briefs para execução separada. Nenhum deles foi executado ao organizar esta divisão, e nenhum chat adicional foi criado.
