# Build do andar com personagens 3D — 2026-11-13

## Comportamento implementado

`Tools > Dungeon Crawler > Build > Floor Scene (M2)` agora reconstrói a base `Floor_01`,
gera a cópia `Floor_01_GeneratedCharacters`, coloca essa cópia como primeira cena habilitada
na lista compartilhada do build, remove a entrada da base `Floor_01` dessa lista e abre a
cena com modelos 3D no Editor. Os demais itens da lista conservam ordem e estado habilitado.
O arquivo da cena base continua sendo a fonte do layout.

Os modelos FBX, controllers e manifestos dos cinco personagens são verificados antes de
reconstruir. A troca para a cena base ocorre antes de atualizar prefabs compartilhados.
O gerador preserva os GUIDs da cópia da cena e dos encounters em vez de apagá-los/recriá-los.

## Arquivos alterados

- `Assets/_Project/Scripts/Editor/DungeonFloorSceneBuilder.cs`: integração e abertura da cena 3D.
- `Assets/_Project/Scripts/Editor/EditorBuildUtility.cs`: seleção idempotente da cena inicial.
- `Assets/_Project/Scripts/Editor/DungeonCrawler.Editor.asmdef`: referência ao Editor do gerador 3D.
- `Tools/Lemo3DGenerator/unity/Editor/GeneratedGameTestBuilder.cs` e cópia instalada em
  `Assets/_Project/Art/Models/Generated/_Scripts/Editor/`: API de construção e preservação de GUIDs.
- `Assets/_Project/Tests/EditMode/FloorBuildTests.cs` e `.meta`: três testes de configuração.
- `Tools/Lemo3DGenerator/README.md` e este relatório: fluxo atualizado.
- Assets regenerados pelo menu: base `Floor_01`, cena 3D, prefabs gerenciados pelo builder,
  variantes dos quatro inimigos/Boss, encounters gerados e dados de navegação.
- `ProjectSettings/EditorBuildSettings.asset`: cena 3D inicial, entrada de Floor_01 removida.
- `Hero_Playtest_Floor.unity`: alterações pendentes salvas após autorização do usuário.
- `reports/floor_build_tests.xml` e `reports/floor_build_idempotence.json`: evidências dos testes.

## Validação e pendências

- `check-unity-cs.ps1`: assemblies Runtime, GameBridge e Editor do gerador compilados sem erros
  pelo Roslyn do Unity 6000.6.9f1 (avisos CS2023 do script de compilação).
- O Unity recompilou os assemblies de Editor e os testes, e o Test Runner descobriu os três
  testes `FloorBuildTests`. Um atributo não suportado pelo NUnit instalado foi removido.
  A versão final do assembly `DungeonCrawler.Editor.dll` foi recompilada após a última alteração.
- `Hero_Playtest_Floor` foi salva conforme autorização explícita do usuário.
- Menu Floor Scene executado duas vezes: ambas as execuções concluíram com quatro variantes,
  cinco encounters e cinco salas religadas; a cena 3D foi aberta automaticamente.
- Comparação SHA256 de seis arquivos .meta (cena e cinco encounters) antes/depois das duas
  construções: zero diferenças. Os GUIDs foram preservados. Evidência em
  `reports/floor_build_idempotence.json`.
- Os nomes internos dos encounters foram alinhados aos nomes dos arquivos, corrigindo os
  avisos introduzidos pela atualização de cópias existentes.
- Test Runner, EditMode, FloorBuildTests: **3 aprovados, 0 falhas**. XML em
  `reports/floor_build_tests.xml`. Verificam substituição da cena provisória, preservação dos
  demais itens/estados da lista, repetição sem duplicatas e recusa de cena inexistente.
- PlayMode na cena reconstruída: herói 3D com espada/capa/materiais e HUD visíveis;
  Console sem erros/avisos durante a inicialização. O log registra recarregamento da cena
  3D ao testar R. Não foi feito um playthrough completo de combate.
- Build And Run, perfil Windows 64-bit: **Succeeded**, aproximadamente 13 segundos.
  `Builds/Windows-Characters-20261113/My project.exe` atualizado com os novos dados do jogo
  e aberto; herói 3D e HUD de progressão visíveis no executável.
- A sessão de jogo foi deixada aberta para o usuário; houve entrada do usuário durante a
  verificação, e a automação de controles foi interrompida para não interferir.
- Na inspeção final durante a jogada do usuário, o HUD mostrava Salas 3/5 e nível 3;
  herói, esqueletos melee, elite e ranged estavam visíveis junto a projéteis e pickups.
- Não há repositório Git inicializado neste diretório.

## Limitações e próximo passo

O builder ainda emite os dois avisos preexistentes `No script asset for WeaponData`. O
Player.log do build atualizado contém dois avisos `The referenced script on this Behaviour
(Game Object '<null>') is missing!`; a associação exata desses avisos ainda precisa ser
investigada. Não houve alteração no sistema de loot/armas nesta tarefa. Não declarar o
projeto inteiro livre de referências ausentes. O Boss não foi exercitado visualmente nesta
validação.

Próximo passo: jogar as salas restantes e investigar os avisos de WeaponData/referências
ausentes como uma correção separada.
