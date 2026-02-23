@echo off
REM ============================================
REM FreePOS Database Setup Script
REM ============================================
REM This script creates the FreePOS database and runs the schema.
REM It expects PostgreSQL bin directory in PATH or uses the installed location.

setlocal enabledelayedexpansion

echo.
echo ============================================
echo   FreePOS Database Setup
echo ============================================
echo.

REM Find PostgreSQL installation
set "PGBIN="
for /d %%D in ("C:\Program Files\PostgreSQL\*") do (
    if exist "%%D\bin\psql.exe" set "PGBIN=%%D\bin"
)

if "%PGBIN%"=="" (
    echo ERROR: PostgreSQL not found in C:\Program Files\PostgreSQL\
    echo Please install PostgreSQL first.
    pause
    exit /b 1
)

echo Found PostgreSQL at: %PGBIN%
echo.

REM Set password for non-interactive use
set "PGPASSWORD=postgres"

REM Check if database already exists
echo Checking if database exists...
"%PGBIN%\psql.exe" -U postgres -h localhost -p 5432 -tc "SELECT 1 FROM pg_database WHERE datname='mywinformsapp_db'" 2>nul | findstr /C:"1" >nul
if %ERRORLEVEL% equ 0 (
    echo Database 'mywinformsapp_db' already exists. Skipping creation.
    goto :run_schema
)

REM Create the database
echo Creating database 'mywinformsapp_db'...
"%PGBIN%\createdb.exe" -U postgres -h localhost -p 5432 mywinformsapp_db
if %ERRORLEVEL% neq 0 (
    echo ERROR: Failed to create database.
    echo Make sure PostgreSQL service is running and credentials are correct.
    pause
    exit /b 1
)
echo Database created successfully.

:run_schema
echo.
echo Running schema setup...
"%PGBIN%\psql.exe" -U postgres -h localhost -p 5432 -d mywinformsapp_db -f "%~dp0schema.sql" >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo WARNING: Some schema statements may have been skipped (tables may already exist).
    echo This is normal for re-installations.
) else (
    echo Schema applied successfully.
)

echo.
echo ============================================
echo   Database setup complete!
echo ============================================
echo.

endlocal
exit /b 0
