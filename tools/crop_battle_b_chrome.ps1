param(
    [Parameter(Mandatory = $true)][string]$Source,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

Add-Type -AssemblyName System.Drawing

function Export-Crop {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [System.Drawing.Rectangle]$Rect,
        [string]$Name
    )

    $crop = New-Object System.Drawing.Bitmap $Rect.Width, $Rect.Height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($crop)
    $graphics.DrawImage($Bitmap, (New-Object System.Drawing.Rectangle 0, 0, $Rect.Width, $Rect.Height), $Rect, [System.Drawing.GraphicsUnit]::Pixel)
    $graphics.Dispose()
    $crop.Save((Join-Path $OutputDirectory $Name), [System.Drawing.Imaging.ImageFormat]::Png)
    $crop.Dispose()
}

function Clear-ConnectedBackdrop {
    param([string]$Path)

    $source = [System.Drawing.Bitmap]::FromFile($Path)
    $bitmap = New-Object System.Drawing.Bitmap $source.Width, $source.Height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.DrawImageUnscaled($source, 0, 0)
    $graphics.Dispose()
    $source.Dispose()

    $width = $bitmap.Width
    $height = $bitmap.Height
    $visited = New-Object 'bool[,]' $width, $height
    $queue = [System.Collections.Generic.Queue[System.Drawing.Point]]::new()

    function Try-Enqueue([int]$x, [int]$y) {
        if ($x -lt 0 -or $x -ge $width -or $y -lt 0 -or $y -ge $height -or $visited[$x, $y]) { return }
        $color = $bitmap.GetPixel($x, $y)
        $max = [Math]::Max($color.R, [Math]::Max($color.G, $color.B))
        $warmMetal = $color.R -gt 48 -and $color.R -gt ($color.B * 1.18)
        if ($max -gt 44 -or $warmMetal) { return }
        $visited[$x, $y] = $true
        $queue.Enqueue([System.Drawing.Point]::new($x, $y))
    }

    for ($x = 0; $x -lt $width; $x++) { Try-Enqueue $x 0; Try-Enqueue $x ($height - 1) }
    for ($y = 0; $y -lt $height; $y++) { Try-Enqueue 0 $y; Try-Enqueue ($width - 1) $y }

    while ($queue.Count -gt 0) {
        $point = $queue.Dequeue()
        $bitmap.SetPixel($point.X, $point.Y, [System.Drawing.Color]::Transparent)
        Try-Enqueue ($point.X - 1) $point.Y
        Try-Enqueue ($point.X + 1) $point.Y
        Try-Enqueue $point.X ($point.Y - 1)
        Try-Enqueue $point.X ($point.Y + 1)
    }

    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

function Clear-RoundCopy {
    param([string]$Path)

    $source = [System.Drawing.Bitmap]::FromFile($Path)
    $bitmap = New-Object System.Drawing.Bitmap $source.Width, $source.Height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.DrawImageUnscaled($source, 0, 0)
    $graphics.Dispose()
    $source.Dispose()

    $paperSamples = [System.Collections.Generic.List[System.Drawing.Color]]::new()
    foreach ($sampleX in @(27, 28, 29, 66, 67, 68)) {
        for ($sampleY = 30; $sampleY -le 77; $sampleY++) {
            $sample = $bitmap.GetPixel($sampleX, $sampleY)
            if ([Math]::Max($sample.R, [Math]::Max($sample.G, $sample.B)) -gt 58) {
                $paperSamples.Add($sample)
            }
        }
    }

    # Preserve the photographed paper grain by reusing clean pixels from the
    # same tag rather than painting a new flat rectangle.
    for ($y = 33; $y -le 72; $y++) {
        for ($x = 30; $x -le 65; $x++) {
            $index = (($x * 17) + ($y * 31)) % $paperSamples.Count
            $bitmap.SetPixel($x, $y, $paperSamples[$index])
        }
    }

    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

function Apply-PolygonMask {
    param([string]$Path, [System.Drawing.Point[]]$Points)

    $source = [System.Drawing.Bitmap]::FromFile($Path)
    $bitmap = New-Object System.Drawing.Bitmap $source.Width, $source.Height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $pathShape = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $pathShape.AddPolygon($Points)
    $graphics.SetClip($pathShape)
    $graphics.DrawImageUnscaled($source, 0, 0)
    $graphics.ResetClip()
    $graphics.Dispose()
    $pathShape.Dispose()
    $source.Dispose()
    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$sourceBitmap = [System.Drawing.Bitmap]::FromFile($Source)

# B quadrant components from the approved 1672 x 941 comparison sheet.
Export-Crop $sourceBitmap (New-Object System.Drawing.Rectangle 876, 8, 218, 62) "battle-header-slip-b.png"
Export-Crop $sourceBitmap (New-Object System.Drawing.Rectangle 1572, 0, 88, 104) "battle-round-tag-b-source.png"
Export-Crop $sourceBitmap (New-Object System.Drawing.Rectangle 1507, 390, 151, 69) "battle-end-turn-b.png"

$sourceBitmap.Dispose()

$headerPath = Join-Path $OutputDirectory "battle-header-slip-b.png"
$roundSourcePath = Join-Path $OutputDirectory "battle-round-tag-b-source.png"
$roundPath = Join-Path $OutputDirectory "battle-round-tag-b.png"
$turnPath = Join-Path $OutputDirectory "battle-end-turn-b.png"

Copy-Item -Force $roundSourcePath $roundPath
Clear-ConnectedBackdrop $headerPath
Apply-PolygonMask $roundPath @(
    [System.Drawing.Point]::new(42, 0), [System.Drawing.Point]::new(50, 0),
    [System.Drawing.Point]::new(51, 22), [System.Drawing.Point]::new(69, 22),
    [System.Drawing.Point]::new(77, 30), [System.Drawing.Point]::new(77, 88),
    [System.Drawing.Point]::new(69, 97), [System.Drawing.Point]::new(21, 97),
    [System.Drawing.Point]::new(14, 88), [System.Drawing.Point]::new(14, 30),
    [System.Drawing.Point]::new(22, 22), [System.Drawing.Point]::new(41, 22)
)
Clear-RoundCopy $roundPath
Apply-PolygonMask $turnPath @(
    [System.Drawing.Point]::new(12, 4), [System.Drawing.Point]::new(133, 4),
    [System.Drawing.Point]::new(146, 16), [System.Drawing.Point]::new(146, 55),
    [System.Drawing.Point]::new(134, 66), [System.Drawing.Point]::new(12, 66),
    [System.Drawing.Point]::new(2, 55), [System.Drawing.Point]::new(2, 16)
)
Remove-Item $roundSourcePath
