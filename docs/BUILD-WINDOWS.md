# Build e reconstrução — Windows 1.0

Unity **6000.6.9f1**, URP 17.6.0 e pacotes fixados em `Packages/manifest.json`/`packages-lock.json`. Abra o projeto por essa versão; não atualize pacotes para reproduzir esta entrega. Feche PlayMode e qualquer outra instância do Editor antes de executar batchmode no mesmo projeto.

## Abrir o projeto existente

Use Unity Hub → Add project → esta pasta, com o Editor fixado e módulo Windows Build Support. Este repositório já contém jogo, Git, pacotes, metas, cenas e dados; não crie outro projeto nem execute git init para continuar. Input é exclusivamente New Input System; Force Text e metas visíveis já foram configurados. Conferir FoundationValidator e manifest/lock antes de corrigir configuração.

Para jogar a revisão atual e testar itens sem depender de drops aleatórios, siga [COMO-TESTAR](../COMO-TESTAR.md). Os atalhos de aff1895 abrem MainMenu ou a cena real de release; o modo com itens é diagnóstico e deve ser identificado como tal nas evidências. SpriteLab/billboard são experimentos separados. Testar aparência não exige reconstruir o andar.

## Build

No PowerShell, a partir de `G:\DungeonCrawler\My project`:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'G:/DungeonCrawler/My project' -buildTarget win64 -executeMethod DungeonCrawler.Editor.MvpReleaseBuild.BuildWindows -quit -logFile 'G:/DungeonCrawler/My project/Reports/Leadership/Windows-rebuild.log'
```

A chamada pode retornar antes de o processo Unity terminar. Aguarde o processo e confira `Reports/Leadership/windows-build.json`: versão 1.0.0, `Succeeded`, zero erros e `development=false`. Saída: `Builds/WindowsMVP/Eldoria.exe` (antes de D-003: `DungeonCrawler.exe`), com MainMenu e Floor_01_GeneratedCharacters. O menu equivalente é **Tools / Dungeon Crawler / Build / Windows Release (Current MVP)**.

`BuildWindowsProfile` gera somente a build Development de diagnóstico em `Builds/WindowsProfile`; não a distribua. Flags, carga e limites das medições: `Tools/AudioGenerator/DIAGNOSTICS.md` e `Reports/Leadership/WindowsPerf/README.md`.

## Testes

Execute separadamente, aguardando uma suíte terminar antes de iniciar a outra. Não use `-quit` com `-runTests` nesta instalação. Para execuções novas, escolha diretório e nomes novos de XML/log, preservando EditMode-final/PlayMode-final históricos. Alguns testes EditMode de idempotência salvam assets; registre hashes/diff antes/depois e não restaure mudanças de terceiros automaticamente.

```powershell
$env:DUNGEON_CRAWLER_TEST_REPORT_DIR='Reports/Leadership/FinalPerf'
& 'C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'G:/DungeonCrawler/My project' -runTests -testPlatform EditMode -testResults 'G:/DungeonCrawler/My project/Reports/Leadership/EditMode-final.xml' -logFile 'G:/DungeonCrawler/My project/Reports/Leadership/EditMode-final.log'
```

Repita com `-testPlatform PlayMode`, resultado `PlayMode-final.xml` e log `PlayMode-final.log`. A variável separa os relatórios novos dos históricos da Task 02. Os resultados da entrega ficam em `Reports/Leadership`; a matriz de cobertura e aceite manual está em `RELEASE-1.0-VALIDATION.md`.

O exemplo acima reconstrói os caminhos da entrega histórica: para auditoria nova, troque **todos** os destinos por uma pasta exclusiva (por exemplo Reports/ProjectAudit/<execucao>) antes de executar. Confira o NUnit XML (passed/failed/skipped), não só o código do processo. Um teste Explicit de captura pode ser ignorado em batchmode; não apresentá-lo como revisão visual concluída.

## Roteiro de regressão

Este roteiro consolida M1–M4 e a QA do candidato. Registrar commit, build/versão, cena, resolução, origem da evidência e resultado; não marcar etapas por presença de código. Valores atuais vêm de SOs e [BALANCE-1.0](BALANCE-1.0.md), não dos checklists antigos.

1. **Fundação e arena:** Console sem erros, referências resolvidas; Bootstrap inicia com ações Gameplay; na MVP_Arena conferir chão/obstáculos/luz, câmera isométrica fixa seguindo suavemente, WASD relativo à tela no XZ e mira horizontal pelo mouse. Inimigo contorna pilares com NavMesh; player não usa Rigidbody.
2. **Combate e sobrevivência:** clique/segurar executa combo01/02/03 com impactos únicos, buffer limitado e retorno à locomoção; dash em movimento/parado respeita cooldown/invulnerabilidade. HP/HUD/flash/shake respondem; morte cancela controles/dano pendente, R e botão reiniciam. Registrar câmera/pose/impacto observados, não só valores.
3. **Salas:** percorrer quatro salas normais; entrada fecha, onda inicia, saída fica bloqueada enquanto um inimigo vive e abre somente após a última morte. Conferir ranged/elite, recuo/tiros, colisão em parede/porta e pool reutilizado; sem atravessar portas ou softlock. O quinto encontro é o Boss: não esperar vitória após sala4.
4. **Boss:** HUD aparece só durante a luta, Slam/Volley avisam antes do dano; transição única a50% com3s invulnerável, visual persistente no Warden e novo padrão. Telegraph nunca abaixo1s; sair da área/dash evita dano segundo as regras. Derrotar até5/5 e observar vitória; depois testar derrota→reinício. Preview de fase/diagnóstico não é prova de vitória normal.
5. **Progressão/bolsa:** XP/ouro imediatos; nível/dano/cura coerentes; armas/poções/armadura entram na bolsa. Testar16slots, stack5 e potências distintas, primeira arma e troca manual, desequipar, defesa, Q/Usar ferido/cheio/morto, Largar1 em espaço livre/bloqueado. Coleta recusada preserva pickup e permite nova tentativa após liberar espaço. I/Tab pausa; Esc volta; UI não dispara ataque.
6. **Visual pós-release:** modelos próprios de itens no chão, ícones/quantidade/estado de poções, uma espada equipada e colete acompanhando Run/Dash/Hit/Death/combos/especial. Trails/faíscas só após acerto aceito; giro com anel, sem cubo branco na apresentação normal; chama animada. Use diagnóstico de itens para cobertura visual e partida normal separada para loot/progressão.
7. **Menus/opções:** MainMenu→Jogar inicia run nova; pausa/bolsa/opções/foco/retorno corretos; terminais fecham overlays e focam Reiniciar. Aplicar persiste, Voltar descarta rascunho. Testar1280×720 janela e1920×1080 tela cheia, dropdowns/textos longos, presets Leve/Padrão sem rosa; fechar/reabrir preserva opções e restaurar preferências anteriores. Ultrawide emulado não comprova monitor físico.
8. **Áudio/legibilidade/build:** música/SFX em0 silenciam categorias; flash/tremor em0 removem efeitos opcionais sem ocultar perigo/fase2. Ouvir ataque/acerto/dano/coleta/morte/rugido/vitória/música, anotar sons ausentes/desconforto/distorção. No Windows normal, concluir andar e derrota/reinício; triar log para exceções/Missing Script/avisos Animator. Métricas de perfil, diagnóstico sintético e escuta humana são evidências distintas; repetir com mesma configuração antes de alegar melhoria.

Aceite humano complementa testes automatizados. Se não abriu a cena e observou PlayMode, a revisão visual permanece `NÃO VALIDADO NO EDITOR`.

### Regressão F04 — Controles

Em Opções → Controles, testar captura de tecla e botão do mouse, conflito (por exemplo dash em Q), Escape/Cancelar/Voltar, timeout de 10 s com partida pausada e perda de foco. Binding rejeitado/cancelado preserva o anterior. Remapear bolsa, poção, pausa e reinício; Escape continua voltando, poção funciona na bolsa e os cliques nos menus não atacam nem consomem. Conferir rótulos do título/HUD/bolsa/opções/derrota, morte→reinício com a tecla nova, run nova sem inventário e overrides preservados após relançar. Restaurar padrão deve persistir após outro relançamento. Navegação UI/mira continuam fixas; sem suporte gamepad completo.

Builder específico: `DungeonCrawler.Editor.ControlBindingsSceneBuilder.Integrate`, menu **Tools / Dungeon Crawler / Input / Integrate Control Remapping (F04)**. Build isolada de validação: `ControlBindingsSceneBuilder.BuildWindowsValidation` gera `Builds/F04/Eldoria.exe` e `Reports/F04/windows-build.json`, recusando sobrescrever esse relatório; não modifica o pacote histórico. Para execuções posteriores, escolher novos destinos. Testes, comandos e limites da entrega em [Reports/F04](../Reports/F04/README.md).

## Reconstrução da composição

Os assets necessários já estão versionados. Não é necessário regenerar personagens, cenário ou imagens para construir o jogo.

- **Tools / Dungeon Crawler / Combo / Integrate Hero Combo** configura combo e referências preservando o SO existente.
- **Tools / Dungeon Crawler / Audio / Integrate Functional Audio** integra áudio, shell e diagnóstico pelas APIs do Editor, reaproveitando definições existentes. O gerador original `python Tools/AudioGenerator/generate.py` só é necessário para reproduzir os WAVs; documentado junto à ferramenta.
- O builder do andar integra a cópia gerada e o shell ao terminar. O build final usa lista explícita MainMenu → Floor_01_GeneratedCharacters; cenas de teste continuam disponíveis no Editor. Não regenere inimigos como parte de ajustes de animação do Hero.
- GUIDs e ajustes de dados existentes devem ser preservados. Não substitua cenas/prefabs por edição textual. Builders recusam assets existentes que não carregam, em vez de sobrescrevê-los.

O pacote Unity de inferência pode adicionar `SENTIS_ANALYTICS_ENABLED` ao carregar o Editor. Isso foi identificado como efeito do pacote; não é requisito do gameplay. Confira o diff e preserve alterações desconhecidas. Os avisos conhecidos de shaders ficam documentados no relatório da entrega.

## Pacote local

Após build final e testes aprovados:

```powershell
python Tools/Release/package_windows.py
```

Gera `Builds/Release/<produto>-<versão>-windows-x64.zip` (nome do executável da build; ex.: `Eldoria-…`; o pacote histórico 1.0.0 é `DungeonCrawler-1.0.0-windows-x64.zip`), README, BUILD-INFO e manifesto SHA256. Verifica PE x64, CRC e hashes; exclui pastas de backup/debug e recusa sobrescrever um pacote existente. Não publica nem envia arquivos. Para outra revisão, preserve o pacote anterior e versione explicitamente o novo artefato.
