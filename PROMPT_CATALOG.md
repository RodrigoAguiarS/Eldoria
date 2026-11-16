# Catálogo de prompts — Eldoria

Base auditada: `5873d7a`, atualizada após o handoff `aff1895` em 14/11/2026. **Nenhum prompt foi executado nesta missão.** F01–F15 são sugestões novas ou complementos identificados; A01–A15 são briefs artísticos. Escolher um ID por entrega. P1 = próximo incremento útil; P2 = expansão/polimento; P3 = mudança ampla. Complexidade B/M/A = baixa/média/alta relativa a este projeto, sem promessa de prazo.

O jogo já tem combo de três golpes, dash, especial/estamina/defesa, inventário de 16 slots, três raridades, equipamento manual, poções/Q, ouro/XP, quatro salas + Boss de duas fases, menus/opções, áudio, arte de itens, chama animada e VFX de combate. Não encomendar esses sistemas como novidade. Evidências/limites: [PROJECT_AUDIT](PROJECT_AUDIT.md); ordem: [NEXT_STEPS](NEXT_STEPS.md).

## Funcionalidades — índice

| ID | Nome | Prioridade | Complexidade | Dependência principal |
| --- | --- | --- | --- | --- |
| F01 | Comparação de equipamentos | P1 | B | InventoryPresenter, PlayerInventory |
| F02 | Ordenar inventário com identidade preservada | P1 | M | InventoryModel e índices de equipamento |
| F03 | Política selecionável de poção rápida | P2 | M | Consumo atual/Q; não é nova poção |
| F04 | Remapeamento de controles | P2 | A | New Input System, opções persistentes |
| F05 | Feedback de cura, coleta e nível | P1 | M | Eventos aceitos; A04–A06 opcionais |
| F06 | Tutorial contextual dispensável | P1 | M | Input, menus e eventos de gameplay |
| F07 | Linha de visão do ranged | P1 | M | EnemyBrain/Combat, NavMesh, pool |
| F08 | Baú de recompensa único por sala | P2 | M | LootRoller, pickups, sala concluída |
| F09 | Obstáculos destrutíveis simples | P2 | M | IDamageable, layers e navegação |
| F10 | Escala de texto e HUD | P2 | M | UGUI, GameOptionsData |
| F11 | Retomar run entre salas | P3 | A | IDs estáveis, snapshot, migração |
| F12 | Escolha de talento ao subir nível | P3 | A | Progressão, modificadores, pausa |
| F13 | Esqueleto lanceiro com investida | P2 | A | FSM/NavMesh, telegraph, A13 |
| F14 | Segundo andar fixo | P3 | A | GameSceneFlow, encounters, A15 |
| F15 | Segundo Boss: Sentinela de Pedra | P3 | A | Boss atual como contrato, A14 |

### F01 — Comparação de equipamentos

- **Objetivo:** mostrar diferença entre item selecionado e equipado. **Benefício:** decidir trocas com confiança.
- **Sistemas afetados:** InventoryPresenter, InventorySlotView, PlayerInventory, WeaponData/ArmorData. **Dependências:** catálogo e equipamento existentes; nenhuma arte nova obrigatória.
- **Complexidade/prioridade:** B / P1. **Aceite:** +6→+12 mostra diferença correta, desequipado usa baseline, poções não exibem comparação de arma.

**Prompt completo para Codex:**

> No Dungeon Crawler, implemente apenas comparação de equipamento na bolsa existente. Leia AGENTS e seções pertinentes de GAME_DESIGN, ARCHITECTURE e MVP, confira Git/Unity/pacotes e os arquivos envolvidos. Inspecione InventoryPresenter, PlayerInventory, WeaponData, ArmorData e os testes antes de planejar arquivos/aceite. Exiba atributo atual, selecionado e diferença de dano base da arma ou defesa, distinguindo bônus plano de dano final por nível; use dados do domínio sem duplicar fórmulas na UI. Preserve 16 slots, raridades sem multiplicador oculto, índices, itens e comandos. Configure referências por builder idempotente, sem editar cenas/prefabs textualmente. Teste seleção, equipar/desequipar, bolsa vazia/cheia, morto e clique sem ataque; confira texto em 720p/1080p no PlayMode da cena de release. Execute testes pertinentes, registre o que não observou e atualize somente documentação essencial afetada. Não implementar ordenação, slots novos ou nova arte.

### F02 — Ordenar inventário com identidade preservada

- **Objetivo:** organizar por tipo/raridade/nome com ordenação estável. **Benefício:** encontrar itens rapidamente.
- **Sistemas afetados:** InventoryModel, PlayerInventory, InventoryPresenter e testes. **Dependências:** remapear índices de equipamento e seleção; F01 é opcional.
- **Complexidade/prioridade:** M / P1. **Aceite:** mesma multiconjunto de itens/potências antes/depois, arma/armadura equipadas preservadas, um evento atômico.

**Prompt completo para Codex:**

> Implemente um botão Ordenar na bolsa atual, sem arrastar itens. Leia AGENTS e trechos de GAME_DESIGN/ARCHITECTURE/MVP, Git/versões e InventoryModel/PlayerInventory/Presenter/testes. Planeje o menor incremento. Ordene no modelo por tipo, rank de raridade e nome com desempate estável; vazios por último. Como equipamento é identificado por índice, preserve a instância/slot original durante o remapeamento, inclusive duas armas com a mesma definição; restaure a seleção coerentemente. Não fundir potências nem mudar capacidade/stack/auto-equip. Publique uma única mudança ao concluir. Adicione EditMode com duplicatas, slots vazios, equipamento, 500 comandos e conservação de quantidades; PlayMode de botão, pausa e ausência de ataque por UI. Atualize builder pelas APIs, valide layout e fluxo na cena de release, rode testes pertinentes e atualize documentação essencial com provas e limites. Não alterar fórmula de dano ou loot.

### F03 — Política selecionável de poção rápida

- **Objetivo:** permitir Q usar a primeira, menor ou maior potência disponível. **Benefício:** reduzir desperdício de cura.
- **Sistemas afetados:** InventoryModel, PlayerInventory, GameOptionsData/Presenter e PotionHudPresenter. **Dependências:** consumo atual, JSON versionado; não depende de novo frasco.
- **Complexidade/prioridade:** M / P2. **Aceite:** padrão preserva primeira poção; HP cheio/morto não consome; HUD corresponde à escolhida.

**Prompt completo para Codex:**

> Acrescente somente uma política configurável de escolha da poção no atalho Q. Consulte AGENTS, GAME_DESIGN/ARCHITECTURE/MVP pertinentes, Git/versões e consumo/opções/HUD existentes. Padrão continua Primeira; alternativas Menor potência e Maior potência usam desempate por índice. Não criar mana, estamina consumível, cooldown ou animação obrigatória de beber. A seleção pertence ao domínio; HUD exibe a mesma poção elegível e o botão Usar do item selecionado mantém sua regra. Planeje migração da opção persistente com fallback para dados ausentes/inválidos/futuros, sem tocar em saves de progressão. Teste potências 15/20/30/40, empate, fim de pilha, HP cheio/morto, Q na bolsa pausada, reinício e persistência. Integre com builder idempotente, rode EditMode/PlayMode pertinentes e confira opções/HUD na cena real. Atualize somente documentos essenciais e relate validações não executadas.

