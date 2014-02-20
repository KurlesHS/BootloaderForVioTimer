using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using BootLoader.Impl;
using BootLoader.Interfaces;
using BootLoader.Protocol.Interface;

namespace BootLoader.Device
{
    public delegate void ErrorDataHandler(object sender, string description);

    public delegate void SessionFinishedHandler(object sender);

    public delegate void FlashingProcessHandler(object sender, int position);

    public delegate void PacketCountHandlet(object sended, long packetCount);

    public class TimerDeviceImpl
    {   
        private readonly IProtocol _protocol;
        private int _process;
        private readonly ITimer _timer;
        private Packet _currentPacket;
        private Stream _stream;
        private const string InternalErrorString = "Внутренняя ошибка, пинать Курлеса";
        private readonly LinkedList<ITimer> _listOfTimers = new LinkedList<ITimer>();

        public int PacketLenght { get; set; }
        public Packet FirstPacketStamp { get; set; }
        public Packet MiddlePacketStamp { get; set; }
        public Packet LastPacketStamp { get; set; }

        public TimerDeviceImpl(IProtocol protocol, ITimer timer = null) {
            _protocol = protocol;
            _currentPacket = null;
            if (timer == null) timer = new Timer();
            _timer = timer;
            _timer.Elapsed += TimerForTimeouts_Elapsed;
            _protocol.IncomingData += ProtocolOnIncomingData;
            _stream = null;
        }

        private void InitPacketStamps() {
            FirstPacketStamp = new Packet {
                BadResponse = "BAD",
                OkResponse = "GO",
                DataBytes = null,
                RetryCount = -1,
                WaitResponseTimeout = 3000,
                DelayBetweenPacket = 300
            };

            MiddlePacketStamp = new Packet {
                BadResponse = "BAD",
                OkResponse = "GO",
                DataBytes = null,
                RetryCount = -1,
                WaitResponseTimeout = 1000,
                DelayBetweenPacket = 300
            };

            LastPacketStamp = new Packet {
                BadResponse = "BAD",
                OkResponse = "GO",
                DataBytes = null,
                RetryCount = -1,
                WaitResponseTimeout = 10000,
                DelayBetweenPacket = 300
            };
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
                ResendCurrentPacketWithDelay(_currentPacket.DelayBetweenPacket);
            }
            if (payload.Length >= maxResponseLenght) {
                ResendCurrentPacketWithDelay(_currentPacket.DelayBetweenPacket);
            }
        }

        private void ResendCurrentPacketWithDelay(double delay) {
            var timer = (ITimer) _timer.Clone();
            _listOfTimers.AddLast(timer);
            timer.Elapsed += TimerOnElapsed;
            if (Math.Abs(delay) < .01) delay = 1.0;
            timer.Start(delay);
        }

