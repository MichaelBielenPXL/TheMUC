@echo off
rem Sends the last commit to the server and (re)starts the compose stack there.
rem Usage: deploy.cmd user@host /remote/folder [ssh-key-file]
rem Started from the Aspire dashboard ("deploy" resource); can also be run by hand.
rem Needs SSH key login for that user. The .env in the remote folder is left alone.
setlocal
if "%~2"=="" (
  echo Usage: deploy.cmd user@host /remote/folder [ssh-key-file]
  exit /b 2
)
set KEY=
if not "%~3"=="" set KEY=-i "%~3" -o IdentitiesOnly=yes
git archive HEAD | ssh %KEY% -o BatchMode=yes %1 "mkdir -p %2 && tar -x -C %2 && cd %2 && docker compose --profile tunnel up --detach --build --remove-orphans"
