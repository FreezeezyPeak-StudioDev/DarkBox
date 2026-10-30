# DarkBox

Gestor privado de contraseñas para Windows. Repositorio privado de respaldo y control personal.

> **Código propietario.** Este no es un proyecto de código abierto. Consulta [LICENSE](LICENSE).

## Estado

Aplicación de escritorio WPF en desarrollo activo. La bóveda se guarda localmente en SQLite y las credenciales se cifran antes de almacenarse.

## Funciones

- Crear y desbloquear una bóveda local con contraseña maestra.
- Añadir, editar, eliminar y consultar credenciales.
- Copiar usuario o contraseña al portapapeles con borrado automático a los 30 segundos.
- Mostrar u ocultar contraseñas.
- Exportar todas las credenciales, o una seleccionada, a PDF.
- Bloquear la bóveda y limpiar la sesión en memoria.
- Interfaz oscura en español.

## Seguridad

- Derivación de la contraseña maestra con **PBKDF2-SHA256** y 600 000 iteraciones por defecto.
- Cifrado autenticado **AES-256-GCM**.
- Clave de bóveda aleatoria, envuelta con la clave derivada de la contraseña maestra.
- Cada credencial se cifra por separado con datos asociados al usuario y al elemento.
- La contraseña maestra nunca se guarda en texto plano.

Limitaciones actuales:

- La base SQLite completa todavía no usa SQLCipher; los campos sensibles de las credenciales sí están cifrados, pero el archivo local no debe compartirse.
- No existe recuperación de contraseña maestra. Si se pierde, la bóveda no se puede descifrar.
- No hay sincronización en la nube ni llavero USB.

## Requisitos

- Windows 10 u 11 (x64).
- [.NET 8 SDK](https://dotnet.microsoft.com/download) para compilar.
- .NET 8 Desktop Runtime para ejecutar la versión publicada que no es autocontenida.

## Compilar y ejecutar

Compilación de desarrollo:

```powershell
dotnet build DarkBox.sln -c Debug
dotnet run --project src\DarkBox.App\DarkBox.App.csproj
```

Publicación del ejecutable:

```powershell
.\CompilarExe.bat
```

El resultado queda en `dist\DarkBox\DarkBox.exe`.

Para una versión portable que no necesite .NET instalado:

```powershell
$env:SELF_CONTAINED = "true"
.\CompilarExe.bat
```

## Pruebas

```powershell
dotnet test DarkBox.sln -c Debug
```

La suite cubre criptografía, repositorios y exportación a PDF.

## Estructura

```text
src/
  DarkBox.App/       Interfaz WPF y ViewModels
  DarkBox.Core/      Modelos, políticas y criptografía
  DarkBox.Data/      Base SQLite y repositorios
  DarkBox.Export/    Exportación a PDF
tests/
  DarkBox.Tests/     Pruebas xUnit
Resources/           Iconos e imágenes utilizadas por la app
dist/                Salida publicada (no se sube al repositorio)
```

## Datos locales

La aplicación guarda sus datos fuera del repositorio:

- Base de datos: `%LOCALAPPDATA%\DarkBox\darkbox.db`
- Registro de fallos: `%LOCALAPPDATA%\DarkBox\crash.log`

## Qué no subir a GitHub

Nunca subas al repositorio:

- `darkbox.db`, `*.db-wal` o `*.db-shm`.
- `crash.log` ni ningún `*.log`.
- PDFs exportados desde la bóveda.
- `bin/`, `obj/`, `.vs/`, `TestResults/` ni `dist/`.
- Certificados, claves privadas, archivos `.env` o secretos.
- Capturas o documentos con credenciales reales.

## Primera publicación en GitHub

1. Crea el repositorio como **Private**, sin README, licencia ni `.gitignore` iniciales.
2. Sube el contenido del proyecto conservando la estructura de carpetas.
3. Incluye `src`, `tests`, `Resources`, la solución, los archivos de configuración, `README.md`, `LICENSE` y `AGENTS.md`.
4. Excluye `dist`, `bin`, `obj`, `.vs`, bases de datos, logs y PDFs.
5. Pulsa **Commit changes** y comprueba que el repositorio siga marcado como privado.

Este repositorio es un respaldo personal. No se publica, no se distribuye y no se aceptan contribuciones externas.
