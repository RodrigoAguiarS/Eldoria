# Compatibilidade após merge F13

Branch codex/f13-door-compat criada da main 1d91a36 (PR #13), em 17/11/2026.

A integração expôs dois testes EditMode de portas com falha: a sala F13 ainda mantinha carving no bloqueio móvel e a referência de raiz não estava configurada. A migração remove obstáculos descendentes do bloqueio e configura somente a raiz estática; LancerTestSceneBuilder chama o mesmo helper, impedindo recorrência ao reconstruir o experimento. Prefab Room_LancerTest atualizado por APIs do Editor; GUID/encounter/atores/bake/primeiro andar preservados.

Validação no workspace de integração Unity 6000.6.9f1: 35 casos F13/portas aprovados, 1 captura explícita ignorada no batch, zero falhas no subconjunto listado em tests.json. Suítes pertinentes totais: 66/66 EditMode e 35 PlayMode aprovados (1 captura explícita ignorada). Escopo total também contém F14; não atribuir sua implementação a esta correção. Não houve build ou validação gráfica específica desta correção.

Arquivos: DoorNavMeshBlockerBuilder.cs, LancerTestSceneBuilder.cs e Room_LancerTest.prefab. Merge autorizado pelo pedido de resolver e integrar a branch F13; complemento do PR #13.
