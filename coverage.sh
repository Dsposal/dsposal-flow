#!/bin/bash

rm -rf "Dsposal.Flow.Tests/TestResults/"
rm -rf "coveragereport/"

dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"

reportgenerator -reports:"Dsposal.Flow.Tests/TestResults/*/coverage.cobertura.xml" \
  -targetdir:"coveragereport" -reporttypes:"Html;Cobertura"
