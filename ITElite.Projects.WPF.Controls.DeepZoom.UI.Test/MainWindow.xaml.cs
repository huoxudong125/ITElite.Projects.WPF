using System;
using System.IO;
using System.Reflection;
using System.Windows;
using ITElite.Projects.Common;
using ITElite.Projects.WPF.Controls.DeepZoom.Core;
using Microsoft.Win32;

namespace ITElite.Projects.WPF.Controls.DeepZoom.UI.Test
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        
         private void ImageSource_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ImageSource.SelectedItem!=null&& File.Exists(ImageSource.SelectedValue.ToString()))
                MultiImage.Source = new DeepZoomImageTileSource(new Uri("file:///" + (ImageSource.SelectedValue)));
        }
        
        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Deep zoom map file(*.dzi)|*.dzi|pyramid Xml file(*.xml)|*.xml|All File(*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    if (openFileDialog.FilterIndex == 1)
                    {
                        MultiImage.Source =
                            new DeepZoomImageTileSource(new Uri(openFileDialog.FileName, UriKind.Absolute));
                    }
                    else if (openFileDialog.FilterIndex == 2)
                    {
                        MultiImage.Source =
                            new HDImageTileSource(new Uri(openFileDialog.FileName, UriKind.Absolute));
                    }

                    FilePathLabel.Content = openFileDialog.FileName;
                    BtnSetResolution_OnClick(null, null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void ChangeFileSize()
        {
            FileSize.Content = string.Format("Pixle Size [{0}*{1}] ,Physic Size:[{2}*{3}]",
                MultiImage.Source.ImageSize.Width,
                MultiImage.Source.ImageSize.Height,
                (MultiImage.Source.ImageSize.Width * MultiImage.Resolution).ToLengthSize(),
                (MultiImage.Source.ImageSize.Height * MultiImage.Resolution).ToLengthSize());
        }

        private void BtnSetResolution_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var resolution = float.Parse(ResolutionTextBox.Text)*1e-9;
                MultiImage.Resolution = resolution;
                ChangeFileSize();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}