@echo off
set EXE=%~dp0dist\Pulso\Pulso.exe
if not exist "%EXE%" (
  echo Ainda nao publicou. Rode publicar.bat primeiro.
  pause
  exit /b 1
)
powershell -NoProfile -Command "Start-Process -FilePath '%EXE%' -Verb RunAs"
