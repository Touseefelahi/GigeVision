# GigeVision
Simple GigeVision implementation, GVSP, GVCP protocol implemented

# How to use:
For a camera of ip  192.168.10.224 we need to initialize the camera objects as below:

    var camera = new Camera("192.168.10.224");
    
To Receive the stream raw bytes we need to subscribe to the frame ready event, Make sure to have the same interface as the camera, if you have multiple interfaces you need to define it explicitly when starting the stream 

    camera.FrameReady += FrameReady;

To start the stream we need to call the method StartStream as:

    //For multiple interfaces you can define the Rx IP as: (In this example 192.168.10.221 is the PC IP)    
    bool isStarted = await camera.StartStreamAsync("192.168.10.221").ConfigureAwait(false);

Once the frame is fully received the FrameReady event will be invoked

    private void FrameReady(object sender, byte[] e)
    {
      // e contains the raw data in bytes
    }
    
Simple WPF app example is here https://github.com/Touseefelahi/GigeVision/blob/master/GigeVisionLibrary.Test.Wpf/MainWindow.xaml.cs