### F04 — Remapeamento de controles

- **Objetivo:** personalizar teclado/mouse e restaurar padrões. **Benefício:** conforto e acessibilidade.
- **Sistemas afetados:** PlayerInputReader, PlayerInventoryInput, GameRestarter, menus, opções. **Dependências:** ações de gameplay e atalhos hoje separados; persistência de overrides própria.
- **Complexidade/prioridade:** A / P2. **Aceite:** conflitos tratados, cancelar captura/voltar sempre possível, bindings aplicados sem editar asset compartilhado.

**Prompt completo para Codex:**

> Implemente remapeamento de teclado/mouse como incremento isolado. Leia AGENTS e seções de produto/arquitetura/MVP, Git/Unity/manifest/lock e todos os donos de InputAction, inclusive inventário, menu e reinício. Planeje uma fonte de bindings runtime que cubra esses donos sem criar API legada. Ofereça captura, cancelamento seguro, indicação de conflito e Restaurar padrão; mantenha uma saída acessível do menu. Persista overrides versionados por usuário, sem modificar o .inputactions compartilhado ou a progressão. Atualize rótulos de controles a partir das ações, sem strings fixas divergentes. Teste binding inválido/duplicado, reload, reset, captura cancelada, pausa/bolsa, morte/reinício e clique na UI sem ataque. Use referências explícitas e builders idempotentes, rode testes pertinentes e valide interação real no Editor e Windows quando possível. Atualize contratos/documentação essencial; registre limitações. Não acrescentar suporte gamepad completo nesta entrega.

### F05 — Feedback de cura, coleta e nível

- **Objetivo:** diferenciar operações aceitas por apresentação discreta. **Benefício:** entender cura, recompensa e evolução.
- **Sistemas afetados:** Health, PlayerInventory/Progression, presenters e áudio existente. **Dependências:** payloads mínimos de aceitação; A04–A06 opcionais, primitivas suficientes.
- **Complexidade/prioridade:** M / P1. **Aceite:** tentativa rejeitada não gera sucesso, efeitos não alteram HP/XP/RNG, reset correto.

**Prompt completo para Codex:**

> Complete o feedback de cura aceita, coleta aceita e subida de nível, preservando os VFX de combo/giro/acerto já existentes. Leia AGENTS e trechos de GDD/arquitetura/MVP, Git/versões, eventos de Health/PlayerInventory/PlayerProgression e presenters. Planeje hooks C# mínimos; Changed de HP sozinho não distingue origem de cura. Emita/observe confirmação com payload necessário após sucesso, sem repetir consumo, cura, XP ou sorteio de loot. Use três efeitos discretos reutilizados e fila de mensagens limitada; assets A04–A06 são opcionais. Respeite pausa, morte, desativação, flash/shake e mixer existentes; não instanciar efeitos a cada operação. Teste coleta cheia, poção com HP cheio/morto, múltiplos níveis, eventos reentrantes e reativação sem inscrições duplicadas. Meça alocações aquecidas com controle positivo, rode testes pertinentes e observe PlayMode da release. Atualize documentação essencial e registre limites; não adicionar hit-stop ou alterar progressão.

### F06 — Tutorial contextual dispensável

- **Objetivo:** ensinar controles e loop gradualmente. **Benefício:** primeira partida compreensível.
- **Sistemas afetados:** menus/HUD, input, combate/dash/inventário e salas. **Dependências:** eventos existentes; estado tutorial separado das opções de gameplay.
- **Complexidade/prioridade:** M / P1. **Aceite:** dispensar/rever, mensagens baseadas em ação concluída e run sem bloqueio artificial.

**Prompt completo para Codex:**

> Implemente onboarding opcional por mensagens contextuais na primeira sala, não uma fase nova. Consulte AGENTS, GAME_DESIGN/ARCHITECTURE/MVP pertinentes, Git/versões, eventos e UI atuais. Planeje passos curtos para mover/mirar, combo, dash, giro, bolsa e poção disponível; conclua passos por eventos confirmados, não busca global ou polling pesado. Permita dispensar e rever pelo menu; persista somente preferência de tutorial, sem save de run. Não exigir drop aleatório para liberar porta nem fornecer itens de diagnóstico numa partida normal. Rótulos devem respeitar bindings disponíveis. Teste ordem inesperada, morrer/reiniciar, bolsa/pausa, dispensar, reset e ausência de item elegível. Integre por builder idempotente, valide 720p/1080p na cena real e execute testes pertinentes. Atualize documentação essencial com aceite observado/pendente. Preserve regras de salas, loot, dano e identidade visual.

### F07 — Linha de visão do ranged

- **Objetivo:** evitar que o ranged prepare tiros sem visão direta. **Benefício:** cobertura e posicionamento mais claros.
- **Sistemas afetados:** EnemyBrain/Combat/Navigation, EnemyData. **Dependências:** máscaras de obstáculos/portas, NavMesh e pool atual.
- **Complexidade/prioridade:** M / P1. **Aceite:** teste de parede/porta, origem do tiro correta e consulta em intervalo controlado.

**Prompt completo para Codex:**

> Adicione linha de visão ao inimigo ranged existente; ele já recua e dispara pelo pool. Leia AGENTS e seções relevantes de GAME_DESIGN/ARCHITECTURE/MVP, Git/versões, EnemyBrain/Combat/Navigation/Data, layers e testes. Planeje uma consulta física temporizada do ponto de disparo ao alvo com máscaras explícitas; parede/porta bloqueia, collider do próprio inimigo não. Preserve estados Idle/Chase/Attack/Dead e tempos configurados. Quando perder visão, não cause tiro atrasado; reposicionamento deve usar NavMeshAgent e evitar SetDestination sem limite por frame. Teste visão livre/bloqueada, porta abrindo, alvo morto/móvel, perda de visão no windup, desativação, pausa e reativação; prove projéteis reutilizados. Atualize dados/referências via builder idempotente, rode EditMode/PlayMode e observe ranged na release. Documente comportamento e limites; não alterar dano, drops ou telegraph do Boss.

### F08 — Baú de recompensa único por sala

- **Objetivo:** recompensa deliberada depois de limpar uma sala. **Benefício:** pausa clara para loot.
- **Sistemas afetados:** RoomController, LootRoller, Pickup, input e presenter de interação. **Dependências:** nova regra de recompensa; A11 opcional.
- **Complexidade/prioridade:** M / P2. **Aceite:** abre uma vez, bloqueado antes da limpeza, item recusado permanece no mundo.

**Prompt completo para Codex:**

