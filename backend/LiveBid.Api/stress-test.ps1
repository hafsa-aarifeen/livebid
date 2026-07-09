# stress-test.ps1 — fire N simultaneous bids at the same auction
$auctionId = "f2061b96-22c6-4b01-a936-a1a9be2b7da8"
$bidderId  = "6669416a-32c8-4964-9576-d8b80b17d918"   # bob
$uri = "http://localhost:5150/api/auctions/$auctionId/bids"

# Current price is 55, increment 5 → all five bids use amounts ≥ 60
$amounts = @(60.00, 61.00, 62.00, 63.00, 64.00)

$jobs = foreach ($amount in $amounts) {
    Start-Job -ScriptBlock {
        param($uri, $amount, $bidderId)
        $body = @{ amount = $amount; bidderId = $bidderId } | ConvertTo-Json
        try {
            $r = Invoke-RestMethod -Method Post -Uri $uri -ContentType "application/json" -Body $body
            [PSCustomObject]@{ Amount = $amount; Result = "ACCEPTED"; NewPrice = $r.newCurrentPrice }
        } catch {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $err = ($reader.ReadToEnd() | ConvertFrom-Json).error
            [PSCustomObject]@{ Amount = $amount; Result = "REJECTED"; NewPrice = $null; Reason = $err }
        }
    } -ArgumentList $uri, $amount, $bidderId
}

$jobs | Wait-Job | Receive-Job | Sort-Object Amount | Format-Table Amount, Result, NewPrice, Reason -AutoSize
$jobs | Remove-Job