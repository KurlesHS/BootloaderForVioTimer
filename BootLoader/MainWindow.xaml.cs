using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.IO.Ports;
using System.Xml;
using System.Runtime.InteropServices;
using BootLoader.Device;
using BootLoader.Protocol.Implemantations;


namespace BootLoader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [GuidAttribute("E868067A-33DC-4563-AC27-566839970EF8")]
    public class SerialPortSetting
    {
        public int Baudrate { get; set; }
        public string PortName { get; set; }
    }
    public partial class MainWindow
    {
        private bool _isCryptingEnabled = true;
        private string _hexFilename = "";
        private string _serialPort = "";
        private readonly BackgroundWorker _bgWorker = new BackgroundWorker();
        readonly string _settingFile;

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
                    ret[index] = match.Groups[1].ToString();    
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
                            _serialPort = serialPortName;
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
                ButtonStartFlashing.IsEnabled = true;
                ComboboxForPortsNames.Text = _serialPort;
            }
            catch (Exception)
            {
                settingFileIsPresents = false;
            }
            LabelForFileName.Text = _hexFilename;
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
                // TODO: restore
                ProgressBar.Text = e.Result.ToString();
            }

            ButtonSelectFile.IsEnabled = true;
            ButtonStartFlashing.IsEnabled = true;
            ComboboxForPortsNames.IsEnabled = true;
            ButtonSelectAndFlashing.IsEnabled = true;
        }


        private bool _inProcess;
        private string _resultString;
        void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            SetMaxValueForProgressBar(1000);
            SetValueForProgressBar(0);
            Debug.WriteLine(String.Format("background: {0}", Thread.CurrentThread.ManagedThreadId));
            if (worker == null)
                return;
            var portSetting = e.Argument as SerialPortSetting;
            if (portSetting == null)
                return;
            SetTextForProgressBar("Открываем порт " + portSetting.PortName);
            var device = new TimerDeviceImpl(new SerialProtocol(portSetting.PortName, portSetting.Baudrate));
            device.ProcessHandler += device_ProcessHandler;
            device.ErrorHandler += device_ErrorHandler;
            device.FinishedHandler += device_FinishedHandler;
            device.PacketHandler += device_PacketHandler;
            try {
                using (var stream = new FileStream(_hexFilename, FileMode.Open)) {
                    if (!device.StartFlashing(stream)) {
                        e.Result = "Ошибка открытия последовательного порта";
                        return;
                    }
                    SetTextForProgressBar("Ожидаем ответа от таймера");
                    _inProcess = true;
                    _resultString = "";
                    while (_inProcess) {
                        Thread.Sleep(20);
                    }
                }
            } catch (Exception) {
                e.Result = "Не получилось открыть файл";
            }
            e.Result = _resultString;
        }

        private long _packetLength;
        void device_PacketHandler(object sended, long packetCount, long packetLenght) {
            SetMaxValueForProgressBar((int) packetCount - 1);
            _packetLength = packetLenght;
        }

        

        void device_FinishedHandler(object sender) {
            _resultString = "Устройство прошито";
            _inProcess = false;
        }

        void device_ErrorHandler(object sender, string description) {
            _resultString = "Ошибка прошивки";
            _inProcess = false;
        }

        void device_ProcessHandler(object sender, int position)
        {
            SetValueForProgressBar(position);
            if (position > 1) SetTextForProgressBar(String.Format("Прошито {0} байт", position * (_packetLength - 2)));
        }

        private void UpdateSettings()
        {
            AddCurrentDeviceCodeToComboboxList();
            LabelForFileName.Text = _hexFilename;
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
                    xw.WriteElementString("SerialPort", ComboboxForPortsNames.Text);
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

        

        private void comboboxForPortsNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _serialPort = ComboboxForPortsNames.Text;
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
            _bgWorker.RunWorkerAsync(new SerialPortSetting {Baudrate = Convert.ToInt32(ComboBoxForSerialPortBaudrate.SelectedItem.ToString()), PortName = ComboboxForPortsNames.Text});
        }

        private void ButtonSelectFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog {DefaultExt = ".tmr", Filter = "Файлы прошивки (*.tmr)|*.tmr"};
            var result = dlg.ShowDialog();
            if (result != true) return;
            _hexFilename = dlg.FileName;
            var x = new TimerDeviceImpl(new SerialProtocol("2323", 32));
            try {
                using (var s = new FileStream(_hexFilename, FileMode.Open)) {
                    var b = x.GetBaudrateFromStream(s);
                    if (b < 0) ProgressBar.Text = "Херню вы какую то выбрали, батенька";
                    ComboBoxForSerialPortBaudrate.SelectedItem = b;
                }
            } finally {
                UpdateSettings();
            }
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
