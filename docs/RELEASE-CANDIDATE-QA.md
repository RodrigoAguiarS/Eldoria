# Roteiro de aceite do candidato Windows

Executável: `G:\DungeonCrawler\My project\Builds\WindowsMVP\DungeonCrawler.exe`. A versão exata da build fica em `Reports/Leadership/windows-build.json`. Este roteiro não declara o candidato aprovado.

Resultado da entrega em 14/11/2026: usuário aprovou explicitamente esta RC2, incluindo vitória 5/5, derrota/reinício, atalhos e áudio/feedback; relatório `Reports/Leadership/windows-rc2-user-acceptance.json`. A versão final 1.0.0 preserva o código runtime, passou novamente 93/69 testes e o smoke do pacote extraído. Este roteiro é mantido para regressões futuras; limites em `RELEASE-1.0-VALIDATION.md`.

1. No menu, abra Opções. Teste Leve e Padrão: o cenário deve continuar legível, sem materiais rosas. Música/efeitos em 0 devem silenciar suas categorias; flash/tremor em 0 devem remover esses efeitos. Volte às intensidades que preferir. Aplicar salva; Voltar descarta ajustes não aplicados.
2. Jogar deve iniciar uma run nova. WASD move, mouse mira, esquerdo executa combo (segurar repete), Espaço faz dash e E/direito usa giro. Verifique especialmente a tecla física E, cuja injeção sintética não foi conclusiva na automação Windows.
3. I/Tab abre a bolsa e pausa. Colete/equipe uma arma e, se cair, armadura. Poções ficam guardadas; Q/Usar consome quando ferido. Não deve haver dano inimigo enquanto a bolsa está aberta, perda de itens ao trocar nem ataque causado por clicar na UI.
4. Deixe um inimigo derrotar o jogador e use Reiniciar. Depois conclua as quatro salas e o Guardião, verificando a transição de fase, telegraph, vitória e 5/5. Mover/atacar/reiniciar deve continuar funcionando após a derrota.
5. Ouça ataques, impactos, dano, coleta, morte, rugido e música. Informe sons ausentes, volume desconfortável/distorção ou giro/impacto visual desalinhado. Ao terminar, volte ao menu e use Sair para encerrar a eventual captura de diagnóstico.

Aceite humano complementa testes de componentes, integração/input e medições. Registrar resultado, versão e limitações; não substituir partida normal por um preview ou pelo cenário sintético de desempenho.
