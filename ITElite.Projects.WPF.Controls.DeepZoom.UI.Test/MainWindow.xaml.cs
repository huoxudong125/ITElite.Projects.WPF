using System;
using System.Windows;
using ITElite.Projects.WPF.Controls.DeepZoom.Core;
using Microsoft.Win32;

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

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Deep zoom map file(*.dzi)|*.dzi|pyramid Xml file(*.xml)|*.xml|All File(*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                if (openFileDialog.FilterIndex == 1)
                {
                    MultiScaleImage.Source = new DeepZoomImageTileSource(new Uri(openFileDialog.FileName, UriKind.Absolute));
                }
                else if (openFileDialog.FilterIndex == 2)
                {
                    MultiScaleImage.Source = new HDImageTileSource(new Uri(openFileDialog.FileName, UriKind.Absolute));
                }
            }
        }

        private void BtnSetResolution_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var resolution = int.Parse(ResolutionTextBox.Text) * 1e-9;
                MultiScaleImage.Resolution = resolution;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}