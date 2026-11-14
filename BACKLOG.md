# Backlog — Dungeon Crawler

PO/Tech Lead: Codex. Escopo e dependências: [ROADMAP.md](ROADMAP.md). Classificação inicial: [auditoria](docs/MVP-AUDIT.md). Status: A fazer → Em andamento → Implementado, validação pendente → Concluído e validado; Bloqueado exige impedimento concreto.

| ID | Prioridade / épico | Tarefa e critério de aceite | Dependências | Status |
| --- | --- | --- | --- | --- |
| AUD-001 | P0 / Fundação | Auditar scripts/assets/cenas, com foco na arena; versões/refs, testes e estados rastreáveis | — | Concluído e validado: auditoria inicial documentada; questões visuais/build registradas |
| REP-001 | P0 / Fundação | Criar Git local; caches/logs/dados locais fora; commit inicial e commits por entrega, preservando metas | AUD-001 | Concluído e validado: cf3720d; branch codex/mvp-stabilization, sem remoto |
| FIX-001 | P0 / Loot | Separar WeaponData.cs e reparar os dois assets via Editor; preservar GUID/nome/bônus; tabelas apontam para assets reais; teste de equipamento/build | AUD-001 | Concluído e validado: 51 EditMode, 28 PlayMode, Windows sem Missing Script |
| FIX-002 | P1 / Renderização | Rastrear/limpar recursos órfãos/refs de render que causam avisos; preservar aparência; verificar Editor e Windows | AUD-001 | Concluído e validado: checker 0 erros, 19 componentes preservados, smoke Windows sem avisos de pós-processamento |
| FIX-003 | P1 / Integração | Builder M1 não apaga dados/recompensas do Floor; reexecução não duplica nem muda GUIDs/composição | REP-001 | Concluído e validado: 53 EditMode; reconstrução M1 executada duas vezes, prefabs compartilhados intactos |
| FIX-004 | P1 / Combate | Cancelamento ao desativar não aciona Animator inativo; sem dano/evento atrasado ou inscrição duplicada após reinício | AUD-001 | A fazer |
| PERF-001 | P1 / Pool | Aquecer projéteis, garantir reuso e cancelamento/limpeza; provar rajada dentro da capacidade sem criar instâncias | R1 / FIX-001 | A fazer |
| BOSS-001 | P1 / Boss | Visual de fase 2 no Warden visível, sem mudar regras 50%/3 s/transição única; tests e captura no jogo | FIX-001 | A fazer |
| LOOP-001 | P1 / Dungeon | Integrar 4 salas: porta bloqueia, spawn correto, morte única, reabertura só ao limpar; ranged/elite atuam | FIX-001, FIX-003 | A fazer |
| LOOP-002 | P1 / Aceite | Run normal até Boss/vitória e morte/reinício em Editor/Windows; gravação de evidências e erros/avisos | LOOP-001, BOSS-001, FIX-002, FIX-004, PERF-001 | A fazer |
| QA-001 | P1 / Arena | Conferência visual específica da MVP_Arena: câmera, HP, flash, shake, dicas e painéis | AUD-001 | A fazer; testes funcionais de cena já passaram |
| PROD-001 | P1 / Produto | Fronteira de 1.0 e regras do especial/recurso/defesa/inventário; distinguir expansão de requisito existente | ROADMAP | Fronteira aprovada com inventário/raridades; especificação dos incrementos pendente |
| RPG-001 | P2 / Gameplay | Implementar especial configurável e recurso/defesa desacoplados; sem alterar progressão existente sem decisão | PROD-001, LOOP-002 | Planejado |
| RPG-002 | P2 / RPG | Inventário interativo e raridades configuráveis, coleta/equipamento/consumo e UI; provar limites e integridade dos itens | PROD-001, FIX-001, LOOP-002 | Obrigatório da 1.0, aprovado pelo usuário |
| UI-001 | P2 / Interface | Menu inicial/pausa/opções/retorno, foco/escala; UI apresenta estado e não decide combate | PROD-001, LOOP-002 | Planejado |
| AUDIO-001 | P2 / Apresentação | Sons de ataque/impacto/dano/morte/coleta/Boss e música, com volume/ciclo de vida configuráveis | LOOP-002 | Planejado |
| ACCESS-001 | P2 / Apresentação | Shake/flash ajustáveis e telegraph legível por forma/tempo além de cor, opções persistentes | UI-001 | Planejado |
| VFX-001 | P2 / Game feel | Efeitos/hit-stop que preservem legibilidade; reuso/pool se houver efeitos transitórios; medir antes/depois | PROD-001, LOOP-002 | Planejado; adicionar só se necessário ao aceite |
| BAL-001 | P2 / Balanço | Definir alvos após playtest; tabela de dano/tempo/XP e mudanças configuráveis com aprovação quando fundamentais | LOOP-002 | Planejado |
| PERF-002 | P2 / Distribuição | Captura CPU/GPU/frame time/GC/memória na mesma carga/hardware, diagnosticar antes de otimizar | LOOP-002 | Planejado |
| SHIP-001 | P2 / Distribuição | Windows release reproduzível, smoke test, percurso completo e instruções de execução; configurações gráficas | R3, PERF-002, BAL-001 | Planejado |
| EXP-001 | Opcional / RPG | Slots adicionais de armadura e afixos complexos | Decisão de produto, RPG-002 | Inventário/raridades movidos para RPG-002; esta expansão não é necessária para a primeira versão |
| EXP-002 | Opcional / Combate | Ataque ranged do jogador, armas correspondentes e novos clips | Decisão de produto | Fora da base atual |
| EXP-003 | Opcional / Conteúdo | Novos andares/geração procedural/save de campanha/quests/coop/lojas | Decisão de produto | Fora da base atual |

