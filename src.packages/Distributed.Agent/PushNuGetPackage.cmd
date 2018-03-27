@for %%* in (.) do (@set NAME=%%~nx*)
nuget push ..\..\bin\packages\%NAME%\*.nupkg -Source https://microsoft.pkgs.visualstudio.com/_packaging/Analog.HyperBuild/nuget/v3/index.json -ApiKey VSTS