<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        btnStart = New Button()
        nudPacketSize = New NumericUpDown()
        pbImage = New PictureBox()
        lblFps = New Label()
        lblTotalFrames = New Label()
        SaveXML = New Button()
        Button1 = New Button()
        Button2 = New Button()
        ComboBoxIP = New ComboBox()
        CType(nudPacketSize, ComponentModel.ISupportInitialize).BeginInit()
        CType(pbImage, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' btnStart
        ' 
        btnStart.Location = New Point(225, 11)
        btnStart.Name = "btnStart"
        btnStart.Size = New Size(75, 23)
        btnStart.TabIndex = 1
        btnStart.Text = "Start"
        btnStart.UseVisualStyleBackColor = True
        ' 
        ' nudPacketSize
        ' 
        nudPacketSize.Location = New Point(306, 12)
        nudPacketSize.Maximum = New Decimal(New Integer() {9000, 0, 0, 0})
        nudPacketSize.Minimum = New Decimal(New Integer() {64, 0, 0, 0})
        nudPacketSize.Name = "nudPacketSize"
        nudPacketSize.Size = New Size(60, 23)
        nudPacketSize.TabIndex = 3
        nudPacketSize.Value = New Decimal(New Integer() {1200, 0, 0, 0})
        ' 
        ' pbImage
        ' 
        pbImage.Location = New Point(12, 41)
        pbImage.Name = "pbImage"
        pbImage.Size = New Size(640, 480)
        pbImage.SizeMode = PictureBoxSizeMode.Zoom
        pbImage.TabIndex = 4
        pbImage.TabStop = False
        ' 
        ' lblFps
        ' 
        lblFps.AutoSize = True
        lblFps.Location = New Point(12, 524)
        lblFps.Name = "lblFps"
        lblFps.Size = New Size(29, 15)
        lblFps.TabIndex = 5
        lblFps.Text = "FPS:"
        ' 
        ' lblTotalFrames
        ' 
        lblTotalFrames.AutoSize = True
        lblTotalFrames.Location = New Point(200, 524)
        lblTotalFrames.Name = "lblTotalFrames"
        lblTotalFrames.Size = New Size(36, 15)
        lblTotalFrames.TabIndex = 7
        lblTotalFrames.Text = "Total:"
        ' 
        ' SaveXML
        ' 
        SaveXML.Location = New Point(372, 11)
        SaveXML.Name = "SaveXML"
        SaveXML.Size = New Size(75, 23)
        SaveXML.TabIndex = 8
        SaveXML.Text = "Save XML"
        SaveXML.UseVisualStyleBackColor = True
        ' 
        ' Button1
        ' 
        Button1.Location = New Point(453, 10)
        Button1.Name = "Button1"
        Button1.Size = New Size(75, 23)
        Button1.TabIndex = 9
        Button1.Text = "Button1"
        Button1.UseVisualStyleBackColor = True
        ' 
        ' Button2
        ' 
        Button2.Location = New Point(534, 10)
        Button2.Name = "Button2"
        Button2.Size = New Size(75, 23)
        Button2.TabIndex = 10
        Button2.Text = "Button2"
        Button2.UseVisualStyleBackColor = True
        ' 
        ' ComboBoxIP
        ' 
        ComboBoxIP.FormattingEnabled = True
        ComboBoxIP.Location = New Point(12, 13)
        ComboBoxIP.Name = "ComboBoxIP"
        ComboBoxIP.Size = New Size(129, 23)
        ComboBoxIP.TabIndex = 11
        ' 
        ' Form1
        ' 
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(664, 550)
        Controls.Add(ComboBoxIP)
        Controls.Add(Button2)
        Controls.Add(Button1)
        Controls.Add(SaveXML)
        Controls.Add(lblTotalFrames)
        Controls.Add(lblFps)
        Controls.Add(pbImage)
        Controls.Add(nudPacketSize)
        Controls.Add(btnStart)
        Name = "Form1"
        Text = "GigE Raw Capture"
        CType(nudPacketSize, ComponentModel.ISupportInitialize).EndInit()
        CType(pbImage, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub
    Friend WithEvents btnStart As Button
    Friend WithEvents nudPacketSize As NumericUpDown
    Friend WithEvents pbImage As PictureBox
    Friend WithEvents lblFps As Label
    Friend WithEvents lblTotalFrames As Label
    Friend WithEvents SaveXML As Button
    Friend WithEvents Button1 As Button
    Friend WithEvents Button2 As Button
    Friend WithEvents ComboBoxIP As ComboBox
End Class
