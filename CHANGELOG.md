# Changelog

## 2026-11-13 — Auditoria e condução técnica

- Assumida a coordenação técnica/PO nos limites do GDD/AGENTS e da solicitação do usuário; decisões significativas de produto continuam sujeitas a aprovação.
- Criados docs/MVP-AUDIT.md, ROADMAP.md e BACKLOG.md com provas, prioridades e limites de 1.0 propostos.
- Criado MvpProjectAudit para inspecionar referências/hierarquias pelo Editor, incluindo assets binários, sem gravar cenas/prefabs.
- Acrescentados seis testes de aceitação que carregam Bootstrap/MVP_Arena e exercitam input, movimento, mira, dash, combate, navegação, morte e reinício. Todos passaram após corrigir isolamento/ordem de input das fixtures.
- Benchmark existente aceita diretório de saída por variável de ambiente, preservando os relatórios históricos da Task 02 ao executar a auditoria.
- Estado inicial: 45 testes EditMode passaram; os 21 testes PlayMode anteriores e os seis novos passaram. A auditoria registrou armas inválidas, integração visual do Boss, pool frio e recursos de renderização não resolvidos. Nenhuma nova funcionalidade de gameplay foi adicionada nesta etapa.
- Histórico Git local criado (cf3720d), com exclusões e triagem de arquivos preparados; branch codex/mvp-stabilization, sem remoto/publicação.
- Decisão D-001: usuário incluiu inventário interativo e raridades no escopo obrigatório da 1.0.

## 2026-11-13 — FIX-001: armas serializadas

- Extraída WeaponData para o arquivo de mesmo nome, mantendo namespace/campos e LootTable.cs.meta.
- Reparados Weapon_IronSword e Weapon_WardenBlade por script idempotente do Editor. GUIDs, nomes, bônus e referências das tabelas preservados; nenhuma edição textual dos assets foi usada no reparo.
- Acrescentados seis testes EditMode de assets/tabelas/equipamento/idempotência e teste PlayMode de coleta física de arma/XP na arena real.
- Validação: 51/51 EditMode e 28/28 PlayMode; build Windows release Succeeded, 0 erros; executável iniciado e ataque observado, sem Missing Script/exceções. Ainda há quatro avisos URP e avisos de compilação dos shaders de inferência Unity. Não representa aceite completo do andar.
- Criado MvpReleaseBuild para gerar Builds/WindowsMVP/DungeonCrawler.exe sem alterar a lista compartilhada de cenas; inspeção corrente de assets guardada separadamente da auditoria inicial.

## 2026-11-13 — FIX-002: recursos URP

- RenderAssetRepair removeu nove subassets de teste sem scripts carregáveis e sete referências obsoletas de debug pelas APIs do Editor, preservando GUIDs, os 19 componentes válidos/parâmetros do volume e renderer features.
- Mantidos recursos de pós-processamento usados pelos construtores URP; demais flags de stripping preservadas. Checker estático: zero erros/avisos. Build Windows com zero erros; smoke visual confirmou herói/cenário/ataque e ausência dos quatro avisos de pós-processamento, Missing Script e exceções.
- Tamanho reportado passou de 136.970.135 para 139.923.015 bytes. Permanecem 496 avisos de shaders de inferência e mensagem de info queue D3D12 do driver; nenhuma alteração de gameplay ou nova arte. Percurso completo continua pendente.

## 2026-11-13 — FIX-003 e PERF-001

- Builder M1 preserva prefabs compartilhados existentes, incluindo recompensas e combo; arquivo inválido existente gera erro sem sobrescrita. 53/53 EditMode, com duas reconstruções completas e comprovação de atores únicos/GUIDs/bytes de prefabs intactos. Fixtures de cena/decoração restauradas ao terminar o teste.
- Pool aquece 16 projéteis, reutiliza o callback e cancela tiros ao desativar. 30/30 PlayMode; Profiler com controle positivo: 5.000 → 0 alocações em 5.000 disparos, primeira rajada com 0 novas instâncias. Corrigida a instrumentação: contador de bytes do thread não detecta alocação de controle nesta instalação. Melee revalidado com Profiler: 0 alocações após aquecimento; sem ganho de CPU. Evidências em Reports/Leadership/PoolPerf.

## Histórico preservado

- Combo de três golpes, integração de clips Generic, cancelamentos, buffer e medições anteriores: [Reports/Task02/README.md](Reports/Task02/README.md).
- Auditoria anterior dos marcos originais: [MVP_AUDIT.md](MVP_AUDIT.md).
