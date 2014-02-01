namespace BootLoader.Protocol.Interface
{
    public delegate void IncomingDataHandler(object sender, byte[] payload);

    public interface IProtocol
    {
        bool Open();
        bool Close();
        void SendData(byte[] dataBytes);
        event IncomingDataHandler IncomingData;
    }
}
