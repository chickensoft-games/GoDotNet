#!/bin/bash

dotnet build --no-restore

# This requires a GODOT4 environment variable.

dotnet ~/coverlet/src/coverlet.console/bin/Debug/net5.0/coverlet.console.dll \
  "./.godot/mono/temp/bin/Debug" --verbosity detailed \
  --target $GODOT4 \
  --targetargs "--run-tests --coverage --quit-on-finish" \
  --format "opencover" \
  --output "./coverage/coverage.xml" \
  --exclude-by-file "**/scenes/**/*.cs" \
  --exclude-by-file "**/test/**/*.cs" \
  --exclude-by-file "**/*Microsoft.NET.Test.Sdk.Program.cs" \
  --exclude-assemblies-without-sources "missingall"

reportgenerator \
  -reports:"./coverage/coverage.xml" \
  -targetdir:"./coverage/report" \
  -reporttypes:Html

reportgenerator \
  -reports:"./coverage/coverage.xml" \
  -targetdir:"./badges" \
  -reporttypes:Badges

mv ./badges/badge_branchcoverage.svg ./reports/branch_coverage.svg
mv ./badges/badge_linecoverage.svg ./reports/line_coverage.svg

rm -rf ./badges