> Implemente um baú experimental numa sala fixa, sem gerar dungeon procedural. Consulte AGENTS, GDD/arquitetura/MVP pertinentes, Git/versões e fluxo real de sala/loot/coleta. Antes de editar, registre como proposta a regra: baú habilitado só após sala limpa, interação F próxima e única; E continua giro. Dados/recompensa em ScriptableObject, UI só solicita interação. Sorteie uma vez e entregue pickups pelo fluxo normal, preservando coleta recusada e sem alterar tabelas antigas. Não usar inventário cheio para apagar a recompensa. Modele primeiro com primitivas; A11 é arte opcional posterior. Teste tentativa bloqueada, alcance, pressionar repetidamente, abrir/desativar/reativar, morte/reinício, capacidade e identidade. Configure por builder idempotente, valide PlayMode da sala e build quando integrado. Rode testes pertinentes e atualize regra/arquitetura/estado essenciais. Não implementar loja ou sistema de chaves.

### F09 — Obstáculos destrutíveis simples

- **Objetivo:** quebrar vasos/caixotes sem afetar o loop principal. **Benefício:** resposta do ambiente e exploração leve.
- **Sistemas afetados:** IDamageable, ataque básico/especial, layers, decoração, loot opcional. **Dependências:** gabarito de sala/NavMesh; A12 opcional.
- **Complexidade/prioridade:** M / P2. **Aceite:** uma quebra, sem bloquear portas/spawns nem danificar alvos duas vezes.

**Prompt completo para Codex:**

> Crie um único tipo de prop destrutível previsto como expansão no GAME_DESIGN. Leia AGENTS, arquitetura/MVP pertinentes, Git/versões, IDamageable, consultas de ataque/especial e builders de ambiente. Planeje alvo próprio com HP/dados SO, evento único de quebra e apresentação reutilizada; não registrar prop como inimigo necessário para limpar sala. Defina máscaras explícitas para básico/especial alcançarem o prop sem quebrar deduplicação. Comece sem drops; qualquer recompensa futura usa dados separados. Preserve corredor, portas, spawns, layers e NavMesh, mantendo centro livre. Teste dano único, vários colliders, ataques simultâneos/reentrantes, morte/pausa e reativação, além de conclusão normal da sala. Use primitivas ou A12 aprovado, integre por builder idempotente, execute testes pertinentes e observe PlayMode. Atualize documentação essencial; não adicionar física de estilhaços ou geração procedural.

### F10 — Escala de texto e HUD

- **Objetivo:** opção de tamanho de texto sem cortar interface. **Benefício:** leitura em resoluções e necessidades diferentes.
- **Sistemas afetados:** UGUI, builders de menu/inventário/HUD, opções persistentes. **Dependências:** layout atual e rótulos nativos.
- **Complexidade/prioridade:** M / P2. **Aceite:** escala configurável com fallback; foco/click targets, dropdowns e bolsa legíveis.

**Prompt completo para Codex:**

> Acrescente escala de texto/HUD às opções existentes, sem trocar UGUI ou a fonte. Consulte AGENTS, seções pertinentes de GDD/arquitetura/MVP, Git/versões, CanvasScaler, builders e GameOptionsData/Presenter. Planeje faixa inicial 100/125/150% como proposta configurável e migração dos dados; use containers/âncoras e ajuste texto preservando targets, não simplesmente multiplique o Canvas inteiro. Aplicar/Voltar/reset seguem o fluxo atual. Teste dados antigos/inválidos, persistência, bolsa cheia, nomes longos, mensagens, HP/Boss e dropdowns. Capture e inspecione 720p/1080p e ultrawide emulado, distinguindo-o de monitor físico. Rode testes de layout/foco pertinentes e valide PlayMode com New Input System, sem clique atravessando overlays. Atualize documentação essencial e registre limites de cobertura/acessibilidade. Não certificar contraste ou escalabilidade sem medição.

### F11 — Retomar run entre salas

- **Objetivo:** salvar checkpoints seguros ao limpar sala. **Benefício:** interromper e continuar uma run curta.
- **Sistemas afetados:** progressão/inventário/saúde, FloorController, GameSceneFlow, menu. **Dependências:** IDs persistentes, esquema versionado, política de aleatoriedade; muda escopo sem save da 1.0.
- **Complexidade/prioridade:** A / P3. **Aceite:** arquivo inválido não trava, reload preserva itens/potências/equipamento e não duplica recompensas.

**Prompt completo para Codex:**

> Projete e implemente somente checkpoint entre salas como expansão explícita do produto sem save atual. Leia AGENTS, GDD/arquitetura/MVP e Git/versões, modelos, FloorController, flow/menu e opções. Primeiro especifique snapshot/limites: salvar apenas sala limpa, estado de player/inventário/equipamento e salas concluídas; não salvar projéteis/inimigos no meio do combate. Use IDs estáveis de definições, JSON versionado, escrita atômica com backup e validação de capacidade/valores/referências. Separe saves de opções e preserve Nova run/Reiniciar. Planeje como loot no chão e RNG serão tratados antes de codificar. Teste round-trip, arquivo truncado/versão futura/ID ausente, duplicatas/potências, checkpoint repetido e retorno sem XP/loot duplicados. Integre Continuar só quando save válido, por builder. Rode testes pertinentes, partida real de salvar/sair/retomar e atualize regras/contratos essenciais. Não criar autosave de combate ou nuvem.

### F12 — Escolha de talento ao subir nível

- **Objetivo:** uma escolha adicional simples de especialização. **Benefício:** variação entre runs.
- **Sistemas afetados:** ExperienceModel/PlayerProgression, modificadores, UI de escolha/pausa. **Dependências:** semântica de empilhamento e origem; F11 somente se persistir.
- **Complexidade/prioridade:** A / P3. **Aceite:** base +10% por nível continua íntegra, escolha única, múltiplos níveis enfileirados sem reentrância.

**Prompt completo para Codex:**

> Implemente um primeiro talento opcional por nível como expansão, sem substituir a progressão atual. Consulte AGENTS, GDD/arquitetura/MVP pertinentes, Git/versões, ExperienceModel/PlayerProgression, combate/defesa/estamina e pausa/UI. Registre antes as opções, valores e regras como proposta ajustável em SO; não inventar árvore extensa. Preserve +10% por nível, cura de nível e bônus +6/+12/+18; mantenha modificadores identificados por origem, sem editar SO compartilhado nem acumular bônus a cada Refresh. UI solicita uma escolha ao domínio e usa pausa coordenada com bolsa/menus. Teste escolha repetida, níveis simultâneos, morte/reinício, reativação, cancelar UI e retorno exato ao estado base. Configure por builder, rode EditMode/PlayMode e faça playtest/balanço antes de afirmar melhoria. Atualize apenas documentos essenciais. Não adicionar slots/afixos, RNG oculto ou save implícito.

### F13 — Esqueleto lanceiro com investida

- **Objetivo:** inimigo de ataque linear anunciado. **Benefício:** variar esquiva e posicionamento.
- **Sistemas afetados:** EnemyData/FSM/Navigation, telegraph e encounters. **Dependências:** decisões temporizadas, dano único por investida; A13 após validar primitivas.
- **Complexidade/prioridade:** A / P2. **Aceite:** aviso ≥1 s proposto, sem atravessar paredes, nenhuma colisão aplica dano em todo frame.

**Prompt completo para Codex:**

