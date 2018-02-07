IF EXIST "Kerbalism-Continued.dll" (
move "Kerbalism-Continued.dll" "Debug\"
rename "Debug\Kerbalism-Continued.dll" "Kerbalism-Continued.dll.debug"
move "Debug\Kerbalism-Continued_Debug.dll.debug" ".\"
rename "Kerbalism-Continued_Debug.dll.debug" "Kerbalism-Continued_Debug.dll"
) ELSE (
move "Kerbalism-Continued_Debug.dll" "Debug\"
rename "Debug\Kerbalism-Continued_Debug.dll" "Kerbalism-Continued_Debug.dll.debug"
move "Debug\Kerbalism-Continued.dll.debug" ".\"
rename "Kerbalism-Continued.dll.debug" "Kerbalism-Continued.dll"
)