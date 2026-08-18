param(
    [Parameter(Mandatory = $true)]
    [string]$SourcePath,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$source = [System.Drawing.Bitmap]::new((Resolve-Path -LiteralPath $SourcePath).Path)
$pixelFormat = [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
$alpha = [System.Drawing.Bitmap]::new($source.Width, $source.Height, $pixelFormat)

try {
    for ($y = 0; $y -lt $source.Height; $y++) {
        for ($x = 0; $x -lt $source.Width; $x++) {
            $color = $source.GetPixel($x, $y)
            $strongestNonGreen = [Math]::Max([int]$color.R, [int]$color.B)
            $greenExcess = [int]$color.G - $strongestNonGreen
            $keyAmount = [Math]::Max(0.0, [Math]::Min(1.0, ($greenExcess - 12.0) / 165.0))
            $newAlpha = [int][Math]::Round(255.0 * (1.0 - $keyAmount))

            if ($newAlpha -le 3) {
                $alpha.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
                continue
            }

            $despilledGreen = [Math]::Min([int]$color.G, $strongestNonGreen + 18)
            $alpha.SetPixel(
                $x,
                $y,
                [System.Drawing.Color]::FromArgb($newAlpha, $color.R, $despilledGreen, $color.B))
        }
    }

    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

    $regions = @(
        @{ Name = 'journey-vitals-parchment-v1.png'; X = 105; Y = 38; Width = 735; Height = 247 },
        @{ Name = 'journey-action-badge-v1.png'; X = 938; Y = 38; Width = 255; Height = 250 },
        @{ Name = 'journey-command-strip-v1.png'; X = 30; Y = 312; Width = 1385; Height = 200 },
        @{ Name = 'journey-resource-dock-v1.png'; X = 58; Y = 552; Width = 685; Height = 365 },
        @{ Name = 'journey-skill-plaque-v1.png'; X = 805; Y = 530; Width = 565; Height = 205 },
        @{ Name = 'journey-end-turn-envelope-v1.png'; X = 805; Y = 730; Width = 555; Height = 315 }
    )

    foreach ($region in $regions) {
        $rect = [System.Drawing.Rectangle]::new(
            [int]$region.X,
            [int]$region.Y,
            [int]$region.Width,
            [int]$region.Height)
        $piece = $alpha.Clone($rect, $pixelFormat)
        try {
            $outputPath = Join-Path $OutputDirectory $region.Name
            $piece.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $piece.Dispose()
        }
    }
}
finally {
    $alpha.Dispose()
    $source.Dispose()
}

Write-Output "Extracted $($regions.Count) UI sprites to $OutputDirectory"