> Adicione um lanceiro experimental em uma sala de teste fixa. Leia AGENTS, GDD/arquitetura/MVP pertinentes, Git/versões e EnemyBrain/Combat/Navigation, dano e telegraph atuais. Planeje dados SO e subetapas do ataque preservando estados externos Idle/Chase/Attack/Dead. Proponha aviso linear de pelo menos 1 s, direção travada ao compromisso, deslocamento via NavMeshAgent validado contra obstáculos e um dano por alvo. Não mover por Rigidbody nem varrer a cena por frame. Comece com primitivas; mantenha ranged/elite e tabelas existentes. Teste saída da faixa, dash/invulnerabilidade, parede/porta, alvo morto, pausa/desativação durante aviso e fim da sala sem softlock. Depois conecte A13/rig Generic em variante isolada e alinhe animação ao dano autoritativo. Execute testes e observe PlayMode; documente parâmetros como propostas de playtest. Não trocar todos os encontros nem adicionar Boss novo.

### F14 — Segundo andar fixo

- **Objetivo:** sequência adicional desenhada à mão. **Benefício:** continuar a aventura com pacing novo.
- **Sistemas afetados:** GameSceneFlow, FloorController, RoomEncounter, progressão/menu. **Dependências:** decisão sobre transição/estado do jogador; A15 opcional; F11 se houver checkpoint persistente.
- **Complexidade/prioridade:** A / P3. **Aceite:** primeiro andar preservado, IDs/refs explícitos e travessia completa de ambas as sequências.

**Prompt completo para Codex:**

> Projete um segundo andar fixo, começando por uma sala jogável e uma transição de teste. Consulte AGENTS, GAME_DESIGN/ARCHITECTURE/MVP, Git/versões, builders, flow, encontros e progressão. Antes de implementar, registre a proposta de persistência entre andares (HP/nível/ouro/bolsa), condições de entrada/vitória/derrota e curva de dificuldade; não transformar a auditoria em aprovação de números. Use composição explícita por dados SO/prefabs, preserve o primeiro andar e seus testes, sem geração procedural. A15 é referência opcional; primitivas validam primeiro navegação, portas e espaço de combate. Teste transição repetida, run nova/reinício/menu, morte e última morte de sala, pausa e referências ausentes. Construa cenas por builder idempotente, execute testes pertinentes e percurso normal no Editor/Windows. Atualize regras, arquitetura e MVP somente no escopo aprovado. Não gerar assets finais ou regenerar herói/inimigos existentes automaticamente.

### F15 — Segundo Boss: Sentinela de Pedra

- **Objetivo:** arena opcional com padrão diferente do Guardião. **Benefício:** nova luta baseada em leitura espacial.
- **Sistemas afetados:** BossData/Brain/telegraph, arena/HUD e progressão de encontro. **Dependências:** contrato de Boss atual; F14 opcional, A14 posterior ao protótipo.
- **Complexidade/prioridade:** A / P3. **Aceite:** ataque anunciado, resolução única/cancelável e vitória/reinício sem interferir no Guardião.

**Prompt completo para Codex:**

> Crie um protótipo isolado de Boss Sentinela de Pedra, sem substituir o Guardião. Leia AGENTS, GDD/arquitetura/MVP pertinentes, Git/versões e BossBrain/Data/Phase/Telegraph, Health, HUD e Floor. Proponha inicialmente uma onda frontal em leque com aviso ≥1 s e recuperação explícita; valide uma fase antes de expandir padrões. Dados em SO, dano único autoritativo e interrupção por morte/desativação/pausa; navegação por NavMeshAgent, projéteis se usados pelo pool atual. Comece com primitivas; A14 é conceito condicional, não mesh pronta. Teste dentro/fora da área, dash, morte durante windup, reativação, HUD só na luta, vitória única e reinício. Integre por builder idempotente numa cena separada, rode testes e playtest Editor/Windows. Documente parâmetros e balanceamento como propostas; preserve as duas fases/50%/3 s do Guardião existente e não alterar loot global.

## Arte — contrato comum

Os 15 briefs abaixo complementam o estado real. Estilo oficial: **low-poly 3D estilizado, fantasia medieval sombria, planos grandes, proporções caricatas, silhueta legível a 45°, fundo de pedra dessaturado e acentos contidos**. Paleta/direção em `Assets/_Project/Art/Concepts/ART_DIRECTION_AND_PROMPTS.md`; procedência em PROVENANCE. Anexar as referências reais à ferramenta. Rig existente Generic/in-place e materiais atuais ficam preservados. Metas novas são propostas de produção, não critérios retroativos da 1.0.

Pasta exclusiva sugerida: `Assets/_Project/Art/ProductionStaging/<ID>/`; não sobrescrever lote existente. Registrar ferramenta/modelo/versão, prompt, data, fontes/licença e edições. Uma imagem não entrega FBX, rig, clip ou UI funcional. Para 3D, usar DCC/ferramenta capaz e validar export; sem capacidade, entregar somente conceito identificado. Spritesheets exigem normalização de grade/pivô e QA; animação raster de efeito não é animação corporal 3D. Integração de código é tarefa Codex separada.

| ID | Asset | Tipo de saída | Prioridade / complexidade | Depende de |
| --- | --- | --- | --- | --- |
| A01 | Ícone de dash | PNG 2D | P1 / B | Dash existente |
| A02 | Ícone de giro | PNG 2D | P1 / B | Especial existente |
| A03 | Skin modular de UI | Sprites 2D separados | P1 / M | UGUI existente |
| A04 | Cura discreta | Spritesheet VFX 2D | P1 / M | F05 |
| A05 | Subida de nível | Spritesheet VFX 2D | P1 / M | F05 |
| A06 | Coleta confirmada | Atlas VFX 2D | P2 / B | F05 |
| A07 | Rastro do dash | Textura VFX 2D | P2 / B | Ponte de apresentação futura |
| A08 | Núcleo do projétil hostil | Modelo 3D estático | P2 / M | Projectile pooled existente |
| A09 | Cauda de projétil | Textura VFX 2D | P2 / B | Reuso/reset do projétil |
| A10 | Impacto Slam | Spritesheet VFX 2D | P2 / M | Hook de impacto do Boss |
| A11 | Baú de pedra e latão | Modelo 3D + clip mecânico | P2 / M | F08 |
| A12 | Família de vasos destrutíveis | Modelos 3D estáticos | P2 / M | F09 |
| A13 | Lanceiro corrompido | Conceito 2D | P2 / M | F13 prototipado |
| A14 | Sentinela de Pedra | Conceito 2D | P3 / M | F15 prototipado |
| A15 | Cripta inundada | Concept art 2D | P3 / M | F14/layout definido |

### A01 — Ícone de dash (ICON-ACTION-DASH)

