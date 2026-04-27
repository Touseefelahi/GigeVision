Imports System.Threading
Imports System.Diagnostics
Imports System.IO
Imports System.Runtime.InteropServices
Imports Emgu.CV
Imports Emgu.CV.CvEnum
Imports GigeVision.Core.Services
Imports GigeVision.OpenCV
Imports GigeVision.Core.Models
Imports GigeVision.Core.Enums
Public Class Form1

    ' ——————————————————————————————
    ' Fields
    ' ——————————————————————————————
    Private cameraService As Camera
    Private streamReceiver = New StreamReceiverParallelOpencv(2)
    Private fpsCounter As Integer
    Private isRecording As Boolean
    Private stopwatch As Stopwatch
    Private imagePath As String
    Private processingThread As Thread
    Private Ip As String
    Private CameraListLoaded As Boolean = False
    Private isFormLoaded As Boolean = False
    Private camerasList As List(Of CameraInformation)
    Private displayCancelTokenSource As Threading.CancellationTokenSource
    Public Event FrameReady(bmp As Bitmap)
    ' --- Color handling state ---
    Private currentBayerConv As ColorConversion = ColorConversion.BayerBg2Bgr
    ' ——————————————————————————————
    ' Constructor / Load
    ' ——————————————————————————————
    Public Sub New()
        InitializeComponent()

        ' 2️ Pre-set packet size control
        nudPacketSize.Minimum = 64
        nudPacketSize.Maximum = 9000
        nudPacketSize.Value = 9000
        btnStart.Enabled = False
        cameraService = New Camera() With {.StreamReceiver = streamReceiver}
        cameraService.Gvcp.ElapsedOneSecond = AddressOf OnElapsedOneSecond
        btnStart.Enabled = True
    End Sub

    ' ——————————————————————————————
    ' Discovering Cameras
    ' ——————————————————————————————
    Private Async Function LoadCameraListAsync() As Task
        camerasList = Await cameraService.Gvcp.GetAllGigeDevicesInNetworkAsnyc()

        If camerasList Is Nothing OrElse camerasList.Count = 0 Then Exit Function

        If InvokeRequired Then
            BeginInvoke(Sub() BindCameras(camerasList))
        Else
            BindCameras(camerasList)
        End If
    End Function

    Private Function GetSelectedCamera() As CameraInformation
        Return TryCast(ComboBoxIP.SelectedItem, CameraInformation)
    End Function

    Private Sub BindCameras(cams As List(Of CameraInformation))
        ComboBoxIP.DataSource = cams
        ComboBoxIP.DisplayMember = "IP"
        ComboBoxIP.SelectedIndex = 0
        cameraService.IP = cams(0).IP
        cameraService.RxIP = cams(0).NetworkIP
        CameraListLoaded = True
    End Sub
    ' ——————————————————————————————
    ' Shared 1-Second Tick Handler
    ' ——————————————————————————————
    Private Sub OnElapsedOneSecond(sender As Object, e As EventArgs)
        If IsDisposed OrElse Not IsHandleCreated Then Return

        ' Post to UI thread and return—no blocking of the heartbeat thread.
        BeginInvoke(CType(Sub()
                              lblFps.Text = $"FPS: {fpsCounter}"
                              lblTotalFrames.Text = $"Total: {streamReceiver.imageIndex}"
                              fpsCounter = 0
                          End Sub, MethodInvoker))
    End Sub
    ' ——————————————————————————————
    ' Start/Stop Button
    ' ——————————————————————————————
    Private Async Sub btnStart_Click(sender As Object, e As EventArgs) Handles btnStart.Click
        btnStart.Enabled = False
        Try
            Dim running = Await StartLiveFeed()
            btnStart.Text = If(running, "Stop", "Start")
            ComboBoxIP.Enabled = Not running
        Catch ex As Exception
            MessageBox.Show(ex.Message, "Start error")
            btnStart.Text = "Start"
            ComboBoxIP.Enabled = True
        Finally
            btnStart.Enabled = True
        End Try
    End Sub

    Private Async Function StartLiveFeed() As Task(Of Boolean)
        If Not CameraListLoaded Then Return False

        Dim selectedCamera = TryCast(ComboBoxIP.SelectedItem, CameraInformation)
        If selectedCamera Is Nothing Then Return False

        ' Always point the service at the camera the user picked
        cameraService.IP = selectedCamera.IP
        cameraService.RxIP = selectedCamera.NetworkIP

        ' Toggle stop
        If cameraService.IsStreaming Then
            displayCancelTokenSource.Cancel()
            Await cameraService.StopStream()
            Return False
        End If

        displayCancelTokenSource = New CancellationTokenSource

        ' Safer default if the network isn’t jumbo end-to-end
        Dim desired = CUInt(nudPacketSize.Value)
        If desired > 1500UI Then desired = 1440UI
        cameraService.Payload = desired

        ' Start the stream; this syncs parameters so Width/Height become valid
        Dim started = Await cameraService.StartStreamAsync()
        If Not started Then Return False

        'Dim pfName As String = cameraService.PixelFormat.ToString()

        'Dim basePat = BasePatternFromPf(pfName)

        'Dim offX As Integer = CInt((Await cameraService.GetParameterValue("OffsetX")).GetValueOrDefault(0))
        'Dim offY As Integer = CInt((Await cameraService.GetParameterValue("OffsetY")).GetValueOrDefault(0))
        'Dim flipX As Boolean = Await cameraService.GetParameterValue("ReverseX").ContinueWith(Function(t) t.Result.GetValueOrDefault(0) <> 0)
        'Dim flipY As Boolean = Await cameraService.GetParameterValue("ReverseY").ContinueWith(Function(t) t.Result.GetValueOrDefault(0) <> 0)

        'Dim pat = AdjustForParity(basePat, flipX, flipY, offX, offY)
        'currentBayerConv = ConvFromPatternBGR(pat)


        ' Kick off processing if needed
        If processingThread Is Nothing OrElse Not processingThread.IsAlive Then
            processingThread = New Thread(Sub() ProcessingPipeline(displayCancelTokenSource.Token)) With {
            .IsBackground = True,
            .Priority = ThreadPriority.Highest
        }
            processingThread.Start()
        End If

        Return True
    End Function

    Private Sub ProcessingPipeline(cancel As CancellationToken)
        Dim localBufferIndex As Integer = 0

        ' Wait for first frame signal so sizes are real
        streamReceiver.waitHandleFrame.Wait()

        Dim w = streamReceiver.GvspInfo.Width
        Dim h = streamReceiver.GvspInfo.Height
        Dim colorMat = New Mat(CInt(h), CInt(w), DepthType.Cv8U, 3)

        Do
            If (cancel.IsCancellationRequested) Then
                Return
            End If

            ' wait for next frame
            streamReceiver.waitHandleFrame.Wait()

            ' drain backlog
            While localBufferIndex < streamReceiver.frameInCounter

                If (cancel.IsCancellationRequested) Then
                    Return
                End If

                Dim src = streamReceiver.image(localBufferIndex Mod streamReceiver.TotalBuffers)

                If src Is Nothing OrElse src.IsEmpty OrElse src.Width = 0 OrElse src.Height = 0 Then
                    localBufferIndex += 1
                    Continue While
                End If

                ' If the camera is Bayer 8-bit, convert to BGR for preview.
                ' Pick the pattern that matches your camera (Rg/Gb/Gr/Bg).
                ' If it’s Mono8: use Gray2Bgr. If 16-bit: scale to 8-bit first.
                If src.NumberOfChannels = 1 Then
                    ' Use the detected pattern
                    Dim conv As ColorConversion = currentBayerConv

                    If src.Depth = DepthType.Cv8U Then
                        CvInvoke.CvtColor(src, colorMat, conv)
                    Else
                        ' 10/12/16-bit → scale then demosaic
                        Dim bits As Integer = 16
                        Dim px = cameraService.PixelFormat.ToString()
                        If px.Contains("10") Then bits = 10
                        If px.Contains("12") Then bits = 12
                        If px.Contains("16") Then bits = 16

                        Dim scale As Double = 255.0 / ((1 << bits) - 1)
                        Using tmp8 As New Mat(src.Rows, src.Cols, DepthType.Cv8U, 1)
                            CvInvoke.ConvertScaleAbs(src, tmp8, scale, 0)
                            CvInvoke.CvtColor(tmp8, colorMat, conv)
                        End Using
                    End If
                Else
                    ' 3-channel path: if camera delivers RGB8Packed, convert to BGR for Bitmap
                    Dim pf = cameraService.PixelFormat.ToString()
                    If pf.Contains("RGB8") AndAlso Not pf.Contains("BGR") Then
                        CvInvoke.CvtColor(src, colorMat, ColorConversion.Rgb2Bgr)
                    Else
                        src.CopyTo(colorMat)
                    End If
                End If


                pbImage.BeginInvoke(Sub()
                                        If pbImage.Image IsNot Nothing Then pbImage.Image.Dispose()
                                        pbImage.Image = colorMat.ToBitmap()
                                    End Sub)

                localBufferIndex += 1
                Interlocked.Increment(fpsCounter)
            End While
        Loop
    End Sub

    Private Sub SaveXML_Click(sender As Object, e As EventArgs) Handles SaveXML.Click
        cameraService.Gvcp.SaveXmlFileFromCamera(Application.StartupPath)
    End Sub

    Private xmlLoaded As Boolean

    Private Async Sub GetExposureGainModes_Click(sender As Object, e As EventArgs) Handles GetExposureGainModes.Click
        Dim r As (GainDB As Double, ExposureUs As Double)
        Try
            r = Await ReadGainExposureDirectAsync(cameraService)
        Catch ex As Exception

        End Try

        Try
            ' 2) The two parameters we care about:
            Dim paramNames = New List(Of String) From {
                "ExposureMode",
                "ExposureAuto",
                "GainAuto",
                "PixelFormat"
            }

            ' We'll collect our display strings here
            Dim display = New Dictionary(Of String, String)

            For Each pname In paramNames
                ' 2a) load its metadata
                If Not Await cameraService.LoadParameter(pname) Then
                    display(pname) = "<not supported>"
                    Continue For
                End If

                ' 2b) fetch the ICategory
                Dim cat As GenICam.ICategory = Await cameraService.GetParameter(pname)
                If cat Is Nothing Then
                    display(pname) = "<missing>"
                    Continue For
                End If

                ' 3) Branch on whether it's an enum or an integer
                If TypeOf cat Is GenICam.GenEnumeration Then
                    Dim ecat = DirectCast(cat, GenICam.GenEnumeration)
                    Dim raw? As Long = Await ecat.PValue.GetValueAsync()
                    If Not raw.HasValue Then
                        display(pname) = "<read error>"
                    Else
                        ' find the matching key in Entries
                        Dim nameEntry = ecat.Entries _
                            .Where(Function(kvp) kvp.Value.Value = raw.Value) _
                            .Select(Function(kvp) kvp.Key) _
                            .FirstOrDefault()
                        display(pname) = If(nameEntry, $"<unknown:{raw}>")
                    End If

                ElseIf TypeOf cat Is GenICam.GenInteger Then
                    Dim icat = DirectCast(cat, GenICam.GenInteger)
                    Dim raw? As Long = Await icat.PValue.GetValueAsync()
                    If Not raw.HasValue Then
                        display(pname) = "<read error>"
                    Else
                        display(pname) = raw.Value.ToString()
                    End If
                ElseIf TypeOf cat Is GenICam.GenFloat Then
                    Dim fcat = DirectCast(cat, GenICam.GenFloat)
                    Dim rawf? As Double = Await fcat.PValue.GetValueAsync()
                    If Not rawf.HasValue Then
                        display(pname) = "<read error>"
                    Else
                        display(pname) = rawf.Value.ToString()
                    End If
                Else
                    display(pname) = "<unsupported type>"
                End If
            Next
            Dim values = If(Not IsNothing(r), $"Gain: {r.GainDB:0.00} dB{vbCrLf}Exposure: {r.ExposureUs:0.##} µs{vbCrLf}", "")
            ' 4) Show them both
            MessageBox.Show(
                values &
                $"Exposure Mode : {display("ExposureMode")}{vbCrLf}" &
                $"Exposure Auto : {display("ExposureAuto")}{vbCrLf}" &
                $"GainAuto : {display("GainAuto")}{vbCrLf}" &
                $"PixelFormat : {display("PixelFormat")}{vbCrLf}" &
                "Camera Features"
              )
        Catch ex As Exception

        End Try

    End Sub

    ''' <summary>
    ''' Mako GainRaw ExposureValue
    ''' FLIR GainRaw ExposureTime_FloatVal
    ''' Menta RegGainValueFloat, GainValue
    ''' </summary>
    ''' <param name="cam"></param>
    ''' <returns></returns>
    Public Async Function ReadGainExposureDirectAsync(cam As Camera) As Task(Of (GainDb As Double, ExposureUs As Double))

        Dim exposureAliasList = {"ExposureTime", "ExposureTimeAbs", "ExposureTime_FloatVal", "ExposureValueFloat", "ExposureValue", "ExposureTimeRaw"}
        Dim gainAliasList = {"Gain", "GainValue", "GainRaw", "RegGainValueFloat", "RegGainValue"}

        '' Take control so the device accepts our reads
        Await cam.Gvcp.TakeControl(False)


        ' ---- GainRaw -> dB ----
        Dim gain As Double = 0
        'Dim gr = Await cam.Gvcp.ReadRegisterAsync(cam.IP, "00104404")  ' GainRaw_Val
        'Dim rawGain As Integer = CInt(gr.RegisterValue)
        'gain = rawGain * 6.020599913 / 16384.0

        For Each paramName As String In gainAliasList
            Dim gainParam = Await cam.GetParameterValue(paramName).ConfigureAwait(False) 'GainRaw
            If (gainParam.HasValue) Then
                gain = If(Name = "GainRaw", gainParam.Value / 10, gainParam.Value)
                Exit For
            End If
        Next


        ' ---- ExposureTime (µs) ----
        Dim exposure As Double = 0
        For Each paramName As String In exposureAliasList
            Dim exposureParam = Await cam.GetParameterValue(paramName).ConfigureAwait(False) 'ExposureValue '000C1004, 000C1024 ExposureTime_FloatVal
            If (exposureParam.HasValue) Then
                exposure = exposureParam.Value
                Exit For
            End If
        Next


        Return (gain, exposure)
    End Function

    Private Async Function TrySetEnum(cam As Camera, name As String, choice As String) As Task
        Try
            Dim en = TryCast(Await cam.GetParameter(name), GenICam.GenEnumeration)
            If en IsNot Nothing AndAlso en.Entries.ContainsKey(choice) Then
                Await en.SetValueAsync(en.Entries(choice).Value)
            End If
        Catch
            ' ignore if not writable/available
        End Try
    End Function

    Private Async Sub ComboBoxIP_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxIP.SelectionChangeCommitted
        If ComboBoxIP.SelectedIndex < 0 OrElse camerasList Is Nothing OrElse isFormLoaded = False Then Exit Sub
        Try
            Try : Await cameraService.Gvcp.ReadXmlFileAsync(cameraService.IP) : Catch : End Try

            cameraService.IP = camerasList(ComboBoxIP.SelectedIndex).IP
            cameraService.RxIP = camerasList(ComboBoxIP.SelectedIndex).NetworkIP
        Catch
            ' ignore
        End Try
    End Sub

    Private Async Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Shown
        Await LoadCameraListAsync()
        isFormLoaded = True
    End Sub

    ' --- helpers ---
    Private Shared Function BasePatternFromPf(pf As String) As String
        Dim s = New String((If(pf, "")).Where(Function(c) Char.IsLetter(c)).ToArray()).ToUpperInvariant()
        If s.Contains("BAYERRG") Then Return "RG"
        If s.Contains("BAYERGR") Then Return "GR"
        If s.Contains("BAYERGB") Then Return "GB"
        If s.Contains("BAYERBG") Then Return "BG"
        Return "BG" ' safe default
    End Function

    Private Shared Function AdjustForParity(basePat As String, flipX As Boolean, flipY As Boolean,
                                        offX As Integer, offY As Integer) As String
        Dim hx = (offX And 1) Xor If(flipX, 1, 0)   ' horizontal phase toggle
        Dim hy = (offY And 1) Xor If(flipY, 1, 0)   ' vertical   phase toggle
        Dim p = basePat

        If hx = 1 AndAlso hy = 0 Then               ' horizontal mirror / odd X offset
            If p = "RG" Then p = "GR"
        ElseIf p = "GR" Then
            p = "RG"
        ElseIf p = "GB" Then
            p = "BG"
        ElseIf p = "BG" Then
            p = "GB"
        ElseIf hx = 0 AndAlso hy = 1 Then           ' vertical mirror / odd Y offset
            If p = "RG" Then
                p = "GB"
            End If
        ElseIf p = "GR" Then
            p = "BG"
        ElseIf p = "GB" Then
            p = "RG"
        ElseIf p = "BG" Then
            p = "GR"
        ElseIf hx = 1 AndAlso hy = 1 Then           ' both axes (180°)
            If p = "RG" Then p = "BG"
        ElseIf p = "GR" Then
            p = "GB"
        ElseIf p = "GB" Then
            p = "GR"
        ElseIf p = "BG" Then
            p = "RG"
        End If
        Return p
    End Function

    Private Shared Function ConvFromPatternBGR(p As String) As ColorConversion
        Select Case p
            Case "RG" : Return ColorConversion.BayerRg2Bgr
            Case "GR" : Return ColorConversion.BayerGr2Bgr
            Case "GB" : Return ColorConversion.BayerGb2Bgr
            Case Else : Return ColorConversion.BayerBg2Bgr
        End Select
    End Function


End Class
