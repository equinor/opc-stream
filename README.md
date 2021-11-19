# opc-stream
A windows console application that streams data from a Comma-Separated-Variable(or "CSV") file into a DA OPC-server. 

CSV-files are "plain-text tables" and are almost the de facto standard format for exchanging time-series data between different programs,
as they are human-readable and creating drivers to read and write such files is usually quite easy to implement.

The intended use-case for this program is creating "test benches" for ["acceptance testing"](https://www.agilealliance.org/glossary/acceptance/) OPC-based applications like MPC. 

## Syntax

The program will get settings from the accompanying ``opc-stream.exe.config``, so the program can also be called as
```
opc-stream.exe
```
without any command-line argument as long as ``CsvFile`` is set.

The name of the csv-file can also be given as a command line option:
```
opc-stream.exe exampleCSV.csv
```
To set the server address, set ``DaOpcServerURI``  and to set the update frequency set ``SampleTime_ms`` in ``opc-stream.exe.config``.

### Custom start- and/or end-date

By using the command line argument ``-s "[datestring]"`` and ``-e "[datestring]"`` it possible to run customizable portions of 
the csv-file, for example

```
opc-stream.exe -s "2021-09-24 20:42:00" -e "2021-11-15 01:38:58"
```
will stream only between those two dates from a csv-file that span much longer.

### Mapping CSV-variable names to other names on the server

By using the command line argument ``-m "[mappingCSVFilename]"`` it is possible to define mappings that 
change the names of the tags on the OPC-server relative to what they are in the CSV-server.

The CSV-file of mappings should have just two columns and no header, the first column is the variable name in 
the CSV-file and the second column is the variable name to map to on the OPC-server, like so:
```
csvName1;opcName1
csvName2;opcName2
```

*Syntax example:*
```
opc-stream.exe -m "mappingFile.csv"
```
*If a mapping file is given, unmapped signals are not added to the tag server.*

### Editing opc-stream.exe.config

The entire ``opc-stream.exe.config`` will look something like this:
```
  <appSettings>
    <!-- the name of the csv-file can also be given as a command line input: -->
    <add key="CsvFile" value=""/>
    <add key="DaOpcServerURI" value="localhost/Statoil.OPC.Server"/>
    <add key="SampleTime_ms" value="50"/>
    <!-- see valid formats: https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings-->
    <add key="TimeStringFormat" value="yyyy-MM-dd HH:mm:ss"/>
    <!-- if "1" then one line is outputted on screen for each time step, to disable set "0" -->
    <add key="BeVerbose" value="1"/>
    <add key="CsvSeparator" value=";"/>
    <add key="TimeToSubtractFromEachWait_ms" value="0"/>
  </appSettings >
```

## Trying it out: Test dataset

The repo comes with a test dataset in the folder ``TestData`` called ``minSelect.csv`` that can be used to test-run the program. 
Note that you need to have an OPC-server with the tags of the test dataset registered in order to run the example. 

Peaking into the ``minSelect.csv`` to see how the ``opc-stream`` expects the file to be formatted:
- first row is the names of each variable, divided by separator ``CsvSeparator`` (in this case ``";"``)
- first column is timestamps in string according to the ``TimeStringFormat``(in this case ``"yyyy-MM-dd HH:mm:ss"``)
- each row below the first represents the values of each variable at that timestamp, separtor by the separator ``CsvSeparator``

``minSelect.csv``: 
```CSV
Time;PID1-Setpoint_Yset;PID2-Setpoint_Yset;Process-Disturbance_D;minSelect-SelectorOut;Process-Output_Y_sim;PID1-PID_U;PID2-PID_U
2021-01-01 00:00:00;50;70;0;50;50;50;51
2021-01-01 00:00:01;50;70;0.62668;50;50.628;50;51
2021-01-01 00:00:02;50;70;1.2434;49.685;51.216;49.685;51
2021-01-01 00:00:03;50;70;1.8406;49.39;51.759;49.39;50.685
(and so on)
```

## Write speeds

In tests with Statoil OPC Server, the program is able to stream 100 tags at 50 Hz  (``SampleTime_ms==20``).

## Timing 

By setting ``SampleTime_ms`` it is possible to control the frequency at which new values from the CSV are written to the OPC-server.

The program tries to time each write operation and "sleep" for a appropriate amount for each so that it is able to maintain this write frequency.

## Time-tags

The programs adds three timing-related tags to the OPC-server :

``_Time_SecondsInMinute`` is an integer goes from 0 to 59 indicating the seconds in the date.

``_Time_SecondsInHour`` is an integer that goes from 0 to 3599 indicating the seconds on the hour of the date.

``_Time_System`` shows the total number of seconds since January 1 1970.

If you use other programs such as MPC to read data from the OPC server, and wish to run through at dataset at faster-than-realtime speeds, 
read either of these timing tags to determine how much time has elapsed between samples and synchronize. 

## Reading different kinds of CSV-formats

The format of the time-string in the first column can be specified by the string ``TimeStringFormat``.
Default is ``"yyyy-MM-dd HH:mm:ss"``, for valid formatting see:
https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings

It is possible to change what the "separator" in the CSV-file is to be, the two most typical are ";" or ",". 

## Dependencies/interface with other programs

One frequently used program to observe tag values on OPC servers is "Matrikon OPC Explorer".

Note that when given a CSV-file, the program automatically spits out a ``opc-stream-taglist.txt`` which is suitable for 
registering tags in the ``Statoil OPC server``.

## Compatability

Note that the program *must* be complied as x86, otherwise it will give an error when connecting to OPC-servers, because .NET interop COM assemblies are only 32-bit. 

