# Changelog

## 2026-11-13 — Auditoria e condução técnica

- Assumida a coordenação técnica/PO nos limites do GDD/AGENTS e da solicitação do usuário; decisões significativas de produto continuam sujeitas a aprovação.
- Criados docs/MVP-AUDIT.md, ROADMAP.md e BACKLOG.md com provas, prioridades e limites de 1.0 propostos.
- Criado MvpProjectAudit para inspecionar referências/hierarquias pelo Editor, incluindo assets binários, sem gravar cenas/prefabs.
- Acrescentados seis testes de aceitação que carregam Bootstrap/MVP_Arena e exercitam input, movimento, mira, dash, combate, navegação, morte e reinício. Todos passaram após corrigir isolamento/ordem de input das fixtures.
- Benchmark existente aceita diretório de saída por variável de ambiente, preservando os relatórios históricos da Task 02 ao executar a auditoria.
- Estado inicial: 45 testes EditMode passaram; os 21 testes PlayMode anteriores e os seis novos passaram. A auditoria registrou armas inválidas, integração visual do Boss, pool frio e recursos de renderização não resolvidos. Nenhuma nova funcionalidade de gameplay foi adicionada nesta etapa.

## Histórico preservado

- Combo de três golpes, integração de clips Generic, cancelamentos, buffer e medições anteriores: [Reports/Task02/README.md](Reports/Task02/README.md).
- Auditoria anterior dos marcos originais: [MVP_AUDIT.md](MVP_AUDIT.md).
