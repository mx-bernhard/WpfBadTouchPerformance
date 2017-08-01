using System.Windows;

namespace WpfTouchPerformance {
    /// <summary>
    ///     Interaction logic for SecondContentControl.xaml
    /// </summary>

    public partial class SecondContentControl {
        public static readonly DependencyProperty ChildProperty = DependencyProperty.Register(
            "Child",
            typeof(UIElement),
            typeof(SecondContentControl),
            new PropertyMetadata(default(UIElement)));

        public SecondContentControl() {
            InitializeComponent();
        }

        public UIElement Child { get => (UIElement) GetValue(ChildProperty); set => SetValue(ChildProperty, value); }
    }
}
