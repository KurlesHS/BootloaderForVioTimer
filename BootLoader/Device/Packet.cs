namespace BootLoader.Device
{
    public class Packet
    {
        public enum TypeOfPacket
        {
            FirstPacket,
            DataPacket
        }

        public byte[] DataBytes { get; set ; }
        public TypeOfPacket Type { get; set; }
        public string OkResponse { get; set; }
        public string BadResponse { get; set; }
        public int WaitResponseTimeout { get; set; }
        public int RetryCount { get; set; }
        public Packet() {
            DataBytes = null;
            Type = TypeOfPacket.FirstPacket;
        }
    }
}