        private void TimerOnElapsed(object sender, TimerEventArg timerEventArg) {
            var timer = (ITimer) sender;
            if (timer == null) return;
            ResendCurrentPacket();
            _listOfTimers.Remove(timer);
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
            _process += 1;
            ProcessHandler(this, _process);
            var p = IsNextPacketPresent() ? MiddlePacketStamp : LastPacketStamp;
            var packet = new Packet {
                WaitResponseTimeout = p.WaitResponseTimeout,
                DataBytes = binaryPacketData,
                OkResponse = p.OkResponse,
                BadResponse = p.BadResponse,
                RetryCount = p.RetryCount
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
            if (_currentPacket.RetryCount != -1) {
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

        private static string ReadXmlSettingFromFirmwareFile(Stream stream) {
            var sb = new StringBuilder();
            while (true)
            {
                var _byte = stream.ReadByte();
                if (_byte < 0) {
                    return "";
                }
                if (_byte == 0) break;
                sb.Append(Convert.ToChar(_byte));
            }
            return  sb.ToString();
        }

        public bool StartFlashing(Stream stream) {
            _process = 0;
            const string errorString = @"Ошибочный файл.";
            if (!_protocol.Open()) return false;
            InitPacketStamps();
            var xmlString = ReadXmlSettingFromFirmwareFile(stream);

            if (!GetPacketParametrsByXml(xmlString)) {
                ErrorHandler(this, errorString);
                _protocol.Close();
                return false;
            }
            var packetCount = stream.Length/PacketLenght;
            PacketHandler(this, packetCount);
            ProcessHandler(this, _process);

            var buf = new byte[PacketLenght];

            stream.Read(buf, 0, PacketLenght);


            var packet = new Packet {
                BadResponse = FirstPacketStamp.BadResponse,
                DataBytes = buf,
                OkResponse = FirstPacketStamp.OkResponse,
                RetryCount = FirstPacketStamp.RetryCount,
                WaitResponseTimeout = FirstPacketStamp.WaitResponseTimeout
            };
            
            _currentPacket = packet;
            SendCurrentPacket();
            return true;
        }

        private int GetBaudrateFromXml(string xmlString) {
            const int defaultVal = -1;
            var xmlDocumet = new XmlDocument();
            try {
                xmlDocumet.LoadXml(xmlString);
                var elementsByTagName = xmlDocumet.GetElementsByTagName("baudrate");
                if (elementsByTagName.Count != 1) return defaultVal;
                var element = elementsByTagName.Item(0);
                return element == null ? defaultVal : Convert.ToInt32(element.InnerText);
            } catch (Exception) {
                return defaultVal;
            }
        }

        public int GetBaudrateFromStream(Stream stream) {
            return GetBaudrateFromXml(ReadXmlSettingFromFirmwareFile(stream));
        }

        public bool GetPacketParametrsByXml(string xmlString) {
            var xmlDocumet = new XmlDocument();
            try {
                xmlDocumet.LoadXml(xmlString);
                var elementsByTagName = xmlDocumet.GetElementsByTagName("packet_length");
                if (elementsByTagName.Count != 1) return false;
                var element = elementsByTagName.Item(0);
                if (element == null) return false;
                PacketLenght = Convert.ToInt32(element.InnerText);
                var packets = new Dictionary<string, Packet> {
                    {"first_packet", FirstPacketStamp},
                    {"middle_packet", MiddlePacketStamp},
                    {"last_packet", LastPacketStamp}
                };
                foreach (var packetName in packets.Keys) {
                    var packet = packets[packetName];
                    elementsByTagName = xmlDocumet.GetElementsByTagName(packetName);
                    element = elementsByTagName.Item(0);
                    if (element == null) return false;
                    foreach (XmlNode subElement in element.ChildNodes) {
                        var value = subElement.InnerText.Trim();
                        switch (subElement.Name) {
                            case "ok_response":
                                packet.OkResponse = value;
                                break;
                            case "error_response":
                                packet.BadResponse = value;
                                break;
                            case "retry_count":
                                packet.RetryCount = Convert.ToInt32(value);
                                break;
                            case "timeout":
                                packet.WaitResponseTimeout = Convert.ToInt32(value);
                                break;
                            case "delay_between_resend_packet":
                                packet.DelayBetweenPacket = Convert.ToInt32(value);
                                break;
                        }
                    }
                }
            } catch (Exception) {
                return false;
            }
            return true;
        }

        private byte[] GetNextPacket() {
            if (_stream == null) return null;
            var buffer = new byte[PacketLenght];
            FillArrayWithValue(buffer, 0x00);
            var lenght = _stream.Read(buffer, 0, PacketLenght);
            return lenght == 0 ? null : buffer;
        }

        private bool IsNextPacketPresent() {
            var length = _stream.Length;
            return length != 0;
        }

        private static void FillArrayWithValue(byte[] array, byte value) {
            if (array == null) return;
            for (var idx = 0; idx < array.Length; ++idx) array[idx] = value;
        }

        public event ErrorDataHandler ErrorHandler;
        public event SessionFinishedHandler FinishedHandler;
        public event FlashingProcessHandler ProcessHandler;
        public event PacketCountHandlet PacketHandler;
    }
}