- **Finalidade:** identificar o dash existente no HUD. **Descrição visual:** dobra ampla de capa azul em movimento e espaço negativo em seta; poucas facetas, sem arma.
- **Estilo oficial:** low-poly estilizado da direção comum. **Consistência:** Hero_Concept_01, Hero.fbx/capa e ícones atuais de `Art/Items/Icons` anexados.
- **Perspectiva/enquadramento:** ícone ortográfico 3/4, objeto central em área 416×416 com margem 48 px. **Cores/materiais:** tecido #2F6BFF, luz #4FD8FF, sombra #1E1830; fosco.
- **Formato:** PNG RGBA 512×512, alpha real, fonte editável; imagem 2D. **Prioridade/complexidade:** P1/B.
- **Prompt inglês:** `Create one 512x512 transparent RGBA game HUD icon for the existing dash action. Use the attached blue-caped Hero and current item icons as identity and rendering references. Depict a single broad royal-blue cape sweep with a forward chevron made of negative space, chunky faceted shapes, restrained hand-painted shading, matte cloth, cyan edge accent. Stylized low-poly dark medieval fantasy, orthographic three-quarter view. Centre the silhouette inside 416x416 pixels with 48px padding. Preserve a strong greyscale silhouette at 32px. Deliver artwork only; key labels and cooldown remain runtime UI.`
- **Negative prompt:** `text, letters, numbers, sword, multiple icons, rarity border, scenery, photorealism, checkerboard, watermark, glow obscuring silhouette`.
- **Aceite:** um ícone distinguível do giro em 32/48 px; sem halo/checkerboard, margens/alpha conferidos.
- **Unity:** Sprite Single, sRGB, bilinear/clamp, mipmaps off, PPU 100; manter cooldown/atalho como UI nativa. Integrar pelo presenter/builder em tarefa separada, sem mudar dash.

### A02 — Ícone de giro (ICON-ACTION-SPECIAL)

- **Finalidade:** identificar o especial já funcional. **Descrição visual:** espada curta de aço dentro de arco circular ciano, centro aberto e sentido de rotação legível.
- **Estilo oficial:** low-poly estilizado. **Consistência:** A01 aprovado, Hero e espadas reais do catálogo; não inventar arma elemental.
- **Perspectiva/enquadramento:** 3/4 ortográfico, 416×416 central. **Cores/materiais:** aço #B9C4CF, latão #E0B040, arco #4FD8FF.
- **Formato:** PNG RGBA 512×512 e fonte; 2D. **Prioridade/complexidade:** P1/B.
- **Prompt inglês:** `Create one 512x512 transparent RGBA icon for the existing 360-degree sword spin. Match the attached approved dash icon, Hero and project sword references. Show one short broad steel sword within a single cyan circular sweep with an open centre, visibly different from a forward dash. Stylized low-poly dark medieval fantasy, broad faceted planes, steel and small brass accents, restrained hand-painted shading, orthographic three-quarter view. Fit artwork in a central 416x416 area with 48px padding. Read clearly at 32px and in greyscale. Keep the image free of UI text and timers; this is artwork, not a new ability.`
- **Negative prompt:** `text, cooldown numbers, runes as letters, extra swords, lightning, fire weapon, closed glowing disc, photorealism, checkerboard, watermark`.
- **Aceite:** leitura sem depender só de cor; mesma família de A01, alpha/margens válidos.
- **Unity:** Sprite/sRGB/no mipmaps/clamp; ligar ao HUD existente por referência explícita. Nenhum custo, dano ou tempo é definido pelo PNG.

### A03 — Skin modular de UI (UI-SKIN)

- **Finalidade:** acabamento das telas existentes sem redesenhar fluxo. **Descrição visual:** painéis azul-preto com chanfros largos e linhas discretas de latão; foco por cantos geométricos separados.
- **Estilo oficial:** medieval estilizado sóbrio, planos grandes. **Consistência:** capturas atuais de MainMenu/bolsa/opções; slots atuais 188×104 e fonte existente.
- **Perspectiva/enquadramento:** frontal ortográfico, componentes isolados, sem mockup achatado. **Cores/materiais:** fundo aproximado #0E131A, latão fosco #E0B040 pouco saturado, sem textura ruidosa atrás de texto.
- **Formato:** PNG RGBA separados: painel 128×128/borda16, botão256×80/borda12, slot192×112/borda8, canto32×32; fonte e mapa de estados. **Prioridade/complexidade:** P1/M.
- **Prompt inglês:** `Produce a modular 2D UI art kit matching the attached current Dungeon Crawler menus and inventory. Stylized low-poly dark medieval fantasy translated into broad flat bevels, blue-black panels and restrained worn-brass lines. Deliver separate transparent RGBA assets: 128x128 panel with 16px nine-slice borders, 256x80 button with 12px borders, 192x112 slot with 8px borders, and 32x32 focus corners. Provide separate normal, hover, focus, pressed and disabled decorative layers, plus empty, selected and equipped markers. Frontal orthographic view, uniform edge thickness, quiet text-safe centres. Keep labels, counters, rarity names and interaction code out of the artwork. No full-screen flattened interface.`
- **Negative prompt:** `baked text, tiny ornaments, skull frames, saturated purple gradients, fake controls, watermark, checkerboard, photorealistic metal`.
- **Aceite:** bordas esticam sem deformação a 720p/1080p; foco/seleção/equipado distinguíveis além de cor; sem mudança de targets.
- **Unity:** sprites Sliced com borders medidos, UGUI/CanvasScaler atual e texto editável. Integrar por builder; arte não cria botões funcionais nem exige pacote/fonte nova.

### A04 — Cura discreta (FX-HEAL)

- **Finalidade:** feedback após cura aceita em F05. **Descrição visual:** motes rubi→branco quente subindo próximos aos pés, anel baixo suave; sem poça/perigo.
- **Estilo oficial:** VFX estilizado de formas grandes, baixo ruído. **Consistência:** frasco real de vida, Hero e piso; não refazer ícone/frasco existentes.
- **Perspectiva/enquadramento:** efeito isolado em células iguais, visto a 45°, pivô no centro dos pés. **Cores/materiais:** rubi suave e marfim; transparência progressiva, sem flash amplo.
- **Formato:** animação raster, 8 frames 256×256, grade4×2 em PNG 1024×512 RGBA, frames/fontes e timing JSON (0,4 s proposto). **Prioridade/complexidade:** P1/M.
- **Prompt inglês:** `Create an eight-frame 2D VFX animation study for a successful health recovery in the attached stylized low-poly dark medieval dungeon. Show a few broad ruby-to-warm-white motes rising close to the Hero's boots and a very faint open foot ring, then fading fully. Fixed 45-degree isometric view, identical camera, scale and bottom-centred anchor in every frame; no character painted into the effect. Use eight equal 256x256 transparent cells in a four-column two-row 1024x512 RGBA sheet. Provide isolated frames if exact layout cannot be guaranteed. Target a restrained 0.4-second visual within a 0.6m gameplay radius. This is a raster effect source, not a healing mechanic or Unity prefab.`
- **Negative prompt:** `character, text, opaque background, checkerboard, poison pool, green damage cloud, full-screen flash, different camera per frame, watermark`.
- **Aceite:** grade/pivô normalizados, frame final transparente, sem cortes ou partículas que sugiram dano; validar sobre piso real.
- **Unity:** material URP transparente compatível e ParticleSystem/sheet reutilizado; F05 dispara só após cura efetiva. Normalizar grade fora da geração, testar pausa/reset/opções; não atrasar cura por animação.

### A05 — Subida de nível (FX-LEVEL)

