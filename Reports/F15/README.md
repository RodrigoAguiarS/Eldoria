# F15 — Sentinela de Pedra

Base `main` @ `c9f8016`, branch `codex/f15-stone-sentinel`. Unity 6000.6.9f1,
URP 17.6.0 e pacotes preservados. Proposta registrada antes do código em
[PROPOSAL](PROPOSAL.md); [parâmetros/reprodução](../../docs/STONE-SENTINEL-F15.md).

## Incremento

Uma fase de primitivas, leque frontal de direção/origem travadas, aviso 1,4 s
(mínimo 1 s), um impacto autoritativo e recuperação 1,1 s. Dados próprios em
SO. Navegação por NavMeshAgent/EnemyNavigation; decisões a cada 0,2 s. Dano é
consumido antes de Health/eventos, inclusive se dash rejeitar o contato.
Morte, desativação e pausa cancelam o golpe; reativação exige novo aviso e não
cura/revive. Sem projéteis, XP, loot, checkpoint ou integração com F14.

Arena `Assets/_Project/Scenes/F15_StoneSentinel_Test.unity`, fora das Build
Settings de release. Builder idempotente mantém GUIDs, dados e layout.
Sessão e callbacks próprios de reinício/saída; vitória e portas reutilizam
RoomController/FloorController. HUD reconcilia a onda explícita também após
toggle. A única extensão compartilhada de runtime é o getter somente leitura
`RoomController.SpawnedEnemies`; contratos/arquivos do Guardião preservados.

## Evidência

| Artefato | Resultado / cobertura |
| --- | --- |
| [EditMode.xml](EditMode.xml) | Primeira execução: 33/33; geometria, SO, composição/idempotência, Guardião, F13/F14 |
| [EditMode-acceptance.xml](EditMode-acceptance.xml) | **Estado final: 33/33**, incluindo todos os callbacks sem referências nulas e idempotência byte a byte |
| [PlayMode-final.xml](PlayMode-final.xml) | 20 aprovados, zero falhas; 2 capturas explícitas ignoradas no batch |
| [PlayMode-visual.xml](PlayMode-visual.xml) | 2/2 no Editor gráfico, 2560×1440; aviso/recuperação/vitória/reinício e luta real |
| [Visual/playtest.txt](Visual/playtest.txt) | Movimento a 5 m/s pelo CharacterController, combo/giro reais, IA ativa; 4 compromissos/4 resoluções, vitória, HP final 100 |
| [PlayMode-Windows.xml](PlayMode-Windows.xml) | **14/14 no WindowsPlayer**, zero falhas: 10 testes de combate, 3 da arena salva e 1 luta real automatizada |
| [WindowsPlayer/playtest.txt](WindowsPlayer/playtest.txt) | Luta completa no executável de teste, 1920×1080, 4 compromissos/4 resoluções, vitória com HP 100 e reinício |
| [unity-static.json](unity-static.json) | Checker inicial: zero erros/avisos de metas/GUIDs/scripts |
| [project-audit.json](project-audit.json) | Inventário inicial: zero divergências nas cópias Tools/instaladas e zero links quebrados naquele estado |
| [unity-static-final.json](unity-static-final.json), [project-audit-final.json](project-audit-final.json) | Checker final: zero erros/avisos; zero divergências Tools/instaladas e links quebrados |
| [protected-assets.json](protected-assets.json) | 24 arquivos protegidos iguais à base Git: contratos/dados/prefabs do Guardião, loot, cenas dos dois andares/menu, pacotes e configurações |
| [windows-build.json](windows-build.json) | Primeira build isolada: sucesso, 0 erros / 496 avisos de shader |
| [Windows-20261117-043305/windows-build.json](Windows-20261117-043305/windows-build.json) | Build com callbacks revisados: sucesso, 0 erros / 496 avisos; executável próprio da arena |
| [Windows-20261117-044105/windows-build.json](Windows-20261117-044105/windows-build.json) | **Build jogável final**, sem Test Runner/Development: sucesso, 0 erros / 496 avisos, 39,33 s |
| [WindowsSmokeFinal/summary.json](WindowsSmokeFinal/summary.json) | PE AMD64/SHA256, abertura e ataque por clique na build final; sem exceções/Missing Script no log |

As [capturas Windows](WindowsPlayer/) mostram esquiva lateral, vitória,
“Explorar arena”, saída e reinício após a revisão dos callbacks. Esse player é
**Development com Test Runner**, separado da build jogável sem testes.

