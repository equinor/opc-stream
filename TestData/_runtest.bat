REM just streaming the entire dataset
opc-stream.exe TestData\minselect.csv

REM mapped tags and specified start and end dates
REM opc-stream.exe TestData\minselect.csv -m TestData\minSelectMapping.csv -s "2021-01-01 00:03:10" -e "2021-01-01 00:09:10"