Write-Host 'Starting API...'
dotnet clean "C:\Users\lukas\Downloads\Proalpha-Projekt\Solution.sln"
dotnet build "C:\Users\lukas\Downloads\Proalpha-Projekt\Solution.sln"
Try {
    $api = Start-Process dotnet 'run --project API' -WindowStyle Hidden -PassThru -ErrorAction Stop
    Write-Host 'API started successfully.'
}
Catch {
    Write-Host 'Failed to start API.'
    exit 1
}
Write-Host 'Starting ClientApp...'
Try {
    Start-Process dotnet 'run --project ClientApp' -WindowStyle Hidden -Wait -ErrorAction Stop
    Write-Host 'ClientApp started successfully.'
}
Catch {
    Write-Host 'Failed to start ClientApp.'
    $api.Kill() 
    exit 1
}
$api.Kill()
