# F13 — Lanceiro com investida: evidências

Branch `claude/f13-lancer` a partir de `main` @ `35bd7be`. Unity 6000.6.9f1 (Windows 11).
Regras e parâmetros (propostas de playtest) em [docs/LANCER-F13.md](../../docs/LANCER-F13.md).

## Resultado

| Artefato | O quê | Resultado |
| --- | --- | --- |
| [EditMode.xml](EditMode.xml) | EditMode completo | **244/245**; 1 falha pré-existente (abaixo). 17/17 F13 |
| [PlayMode.xml](PlayMode.xml) | PlayMode completo, 1ª execução | 176 aprovados, 12 explícitos ignorados, **1 falha F13 no próprio teste** (abaixo) |
| [PlayMode-lancer-retest.xml](PlayMode-lancer-retest.xml) | Lancer após corrigir o teste | 13/14: revelou lanceiro no vão da porta (achado 1) |
| [PlayMode-room-diag.xml](PlayMode-room-diag.xml) | Diagnóstico por fase | Perseguição e investida no mesmo z = −8,074 |
| [PlayMode-room-carve.xml](PlayMode-room-carve.xml) | Após carving nas portas da sala de teste | 1/1 (+1 explícito) |
| [PlayMode-lancer-final.xml](PlayMode-lancer-final.xml) | **Todos os testes F13 no estado final** | **14/14**, 1 captura explícita ignorada em batch |
| [PlayMode-capture.xml](PlayMode-capture.xml) | Captura em Editor gráfico | 1/1, 2560×1440 ([Visual](Visual/)) |
| hashes-build3/4, guids-build3/4 | Builder executado duas vezes | SOs/materiais byte-idênticos, GUIDs iguais; prefab e cena regravados com a raiz do prefab preservada (fileID referenciado pelo encounter confere) |

Logs (`*.log`) ficam locais (ignorados pelo Git).

### Cobertura do aceite

| Critério | Evidência |
| --- | --- |
| Aviso ≥ 1 s | `LungeTimelineTests.Telegraph_IsNeverShorterThanOneSecond` (0; 0,2; 0,99; −3; NaN → 1 s); `LancerLungeTests.Telegraph_LastsAtLeastOneSecond_WithoutMovementOrDamage` (asset com 0,2 s → 1 s real, sem deslocamento/dano antes); sala salva confere `TelegraphDuration ≥ 1` |
| Direção travada | `Direction_IsLockedAtCommit_TargetLeavingLaneTakesNoDamage`; `Brain_ChasesCommitsAndKeepsFourExternalStates` (brain não gira durante o compromisso) |
| Sem atravessar paredes | `BakedWall_ClipsLane_LancerNeverCrossesIt` (NavMesh.Raycast) |
| Porta | `ClosedDoorNotInNavMesh_ClipsLaneBySolidCast` (SphereCast em Door); sala salva: nenhum lanceiro passa de z = −7,5 (face da porta −8,0) |
| Nenhuma colisão aplica dano em todo frame | `TargetInsideLaneForWholeDash_IsDamagedExactlyOnce` (alvo grudado na lança por todo dash e recuperação → 1 dano); `Contact_CanBeClaimedOncePerLunge_AndOnlyDuringDash` |
| Saída da faixa | `Direction_IsLocked...` (alvo sai lateralmente → 0 dano, lança percorre a faixa toda) |
| Dash/invulnerabilidade | `DashInvulnerability_ConsumesContact_LancerPassesThroughWithoutLaterHit` (janela de 0,2 s expira com o lanceiro ainda sobre o alvo: sem segundo contato) |
| Alvo morto | `DeadOrDestroyedTarget_CancelsDuringTelegraph` (morto e destruído) |
| Pausa / desativação no aviso | `Pause_FreezesTelegraph_ThenResumesRemainingWarningAndHitsOnce`; `DisableDuringTelegraph_HidesWarning_DoesNotRefundCooldown` (componente e GameObject) |
| Fim da sala sem softlock | `Room_KillingLancersDuringTelegraph_ClearsRoomAndOpensExit`; `LancerTestRoomTests.SavedRoom_...` (cena salva, prefab real, ≥3 compromissos em 9 s, sala e andar concluídos, corpos liberados) |
| Estados externos preservados | Brain só usa Idle/Chase/Attack/Dead; 175 PlayMode e 227 EditMode não-F13 seguem passando (melee/ranged/elite/Boss) |
| Sem Rigidbody / sem varredura por frame | `LancerAssetTests` (sem Rigidbody/EnemyCombat no prefab); código: `NavMeshAgent.Move` e conta de segmento por frame, consultas de física/NavMesh só no compromisso e na visão cacheada (0,1 s) |
| Encontros/tabelas de release intactos | `LancerAssetTests.ReleaseEncounters_DoNotReferenceLancer...`; `TestScene_ExistsOutsideBuildSettings`; `git status` sem assets de release alterados |

