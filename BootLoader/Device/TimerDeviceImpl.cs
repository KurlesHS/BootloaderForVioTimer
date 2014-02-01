using System;
using System.Collections.Generic;
using System.IO;
using BootLoader.Impl;
using BootLoader.Interfaces;
using BootLoader.Protocol.Interface;

namespace BootLoader.Device
{
    public delegate void ErrorDataHandler(object sender, string description);

    public class TimerDeviceImpl
    {
        private readonly IProtocol _protocol;
        private readonly ITimer _timer;
        private readonly Queue<Packet> _packetsQueue;
        private Packet _currentPacket;
        private int _packetLenght;

        public TimerDeviceImpl(IProtocol protocol, ITimer timer = null) {
            _protocol = protocol;
            _currentPacket = null;
            if (timer == null) {
                timer = new Timer();
            }
            _timer = timer;
            _timer.Elapsed += TimerForTimeouts_Elapsed;
            _packetsQueue = new Queue<Packet>();
            _protocol.IncomingData += ProtocolOnIncomingData;
            _packetLenght = 0;
        }

        private void ProtocolOnIncomingData(object sender, byte[] payload) {
            var line = System.Text.Encoding.ASCII.GetString(payload);
            _timer.Stop();
            if (_currentPacket == null) {
                ErrorHandler(this, "Внутренняя ошибка");
                _protocol.Close();
                return;
            }
            if (_currentPacket.Type == Packet.TypeOfPacket.FirstPacket) {
                if (line.Length == 5 && line.Substring(0, 2) == "GO") {
                    try {
                        _packetLenght = Convert.ToInt32(line.Substring(2, 3));
                    }
                    catch (FormatException e) {
                        ErrorHandler(this, "Неверный стартовый ответ");
                    }
                }
            }
        }

        private void TimerForTimeouts_Elapsed(object sender, TimerEventArg e) {
            const string errorString = "Таймаут при ожидании ответа от устройства";
            _timer.Stop();
            if (_currentPacket == null) {
                ErrorHandler(this, errorString);
                _protocol.Close();
                return;
            }
            if (_currentPacket.Type != Packet.TypeOfPacket.FirstPacket) {
                --_currentPacket.RetryCount;
                if (_currentPacket.RetryCount <= 0) {
                    ErrorHandler(this, errorString);
                    _currentPacket = null;
                    _protocol.Close();
                    return;
                }
                TryToSendCurrentPacket();
            } else {
                TryToSendCurrentPacket();
            }
        }

        private void AddPacket(Packet packet) {
            _packetsQueue.Enqueue(packet);
        }

        public bool StartFlashing(Stream stream) {
            if (!_protocol.Open()) return false;

            return true;
        }
        public bool ConnectAndStartSendingPackets() {
            if (!_protocol.Open()) return false;
            _currentPacket = _packetsQueue.Dequeue();
            TryToSendCurrentPacket();
            return true;
        }

        private void TryToSendCurrentPacket() {
            _protocol.SendData(_currentPacket.DataBytes);
            _timer.Start(_currentPacket.WaitResponseTimeout);
        }

        private event ErrorDataHandler ErrorHandler;
    }
}