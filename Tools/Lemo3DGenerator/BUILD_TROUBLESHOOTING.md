# Modelos 3D ausentes — diagnóstico e correção (2026-11-13)

Relatório da seleção manual inicial da cena. Para a alteração posterior do próprio menu
Build > Floor Scene (M2), incluindo preservação de GUIDs e validações pendentes, consulte
[FLOOR_BUILD_WORKFLOW.md](FLOOR_BUILD_WORKFLOW.md). O fluxo descrito ao final deste relatório
era o comportamento anterior do builder.

## Causa confirmada

A cena aberta era `Assets/_Project/Scenes/Floor_01.unity`, construída com cápsulas provisórias.
O gerador 3D integra os modelos em uma cópia separada:
`Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity`.
Os modelos FBX e essa cópia continuavam presentes. A cópia não estava incluída no build;
a primeira cena habilitada era `Assets/Scenes/SampleScene.unity`.

O Console da captura também registra a execução do builder de Floor_01: esse comando
prepara a cena original com primitivas e não instala os personagens gerados nela.

## Alterações

- `ProjectSettings/EditorBuildSettings.asset`: adicionada a cena com modelos 3D na posição 0,
  habilitada; as quatro cenas anteriores permanecem habilitadas na mesma ordem relativa.
- `Assets/Settings/Build Profiles/Windows.asset` e metadados/pasta: perfil Windows Intel 64-bit
  criado e ativado pela interface do Unity, usando a lista compartilhada de cenas.
- Este relatório. Nenhum script de gameplay, modelo, prefab ou cena foi editado.
- Executável de verificação gerado em `Builds/Windows-Characters-20261113/My project.exe`.
  Manter o executável junto às DLLs e à pasta de dados produzidas pelo Unity.

## Validação executada

- Unity 6000.6.9f1: cena 3D aberta e executada em PlayMode; herói visível, Console 0 erros/0 avisos.
- Configuração salva por File > Save Project e confirmada no arquivo EditorBuildSettings.
- Build And Run com perfil Windows: compilação concluída e executável iniciado.
- Inspeção visual do executável: herói 3D, materiais, espada, capa, cenário e HUD visíveis.
- Durante a interação do usuário, observados herói em movimento e HUD em Salas: 2/5.
- Player.log registra spawns dos encounters `Encounter_Room1_Generated` (2 inimigos) e
  `Encounter_Room2_Generated` (3 inimigos).

## Limitações e próximo passo

- O Player.log contém dois avisos `The referenced script on this Behaviour (Game Object '<null>')
  is missing!`. A captura inicial já tinha avisos sobre WeaponData; a origem dos dois avisos
  do executável ainda não foi identificada. Não declarar o projeto inteiro livre de referências
  faltantes. Os avisos não impediram a exibição do herói 3D.
- Não foi validado o andar inteiro, todos os modelos de inimigos, Boss ou reinício por R.
  Nenhum teste automatizado foi adicionado/executado para esta alteração de configuração.
- O projeto não é um repositório Git; `git status` retorna "not a git repository".
- Para jogar no Editor, abrir `Floor_01_GeneratedCharacters.unity` e usar Play.
- Depois de alterar/reconstruir Floor_01, o fluxo existente é
  Tools > Dungeon Crawler > Generated Characters > Build In-Game Test (Floor_01 copy).
  Esse comando recria a cópia e seus encounters; conferir novamente a inclusão e a posição
  da cena no build após executá-lo, pois o builder atual apaga e recria esses assets.
- Próximo passo técnico separado: localizar os dois avisos de referência ausente no player
  e validar a integração visual das salas restantes.
