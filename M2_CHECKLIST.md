# M2 — Checklist manual (Unity Editor)

Status: código escrito por análise estática. **NÃO VALIDADO NO EDITOR.**

## Preparação
1. Console sem erros de compilação.
2. `Tools/Dungeon Crawler/Setup/Ensure Folders And Layers` (novos: layer `Door`, pastas `Prefabs/Projectiles`, `ScriptableObjects/Enemies` e `Encounters`).
3. `Tools/Dungeon Crawler/Build/Floor Scene (M2)`. Rodar duas vezes: Hierarchy não pode duplicar objetos.
   - Gera: prefab `Room_Normal`, `Enemy_Ranged`, `Enemy_Elite`, `Projectile`, EnemyData e 4 RoomEncounters, cena `Scenes/Floor_01.unity`.
4. Test Runner: EditMode (`DungeonLogicTests`) e PlayMode (`RoomSystemsPlayModeTests`).
5. O `Prefabs/Enemies/Enemy_Melee.prefab` foi regravado pelo builder (agora parametrizado); a cena `MVP_Arena` deve continuar funcionando (refaça `Build/MVP Scene (M1)` se algo ficar quebrado).

## Floor_01 em PlayMode (aceite: quatro salas sem softlock; portas só abrem com sala limpa)
- [ ] Sem exceções; sem Missing Script/Reference nos prefabs de sala.
- [ ] Jogador começa fora da sala 1; HUD mostra `Salas: 0/4`.
- [ ] Ao entrar no trigger: porta de entrada fecha, porta de saída continua fechada, inimigos surgem em ~0,8 s.
- [ ] Porta de saída **não abre** enquanto houver inimigo vivo (testar matar todos menos um).
- [ ] Ao morrer o último: saída afunda e abre; HUD `Salas: 1/4`.
- [ ] Salas 2–4 repetem o ciclo; porta de entrada fecha atrás do jogador.
- [ ] Sala 3: 1 inimigo roxo (ranged) dispara bolas laranja, recua quando o jogador se aproxima (< 5 m), flash laranja de windup antes do tiro.
- [ ] Sala 4: elite (laranja, maior, 120 HP, 20 de dano) + 2 ranged + 1 melee.
- [ ] Projéteis somem ao bater em parede/jogador/porta; dash evita o dano; sem Instantiate por tiro (Profiler: `ProjectilePool` ativos/inativos estáveis).
- [ ] Ao limpar a sala 4: `ANDAR COMPLETO!`.
- [ ] Morrer em qualquer sala e reiniciar (R/botão) recarrega Floor_01 do zero.
- [ ] Inimigos não atravessam portas fechadas nem ficam presos nos pilares/cantos (softlock).

## Pontos de atenção (incertezas da análise estática)
- **Trigger com CharacterController:** `OnTriggerEnter` deve disparar com o CC. Se a sala não iniciar, é o primeiro suspeito (alternativa: Rigidbody kinematic no Player ou checagem por OverlapBox).
- **NavMesh:** baked uma vez com portas excluídas (layer `Door`). Inimigos ranged podem recuar pelo vão para o corredor; esperado.
- **Ranged sem linha de visão:** atira mesmo com parede no meio (projétil bate na parede). IA de LoS fica para depois.
- **Dificuldade:** o jogador não cura entre salas (não há sistema de cura até o M4). Ajustar `Encounter_Room*` e `EnemyData` conforme playtest.
- Camera ortogonal ao corredor: paredes têm 2 m para reduzir oclusão; avaliar na câmera real.
