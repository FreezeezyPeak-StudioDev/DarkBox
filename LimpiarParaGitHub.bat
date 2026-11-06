@echo off
setlocal enabledelayedexpansion

set "PROJECT_ROOT=%~dp0"
set "FOUND=0"

echo ===================================================
echo LIMPIEZA DE ARCHIVOS DE COMPILACION - DARKBOX
echo ===================================================
echo Esto elimina dist, bin, obj, TestResults y .vs.
echo Solo borra carpetas generadas; no toca el codigo.
echo.

rem --- dist (ejecutable publicado) ---
if exist "%PROJECT_ROOT%dist" (
    echo Eliminando dist\...
    rmdir /s /q "%PROJECT_ROOT%dist"
    set "FOUND=1"
)

rem --- bin y obj dentro de src\* y tests\* ---
for /d %%d in ("%PROJECT_ROOT%src\*") do (
    if exist "%%d\bin" (
        echo Eliminando %%~nxd\bin ...
        rmdir /s /q "%%d\bin"
        set "FOUND=1"
    )
    if exist "%%d\obj" (
        echo Eliminando %%~nxd\obj ...
        rmdir /s /q "%%d\obj"
        set "FOUND=1"
    )
)

for /d %%d in ("%PROJECT_ROOT%tests\*") do (
    if exist "%%d\bin" (
        echo Eliminando %%~nxd\bin ...
        rmdir /s /q "%%d\bin"
        set "FOUND=1"
    )
    if exist "%%d\obj" (
        echo Eliminando %%~nxd\obj ...
        rmdir /s /q "%%d\obj"
        set "FOUND=1"
    )
)

rem --- resultados de pruebas y cache de Visual Studio ---
if exist "%PROJECT_ROOT%TestResults" (
    echo Eliminando TestResults\...
    rmdir /s /q "%PROJECT_ROOT%TestResults"
    set "FOUND=1"
)

if exist "%PROJECT_ROOT%.vs" (
    echo Eliminando .vs\...
    rmdir /s /q "%PROJECT_ROOT%.vs"
    set "FOUND=1"
)

if "!FOUND!"=="0" (
    echo No habia archivos de compilacion que eliminar.
) else (
    echo.
    echo Limpieza completada. Solo queda codigo fuente y recursos.
)

echo.
pause