## Falhas e achados

- **Pré-existente (não F13):** `ItemsCombatAssetTests.IntegrateTwice_IsIdempotentAndPreservesPickupIdentity`
  falha com `Inventory slot missing: 0`, idêntica aos XML da F12 (registrada desde a F10).
  Os testes de idempotência do EditMode regravaram cenas/prefabs de release; esses efeitos
  foram descartados com `git checkout` antes do commit, como na F12.
- **Erro do teste F13, corrigido:** a 1ª versão do teste da sala teleportava o jogador para fora
  do `detectionRange` (13,1 m > 13 m) no frame do spawn; os lanceiros ficavam corretamente em Idle.
  O teste agora espera a detecção antes de reposicionar.
- **Achado 1 (pré-existente, afeta release, não corrigido aqui):** portas (layer Door) não entram
  no bake do NavMesh e não têm `NavMeshObstacle`. Qualquer `NavMeshAgent` pode caminhar pelo vão de
  uma porta **fechada** quando o jogador encosta nela. Reproduzido na sala de teste antes do carving:
  jogador invulnerável em (0, 0, −7) junto à porta de entrada fechada → lanceiro em z = −8,07,
  dentro do bloqueio, alcançado durante a perseguição (a investida não avançou além).
  Na sala F13 cada bloqueio de porta recebeu `NavMeshObstacle` com carving; as salas de release
  continuam sem ele. Correção para release exige tarefa própria (prefabs de sala/cenas).

## Observação visual (PlayMode gráfico)

[lancer-telegraph.png](Visual/lancer-telegraph.png): faixa escura com preenchimento laranja além
da metade, tint laranja no corpo, lança indicando a direção. [lancer-dash.png](Visual/lancer-dash.png):
~0,12 s após o fim do aviso, faixa escondida e lanceiro deslocado em linha reta.
[room-cleared.png](Visual/room-cleared.png): porta norte aberta, +15 XP e drop. Os corpos ficam
de pé até o `corpseLifetime` (sem animação de morte nas primitivas, como os demais inimigos).
Jogador invulnerável e teleportado: diagnóstico, **não** aceite de run normal nem arte final.

## Não executado / pendências

- Playtest humano dos valores (aviso, largura da faixa, recuperação, dash como resposta).
- Build Windows: não gerada; a sala F13 fica fora das Build Settings.
- A13 / rig Generic: bloqueado. A13 ainda não existe (conceito 2D) e o catálogo pede o F13
  validado antes do 3D. Os eventos `TelegraphStarted/DashStarted/TargetHit/DashEnded/Finished/Cancelled`
  estão prontos para uma ponte Generic alinhar a pose de impacto ao dano autoritativo.
- Áudio dedicado da investida.

## Reproduzir

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'G:/DungeonCrawler/My project' -executeMethod DungeonCrawler.Editor.LancerTestSceneBuilder.Build -quit -logFile <novo.log>
& 'C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'G:/DungeonCrawler/My project' -runTests -testPlatform PlayMode -testFilter DungeonCrawler.Tests.PlayMode.Lancer -testResults <novo.xml> -logFile <novo.log>
```

Captura: mesmo comando sem `-batchmode -nographics`, com
`-testFilter DungeonCrawler.Tests.PlayMode.LancerTestRoomTests.Capture_TelegraphDashAndClearedRoom`
e `DUNGEON_CRAWLER_TEST_REPORT_DIR` apontando para uma pasta nova (recusa sobrescrever PNGs).
