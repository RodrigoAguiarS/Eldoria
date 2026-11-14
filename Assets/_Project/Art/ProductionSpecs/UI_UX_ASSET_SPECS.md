# Especificações de UI/UX e assets de interface

Data: 14/11/2026. A 1.0 já contém menu, opções, pausa, inventário, morte/vitória e HUD. O diagnóstico abaixo distingue funcionamento comprovado anteriormente de acabamento e cobertura visual pendentes. **NÃO VALIDADO NO EDITOR nesta auditoria.**

## Lote ITEMS-COMBAT — inventário e HUD de poções (14/11/2026)

Slots 188×104 com ícone 64 px, quantidade sobreposta (×N), nome/raridade/"Cura N"/"Equipado" em texto e seleção por moldura + marcador em losango (não só cor). Detalhes e resumo de equipamento mostram o mesmo ícone. HUD-POTIONS (canto superior esquerdo, abaixo da estamina): ícone, tecla Q, total somado e estado textual (Pronto · +N HP / HP cheio / Sem poções / Indisponível). Observado em 2560×1440 no Editor (`Reports/Leadership/ProductionVisual/Captures/inventory.png`); 1280×720 e 1920×1080 observados na build (ver nota abaixo).

**Conferência na build Windows (14/11/2026, controle autorizado pelo usuário):** partida normal até a sala 2 em 1920×1080 tela cheia e em 1280×720 janela (opções restauradas para 1920×1080/tela cheia/Alta ao final). Observados: drops reais de ouro (pilha de moedas), XP (orbe com anel) e poção; Largar 1 recria o frasco no chão; pilha ×2 da mesma potência com ícone, "Cura 15" e detalhes com ícone; HUD de poções ×N/Pronto/Sem poções/Indisponível (morto); Q consome uma unidade; layouts de 720p e 1080p sem cortes. No modo diagnóstico `-dcc-stress` da build: rastro na espada, faíscas/lascas em acertos e anel do giro, sem o retângulo branco. Logs do player sem exceções/Missing Script. Capturas em `Reports/Leadership/ProductionVisual/Captures/Build`. **Não observado na build:** espadas e colete equipados (nenhuma arma/armadura dropou até a sala 2; o visual equipado foi verificado no PlayMode da mesma cena).

## Sistema atual e direção preservada

UGUI Canvas ScreenSpaceOverlay, CanvasScaler ScaleWithScreenSize, referência1920×1080, match .5. Text e LegacyRuntime.ttf, InputSystemUIInputModule. Fundos azul-preto (.035,.05,.075) e janelas (.055,.075,.1), overlay opaco/translúcido conforme builder; botões dourados (.8,.68,.45) com texto escuro (.04,.06,.08). Esses valores são do código de composição, não nova paleta. Manter consistência com pedra/couro/latão; arte complementar discreta, texto em runtime. Sem trocar biblioteca, fonte ou tema.

O relatório API comprova hierarquias/componentes mas não serializa todos RectTransforms. Dimensões abaixo vêm dos builders atuais; versões salvas têm que ser conferidas no Editor antes de integrar. Não presumir que uma imagem de referência ou nova skin define estados/código.

## Fluxo de telas e entradas

Menu inicial → Jogar → Floor gerada; iniciar/reiniciar cria run nova. MenuScreenStack enum contém Title/Pause/Inventory/Options; ao empilhar esconde a anterior, ao voltar mostra a anterior e seleciona seu **firstFocus configurado**, sem guardar um histórico arbitrário de foco por botão. Morte/vitória encerram overlays e selecionam reinício. I/Tab abre bolsa e pausa offline; Esc volta/pausa; Q continua como ação separada durante bolsa; menu não deve reativar controles de jogador morto.

Menu atual só tem Jogar/Opções/Sair: não adicionar Continuar/save. Pausa tem Continuar/Reiniciar/Opções/Voltar ao menu/Sair. Configurações edita uma cópia e só aplica/salva via botão; Voltar descarta rascunho. Não desenhar modal de confirmação como se implementado. Nova confirmação seria decisão de produto, não requisito da auditoria.

## Layout por superfície

