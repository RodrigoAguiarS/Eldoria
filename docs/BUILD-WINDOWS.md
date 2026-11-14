# Build e reconstrução — Windows 1.0

Unity **6000.6.9f1**, URP 17.6.0 e pacotes fixados em `Packages/manifest.json`/`packages-lock.json`. Abra o projeto por essa versão; não atualize pacotes para reproduzir esta entrega. Feche PlayMode e qualquer outra instância do Editor antes de executar batchmode no mesmo projeto.

## Build

No PowerShell, a partir de `G:\DungeonCrawler\My project`:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'G:/DungeonCrawler/My project' -buildTarget win64 -executeMethod DungeonCrawler.Editor.MvpReleaseBuild.BuildWindows -quit -logFile 'G:/DungeonCrawler/My project/Reports/Leadership/Windows-rebuild.log'
```

A chamada pode retornar antes de o processo Unity terminar. Aguarde o processo e confira `Reports/Leadership/windows-build.json`: versão 1.0.0, `Succeeded`, zero erros e `development=false`. Saída: `Builds/WindowsMVP/DungeonCrawler.exe`, com MainMenu e Floor_01_GeneratedCharacters. O menu equivalente é **Tools / Dungeon Crawler / Build / Windows Release (Current MVP)**.

`BuildWindowsProfile` gera somente a build Development de diagnóstico em `Builds/WindowsProfile`; não a distribua. Flags, carga e limites das medições: `Tools/AudioGenerator/DIAGNOSTICS.md` e `Reports/Leadership/WindowsPerf/README.md`.

## Testes

Execute separadamente, aguardando uma suíte terminar antes de iniciar a outra. Não use `-quit` com `-runTests` nesta instalação.

```powershell
$env:DUNGEON_CRAWLER_TEST_REPORT_DIR='Reports/Leadership/FinalPerf'
& 'C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'G:/DungeonCrawler/My project' -runTests -testPlatform EditMode -testResults 'G:/DungeonCrawler/My project/Reports/Leadership/EditMode-final.xml' -logFile 'G:/DungeonCrawler/My project/Reports/Leadership/EditMode-final.log'
```

Repita com `-testPlatform PlayMode`, resultado `PlayMode-final.xml` e log `PlayMode-final.log`. A variável separa os relatórios novos dos históricos da Task 02. Os resultados da entrega ficam em `Reports/Leadership`; a matriz de cobertura e aceite manual está em `RELEASE-1.0-VALIDATION.md`.

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

Gera `Builds/Release/DungeonCrawler-1.0.0-windows-x64.zip`, README, BUILD-INFO e manifesto SHA256. Verifica PE x64, CRC e hashes; exclui pastas de backup/debug e recusa sobrescrever um pacote existente. Não publica nem envia arquivos. Para outra revisão, preserve o pacote anterior e versione explicitamente o novo artefato.
