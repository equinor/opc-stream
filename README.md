# opc-stream
A windows console application that streams data from a Comma-Separated-Variable(or "CSV") file into a DA OPC-server. 

CSV-files are "plain-text tables" and are almost the de facto standard format for exchanging time-series data between different programs,
as they are human-readable and creating drivers to read and write such files is usually quite easy to implement.

The inteded use-case for this program is bench-testing OPC-based applications like MPC. 

## Compatability

- The program has been tested with the CSV-files which https://github.com/equinor/sigma_open instances create.
- The program can also stream simulations that have been stored to file using ``TimeSeriesDataSet.ToCSV()`` in
https://github.com/equinor/timeseriesanalysis (that is how the sample dataset was created.)

Note that the program *must* be complied as x86, otherwise it will give an error when connecting to OPC-servers, because .NET interop COM assemblies are only 32-bit. 

## Syntax

To call the program from the windows command line to run a file names exampelCSV.csv:
``"opc-stream.exe exampleCSV.csv"``

The program will get other settings from the accompanying ``opc-stream.exe.config``, so the progarm can also be called as
```"opc-stream.exe"``` without any command-line arguments, in which case the CSV-file name must be specified by the variable ``CsvFile`` inside the 
exe.config file. 

To set the server address, set(``DaOpcServerURI``)  and to set the update frequency set(``SampleTime_ms``).

The entire ``opc-stream.exe.config`` will look something like this:
```
  <appSettings>
    <!-- the name of the csv-file can also be given as a command line input: -->
    <add key="CsvFile" value=""/>
    <add key="DaOpcServerURI" value="localhost/Statoil.OPC.Server"/>
    <add key="SampleTime_ms" value="50"/>
    <add key="CsvSeparator" value=";"/>
    <!-- see valid formats at: https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings-->
    <add key="TimeStringFormat" value="yyyy-MM-dd HH:mm:ss"/>
    <!-- if "1" then one line is outputted on screen for each time step, to disable set "0" -->
    <add key="BeVerbose" value="1"/>
    <!-- stopwatch-based "sleep" between each write tends to make writing about 10% too slow if running faster than 1Hz, this factor can counteract this timing offset -->
    <add key="TimeToSubtractFromEachWait_ms" value="2"/>
  </appSettings >
```

## Trying it out: Test dataset

The repo comes with a test dataset in the folder ``TestData`` called ``minSelect.csv`` that can be used to test-run the program. 
Note that you need to have an OPC-server with the tags of the test dataset registered in order to run the example. 

## Write speeds

In tests with Statoil OPC Server, write speeds were about 10-40 ms with 7 tags and 1.800 seconds with 1000 tags.

## Timing 

By setting ``SampleTime_ms`` it is possible to control the frequency at which new values from the CSV are written to the OPC-server.

The program tries to time each write operation and "sleep" for a appropriate amount for each so that it is able to maintain this write frequency.

## Timing errors

A *"timing error"* is defined as when one full update of the tags on the opc-server takes longer than the  ``SampleTime_ms``. 
The program will keep track if this occurs and give a warning. 

How fast you can update the server without timing errors depens on the number of tags, but the test dataset with just 10 tags can run at least at 20 Hz, 
while tests with 1000 tags give timing errors if running faster than 0.5 Hz. 

## Timing offsets at high update frequencies


If running the program close to the limit where timing errors may occur, it has been observed that the time to stream a CSV-file 
to completion may be higher than expected, a *"timing offset"*. For instance the test dataset will take 10% too long to run if running at 20 Hz, but 
this error falls to just 1-2% at 5 Hz. 

This timing-offset happens even if ``opc-stream`` tries to time its execution time and calculate a "sleep time" for each cycle to try to maintain 
its desired update frequency. This is not a "timing error" as defined above. For some reason the program sleeps slightly too long 
at each update cycle at high update frequencies

To try to alleviate timing offset,  a constant is subtracted from the 
"sleep time", the amount to subtract in milliseconds is set by ``TimeToSubtractFromEachWait_ms``, the default value is ``2`` ms, but if you are
pushing the output hard you may need to tweak this setting. 

At the completion of every stream the program will output statistics about how long the program took compared to the expected time, this can be used
to fine-tune the timing if neccessarry.

*It is possible to turn on verbose logging of each time-step by setting ``<add key="BeVerbose" value="0"/>`` but this does not
appear to affect timing or timing errors the slightest.*

## Time-tags

The programs adds two timing-related tags to the OPC-server ``_Time_Seconds`` and ``_Time_System``.

``_Time_Seconds`` is an integer goes from 0 to 59 indicating the seconds in the date.

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

The program re-uses CSV-functionality from the library "TimeSeriesAnalysis", which on NuGet is only complied as 64-bit. To keep it 
simple, a x86 build of this library is checked in in the "assemblies" folder of the repository
