dotnet restore BogChatDesktopClient.sln
dotnet build --no-restore BogChatDesktopClient.sln -p:Version=1.0.3
dotnet publish -r win-x64 -f net9.0-windows10.0.22621 --self-contained true -c Release -p:Version=1.0.3 -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true -o publish/win-x64 "BogChatDesktopClient/BogChatDesktopClient.csproj"
del ".\publish\win-x64\BogChat.exe"
ren ".\publish\win-x64\BogChatDesktopClient.exe" "BogChat.exe"
iscc /DMyAppVersion="1.0.3" ".\BogChatInstaller.iss"                                                      