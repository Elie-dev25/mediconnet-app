cd d:\mediconnet_app

# Count Frontend Components
Write-Host "Counting Frontend Components..."
$components = (Get-ChildItem -Path Mediconnet-Frontend/src/app -Recurse -Filter "*.component.ts" -ErrorAction SilentlyContinue | Measure-Object).Count
Write-Host "Components: $components"

# Count Frontend Services
$services = (Get-ChildItem -Path Mediconnet-Frontend/src/app -Recurse -Filter "*.service.ts" -ErrorAction SilentlyContinue | Measure-Object).Count
Write-Host "Services: $services"

# Count Backend Controllers
$controllers = (Get-ChildItem -Path Mediconnet-Backend/Controllers -Filter "*Controller.cs" -ErrorAction SilentlyContinue | Measure-Object).Count
Write-Host "Controllers: $controllers"

# Count Backend Entities
$entities = (Get-ChildItem -Path Mediconnet-Backend/Data -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue | Where-Object { $_.Name -notlike "*Context*" -and $_.Name -notlike "*Configuration*" } | Measure-Object).Count
Write-Host "Entities: $entities"

# Count Migrations
$migrations = (Get-ChildItem -Path Mediconnet-Backend/Migrations -Filter "*.cs" -ErrorAction SilentlyContinue | Measure-Object).Count
Write-Host "Migrations: $migrations"

# Count total endpoints
Write-Host "Analyzing endpoints..."
$allControllers = Get-ChildItem -Path Mediconnet-Backend/Controllers -Filter "*Controller.cs" -ErrorAction SilentlyContinue
$totalEndpoints = 0
$getCount = 0
$postCount = 0
$putCount = 0
$deleteCount = 0

foreach ($file in $allControllers) {
    $content = Get-Content $file.FullName -Raw
    $getCount += ([regex]::Matches($content, "HttpGet")).Count
    $postCount += ([regex]::Matches($content, "HttpPost")).Count
    $putCount += ([regex]::Matches($content, "HttpPut")).Count
    $deleteCount += ([regex]::Matches($content, "HttpDelete")).Count
}
$totalEndpoints = $getCount + $postCount + $putCount + $deleteCount
Write-Host "Total Endpoints: $totalEndpoints (GET: $getCount, POST: $postCount, PUT: $putCount, DELETE: $deleteCount)"

# Modules
$modules = (Get-ChildItem -Path Mediconnet-Frontend/src/app -Recurse -Filter "*.module.ts" -ErrorAction SilentlyContinue | Measure-Object).Count
Write-Host "Modules: $modules"

# Pipes
$pipes = (Get-ChildItem -Path Mediconnet-Frontend/src/app -Recurse -Filter "*.pipe.ts" -ErrorAction SilentlyContinue | Measure-Object).Count
Write-Host "Pipes: $pipes"

# Create results JSON
$results = @{
    timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    frontend = @{
        components = $components
        services = $services
        modules = $modules
        pipes = $pipes
    }
    backend = @{
        controllers = $controllers
        endpoints = $totalEndpoints
        entities = $entities
        migrations = $migrations
        httpMethods = @{
            get = $getCount
            post = $postCount
            put = $putCount
            delete = $deleteCount
        }
    }
}

$filename = "performance-testing/results/metrics-$(Get-Date -Format 'yyyy-MM-dd_HHmmss').json"
$results | ConvertTo-Json | Out-File $filename

Write-Host "`nMetrics saved to: $filename"
Get-Content $filename
