\
    @echo off
    setlocal
    echo Building single-file, self-contained for win-x64...
    dotnet publish -c Release -r win-x64 ^
      -p:PublishSingleFile=true ^
      -p:PublishTrimmed=true ^
      -p:IncludeNativeLibrariesForSelfExtract=true ^
      -p:SelfContained=true
    if %ERRORLEVEL% NEQ 0 (
        echo Publish failed.
        exit /b 1
    )
    echo Done. Find EXE at: bin\Release\net8.0-windows\win-x64\publish\AutoFillApp.exe
    endlocal
