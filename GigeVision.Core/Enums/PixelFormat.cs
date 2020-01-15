namespace GigeVision.Core.Enums
{
    /// <summary>
    /// Pixel format of GVSP stream
    /// </summary>
    public enum PixelFormat
    {
        /// <summary>
        /// GVSP_PIX_MONO8
        /// </summary>
        Mono8 = 0x01080001,

        /// <summary>
        /// GVSP_PIX_MONO8_SIGNED
        /// </summary>
        Mono8Signed = 0x01080002,

        /// <summary>
        /// GVSP_PIX_BAYGR8
        /// </summary>
        BayerGR8 = 0x01080008,

        /// <summary>
        /// GVSP_PIX_BAYRG8
        /// </summary>
        BayerRG8 = 0x01080009,

        /// <summary>
        /// GVSP_PIX_BAYGB8
        /// </summary>
        BayerGB8 = 0x0108000A,

        /// <summary>
        /// GVSP_PIX_BAYBG8
        /// </summary>
        BayerBG8 = 0x0108000B,

        /// <summary>
        /// GVSP_PIX_MONO10_PACKED
        /// </summary>
        Mono10Packed = 0x010C0004,

        /// <summary>
        /// GVSP_PIX_MONO12_PACKED
        /// </summary>
        Mono12Packed = 0x010C0006,

        /// <summary>
        /// GVSP_PIX_BAYGR10_PACKED
        /// </summary>
        BayerGR10Packed = 0x010C0026,

        /// <summary>
        /// GVSP_PIX_BAYRG10_PACKED
        /// </summary>
        BayerRG10Packed = 0x010C0027,

        /// <summary>
        /// GVSP_PIX_BAYGB10_PACKED
        /// </summary>
        BayerGB10Packed = 0x010C0028,

        /// <summary>
        /// GVSP_PIX_BAYBG10_PACKED
        /// </summary>
        BayerBG10Packed = 0x010C0029,

        /// <summary>
        /// GVSP_PIX_BAYGR12_PACKED
        /// </summary>
        BayerGR12Packed = 0x010C002A,

        /// <summary>
        /// GVSP_PIX_BAYRG12_PACKED
        /// </summary>
        BayerRG12Packed = 0x010C002B,

        /// <summary>
        /// GVSP_PIX_BAYGB12_PACKED
        /// </summary>
        BayerGB12Packed = 0x010C002C,

        /// <summary>
        /// GVSP_PIX_BAYBG12_PACKED
        /// </summary>
        BayerBG12Packed = 0x010C002D,

        /// <summary>
        /// GVSP_PIX_MONO10
        /// </summary>
        Mono10 = 0x01100003,

        /// <summary>
        /// GVSP_PIX_MONO12
        /// </summary>
        Mono12 = 0x01100005,

        /// <summary>
        /// GVSP_PIX_MONO16
        /// </summary>
        Mono16 = 0x01100007,

        /// <summary>
        /// GVSP_PIX_BAYGR10
        /// </summary>
        BayerGR10 = 0x0110000C,

        /// <summary>
        /// GVSP_PIX_BAYRG10
        /// </summary>
        BayerRG10 = 0x0110000D,

        /// <summary>
        /// GVSP_PIX_BAYGB10
        /// </summary>
        BayerGB10 = 0x0110000E,

        /// <summary>
        /// GVSP_PIX_BAYBG10
        /// </summary>
        BayerBG10 = 0x0110000F,

        /// <summary>
        /// GVSP_PIX_BAYGR12
        /// </summary>
        BayerGR12 = 0x01100010,

        /// <summary>
        /// GVSP_PIX_BAYRG12
        /// </summary>
        BayerRG12 = 0x01100011,

        /// <summary>
        /// GVSP_PIX_BAYGB12
        /// </summary>
        BayerGB12 = 0x01100012,

        /// <summary>
        /// GVSP_PIX_BAYBG12
        /// </summary>
        BayerBG12 = 0x01100013,

        /// <summary>
        /// GVSP_PIX_MONO14
        /// </summary>
        Mono14 = 0x01100025,

        /// <summary>
        /// GVSP_PIX_BAYGR16
        /// </summary>
        BayerGR16 = 0x0110002E,

        /// <summary>
        /// GVSP_PIX_BAYRG16
        /// </summary>
        BayerRG16 = 0x0110002F,

        /// <summary>
        /// GVSP_PIX_BAYGB16
        /// </summary>
        BayerGB16 = 0x01100030,

        /// <summary>
        /// GVSP_PIX_BAYBG16
        /// </summary>
        BayerBG16 = 0x01100031,

        /// <summary>
        /// GVSP_PIX_YUV411_PACKED
        /// </summary>
        YUV411Packed = 0x020C001E,

        /// <summary>
        /// GVSP_PIX_YUV422_PACKED
        /// </summary>
        YUV422Packed = 0x0210001F,

        /// <summary>
        /// GVSP_PIX_YUV422_YUYV_PACKED
        /// </summary>
        YUYVPacked = 0x02100032,

        /// <summary>
        /// GVSP_PIX_RGB565_PACKED
        /// </summary>
        RGB565Packed = 0x02100035,

        /// <summary>
        /// GVSP_PIX_BGR565_PACKED
        /// </summary>
        BGR565Packed = 0x02100036,

        /// <summary>
        /// GVSP_PIX_RGB8_PACKED
        /// </summary>
        RGB8Packed = 0x02180014,

        /// <summary>
        /// GVSP_PIX_BGR8_PACKED
        /// </summary>
        BGR8Packed = 0x02180015,

        /// <summary>
        /// GVSP_PIX_YUV444_PACKED
        /// </summary>
        YUV444Packed = 0x02180020,

        /// <summary>
        /// GVSP_PIX_RGB8_PLANAR
        /// </summary>
        RGB8Planar = 0x02180021,

        /// <summary>
        /// GVSP_PIX_RGBA8_PACKED
        /// </summary>
        RGBA8Packed = 0x02200016,

        /// <summary>
        /// GVSP_PIX_BGRA8_PACKED
        /// </summary>
        BGRA8Packed = 0x02200017,

        /// <summary>
        /// GVSP_PIX_RGB10V1_PACKED
        /// </summary>
        RGB10V1Packed = 0x0220001C,

        /// <summary>
        /// GVSP_PIX_RGB10V2_PACKED
        /// </summary>
        RGB10V2Packed = 0x0220001D,

        /// <summary>
        /// GVSP_PIX_RGB12V1_PACKED
        /// </summary>
        RGB12V1Packed = 0x02240034,

        /// <summary>
        /// GVSP_PIX_RGB10_PACKED
        /// </summary>
        RGB10Packed = 0x02300018,

        /// <summary>
        /// GVSP_PIX_BGR10_PACKED
        /// </summary>
        BGR10Packed = 0x02300019,

        /// <summary>
        /// GVSP_PIX_RGB12_PACKED
        /// </summary>
        RGB12Packed = 0x0230001A,

        /// <summary>
        /// GVSP_PIX_BGR12_PACKED
        /// </summary>
        BGR12Packed = 0x0230001B,

        /// <summary>
        /// GVSP_PIX_RGB10_PLANAR
        /// </summary>
        RGB10Planar = 0x02300022,

        /// <summary>
        /// GVSP_PIX_RGB12_PLANAR
        /// </summary>
        RGB12Planar = 0x02300023,

        /// <summary>
        /// GVSP_PIX_RGB16_PLANAR
        /// </summary>
        RGB16Planar = 0x02300024,

        /// <summary>
        /// GVSP_PIX_RGB16_PACKED
        /// </summary>
        RGB16Packed = 0x02300033,
    }
}