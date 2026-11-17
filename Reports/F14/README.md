# F14 — primeira sala do segundo andar fixo

Base original: main `1d91a36` (merge F13 pelo PR #13). Branch: `codex/f14-second-fixed-floor`.
Unity 6000.6.9f1, pacotes preservados. Proposta registrada antes do código: [PROPOSAL](PROPOSAL.md).

## Entrega

- Primeiro andar preserva cinco salas/encontros/arte. Depois de concluir, **Descer ao segundo andar** mantém HP, nível/XP, ouro, bolsa exata, equipamento, talentos pendentes/escolhidos e recursos; sem cura/XP/loot extra. Coletar antes: sem retorno e loot no chão não acompanha.
- Cena `Floor_02_Fixed`: uma sala de primitivas de 20×20, dois pilares laterais e quatro inimigos existentes (2 melee + 2 ranged). Sem novo Boss, geração procedural, arte final ou regeneração de personagens. Uma sala é o primeiro incremento, não o segundo andar completo de cinco salas.
- SO de sequência com IDs/catálogos/cenas explícitos, referenciado por prefab de configuração para sobreviver à serialização binária das cenas. Checkpoint v3 admite chegada segura antes do trigger; v1/v2 continuam legíveis. Menu escolhe principal válido em todos os andares antes de backup.
- Transição valida destino/estado/escrita e bloqueia repetição. O checkpoint antigo não pode sobrescrever chegada. A instância do jogador que parte ignora impactos tardios do mesmo frame; a chegada não herda invulnerabilidade. Falhas preservam run atual. Reinício/nova run voltam ao primeiro andar; morte descarta checkpoint/backup.

## Validação

Resultados finais são os XML nomeados `*-final` e os retestes específicos; execuções anteriores ficam preservadas para mostrar falhas reproduzidas/correções. 66/66 EditMode pertinentes aprovados: F14, checkpoint, F13 e portas, incluindo builder idempotente e comparação de bytes de prefabs/dados protegidos e cenas.

35 PlayMode pertinentes aprovados, zero falhas (1 captura F13 explícita ignorada no batch), em `PlayMode-final.xml`. Após retirar a barra de Boss herdada e encurtar o título de vitória, 9/9 EditMode F14 passaram (`EditMode-visual-fix.xml`).

Percurso gráfico completo aprovado duas vezes, com a apresentação final em `PlayMode-traversal-verified.xml` e `TraversalVerified/`: menu → nova run → cinco salas e Boss → descida → sala F14 → vitória. Movimento conduzido por PlayerMovement/CharacterController a 5 m/s e mira automatizada; dano por combo/giro real, IA ativa, coleta por colisores, bolsa/equipamento/talentos normais. Não usa teleporte, conclusão forçada ou dano sintético nessa prova. A proteção de um frame da instância que parte é a própria regra da transição; não há invulnerabilidade diagnóstica no combate. Primeiro andar terminou nível 5, HP 100, ouro 127; segundo terminou nível 6, HP 93,04, ouro 141. Esses números são observações dessa execução, não balanceamento aprovado.

Capturas 2560×1440 inspecionadas: primeiro andar concluído, chegada, quatro inimigos ativos na arena de primitivas e vitória F14. A primeira captura revelou a barra de Boss indevida; ela foi removida por builder e a captura refeita. Os textos novos usam os baselines do UiTextScale existente, sem reconfigurar o layout antigo do menu.

Build Windows x64 aprovada: `Builds/F14/Eldoria.exe`, zero erros, 496 avisos de shader, 38,04 s (`windows-build.json`). Contém MainMenu e os dois andares. Executável PE AMD64 e SHA256 registrados em `WindowsSmoke/executable.json`.

Smoke da build por UI: menu Eldoria aberto em 1920×1080 (opção atual prevaleceu sobre o argumento inicial de janela), **Nova run** abriu primeiro andar com nível 1, XP/ouro zero e bolsa vazia; clique executou ataque/animação/trail. Nenhuma exceção/Missing Script no log. Teclas sintéticas de Escape não abriram a pausa nesta sessão: pausa Windows por teclado real e percurso completo no executável permanecem pendentes; não inferir regressão do jogo sem reprodução humana. Checkpoint do smoke isolado em `WindowsSmoke/Checkpoints`, sem tocar o checkpoint do usuário.

Depois do build/registro de escala dos novos textos, 9/9 EditMode F14 passaram novamente (`EditMode-postbuild.xml`).

## Regressões encontradas

1. F13 após merge mantinha carving no bloqueio móvel e não preenchia a nova referência de raiz estática. Dois testes de portas falharam. A migração remove o obstáculo filho e configura a raiz; o builder do lanceiro passa a usar o mesmo contrato, impedindo recorrência. Correção isolada em `codex/f13-door-compat`, integrada na main pelo [PR #14](https://github.com/RodrigoAguiarS/Eldoria/pull/14); conflitos originais integrados pelo [PR #13](https://github.com/RodrigoAguiarS/Eldoria/pull/13).
2. Referência direta do novo SO de sequência não sobreviveu à cena binária. Cinco testes F14 PlayMode falharam. A configuração em prefab e injeção por componente substituem essa referência, com teste de carga real.
3. API `objectReferenceInstanceIDValue` é erro de compilação no Editor 6000.6; o teste usa `objectReferenceEntityIdValue`, conforme API já empregada no projeto.

## Reprodução

Builder: `DungeonCrawler.Editor.SecondFloorSceneBuilder.Integrate`, menu **Tools / Dungeon Crawler / Dungeon / Integrate Second Fixed Floor (F14)**. Reexecutar não duplica objetos nem reconstrói dados/layout existente. `SecondFloorSceneBuilder.BuildWindowsValidation` escreve somente build/relatório isolados em `Builds/F14` e `Reports/F14/windows-build.json`, recusando sobrescrever evidência.

Testes pertinentes: `SecondFloorTests`, `SecondFloorPlayModeTests`, `RunCheckpointTests`, `RunCheckpointPlayModeTests`, `FloorIntegrationTests`, `DoorNavMeshBlockerAssetTests`, `DoorNavMeshBlockingTests` e F13 (`Lancer*`/`LungeTimelineTests`). Usar sempre nova pasta/nomes de resultado e `DUNGEON_CRAWLER_TEST_REPORT_DIR` exclusivo.

## Limites

Os testes de fronteira usam conclusão/teleporte/dano sintéticos; o percurso gráfico separado usa combate e movimento reais com condução automatizada. A automação não substitui uma partida humana nem aprova dificuldade, sensação dos controles ou arte final. Números da nova composição são proposta de playtest; dados de combate existentes não foram rebalanceados. A15 não foi executada. Revisão artística geral, hardware adicional e expansão para demais salas/Boss permanecem fora deste incremento.

Efeitos conhecidos dos testes foram separados do incremento: o builder F11 alterou tamanhos/posição do menu; a cena original foi restaurada e somente a composição F14 reaplicada. A compilação assíncrona de shaders foi restaurada após captura; o define SENTIS_ANALYTICS_ENABLED introduzido pelo pacote foi removido do diff final. Nenhuma evidência histórica foi sobrescrita.

## Arquivos e continuidade

Runtime: GameSceneFlow/GameRestarter, FixedFloorSequence/FixedFloorConfiguration, FloorTransitionController/Presenter e catálogo/controller/payload de checkpoint. Editor: SecondFloorSceneBuilder e inclusão da cena em MvpReleaseBuild; helper BuildRoomPrefab apenas exposto internamente. Assets novos: cena/sala/encontro/material/catálogo/sequência/configuração F14. Cenas antigas recebem somente composição e botão; MainMenu mantém seu layout anterior. Documentação: regras, arquitetura, MVP, fila, changelog, checkpoint e build.

Próximo passo: revisar PR F14; para validar manualmente, abrir Builds/F14/Eldoria.exe e percorrer até o Boss, coletar recompensas e descer pelo painel. Não fazer merge F14 sem revisão do usuário. Demais salas/Boss, A15 e balanceamento humano continuam separados.
