using ITElite.Projects.WPF.Controls.DeepZoom.Core;
using System;
using System.Reflection;
using System.Windows;

namespace ITElite.Projects.WPF.Controls.DeepZoom.UI.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ImageSource_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ImageSource.SelectedItem!=null)
                MultiImage.Source = new DeepZoomImageTileSource(new Uri("file:///" + (ImageSource.SelectedValue.ToString())));
        }
    }
}