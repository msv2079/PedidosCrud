$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$dockerCommand = Get-Command docker -ErrorAction SilentlyContinue
$dockerExecutable = if ($dockerCommand) { $dockerCommand.Source } else { Join-Path $env:LOCALAPPDATA 'Programs\DockerDesktop\resources\bin\docker.exe' }

if (!(Test-Path -LiteralPath $dockerExecutable)) {
    throw 'Docker nao encontrado. Instale o Docker Desktop e reabra o terminal.'
}

$environmentNames = @('Authentication__Username', 'Authentication__Password', 'Authentication__SigningKey', 'APP_PORT')
$previousValues = @{}

foreach ($name in $environmentNames) {
    $previousValues[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}

$randomGenerator = [Security.Cryptography.RandomNumberGenerator]::Create()
$randomBytes = New-Object byte[] 48
$randomGenerator.GetBytes($randomBytes)
$env:Authentication__SigningKey = [Convert]::ToBase64String($randomBytes)
$randomGenerator.GetBytes($randomBytes)
$env:Authentication__Password = [Convert]::ToBase64String($randomBytes)
$randomGenerator.Dispose()
$env:Authentication__Username = "docker-check-$([Guid]::NewGuid().ToString('N'))"
$portListener = New-Object Net.Sockets.TcpListener([Net.IPAddress]::Loopback, 0)
$portListener.Start()
$env:APP_PORT = [string]$portListener.LocalEndpoint.Port
$portListener.Stop()
$baseUrl = "http://localhost:$env:APP_PORT"
$composeProject = "pedidoscrud-check-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
$containerStarted = $false

function Send-ApiRequest {
    param([string]$Path, [string]$Method = 'GET', $Body = $null, [int]$ExpectedStatus = 200)

    $requestParameters = @{ Uri = "$baseUrl$Path"; Method = $Method; UseBasicParsing = $true; Headers = $apiHeaders }

    if ($null -ne $Body) {
        $requestParameters.Body = ConvertTo-Json $Body -Compress
        $requestParameters.ContentType = 'application/json'
    }

    try {
        $response = Invoke-WebRequest @requestParameters

        if ([int]$response.StatusCode -ne $ExpectedStatus) {
            throw "HTTP $($response.StatusCode), esperado $ExpectedStatus em $Method $Path."
        }

        if ($response.Content) {
            return ConvertFrom-Json $response.Content
        }
    } catch [Net.WebException] {
        if (!$_.Exception.Response -or [int]$_.Exception.Response.StatusCode -ne $ExpectedStatus) {
            throw
        }
    }
}

Push-Location $projectRoot

try {
    $engineReady = $false

    for ($attempt = 0; $attempt -lt 15; $attempt++) {
        & $dockerExecutable info --format '{{.ServerVersion}}' 2>$null

        if ($LASTEXITCODE -eq 0) {
            $engineReady = $true
            break
        }

        Start-Sleep -Seconds 2
    }

    if (!$engineReady) {
        throw 'O motor do Docker nao esta disponivel. Abra o Docker Desktop e aguarde a inicializacao.'
    }

    & $dockerExecutable compose --project-name $composeProject config --quiet

    if ($LASTEXITCODE -ne 0) {
        throw 'A configuracao do Compose e invalida.'
    }

    $containerStarted = $true
    & $dockerExecutable compose --project-name $composeProject up -d --build

    if ($LASTEXITCODE -ne 0) {
        throw 'Falha no build ou na inicializacao do container.'
    }

    $applicationReady = $false

    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        try {
            $health = Invoke-RestMethod -Uri "$baseUrl/health"
            $applicationReady = $health.status -eq 'healthy'
        } catch [Net.WebException] {
        }

        if ($applicationReady) {
            break
        }

        Start-Sleep -Seconds 1
    }

    if (!$applicationReady) {
        throw 'A aplicacao nao ficou disponivel no container.'
    }

    $apiHeaders = @{}
    $login = Send-ApiRequest '/api/auth/login' 'POST' @{ username = $env:Authentication__Username; password = $env:Authentication__Password }
    $apiHeaders.Authorization = "Bearer $($login.accessToken)"
    $initialOrders = Send-ApiRequest '/api/pedidos'

    if ($initialOrders.totalCount -ne 10000) {
        throw 'A carga inicial deve conter exatamente 10.000 pedidos.'
    }

    $orderInput = @{ customerName = 'Cliente de verificacao'; description = 'Teste isolado em Docker'; totalAmount = 1000000; status = 'Open' }
    $createdOrder = Send-ApiRequest '/api/pedidos' 'POST' $orderInput 201
    $orderInput.totalAmount = 1000000.01
    Send-ApiRequest '/api/pedidos' 'POST' $orderInput 400 | Out-Null
    $orderInput.totalAmount = 1000000
    $orderInput.status = 'Paid'
    $orderInput.version = $createdOrder.version
    $updatedOrder = Send-ApiRequest "/api/pedidos/$($createdOrder.id)" 'PUT' $orderInput
    Send-ApiRequest "/api/pedidos/$($createdOrder.id)" 'PUT' $orderInput 409 | Out-Null
    Send-ApiRequest "/api/pedidos/$($createdOrder.id)?version=$($updatedOrder.version)" 'DELETE' $null 204 | Out-Null
    Send-ApiRequest "/api/pedidos/$($createdOrder.id)" 'GET' $null 404 | Out-Null
    $finalOrders = Send-ApiRequest '/api/pedidos'

    if ($finalOrders.totalCount -ne 10000) {
        throw 'A contagem final deve voltar a 10.000 pedidos.'
    }

    $pageResponse = Invoke-WebRequest -Uri "$baseUrl/account/login" -UseBasicParsing
    $assetResponse = Invoke-WebRequest -Uri "$baseUrl/lib/bootstrap/css/bootstrap.min.css" -UseBasicParsing

    if ($pageResponse.StatusCode -ne 200 -or $assetResponse.StatusCode -ne 200) {
        throw 'A interface e os arquivos estaticos devem estar disponiveis.'
    }

    Write-Host 'Docker validado: build, startup, carga inicial, JWT, CRUD, limite monetario, conflitos, pagina e assets.'
} finally {
    if ($containerStarted) {
        & $dockerExecutable compose --project-name $composeProject down --remove-orphans
    }

    Pop-Location

    foreach ($name in $environmentNames) {
        [Environment]::SetEnvironmentVariable($name, $previousValues[$name], 'Process')
    }
}
