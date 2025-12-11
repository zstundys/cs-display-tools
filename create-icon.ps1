# Create application icon for DisplayRefreshRate
# Generates a 32x32 ICO file with "Hz" text

Add-Type -AssemblyName System.Drawing

$iconPath = Join-Path $PSScriptRoot "app.ico"
[int]$size = 32

# Create bitmap
$bitmap = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# High quality rendering
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$graphics.Clear([System.Drawing.Color]::Transparent)

# Background - rounded purple rectangle
$bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 88, 86, 214))
[int]$radius = 4
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddArc(0, 0, $radius * 2, $radius * 2, 180, 90)
$path.AddArc($size - $radius * 2 - 1, 0, $radius * 2, $radius * 2, 270, 90)
$path.AddArc($size - $radius * 2 - 1, $size - $radius * 2 - 1, $radius * 2, $radius * 2, 0, 90)
$path.AddArc(0, $size - $radius * 2 - 1, $radius * 2, $radius * 2, 90, 90)
$path.CloseFigure()
$graphics.FillPath($bgBrush, $path)

# Draw "Hz" text centered
$textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$font = New-Object System.Drawing.Font("Segoe UI", 12, [System.Drawing.FontStyle]::Bold)
$text = "Hz"
$textSize = $graphics.MeasureString($text, $font)
$x = ($size - $textSize.Width) / 2
$y = ($size - $textSize.Height) / 2
$graphics.DrawString($text, $font, $textBrush, $x, $y)

# Convert to Icon and save
$hIcon = $bitmap.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)
$fs = [System.IO.File]::Create($iconPath)
$icon.Save($fs)
$fs.Close()

# Cleanup
$icon.Dispose()
$graphics.Dispose()
$bitmap.Dispose()
$font.Dispose()
$textBrush.Dispose()
$bgBrush.Dispose()
$path.Dispose()

Write-Host "Icon created: $iconPath" -ForegroundColor Green
