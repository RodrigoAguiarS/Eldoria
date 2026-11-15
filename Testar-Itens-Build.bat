@echo off
REM Abre a ultima build direto no andar, ja com espadas, colete, pocoes e um pickup de cada tipo a frente.
cd /d "%~dp0Builds\WindowsMVP"
if not exist DungeonCrawler.exe (echo Build nao encontrada. No Unity use Tools ^> Dungeon Crawler ^> 3. Gerar build Windows e abrir & pause & exit /b 1)
start "" DungeonCrawler.exe -dcc-start-floor -dcc-items