- **Finalidade:** destacar nível ganho sem esconder combate. **Descrição visual:** anel ciano fino sobe dos pés à cintura, poucos motes de latão; centro livre.
- **Estilo oficial:** low-poly estilizado, brilho contido. **Consistência:** orbe XP com anel já existente, Hero e telegraph do Boss para separar sucesso/perigo.
- **Perspectiva/enquadramento:** 45° fixa e pivô nos pés, 12 células iguais. **Cores/materiais:** ciano #4FD8FF e latão #E0B040; nenhuma luz branca opaca.
- **Formato:** spritesheet RGBA 1024×768,4×3 células256, frames/fontes e timing JSON; animação 2D de0,7 s proposta. **Prioridade/complexidade:** P1/M.
- **Prompt inglês:** `Create a twelve-frame raster level-up effect for the attached blue-caped Hero in a stylized low-poly dark medieval dungeon. A single thin cyan open ring rises from boots toward waist, accompanied by sparse broad brass motes, then dissipates completely. Keep the combat floor visible and use no text, digits or character image. Fixed 45-degree isometric camera, identical scale and foot-centre anchor across twelve 256x256 transparent frames, four columns by three rows in a 1024x768 RGBA sheet. Deliver individual frames when grid precision needs later normalization. Target a 0.7-second animation and a restrained 0.8m radius. Match existing XP visuals without recreating the XP pickup.`
- **Negative prompt:** `level text, numbers, opaque disc, boss danger red, explosion, camera movement, clipped cells, checkerboard, watermark`.
- **Aceite:** continuidade sem saltos, base estável, sem confusão com perigo, final transparente.
- **Unity:** visual reutilizado ligado a LeveledUp; múltiplos níveis não causam cura/dano extra. Conferir grade, alpha, reset e flash reduzido em PlayMode; não entregar como AnimationClip corporal.

### A06 — Coleta confirmada (FX-LOOT)

- **Finalidade:** confirmar pickup aceito em F05. **Descrição visual:** três glints: ciano XP, latão ouro e marfim equipamento/poção, mesma forma compacta.
- **Estilo oficial:** facetas grandes e poucas partículas. **Consistência:** pickups existentes de Art/Items e ícones; manter cores de perigo distintas.
- **Perspectiva/enquadramento:** 45°, três imagens isoladas no mesmo gabarito. **Cores/materiais:** ciano/latão/marfim, transparência suave.
- **Formato:** três PNG RGBA128×128 e atlas 384×128 com mapa de rects/fonte; atlas2D, não spritesheet corporal. **Prioridade/complexidade:** P2/B.
- **Prompt inglês:** `Produce a consistent family of three small transparent 2D collection glints for the attached current dungeon pickups: cyan for XP, muted brass for gold, warm ivory for stored gear or potions. Use one compact broad faceted sparkle shape per image, the same proportions and restrained shading, viewed from the fixed 45-degree isometric camera. Stylized low-poly dark medieval fantasy, readable over grey-blue stone. Deliver three isolated 128x128 RGBA PNGs plus a 384x128 three-cell atlas and editable source. No item model, text or number. These textures will be emitted briefly only after accepted collection; do not imply a new reward type.`
- **Negative prompt:** `coins or potion duplicates, text, new currency symbol, large burst, background, checkerboard, watermark`.
- **Aceite:** três variantes legíveis com pouco brilho, alpha válido, ≤0,35 m e0,2–0,35 s como meta de integração.
- **Unity:** efeito reutilizado, até8 partículas propostos; Collected só após sucesso. Pickup recusado não emite; textura não altera tabelas ou RNG.

### A07 — Rastro do dash (FX-DASH)

- **Finalidade:** polir deslocamento existente. **Descrição visual:** faixa azul afilada com duas ou três dobras largas de capa, sem duplicata opaca do personagem.
- **Estilo oficial:** VFX estilizado discreto. **Consistência:** Hero capa/Run/Dash atuais e FX-Slash existente, sem copiar o arco de ataque.
- **Perspectiva/enquadramento:** textura de faixa em UV frontal, +U segue movimento; prévia aplicada a45°. **Cores/materiais:** azul/ciano com alpha que zera nas extremidades.
- **Formato:** PNG RGBA256×64 e fonte; textura 2D estática para trail. **Prioridade/complexidade:** P2/B.
- **Prompt inglês:** `Create a single 256x64 transparent RGBA ribbon texture for a subtle dash trail on the attached royal-blue-caped Hero. Use two or three broad tapered cloth-like blue sweeps with a restrained cyan edge, large simple faceted shapes and smooth alpha fade at both ends. Stylized low-poly dark medieval fantasy. Flat UV texture, horizontal U axis follows motion; no perspective distortion or painted character. Match the current cape identity, not the sword slash effect. Provide editable source and an optional 45-degree isometric application preview labelled as preview. The texture is presentation only and must not suggest a damaging dash.`
- **Negative prompt:** `character clones, text, weapon arc, lightning, smoke wall, opaque background, checkerboard, watermark`.
- **Aceite:** transparente nas bordas, sem trilha antiga após reset; cauda≤0,2 s proposta, não oculta chão/telegraph.
- **Unity:** ponte de início/fim de dash em tarefa Codex; trail/partículas reutilizados e limpos ao cancelar/pausar/reativar. Sem mudar duração/invulnerabilidade.

### A08 — Núcleo do projétil hostil (polimento de FX-PROJECTILE)

- **Finalidade:** enriquecer a esfera funcional já pooled. **Descrição visual:** núcleo facetado laranja de silhueta esférica, casca simples e pequena fenda quente; nenhum raio novo.
- **Estilo oficial:** low-poly 3D. **Consistência:** cajado ranged, projétil laranja e runas do Warden atuais.
- **Perspectiva/enquadramento:** modelo real centrado na origem; previews frontal/lateral/45°. **Cores/materiais:** ferro escuro #3E4152 e emissão laranja #FF9A2E, um material preferido.
- **Formato:** fonte DCC ou gerador editável + FBX estático, GLB opcional de revisão; alvo≤300 triângulos e envelope visual0,4 m. **Prioridade/complexidade:** P2/M.
- **Prompt inglês:** `Using a genuine 3D modelling/export tool, create a small faceted hostile projectile visual matching the attached current ranged staff, Warden and orange projectile. Preserve an approximately 0.4m spherical envelope, centred pivot, Unity metre scale, +Y up and +Z forward. Build one broad angular core with a simple dark iron shell and a restrained warm-orange emissive fissure; no spikes extending the danger footprint. Stylized low-poly dark medieval fantasy, target at most 300 triangles and one URP-compatible material. Deliver editable source and an actual static FBX, with orthographic front/side and 45-degree isometric previews. This replaces only a visual child in a later integration; preserve collision, speed, damage and pooling. If your tool only makes images, deliver a clearly labelled concept instead of claiming a mesh.`
- **Negative prompt:** `new explosion, larger hit radius, rig, photorealism, many materials, baked scene light, text, watermark`.
- **Aceite:** mesh/import/bounds/pivô/normais conferidos; sem collider/dano novos; orçamento medido.
- **Unity:** filho visual do Projectile, URP/Lit ou emissivo compatível; collider/lifetime/pool existentes preservados. Testar 16 tiros simultâneos e reset; medir GPU antes de declarar ganho.

