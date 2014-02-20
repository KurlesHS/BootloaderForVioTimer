using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PrepareFirmware
{
    /// <summary>
    /// Interaction logic for NumericTextBox.xaml
    /// </summary>
    public partial class NumericTextBox
    {
        private readonly DispatcherTimer _timer;
        private int _direction;
        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private string ValueAsString
        {
            get { return (string)GetValue(ValueAsStringProperty); }
            set { SetValue(ValueAsStringProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValueAsString.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueAsStringProperty =
            DependencyProperty.Register("ValueAsString", typeof(string), typeof(NumericTextBox), new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ValueAsStringPropertyChangedCallback));



        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(NumericTextBox), new PropertyMetadata(0, ValuePropertyChangedCallback ));

        private static void ValuePropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs) {
            var source = dependencyObject as NumericTextBox;
            if (source == null) return;
            source.ValueAsString = Convert.ToString(dependencyPropertyChangedEventArgs.NewValue);
        }

        private static void ValueAsStringPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs) {
            var source = dependencyObject as NumericTextBox;
            var str = dependencyPropertyChangedEventArgs.NewValue;
            if (source != null) {
                try {
                    source.Value = Convert.ToInt32(str);
                } catch (Exception) {
                    source.ValueAsString = Convert.ToString(source.Value);
                    source.InnerTextBox.Text = source.ValueAsString;
                }
            }
        }

  
        public int MinValue
        {
            get { return (int)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for minValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register("MinValue", typeof(int), typeof(NumericTextBox), new PropertyMetadata(0));

        public int MaxValue
        {
            get { return (int)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(int), typeof(NumericTextBox), new PropertyMetadata(0));

        public int Step
        {
            get { return (int)GetValue(StepProperty); }
            set { SetValue(StepProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Step.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register("Step", typeof(int), typeof(NumericTextBox), new PropertyMetadata(0));

        public NumericTextBox()
        {
            InitializeComponent();
            Value = 0;
            ValueAsString = "0";
            MinValue = 0;
            MaxValue = 60000;
            Step = 1;
            _direction = 0;
            _timer = new DispatcherTimer();
            _timer.Tick += _timer_Tick;
            _timer.Interval =new TimeSpan(0, 0, 0, 0, 80);
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            if (_direction == 0)
                _timer.Stop();
            else {
                MakeStep(_direction);
            }
        }

        void MakeStep(int direction) {
            var delta = direction * Step;
            if (delta > 0 && Value < MaxValue) {
                if (delta + Value > MaxValue) {
                    Value = MaxValue;
                } else {
                    Value += delta;
                }
            } else if (delta < 0 && Value > MinValue) {
                if (delta + Value > MaxValue) {
                    Value = MinValue;
                } else {
                    Value += delta;
                }
            }   
        }

        private void InnerTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            if (e == null) throw new ArgumentNullException("e");
            if (e.Delta > 0 && Value < MaxValue)
                MakeStep(1);
            else if (e.Delta < 0 && Value > MinValue)
                MakeStep(-1);
        }

        private void DownButton_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            _direction = -1;
            MakeStep(_direction);
            _timer.Start();
        }

        private void DownButton_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _direction = 1;
            MakeStep(_direction);
            _timer.Start();
        }

        private void DownButton_OnMouseButtonUp(object sender, MouseButtonEventArgs e) {
            _direction = 0;
        }

        private void InnerTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            var text = InnerTextBox.Text;
            var pos = InnerTextBox.CaretIndex;
            var newText = text.Insert(pos, e.Text);
            int outVal;
            var x = Int32.TryParse(newText, out outVal);
            if (outVal < MinValue || outVal > MaxValue)
                x = false;
            e.Handled = !x;
            base.OnPreviewTextInput(e);
        }

        private void InnerTextBox_OnTextChanged(object sender, TextChangedEventArgs e) {
            var newText = InnerTextBox.Text;
            int outVal;
            var x = Int32.TryParse(newText, out outVal);
            if (x) 
                Value = outVal;
            else {
                ValueAsString = Convert.ToString(Value);
            }
        }
    }
}
