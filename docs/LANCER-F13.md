# F13 — Esqueleto lanceiro com investida (experimental)

Estado: **experimental, somente na sala de teste** `Assets/_Project/Scenes/F13_Lancer_Test.unity`
(fora das Build Settings). Nenhum encontro, tabela, prefab ou cena de release foi alterado.
Valores abaixo são **propostas de playtest**, sem validação humana.

## Comportamento

O lanceiro usa os mesmos estados externos dos demais inimigos (Idle → Chase → Attack → Dead).
Dentro de Attack, a investida tem subetapas próprias:

1. **Compromisso** — com visão livre (Obstacle/Door) e alvo dentro do alcance de engajamento,
   a direção é travada no plano XZ e a faixa é calculada uma única vez: recortada pela borda do
   NavMesh (paredes assadas) e por um SphereCast em Obstacle/Door (portas não entram no NavMesh).
   O corpo gira de uma vez para a faixa; o brain deixa de girá-lo até o fim da investida.
2. **Aviso** — faixa no chão com o comprimento real recortado; o preenchimento avança do lanceiro
   até o fim. Duração mínima de **1 s** imposta pelo código (`EnemyLungeData.MinTelegraphSeconds`)
   mesmo que o asset tenha valor menor. Sem deslocamento nem dano nesse intervalo.
3. **Investida** — deslocamento por `NavMeshAgent.Move` ao longo da faixa travada (nunca Rigidbody).
   O contato é uma conta de distância do segmento percorrido no frame até o alvo injetado; não há
   consulta de cena por frame. Só a **primeira** sobreposição por investida chama `TakeDamage`:
   - dano aceito → a investida para no impacto e entra em recuperação;
   - alvo invulnerável (dash) → o contato é consumido, sem dano, e o lanceiro atravessa até o fim
     da faixa. Colisão contínua nunca repete dano.
4. **Recuperação** — parado e vulnerável; depois volta ao fluxo normal do brain.

Cancelamentos: alvo morto/destruído, troca de alvo, morte do lanceiro, desativação do componente
ou do objeto e saída do estado Attack escondem o aviso e descartam a investida. O cooldown já
iniciado no compromisso não é reembolsado (mesma regra do EnemyCombat/F07). Pausa
(`timeScale = 0`) congela aviso, investida e recuperação; nenhum compromisso começa pausado.

## Parâmetros propostos (playtest)

| Asset | Campo | Valor | Observação |
| --- | --- | --- | --- |
| `Enemy_Lancer` (EnemyData) | maxHealth | 45 | entre melee (40) e elite (120) |
| | moveSpeed | 3,2 | |
| | attackDamage | 18 | golpe único por investida |
| | attackRange | 5,5 m | distância para iniciar o compromisso |
| | attackCooldown | 3,2 s | cobre um ciclo completo (~2,34 s) |
| | detection / loseInterest | 13 / 19 m | |
| `Enemy_Lancer_Lunge` (EnemyLungeData) | telegraphSeconds | 1,1 s | mínimo forçado 1 s |
| | lungeDistance | 7 m | máximo; recortado por parede/porta |
| | lungeSpeed | 16 m/s | ~0,44 s para 7 m |
| | hitRadius | 0,75 m | meia largura da faixa = largura visível |
| | recoverSeconds | 0,8 s | janela de punição |
| | obstacleMask | Obstacle + Door | atores e triggers fora |
| | castHeight / wallMargin | 1 m / 0,1 m | |
| | sightInterval | 0,1 s | visão cacheada, como o ranged F07 |

Recompensa: 15 XP e referência somente leitura a `Loot_Melee` (tabela não alterada).
Encontro de teste próprio: `Encounter_LancerTest` com 2 lanceiros.

Perguntas para o playtest: aviso de 1,1 s é legível a 45° com dois lanceiros? A faixa de 1,5 m é
justa contra o raio do jogador? 0,8 s de recuperação dá janela de contra-ataque com o combo? O
dash (0,25 s de invulnerabilidade) é a resposta preferida ou basta sair da faixa?

## Limites conhecidos

- A faixa é validada no compromisso. Uma porta que feche **durante** os ~0,44 s da investida não
  é reconsultada; o NavMesh continua impedindo atravessar paredes assadas. Nas salas atuais as
  portas só fecham na entrada do jogador, antes do spawn.
- Portas (layer Door) não entram no bake do NavMesh e, nas salas de release, não têm
  `NavMeshObstacle`: qualquer agente pode andar pelo vão de uma porta fechada (achado pré-existente,
  ver [Reports/F13](../Reports/F13/README.md)). A sala de teste F13 adiciona carving aos próprios
  bloqueios de porta; release segue inalterada até tarefa própria.
- O lanceiro atravessa visualmente outros inimigos durante a investida (sem física entre atores).
- Sem áudio dedicado: `EnemyAudioBridge` escuta `EnemyCombat`, que o lanceiro não usa.
- Reiniciar pelo HUD na cena de teste não funciona (a cena não está nas Build Settings);
  saia do PlayMode para repetir.
- **A13/rig Generic não integrado**: o conceito 2D A13 existe desde 17/11/2026
  (`Assets/_Project/Art/Concepts/SkeletonLancer_Concept_01.png`, ponta da lança a ~0,9 m do pivô na guarda),
  mas não há modelo, rig nem clip; o catálogo pede o design F13 validado antes de encomendar 3D. `EnemyLungeAttack` publica `TelegraphStarted`,
  `DashStarted`, `TargetHit` (dano aceito, autoritativo), `DashEnded`, `Finished` e `Cancelled`
  para uma futura ponte Generic alinhar a pose de impacto ao dano, sem AnimationEvent de dano.

## Reprodução

Menu `Tools/Dungeon Crawler/Enemies/Build Lancer Test Room (F13)` (idempotente; preserva SOs
ajustados). Abrir `F13_Lancer_Test.unity`, Play, caminhar para o centro da sala.
Evidências em [Reports/F13](../Reports/F13/README.md).
