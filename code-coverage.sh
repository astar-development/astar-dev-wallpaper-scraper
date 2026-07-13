dotnet test \
  --coverage \
  --coverage-output-format cobertura \
  --results-directory ./TestResults

dotnet tool restore
dotnet reportgenerator -reports:TestResults/*.cobertura.xml -targetdir:CoverageReport
