# F12 — Talento por nível: evidências e limites

Base `151c675` (main), branch `claude/f12-level-talents`, Unity 6000.6.9f1 em
batchmode `-nographics`. Regras e valores: [TALENTS-F12](../../docs/TALENTS-F12.md).

## Resultados executados

| Arquivo | Escopo | Resultado |
| --- | --- | --- |
| [editmode-full-final.xml](editmode-full-final.xml) | EditMode completo (código final) | **227/228**; 1 falha pré-existente da F10 (abaixo) |
| [editmode-full-1.xml](editmode-full-1.xml) | EditMode completo, 1ª execução | 227/228, mesma falha |
| [playmode-full.xml](playmode-full.xml) | PlayMode completo, 1ª execução | 162 aprovados, 1 falha F12 (corrigida), 11 capturas explícitas ignoradas |
| [playmode-retest.xml](playmode-retest.xml) | F12 + checkpoint + menus/pausa/escala após correção | 23/23, 1 captura ignorada |
| [editmode-f12-final.xml](editmode-f12-final.xml) | Talentos, checkpoint, progressão, comparação F01, especial | 71/71 |
| [playmode-committed-state.xml](playmode-committed-state.xml) | F12, checkpoint, menus, pausa, escala, aceite M1, andar | **37/37**, 1 captura ignorada |

`committed-state.sha256` registra prefab, três cenas e catálogo de checkpoint
commitados; o PlayMode final rodou sobre esses bytes e não os alterou. Uma nova
execução do builder sobre esse estado não muda nenhum byte (ponto fixo).

A falha do 1º PlayMode completo vinha do próprio teste novo: `Restart()` recusou
porque o diretório isolado do checkpoint não existia e `File.Delete` lança
`DirectoryNotFoundException`. O teste agora cria o diretório. Em produção
`persistentDataPath` sempre existe; o comportamento do F11 não foi alterado.

## Cobertura do aceite

- **+10%/nível íntegro:** `DamageMultiplier` permanece 1,1/1,2/1,3; talento é
  `TalentDamageMultiplier` separado. Ferro +6 no nível 2 com Lâmina 1 = 26 × 1,1 × 1,05.
- **Cura de nível e armas:** dois níveis numa coleta curam 2 × 40% e emitem um
  `LeveledUp`; bônus da arma inalterado. Asserts confirmam que as definições de
  especial e armadura não são editadas pelos talentos.
- **Escolha única / repetida:** cada escolha aceita gasta uma; sem pendência é recusada;
  duplo clique dentro de 0,3 s não gasta a segunda escolha enfileirada.
- **Sem reentrância:** `TryChoose` chamado dentro de `Chosen` é recusado.
- **Reativação:** desabilitar/reabilitar talentos e inventário 5× mantém dano,
  defesa e cooldown idênticos (atribuição absoluta).
- **Cancelar UI:** Esc/Voltar retorna à Pausa sem alterar ranks ou pendências.
- **Morte/reinício:** morto não escolhe nem abre a tela; menus fecham. Reiniciar
  cria jogador sem talentos e multiplicadores 1.
- **Retorno ao estado base:** restauração vazia zera os três termos; desequipar
  armadura deixa só o termo do talento.
- **Checkpoint v2:** round-trip de ranks; v1 sem talentos válido e migrado;
  v1 com talentos, ID desconhecido/duplicado, rank 0 ou acima do máximo, soma acima
  de `nível − 1` e entrada nula rejeitados antes de mutar.

## Limites e pendências

- **NÃO VALIDADO VISUALMENTE.** Nenhuma captura gráfica nem inspeção da tela de
  talentos, do botão na Pausa ou do aviso no HUD em 720p/1080p/150%. A geometria
  vem do builder (reserva para 150%), mas não foi observada.
- **Sem playtest humano nem medição de balanceamento.** Os valores são a proposta
  inicial; não se afirma melhoria de variação ou dificuldade.
- Build Windows não gerada nesta entrega.
- **Falha pré-existente (já na main, registrada no F11):**
  `ItemsCombatAssetTests.IntegrateTwice_IsIdempotentAndPreservesPickupIdentity`
  com `Inventory slot missing: 0` (`TextScaleSceneBuilder` move os slots).
- Testes de builder antigos (F09/F11/arte) reescrevem cenas, `MainMenu`,
  `Mat_Vase_F09.mat` e `Reports/Leadership/ProductionVisual/validation.json`
  durante o EditMode; esses efeitos foram descartados e o builder F12 foi reaplicado.
  Abrir o Editor acrescenta `SENTIS_ANALYTICS_ENABLED` em `ProjectSettings`;
  revertido, não faz parte desta entrega.
- Componente recém-adicionado recebe `fileID` novo quando o builder parte de uma
  base sem F12; sobre estado já integrado o resultado é idêntico.

## Reprodução

```
Unity.exe -batchmode -nographics -projectPath <projeto> -executeMethod DungeonCrawler.Editor.TalentSceneBuilder.Integrate -quit -logFile <log>
Unity.exe -batchmode -nographics -projectPath <projeto> -runTests -testPlatform PlayMode -testFilter "TalentSceneTests|PlayerTalentsPlayModeTests" -testResults <xml> -logFile <log>
```

Menu equivalente: `Tools/Dungeon Crawler/Progression/Integrate Level Talents (F12)`.
