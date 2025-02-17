dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
dotnet "./reportgenerator/4.6.4/tools/netcoreapp2.1/ReportGenerator.dll" "-reports:./**/**/*.cobertura.xml" "-targetdir:./CoverageReports"
