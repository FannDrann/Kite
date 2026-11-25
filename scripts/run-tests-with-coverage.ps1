$ErrorActionPreference = 'Stop'

function Invoke-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet завершился с кодом $LASTEXITCODE."
    }
}

$projectDirectory = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $projectDirectory
try {
    $coverageDirectory = Join-Path 'tests/coverage' ([Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $coverageDirectory -Force | Out-Null

    Invoke-Dotnet test Kite.slnx --settings tests/tests.runsettings --collect 'XPlat Code Coverage' --results-directory $coverageDirectory
    $reports = @(Get-ChildItem -LiteralPath $coverageDirectory -Recurse -Filter 'coverage.cobertura.xml' -File |
        ForEach-Object { Resolve-Path -LiteralPath $_.FullName -Relative })
    if ($reports.Count -eq 0) {
        throw 'Отчёт о покрытии не найден.'
    }

    $merged = Join-Path $coverageDirectory 'merged.cobertura.xml'
    Invoke-Dotnet tool run dotnet-coverage merge @reports --output $merged --output-format cobertura
    Invoke-Dotnet tool run reportgenerator "-reports:$merged" '-targetdir:tests/coverage-report' '-reporttypes:Html'
}
finally {
    Pop-Location
}
