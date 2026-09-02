@echo off
cd /d "%~dp0"
if exist "RJP.Signer.Bridge.exe" (
  start "RJP Signer Bridge" "RJP.Signer.Bridge.exe"
) else (
  echo RJP.Signer.Bridge.exe nao encontrado nesta pasta.
  echo Faz primeiro o workflow Build Windows Bridge no GitHub Actions.
  pause
)
