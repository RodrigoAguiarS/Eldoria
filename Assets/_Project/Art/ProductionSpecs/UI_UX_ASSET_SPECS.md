# Especificações de UI/UX e assets de interface

> Consolidação de 14/11/2026: fichas repetidas foram substituídas por índices por ID; os dados e prompts completos permanecem no manifesto, sem alteração. Ler somente o registro necessário. Lotes ANIM-TORCH e ITEMS-COMBAT já foram produzidos/integrados; o diagnóstico introdutório antigo abaixo é histórico onde divergir desses lotes. Novas sugestões prontas para escolha estão em [PROMPT_CATALOG](../../../../PROMPT_CATALOG.md).

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

- **Estado atual:** ItemVisualCatalog fornece ícones; slots têm quantidade e seleção por moldura/losango; PotionHudPresenter mostra total/estado. Detalhes fixos já cumprem consulta de atributos. Demais tabelas de proposta abaixo são históricas quando divergem do lote ITEMS-COMBAT.
- **Limite visual:** textos18–24 px no canvas1920 escalam aproximadamente a 12–16 px em 720p; isso não prova ilegibilidade, mas exige conferir acentos, nomes longos como Lâmina do Guardião e linha Raro·Equipado com ícone novo. Não há prova nova de wrap/contraste ou falha de foco em cada tamanho.
- **Diretriz de validação:** manter texto informativo >=18 px na referência1080p e contraste alvo4,5:1 (texto)/3:1(elementos grandes), sinal redundante além de cor. São metas das skills para QA, não certificação atingida. Medir sobre background final considerando alpha. Ampliação200%/ultrawide são sugestões de acessibilidade a avaliar, sem prometer suporte implementado.
- **Animações:** transições atuais são imediatas. UI-TRANSITION propõe fade0,10–0,15 s unscaled, sem atrasar foco/click, nem reabrir input abaixo; reduzir movimento pode ser exigido em tarefa futura, não existe opção nova automaticamente.
- **Sem confirmação/descarte/campanha:** voltar ao menu/reiniciar perde run conforme produto aprovado; um modal de confirmação pode ser sugerido ao design, mas não está na lista obrigatória de produção.
- **Recursos fora da UI:** sem dano flutuante nem toast positivo por aquisição localizado. HP/flash/áudio e valores de progressão já dão feedback funcional; não gerar painéis ricos que dependem de eventos de payload inexistentes.

## Aceite por tela

Abrir cada superfície com entradas reais, conferir foco inicial/Tab/Enter/Esc e todos botões, esconder input inferior, I/Tab eQ, pause/options/retorno, morte/vitória e reinício. Testar1280×720 e1920×1080 com maior nome e bolsa cheia de todas raridades/potências; dropdown aberto, botão disabled, slotselecionado+equipado e aviso decheio. Screenshots de cada estado e resolução, sem inferir suporte físico ultrawide. Layout anterior observado nas duas resoluções consta na release; não foi reexecutado aqui.

## Fichas detalhadas

<a id="ui-main"></a>

## UI-MAIN — Menu principal

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-MAIN]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="ui-options"></a>

## UI-OPTIONS — Configurações

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-OPTIONS]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="ui-pause"></a>

## UI-PAUSE — Pausa

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-PAUSE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="ui-inventory"></a>

## UI-INVENTORY — Inventário

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-INVENTORY]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="ui-slots"></a>

## UI-SLOTS — Slots e estados

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-SLOTS]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="ui-equipment"></a>

## UI-EQUIPMENT — Equipamentos e status

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-EQUIPMENT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="ui-details"></a>

## UI-DETAILS — Detalhes do item selecionado

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-DETAILS]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="ui-death"></a>

## UI-DEATH — Tela de morte

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-DEATH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="ui-victory"></a>

## UI-VICTORY — Tela de vitória

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-VICTORY]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="hud-hp"></a>

## HUD-HP — Vida do jogador

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=HUD-HP]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="hud-dash"></a>

## HUD-DASH — Cooldown do dash

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=HUD-DASH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="hud-stamina"></a>

## HUD-STAMINA — Estamina e giro

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=HUD-STAMINA]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="hud-progression"></a>

## HUD-PROGRESSION — XP, nível, ouro e arma

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=HUD-PROGRESSION]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="hud-floor"></a>

## HUD-FLOOR — Progresso das salas

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=HUD-FLOOR]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="hud-boss"></a>

## HUD-BOSS — Vida do Guardião

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=HUD-BOSS]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="hud-enemy"></a>

## HUD-ENEMY — HP inimigo na arena de teste

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=HUD-ENEMY]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="hud-notice"></a>

## HUD-NOTICE — Avisos de inventário

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=HUD-NOTICE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="ui-type"></a>

## UI-TYPE — Tipografia e widgets atuais

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-TYPE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="hud-potions"></a>

## HUD-POTIONS — Atalho/quantidade de poções no HUD

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=HUD-POTIONS]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="icon-action-dash"></a>

## ICON-ACTION-DASH — Dash — ícone

**Estado atual:** AUSENTE. **Tipo:** ícone 2D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ICON-ACTION-DASH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="icon-action-special"></a>

## ICON-ACTION-SPECIAL — Giro de espada — ícone

**Estado atual:** AUSENTE. **Tipo:** ícone 2D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ICON-ACTION-SPECIAL]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="ui-skin"></a>

## UI-SKIN — Partes modulares da UI

**Estado atual:** AUSENTE. **Tipo:** interface. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-SKIN]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="ui-transition"></a>

## UI-TRANSITION — Transições discretas

**Estado atual:** AUSENTE. **Tipo:** interface. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-TRANSITION]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.
