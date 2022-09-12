# GigeVision
Simple GigeVision implementation, GVSP, GVCP protocol implemented

## How to use:

### Get all cameras in the network

    var camera = new Camera();
    var listOfDevices = await camera.Gvcp.GetAllGigeDevicesInNetworkAsnyc().ConfigureAwait();    

### Read Register   
        
    var gvcp = new Gvcp();
    gvcp.CameraIP = "192.168.10.99";
    var reply = await gvcp.ReadRegisterAsync("0x0D04");
    
### Write Register
To write register you must take the control by Writing 0x02 to CCP register, you can write it manually or use the TakeControl Method from library.
If succeeded it will give you control for the time that is set in Heartbeat Register, send True as a parameter to keep the control status alive

       var gvcp = new Gvcp
       {
           CameraIp = "192.168.10.99"
       };
       if (await gvcp.TakeControl()) //Send True as a parameter to keep alive
       {
           var reply = await gvcp.WriteRegisterAsync("0x0D04", 1000);
       }
       else
       {
           throw new Exception("Camera is already in control");
       }
                       
### Read Memory

    var gvcp = new Gvcp();
    gvcp.CameraIP = "192.168.10.99";
    var reply = await gvcp.ReadMemoryAsync("0x0D04", 500); //To read 500 bytes from memory address 0x0D04

Above mentioned samples are very basic, and in the Gvcp class there are too many method overloads for each function. Link here: https://github.com/Touseefelahi/GigeVision/blob/master/GigeVision.Core/Services/Gvcp.cs




## Direct Use for Streaming, For a camera of ip  192.168.10.224 we need to initialize the camera objects as below:

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

There are some issues with the namespaces it is conflicting Services and Models, it will be fixed someday
