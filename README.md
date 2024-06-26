# ffmpeg-dotnet

.net wrapper for some ffmpeg tasks

![image](https://github.com/alexeygrigorev/ffmpeg-dotnet/assets/875246/c39a2b10-18bb-4b32-bca8-0c27cf04a640)


```bash
wget https://github.com/alexeygrigorev/ffmpeg-dotnet/releases/download/init/ffmpeg.exe
```

```bash
dotnet build
cp ffmpeg.exe bin/Debug/net8.0-windows/

dotnet run

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

dotnet publish -c Release -r win-x64 --self-contained true

dotnet publish -c Release -r win-x64 --self-contained false
```