### A09 — Cauda de projétil (FX-PROJECTILE-TRAIL)

- **Finalidade:** mostrar direção de tiro. **Descrição visual:** cauda laranja compacta em gota, centro fino, sem efeito explosivo.
- **Estilo oficial:** VFX low-poly estilizado. **Consistência:** A08 ou esfera atual, cajado e piso existentes.
- **Perspectiva/enquadramento:** UV horizontal frontal com bordas transparentes, prévia45°. **Cores/materiais:** laranja #FF9A2E→ferrugem #8A4A22 e alpha0.
- **Formato:** PNG RGBA128×32 + fonte; textura 2D. **Prioridade/complexidade:** P2/B.
- **Prompt inglês:** `Create one 128x32 transparent RGBA tapered ribbon texture for the attached existing orange hostile projectile. A short broad warm-orange head fading into burnt rust and complete transparency, simple faceted visual rhythm, no sparks outside the compact envelope. Stylized low-poly dark medieval dungeon, matching the current staff and projectile rather than the Hero's cyan sword. Flat horizontal UV layout with no baked perspective; deliver editable source and optional isometric preview separately. Design for a tail shorter than 0.15 seconds and 0.6m after integration. Artwork only; no change to projectile collision or damage.`
- **Negative prompt:** `fireball explosion, cyan friendly magic, text, long opaque smoke, busy particles, checkerboard, watermark`.
- **Aceite:** legível contra pedra, bordas alpha0 e sem exagerar área perigosa.
- **Unity:** trail visual no projétil pooled; limpar em retorno/OnEnable e respeitar pausa; não criar objeto/texture por tiro. A08 opcional.

### A10 — Impacto Slam (FX-SLAM)

- **Finalidade:** pontuar impacto resolvido do Guardião. **Descrição visual:** anel baixo de poeira e poucas lascas angulares de pedra, borda laranja apagada; sem chão persistente de dano.
- **Estilo oficial:** low-poly estilizado e chão legível. **Consistência:** Warden/maul, _GameArt e telegraph existente; não redesenhar aviso.
- **Perspectiva/enquadramento:** superfície de chão em vista ortográfica superior para quad; prévia45°, centro fixo. **Cores/materiais:** pedra #55596B, ferrugem/laranja sutil, alpha alto no centro.
- **Formato:** spritesheet 2D8 frames256,4×2 PNG 1024×512 RGBA, frames/fonte/timing0,35 s proposto. **Prioridade/complexidade:** P2/M.
- **Prompt inglês:** `Create an eight-frame ground-impact raster effect for the attached existing Warden slam. A low thin dust shock ring and a few chunky angular stone chips expand briefly and dissipate, with a muted warm-orange edge. Stylized low-poly dark medieval fantasy matching the current grey-blue dungeon stone. Use a fixed top-down orthographic ground-plane view, stable centre and scale, eight equal 256x256 transparent frames in a 4x2 1024x512 RGBA sheet; provide individual frames for normalization if needed. Keep the centre open and the floor readable. Target 0.35 seconds. This is the cosmetic aftermath of a resolved hit, never the advance warning or a persistent damage area. Supply a separate 45-degree gameplay preview only as reference.`
- **Negative prompt:** `lava pool, new attack, opaque disc, text, camera shake baked into frames, giant explosion, checkerboard, watermark`.
- **Aceite:** frame final transparente; não encobre player/aviso; raio integrado acompanha3,5/4,55 m reais, não imagem arbitrária.
- **Unity:** hook de resolução autoritativa em tarefa separada, sem chamar dano. Reuso/pool, pausa e cancelamento testados; telegraph mínimo1 s intacto.

### A11 — Baú de pedra e latão (condicional F08)

- **Finalidade:** visual de baú único após sala limpa; não existe baú funcional atualmente. **Descrição visual:** caixa baixa de pedra azul com aro de latão, tampa de dobradiça traseira e pega larga; aberto/fechado distinguíveis.
- **Estilo oficial:** low-poly 3D medieval. **Consistência:** _GameArt, portas e ouro reais; evitar desenho comercial reconhecível.
- **Perspectiva/enquadramento:** modelo real, previews ortográficos e45°, pivô base central, tampa com pivô de dobradiça. **Cores/materiais:** pedra #55596B/#3E4152, latão #E0B040 desgastado.
- **Formato:** fonte editável + FBX com partes separadas; clip mecânico Open in-place0,6 s proposto, sem rig humanoide; alvo≤1200 tris,≤2 materiais. **Prioridade/complexidade:** P2/M.
- **Prompt inglês:** `With a real 3D modelling and animation tool, build an original low-poly reward chest for the attached grey-blue stone dungeon. A low chunky stone coffer with restrained worn-brass bands, one broad handle and a clearly separate hinged lid. Approximate dimensions 1.2m wide, 0.7m deep, 0.8m tall, metre scale, +Y up, +Z front, base-centred pivot and rear lid hinge. Target at most 1200 triangles and two matte URP-compatible materials. Deliver editable source, a genuine FBX with named Base and Lid parts, and one optional 0.6-second in-place mechanical Open take. Supply front/side and fixed 45-degree previews. Do not generate reward logic, colliders, loot or a humanoid rig. If only image generation is available, deliver a concept labelled as such.`
- **Negative prompt:** `copyrighted chest, skull ornament overload, photorealism, new currency, text, floating pivot, watermark`.
- **Aceite:** tampa não corta base, pivôs/escala/take conferidos; silhuetas aberto/fechado legíveis; F08 decidido antes da produção final.
- **Unity:** variante/prefab novo pelo Editor, collider simples separado e estado visual dirigido pelo baú; animação não entrega recompensa. Manter fora de spawns/passagens/NavMesh crítico.

### A12 — Vasos destrutíveis (MODEL-DESTRUCTIBLE, condicional F09)

- **Finalidade:** cenário quebrável futuro. **Descrição visual:** vaso de barro baixo e caixa de madeira reforçada, formas grossas, rachaduras discretas; versões intacta/quebrada sem gore.
- **Estilo oficial:** low-poly 3D. **Consistência:** musgo/pedra/couro do cenário, densidade de detalhe existente.
- **Perspectiva/enquadramento:** modelos isolados, pivô no chão; previews frente/lado/45°. **Cores/materiais:** barro ferrugem #8A4A22 dessaturado e madeira #7A4B2A; sem acentos de perigo/loot garantido.
- **Formato:** fontes + FBX estáticos nomeados intact/broken;≤600 tris/prop propostos e1 material compartilhado; sem animação física obrigatória. **Prioridade/complexidade:** P2/M.
- **Prompt inglês:** `Using a real 3D authoring/export tool, create two small environmental breakable prop families matching the attached stylized low-poly dungeon: one squat clay jar and one sturdy wooden crate. Large faceted planes, muted burnt-clay and brown wood, subtle cracks and broad wear, no painted reward symbols. Provide intact and broken static variants with clearly named parts, base-centred pivots, metre scale, +Y up and +Z front. Target at most 600 triangles per intact prop and one shared matte material where practical. Deliver editable source and actual FBX files with front/side and 45-degree isometric previews. Broken versions are static visual states; do not add physics, colliders, loot logic or damaging fragments. With an image-only tool deliver concepts explicitly labelled as concepts.`
- **Negative prompt:** `photorealism, glass splinters, gore, complex rigidbody simulation, guaranteed loot marks, text, watermark`.
- **Aceite:** formas distintas a128px, bounds estáveis entre variantes, sem aumentar footprint; F09 validado com primitivas antes.
- **Unity:** novos prefabs visuais, colliders/layers definidos pelo domínio e colocação que não bloqueia portas. Reutilizar quebra se houver burst; não registrar como inimigo da sala.

