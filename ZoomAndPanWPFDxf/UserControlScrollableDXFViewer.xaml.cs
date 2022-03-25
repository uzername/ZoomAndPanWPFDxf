using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
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
    /// Interaction logic for UserControlScrollableDXFViewer.xaml
    /// </summary>
    public partial class UserControlScrollableDXFViewer : UserControl
    {
        #region DXF PARSING LOGIC
        public static double ConvertToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }
        private DxfBoundingBox currentBBox;
        private DxfFile dxfFile = null;

        public void processDxfFile(String inFilePath)
        {
            dxfFile = DxfFile.Load(inFilePath);
            currentBBox = dxfFile.GetBoundingBox();

        }

        /// <summary>
        /// returns bounding box of DXF file: [minX,minY, maxX,maxY]
        /// just a bound box of dxf file, no rotation
        /// </summary>
        /// <returns></returns>
        public List<Double> getActiveBoundBoxValues()
        {
            List<Double> retV = new List<double>();
            retV.Add(currentBBox.MinimumPoint.X);
            retV.Add(currentBBox.MinimumPoint.Y);
            retV.Add(currentBBox.MaximumPoint.X);
            retV.Add(currentBBox.MaximumPoint.Y);
            return retV;
        }

        internal List<double> renderCurrentlyProcessedFile(bool isMirrored, double rotationAngleDegrees)
        {
            List<Double> boundBox = new List<double>(new double[] { 0, 0, 0, 0 });
            if ((dxfFile == null) || (dxfFile.Entities.Count == 0))
            {
                // parse dxf file first
                return boundBox;
            }

            double currentHeightMain = this.ActualHeight;
            double currentWidthMain = this.ActualWidth;

            boundBox = getActiveBoundBoxValues(); //include also rotation here
            double bboxWidthPrimal = Math.Abs(boundBox[0] - boundBox[2]);
            double bboxHeightPrimal = Math.Abs(boundBox[1] - boundBox[3]);
            double scaleX = currentWidthMain / bboxWidthPrimal;
            double scaleY = currentHeightMain / bboxHeightPrimal;
            double usedScale = scaleX < scaleY ? scaleX : scaleY;
            this.renderBaseDXF.Width = bboxWidthPrimal * usedScale;
            this.renderBaseDXF.Height = bboxHeightPrimal * usedScale;
            // now - conjure proper transformation sequence
            // first - assign center to zero
            // then - scale relatively to zero
            // next - move to center of screen

            foreach (DxfEntity entity in dxfFile.Entities)
            {
                DxfColor entityColor = entity.Color;

                switch (entity.EntityType)
                {
                    case DxfEntityType.Line:
                        {
                            DxfLine lineDxf = (DxfLine)entity;
                            Line lineGraphic = new Line();

                            lineGraphic.X1 = lineDxf.P1.X;
                            lineGraphic.Y1 = lineDxf.P1.Y;
                            lineGraphic.X2 = lineDxf.P2.X;
                            lineGraphic.Y2 = lineDxf.P2.Y;
                            lineGraphic.Stroke = Brushes.Black;                            
                            this.renderBaseDXF.Children.Add(lineGraphic);
                            break;
                        }
                    case DxfEntityType.Arc:
                        {
                            /*
                            DxfArc arcDxf = (DxfArc)entity;
                            // arc in dxf is counterclockwise
                            Arc arcGraphic = new Arc();
                            double correctedXCenter = arcDxf.Center.X;
                            double correctedYCenter = arcDxf.Center.Y;
                            // ayyy lmao that's a meme but it works. I have no idea why it worked, but it... uhh, it will backfire at some case
                            arcGraphic.StartAngle = UserControlDXFviewer.ConvertToRadians((arcDxf.EndAngle));
                            arcGraphic.EndAngle = UserControlDXFviewer.ConvertToRadians((arcDxf.StartAngle));
                            arcGraphic.Radius = arcDxf.Radius;
                            arcGraphic.Center = new Point(correctedXCenter, correctedYCenter);
                            arcGraphic.Stroke = Brushes.Black;
                            arcGraphic.StrokeThickness = 1 / usedScale;
                            arcGraphic.RenderTransform = groupOperation;
                            this.renderBaseDXF.Children.Add(arcGraphic);
                            */
                            break;
                        }
                }
            }

            return boundBox;
        }
        #endregion

        #region SCROLLABLE LOGIC
        /// <summary>
        /// Specifies the current state of the mouse handling logic.
        /// </summary>
        private MouseHandlingMode mouseHandlingMode = MouseHandlingMode.None;

        /// <summary>
        /// The point that was clicked relative to the ZoomAndPanControl.
        /// </summary>
        private Point origZoomAndPanControlMouseDownPoint;

        /// <summary>
        /// The point that was clicked relative to the content that is contained within the ZoomAndPanControl.
        /// </summary>
        private Point origContentMouseDownPoint;

        /// <summary>
        /// Records which mouse button clicked during mouse dragging.
        /// </summary>
        private MouseButton mouseButtonDown;

        public UserControlScrollableDXFViewer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Event raised on mouse down in the ZoomAndPanControl.
        /// </summary>
        private void zoomAndPanControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            renderBaseDXF.Focus();
            Keyboard.Focus(renderBaseDXF);

            mouseButtonDown = e.ChangedButton;
            origZoomAndPanControlMouseDownPoint = e.GetPosition(zoomAndPanControl);
            origContentMouseDownPoint = e.GetPosition(renderBaseDXF);

            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0 &&
                (e.ChangedButton == MouseButton.Left ||
                 e.ChangedButton == MouseButton.Right))
            {
                // Shift + left- or right-down initiates zooming mode.
                mouseHandlingMode = MouseHandlingMode.Zooming;
            }
            else if (mouseButtonDown == MouseButton.Left)
            {
                // Just a plain old left-down initiates panning mode.
                mouseHandlingMode = MouseHandlingMode.Panning;
            }

            if (mouseHandlingMode != MouseHandlingMode.None)
            {
                // Capture the mouse so that we eventually receive the mouse up event.
                zoomAndPanControl.CaptureMouse();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Event raised on mouse up in the ZoomAndPanControl.
        /// </summary>
        private void zoomAndPanControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mouseHandlingMode != MouseHandlingMode.None)
            {
                if (mouseHandlingMode == MouseHandlingMode.Zooming)
                {
                    if (mouseButtonDown == MouseButton.Left)
                    {
                        // Shift + left-click zooms in on the content.
                        ZoomIn();
                    }
                    else if (mouseButtonDown == MouseButton.Right)
                    {
                        // Shift + left-click zooms out from the content.
                        ZoomOut();
                    }
                }

                zoomAndPanControl.ReleaseMouseCapture();
                mouseHandlingMode = MouseHandlingMode.None;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Event raised on mouse move in the ZoomAndPanControl.
        /// </summary>
        private void zoomAndPanControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseHandlingMode == MouseHandlingMode.Panning)
            {
                //
                // The user is left-dragging the mouse.
                // Pan the viewport by the appropriate amount.
                //
                Point curContentMousePoint = e.GetPosition(renderBaseDXF);
                Vector dragOffset = curContentMousePoint - origContentMouseDownPoint;

                zoomAndPanControl.ContentOffsetX -= dragOffset.X;
                zoomAndPanControl.ContentOffsetY -= dragOffset.Y;

                e.Handled = true;
            }
        }
        /// <summary>
        /// Event raised by rotating the mouse wheel
        /// </summary>
        private void zoomAndPanControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            if (e.Delta > 0)
            {
                ZoomIn();
            }
            else if (e.Delta < 0)
            {
                ZoomOut();
            }
        }
        /// <summary>
        /// Zoom the viewport in by a small increment.
        /// </summary>
        private void ZoomIn()
        {
            //zoomAndPanControl.ContentScale += 0.05;
            zoomAndPanControl.ContentScale *= 1.25;

            foreach (var itemChild in (this.renderBaseDXF).Children)
            {
                if (itemChild is System.Windows.Shapes.Shape)
                {
                    (itemChild as System.Windows.Shapes.Shape).StrokeThickness = 1 / this.zoomAndPanControl.ContentScale;
                }
            }

        }
        private void ZoomOut()
        {
            //zoomAndPanControl.ContentScale -= 0.05;
            zoomAndPanControl.ContentScale /= 1.25;
            foreach (var itemChild in (this.renderBaseDXF).Children)
            {
                if (itemChild is System.Windows.Shapes.Shape)
                {
                    (itemChild as System.Windows.Shapes.Shape).StrokeThickness = 1 / this.zoomAndPanControl.ContentScale;
                }
            }
        }
        #endregion
    }

    public enum MouseHandlingMode
    {
        /// <summary>
        /// Not in any special mode.
        /// </summary>
        None,
        /// <summary>
        /// The user is left-mouse-button-dragging to pan the viewport.
        /// </summary>
        Panning,
        /// <summary>
        /// The user is holding down shift and left-clicking or right-clicking to zoom in or out.
        /// </summary>
        Zooming,
    }



}
