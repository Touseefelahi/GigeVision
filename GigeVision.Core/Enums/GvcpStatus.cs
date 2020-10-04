namespace GigeVision.Core.Enums
{
    /// <summary>
    /// GVCP command status : (As it is from GigE vision 1.2 protocol)
    /// </summary>
    public enum GvcpStatus
    {
        /// <summary>
        /// Command executed successfully
        /// </summary>
        GEV_STATUS_SUCCESS = 0x0000,

        /// <summary>
        /// Only applies to packet being resent. This flag is preferred over the GEV_STATUS_SUCCESS
        /// when the GVSP transmitter sends a resent packet. This can be used by a GVSP receiver to
        /// better monitor packet resend.
        /// </summary>
        GEV_STATUS_PACKET_RESEND = 0x0100,

        /// <summary>
        /// Command is not supported by the device
        /// </summary>
        GEV_STATUS_NOT_IMPLEMENTED = 0x8001,

        /// <summary>
        /// At least one parameter provided in the command is invalid (or out of range) for the device
        /// </summary>
        GEV_STATUS_INVALID_PARAMETER = 0x8002,

        /// <summary>
        /// An attempt was made to access a non existent address space location.
        /// </summary>
        GEV_STATUS_INVALID_ADDRESS = 0x8003,

        /// <summary>
        /// The addressed register cannot be written to
        /// </summary>
        GEV_STATUS_WRITE_PROTECT = 0x8004,

        /// <summary>
        /// A badly aligned address offset or data size was specified.
        /// </summary>
        GEV_STATUS_BAD_ALIGNMENT = 0x8005,

        /// <summary>
        /// An attempt was made to access an address location which is currently/momentary not
        /// accessible. This depends on the current state of the device, in particular the current
        /// privilege of the application.
        /// </summary>
        GEV_STATUS_ACCESS_DENIED = 0x8006,

        /// <summary>
        /// A required resource to service the request is not currently available. The request may
        /// be retried at a later time.
        /// </summary>
        GEV_STATUS_BUSY = 0x8007,

        /// <summary>
        /// An internal problem in the device implementation occurred while processing the request.
        /// Optionally the device provides a mechanism for looking up a detailed description of the
        /// problem. (Log files, Event log, ‘Get last error’ mechanics). This error is intended to
        /// report problems from underlying services (operating system, 3rd party library) in the
        /// device to the client side without translating every possible error code into a GigE
        /// Vision equivalent.
        /// </summary>
        GEV_STATUS_LOCAL_PROBLEM = 0x8008,

        /// <summary>
        /// Message mismatch (request and acknowledge do not match)
        /// </summary>
        GEV_STATUS_MSG_MISMATCH = 0x8009,

        /// <summary>
        /// This version of the GVCP protocol is not supported
        /// </summary>
        GEV_STATUS_INVALID_PROTOCOL = 0x800A,

        /// <summary>
        /// Timeout, no message received
        /// </summary>
        GEV_STATUS_NO_MSG = 0x800B,

        /// <summary>
        /// The requested packet is not available anymore.
        /// </summary>
        GEV_STATUS_PACKET_UNAVAILABLE = 0x800C,

        /// <summary>
        /// Internal memory of GVSP transmitter overrun (typically for image acquisition)
        /// </summary>
        GEV_STATUS_DATA_OVERRUN = 0x800D,

        /// <summary>
        /// The message header is not valid. Some of its fields do not match the specification.
        /// </summary>
        GEV_STATUS_INVALID_HEADER = 0x800E,

        /// <summary>
        /// The device current configuration does not allow the request to be executed due to
        /// parameters consistency issues.
        /// </summary>
        GEV_STATUS_WRONG_CONFIG = 0x800F,

        /// <summary>
        /// The requested packet has not yet been acquired. Can be used for linescan cameras device
        /// when line trigger rate is slower than application timeout.
        /// </summary>
        GEV_STATUS_PACKET_NOT_YET_AVAILABLE = 0x8010,

        /// <summary>
        /// The requested packet and all previous ones are not available anymore and have been
        /// discarded from the GVSP transmitter memory. An application associated to a GVSP receiver
        /// should not request retransmission of these packets again.
        /// </summary>
        GEV_STATUS_PACKET_AND_PREV_REMOVED_FROM_MEMORY = 0x8011,

        /// <summary>
        /// The requested packet is not available anymore and has been discarded from the GVSP
        /// transmitter memory. However, applications associated to GVSP receivers can still
        /// continue using their internal resend algorithm on earlier packets that are still
        /// outstanding. This does not necessarily indicate than any previous data is actually
        /// available, just that the application should not just assume everything earlier is no
        /// longer available.
        /// </summary>
        GEV_STATUS_PACKET_REMOVED_FROM_MEMORY = 0x8012,

        /// <summary>
        ///Generic error. Try to avoid and use a more descriptive status code from list above.
        /// </summary>
        GEV_STATUS_ERROR = 0x8FFF,
    }
}