
@echo off
echo Building AutoFillApp...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
pause
