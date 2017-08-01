

using System.Windows;
using System.Windows.Markup;

namespace WpfTouchPerformance {
    /// <summary>
    /// Interaction logic for MyContentControl.xaml
    /// </summary>
    [ContentProperty("Child")]
    public partial class MyContentControl {
        public MyContentControl() {
            InitializeComponent();
        }

        public static readonly DependencyProperty ChildProperty = DependencyProperty.Register(
            "Child",
            typeof(UIElement),
            typeof(MyContentControl),
            new PropertyMetadata(default(UIElement)));


        public UIElement Child { get { return (UIElement) GetValue(ChildProperty); } set { SetValue(ChildProperty, value); } }

    }
}
