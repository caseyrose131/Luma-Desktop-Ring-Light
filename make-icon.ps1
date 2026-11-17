$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$sizes = @(16,24,32,48,64,128,256)
$images = @()
foreach ($size in $sizes) {
    $bitmap = New-Object System.Drawing.Bitmap($size,$size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::FromArgb(12,16,21))
    $unit = $size / 80.0
    $graphics.TranslateTransform([single](8*$unit),[single](8*$unit))
    $graphics.ScaleTransform([single]$unit,[single]$unit)
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(132,255,203),7)
    $pen.StartCap = $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Square
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Bevel
    [System.Drawing.PointF[]]$points = @([System.Drawing.PointF]::new(18,9),[System.Drawing.PointF]::new(9,18),[System.Drawing.PointF]::new(9,46),[System.Drawing.PointF]::new(18,55),[System.Drawing.PointF]::new(46,55),[System.Drawing.PointF]::new(55,46),[System.Drawing.PointF]::new(55,18),[System.Drawing.PointF]::new(46,9))
    $graphics.DrawLines($pen,$points)
    $graphics.DrawLine($pen,32,5,32,26)
    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream,[System.Drawing.Imaging.ImageFormat]::Png)
    $images += ,$stream.ToArray()
    if ($size -eq 256) { $bitmap.Save("$PSScriptRoot\LUMA-logo.png",[System.Drawing.Imaging.ImageFormat]::Png) }
    $stream.Dispose(); $pen.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
}
$iconStream = [System.IO.File]::Create("$PSScriptRoot\LUMA.ico")
$writer = New-Object System.IO.BinaryWriter($iconStream)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i=0; $i -lt $sizes.Count; $i++) {
    $dimension = $sizes[$i] % 256
    $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$images[$i].Length); $writer.Write([uint32]$offset)
    $offset += $images[$i].Length
}
foreach ($bytes in $images) { $writer.Write([byte[]]$bytes) }
$writer.Dispose()