Cobertura funcional: interior, bordas inclusivas, fora do raio, laterais e
atrás; direção e dados travados; dano único/reentrada; invulnerabilidade e dash
real dentro do setor; morte de atacante/alvo no windup; desativação do brain,
objeto, Health e telegraph; pausa no menu e retorno com aviso novo; NavMesh e
caminho completo reais; HUD antes/durante/depois da luta, morte/toggle;
vitória única e reinício local sem tocar checkpoint. O teste de preservação
compara bytes de todos os assets/prefabs/cenas/materiais/metas existentes.

Regressão: BossTests, BossPlayModeTests, GeneratedBossPresentationTests e
FloorIntegrationTests mantêm duas fases, 50%, 3 s, transição única,
invulnerabilidade/cor do Guardião e conclusão 5/5. Nenhum dado/tabela global de
loot nem prefab/cena do Guardião foi alterado.

## Achados corrigidos

1. `PlayMode.xml`: 17 aprovados, 3 falhas da composição F15 por
   RunCheckpointPresenter herdado do shell, após remover seu controller.
   Builder remove o presenter; reteste `PlayMode-final.xml` passou 20/20.
2. Revisão gráfica: “Coletar recompensas” não se aplica à arena sem loot;
   virou “Explorar arena”. Tela terminal tem reinício local e saída própria.
3. `EditMode-final.xml`: 32/33; a checagem adicional detectou callbacks antigos
   nulos mantidos pelo helper Link (que adiciona, sem substituir). F15 agora
   substitui a lista inteira por um único callback e testa os botões reais de
   vitória/derrota. `EditMode-verified.xml` encontrou a mesma referência antiga
   também no botão Restart da pausa; tratado pela mesma reparação, com remoção
   de callbacks nulos na cópia da arena. `EditMode-acceptance.xml` passou 33/33
   e os botões de vitória/derrota passaram no Windows. Evidências anteriores preservadas.

## Limites e reprodução

Números são propostas, sem aprovação humana de balanceamento/arte/A14. A luta
gráfica automatizada usa movimento e dano normais, sem teleporte, invulnerabilidade,
alteração de HP/estatísticas ou dano sintético; outras provas de ciclo de vida
usam posicionamento/dano controlados e não equivalem à partida normal.

Smoke por UI do player Windows abriu arena em 1920×1080 (opção salva prevaleceu
ao argumento 1280×720); mouse executou ataque/animação/trail, sem exceções no log.
W/Escape sintéticos do controle de desktop não foram reconhecidos nesta sessão,
assim como já registrado em F14. Isso limita a prova por UI, não demonstra
regressão do New Input System. O Test Runner Windows executou o controle/dash
pelo New Input System e completou a luta usando movimento/combo/giro normais.
Teclado físico e avaliação humana seguem pendentes. O log do Editor ao encerrar
o Test Runner Windows registrou avisos internos JobTempAlloc; não há medição ou
certificação de desempenho nesta entrega.

Comandos usam sempre novos destinos de XML/log e
`DUNGEON_CRAWLER_TEST_REPORT_DIR` exclusivo. Editor fechado para batch:

```powershell
$unityF15 = 'C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe'
& $unityF15 -batchmode -nographics -projectPath 'G:/DungeonCrawler/My project' -executeMethod DungeonCrawler.Editor.StoneSentinelSceneBuilder.Build -quit -logFile <novo.log>
& $unityF15 -batchmode -nographics -projectPath 'G:/DungeonCrawler/My project' -runTests -testPlatform EditMode -testFilter 'StoneSentinel|BossTests|LancerAssetTests|SecondFloorTests' -testResults <novo.xml> -logFile <novo.log>
& $unityF15 -batchmode -nographics -projectPath 'G:/DungeonCrawler/My project' -runTests -testPlatform PlayMode -testFilter 'StoneSentinel|BossPlayModeTests|GeneratedBossPresentationTests|FloorIntegrationTests' -testResults <novo.xml> -logFile <novo.log>
```

Capturas: sem `-batchmode -nographics`, filtro de método
`StoneSentinelArenaTests.CaptureRealWarningRecoveryVictoryAndRestart` ou
`StoneSentinelArenaTests.PlaytestActualMovementCombatAndSpatialDodge`.
Player de teste (separado da build jogável):
`DUNGEON_CRAWLER_F15_TEST_PLAYER=1`, `-testPlatform StandaloneWindows64`,
`-buildPlayerPath <pasta-nova>` e mesmos destinos exclusivos. O modificador
inclui a cena F15 apenas no player de teste; não altera Build Settings.

Build jogável final: `Builds/F15/Windows-20261117-044105/Eldoria.exe`.
O editor de pacotes ativou automaticamente SENTIS_ANALYTICS_ENABLED durante
importação; o Test Runner/build também serializa configurações transitórias.
Essas alterações de configurações compartilhadas não fazem parte da entrega.

Próximo passo: revisão do PR e playtest humano dos parâmetros, sem fazer merge
ou ampliar padrões/A14 automaticamente.
