@echo off
setlocal
set DOTNET=dotnet
if exist "%ProgramFiles%\dotnet\dotnet.exe" set DOTNET=%ProgramFiles%\dotnet\dotnet.exe

echo Publicando Pulso (nativo, self-contained win-x64)...
"%DOTNET%" publish "%~dp0src\Pulso\Pulso.csproj" -c Release -r win-x64 --self-contained true -o "%~dp0dist\Pulso" -p:PublishSingleFile=false
if errorlevel 1 exit /b 1

copy /Y "%~dp0src\publish\Abrir-como-administrador.bat" "%~dp0dist\Pulso\" >nul
copy /Y "%~dp0src\publish\LEIA-ME.txt" "%~dp0dist\Pulso\" >nul

echo.
echo Pronto: dist\Pulso\Pulso.exe
echo Zip a pasta inteira para mandar para alguem.
endlocal
