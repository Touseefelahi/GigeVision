Imports System.Threading
Imports System.Diagnostics
Imports System.IO
Imports System.Runtime.InteropServices
Imports Emgu.CV
Imports Emgu.CV.CvEnum
Imports GigeVision.Core.Services
Imports GigeVision.OpenCV
Imports GigeVision.Core.Models
Public Class Form1

    ' ——————————————————————————————
    ' Fields
    ' ——————————————————————————————
    Private cameraService As Camera
    Private streamReceiver As New StreamReceiverParallelOpencv(2)
    Private imageBuffers() As Bitmap
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
        PrepCamera()
        btnStart.Enabled = True
    End Sub

    Async Sub PrepCamera()
        'GigeVision.Core.NetworkService.AllowAppThroughFirewall()
        cameraService = New Camera() With {.StreamReceiver = streamReceiver}
        cameraService.IsMulticast = True
        cameraService.Gvcp.ElapsedOneSecond = AddressOf OnElapsedOneSecond
        Await LoadCameraListAsync()
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
        ' Marshal back to UI thread if necessary
        If InvokeRequired Then
            Invoke(Sub() OnElapsedOneSecond(sender, e))
            Return
        End If

        lblFps.Text = $"FPS: {fpsCounter}"
        lblTotalFrames.Text = $"Total: {streamReceiver.imageIndex}"
        fpsCounter = 0
    End Sub

    ' ——————————————————————————————
    ' Start/Stop Button
    ' ——————————————————————————————
    Private Async Sub btnStart_Click(sender As Object, e As EventArgs) Handles btnStart.Click

        If (cameraService.Gvcp.IsXmlFileLoaded) Then
            Await cameraService.Gvcp.ReadXmlFileAsync()
        End If

        If (cameraService.Width <= 0 OrElse cameraService.Height <= 0) Then
            'MessageBox.Show("Test")
            Exit Sub
        End If

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

        ' Toggle stop
        If cameraService.IsStreaming Then
            displayCancelTokenSource.Cancel()
            Await cameraService.StopStream()
            Return False
        End If

        displayCancelTokenSource = New CancellationTokenSource

        ' Safer default if your network isn’t jumbo end-to-end
        Dim desired = CUInt(nudPacketSize.Value)
        If desired > 1500UI Then desired = 1440UI
        cameraService.Payload = desired

        ' Start stream (this calls SyncParameters internally → Width/Height become valid)
        Dim started = Await cameraService.StartStreamAsync()
        If Not started Then Return False

        ' Allocate AFTER stream start so Width/Height are valid
        ReDim imageBuffers(streamReceiver.TotalBuffers - 1)
        For i = 0 To streamReceiver.TotalBuffers - 1
            imageBuffers(i) = New Bitmap(CInt(cameraService.Width),
                                     CInt(cameraService.Height),
                                     Imaging.PixelFormat.Format24bppRgb)
        Next

        ' Start the processing loop (you had removed this earlier)
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
                    If cameraService.PixelFormat.ToString().Contains("Bayer") Then
                        CvInvoke.CvtColor(src, colorMat, ColorConversion.BayerRg2Bgr) ' change if your pattern differs
                    Else
                        ' Mono → color for display
                        CvInvoke.CvtColor(src, colorMat, ColorConversion.Gray2Bgr)
                    End If
                Else
                    ' Already 3-channel; just copy
                    src.CopyTo(colorMat)
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

    Private Async Sub Button2_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Try

            ' 2) The two parameters we care about:
            Dim paramNames = New List(Of String) From {
                "ExposureMode",
                "ExposureAuto",
                "GainAuto"
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
                    Dim rawf? As Long = Await fcat.PValue.GetValueAsync()
                    If Not rawf.HasValue Then
                        display(pname) = "<read error>"
                    Else
                        display(pname) = rawf.Value.ToString()
                    End If
                Else
                    display(pname) = "<unsupported type>"
                End If
            Next

            ' 4) Show them both
            MessageBox.Show(
                $"Exposure Mode : {display("ExposureMode")}{vbCrLf}" &
                $"Exposure Auto : {display("ExposureAuto")}{vbCrLf}" &
                $"GainAuto : {display("GainAuto")}{vbCrLf}",
                "Camera Features"
            )
        Catch ex As Exception

        End Try

    End Sub

    Private Async Sub Button2_Click_1(sender As Object, e As EventArgs) Handles Button2.Click
        Try
            Dim r = Await ReadGainExposureDirectAsync(cameraService)
            MessageBox.Show($"Gain: {r.GainDb:0.00} dB{vbCrLf}Exposure: {r.ExposureUs:0.##} µs")

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

    Private Sub ComboBoxIP_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxIP.SelectedIndexChanged
        If (Not isFormLoaded OrElse cameraService.IP = "") Then
            Exit Sub
        End If
        Try
            cameraService.IP = camerasList(ComboBoxIP.SelectedIndex).IP
            cameraService.RxIP = camerasList(ComboBoxIP.SelectedIndex).NetworkIP
        Catch ex As Exception

        End Try
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        isFormLoaded = True
    End Sub
End Class
