SET output=MPLite-Release
SET source=MPLite\bin\Release
del "%output%"
mkdir %output%
copy "%source%\*.dll" "%output%\*.dll"
copy "%source%\MPLite.exe" "%output%\MPLite.exe"
PAUSE