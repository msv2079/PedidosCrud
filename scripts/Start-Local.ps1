param([switch]$Docker)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$environmentNames = @('Authentication__Username', 'Authentication__Password', 'Authentication__SigningKey')
$previousValues = @{}

foreach ($name in $environmentNames) {
    $previousValues[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}

try {
    if ([string]::IsNullOrWhiteSpace($env:Authentication__Username)) {
        $env:Authentication__Username = Read-Host 'Escolha o usuario de acesso'
    }

    if ([string]::IsNullOrWhiteSpace($env:Authentication__Username) -or $env:Authentication__Username.Length -gt 100) {
        throw 'Informe um usuario de 1 a 100 caracteres.'
    }

    if ([string]::IsNullOrEmpty($env:Authentication__Password)) {
        $securePassword = Read-Host 'Escolha a senha (12 a 256 caracteres)' -AsSecureString
        $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)

        try {
            $env:Authentication__Password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
        } finally {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
        }
    }

    if ($env:Authentication__Password.Length -lt 12 -or $env:Authentication__Password.Length -gt 256) {
        throw 'A senha deve ter de 12 a 256 caracteres.'
    }

    if ([string]::IsNullOrEmpty($env:Authentication__SigningKey)) {
        $keyBytes = New-Object byte[] 48
        $randomGenerator = [Security.Cryptography.RandomNumberGenerator]::Create()
        $randomGenerator.GetBytes($keyBytes)
        $randomGenerator.Dispose()
        $env:Authentication__SigningKey = [Convert]::ToBase64String($keyBytes)
    }

    Push-Location $projectRoot

    try {
        if ($Docker) {
            $dockerCommand = Get-Command docker -ErrorAction SilentlyContinue
            $dockerLocations = @((Join-Path $env:LOCALAPPDATA 'Programs\DockerDesktop\resources\bin\docker.exe'), (Join-Path $env:ProgramFiles 'Docker\Docker\resources\bin\docker.exe'))
            $dockerExecutable = if ($dockerCommand) { $dockerCommand.Source } else { $dockerLocations | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1 }

            if (!$dockerExecutable) {
                throw 'Docker nao encontrado. Instale o Docker Desktop e reabra o terminal.'
            }

            Write-Host 'Aplicacao em http://localhost:8080. Use Ctrl+C para encerrar.'
            & $dockerExecutable compose up --build
        } else {
            Write-Host 'Aplicacao em http://localhost:5021. Use Ctrl+C para encerrar.'
            dotnet run --project src/PedidosCrud.Web --launch-profile http
        }

        if ($LASTEXITCODE -ne 0) {
            throw "A execucao terminou com codigo $LASTEXITCODE."
        }
    } finally {
        Pop-Location
    }
} finally {
    foreach ($name in $environmentNames) {
        [Environment]::SetEnvironmentVariable($name, $previousValues[$name], 'Process')
    }
}
