using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BootLoader.Impl;
using BootLoader.Interfaces;
using BootLoader.Protocol.Interface;

namespace BootLoader.Device
{
    public delegate void ErrorDataHandler(object sender, string description);

    public delegate void SessionFinishedHandler(object sender);

    public class TimerDeviceImpl
    {
        private readonly IProtocol _protocol;
        private readonly ITimer _timer;
        private Packet _currentPacket;
        private Stream _stream;
        private const int PacketLenght = 32;
        private const int MidlePacketTimeout = 1000;
        private const int LastPacketTimeout = 10000;
        private const int DelayBetweenResendPacket = 2000;
        private const int RetryCount = 3;
        private const string InternalErrorString = "Внутренняя ошибка, пинать Курлеса";


        public TimerDeviceImpl(IProtocol protocol, ITimer timer = null) {
            _protocol = protocol;
            _currentPacket = null;
            if (timer == null) timer = new Timer();
            _timer = timer;
            _timer.Elapsed += TimerForTimeouts_Elapsed;
            _protocol.IncomingData += ProtocolOnIncomingData;
            _stream = null;
        }

        private void ProtocolOnIncomingData(object sender, byte[] payload) {
            var line = Encoding.ASCII.GetString(payload);
            _timer.Stop();
            if (_currentPacket == null) {
                ErrorHandler(this, InternalErrorString);
                _protocol.Close();
                return;
            }
            var maxResponseLenght = Math.Max(_currentPacket.OkResponse.Length, _currentPacket.BadResponse.Length);
            if (line == _currentPacket.OkResponse) {
                SendNextPacket();
            } else if (line == _currentPacket.BadResponse) {
                ResendCurrentPacketWithDelay(DelayBetweenResendPacket);
            }
            if (payload.Length >= maxResponseLenght) {
                ResendCurrentPacketWithDelay(DelayBetweenResendPacket);
            }
        }

        private void ResendCurrentPacketWithDelay(double delay) {
            var timer = (ITimer) _timer.Clone();
            GC.KeepAlive(timer);
            timer.Elapsed += TimerOnElapsed;
            timer.Start(delay);
        }

        private void TimerOnElapsed(object sender, TimerEventArg timerEventArg) {
            var timer = (ITimer) sender;
            if (timer == null) return;
            ResendCurrentPacket();
            timer.Dispose();
        }

        private void ResendCurrentPacket() {
            if (_currentPacket == null) {
                ErrorHandler(this, InternalErrorString);
                _protocol.Close();
                return;
            }
            if (--_currentPacket.RetryCount <= 0) {
                ErrorHandler(this, "Закончились попытки переслать пакет данных");
                _protocol.Close();
                return;
            }
            SendCurrentPacket();
        }

        private void SendNextPacket() {
            var binaryPacketData = GetNextPacket();
            if (binaryPacketData == null) {
                _protocol.Close();
                FinishedHandler(this);
                return;
            }
            var isNextPacketPresent = IsNextPacketPresent();
            var packet = new Packet {
                WaitResponseTimeout = isNextPacketPresent ? MidlePacketTimeout : LastPacketTimeout,
                DataBytes = binaryPacketData,
                OkResponse = "GOOD",
                BadResponse = "BAD",
                RetryCount = RetryCount,
                Type = Packet.TypeOfPacket.DataPacket
            };
            _currentPacket = packet;
            SendCurrentPacket();
        }

        private void SendCurrentPacket() {
            _protocol.SendData(_currentPacket.DataBytes);
            _timer.Start(_currentPacket.WaitResponseTimeout);
        }

        private void TimerForTimeouts_Elapsed(object sender, TimerEventArg e) {
            const string errorString = "Таймаут при ожидании ответа от устройства";
            _timer.Stop();
            if (_currentPacket == null) {
                _protocol.Close();
                ErrorHandler(this, InternalErrorString);
                return;
            }
            if (_currentPacket.Type != Packet.TypeOfPacket.FirstPacket) {
                --_currentPacket.RetryCount;
                if (_currentPacket.RetryCount <= 0) {
                    _currentPacket = null;
                    _protocol.Close();
                    ErrorHandler(this, errorString);
                    return;
                }
                SendCurrentPacket();
            } else {
                SendCurrentPacket();
            }
        }

        public bool StartFlashing(Stream stream) {
            if (!_protocol.Open()) return false;
            _stream = stream;
            var packet = new Packet {
                BadResponse = "BAD",
                DataBytes = Encoding.ASCII.GetBytes("START"),
                OkResponse = "GO",
                RetryCount = -1,
                Type = Packet.TypeOfPacket.FirstPacket,
                WaitResponseTimeout = 3000
            };
            _currentPacket = packet;
            SendCurrentPacket();
            return true;
        }

        private static UInt16 ChkSum(IList<byte> array, int lenght) {
            UInt64 sum = 0;
            var i = 0;
            if (array.Count < lenght) {
                lenght = array.Count;
            }
            while (i < lenght) {
                sum += ((UInt64) array[i++] << 8) + array[i++];
            }
            sum = (sum >> 16) + (sum & 0xffff);
            sum += (sum >> 16);
            var answer = (UInt16) ~sum;
            return answer;
        }

        private byte[] GetNextPacket() {
            if (_stream == null) return null;
            var buffer = new byte[PacketLenght + 2];
            FillArrayWithValue(buffer, 0x00);
            var lenght = _stream.Read(buffer, 0, PacketLenght);
            var chk = ChkSum(buffer, PacketLenght);
            // now add crc to packet
            buffer[PacketLenght] = (byte) (chk & 0xff);
            buffer[PacketLenght + 1] = (byte) ((chk >> 8) & 0xff);
            return lenght == 0 ? null : buffer;
        }

        private bool IsNextPacketPresent() {
            return _stream.Length != 0;
        }

        private static void FillArrayWithValue(byte[] array, byte value) {
            if (array == null) return;
            for (var idx = 0; idx < array.Length; ++idx) array[idx] = value;
        }

        public bool ConnectAndStartSendingPackets() {
            if (!_protocol.Open()) return false;
            SendCurrentPacket();
            return true;
        }

        public event ErrorDataHandler ErrorHandler;
        public event SessionFinishedHandler FinishedHandler;
    }
}