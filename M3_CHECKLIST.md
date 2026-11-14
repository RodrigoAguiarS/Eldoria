# M3 — Checklist manual (Unity Editor)

Status: código escrito e **compilado fora do Unity** (Roslyn 6000.6.9f1: runtime, editor e testes, 0 erros).
**Validação parcial via `Logs/Editor.log`** (leitura estática do log; eu não opero o Editor):
- OK: após o builder com Encounter dentro dos prefabs, as 5 salas instanciam inimigos (`Room_1`: 2, `Room_2`: 3, `Room_3`: 3, `Room_4`: 4, `Room_5_Boss`: 1) sem o aviso "NÃO ATRIBUÍDO".
- Ruído conhecido: NREs de `Awake/OnEnable` (EnemyBrain, BossBrain, DoorController, RoomController, PlayerInputReader...) aparecem **durante o builder** (AddComponent em edição, antes da fiação). Não ocorrem em runtime; limpar depois com `[DisallowMultipleComponent]`/guardas ou `Application.isPlaying`.
- Erro de uso: rodar o builder em PlayMode falha (`SaveCurrentModifiedScenesIfUserWantsTo`); pare o Play antes.
- Os encounters aparecem com sufixo `_Generated` no log recente (não foi gerado por mim; confirmar origem).
- **Ainda manual:** telegraph, transição aos 50%, rajada, vitória, HUD (itens abaixo).

## Preparação
1. Console sem erros de compilação.
2. `Tools/Dungeon Crawler/Build/Floor Scene (M2)` (agora gera o andar de **5 salas**: 4 normais + arena do Boss). Rodar duas vezes: Hierarchy não pode duplicar objetos.
   - Novos assets: `Prefabs/Boss/Boss_Warden.prefab`, `Prefabs/Rooms/Room_Boss.prefab`, `ScriptableObjects/Enemies/Boss_Warden_Data.asset`, `ScriptableObjects/Encounters/Encounter_Boss.asset`.
   - `Room_Normal.prefab` é regravado com o mesmo layout de antes (agora parametrizado).
3. Test Runner: EditMode (`BossTests`) e PlayMode (`BossPlayModeTests`) + os anteriores.

## Andar completo (aceite: completar o andar de cinco salas; fase do Boss ocorre uma única vez)
- [ ] HUD mostra `Salas: 0/5`; salas 1–4 funcionam como no M2.
- [ ] Ao limpar a sala 4, a porta abre para a arena do Boss (24×24), sem softlock.
- [ ] Ao entrar na arena: portas fecham; após ~0,8 s o Boss surge; ~1,5 s depois começa a perseguir.
- [ ] **Barra de vida do Boss** (topo, "GUARDIÃO") aparece só durante a luta e some quando ele morre.

## Boss — Fase 1
- [ ] Persegue o jogador e usa **Slam**: disco vermelho no chão cresce por ~1,4 s; o dano (25) só acontece ao final, só se o jogador ainda estiver dentro do raio (3,5 m).
- [ ] Dash durante o golpe evita o dano; sair do círculo evita o dano.
- [ ] Depois do golpe há recuperação (~0,8 s) e cooldown (~2,5 s).

## Boss — transição aos 50% (uma única vez)
- [ ] Ao chegar a 50% de HP: Boss para, cancela o golpe em andamento, some o telegraph.
- [ ] **Invulnerável por 3 s** (acertos não tiram vida nem piscam).
- [ ] Rugido: shake de câmera + pulso de escala; cor muda do roxo para vermelho (branco → vermelho durante os 3 s).
- [ ] A transição **não repete** (mesmo que a vida oscile); sem cura no jogo, mas confirmar pelos logs/estado.

## Boss — Fase 2
- [ ] Mais rápido (×1,3), Slam maior (×1,3) e telegraph de 1,0 s (nunca abaixo de 1 s).
- [ ] **Rajada radial** de 10 projéteis, com aviso de ~1,2 s (anel ao redor do Boss) antes de disparar; a rajada seguinte preenche os intervalos; projéteis vêm do pool.
- [ ] Projéteis somem ao bater em parede/jogador.

## Fim
- [ ] Ao morrer: barra some, corpo some em ~4 s, saída abre, `Salas: 5/5` / `ANDAR COMPLETO!`.
- [ ] ~1,5 s depois aparece **VITÓRIA!** com botão "Jogar de novo (R)".
- [ ] Morrer em qualquer ponto mostra game over; R/botões reiniciam o andar.

## Pontos de atenção (incertezas da análise estática)
- **Telegraph:** discos são cilindros achatados (Lit opaco) em y=0,03/0,05; conferir z-fighting com o piso e se o disco fica visível sob as paredes baixas.
- **NavMesh e corpo grande:** o agente do Boss usa raio 0,5 (igual ao bake) e o corpo visual/collider é maior (2,2×); ele pode encostar visualmente nas paredes.
- **Rugido sem áudio:** o projeto ainda não tem áudio (M4); o rugido é só visual + shake.
- **Boss sem linha de visão:** persegue/atacam sem checar paredes (sem pilares na arena, então não afeta).
- **Balanceamento:** Boss 300 HP, jogador 100 HP sem cura nas salas — pode ser difícil. Ajustar em `Boss_Warden_Data.asset` e nos `Encounter_*` conforme playtest.
- Cenas `MVP_Arena` e `Floor_01` estão em formato binário; o builder regrava `Floor_01`. Recomendado `git init` antes de rodar.
