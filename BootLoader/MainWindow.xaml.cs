using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.IO.Ports;
using System.Xml;
using System.Runtime.InteropServices;


namespace BootLoader
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [GuidAttribute("E868067A-33DC-4563-AC27-566839970EF8")]
    public partial class MainWindow
    {
        private bool _isCryptingEnabled = true;
        private string _hexFilename = "";
        private string _serialPort = "";
        private readonly byte[] _buffer = new byte[0x10000];
        private readonly BackgroundWorker _bgWorker = new BackgroundWorker();
        readonly string _settingFile;
        enum FlasherStatus
        {
            WaitReady,
            Ready,
            WaitResponse,
            ReadyToSendNextPacket,
            Timeout,
            WrongPacket,
            Bad,
            WaitLastResponse,
            LastPacket,
            
        }

        private static string[] FixComPortsNames(ICollection<string> comportStrings)
        {
            var ret = new string[comportStrings.Count];
            var index = 0;
            foreach (var comportString in comportStrings)
            {
                var regex = new Regex("\\b(\\w+\\d+)");
                var match = regex.Match(comportString);
                if (match.Success)
                {
                    ret[index] = match.Groups[0].ToString();    
                }
                else
                {
                    ret[index] = comportString;
                }
                ++index;
            }
            return ret;

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            UpdateSettings();
            base.OnClosing(e);
        }

        private void UpdateIsCryptingEnabledButtonText()
        {
            var text = _isCryptingEnabled ? "Включено" : "Отключено";
            IsCryptEnabledButton.Content = text;
        }
        public MainWindow()
        {
            var array = new List<byte> { 12, 12, 43, 54, 34, 23, 23, 33 };
            var crc = Chksm(array.ToArray());

            array.Add((byte)(crc >> 8));
            array.Add((byte)crc);

            Debug.WriteLine(String.Format("crc: {0}, {1:x2}", _CRC(array.ToArray()), crc));
            
            InitializeComponent();
            _bgWorker.WorkerReportsProgress = true;
            _bgWorker.WorkerSupportsCancellation = true;
            var ports = SerialPort.GetPortNames();
            ports = FixComPortsNames(ports);

            int[] baudRate = { 4800, 9600, 19200, 38400, 57600, 115200, 230400 };
            ComboBoxForSerialPortBaudrate.ItemsSource = baudRate;
            ComboBoxForSerialPortBaudrate.SelectedItem = baudRate[0];
            
            _bgWorker.DoWork += bgWorker_DoWork;
            _bgWorker.RunWorkerCompleted += bgWorker_RunWorkerCompleted;
            _bgWorker.ProgressChanged += bgWorker_ProgressChanged;
            ComboboxForPortsNames.ItemsSource = ports;
            if (ports.Length > 0)
            {
                ComboboxForPortsNames.SelectedItem = ports[0];
                _serialPort = ports[0];
            }
            _settingFile = AppDomain.CurrentDomain.BaseDirectory;
            if (_settingFile.Length > 0)
                if (_settingFile.Substring(_settingFile.Length - 1, 1) != "\\")
                    _settingFile += "\\";
            _settingFile += "settings.xml";
            ProgressBar.Text = "Выберите файл";
            var settingFileIsPresents = true;
            try
            {
                var xd = new XmlDocument();
                xd.Load(_settingFile);
                var currentDeviceCode = "";
                if (xd.DocumentElement != null)
                    foreach (XmlNode node in xd.DocumentElement.ChildNodes)
                    {
                        if (node.Name == "FileName")
                            _hexFilename = node.InnerText;
                        if (node.Name == "SerialPort")
                        {
                            var serialPortName = node.InnerText;
                            if (ComboboxForPortsNames.Items.Contains(serialPortName))
                                _serialPort = node.InnerText;
                        }
                            
                        if (node.Name == "CryptIsEnabled")
                            _isCryptingEnabled = node.InnerText == "true";
                        if (node.Name == "BaudRate")
                        {   
                            ComboBoxForSerialPortBaudrate.SelectedItem = Convert.ToInt32(node.InnerText);   
                        }
                        if (node.Name == "CurrentDeviceCode")
                            currentDeviceCode = node.InnerText;
                        if (node.Name == "ListOfDeveiceCodes")
                        {
                            var xmlNodeList = node.ChildNodes;
                            foreach (XmlNode xmlNode in xmlNodeList)
                            {
                                if (xmlNode.Name == "DeviceCode")
                                {
                                    ComboBoxForDeviceCode.Items.Add(xmlNode.InnerText);
                                }
                            }
                        }
                    }
                if (ComboBoxForDeviceCode.Items.Contains(currentDeviceCode))
                {
                    ComboBoxForDeviceCode.SelectedItem = currentDeviceCode;
                }
                else
                {
                    ComboBoxForDeviceCode.Text = currentDeviceCode;
                }
                UpdateIsCryptingEnabledButtonText();
                ParseHexFile();
                ButtonStartFlashing.IsEnabled = true;
                ComboboxForPortsNames.SelectedItem = _serialPort;
            }
            catch (Exception)
            {
                settingFileIsPresents = false;
            }
            if (!settingFileIsPresents)
            {
                UpdateSettings();
            }
        }
        // проверка крк пакета возращает 1 если верно
        static byte _CRC(IList<byte> array)
        {

            UInt64 sum = 0;
            byte i = 0;

            while (i < array.Count)
                sum += ((UInt64)array[i++] << 8) + array[i++];
            sum = (sum >> 16) + (sum & 0xFFFF);
            sum += (sum >> 16);
            return sum == 0xffff ? (byte)1 : (byte)0;
        }

        static UInt16 Chksm(byte[] array)
        {
            UInt64 sum = 0;
            var i = 0;
            while (i < array.Length)
            {
                sum += ((UInt64)array[i++] << 8) + array[i++];
            }
            sum = (sum >> 16) + (sum & 0xffff);
            sum += (sum >> 16);
            var answer = (UInt16)~sum;
            return answer;
        }

        void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                ProgressBar.Text = "Операция отменена";
            }
            else if (e.Error != null)
            {
                ProgressBar.Text = String.Format("Ошибка: {0}", e.Error.Message);
            }
            else
            {
                ProgressBar.Text = e.Result.ToString();
            }

            ButtonSelectFile.IsEnabled = true;
            ButtonStartFlashing.IsEnabled = true;
            ComboboxForPortsNames.IsEnabled = true;
            ButtonSelectAndFlashing.IsEnabled = true;
        }

        byte[] CalculateCrc()
        {

            var crc = new byte[] { 0, 0, 0, 0 };
            for (var i = 0; i < (_buffer.Length); i += 4)
            {
                crc[0] ^= _buffer[i];
                crc[1] ^= _buffer[i + 1];
                crc[2] ^= _buffer[i + 2];
                crc[3] ^= _buffer[i + 3];
            }
            return crc;
        }

        private System.Timers.Timer _timer;
        private static readonly object Locker = new Object();
        private FlasherStatus _currentFlashStatus;
        void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            _readBufOffset = 0;
            var startAddress = (UInt32)_minAddress;
            var iterators = ((uint)_maxAddress - startAddress) / 128;
            iterators += 4;
            var curIter = 0;
            SetMaxValueForProgressBar((int)iterators);
            SetValueForProgressBar(curIter);
            Debug.WriteLine(String.Format("background: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId));
            if (worker == null)
                return;
            var portName = e.Argument.ToString();
            var portStatus = String.Format("Порт {0} открыт", portName);
            SetTextForProgressBar(String.Format("Попытка открыть {0} порт", portName));

            using (var sp = new SerialPort(portName))
            {
                try
                {   
                    sp.DataReceived += OnSerialDataReceived;
                    sp.Parity = Parity.None;
                    sp.DataBits = 8;
                    sp.BaudRate = 38400;
                    sp.Handshake = Handshake.None;
                    sp.StopBits = StopBits.One;
                    sp.Open();
                    SetValueForProgressBar(++curIter);



                }
                catch (Exception)
                {
                    e.Result = String.Format("Не получилось открыть {0} порт", portName);
                    return;
                }
                SetTextForProgressBar(portStatus);
                _timer = new System.Timers.Timer {Interval = 5000};
                _timer.Elapsed += OnTimer2;

                try
                {
                    sp.Write(new ASCIIEncoding().GetBytes("start"), 0, 5);
                    _currentFlashStatus = FlasherStatus.WaitReady;


                    _timer.Start();
                    var isProcess = true;
                    var tempMaxAddress = _maxAddress;
                    var crcIsSended = false;
                    while (isProcess)
                    {
                        System.Threading.Thread.Sleep(0);
                        switch (_currentFlashStatus)
                        {
                            case FlasherStatus.WaitReady:
                            case FlasherStatus.WaitResponse:
                            case FlasherStatus.WaitLastResponse:
                                break;
                            case FlasherStatus.Ready:
                            case FlasherStatus.ReadyToSendNextPacket:
                                {
                                    SetValueForProgressBar(++curIter);
                                    _timer.Stop();
                                    var packet = new List<byte>();
                                    UInt16 crc;
                                    if (startAddress >= 0x10000)
                                    {
                                        // всё, прошили
                                        if (crcIsSended)
                                        {
                                            packet.Add(6);
                                            packet.Add(4);
                                            packet.Add(0);
                                            packet.Add(0);
                                            crc = Chksm(packet.ToArray());
                                            packet.Add((byte)(crc >> 8));
                                            packet.Add((byte)crc);
                                            codeList(ref packet);
                                            _timer.Interval = 7000;
                                            _timer.Start();
                                            _currentFlashStatus = FlasherStatus.WaitLastResponse;
                                            sp.Write(packet.ToArray(), 0, packet.Count);
                                            break;
                                        }
                                        // посылаем crc
                                        packet.Add(10);
                                        packet.Add(3);
                                        packet.Add(0);
                                        packet.Add(0);
                                        var crcOfMemory = CalculateCrc();
                                        for (var i = 0; i < 4; ++i )
                                            packet.Add(crcOfMemory[i]);
                                        crc = Chksm(packet.ToArray());
                                        packet.Add((byte)(crc >> 8));
                                        packet.Add((byte)crc);
                                        codeList(ref packet);
                                        _timer.Interval = 7000;
                                        _timer.Start();
                                        crcIsSended = true;
                                        _currentFlashStatus = FlasherStatus.WaitResponse;
                                        sp.Write(packet.ToArray(), 0, packet.Count);
                                        break;
                                    }
                                    if (startAddress >= tempMaxAddress) 
                                    {
                                        // закончились полезные данные
                                        packet.Add(6);
                                        packet.Add(2);
                                        packet.Add((byte)_maxAddress);
                                        packet.Add((byte)(_maxAddress >> 8));
                                        crc = Chksm(packet.ToArray());
                                        packet.Add((byte)(crc >> 8));
                                        packet.Add((byte)crc);
                                        codeList(ref packet);
                                        startAddress = 0x10000;
                                        tempMaxAddress = 0x100000;
                                        _currentFlashStatus = FlasherStatus.WaitResponse;
                                        sp.Write(packet.ToArray(), 0, packet.Count);
                                        _timer.Interval = 7000;
                                        _timer.Start();
                                        
                                        break;
                                    }
                                        
                                    packet.Add(134);
                                    packet.Add(1);
                                    packet.Add((byte)startAddress);
                                    packet.Add((byte)(startAddress >> 8));

                                    for (var i = 0; i < 128; ++i)
                                    {
                                        packet.Add(_buffer[startAddress + i]);
                                    }
                                    crc = Chksm(packet.ToArray());
                                    packet.Add((byte)(crc >> 8));
                                    packet.Add((byte)crc);
                                    codeList(ref packet);
                                    _currentFlashStatus = FlasherStatus.WaitResponse;
                                    sp.Write(packet.ToArray(), 0, packet.Count);
                                    _timer.Interval = 3000;
                                    _timer.Start();
                                    startAddress += 128;
                                }
                                break;
                            case FlasherStatus.Timeout:
                                {
                                    isProcess = false;
                                    e.Result = "Превышено время ожидания ответа от устройства";
                                }
                                break;
                            case FlasherStatus.WrongPacket:
                                {
                                    isProcess = false;
                                    e.Result = "Принят не верный ответ от устройства";
                                }
                                break;
                            case FlasherStatus.Bad:
                                {
                                    isProcess = false;
                                    e.Result = "Устройство отрапортовало об ошибке";
                                }
                                break;
                            case FlasherStatus.LastPacket:
                                {
                                    SetValueForProgressBar(++curIter);
                                    isProcess = false;
                                    e.Result = "Устройство прошито";
                                }
                                break;
                        }
                    }
                    SetValueForProgressBar((int)iterators);
                }
                catch (Exception exc)
                {

                    e.Result = exc.Message;
                }
                finally
                {
                    _timer.Stop();
                    sp.Close();
                }
            }
        }

        void codeList(ref List<byte> lst)
        {
            for (var i = 0; i < lst.Count; ++i)
                lst[i] ^= 0x95;
        }
        void OnTimer2(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (Locker)
            {
                _currentFlashStatus = FlasherStatus.Timeout;
            }
        }

        static private readonly byte[] ReadBuf = new byte[0x100];
        static private int _readBufOffset;
        void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Debug.WriteLine(String.Format("readyread: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId));

            lock (Locker)
            {
                var sp = sender as SerialPort;
                if (sp == null)
                    return;
                _timer.Stop();
                var len = sp.Read(ReadBuf, _readBufOffset, sp.BytesToRead);
                _readBufOffset += len;
                switch (_currentFlashStatus)
                {
                    case FlasherStatus.WaitReady:
                    {
                        var response = Encoding.ASCII.GetString(ReadBuf, 0, _readBufOffset);
                        switch (response)
                        {
                            case "bad":
                                _currentFlashStatus = FlasherStatus.Bad;
                                break;
                            case "ready":
                                _currentFlashStatus = FlasherStatus.ReadyToSendNextPacket;
                                _readBufOffset = 0;
                                break;
                            default:
                                if (_readBufOffset >= 5)
                                    _currentFlashStatus = FlasherStatus.WrongPacket;
                                else
                                {
                                    _timer.Interval = 1000;
                                    _timer.Start();
                                }
                                break;
                        }
                    }
                        break;
                    case FlasherStatus.WaitResponse:
                    {
                        var response = Encoding.ASCII.GetString(ReadBuf, 0, _readBufOffset);
                        switch (response)
                        {
                            case "bad":
                                _currentFlashStatus = FlasherStatus.Bad;
                                break;
                            case "good":
                                _currentFlashStatus = FlasherStatus.ReadyToSendNextPacket;
                                _readBufOffset = 0;
                                break;
                            default:
                                if (_readBufOffset >= 4)
                                    _currentFlashStatus = FlasherStatus.WrongPacket;
                                else
                                {
                                    _timer.Interval = 1000;
                                    _timer.Start();
                                }
                                break;
                        }
                    }
                        break;
                    case FlasherStatus.WaitLastResponse:
                    {
                        var response = Encoding.ASCII.GetString(ReadBuf, 0, _readBufOffset);
                        switch (response)
                        {
                            case "bad":
                                _currentFlashStatus = FlasherStatus.Bad;
                                break;
                            case "good":
                                _currentFlashStatus = FlasherStatus.LastPacket;
                                _readBufOffset = 0;
                                break;
                            default:
                                if (_readBufOffset >= 4)
                                    _currentFlashStatus = FlasherStatus.WrongPacket;
                                else
                                {
                                    _timer.Interval = 1000;
                                    _timer.Start();
                                }
                                break;
                        }
                    }
                        break;
                    default:
                        {
                            _currentFlashStatus = FlasherStatus.WrongPacket;
                        }
                        break;
                }
            }
        }

        private void UpdateSettings()
        {
            AddCurrentDeviceCodeToComboboxList();
            try
            {
                var settings = new XmlWriterSettings
                {
                    // включаем отступ для элементов XML документа
                    // (позволяет наглядно изобразить иерархию XML документа)
                    Indent = true,
                    IndentChars = "  ",
                    // задаем переход на новую строку
                    NewLineChars = "\n",
                    // Нужно ли опустить строку декларации формата XML документа
                    // речь идет о строке вида "<?xml version="1.0" encoding="utf-8"?>"
                    OmitXmlDeclaration = false
                };
                using (var xw = XmlWriter.Create(_settingFile, settings))
                {
                    xw.WriteStartElement("Flasher");

                    xw.WriteElementString("FileName", _hexFilename);
                    xw.WriteElementString("SerialPort", _serialPort);
                    xw.WriteElementString("CryptIsEnabled", _isCryptingEnabled ? "true" : "false");
                    xw.WriteElementString("BaudRate", ComboBoxForSerialPortBaudrate.Text);
                    var itemCollection = ComboBoxForDeviceCode.Items;
                    if (!itemCollection.IsEmpty)
                    {
                        xw.WriteStartElement("ListOfDeveiceCodes");
                        foreach (var item in itemCollection)
                        {
                            xw.WriteElementString("DeviceCode", item.ToString());
                        }
                        xw.WriteEndElement();
                        xw.WriteElementString("CurrentDeviceCode", ComboBoxForDeviceCode.Text);
                    }
                    xw.WriteEndElement();
                    xw.Flush();
                    xw.Close();
                }
            }
            catch (Exception)
            {
                ProgressBar.Text = "Ошибка при обновлени настроек";
            }
        }
        private void SetMaxValueForProgressBar(int value)
        {
            ProgressBar.Dispatcher.BeginInvoke(new Action<int>(x => { ProgressBar.Maximum = x; }), value);
        }

        private void SetValueForProgressBar(int value)
        {
            ProgressBar.Dispatcher.BeginInvoke(new Action<int>(x => { ProgressBar.Value = x; }), value);
        }

        private void SetTextForProgressBar(string text)
        {
            ProgressBar.Dispatcher.BeginInvoke(new Action<string>(x => { ProgressBar.Text = x; }), text);
        }

        protected static ushort GetChecksum(byte[] bytes, int startAddress = 0)
        {
            ulong sum = 0;
            // Sum all the words together, adding the final byte if size is odd
            var i = startAddress;
            for (; i < bytes.Length - 1; i += 2)
            {
                sum += BitConverter.ToUInt16(bytes, i);
            }
            if (i != bytes.Length)
                sum += bytes[i];
            // Do a little shuffling
            sum = (sum >> 16) + (sum & 0xFFFF);
            sum += (sum >> 16);
            return (ushort)(~sum);
        }

        delegate void ChangeProgressBarValue(int value);
        public void ChangeProgerssBarValue(int value)
        {
            if (ProgressBar.Dispatcher.CheckAccess())
                ProgressBar.Value = value;
            else
            {
                ProgressBar.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new ChangeProgressBarValue(ChangeProgerssBarValue),
                    value);
            }
        }

        private int _minAddress;
        private int _maxAddress;
        private void ParseHexFile()
        {
            LabelForFileName.Text = _hexFilename;
            for (var i = 0; i < 0x10000; ++i)
                _buffer[i] = 0x00;

            using (var sr = new StreamReader(_hexFilename))
            {

                _minAddress = 0x10000;
                _maxAddress = 0;
                var converted = true;
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();

                    try
                    {
                        if (line == null)
                            throw  new Exception();
                        if (line.Substring(0, 1) != ":")
                            throw new Exception();
                        byte crc = 0;
                        // считаем контрольную сумму
                        
                            
                        for (var x = 1; x < line.Length; x += 2)
                        {
                            var val = Convert.ToByte(line.Substring(x, 2), 16);
                            crc += val;
                        }
                        if (crc != 0)
                            throw new Exception();
                        // контрольная сумма совпала
                        var lenData = Convert.ToInt32(line.Substring(1, 2), 16);
                        var startAddress = Convert.ToInt32(line.Substring(3, 4), 16);
                        var type = Convert.ToInt32(line.Substring(7, 2), 16);
                        switch (type)
                        {
                            case 5:
                                {
                                    // pc counter. Игнорируем
                                }
                                break;
                            case 1:
                                {
                                    // конец файла
                                    if (lenData != 0)
                                        throw new Exception();
                                }
                                break;
                            case 0:
                                {
                                    // данные
                                    // заполняем буфер
                                    for (var i = 0; i < lenData; ++i)
                                        _buffer[i + startAddress] = Convert.ToByte(line.Substring(i * 2 + 9, 2), 16);
                                    //корректируем занчения
                                    if (_minAddress > startAddress)
                                        _minAddress = startAddress;
                                    if (_maxAddress < (startAddress + lenData))
                                        _maxAddress = startAddress + lenData;
                                }
                                break;
                            default:
                                throw new Exception();
                        }
                    }
                    catch (Exception)
                    {
                        converted = false;
                        break;
                    }
                }
                if (!converted)
                {
                    ProgressBar.Text = "Не верный формат hex файла";
                    ButtonStartFlashing.IsEnabled = false;
                }
                else
                {
                    ButtonStartFlashing.IsEnabled = true;
                    // подводим к границе 128 байт
                    _minAddress -= _minAddress % 128;
                    ++_maxAddress;
                    // maxAddress - первый свободный адрес
                    // так же к границе 128 байт подводим, но тут к верхней
                    if (_maxAddress % 128 != 0)
                    {
                        _maxAddress -= _maxAddress % 128;
                        _maxAddress += 128;
                    }

                    Debug.WriteLine(String.Format("minAddres = {0}, maxAddress = {1}", _minAddress, _maxAddress));
                    ProgressBar.Text = "Всё готово к прошивке";
                }
            }
        }

        private void comboboxForPortsNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _serialPort = ComboboxForPortsNames.SelectedItem.ToString();
            UpdateSettings();
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };
        public static Point GetMousePosition()
        {
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }
        private void ButtonCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private bool _mouseIsCaptured;
        private Point _lastMousePos;

        private void Window_MouseMove_1(object sender, MouseEventArgs e)
        {
            if (_mouseIsCaptured)
            {
                var curMousePos = GetMousePosition();
                var deltax = _lastMousePos.X - curMousePos.X;
                var deltay = _lastMousePos.Y - curMousePos.Y;
                _lastMousePos.X = curMousePos.X;
                _lastMousePos.Y = curMousePos.Y;
                Top -= deltay;
                Left -= deltax;
            }
        }

        private void TextBlock_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            //this.DragMove();
            var el = sender as UIElement;
            if (el == null)
                return;
            if (e.LeftButton == MouseButtonState.Pressed)
                _mouseIsCaptured = el.CaptureMouse();
            _lastMousePos = GetMousePosition();
        }

        private void TextBlock_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                _mouseIsCaptured = false;
                var el = sender as UIElement;
                if (el == null)
                    return;
                el.ReleaseMouseCapture();
            }
        }

        private void ButtonStartFlashing_Click(object sender, RoutedEventArgs e)
        {
            ParseHexFile();
            if (!ButtonStartFlashing.IsEnabled)
            {   
                ButtonStartFlashing.IsEnabled = true;
                return;
            }
            ButtonSelectFile.IsEnabled = false;
            ButtonStartFlashing.IsEnabled = false;
            ComboboxForPortsNames.IsEnabled = false;
            ButtonSelectAndFlashing.IsEnabled = false;
            ProgressBar.Text = "Идет прошивка, подождите...";
            _bgWorker.RunWorkerAsync(_serialPort);
        }

        private void ButtonSelectFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog {DefaultExt = ".hex", Filter = "HEX files (*.hex)|*.hex"};
            var result = dlg.ShowDialog();
            if (result != true) return;
            _hexFilename = dlg.FileName;
            ParseHexFile();
            UpdateSettings();
        }

        private void ButtonSelectAndFlashing_Click(object sender, RoutedEventArgs e)
        {
            ButtonSelectFile_Click(this, new RoutedEventArgs());
            if (ButtonStartFlashing.IsEnabled)
                ButtonStartFlashing_Click(this, new RoutedEventArgs());
            else
                ProgressBar.Text = "Херню вы какую то выбрали, батенька";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _isCryptingEnabled ^= true;
            UpdateIsCryptingEnabledButtonText();
        }

        private void ComboBoxForDeviceCode_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                AddCurrentDeviceCodeToComboboxList();
            }
        }

        private void AddCurrentDeviceCodeToComboboxList()
        {
            ItemCollection itemCollection = ComboBoxForDeviceCode.Items;
            string addedItem = ComboBoxForDeviceCode.Text;
            addedItem = addedItem.Trim(' ');
            if (addedItem.Length > 0)
            {
                if (!itemCollection.Contains(addedItem))
                {
                    itemCollection.Add(ComboBoxForDeviceCode.Text);
                }

            }
        }
    }
}
