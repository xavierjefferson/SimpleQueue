mkdir .\packages
..\.nuget\nuget.exe pack ..\nuspecs\fissolue.simplequeue.nuspec -outputdirectory .\packages
..\.nuget\nuget.exe pack ..\nuspecs\fissolue.simplequeue.fluentnhibernate.nuspec -outputdirectory .\packages
pause