param(
    [string]$FromDate = "2025-12-31"
)

# Generates treasury-currency-map.json from Treasury API descriptors on/after FromDate.
# Uses the full historical map (if present) as the ISO lookup source.

$existingPath = Join-Path $PSScriptRoot "..\src\CardLedger.Infrastructure\Treasury\Data\treasury-currency-map-full.json"
$outputPath = Join-Path $PSScriptRoot "..\src\CardLedger.Infrastructure\Treasury\Data\treasury-currency-map.json"

$lookup = @{}
if (Test-Path $existingPath) {
    $existing = Get-Content $existingPath -Raw | ConvertFrom-Json
    foreach ($entry in $existing) {
        $lookup[$entry.countryCurrencyDesc] = $entry.iso4217
    }
} elseif (Test-Path $outputPath) {
    $existing = Get-Content $outputPath -Raw | ConvertFrom-Json
    foreach ($entry in $existing) {
        $lookup[$entry.countryCurrencyDesc] = $entry.iso4217
    }
}

$exact = @{
    "United States-Dollar" = "USD"
    "Australia-Dollar" = "AUD"
    "Canada-Dollar" = "CAD"
    "Japan-Yen" = "JPY"
    "United Kingdom-Pound" = "GBP"
    "United Kingdom-Pound Sterling" = "GBP"
    "Euro Zone-Euro" = "EUR"
    "Cross Border-Euro" = "EUR"
    "China-Renminbi" = "CNY"
    "Hong Kong-Dollar" = "HKD"
    "New Zealand-Dollar" = "NZD"
    "Singapore-Dollar" = "SGD"
    "Switzerland-Franc" = "CHF"
    "India-Rupee" = "INR"
    "Mexico-Peso" = "MXN"
    "Mexico-New Peso" = "MXN"
    "Brazil-Real" = "BRL"
    "South Africa-Rand" = "ZAR"
    "Korea-Won" = "KRW"
    "Turkey-Lira" = "TRY"
    "Turkey-New Lira" = "TRY"
    "Russia-Ruble" = "RUB"
    "Poland-Zloty" = "PLN"
    "Sweden-Krona" = "SEK"
    "Norway-Krone" = "NOK"
    "Denmark-Krone" = "DKK"
    "Israel-Shekel" = "ILS"
    "Jerusalem-Shekel" = "ILS"
    "Saudi Arabia-Riyal" = "SAR"
    "United Arab Emirates-Dirham" = "AED"
    "Thailand-Baht" = "THB"
    "Malaysia-Ringgit" = "MYR"
    "Indonesia-Rupiah" = "IDR"
    "Philippines-Peso" = "PHP"
    "Czech Republic-Koruna" = "CZK"
    "Hungary-Forint" = "HUF"
    "Romania-Leu" = "RON"
    "Romania-New Leu" = "RON"
    "Romania-Third Leu" = "RON"
    "Ukraine-Hryvnia" = "UAH"
    "Egypt-Pound" = "EGP"
    "Nigeria-Naira" = "NGN"
    "Kenya-Shilling" = "KES"
    "Pakistan-Rupee" = "PKR"
    "Vietnam-Dong" = "VND"
    "Colombia-Peso" = "COP"
    "Chile-Peso" = "CLP"
    "Peru-Sol" = "PEN"
    "Peru-Nuevo Sol" = "PEN"
    "Argentina-Peso" = "ARS"
    "Iceland-Krona" = "ISK"
    "Morocco-Dirham" = "MAD"
    "Qatar-Riyal" = "QAR"
    "Kuwait-Dinar" = "KWD"
    "Bahrain-Dinar" = "BHD"
    "Oman-Rial" = "OMR"
    "Jordan-Dinar" = "JOD"
    "Iraq-Dinar" = "IQD"
    "Lebanon-Pound" = "LBP"
    "Taiwan-Dollar" = "TWD"
}

