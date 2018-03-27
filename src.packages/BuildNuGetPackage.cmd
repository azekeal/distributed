@echo off
set NAME=%~nx0
for /d %%* in (*.*) do (
	@echo off 
	echo ===== Running %%~nx*\%NAME% =====
	pushd %%~nx*
	@echo on 
	%NAME%
	@echo off 
	@echo.
	popd
)