namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            int[] baudRate = { 4800, 9600, 19200, 38400, 57600, 115200, 230400 };
            BaudrateComboBox.ItemsSource = baudRate;
            BaudrateComboBox.SelectedValue = baudRate[2];
            FilenameTextBox.Text = @"Файл не выбран";
        }
    }
}