# Longest suffix first to avoid ambiguous matches
$suffixRules = @(
    @{ Suffix = "-East Caribbean Dollar"; Iso = "XCD" },
    @{ Suffix = "-Bolivar Soberano"; Iso = "VES" },
    @{ Suffix = "-Bolivar Fuerte"; Iso = "VES" },
    @{ Suffix = "-Caribbean Guilder"; Iso = "XCG" },
    @{ Suffix = "-Nuevo Sol"; Iso = "PEN" },
    @{ Suffix = "-New Manat"; Iso = "AZN" },
    @{ Suffix = "-Second Manat"; Iso = "AZN" },
    @{ Suffix = "-New Kwacha"; Iso = "ZMW" },
    @{ Suffix = "-New Ruble"; Iso = "BYN" },
    @{ Suffix = "-New Peso"; Iso = "MXN" },
    @{ Suffix = "-New Lira"; Iso = "TRY" },
    @{ Suffix = "-New Leu"; Iso = "RON" },
    @{ Suffix = "-Third Leu"; Iso = "RON" },
    @{ Suffix = "-Pound Sterling"; Iso = "GBP" },
    @{ Suffix = "-Cfa Franc"; Iso = "XOF" },
    @{ Suffix = "-Sudanese Pound"; Iso = "SDG" },
    @{ Suffix = "-Old Leone"; Iso = "SLE" },
    @{ Suffix = "-Euro"; Iso = "EUR" },
    @{ Suffix = "-Dollar"; Iso = "USD" },
    @{ Suffix = "-Pound"; Iso = "GBP" },
    @{ Suffix = "-Yen"; Iso = "JPY" },
    @{ Suffix = "-Franc"; Iso = "CHF" },
    @{ Suffix = "-Real"; Iso = "BRL" },
    @{ Suffix = "-Peso"; Iso = "MXN" },
    @{ Suffix = "-Rand"; Iso = "ZAR" },
    @{ Suffix = "-Won"; Iso = "KRW" },
    @{ Suffix = "-Lira"; Iso = "TRY" },
    @{ Suffix = "-Ruble"; Iso = "RUB" },
    @{ Suffix = "-Zloty"; Iso = "PLN" },
    @{ Suffix = "-Krona"; Iso = "SEK" },
    @{ Suffix = "-Krone"; Iso = "NOK" },
    @{ Suffix = "-Shekel"; Iso = "ILS" },
    @{ Suffix = "-Riyal"; Iso = "SAR" },
    @{ Suffix = "-Dirham"; Iso = "AED" },
    @{ Suffix = "-Baht"; Iso = "THB" },
    @{ Suffix = "-Ringgit"; Iso = "MYR" },
    @{ Suffix = "-Rupiah"; Iso = "IDR" },
    @{ Suffix = "-Rupee"; Iso = "INR" },
    @{ Suffix = "-Dong"; Iso = "VND" },
    @{ Suffix = "-Dinar"; Iso = "JOD" },
    @{ Suffix = "-Koruna"; Iso = "CZK" },
    @{ Suffix = "-Forint"; Iso = "HUF" },
    @{ Suffix = "-Leu"; Iso = "RON" },
    @{ Suffix = "-Hryvnia"; Iso = "UAH" },
    @{ Suffix = "-Naira"; Iso = "NGN" },
    @{ Suffix = "-Shilling"; Iso = "KES" },
    @{ Suffix = "-Renminbi"; Iso = "CNY" },
    @{ Suffix = "-Bolivar"; Iso = "VES" },
    @{ Suffix = "-Boliviano"; Iso = "BOB" },
    @{ Suffix = "-Sol"; Iso = "PEN" },
    @{ Suffix = "-Quetzal"; Iso = "GTQ" },
    @{ Suffix = "-Colon"; Iso = "CRC" },
    @{ Suffix = "-Cordoba"; Iso = "NIO" },
    @{ Suffix = "-Lempira"; Iso = "HNL" },
    @{ Suffix = "-Balboa"; Iso = "PAB" },
    @{ Suffix = "-Dolares"; Iso = "USD" },
    @{ Suffix = "-Guilder"; Iso = "ANG" },
    @{ Suffix = "-Kuna"; Iso = "HRK" },
    @{ Suffix = "-Kip"; Iso = "LAK" },
    @{ Suffix = "-Taka"; Iso = "BDT" },
    @{ Suffix = "-Kyat"; Iso = "MMK" },
    @{ Suffix = "-Riel"; Iso = "KHR" },
    @{ Suffix = "-Tugrik"; Iso = "MNT" },
    @{ Suffix = "-Som"; Iso = "KGS" },
    @{ Suffix = "-Somoni"; Iso = "TJS" },
    @{ Suffix = "-Manat"; Iso = "AZN" },
    @{ Suffix = "-Lari"; Iso = "GEL" },
    @{ Suffix = "-Dram"; Iso = "AMD" },
    @{ Suffix = "-Lev"; Iso = "BGN" },
    @{ Suffix = "-Marka"; Iso = "BAM" },
    @{ Suffix = "-Denar"; Iso = "MKD" },
    @{ Suffix = "-Kwacha"; Iso = "ZMW" },
    @{ Suffix = "-Metical"; Iso = "MZN" },
    @{ Suffix = "-Ouguiya"; Iso = "MRU" },
    @{ Suffix = "-Ariary"; Iso = "MGA" },
    @{ Suffix = "-Dobras"; Iso = "STN" },
    @{ Suffix = "-Pa'Anga"; Iso = "TOP" },
    @{ Suffix = "-Tala"; Iso = "WST" },
    @{ Suffix = "-Vatu"; Iso = "VUV" },
    @{ Suffix = "-Kina"; Iso = "PGK" },
    @{ Suffix = "-Guarani"; Iso = "PYG" },
    @{ Suffix = "-Rufiyaa"; Iso = "MVR" },
    @{ Suffix = "-Maloti"; Iso = "LSL" },
    @{ Suffix = "-Lilangeni"; Iso = "SZL" },
    @{ Suffix = "-Pula"; Iso = "BWP" },
    @{ Suffix = "-Cedi"; Iso = "GHS" },
    @{ Suffix = "-Leone"; Iso = "SLE" },
    @{ Suffix = "-Gourde"; Iso = "HTG" },
    @{ Suffix = "-Afghani"; Iso = "AFN" },
    @{ Suffix = "-Lek"; Iso = "ALL" },
    @{ Suffix = "-Kwanza"; Iso = "AOA" },
    @{ Suffix = "-Gold"; Iso = "ZWG" },
    @{ Suffix = "-Rtgs"; Iso = "ZWL" },
    @{ Suffix = "-Fuerte"; Iso = "VES" },
    @{ Suffix = "-Soberano"; Iso = "VES" }
)

