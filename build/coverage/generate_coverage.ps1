dotnet test ../ATLAS.Kernel.JsonEngine.Tests `
    --settings coverage.runsettings `
    --collect:"XPlat Code Coverage"

dotnet test ../ATLAS.Kernel.JsonEngine.Integration.Tests `
    --settings coverage.runsettings `
    --collect:"XPlat Code Coverage"

reportgenerator `
    -reports:"../**/coverage.cobertura.xml" `
    -targetdir:"../coverage-report" `
    -reporttypes:Html
