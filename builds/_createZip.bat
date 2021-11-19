echo create a zipped numbered release of the the complete library including dependencies

set buildnr=%1

set name=opc-stream.%buildnr%.zip

del %name%

xcopy ..\TestData\*.* TestData /I

zip -j %name% ..\readme.md 
zip -j %name% ..\bin\debug\*.dll
zip -j %name% ..\bin\debug\*.pdb
zip -j %name% ..\bin\debug\*.dll.config

zip -r %name% TestData\*.* 

zip -T %name% 

unzip -l %name%

del /Q TestData\*.*
rmdir TestData 