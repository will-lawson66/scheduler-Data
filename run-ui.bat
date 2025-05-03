@echo off
echo Starting Instrument Data UI Application...
cd "%~dp0"
dotnet run --project Instrument.Data.UI/Instrument.Data.UI.csproj
pause
