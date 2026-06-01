$ErrorActionPreference = "Stop"

$envPath = ".\.env"
$rawOutputPath = ".\seed\raw-spoonacular.json"

$apiKeyLine = Get-Content $envPath | Where-Object { $_ -match "^SPOONACULAR_API_KEY=" } | Select-Object -First 1

if (-not $apiKeyLine) {
    throw "No encontré SPOONACULAR_API_KEY en .env"
}

$apiKey = $apiKeyLine.Split("=", 2)[1].Trim()

$query = "meat"
$number = 2

$searchUrl = "https://api.spoonacular.com/recipes/complexSearch?query=$query&number=$number&apiKey=$apiKey"
Write-Host "Buscando recetas..."
$search = Invoke-RestMethod $searchUrl

$ids = ($search.results | ForEach-Object { $_.id }) -join ","

if (-not $ids) {
    throw "No se encontraron recetas para query=$query"
}

Write-Host "IDs encontrados: $ids"

$detailsUrl = "https://api.spoonacular.com/recipes/informationBulk?ids=$ids&includeNutrition=true&apiKey=$apiKey"
Write-Host "Descargando detalle..."
$details = Invoke-RestMethod $detailsUrl

$details | ConvertTo-Json -Depth 40 | Out-File $rawOutputPath -Encoding utf8

Write-Host "Listo. Guardado en $rawOutputPath"