| Superfície/ID | Hierarquia e geometria de referência | Conteúdo/estados e preservação |
| --- | --- | --- |
| UI-MAIN | MainMenu/TitlePanel/Content 1100×820 central | Título76 px; descrição 28; botões 500×72 (Jogar/Opções/Sair); controles 24; foco Jogar |
| UI-OPTIONS | OptionsPanel/Window 1040×860 | Título42; quatro linhas de sliders Música/SFX/Tremor/Flash com valores; Qualidade/Resolução dropdown; Tela cheia toggle; status; Aplicar e salvar/Voltar |
| UI-PAUSE | PausePanel/Window 540×630 | Título42; botões 380×60, Esc volta, foco Continuar. Sem input na camada inferior |
| UI-INVENTORY | InventoryPanel/Window 1180×780, central | Topo título32/capacidade; fecharI/Tab/Esc20; grade4×4; lateral de 298 px; cinco ações inferiores; pausado |
| UI-SLOTS | Slot_00…15:188×104, origem(32,-118), passo(200,116) | Hoje nome + linha 18 px de raridade/equipado, quantidade no nome. Ícone novo48 px pode usar coluna reservada e reduzir texto somente após medir wrap |
| UI-EQUIPMENT | Lateral x 850,y -118,298×230 | Arma/Armadura/Dano/Defesa/HP; só 2 slots lógicos, sem silhueta de corpo obrigatória |
| UI-DETAILS | Lateral x 850,y -370,298×240 | Nome/raridade/stat/quantidade/ajuda; detalhes fixos da seleção, não hover tooltip |
| Ações bolsa | y -670:Equipar x 32,Desequipar 232,Usar 432,Largar 1 x632,188×58; Voltar x 850,298×58 | Interactable vem do domínio; arte não decide consumo ou equipamento |
| UI-DEATH/UI-VICTORY | GameOverPanel/VictoryPanel overlays | Mensagem, Reiniciar/Jogar novamente, Voltar ao menu; preservar firstFocus, estado terminal e run nova |
| HUD-HP/HUD-DASH | PlayerHealthBar/DashBar | HP e recarga com barra; dash cheio=pronto. Não desenhar mana inexistente |
| HUD-STAMINA | (24,-124),500×44, fonte20 e barra5 px | Valor estamina e giroE/direito:pronto/custo 30/cooldown/sem estamina |
| HUD-PROGRESSION | XpBar/ProgressionLabel | Nv/XP/Ouro/Arma do PlayerProgression; sem slots deouro |
| HUD-FLOOR/HUD-BOSS | FloorProgress/BossHealthBar | Salas/5 e HPBoss só na luta; não confundir com minimapa/barras de todos inimigos |
| HUD-NOTICE | InventoryNotice central baixo,1100×48,24 px | Avisos3 s unscaled; feedback negativo/coleta recusada e item largado |
| HUD-POTIONS (proposta P1) | Novo bloco160×64 com ícone 48×48, top-left x 24/y -180 | Q, total e motivo de indisponível; confirmar colisão com HUD real antes de salvar |

Uma composição futura pode ajustar âncoras/containers para preservar essas relações, sem impor coordenadas de uma imagem a todas as resoluções. Não redesenhar a tela inteira ao integrar um ícone.

## Estados visuais dos componentes

| Componente | Estado atual | Alvo de apresentação, sem alterar regra |
| --- | --- | --- |
| Slot vazio | Travessão/Vazio cinza | Sem ícone; manter sinal de vazio, foco ainda visível |
| Ocupado | Nome/raridade com cor | Ícone individual + nome/raridade em runtime |
| Selecionado | Fundo(.32,.26,.16), demais(.10,.13,.17) | Cantos/contorno de seleção além da cor; independe de raridade |
| Hover | Slot Transition.None | Se adotado, leve destaque de borda; não trocar seleção automaticamente sem regra aprovada |
| Foco teclado | OnSelect seleciona item; fundo representa seleção | Marcador de foco distinto de hover; manter seleção e foco coerentes |
| Pressed | Botões UGUI padrão; slot sem transição | Escurecimento/recuo visual até2 px, sem mover hit target nem repetir comando |
| Equipado | Texto Equipado | Badge/símbolo+texto separado do PNG; item permanece no slot |
| Equipar | Só vivo,gear e não equipado | Disabled sem depender só de opacidade/cor; manter label |
| Desequipar | Só gear selecionado já equipado | Disabled quando aplicável; não destruir item |
| Usar | Só poção,vivo,HP<max | Desabilitar em HPcheio/morto/sem item, não sugerir cooldown |
| Largar 1 | Só vivo,item não vazio; erro se sem posição | Preservar mensagem de erro nos detalhes; não abrir nova regra de descarte |
| Quantidade | ×N no nome seN>1 | Badge numérico externo ao ícone; N=1 ainda item válido |
| Raridade | Comum/Raro/Épico por nome/cor | Nome permanece; borda/forma externa opcional, sem bônus inferido |
| Giro | Texto cooldown/custo/pronto/sem estamina | Ícone opcional sem esconder segundos/custo; não desenhar skill tree |
| Dash | Barra cheia=pronto | Ícone opcional e recarga coerente com barra atual |
| PoçõesHUD | Ausente | TotalQ com pronto/0/HPcheio/morto; seleção/consumo continuam no domínio |
| Options | Sliders/dropdowns/toggle, status e aplicar | Hover/focus/pressed/disabled coerentes; dropdown acima sem cortar itens |

