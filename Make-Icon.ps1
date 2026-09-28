# Resize the user-supplied artwork without changing its design.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$source=[System.Drawing.Image]::FromFile((Join-Path $PSScriptRoot 'assets\icon-original.png'))
$result=[System.Drawing.Bitmap]::new(256,256)
$graphics=[System.Drawing.Graphics]::FromImage($result)
try {
    $graphics.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.CompositingQuality=[System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.DrawImage($source,0,0,256,256)
    $result.Save((Join-Path $PSScriptRoot 'package\icon.png'),[System.Drawing.Imaging.ImageFormat]::Png)
} finally {
    $graphics.Dispose(); $result.Dispose(); $source.Dispose()
}
