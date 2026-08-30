@echo off
powershell -NoProfile -Command "Start-Process -FilePath '%~dp0Pulso.exe' -Verb RunAs"
