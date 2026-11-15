# PERF-001 — pool e controle de alocações

> Registro histórico da execução indicada. Afirmações de ausência de Git/arte/sistemas e pendências descrevem aquela data; consultar [PROJECT_AUDIT](../../../PROJECT_AUDIT.md) para estado atual. Medições e limites abaixo foram preservados; não são testes reexecutados por esta revisão documental.

Unity 6000.6.9f1, Editor Windows batchmode, mesmo projeto/hardware, 16 projéteis por rajada, 200 disparos de aquecimento e 5.000 disparos sequenciais medidos. Tempos por Stopwatch, contagem de alocações pelo ProfilerRecorder `GC.Alloc` no thread atual; controles detectaram uma alocação de byte[4096]. Medições isoladas, sem inferência de FPS ou ganho garantido em build.

| Medida | Antes | Depois |
| --- | ---: | ---: |
| Instâncias inativas após Initialize | 0 | 16 |
| Instâncias criadas na primeira rajada | 16 | 0 |
| CPU de Initialize (ms) | 0,4709 | 0,4885 |
| CPU da primeira rajada (ms) | 2,4852 | 0,4895 |
| CPU de 5.000 disparos aquecidos (ms) | 11,9206 | 10,1984 |
| GC.Alloc: número de alocações nos 5.000 disparos | 5.000 | 0 |

Arquivos comparáveis: projectile-pool-before-profiler.json e projectile-pool-after-profiler.json. O aquecimento desloca criação para a inicialização; nenhuma afirmação de redução do custo total de carregamento. O callback de devolução agora é criado uma vez. A desativação devolve todos os projéteis em voo; reativar não retoma tiros antigos. Testes verificam reuso de 16 instâncias distintas e devolução idempotente.

**Correção da instrumentação:** GC.GetAllocatedBytesForCurrentThread retornou zero até para o controle byte[4096] nesta execução. Os campos *GcBytes/threadCounterBytes são preservados como dados brutos, mas não demonstram ausência de alocação. GC.Alloc.Value representa tempo do marcador; a medição usa GC.Alloc.Count, como o pacote Unity Performance Testing instalado. O primeiro teste da instrumentação falhou porque comparava o tempo do marcador com bytes; isso foi corrigido antes de comparar versões.

O benchmark melee foi repetido com o contador validado: consulta antiga 121,8859 ms e consulta atual 422,3702 ms para 10.000 iterações/32 alvos/64 colliders, ambas com **zero alocações** no Profiler. A antiga trunca a consulta em 16 colliders; a atual resolve a multidão inteira e deduplica colliders. Não é um ganho de CPU. Resultados históricos da Task02 permanecem intactos; a prova atual de ausência de alocações vem do Profiler com controle positivo.

Validação: PlayMode-pool.xml, 30/30. Captura CombatProfiler.raw e logs completos locais são ignorados pelo Git; os JSONs e XMLs são versionados. Testes EditMode da entrega anterior: 53/53. Perfil de frame completo CPU/GPU/memória e run normal do andar permanecem pendentes.
