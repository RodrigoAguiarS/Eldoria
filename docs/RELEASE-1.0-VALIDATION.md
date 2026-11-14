# Validação da entrega Windows 1.0

Escopo: AGENTS, GAME_DESIGN, ARCHITECTURE, MVP e decisões D-001/D-002. Entrega Windows **1.0.0 concluída em 14/11/2026**, com as provas e limites abaixo. Pacote local `Builds/Release/DungeonCrawler-1.0.0-windows-x64.zip`, 53.496.573 bytes, SHA256 `c0b846b3d638ab16a046801136babd2cde64d4e53fd18bd191756b79daaa2f05`; nenhuma publicação externa.

## Matriz de evidências

| Requisito | Evidência inspecionada | Resultado e limite |
| --- | --- | --- |
| Fundação, versões e histórico | FoundationValidator, asset-audit-current.json; Git desde cf3720d, backup externo inicial | Unity 6000.6.9f1 e versões instaladas verificadas; histórico local, sem remoto |
| Preservação de referências/arte | Auditoria API com issues vazio; GUIDs/bytes protegidos por testes de builders; hero-special-preservation.json; changed-files-since-audit.txt | Zero referências inválidas diagnosticadas; imagens não alteradas; Hero mantém malha/materiais/12 clips anteriores ao especial |
| APIs e composição | Checkers estáticos e inspeção de runtime/builders; FoundationTests | New Input System, CharacterController.Move, namespaces DungeonCrawler, referências explícitas; cenas/prefabs atualizados pelo Editor |
| Combo 1/2/3, buffer e segurar | PlayerComboModelTests, PlayerInputComboTests e PlayerComboCombatTests em XMLs RC2; Reports/Task02 | Um comando antecipado, 0,15 s; recuperação final e três movimentos reais, sem reiniciar animação por AttackVisual |
| Autoridade/cancelamento e multidão | PlayerComboCombatTests: morte/dash/disable, Animator inativo, progressão, deduplicação 8/9/32/150; relatório Task02 | Dano por timer/eventos C#, sem duplicação por AnimationEvent; buffer ampliado antes de atingir alvos |
| Navegação, ranged/elite e quatro salas | MvpSceneAcceptanceTests e FloorIntegrationTests; relatos normais Editor/Windows | NavMeshAgent, navegação/dano e projéteis reais; portas/última morte/reativação/vitória única testadas |
| Guardião e conclusão | BossTests, BossPlayModeTests, GeneratedBossPresentationTests; partidas normais confirmadas | Transição única a 50%, 3 s invulnerável, pulso/cor persistente e telegraph ≥1 s; vitória normal 5/5 confirmada pelo usuário |
| Progressão e loot | ProgressionTests, WeaponAssetTests, InventoryAssetIntegrationTests, PlayerInventoryPlayModeTests | Fórmula/XP/cura preservados; armas +6/+12; raridade sem multiplicador oculto; entradas anteriores mantidas |
| Inventário, armadura e poções | InventoryModelTests, InventoryPauseInputTests, InventorySceneUiTests, PlayerInventoryPlayModeTests | 16 slots/pilhas por potência, Q somente vivo/ferido, equipamento manual, pickup cheio preservado, descarte seguro e defesa aprovados |
| Especial, recurso e animação | PlayerSpecialModel/Asset/Input/CombatTests; SpecialPerf | Giro Generic in-place >350°, estamina/cooldown/dano radial; cancelamentos/pausa e consultas saturadas sem duplicação |
| Menus, pausa, reinício e opções | GameShellAssetTests, GameMenusInputTests, GameOptionsTests, GraphicsOptionsPlayModeTests; windows-menus-smoke.json | Foco/retorno/run nova/persistência, resolução/janela, presets por cópia URP e migração v1→v2 aprovados |
| Áudio funcional e reuso | GameAudioAssetTests e GameAudioPlayModeTests; RC2Perf/audio-pool.json; audio-mix-synthetic-measurements.json | 8 vozes reutilizadas; eventos/mute/lifecycle; 1.000 chamadas aquecidas sem GC.Alloc com controle positivo; mistura sintética sem clipping |
| Legibilidade e aceite do produto | windows-rc2-user-acceptance.json | Usuário confirmou esta RC2: derrota→reinício, vitória 5/5, E/I/Tab/Q, áudio/feedback e ajustes em zero. Prova manual, sem captura automatizada do desfecho |
| Desempenho antes/depois | WindowsPerf/release-comparison.json e development-comparison.json; PoolPerf, SpecialPerf, RC2Perf | Mesma carga/hardware/build por par; CPU/GPU/frame/memória e GC medidos. Limites e alocações não zero do player documentados; nenhuma alegação de ganho por velocidade de animação |
| Testes atuais | EditMode-final.xml: 93/93; PlayMode-final.xml: 69/69 | Suítes finais reexecutadas, zero falhas/skips. Runtime source preservado; a alteração de código final foi ReleaseVersion no builder |
| Build Windows final | windows-build.json, final-runtime-equivalence.json e windows-package-smoke.json | 1.0.0 x64 release, Succeeded, zero erros, 496 avisos de shaders conhecidos. Três assemblies mantiveram hash; Runtime.dll mudou após rebuild, sem alegação de identidade binária. Testes finais e smoke aprovados |
| Distribuição | Tools/Release/package_windows.py, README, manifesto SHA256, windows-package.json e windows-package-smoke.json | ZIP final com CRC e 208 hashes verificados, extração revalidada, menu→andar→especial observado e encerramento normal. Zero exceções/Missing Script no smoke |

## Limitações conhecidas

- Aceite de partida completa/teclas físicas/escuta e feedback da RC2 é confirmação explícita do usuário. A automação observou partes do jogo, incluindo 3/5 na partida normal; não se atribui a ela uma vitória que não capturou. O log concluído registra as cinco ondas e nenhum erro de runtime, sem provar sozinho a morte do Boss.
- Medições de frame são cargas sintéticas de 32 atores estáticos/64 colliders, sem IA/travessia. Uma amostra por condição em Ryzen 5 5500/RTX 3060/D3D12; não garantem FPS em outros computadores. Há timings GPU ausentes e GC bytes não disponível no release; Development separado tem overhead próprio.
- Mistura sintética medida a volumes padrão e partida normal capturada separadamente (78,912 s, pico −8,8 dBFS, −30,2 LUFS, zero amostras em clipping). A aprovação de escuta é humana. Não há medição exaustiva de todas as combinações de vozes em volume máximo. Capturas WAV ficam locais, fora do pacote/Git.
- Layout Windows observado em 1280×720 e 1920×1080; ultrawide físico não foi validado nesta máquina. Isso é limite de cobertura de hardware, sem declarar suporte visual testado nesses monitores.
- Avisos de shaders do pacote Unity de inferência e mensagem D3D12 info queue do driver são conhecidos. Nenhuma mudança de Packages foi usada para ocultá-los.
- Executável sem assinatura; distribuição local em ZIP, sem publicação em loja. Opções persistem por usuário; progresso/inventário reiniciam entre runs conforme o produto aprovado.
