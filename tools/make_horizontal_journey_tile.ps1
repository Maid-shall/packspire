param(
    [Parameter(Mandatory = $true)]
    [string] $InputPath,

    [Parameter(Mandatory = $true)]
    [string] $OutputPath
)

Add-Type -AssemblyName System.Drawing

$source = [System.Drawing.Bitmap]::new((Resolve-Path -LiteralPath $InputPath).Path)
$tile = [System.Drawing.Bitmap]::new(
    $source.Width,
    $source.Height,
    [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

try {
    $half = [int]($source.Width / 2)
    $quarter = [int]($source.Width / 4)

    # Use the source's central half, then mirror it. Both outer edges therefore
    # contain the same pixel column and remain continuous after horizontal wrap.
    for ($y = 0; $y -lt $source.Height; $y++) {
        for ($x = 0; $x -lt $half; $x++) {
            $pixel = $source.GetPixel($quarter + $x, $y)
            $tile.SetPixel($x, $y, $pixel)
            $tile.SetPixel($source.Width - 1 - $x, $y, $pixel)
        }
    }

    $destination = [System.IO.Path]::GetFullPath($OutputPath)
    $parent = [System.IO.Path]::GetDirectoryName($destination)
    [System.IO.Directory]::CreateDirectory($parent) | Out-Null
    $tile.Save($destination, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $tile.Dispose()
    $source.Dispose()
}
