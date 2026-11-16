# Abysalto Cart API

A small shopping cart API built with ASP.NET Core, EF Core and SQL Server. 
Each cart belongs to the authenticated user. 
The API calculates totals from product prices in the database. `
ETag` and `If-Match` prevent stale writes to cart items.

## Run locally

Install the .NET 10 SDK, Docker with Compose and PowerShell. 
Start Docker and run the commands from the repository root.

### 1. Configure the database

Run this command to create a `.env` file with a generated password when starting from a fresh clone. 
If the file already exists, enter the password for the local SQL Server. 
The command stores the matching connection string in .NET User Secrets.

```powershell
$api = (Resolve-Path '.\Abysalto.Cart.Api\Abysalto.Cart.Api\Abysalto.Cart.Api.csproj' -ErrorAction Stop).Path
```

```powershell
if (Test-Path .env) { $sqlPassword = [System.Net.NetworkCredential]::new('', (Read-Host 'SQL Server password' -AsSecureString)).Password } else { $sqlPassword = 'Local!' + [guid]::NewGuid().ToString('N'); Set-Content -LiteralPath .env -Value "MSSQL_SA_PASSWORD='$sqlPassword'" -Encoding ascii }; $quotedPassword = $sqlPassword.Replace("'", "''"); dotnet user-secrets set 'ConnectionStrings:CartDb' "Server=127.0.0.1,14333;Database=AbysaltoCart;User Id=sa;Password='$quotedPassword';Encrypt=True;TrustServerCertificate=True" --project $api; Remove-Variable sqlPassword, quotedPassword
```

```powershell
docker compose up -d sqlserver
```

```powershell
docker compose logs -f --tail 30 sqlserver
```

Wait for `SQL Server is now ready for client connections`, 
then press Ctrl+C to stop following the logs. SQL Server keeps running. 
The local SQL Server port is `14333`.

### 2. Apply the migration

```powershell
dotnet tool restore
```

```powershell
dotnet ef database update --project $api -- --environment Development
```

The initial migration adds two sample products with IDs `1` and `2`.

### 3. Generate demo tokens and start the API

Generate the two tokens in the PowerShell session you will use to send requests. 
The signing key is stored in User Secrets. Keep `.env`, connection strings containing passwords and generated tokens out of Git.

```powershell
dotnet dev-certs https --trust
```

```powershell
$alice = dotnet user-jwts create --project $api --name alice --audience 'https://localhost:7008' --valid-for 1d --output token; $bob = dotnet user-jwts create --project $api --name bob --audience 'https://localhost:7008' --valid-for 1d --output token
```

In a second PowerShell session opened at the repository root, run the API using its HTTPS profile.

```powershell
dotnet run --project .\Abysalto.Cart.Api\Abysalto.Cart.Api\Abysalto.Cart.Api.csproj --launch-profile https
```

The API listens at `https://localhost:7008`. If you change the HTTPS port in the launch profile, update the token audience and the demo URL to match. The development OpenAPI document is available at `/openapi/v1.json`.

## Request examples

| Method and path | Behaviour |
| --- | --- |
| `POST /api/carts` | Creates an empty cart and returns `201 Created`, `Location` and `ETag`. |
| `GET /api/carts/{id}` | Returns the cart, its items, total in EUR and current `ETag`. |
| `PUT /api/carts/{id}/items/{productId}` | Adds the product or sets its quantity. Send `{"quantity":2}` and `If-Match`. Returns `204 No Content`. |
| `DELETE /api/carts/{id}/items/{productId}` | Removes the item. Send `If-Match`. Returns `204 No Content`. |

Send a bearer token with each request. A request without a token returns `401`. An authenticated user requesting another user's cart receives `404`. For writes, send the cart's current `ETag` exactly as returned, including its quotes. A missing `If-Match` returns `428` and an out-of-date `ETag` returns `412`. After a successful write or a stale response, fetch the cart again to obtain its current state and version.

Create a cart as Alice.

```powershell
$auth = @{ Authorization = "Bearer $alice" }; $created = Invoke-WebRequest -UseBasicParsing -Method Post -Uri 'https://localhost:7008/api/carts' -Headers $auth -ErrorAction Stop; $cartUrl = @($created.Headers['Location'])[0]; $created.StatusCode
```

Read the cart version and add two units of product `1`.

```powershell
$read = Invoke-WebRequest -UseBasicParsing -Uri $cartUrl -Headers $auth -ErrorAction Stop; $writeHeaders = @{ Authorization = "Bearer $alice"; 'If-Match' = @($read.Headers['ETag'])[0] }; $put = @{ UseBasicParsing = $true; Method = 'Put'; Uri = "$cartUrl/items/1"; Headers = $writeHeaders; ContentType = 'application/json'; ErrorAction = 'Stop' }; (Invoke-WebRequest @put -Body '{"quantity":2}').StatusCode
```

Reuse the old `ETag` to try setting the quantity to three. This request should receive `412`.

```powershell
try { Invoke-WebRequest @put -Body '{"quantity":3}' | Out-Null; throw 'Stale write unexpectedly succeeded.' } catch { if ([int]$_.Exception.Response.StatusCode -ne 412) { throw }; 412 }
```

Read the cart again. The quantity should still be `2` and the total `19.98`.

```powershell
Invoke-RestMethod -Uri $cartUrl -Headers $auth -ErrorAction Stop | ConvertTo-Json -Depth 4
```

Try reading Alice's cart as Bob. This request should receive `404`.

```powershell
try { Invoke-WebRequest -UseBasicParsing -Uri $cartUrl -Headers @{ Authorization = "Bearer $bob" } -ErrorAction Stop | Out-Null; throw 'Bob was able to read Alice''s cart.' } catch { if ([int]$_.Exception.Response.StatusCode -ne 404) { throw }; 404 }
```

## Unit tests

The tests cover totals with multiple items, an empty cart and quantity validation. Run them independently of SQL Server and the running API.

```powershell
dotnet test .\Abysalto.Cart.Tests\Abysalto.Cart.Tests.csproj
```

The HTTP examples above exercise authentication, ownership and concurrency against SQL Server.

## Demo assumptions

The catalogue uses seeded products with fixed EUR prices. The cart's SQL Server `rowversion` supplies its `ETag`. If a future catalogue changes prices independently, the validator also needs to account for those price changes. The local JWTs and `sa` connection are for development.
