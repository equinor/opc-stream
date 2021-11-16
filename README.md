# opc-stream
A windows console application that streams data from a Comma-Separated-Variable(CSV, a "plain-text table")-file into a DA OPC-server. 

The inteded use-case is testing OPC-based applications like MPC. 

## Compatability

- The program has been tested with the CSV-files which https://github.com/equinor/sigma_open instances create.
- The program can also stream simulations that have been stored to file using ``TimeSeriesDataSet.ToCSV()`` in
https://github.com/equinor/timeseriesanalysis

Note that the program *must* be complied as x86, otherwise it will give an error when connecting to OPC-servers, because .NET interop COM assemblies are only 32-bit. 

## Syntax

To call the program from the command line:
``opc-stream.exe exampleCSV.csv``

or 
```opc-stream.exe``` and specify the csv-file name in ``opc-stream.exe.config``.

``opc-stream.exe.config`` must be edited to change server or update frequency:
```
  <appSettings>
    <!-- the name of the csv-file can also be given as a command line input: -->
    <add key="CsvFile" value=""/>
    <add key="DaOpcServerURI" value="localhost/Statoil.OPC.Server"/>
    <add key="SampleTime_ms" value="50"/>
    <add key="CsvSeparator" value=";"/>
    <!-- see valid formats at: https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings-->
    <add key="TimeStringFormat" value="yyyy-MM-dd HH:mm:ss"/>
    <add key="CSVSeparator" value=";"/>
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

By setting ```SampleTime_ms`` it is possible to control the frequency at which new values from the CSV are written to the OPC-server.

The program tries to time each write operation and "sleep" for a appropriate amount for each so that it is able to maintain this write frequency.

Writing even just 6-7 tags will take a variable amount of time between 10-40 ms in tests, so running this program much quicker than 20 Hz or 
``SampleTime_ms=50`` may produce timing errors. The program will warn if any real-time deadlines are missing("timing errors"). Timing errors increase
the quicker sampling time is chosen above 1Hz. To try to alleviate this, it is possible to subtract a constant from the 
"wait time" by overriding ``TimeToSubtractFromEachWait_ms``.

It is possible to turn on verbose logging of each time-step by setting ``<add key="BeVerbose" value="0"/>`` but this does not
appear to affect timing or timing errors the slightest.

**It is common to see that this program takes 10% too long in total at high frequencies(20Hz), this timing error falls with lower sampling rates,
at 5Hz the error is only 1-2%.

## Time-tags

The programs adds to timing-related tags to the OPC-server ``_Time_Seconds`` and ``_Time_System``.

``_Time_Seconds`` is an integer goes from 0 to 59 indicating the seconds in the date.

``_Time_System`` shows the total number of seconds since January 1 1970.

If you use other programs to read data from the OPC, read these tags to determine how much time has elapsed between samples.

## Reading different kinds of CSV-formats

The format of the time-string in the first column can be specified by the string ``TimeStringFormat``.
Default is ``"yyyy-MM-dd HH:mm:ss"``, for valid formatting see:
https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings

It is possible to change what the "separator" in the CSV-file is to be, the two most typical are ";" or ",". 

## Dependencies/interface with other programs

You may for instance use "Matrikon OPC Explorer" to view the data on the OPC server.

Note that when given a CSV-file, the program automatically spits out a ``opc-stream-taglist.txt`` which is suitable for 
registering tags in the ``Statoil OPC server``.

The program re-uses CSV-functionality from the library "TimeSeriesAnalysis", which on nuget is only complied as 64-bit. To keep it 
simple, a x86 build of this library is checked in in the "assemblies" folder.