# F10 — plano

Base e0860e6, main atualizada; branch codex/f10-text-hud-scale.
Unity 6000.6.9f1 / UGUI 2.6.0 / Input System 1.20.1; pacotes preservados.

Escopo: JSON v4 na chave existente, migração v1–v3 para 100%, escolhas
100/125/150% e fallback 100%. Aplicar persiste, Voltar descarta; restaurar
padrões gerais altera somente o rascunho até Aplicar. Sem mudança de fonte.

Arquivos reservados: GameOptionsData/Presenter, InventorySlotView,
UiTextScale, TextScaleSceneBuilder, integração dos builders de shell/controles,
quatro cenas oficiais e testes EditMode/PlayMode pertinentes. Documentação:
GDD, arquitetura, MVP, NEXT_STEPS, BUILD-WINDOWS e Reports/F10.

Containers dimensionados para 150%, grid ancorado, detalhes/equipamento com
rolagem, dropdowns com linhas ampliadas, HUD redistribuído por região. A escala
só altera fontes por baselines serializadas, sem transformar o Canvas ou
reduzir os alvos de interação. Nomes muito longos recebem resumo com reticências
no slot e nome integral nos detalhes roláveis.

Aceite: migração/inválidos/persistência, aplicação repetida sem acumulação,
reset/voltar, foco e cliques de UI sem ataques, bolsa 16/16, nomes longos,
mensagens, HP/Boss e dropdowns. Capturas gráficas PlayMode 1280x720,
1920x1080 e 2560x1080 emulado, incluindo 100/125/150. Medição de geometria e
inspeção de imagens; sem certificar contraste, monitor ultrawide físico,
acessibilidade completa, gamepad ou escalas acima de 150%.
