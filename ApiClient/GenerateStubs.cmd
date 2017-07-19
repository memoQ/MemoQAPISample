REM svcutil.exe may be missing from your path
REM Its likely location is: C:\Program Files (x86)\Microsoft SDKs\Windows\V10.0A\bin\NETFX 4.6.1 Tools
REM Or a similarly versioned folder. It is part of Microsoft Windows SDK.

REM Your memoQ server's URL, ending in "/memoqservices", without a traling slash
SET URL=https://my-memoq-server.com/memoqservices

svcutil.exe /o:Interfaces\ITMService.cs /n:*,MQS.TM %URL%/tm
svcutil.exe /o:Interfaces\ITBService.cs /n:*,MQS.TB %URL%/tb
svcutil.exe /o:Interfaces\IResourceService.cs /n:*,MQS.Resource %URL%/resource
svcutil.exe /o:Interfaces\ISecurityService.cs /n:*,MQS.Security %URL%/security
svcutil.exe /o:Interfaces\IFileManagerService.cs /n:*,MQS.FileManager %URL%/filemanager
svcutil.exe /o:Interfaces\IServerProjectService.cs /n:*,MQS.ServerProject %URL%/serverproject

DEL output.config
