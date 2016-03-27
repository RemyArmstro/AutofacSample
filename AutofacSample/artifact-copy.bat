echo off
set source=%1
set dest=%2

if not exist "%dest%" (
	echo "Creating directory: %dest%"
	mkdir "%dest%" 2>nul
)

echo "Copying: %source% to %dest%"
copy %source% %dest%