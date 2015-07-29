using System;
using System.Windows;
using System.Windows.Input;

namespace ITElite.Projects.WPF.Controls.ZoomableCanvas.Test
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point _StartPoint;
        private double scale = 1.0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void UIElement_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var zoomCanvas = sender as System.Windows.Controls.ZoomableCanvas;
            scale = (scale + e.Delta/200.0);
            scale = scale > 0.1 ? scale : 0.1;
            zoomCanvas.Scale = scale;

            Console.WriteLine("Current Delta is [{0}],Current Scale is [{1}]", e.Delta, scale);
        }

        private void UIElement_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var control = sender as System.Windows.Controls.ZoomableCanvas;
                var currentPoint = e.GetPosition(control);
                control.Offset = new Point(control.Offset.X - (currentPoint.X - _StartPoint.X)
                    , control.Offset.Y - (currentPoint.Y - _StartPoint.Y));
            }
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var control = sender as System.Windows.Controls.ZoomableCanvas;
            _StartPoint = e.GetPosition(control);
        }
    }
}