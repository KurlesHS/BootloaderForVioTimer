using System.Text;
using BootLoader.Protocol.Interface;

namespace BootLoaderUnitTestProject
{
    public class FakeProtocol : IProtocol
    {
        private bool _isOpened;
        private bool _firstPacket;

        public int PacketCount { get; private set; }

        public FakeProtocol() {
            _isOpened = false;
            _firstPacket = true;
            PacketCount = 0;
        }

        public bool Open() {
            _isOpened = true;
            return true;
        }

        public bool Close() {
            _isOpened = false;
            return false;
        }

        public void SendData(byte[] dataBytes) {
            if (!_isOpened) return;
            PacketCount += 1;
        }

        public void Process() {
            if (_firstPacket) {
                IncomingData(this, Encoding.ASCII.GetBytes("GO"));
                _firstPacket = false;
            } else {
                IncomingData(this, Encoding.ASCII.GetBytes("GOOD"));
            }
        }

        public event IncomingDataHandler IncomingData;
    }
}