using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BootLoader.Protocol.Interface
{
    public interface IProtocol
    {
        bool Open();
        bool Close();
        void SendData(byte[] dataBytes);
        void AddReceiveDataListener(Action<object, byte[]> listener);
    }
}
