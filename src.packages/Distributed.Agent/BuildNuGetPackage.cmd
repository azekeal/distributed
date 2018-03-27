@for %%* in (.) do @set NAME=%%~nx*
@set DIR=%CD%
@mkdir ..\..\bin\packages\%NAME%
@pushd ..\..\bin\packages\%NAME%
@del *.nupkg
nuget pack %DIR%\%NAME%.nuspec -OutputDirectory %CD%
@popd