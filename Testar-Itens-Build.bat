@echo off
REM Abre a ultima build direto no andar, ja com espadas, colete, pocoes e um pickup de cada tipo a frente.
cd /d "%~dp0Builds\WindowsMVP"
if exist Eldoria.exe (start "" Eldoria.exe -dcc-start-floor -dcc-items & exit /b 0)
REM Build anterior ao nome Eldoria (D-003): ainda abre, mas gere uma build nova para ver o nome atual.
if exist DungeonCrawler.exe (echo Build antiga com o nome anterior. Gere uma build nova para ver Eldoria. & start "" DungeonCrawler.exe -dcc-start-floor -dcc-items & exit /b 0)
echo Build nao encontrada. No Unity use Tools ^> Dungeon Crawler ^> 3. Gerar build Windows e abrir & pause & exit /b 1
