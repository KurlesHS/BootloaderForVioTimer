﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Win32;


namespace PrepareFirmware
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private const string FileNotSelectedText = @"Файл не выбран";
        private const string SettingsFile = "prepare_firmware_settings.xml";
        private const string RootTag = "PrepareFirmwareSettings";
        private const string BaudrateTag = "Baudrate";
        private const string StartAddressTag = "StartAddress";
        private const string PacketLenghtTag = "PacketLenght";
        private const string FirmwareFileTag = "FirmwareFile2";
        private const string CryptoFileTag = "CryptoFile2";
        private const string StartPacketTag = "StartPacket";
        private const string MiddlePacketTag = "MiddlePacket";
        private const string LastPacketTag = "LastPacket";
        private const string DeviceCodeTag = "DeviceCode";
        private const string RetryCountTag = "RetryCount";
        private const string OkResponceTag = "OkResponce";
        private const string BadResponceTag = "BadResponce";
        private const string TimeoutBetweenPacketTag = "TimeoutBetweenPacket";
        private const string TimeoutTag = "Timetout";
        private string _cryptFileName;
        private string _firmwareFileName;
        private readonly VioCrypt _vioCrypt;

        public MainWindow() {
            InitializeComponent();
            int[] baudRate = {4800, 9600, 19200, 38400, 57600, 115200, 230400};
            BaudrateComboBox.ItemsSource = baudRate;
            BaudrateComboBox.SelectedValue = baudRate[2];
            FirmwareFilenameTextBox.Text = FileNotSelectedText;
            InitializeFormValues();
            ReadSettings();
            Closed += OnClosed;
            _vioCrypt = new VioCrypt();
        }


        private void InitializeFormValues() {
            StartAddressTextBox.MinValue = 0x00;
            StartAddressTextBox.MaxValue = Int32.MaxValue;
            PacketLenghtTextBox.MinValue = 2;
            PacketLenghtTextBox.MaxValue = 256;

            StartPacketDelayBetweenWrongPacketNumericTextBox.MinValue = 0;
            StartPacketDelayBetweenWrongPacketNumericTextBox.MaxValue = 60000;
            MiddlePacketDelayBetweenWrongPacketNumericTextBox.MinValue = 0;
            MiddlePacketDelayBetweenWrongPacketNumericTextBox.MaxValue = 60000;
            LastPacketDelayBetweenWrongPacketNumericTextBox.MinValue = 0;
            LastPacketDelayBetweenWrongPacketNumericTextBox.MaxValue = 60000;

            StartPacketTimeoutTextBox.MinValue = 0;
            StartPacketTimeoutTextBox.MaxValue = 60000;
            MiddlePacketTimeoutTextBox.MinValue = 0;
            MiddlePacketTimeoutTextBox.MaxValue = 60000;
            LastPacketTimeoutTextBox.MinValue = 0;
            LastPacketTimeoutTextBox.MaxValue = 60000;

            StartPacketTryCountNumericTextBox.MinValue = -1;
            StartPacketTryCountNumericTextBox.MaxValue = 60000;
            MiddlePacketTryCountNumericTextBox.MinValue = -1;
            MiddlePacketTryCountNumericTextBox.MaxValue = 60000;
            LastPacketTryCountNumericTextBox.MinValue = -1;
            LastPacketTryCountNumericTextBox.MaxValue = 60000;
        }

        private void OnClosed(object sender, EventArgs eventArgs) {
            SaveSettings();
        }

        private void ReadSettings() {
            var baudrate = "19200";
            var startAddress = "8388608";
            var packetLenght = "32";
            var firmwareFile = "";
            var cryptoFile = "";
            var firstPacketDeviceCode = "dev_code";
            var firstPacketRetryCount = "-1";
            var firstPacketOkResponce = "GO";
            var firstPacketBadResponce = "BAD";
            var firstPacketTimeout = "3";
            var firstPacketTimeoutBetweenPacket = "3";
            var middlePacketRetryCount = "3";
            var middlePacketOkResponce = "GOOD";
            var middlePacketBadResponce = "BAD";
            var middlePacketTimeout = "3";
            var middlePacketTimeoutBetweenPacket = "3";
            var lastPacketRetryCount = "3";
            var lastPacketOkResponce = "GOOD";
            var lastPacketBadResponce = "BAD";
            var lastPacketTimeout = "3";
            var lastPacketTimeoutBetweenPacket = "3";

            try {
                var mainDocument = XDocument.Load(SettingsFile);
                var document = mainDocument.Element(RootTag);
                if (document != null) {
                    var element = document.Element(BaudrateTag);
                    if (element != null)
                        baudrate = element.Value;
                    element = document.Element(StartAddressTag);
                    if (element != null)
                        startAddress = element.Value;
                    element = document.Element(PacketLenghtTag);
                    if (element != null)
                        packetLenght = element.Value;
                    element = document.Element(FirmwareFileTag);
                    if (element != null)
                        firmwareFile = element.Value;
                    element = document.Element(CryptoFileTag);
                    if (element != null)
                        cryptoFile = element.Value;
                    var childElement = document.Element(StartPacketTag);
                    if (childElement != null) {
                        element = childElement.Element(DeviceCodeTag);
                        if (element != null)
                            firstPacketDeviceCode = element.Value;
                        GetPacketConfig(childElement, ref firstPacketRetryCount, ref firstPacketOkResponce,
                            ref firstPacketBadResponce, ref firstPacketTimeout, ref firstPacketTimeoutBetweenPacket);
                    }
                    childElement = document.Element(MiddlePacketTag);
                    GetPacketConfig(childElement, ref middlePacketRetryCount, ref middlePacketOkResponce,
                        ref middlePacketBadResponce, ref middlePacketTimeout, ref middlePacketTimeoutBetweenPacket);
                    childElement = document.Element(LastPacketTag);
                    GetPacketConfig(childElement, ref lastPacketRetryCount, ref lastPacketOkResponce,
                        ref lastPacketBadResponce, ref lastPacketTimeout, ref lastPacketTimeoutBetweenPacket);
                }
            } catch (Exception) {
                baudrate = "19200";
            }
            BaudrateComboBox.SelectedItem = 19200;
            foreach (var item in BaudrateComboBox.Items.Cast<object>().Where(item => item.ToString() == baudrate)) {
                BaudrateComboBox.SelectedItem = item;
                break;
            }
            SetIntegerValue(StartAddressTextBox, startAddress, 0x800000);
            SetIntegerValue(PacketLenghtTextBox, packetLenght, 32);
            _firmwareFileName = firmwareFile.Trim() == "" ? FileNotSelectedText : firmwareFile.Trim();
            _cryptFileName = cryptoFile.Trim() == "" ? FileNotSelectedText : cryptoFile.Trim();

            FirmwareFilenameTextBox.Text = _firmwareFileName;
            CryptoFilenameTextBox.Text = _cryptFileName;

            DeviceCodeTextBox.Text = firstPacketDeviceCode;

            StartPacketOkResponseTextBox.Text = firstPacketOkResponce;
            StartPacketBadResponseTextBox.Text = firstPacketBadResponce;
            SetIntegerValue(StartPacketTryCountNumericTextBox, firstPacketRetryCount, -1);
            SetIntegerValue(StartPacketDelayBetweenWrongPacketNumericTextBox, firstPacketTimeoutBetweenPacket, 3);
            SetIntegerValue(StartPacketTimeoutTextBox, firstPacketTimeout, 3);

            MiddlePacketOkResponceTextBox.Text = middlePacketOkResponce;
            MiddlePacketBadResponceTextBox.Text = middlePacketBadResponce;
            SetIntegerValue(MiddlePacketTryCountNumericTextBox, middlePacketRetryCount, 3);
            SetIntegerValue(MiddlePacketDelayBetweenWrongPacketNumericTextBox, middlePacketTimeoutBetweenPacket, 3);
            SetIntegerValue(MiddlePacketTimeoutTextBox, middlePacketTimeout, 3);

            LastPacketOkResponceTextBox.Text = lastPacketOkResponce;
            LastPacketBadResponceTextBox.Text = lastPacketBadResponce;
            SetIntegerValue(LastPacketTryCountNumericTextBox, lastPacketRetryCount, 3);
            SetIntegerValue(LastPacketDelayBetweenWrongPacketNumericTextBox, lastPacketTimeoutBetweenPacket, 3);
            SetIntegerValue(LastPacketTimeoutTextBox, lastPacketTimeout, 3);
        }

        private static void SetIntegerValue(NumericTextBox textBox, string stringValue, int defaultValue) {
            try {
                textBox.Value = Convert.ToInt32(stringValue);
            } catch (Exception) {
                textBox.Value = defaultValue;
            }
        }

        private static void GetPacketConfig(XContainer childElement, ref string retryCount, ref string okResponce,
            ref string badResponce, ref string timeout, ref string timeoutBetweenPacket) {
            if (childElement == null) return;
            var element = childElement.Element(RetryCountTag);
            if (element != null)
                retryCount = element.Value;
            element = childElement.Element(OkResponceTag);
            if (element != null)
                okResponce = element.Value;
            element = childElement.Element(BadResponceTag);
            if (element != null)
                badResponce = element.Value;
            element = childElement.Element(TimeoutTag);
            if (element != null)
                timeout = element.Value;
            element = childElement.Element(TimeoutBetweenPacketTag);
            if (element != null)
                timeoutBetweenPacket = element.Value;
        }

        private void SaveSettings() {
            var settings = new XmlWriterSettings {
                Indent = true,
                IndentChars = (" "),
                Encoding = new UTF8Encoding(false)
            };
            using (var writer = XmlWriter.Create(SettingsFile, settings)) {
                writer.WriteStartElement(RootTag);

                writer.WriteElementString(BaudrateTag, BaudrateComboBox.SelectedItem.ToString());
                writer.WriteElementString(StartAddressTag, Convert.ToString(StartAddressTextBox.Value));
                writer.WriteElementString(PacketLenghtTag, Convert.ToString(PacketLenghtTextBox.Value));
                writer.WriteElementString(FirmwareFileTag, FirmwareFilenameTextBox.Text);
                writer.WriteElementString(CryptoFileTag, CryptoFilenameTextBox.Text);

                writer.WriteStartElement(StartPacketTag);
                writer.WriteElementString(DeviceCodeTag, DeviceCodeTextBox.Text);
                writer.WriteElementString(OkResponceTag, StartPacketOkResponseTextBox.Text);
                writer.WriteElementString(BadResponceTag, StartPacketBadResponseTextBox.Text);
                writer.WriteElementString(RetryCountTag, Convert.ToString(StartPacketTryCountNumericTextBox.Value));
                writer.WriteElementString(TimeoutTag, Convert.ToString(StartPacketTimeoutTextBox.Value));
                writer.WriteElementString(TimeoutBetweenPacketTag,
                    Convert.ToString(StartPacketDelayBetweenWrongPacketNumericTextBox.Value));
                writer.WriteEndElement();

                writer.WriteStartElement(MiddlePacketTag);
                writer.WriteElementString(OkResponceTag, MiddlePacketOkResponceTextBox.Text);
                writer.WriteElementString(BadResponceTag, MiddlePacketBadResponceTextBox.Text);
                writer.WriteElementString(RetryCountTag, Convert.ToString(MiddlePacketTryCountNumericTextBox.Value));
                writer.WriteElementString(TimeoutTag, Convert.ToString(MiddlePacketTimeoutTextBox.Value));
                writer.WriteElementString(TimeoutBetweenPacketTag,
                    Convert.ToString(MiddlePacketDelayBetweenWrongPacketNumericTextBox.Value));
                writer.WriteEndElement();

                writer.WriteStartElement(LastPacketTag);
                writer.WriteElementString(OkResponceTag, LastPacketOkResponceTextBox.Text);
                writer.WriteElementString(BadResponceTag, LastPacketBadResponceTextBox.Text);
                writer.WriteElementString(RetryCountTag, Convert.ToString(LastPacketTryCountNumericTextBox.Value));
                writer.WriteElementString(TimeoutTag, Convert.ToString(LastPacketTimeoutTextBox.Value));
                writer.WriteElementString(TimeoutBetweenPacketTag,
                    Convert.ToString(LastPacketDelayBetweenWrongPacketNumericTextBox.Value));
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }

        private void ButtonSelectFirmwareFile_Click(object sender, RoutedEventArgs e) {
            var filename = SelectFile(".bin", "Firmware files (*.bin)|*.bin|All files|*.*");
            if (filename == null) return;
            FirmwareFilenameTextBox.Text = filename;
            _firmwareFileName = filename;
        }

        private void ButtonSelectCryptoFile_Click(object sender, RoutedEventArgs e) {
            var filename = SelectFile(".crt", "Crypto files (*.bin)|*.bin|All files|*.*");
            if (filename == null) return;
            CryptoFilenameTextBox.Text = filename;
            _cryptFileName = filename;
        }

        private static string SelectFile(string defaultExt, string filter) {
            // fucking piece of shit change current directry!
            var dlg = new OpenFileDialog {
                DefaultExt = defaultExt,
                Filter = filter,
                RestoreDirectory = true
            };
            var result = dlg.ShowDialog();
            return result != true ? null : dlg.FileName.Clone().ToString();
        }

        private void ButtonMakeEveryoneHappy_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
            try {
                using (var stream = new BinaryReader(new FileStream(CryptoFilenameTextBox.Text, FileMode.Open))) {
                    const int chunk = 0x100;
                    var cryptTable = new byte[chunk];
                    var bytesReaded = stream.Read(cryptTable, 0, chunk);
                    if (bytesReaded != chunk || stream.BaseStream.Length != chunk) {
                        MessageBox.Show(this, "Неверный крипто файл", "Ошибка");
                        return;
                    }
                    _vioCrypt.CryptTable = cryptTable;
                    if (_vioCrypt.CryptTable == null) {
                        MessageBox.Show(this, "Неверный крипто файл (ecть повторяющиеся байты)", "Ошибка");
                        return;
                    }
                }
            } catch (Exception) {
                MessageBox.Show(this, "Ошибка открытия крипто файла", "Ошибка!");
                return;
            }
            var dlg = new SaveFileDialog {
                RestoreDirectory = true,
                Filter = "Файлы для прошивки (*.tmr)|*.tmr"
            };
            if (dlg.ShowDialog() != true) {
                return;
            }

            byte[] firmwareBuffer;
            try {
                using (var reader = new BinaryReader(new FileStream(FirmwareFilenameTextBox.Text, FileMode.Open))) {
                    var fileLen = reader.BaseStream.Length;
                    
                    firmwareBuffer = fileLen%2 == 0 ? new byte[fileLen] : new byte[fileLen + 1];
                    firmwareBuffer[firmwareBuffer.Length - 1] = 0x00;
                    var readed = reader.Read(firmwareBuffer, 0, (int)fileLen);
                    if (readed != fileLen) {
                        MessageBox.Show(this, "Ошибка чтения входного файла!", "Ошибка!");
                        return;
                    }
                }
            } catch (Exception) {
                MessageBox.Show(this, "Ошибка чтения входного файла!", "Ошибка!");
                return;
            }
            var filename = dlg.FileName;
            try {
                var fileStream = new FileStream(filename, FileMode.Create);
                var xmlSettings = new XmlWriterSettings {
                    Indent = true,
                    IndentChars = (" "),
                    Encoding = new UTF8Encoding(false)
                };
                using (var writer = XmlWriter.Create(fileStream, xmlSettings)) {
                    // Write XML data.
                    const string rootTag = "vio_timer";
                    const string packetLenghtTag = "packet_length";
                    const string startPacketTag = "first_packet";
                    const string middlePacketTag = "middle_packet";
                    const string lastPacketTag = "last_packet";
                    const string baudrateTag = "baudrate";
                    const string okResponceTag = "ok_response";
                    const string badResponceTag = "error_response";
                    const string retryCountTag = "retry_count";
                    const string timeoutTag = "timeout";
                    const string delayBetweenResendPacket = "delay_between_resend_packet";


                    writer.WriteStartElement(rootTag);

                    writer.WriteElementString(baudrateTag, BaudrateComboBox.SelectedItem.ToString());
                    writer.WriteElementString(packetLenghtTag, Convert.ToString(PacketLenghtTextBox.Value + 2));

                    writer.WriteStartElement(startPacketTag);
                    writer.WriteElementString(okResponceTag, StartPacketOkResponseTextBox.Text);
                    writer.WriteElementString(badResponceTag, StartPacketBadResponseTextBox.Text);
                    writer.WriteElementString(retryCountTag, Convert.ToString(StartPacketTryCountNumericTextBox.Value));
                    writer.WriteElementString(timeoutTag, Convert.ToString(StartPacketTimeoutTextBox.Value));
                    writer.WriteElementString(delayBetweenResendPacket,
                        Convert.ToString(StartPacketDelayBetweenWrongPacketNumericTextBox.Value));
                    writer.WriteEndElement();

                    writer.WriteStartElement(middlePacketTag);
                    writer.WriteElementString(okResponceTag, MiddlePacketOkResponceTextBox.Text);
                    writer.WriteElementString(badResponceTag, MiddlePacketBadResponceTextBox.Text);
                    writer.WriteElementString(retryCountTag, Convert.ToString(MiddlePacketTryCountNumericTextBox.Value));
                    writer.WriteElementString(timeoutTag, Convert.ToString(MiddlePacketTimeoutTextBox.Value));
                    writer.WriteElementString(delayBetweenResendPacket,
                        Convert.ToString(MiddlePacketDelayBetweenWrongPacketNumericTextBox.Value));
                    writer.WriteEndElement();

                    writer.WriteStartElement(lastPacketTag);
                    writer.WriteElementString(okResponceTag, LastPacketOkResponceTextBox.Text);
                    writer.WriteElementString(badResponceTag, LastPacketBadResponceTextBox.Text);
                    writer.WriteElementString(retryCountTag, Convert.ToString(LastPacketTryCountNumericTextBox.Value));
                    writer.WriteElementString(timeoutTag, Convert.ToString(LastPacketTimeoutTextBox.Value));
                    writer.WriteElementString(delayBetweenResendPacket,
                        Convert.ToString(LastPacketDelayBetweenWrongPacketNumericTextBox.Value));
                    writer.WriteEndElement();

                    writer.WriteEndElement();
                    writer.Flush();
                }
                using (var writer = new BinaryWriter(fileStream)) {
                    writer.Write((byte) 0x00);
                    _vioCrypt.ResetCryptState();
                    var packetLen = PacketLenghtTextBox.Value;
                    var buffer = new byte[packetLen + 2];
                    var fileLen = firmwareBuffer.Length;
                    var packetCount = fileLen / packetLen;
                    if (fileLen % packetLen != 0) ++packetCount;
                    ClearBuffer(buffer);
                    var devCodeBytes = Encoding.UTF8.GetBytes(DeviceCodeTextBox.Text);
                    Array.Copy(devCodeBytes, buffer, devCodeBytes.Length < 0x10 ? devCodeBytes.Length : 0x10);
                    Array.Copy(ConvertIntToBytes(StartAddressTextBox.Value, 0x04), 0x00, buffer, 0x10, 0x04);
                    Array.Copy(ConvertIntToBytes(packetCount, 0x02), 0x00, buffer, 0x14, 0x02);
                    Array.Copy(ConvertIntToBytes(Chksm(firmwareBuffer), 0x02), 0x00, buffer, 0x16, 0x02);
                    SavePacket(writer, buffer);
                    var stream = new MemoryStream(firmwareBuffer);
                    while (true) {
                        ClearBuffer(buffer);
                        var readed = stream.Read(buffer, 0, packetLen);
                        if (readed == 0) {
                            MessageBox.Show(this, "Подготовка файла прошла успешно!", "Успех!");
                            break;
                        }
                        SavePacket(writer, buffer);
                    }
                    writer.Flush();
                    writer.Close();
                }
            } catch (Exception) {
                MessageBox.Show(this, "Ошибка открытия файла для записи", "Ошибка!");
            }
        }

        private void SavePacket(BinaryWriter writeStream, byte[] buffer) {
            // TODO: Зашифровать пакетик
            var crypted = _vioCrypt.ContinueCrypt(buffer, buffer.Length - 2);
            Array.Copy(crypted, buffer, buffer.Length - 2);
            var lenPacket = buffer.Length - 2;
            var chk = Chksm(buffer, lenPacket);
            byte[] chkBytes = ConvertIntToBytes(chk, 2);
            buffer[lenPacket] = chkBytes[1];
            buffer[lenPacket + 1] = chkBytes[0];
            writeStream.Write(buffer, 0, buffer.Length);
        }

        private static byte[] ConvertIntToBytes(Int64 number, int bytesCount) {
            var retBuf = new byte[bytesCount];
            for (var i = 0; i < bytesCount; ++i) {
                retBuf[i] = (byte) (number & 0xff);
                number >>= 8;
            }
            return retBuf;
        }

        private static void ClearBuffer(byte[] buffer) {
            for (var idx = 0; idx < buffer.Length; idx++) buffer[idx] = 0x00;
        }

        public static UInt16 Chksm(byte[] array, int lenght = 0) {
            UInt64 sum = 0;
            var i = 0;
            if (lenght == 0) lenght = array.Length;
            while (i < lenght) {
                sum += ((UInt64) array[i++] << 8) + array[i++];
            }
            sum = (sum >> 16) + (sum & 0xffff);
            sum += (sum >> 16);
            var answer = (UInt16) ~sum;
            return answer;
        }
    }
}