function Get-IsoCode([string]$desc) {
    if ($lookup.ContainsKey($desc)) { return $lookup[$desc] }
    if ($exact.ContainsKey($desc)) { return $exact[$desc] }
    foreach ($rule in $suffixRules) {
        if ($desc.EndsWith($rule.Suffix)) { return $rule.Iso }
    }
    return $null
}

$page = 1
$descriptors = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
do {
    $uri = "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/rates_of_exchange" +
        "?fields=country_currency_desc,effective_date" +
        "&filter=effective_date:gte:$FromDate" +
        "&sort=country_currency_desc" +
        "&page[size]=1000" +
        "&page[number]=$page"
    $resp = Invoke-RestMethod -Uri $uri
    foreach ($item in $resp.data) {
        [void]$descriptors.Add($item.country_currency_desc)
    }
    $totalPages = $resp.meta.pagination.'total-pages'
    $page++
} while ($page -le $totalPages)

$entries = @()
$unmapped = @()
foreach ($desc in ($descriptors | Sort-Object)) {
    $iso = Get-IsoCode $desc
    if ($iso) {
        $entries += [ordered]@{ countryCurrencyDesc = $desc; iso4217 = $iso }
    } else {
        $unmapped += $desc
    }
}

$dir = Split-Path $outputPath -Parent
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
$entries | ConvertTo-Json -Depth 3 | Set-Content -Path $outputPath -Encoding UTF8
Write-Host "FromDate: $FromDate"
Write-Host "Mapped: $($entries.Count), Unmapped: $($unmapped.Count)"
if ($unmapped.Count -gt 0) { $unmapped | ForEach-Object { Write-Host "  UNMAPPED: $_" } }
