# Especial — medição e limites

> Registro histórico da execução indicada. Afirmações de ausência de Git/arte/sistemas e pendências descrevem aquela data; consultar [PROJECT_AUDIT](../../../PROJECT_AUDIT.md) para estado atual. Medições e limites abaixo foram preservados; não são testes reexecutados por esta revisão documental.

Unity 6000.6.9f1, Mono no Editor, batchmode/nographics; mesma máquina das medições InventoryUiPerf. Não é perfil de frame/GPU da versão distribuível.

| Consulta | Carga após aquecimento | CPU total | GC.Alloc |
| --- | --- | --- | --- |
| Básico antes do especial (InventoryUiPerf/after_profiled.json) | 10.000 consultas, 32 alvos / 64 colliders, 200 aquecimentos | 409,228 ms | 0 |
| Básico integrado com especial (after_profiled.json) | Mesma carga e aquecimento | 397,292 ms | 0 |
| Giro 360° (special-query.json) | 1.000 consultas, 32 alvos / 64 colliders, 100 aquecimentos | 27,817 ms | 0 |

Variação de uma amostra não demonstra ganho de CPU; o giro não faz o filtro de cone do básico e não serve como substituto equivalente para comparar desempenho. O teste do giro confirma controle positivo de uma alocação e armazenamento reaproveitado após saturação. Tempos incluem aplicação de dano e notificações de Health nos alvos de teste, sem Animator/UI.

O contador de bytes do thread não detecta a alocação de controle nesta instalação; a conclusão de zero alocações usa contagem de amostras do ProfilerRecorder GC.Alloc. A medição final de CPU/GPU/frame/memória em build continua em R4.