## Registro da execução e retomada

- AUD-001: documentos e versões conferidos; snapshot/backup; 155 assets inspecionados no Editor; 45 EditMode e 21 anteriores + 6 testes novos de cena aprovados. Três falhas iniciais eram das fixtures e foram corrigidas sem mudar gameplay.
- Estado seguro inicial: nenhum arquivo desconhecido alterado desde o snapshot; projeto sem Git no início; Editor fechado normalmente para executar batchmode.
- REP-001: Git inicializado em main, commit cf3720d, trabalho na branch codex/mvp-stabilization. 978 arquivos incluídos; nenhuma pasta de cache/build/log, configuração local de assistente ou token detectado pela triagem entrou no commit.
- D-001: usuário aprovou inventário e raridades na 1.0; decisão incorporada no roadmap, sem interromper a estabilização.
- FIX-001: WeaponData extraído, armas reparadas via AssetDatabase com GUIDs/nome/bônus preservados, tabelas resolvidas; seis testes novos EditMode e uma prova física de coleta de arma/XP na arena. Suítes completas: 51/51 EditMode e 28/28 PlayMode. Build Windows Succeeded, 0 erros; log do executável tem 0 Missing Script/0 exceções e 4 avisos URP. Build tem 496 avisos de shaders do pacote de inferência Unity; tratar fora da correção de loot.
- FIX-002: nove subassets sem script removidos pela API dedicada; 19 componentes de volume e renderer features preservados; sete referências de debug obsoletas limpas. Build Windows com 0 erros; executável sem os quatro avisos de pós-processamento. Recursos adicionais aumentaram o tamanho reportado em 2.952.880 bytes. Os 496 avisos de shaders de inferência e a mensagem de info queue D3D12 do driver seguem registrados, sem exceções.
- FIX-003: M1 reaproveita Player/Enemy_Melee existentes e recusa sobrescrever arquivo que não carrega. Dois testes protegem recompensas/combo/GUIDs/bytes dos prefabs e executam duas reconstruções completas com um jogador e um inimigo. 53/53 EditMode passaram. O teste restaura exatamente os assets de cenário/decoração usados como fixture, sem manter alterações de arte/file IDs geradas pelo builder.
- Próxima ação exata: PERF-001 e FIX-004, depois BOSS-001 e LOOP-001. As configurações do pacote de inferência podem adicionar automaticamente SENTIS_ANALYTICS_ENABLED ao abrir o Editor; essa alteração de define não é parte das entregas de gameplay e foi identificada no código do pacote.
