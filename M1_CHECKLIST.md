# M1 — Checklist manual (Unity Editor)

Status: código escrito por análise estática. **NÃO VALIDADO NO EDITOR.**

## Preparação
1. Abrir o projeto e confirmar Console sem erros de compilação.
2. `Tools/Dungeon Crawler/Setup/Ensure Folders And Layers` (cria também o layer `Obstacle`).
3. `Tools/Dungeon Crawler/Build/MVP Scene (M1)`. Rodar uma segunda vez: não deve duplicar objetos (Hierarchy igual).
4. `Tools/Dungeon Crawler/Setup/Validate Foundation (M0)` — tudo `[OK]`.
5. Test Runner: EditMode (`HealthAndCooldownTests`, `FoundationTests`) e PlayMode (`CombatPlayModeTests`).

## Cena `Assets/_Project/Scenes/MVP_Arena.unity` em PlayMode
- [ ] Console sem exceções; sem Missing Script/Reference na Hierarchy.
- [ ] WASD move relativo à tela (W = "para cima" na tela), sem subir/descer.
- [ ] Câmera segue suave e **não gira** quando o jogador gira.
- [ ] Jogador gira para o mouse (só em Y); a marca amarela indica a frente.
- [ ] Botão esquerdo ataca (flash do arco); cooldown ~0,5 s; segurar repete.
- [ ] Espaço: dash na direção do WASD (ou frente); cooldown 2 s (barra azul recarrega).
- [ ] Durante o dash (~0,25 s) o dano do inimigo é ignorado.
- [ ] Inimigo vermelho persegue (Idle → Chase → Attack), pisca laranja no windup e causa 10 de dano.
- [ ] Barra de vida do inimigo acima da cabeça cai a cada golpe (40 HP = 2 golpes) e some na morte; corpo some após 2 s.
- [ ] Jogador pisca vermelho + shake leve ao ser atingido; barra de HP (HUD) atualiza.
- [ ] Ao morrer: controles param, painel "VOCÊ MORREU" aparece; botão e tecla R reiniciam a cena.
- [ ] Inimigo contorna pilares (NavMesh gerado).

## Pontos de atenção (incertezas da análise estática)
- **Câmera:** `CinemachineCamera` sem componente de rotação deve manter a rotação 45° do transform. Se a câmera "olhar" estranho, conferir `CM Iso Camera` (Rotation Control = None).
- **Shake:** depende de `CinemachineImpulseSource` (player) + `CinemachineImpulseListener` (CM Iso Camera) com configuração padrão; ajustar `force` em `PlayerHitShake` se imperceptível.
- **NavMesh:** `NavMeshSurface.BuildNavMesh()` no builder; se o inimigo não andar, verificar o NavMeshData salvo ao lado da cena e o aviso "Failed to create agent".
- **Fonte da UI:** `LegacyRuntime.ttf` (UnityEngine.UI.Text).
- Valores de balanceamento (HP 100/40, dano 20/10, velocidade 5, cooldown de dash 2 s) são ajustáveis: `EnemyData` em `ScriptableObjects/Enemies/Enemy_Melee.asset`; jogador nos componentes do prefab `Player`.
