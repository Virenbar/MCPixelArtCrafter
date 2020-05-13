﻿Imports MCPixelArtCrafter.Data.SPQ
Imports SimplePaletteQuantizer.Ditherers
Imports SimplePaletteQuantizer.Ditherers.Ordered
Imports SimplePaletteQuantizer.Ditherers.ErrorDiffusion
Imports SimplePaletteQuantizer.Helpers
Imports SimplePaletteQuantizer.Quantizers

Namespace Helpers
    Public Class MapResultCreator
        Dim targetImage As Image
        Private Shared activeQuantizer As IColorQuantizer = New MapColorQuantizer
        Private Shared activeDitherer As IColorDitherer = New FilterLiteSierra 'New FloydSteinbergDitherer

        'Public Shared Async Function CreateMap(Image As Bitmap, progress As IProgress(Of Integer), token As Threading.CancellationToken) As Task(Of MapResult)
        '    Dim result As MapResult

        '    ' Dim t = MapResultCreator.CreateMap(Image)
        '    Dim InImage = New Bitmap(Image)
        '    Dim w = InImage.Width
        '    Dim h = InImage.Height
        '    ReDim Map(w - 1, h - 1)
        '    _OutImage = New Bitmap(w, h)
        '    Dim closest As MapColor
        '    Await Task.Run(
        '        Sub()
        '            For x = 0 To w - 1
        '                For y = 0 To h - 1
        '                'progress.Report(x * h + (y + 1))
        '                If token.IsCancellationRequested Then
        '                        token.ThrowIfCancellationRequested()
        '                    End If
        '                    Dim sPixel = InImage.GetPixel(x, y)
        '                    If sPixel.A < 256 / 2 Then Continue For
        '                    closest = MapColorsCollection.GetClosest(sPixel)
        '                    If Config.Dither Then
        '                        FSDither.ApplyDither(InImage, closest.Color, sPixel, x, y)
        '                    End If
        '                    OutImage.SetPixel(x, y, closest.Color)
        '                    If Not UsedMapColors.ContainsKey(closest) Then UsedMapColors.Add(closest, 0)
        '                    UsedMapColors(closest) += 1
        '                    Map(x, y) = closest
        '                Next
        '                progress.Report((x + 1) * h)
        '            Next
        '        End Sub)
        '    Await Task.Delay(1 * 500)
        'End Function

        Public Shared Async Function CreateMap(image As Image) As Task(Of Object)
            MapColorQuantizer.SetPalette(MapColorsCollection.Palette)
            Dim targetImage = Await Quantize(image.Clone)
            targetImage.Save("D:\test.png", Imaging.ImageFormat.Png)
            Return targetImage
        End Function
        Private Shared Async Function Quantize(img As Image) As Task(Of Image)
            Dim targetImage As Image = Nothing
            Dim sourceImage = img

            ' tries to retrieve an image based on HSB quantization
            Dim parallelTaskCount = If(activeQuantizer.AllowParallel, Convert.ToInt32(1), 1)
            Dim uiScheduler = TaskScheduler.FromCurrentSynchronizationContext()
            Dim colorCount = MapColorsCollection.Palette.Count

            ' disables all the controls And starts running
            Dim before = Now

            ' quantization process
            Dim quantization = Task.Factory.StartNew(
                Sub() targetImage = ImageBuffer.QuantizeImage(sourceImage, activeQuantizer, activeDitherer, colorCount, parallelTaskCount),
                TaskCreationOptions.LongRunning)

            ' finishes after running
            Await quantization.ContinueWith(
                Sub()
                    ' detects operation duration
                    Dim duration = Now - before
                    Dim perPixel = New TimeSpan(duration.Ticks / (sourceImage.Width * sourceImage.Height))

                    ' detects error And color count
                    Dim originalColorCount = activeQuantizer.GetColorCount()
                    Dim nrmsdString = String.Empty

                    ' calculates NRMSD error, if requested
                    If (True) Then
                        Dim nrmsd = ImageBuffer.CalculateImageNormalizedMeanError(sourceImage, targetImage, parallelTaskCount)
                        nrmsdString = String.Format(" (NRMSD = {0:0.#####})", nrmsd)
                    End If

                    ' spits some duration statistics (those actually slow the processing quite a bit, turn them off to make it quicker)
                    ' editSourceInfo.Text = String.Format("Original: {0} colors ({1} x {2})", originalColorCount, sourceImage.Width, sourceImage.Height)
                    ' editTargetInfo.Text = String.Format("Quantized: {0} colors{1}", colorCount, nrmsdString)
                End Sub,
                uiScheduler)
            Return targetImage
        End Function
    End Class
End Namespace