Ícones de item não devem conter número, nome, raridade, cooldown ou fundo quadriculado. UI-SKIN propõe só partes reutilizáveis no tema existente, não nova direção. ICON-ACTION-DASH/SPECIAL são P2; não produzir ícone de habilidade nova.

## Legibilidade, movimento e problemas identificados

- **Confirmado em código:** ausência de binding/ícones de item; slots sem transição; destaque de seleção depende principalmente da cor; sem quantidade de poção no HUD. Detalhes fixos já cumprem a consulta de atributos e não precisam virar tooltip novo.
- **Limite visual:** textos18–24 px no canvas1920 escalam aproximadamente a 12–16 px em 720p; isso não prova ilegibilidade, mas exige conferir acentos, nomes longos como Lâmina do Guardião e linha Raro·Equipado com ícone novo. Não há prova nova de wrap/contraste ou falha de foco em cada tamanho.
- **Diretriz de validação:** manter texto informativo >=18 px na referência1080p e contraste alvo4,5:1 (texto)/3:1(elementos grandes), sinal redundante além de cor. São metas das skills para QA, não certificação atingida. Medir sobre background final considerando alpha. Ampliação200%/ultrawide são sugestões de acessibilidade a avaliar, sem prometer suporte implementado.
- **Animações:** transições atuais são imediatas. UI-TRANSITION propõe fade0,10–0,15 s unscaled, sem atrasar foco/click, nem reabrir input abaixo; reduzir movimento pode ser exigido em tarefa futura, não existe opção nova automaticamente.
- **Sem confirmação/descarte/campanha:** voltar ao menu/reiniciar perde run conforme produto aprovado; um modal de confirmação pode ser sugerido ao design, mas não está na lista obrigatória de produção.
- **Recursos fora da UI:** sem dano flutuante nem toast positivo por aquisição localizado. HP/flash/áudio e valores de progressão já dão feedback funcional; não gerar painéis ricos que dependem de eventos de payload inexistentes.

## Aceite por tela

Abrir cada superfície com entradas reais, conferir foco inicial/Tab/Enter/Esc e todos botões, esconder input inferior, I/Tab eQ, pause/options/retorno, morte/vitória e reinício. Testar1280×720 e1920×1080 com maior nome e bolsa cheia de todas raridades/potências; dropdown aberto, botão disabled, slotselecionado+equipado e aviso decheio. Screenshots de cada estado e resolução, sem inferir suporte físico ultrawide. Layout anterior observado nas duas resoluções consta na release; não foi reexecutado aqui.

## Fichas detalhadas

## UI-MAIN — Menu principal

**Categoria:** F — Menus e interfaces. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** TitlePanel com título, descrição, Jogar/Opções/Sair e controles; Content 1100×820, fonte título 76, botões 500×72.

**Evidências:**