### A13 — Lanceiro corrompido (conceito, condicional F13)

- **Finalidade:** guiar futuro inimigo de investida. **Descrição visual:** esqueleto angular de cabeça grande, lança curta grossa à frente, ombreira assimétrica e base larga; postura de aviso claramente diferente do melee.
- **Estilo oficial:** low-poly 3D representado em imagem 2D. **Consistência:** SkeletonMelee/Elite FBX e conceitos, Hero para escala; não substituir família existente.
- **Perspectiva/enquadramento:** folha2D frente/lado/costas/45° e silhueta em preto; câmera ortográfica coerente. **Cores/materiais:** osso #CFC6A8, ferro #3E4152, ferrugem #8A4A22, olhos vermelhos #FF2A2A.
- **Formato:** PNG 2048×1536 e fonte, fundo neutro; conceito 2D, sem rig/FBX/clip prometido. **Prioridade/complexidade:** P2/M.
- **Prompt inglês:** `Create a 2048x1536 concept sheet for an original corrupted skeleton spearman, a proposed new enemy for the attached low-poly dark medieval dungeon. Match the existing SkeletonMelee and Elite family: oversized angular skull, broad simple bone planes, dark corroded iron and rust, restrained red eyes. Add one thick short spear held forward and an asymmetric shoulder plate, with a wide readable stance distinct from the sword enemy. Show consistent orthographic front, side and back views plus a fixed 45-degree isometric gameplay view and a black silhouette. Include a separate windup pose suggesting a committed straight lunge without changing body proportions. Neutral background, no labels. This is 2D visual development only, not a functional mesh, rig or animation clip.`
- **Negative prompt:** `new art style, photorealism, thin spear, copyrighted character, extra limbs, text, watermark, gore`.
- **Aceite:** identidade entre vistas, distinção em silhueta128px, arma não torna hitbox implícita; design de F13 antes de encomendar 3D.
- **Unity:** importar como Default/sRGB para referência, não Sprite de gameplay; modelagem/Generic/clip e integração serão tarefas posteriores separadas com fonte real.

### A14 — Sentinela de Pedra (conceito, condicional F15)

- **Finalidade:** direção para Boss novo opcional. **Descrição visual:** guardião de blocos rachados, torso baixo largo, braços em pilares e núcleo ciano estreito; sem elmo/marreta do Warden.
- **Estilo oficial:** low-poly 3D em conceito 2D, massa grande e poucas facetas. **Consistência:** _GameArt/runas e Warden apenas para escala/nível de detalhe; personagem original.
- **Perspectiva/enquadramento:** frente/lado/costas/45° e postura de leque frontal; escala comparativa Hero. **Cores/materiais:** pedra #55596B/#3E4152, musgo #4C5A44, runas #4FD8FF contidas, rachadura ameaçadora laranja opcional.
- **Formato:** PNG 2048×1536 + fonte; conceito 2D. **Prioridade/complexidade:** P3/M.
- **Prompt inglês:** `Design an original Stone Sentinel boss concept for the attached stylized low-poly medieval dungeon, conditional on a future prototype. A broad low torso made of large cracked grey-blue stone blocks, pillar-like arms, restrained moss and a narrow inset cyan core; avoid the existing Warden's horned helmet and maul silhouette. Show coherent orthographic front, side and back views, a fixed 45-degree isometric view, one readable frontal-sweep anticipation pose and a small blue-caped Hero for scale. Broad faceted planes, matte stone, sparse cyan runes, clear limb separation and an open readable combat footprint. Deliver a 2048x1536 neutral-background PNG and editable source. This is 2D concept art, not a model, rig, combat specification or animation clip.`
- **Negative prompt:** `copyrighted boss, photorealistic rubble, hundreds of tiny stones, copied Warden, text, watermark, full-screen magic`.
- **Aceite:** corpo/ataque legíveis sobre chão, silhueta distinta, sem presumir nova fase/dano. F15 validado antes de arte final.
- **Unity:** textura Default/sRGB apenas de referência; malha/rig Generic/takes futuros têm aceite próprio, nunca inferidos da imagem.

### A15 — Cripta inundada (concept art, condicional F14)

- **Finalidade:** explorar identidade de segundo andar fixo. **Descrição visual:** pedra úmida, canais rasos nas bordas, passarela central seca, pilares baixos e nichos com runas; piso de combate sempre legível.
- **Estilo oficial:** low-poly 3D estilizado em pintura2D, fantasia medieval sombria. **Consistência:** DungeonRoom_Concept_01, ModularKit e _GameArt; preservar módulos2m/portas4m como referências de layout.
- **Perspectiva/enquadramento:** visão45° sem perspectiva dramática, folha complementar de peças; centro≈10×10m livre na sala normal16×16m.
- **Cores/materiais:** pedra #55596B/#3E4152, musgo #4C5A44, água azul escuro dessaturada, tochas #FF9A2E e runas ciano discretas.
- **Formato:** PNG 2048×1536 e fonte, imagem 2D opaca; concept art, sem mapa de colisão ou kit3D funcional. **Prioridade/complexidade:** P3/M.
- **Prompt inglês:** `Create a 2048x1536 environment concept for a proposed fixed second dungeon floor, using the attached current DungeonRoom and ModularKit references. Stylized low-poly dark medieval fantasy, large bevelled grey-blue stone blocks, damp muted surfaces, shallow water channels only along the edges and a dry open central fighting platform. Show a fixed readable 45-degree isometric view of a 16x16m interior with approximately 10x10m clear combat space, 4m door gaps and a 2m modular construction rhythm. Add low pillars, restrained moss, warm-orange torch accents and sparse cyan rune niches. Keep player routes, threats and floor boundaries readable. Include a separate small modular shape study without text. Deliver concept art and editable source only; do not claim a navigable level, mesh kit, water shader or collision map.`
- **Negative prompt:** `photorealism, deep opaque water covering combat, cluttered centre, giant fog, text, copyrighted environment, cinematic camera tilt, watermark`.
- **Aceite:** rotas claras e espaço de combate preservado; distinguir ambiente proposto de nível aprovado; sem prometer água interativa.
- **Unity:** Default/sRGB como referência. F14 constrói primeiro geometria jogável por builder; malhas, água, materiais, colliders e NavMesh ficam em integração posterior. Não substituir o andar atual por imagem.
