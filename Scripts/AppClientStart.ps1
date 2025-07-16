Write-Host 'Starting API...'
dotnet clean "C:\Users\lukas\Downloads\Proalpha-Projekt\Solution.sln"
dotnet build "C:\Users\lukas\Downloads\Proalpha-Projekt\Solution.sln"
$api = Start-Process dotnet 'run --project API'      -WindowStyle Hidden -PassThru
Start-Process dotnet 'run --project ClientApp' -WindowStyle Hidden -Wait
Write-Host 'Client closed - stopping API...'
$api.Kill() 