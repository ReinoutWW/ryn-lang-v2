@echo off
REM GP-λ compiler and runtime wrapper for Windows

REM Get the directory where this script is located
set SCRIPT_DIR=%~dp0

REM Run the CLI tool
dotnet "%SCRIPT_DIR%src\GpLambda.CLI\bin\Debug\net8.0\gpl.dll" %*