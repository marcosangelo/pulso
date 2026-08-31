@echo off
setlocal
set BIN=%~dp0src\Pulso\bin\Release\net8.0-windows\Pulso.exe
set DIST=%~dp0dist\Pulso\Pulso.exe
set DOTNET=dotnet
if exist "%ProgramFiles%\dotnet\dotnet.exe" set DOTNET=%ProgramFiles%\dotnet\dotnet.exe

rem Self-contained em dist\ e barrado pelo Smart App Control (0x800711C7) em PCs
rem com Controle de Aplicativos Inteligente ligado. O host do SDK nao e.
if exist "%BIN%" (
  start "" "%BIN%"
  exit /b 0
)

if exist "%DOTNET%" (
  echo Compilando e abrindo via .NET SDK...
  "%DOTNET%" run --project "%~dp0src\Pulso\Pulso.csproj" -c Release
  exit /b %ERRORLEVEL%
)

if exist "%DIST%" (
  echo Tentando dist\Pulso.exe — se fechar na hora, o Windows bloqueou o DLL.
  echo Windows Security → Controle de aplicativos e do navegador → Controle de aplicativos inteligente.
  start "" "%DIST%"
  exit /b 0
)

echo Compile primeiro:
echo   dotnet build "%~dp0src\Pulso\Pulso.csproj" -c Release
pause
endlocal
