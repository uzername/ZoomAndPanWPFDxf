using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ZoomAndPanWPFDxf
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

        private void MirrorCheckbox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void MirrorCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void RotationAngle_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void RenderDXF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (String.IsNullOrEmpty(this.PathToDXF.Text) == false)
                {
                    String rotationAngleRaw = this.RotationAngle.Text;
                    rotationAngleRaw = String.IsNullOrEmpty(rotationAngleRaw) ? "0" : rotationAngleRaw;
                    double rotationAngleDegrees = 0.0;
                    Double.TryParse(rotationAngleRaw, out rotationAngleDegrees);

                    this.DXFrenderPlane.processDxfFile(this.PathToDXF.Text);
                    List<Double> boundValues2 = this.DXFrenderPlane.renderCurrentlyProcessedFile(MirrorCheckbox.IsChecked.GetValueOrDefault(false), rotationAngleDegrees);
                    List<Double> boundValues = this.DXFrenderPlane.getActiveBoundBoxValues();
                    LowerCoordBoundBox.Content = boundValues[0].ToString("0.####") + ";" + boundValues[1].ToString("0.####") + "||" + boundValues[2].ToString("0.####") + ";" + boundValues[3].ToString("0.####");
                    UpperCoordBoundBox.Content = boundValues2[0].ToString("0.####") + ";" + boundValues2[1].ToString("0.####") + "||" + boundValues2[2].ToString("0.####") + ";" + boundValues2[3].ToString("0.####");

                }
            }
            catch (Exception e2)
            {
                MessageBox.Show(e2.Message);
            }
        }
    }
}
