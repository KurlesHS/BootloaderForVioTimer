using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using BootLoader.Protocol.Interface;

namespace BootLoader.Protocol.Implemantations
{
    class SerialProtocol : IProtocol
    {
        readonly SerialPort _serialPort = new SerialPort();
        private readonly List<Action<object, byte[]>> _dataReceiverListeners = new List<Action<object, byte[]>>();
        private readonly byte[] _readBuffer = new byte[0x400];

        public SerialProtocol(string serialPortName, int baudrate) {
            _serialPort.BaudRate = baudrate;
            _serialPort.PortName = serialPortName;
            _serialPort.DataReceived += SerialPortOnDataReceived;
            _serialPort.DataBits = 8;
            _serialPort.Handshake = Handshake.None;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;
        }

        private void SerialPortOnDataReceived(object sender, SerialDataReceivedEventArgs serialDataReceivedEventArgs) {

            
            var readedBytes = _serialPort.Read(_readBuffer, 0, _readBuffer.Length);
            
            var outBytes = new byte[readedBytes];
            Array.Copy(_readBuffer, 0, outBytes, 0, readedBytes);
            IncomingData(this, outBytes);
        }

        public bool Open() {
            try {
                _serialPort.Open();
            }
            catch (Exception) {
                return false;
            }
            return true;
        }

        public bool Close() {
            try {
                _serialPort.Close();
            }
            catch (IOException) {
                return false;
            }
            return true;
        }

        public void SendData(byte[] dataBytes) {
            _serialPort.Write(dataBytes, 0, dataBytes.Length);
        }

        public event IncomingDataHandler IncomingData;
    }
}
