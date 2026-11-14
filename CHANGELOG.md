# Changelog

## 2026-11-14 — Menus e opções persistentes

- Menu inicial, pausa/opções/retorno e botões de retorno nas telas de vitória/derrota. Pilha de telas mantém foco e oculta o menu anterior; telas terminais selecionam Reiniciar. Build agora inclui MainMenu e o andar, preservando as cenas de teste no Editor.
- JSON versionado em PlayerPrefs armazena volumes, flash/tremor e qualidade/resolução/tela cheia; defaults do SO não mudam. Flash ajustável também nos modelos gerados e tremor ajustável no dano/rugido, preservando a cor persistente da fase 2.
- 90/90 EditMode; suíte inicial PlayMode 62/63 e os três testes de menus passaram na reexecução após corrigir tolerância numérica da fixture. Controle positivo confirma sinal Cinemachine normal e nenhum sinal com intensidade zero. Build Windows Succeeded, 0 erros, 496 avisos de shaders conhecidos.
- Revisão Windows encontrou texto cortado nos dropdowns (fontes maiores que linhas padrão). Corrigido e revalidado: quatro testes de menus, glifos visíveis/seleção por clique e duas execuções Windows. Menu/opções legíveis em 1080p e 720p; aplicar 1280×720/janela, sair e reabrir preservou o ajuste. Configuração anterior 1920×1080/tela cheia restaurada. Jogar abriu o andar; direito acionou pose do especial/custo/cooldown e retorno à locomoção. Sem exceções/Missing Script no log; troca de qualidade/ultrawide e percurso final continuam em QA. Sons/música ainda em implementação.
- Observada diretamente a tela de vitória/andar completo da partida do usuário com inventário/especial no Editor antes de encerrar PlayMode com autorização. Evidência em Reports/Leadership/editor-inventory-special-victory.json.

## 2026-11-14 — Especial e estamina

- Implementado giro autoritativo em 360° com custo 30, cooldown 6 s, estamina 100 e regeneração 20/s após 0,8 s. Dados em PlayerSpecialDefinition; combo e especial respeitam recuperação mútua. Morte/dash/pausa/desativação cancelam impacto; segurar E/direito não repete.
- Clip Special adicionado ao Hero Generic, sem alterar malha, materiais, transformações dos ossos nem os 12 clips existentes (comparação dos dados GLB registrada). Ponte recebe eventos C#; nenhum AnimationEvent causa dano. Prefab/três cenas configurados via Editor, com HUD de recurso.
- 83/83 EditMode e 60/60 PlayMode passaram; teste adicional de medição passou. Consulta aquecida de 32 alvos/64 colliders: 27,817 ms/1.000 execuções, zero GC.Alloc com controle positivo. Comparação de CPU/limites em Reports/Leadership/SpecialPerf.
- Editor aberto em PlayMode: HUD/estamina/cooldown observados durante entrada do usuário. Revisão visual do giro interrompida pelo usuário com Escape; não declarada concluída. Build Windows com especial ainda pendente.

## 2026-11-14 — Inventário jogável e pausa

- Integrados 16 espaços, raridades Comum/Raro/Épico, equipamento manual, poções com Q e atalhos I/Tab/Esc. Pausa cancela ações e bloqueia dano/spawns pendentes, restaurando apenas controles previamente habilitados.
- UI apresenta atributos e solicita comandos; largar uma unidade devolve pickup com identidade/potência preservadas, recusando locais bloqueados. Armas +6/+12 mantidas; armadura e arma épica são dados novos, sem multiplicador oculto de raridade.
- Builder idempotente configura prefab e três cenas por APIs do Editor. 75/75 EditMode e 49/49 PlayMode; checker estático sem erros/avisos. Layout observado em PlayMode com preview identificado; teste de cliques usa New Input System. Aceite Windows desta funcionalidade fica na QA final.

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

## 2026-11-14 — Integração do andar e decisões da 1.0

- Usuário confirmou partida normal Windows concluída com vitória/5 de 5 na build com os reparos do Boss/salas. Log registra cinco ondas e nenhuma exceção/Missing Script. Morte/reinício após derrota Windows e flash/shake visual permanecem na QA final.

- LOOP-001: corrigidas duas falhas reproduzidas de reativação em RoomController/FloorController, preservando referências e reconciliando mortes/progresso sem buscas globais. 54/54 EditMode e 38/38 PlayMode, incluindo ondas/portas/vitória única, mortes enquanto desativado e ranged usando pool real.
- Botão Reiniciar validado com clique New Input System. Usuário confirmou partida normal do Editor concluída com vitória/5 de 5; agente observou a luta em 4/5 e o Boss em fase 2. Run Windows com reparos atuais continua pendente.
- D-002: usuário aprovou integralmente as regras documentadas da 1.0 (inventário/raridades/consumo/equipamento/armadura/especial/estamina/menus/opções). Implementação de R3 autorizada, sem alterar bônus +6/+12 ou fórmula de progressão.

- Confirmação manual posterior: usuário confirmou WASD, Espaço e R funcionando na build Windows R1.
- BOSS-001: pulso da transição ligado ao modelo Warden visível, cor de fase 2 persistente e restauração após flash sem instanciar materiais. Prefab configurado pelas APIs e builder atualizado nas duas cópias. 54 EditMode/32 PlayMode; preview visual branco → vermelho no Editor. Na luta real, observados 4/5 salas, Warden vermelho/telegraph e HUD; vitória não observada. Arena original também revisada em PlayMode; input do botão Reiniciar e flash/shake ainda em validação.

- FIX-004/R1: corrigidas chamadas ao Animator inativo nas duas cópias da ponte do jogador. Regressão reproduzida com três avisos na cena gerada; corrigida com zero avisos, cancelamento sem impacto atrasado e timers funcionando com Animator desabilitado. 53/53 EditMode e 31/31 PlayMode. Build Windows incremental sem erros/avisos e smoke de render/ataque sem Missing Script/exceções/avisos Animator. Cache de shaders não substitui rebuild limpo; teclado/reinício Windows e run completa continuam pendentes de confirmação visual.

## Histórico preservado

- R3 / integração de inventário: PlayerInventory aplica equipamento/defesa e consome poções apenas com jogador vivo/ferido; progressão mantém cálculo/XP/cura de nível. Armor adicionado ao loot preservando índices antigos. Pickup recusado permanece no chão e tenta novamente sem spam de notificações; teste físico prova coleta após liberar espaço. 72/72 EditMode e 43/43 PlayMode. As cenas ainda não receberam o componente/configuração/UI nesta entrega; coleta anterior permanece como compatibilidade nos jogadores sem inventário.

- R3 / RPG-002 em andamento: ItemDefinition/raridades/armadura/poção/configuração e InventoryModel com 16 espaços, pilhas por potência, comandos atômicos e equipamento por índice estável. WeaponData preserva campos/GUID/bônus e compartilha a base de itens. 64/64 EditMode, incluindo assets reais e sequência de 500 comandos; coleta/UI ainda não integradas nesta entrega.

- Combo de três golpes, integração de clips Generic, cancelamentos, buffer e medições anteriores: [Reports/Task02/README.md](Reports/Task02/README.md).
- Auditoria anterior dos marcos originais: [MVP_AUDIT.md](MVP_AUDIT.md).
