namespace BootLoader.Device
{
    public class Packet
    {

        public byte[] DataBytes { get; set ; }
        public string OkResponse { get; set; }
        public string BadResponse { get; set; }
        public int WaitResponseTimeout { get; set; }
        public int RetryCount { get; set; }
        public int DelayBetweenPacket { get; set; }
        public Packet() {
            DataBytes = null;
        }
    }
}