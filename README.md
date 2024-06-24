# ffmpeg-dotnet
.net wrapper for some ffmpeg tasks

```
dotnet build
cp ffmpeg.exe bin/Debug/net8.0-windows/

dotnet run

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

dotnet publish -c Release -r win-x64 --self-contained true

dotnet publish -c Release -r win-x64 --self-contained false
```