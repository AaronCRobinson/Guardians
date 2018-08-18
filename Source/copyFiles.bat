@echo off
SET "ProjectName=Guardians"
SET "SolutionDir=C:\Users\robin\Desktop\Games\RimWorld Modding\Source\Guardians\Source"
@echo on

del /S /Q "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\%ProjectName%\Defs\*"

xcopy /S /Y "%SolutionDir%\..\About\*" "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\%ProjectName%\About\"
xcopy /S /Y "%SolutionDir%\..\Defs\*" "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\%ProjectName%\Defs\"
xcopy /S /Y "%SolutionDir%\..\Textures\*" "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\%ProjectName%\Textures\"