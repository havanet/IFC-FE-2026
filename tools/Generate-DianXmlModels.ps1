$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

Push-Location $root
try {
    dotnet run --project 'tools\IFC.Facturacion.XmlGenerator' --configuration Release -- $root
    if ($LASTEXITCODE -ne 0) {
        throw 'No fue posible generar las clases XML de factura DIAN.'
    }
}
finally {
    Pop-Location
}