- [Assets/_Project/Scripts/UI/MainMenuController.cs](../../../../Assets/_Project/Scripts/UI/MainMenuController.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Scenes/MainMenu.unity](../../../../Assets/_Project/Scenes/MainMenu.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## UI-OPTIONS — Configurações

**Categoria:** F — Menus e interfaces. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Window 1040×860: Música/SFX/Tremor/Flash, qualidade, resolução, tela cheia, status, Aplicar e salvar/Voltar. Fontes 22–42.

**Evidências:**

- [Assets/_Project/Scripts/UI/GameOptionsPresenter.cs](../../../../Assets/_Project/Scripts/UI/GameOptionsPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Scenes/MainMenu.unity](../../../../Assets/_Project/Scenes/MainMenu.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## UI-PAUSE — Pausa

**Categoria:** F — Menus e interfaces. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Window 540×630: Continuar, Reiniciar, Opções, Menu, Sair; só topo interativo.

**Evidências:**

- [Assets/_Project/Scripts/UI/GameMenuController.cs](../../../../Assets/_Project/Scripts/UI/GameMenuController.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## UI-INVENTORY — Inventário

**Categoria:** D — Inventário e itens. **Status:** INCOMPLETO. **Prioridade:** P1. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado e funcional; apresentação parcial.

**Visual:** Window central 1180×780, título/capacidade, grade 4×4, lateral equipamento/detalhes; ações inferiores. Funcional textual; ícones ausentes.

**Evidências:**

- [Assets/_Project/Scripts/UI/InventoryPresenter.cs](../../../../Assets/_Project/Scripts/UI/InventoryPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE, ICON-ITEM-IRON, ICON-ITEM-WARDEN, ICON-ITEM-ANCIENT, ICON-ITEM-LEATHER, ICON-ITEM-HEALTH

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## UI-SLOTS — Slots e estados

**Categoria:** D — Inventário e itens. **Status:** INCOMPLETO. **Prioridade:** P1. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado e funcional; apresentação parcial.

**Visual:** 16 slots 188×104, passo 200×116; nome/raridade/quantidade/Equipado; vazio com travessão. Seleção por fundo; Transition.None sem hover/focus distinto.

**Evidências:**

- [Assets/_Project/Scripts/UI/InventorySlotView.cs](../../../../Assets/_Project/Scripts/UI/InventorySlotView.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE, ICON-ITEM-IRON, ICON-ITEM-WARDEN, ICON-ITEM-ANCIENT, ICON-ITEM-LEATHER, ICON-ITEM-HEALTH

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## UI-EQUIPMENT — Equipamentos e status

**Categoria:** D — Inventário e itens. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Resumo lateral textual: arma/armadura/dano/defesa/HP; não há paper doll nem tela própria.

**Evidências:**

- [Assets/_Project/Scripts/UI/InventoryPresenter.cs](../../../../Assets/_Project/Scripts/UI/InventoryPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## UI-DETAILS — Detalhes do item selecionado

**Categoria:** D — Inventário e itens. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Nome, raridade, atributo, quantidade e ajuda. Painel fixo, não tooltip hover.

**Evidências:**

- [Assets/_Project/Scripts/UI/InventoryPresenter.cs](../../../../Assets/_Project/Scripts/UI/InventoryPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## UI-DEATH — Tela de morte

**Categoria:** F — Menus e interfaces. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** GameOverPanel: Reiniciar/Voltar ao menu; foco inicial no reinício.

**Evidências:**

- [Assets/_Project/Scripts/UI/GameOverPresenter.cs](../../../../Assets/_Project/Scripts/UI/GameOverPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## UI-VICTORY — Tela de vitória

**Categoria:** F — Menus e interfaces. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** VictoryPanel: jogar novamente/menu; conclusão 5/5.

**Evidências:**

- [Assets/_Project/Scripts/UI/VictoryPresenter.cs](../../../../Assets/_Project/Scripts/UI/VictoryPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## HUD-HP — Vida do jogador

**Categoria:** G — HUD e gameplay. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Barra de HP por eventos Health.

**Evidências:**

- [Assets/_Project/Scripts/UI/HealthBarPresenter.cs](../../../../Assets/_Project/Scripts/UI/HealthBarPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## HUD-DASH — Cooldown do dash

**Categoria:** G — HUD e gameplay. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Barra cheia = pronto; cooldown default 2 s; ícone ausente.

**Evidências:**

- [Assets/_Project/Scripts/UI/DashCooldownPresenter.cs](../../../../Assets/_Project/Scripts/UI/DashCooldownPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## HUD-STAMINA — Estamina e giro

**Categoria:** G — HUD e gameplay. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** PlayerResource (24,-124), 500×44, fonte 20: valor, barra, cooldown/custo/pronto/sem estamina.

**Evidências:**

- [Assets/_Project/Scripts/UI/PlayerResourcePresenter.cs](../../../../Assets/_Project/Scripts/UI/PlayerResourcePresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## HUD-PROGRESSION — XP, nível, ouro e arma

**Categoria:** G — HUD e gameplay. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Barra XP e texto Nv/XP/Ouro/Arma; sem moedas novas.

**Evidências:**

- [Assets/_Project/Scripts/UI/ProgressionPresenter.cs](../../../../Assets/_Project/Scripts/UI/ProgressionPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## HUD-FLOOR — Progresso das salas

**Categoria:** G — HUD e gameplay. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Contador de salas /5; não é minimapa.

**Evidências:**

- [Assets/_Project/Scripts/UI/FloorProgressPresenter.cs](../../../../Assets/_Project/Scripts/UI/FloorProgressPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## HUD-BOSS — Vida do Guardião

**Categoria:** G — HUD e gameplay. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Barra só durante encontro e some ao morrer; binding por EnemiesSpawned.

**Evidências:**

- [Assets/_Project/Scripts/UI/BossHealthPresenter.cs](../../../../Assets/_Project/Scripts/UI/BossHealthPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## HUD-ENEMY — HP inimigo na arena de teste

**Categoria:** G — HUD e gameplay. **Status:** EXISTENTE. **Prioridade:** P2. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** HealthBarPresenter adicional na MVP_Arena; não comprova barras individuais da multidão na Floor.

**Evidências:**

- [Assets/_Project/Scripts/UI/HealthBarPresenter.cs](../../../../Assets/_Project/Scripts/UI/HealthBarPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Scenes/MVP_Arena.unity](../../../../Assets/_Project/Scenes/MVP_Arena.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## HUD-NOTICE — Avisos de inventário

**Categoria:** G — HUD e gameplay. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** Cheio/morto/inválido/item largado, 3 s unscaled; sem toast positivo por item coletado.

**Evidências:**

- [Assets/_Project/Scripts/UI/InventoryStatusPresenter.cs](../../../../Assets/_Project/Scripts/UI/InventoryStatusPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## UI-TYPE — Tipografia e widgets atuais

**Categoria:** F — Menus e interfaces. **Status:** EXISTENTE. **Prioridade:** P0. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Apresentação da funcionalidade já entregue

**Integração:** Integrado; preservar e revalidar após mudanças.

**Visual:** UGUI Text/LegacyRuntime.ttf, fundos azul-preto/botões dourados; nenhuma fonte artística nova licenciada.

**Evidências:**

- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Reports/Leadership/asset-audit-current.json](../../../../Reports/Leadership/asset-audit-current.json) — Hierarquias/componentes serializados lidos; não é inspeção visual nova.
- [Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity](../../../../Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Aceite (pendente de execução para mudanças):**

- Clique/foco/seleção e retorno da pilha preservados; UI não causa ataque.
- Sem cortes a 1920×1080/1280×720; medir contraste após mudança.
- UI solicita comandos, sem calcular dano/consumo/equipamento.


## HUD-POTIONS — Atalho/quantidade de poções no HUD

**Categoria:** E — Poções e consumíveis. **Status:** AUSENTE. **Prioridade:** P1. **Tipo:** interface. **Escopo:** aprovado_1_0.

**Finalidade:** Leitura de Q sem abrir inventário

**Integração:** Produção/integração visual pendente.

**Visual:** Proposta: ícone frasco 48×48, Q e total; zero/HP cheio/morto indisponível, sem sugerir cooldown.

**Evidências:**

- [Assets/_Project/Scripts/UI/InventoryPresenter.cs](../../../../Assets/_Project/Scripts/UI/InventoryPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Player/PlayerInventoryInput.cs](../../../../Assets/_Project/Scripts/Player/PlayerInventoryInput.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/UI/PlayerResourcePresenter.cs](../../../../Assets/_Project/Scripts/UI/PlayerResourcePresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** ICON-ITEM-HEALTH, HUD-STAMINA

**Integração técnica pendente:**

- Presenter futuro somente leitura em Inventory.Changed/Health.Changed; não mudar escolha de Q nem capacidade.

**Dimensões/tempo:** Componente 160×64 no canvas 1920×1080; âncora superior esquerda/margem 24, proposta y=-180

**Entrega:** UGUI editável; ícone referenciado; textos runtime

**Contrato Unity:** UGUI editável; sprites e texto separados, referência 1920×1080/CanvasScaler match .5. Preservar Text/LegacyRuntime.ttf, InputSystemUIInputModule, pilha de menus e presenters atuais; imagem achatada não é interface funcional.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#hud-potions) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## ICON-ACTION-DASH — Dash — ícone

**Categoria:** G — HUD e gameplay. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** ícone 2D. **Escopo:** sugestão_polimento.

**Finalidade:** Leitura de comando existente

**Integração:** Produção/integração visual pendente.

**Visual:** Blue cape sweep with a broad forward chevron-shaped cutout, no weapon arrow, no text.

**Evidências:**

- [Assets/_Project/Scripts/UI/DashCooldownPresenter.cs](../../../../Assets/_Project/Scripts/UI/DashCooldownPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/UI/PlayerResourcePresenter.cs](../../../../Assets/_Project/Scripts/UI/PlayerResourcePresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Dimensões/tempo:** 512×512; leitura 48/32 px

**Entrega:** PNG RGBA + fonte

**Contrato Unity:** PNG RGBA 512×512 + fonte; alpha real, conteúdo 416×416 central/margem 48 px. Sprite Single, sRGB, mipmaps off, bilinear, clamp, PPU 100; testar a 48/32 px. Sem texto, números ou moldura de raridade baked.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#icon-action-dash) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## ICON-ACTION-SPECIAL — Giro de espada — ícone

**Categoria:** G — HUD e gameplay. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** ícone 2D. **Escopo:** sugestão_polimento.

**Finalidade:** Leitura de comando existente

**Integração:** Produção/integração visual pendente.

**Visual:** Short steel sword inside a broad cyan circular sweep, open centre, suggesting one complete rotation.

**Evidências:**

- [Assets/_Project/Scripts/UI/DashCooldownPresenter.cs](../../../../Assets/_Project/Scripts/UI/DashCooldownPresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/UI/PlayerResourcePresenter.cs](../../../../Assets/_Project/Scripts/UI/PlayerResourcePresenter.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** Nenhuma dependência lógica nova; preservar composição atual.

**Dimensões/tempo:** 512×512; leitura 48/32 px

**Entrega:** PNG RGBA + fonte

**Contrato Unity:** PNG RGBA 512×512 + fonte; alpha real, conteúdo 416×416 central/margem 48 px. Sprite Single, sRGB, mipmaps off, bilinear, clamp, PPU 100; testar a 48/32 px. Sem texto, números ou moldura de raridade baked.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#icon-action-special) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## UI-SKIN — Partes modulares da UI

**Categoria:** F — Menus e interfaces. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** interface. **Escopo:** sugestão_polimento.

**Finalidade:** Polimento opcional preservando telas atuais

**Integração:** Produção/integração visual pendente.

**Visual:** Manter azul-preto/dourado; chanfros discretos e foco por cantos além da cor. Não redesenhar telas.

**Evidências:**

- [Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs](../../../../Assets/_Project/Scripts/Editor/GameShellSceneBuilder.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.
- [Assets/_Project/Scripts/UI/InventorySlotView.cs](../../../../Assets/_Project/Scripts/UI/InventorySlotView.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-TYPE

**Dimensões/tempo:** Painel 128×128/borda16; botão 256×80/borda12; slot192×112/borda8; canto foco32×32

**Entrega:** Sprites RGBA separados 9-slice + fonte e mapa de estados

**Contrato Unity:** UGUI editável; sprites e texto separados, referência 1920×1080/CanvasScaler match .5. Preservar Text/LegacyRuntime.ttf, InputSystemUIInputModule, pilha de menus e presenters atuais; imagem achatada não é interface funcional.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#ui-skin) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.


## UI-TRANSITION — Transições discretas

**Categoria:** F — Menus e interfaces. **Status:** AUSENTE. **Prioridade:** P2. **Tipo:** interface. **Escopo:** sugestão_polimento.

**Finalidade:** Polimento opcional de ação existente

**Integração:** Produção/integração visual pendente.

**Visual:** Short panel opacity fade and subtle focus-corner settle; no panel sliding, no delayed interaction.

**Evidências:**

- [Assets/_Project/Scripts/UI/MenuScreenStack.cs](../../../../Assets/_Project/Scripts/UI/MenuScreenStack.cs) — Arquivo/família inspecionado; existência não equivale a aceite visual individual.

**Dependências:** UI-SKIN

**Integração técnica pendente:**

- MenuScreenStack atual mostra/oculta de imediato; camada de tween ainda ausente.

**Dimensões/tempo:** 0.10–0.15 s unscaled time; no input blocking

**Entrega:** UGUI editável + spec de tween

**Contrato Unity:** UGUI editável; sprites e texto separados, referência 1920×1080/CanvasScaler match .5. Preservar Text/LegacyRuntime.ttf, InputSystemUIInputModule, pilha de menus e presenters atuais; imagem achatada não é interface funcional.

**Condição de produção:** Lote futuro; nenhuma geração autorizada nesta etapa.

**Prompt completo em inglês:** [ASSET_GENERATION_PROMPTS.md](ASSET_GENERATION_PROMPTS.md#ui-transition) — mesmo ID; também disponível em `production.prompt_en` no manifesto.

**Aceite (pendente de execução para mudanças):**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.
