# F15 — proposta anterior ao protótipo

Base `main` atualizada: `c9f80162acb3902c54ff8fb90baff2e461c65758`; branch `codex/f15-stone-sentinel`.

Uma única Sentinela de primitivas, em arena separada, sem conexão com F14/release,
loot, XP, checkpoint ou segunda fase. Reutilizar Health, EnemyNavigation/NavMeshAgent,
IEnemyInjectable, RoomController/FloorController, jogador e shell existentes.
BossBrain/Data/Phase/Telegraph do Guardião permanecem intactos.

Propostas de playtest: HP 240, velocidade 2,4 m/s, think 0,2 s, intro 1,5 s,
leque frontal de 100° e 6 m, dano 24, aviso 1,4 s (mínimo 1 s), recuperação
1,1 s, intervalo 1,2 s após recuperação. Origem/direção travadas ao compromisso;
área considera o centro XZ do jogador, com tolerância vertical de 2 m.
Sem projéteis. Um impacto autoritativo, inclusive se Health rejeitar por dash.
Sair do leque, das bordas ou do alcance evita dano.

Morte/desativação do atacante ou alvo e pausa cancelam o golpe, escondem aviso e
param navegação; retomar exige novo aviso completo. Reativação não cura/revive nem
resolve golpe antigo. Recuperação é uma janela explícita para contra-ataque.

Arquivos próprios em Scripts/Boss, UI, Editor e Tests; dados/prefab/sala/cena F15.
Builder idempotente preserva GUIDs e parâmetros existentes. Cena fora das Build
Settings de release; build Windows isolada contém somente a arena.
Sessão isolada reinicia essa cena e não lê/apaga o checkpoint da run normal.

Aceite: geometria dentro/fora/borda/atrás; direção travada; dash; morte no windup;
pausa e reativação; dano/vitória únicos; HUD só durante luta; reinício limpo;
NavMesh válido; composição sem Missing Script; regressão do Guardião.
Executar EditMode/PlayMode, abrir cena e observar PlayMode gráfico, build/smoke
Windows, registrar evidências e limitações sem atribuir aceite humano à automação.
A14 e novos padrões ficam para tarefa posterior.
