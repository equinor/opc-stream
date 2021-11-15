# opc-stream
Streams data from a csv-file into a DA OPC-server. 

This is intended for testing purposes.

Note that the program *must* be complied as x86, otherwise it will give an error when connecting to OPC-servers, because .NET interop COM assemblies are only 32-bit. 

Syntax:
``opc-stream.exe exampleCSV.csv``

``opc-stream.exe.config`` must be edited to change server or update frequency:
```
  <appSettings>
    <add key="CsvFile" value=""/>
    <add key="DaOpcServerURI" value="localhost/Statoil.OPC.Server"/>
    <add key="SampleTime_ms" value="1000"/>
    <add key="CsvSeparator" value=";"/>
  </appSettings >
```

Note that when given a CSV-file, the program automatically spits out a ``opc-stream-taglist.txt`` which is suitable for 
registering tags in the ``Statoil OPC server``.

The program re-uses CSV-functionality from the library "TimeSeriesAnalysis", which on nuget is only complied as 64-bit. To keep it 
simple, a x86 build of this library is checked in in the "assemblies" folder.
