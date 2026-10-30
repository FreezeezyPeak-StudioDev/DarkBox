@echo off
setlocal

set "CONFIGURATION=Release"
set "FRAMEWORK=net8.0-windows"
set "RUNTIME=win-x64"
set "PROJECT_ROOT=%~dp0"
set "SOLUTION_FILE=%PROJECT_ROOT%DarkBox.sln"
set "PROJECT_FILE=%PROJECT_ROOT%src\DarkBox.App\DarkBox.App.csproj"
set "APP_NAME=DarkBox"
set "DIST_ROOT=%PROJECT_ROOT%dist"
set "APP_FOLDER=%DIST_ROOT%\DarkBox"

rem false = necesita .NET 8 Desktop Runtime instalado.
rem true  = carpeta totalmente portable (mas pesada, no necesita .NET instalado).
if not defined SELF_CONTAINED set "SELF_CONTAINED=false"
if not defined START_APP set "START_APP=1"

echo ===================================================
echo COMPILADOR DE DARKBOX
echo ===================================================

where dotnet >nul 2>nul
if errorlevel 1 (
    echo ERROR: No se encontro el comando 'dotnet'.
    echo Instala el SDK de .NET 8 desde: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

set "DOTNET_MAJOR=0"
for /f "tokens=1 delims=." %%v in ('dotnet --version') do set "DOTNET_MAJOR=%%v"
if %DOTNET_MAJOR% LSS 8 (
    echo ERROR: Se necesita el SDK de .NET 8 o superior.
    echo Version detectada:
    dotnet --version
    pause
    exit /b 1
)

echo Limpiando archivos temporales...
taskkill /f /im "%APP_NAME%.exe" /t >nul 2>nul
ping 127.0.0.1 -n 2 >nul

if exist "%DIST_ROOT%" rmdir /s /q "%DIST_ROOT%"
dotnet clean "%SOLUTION_FILE%" -c %CONFIGURATION% --nologo >nul 2>nul
mkdir "%APP_FOLDER%"

echo Restaurando proyecto...
dotnet restore "%PROJECT_FILE%" --runtime %RUNTIME% --nologo
if errorlevel 1 goto error

echo Compilando version %CONFIGURATION% (%FRAMEWORK%)...
dotnet publish "%PROJECT_FILE%" -c %CONFIGURATION% -f %FRAMEWORK% -r %RUNTIME% --self-contained %SELF_CONTAINED% -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false -o "%APP_FOLDER%" --no-restore --nologo
if errorlevel 1 goto error

echo.
echo Compilacion lista:
echo "%APP_FOLDER%"
echo.
echo Ejecutable:
echo "%APP_FOLDER%\%APP_NAME%.exe"

if "%SELF_CONTAINED%"=="false" (
    echo.
    echo Nota: esta version necesita .NET 8 Desktop Runtime en el equipo.
    echo Para una version portable, ejecuta este script con SELF_CONTAINED=true.
)

echo.
if "%START_APP%"=="1" (
    start "" "%APP_FOLDER%\%APP_NAME%.exe"
)

exit /b 0


:error
echo.
echo Error al compilar DarkBox.
pause
exit /b 1
