# F09 — vasos destrutíveis simples

Base `6fdf184`, main atualizada; branch `codex/f09-destructible-props`.
Unity 6000.6.9f1, URP 17.6.0; versões de pacotes preservadas.
Escopo e aceite em [PLAN](PLAN.md).

Um vaso de primitivas por sala de release, 40 HP configuráveis em
`Prop_Vase_F09.asset`. Dois colliders compartilham um IDamageable; básico/especial
mantêm deduplicação e usam a máscara adicional Destructible. Layer sem colisão
física, fora das máscaras de cobertura/projéteis/NavMesh; vasos nas laterais,
centro livre. Sem drops, XP, sorteios, estilhaços físicos ou participação na onda.
Uma quebra troca o visual por destroços estáticos previamente compostos.
DamageFlash existente apresenta dano não letal e respeita a opção de flash.

HP e estado terminal são locais à instância e confirmados antes dos eventos;
reentrada durante notificações é recusada. Desativação/reativação não restaura o
vaso; recarregar a cena cria outro. A apresentação reconcilia o estado ao voltar.
Recompensas futuras precisam de dados separados da definição do vaso.

## Evidências

- [Edit01/results.xml](Edit01/results.xml): **163/163 EditMode**, zero falhas.
  Dois casos novos verificam composição idempotente byte a byte, GUIDs,
  referências/Missing Script, máscaras, dados/prefabs de salas/NavMesh preservados,
  iluminação e distâncias de portas/spawns. Os pares de colisão entre outras
  layers permanecem iguais.
- [Play01/results.xml](Play01/results.xml): **146 aprovados, zero falhas**,
  10 capturas explícitas ignoradas em batch. Inclui 14 casos funcionais novos:
  colliders duplicados/buffer cheio, básico/giro no mesmo frame, máscaras/cone,
  dano inválido, pausa/desativação, morte/reentrada e apresentação após toggle/
  recarga. A integração pelas cinco ondas confirma vitória com vasos intactos.
- [Visual02/results.xml](Visual02/results.xml): **1/1** no Editor gráfico,
  **2560×1440**, cena real de release. Básico levou 40→20 HP; giro quebrou o
  vaso pelo relógio real de impacto. Imagens intacto/danificado/quebrado foram
  abertas e inspecionadas; vaso e três destroços ficam legíveis ao lado do Hero.
  [Contexto](Visual02/context.txt), [intacto](Visual02/vase-intact.png),
  [quebrado](Visual02/vase-broken.png). Visual01 também passou; motivou somente
  um ajuste de enquadramento porque o personagem ocultava os destroços.
- [Checker final](unity-static-final.json): zero erros/avisos de GUIDs/metas/scripts.
  Compilação das suítes e das duas capturas sem erros C#.
- [Resumo](validation-summary.json): 872 assets existentes preservados na
  comparação canônica Git, além dos pacotes e relatórios históricos. Somente a
  cena de release e o prefab Player mudam entre os assets de conteúdo existentes;
  dados/material/scripts/metas de F09 são novos. [Auditoria](audit-final.json):
  fontes/cópias sincronizadas e zero links locais quebrados.

O Test Runner completo inclui builders antigos que reserializam outros assets
e geram um relatório no destino histórico ProductionVisual. Essas saídas são
inspecionadas e restauradas à baseline; a cópia nova da validação fica em F09.
DynamicsManager é gravado pela Unity no schema 24 para persistir a nova matriz;
não é alteração de versão do Editor nem de parâmetros de física.

## Reprodução

1. Menu **Tools / Dungeon Crawler / Environment / Integrate Destructible Vases
   (F09)**, ou `DungeonCrawler.Editor.DestructiblePropSceneBuilder.Integrate`.
   Reexecutar preserva definição/posição existente e não duplica objetos.
2. Abrir a versão atual pelo menu principal. Em cada sala, localizar o vaso
   lateral em posição local `(-5.5, 0, -2)`. Básico inicial dá 20 de dano; giro
   inicial dá 40. Arma/nível/combo mantêm os multiplicadores existentes.
3. Quebrar, pausar/continuar e desativar/reativar: não há dano repetido, drops ou
   restauração de HP. Reiniciar produz vasos intactos. Deixar vasos vivos não
   impede abrir portas/concluir o andar.
4. Testes: `DestructiblePropAssetTests`, `DestructiblePropTests` e regressões de
   combate/salas. A captura explícita
   `DestructiblePropTests.Capture_ReleaseVaseBeforeAndAfterActualAttacks` exige
   Editor gráfico, sem `-batchmode`/`-nographics`. Usar diretório novo em
   `DUNGEON_CRAWLER_TEST_REPORT_DIR` para cada execução.

Posicionamento/invulnerabilidade nas capturas e mortes controladas das ondas
nos testes de integração são diagnósticos. Não representam aceite humano de
uma partida normal, arte definitiva ou nova distribuição Windows.
