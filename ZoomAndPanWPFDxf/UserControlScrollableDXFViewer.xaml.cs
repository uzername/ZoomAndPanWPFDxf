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
        double usedScale = 1;

        public void processDxfFile(String inFilePath)
        {
            dxfFile = DxfFile.Load(inFilePath);
            currentBBox = dxfFile.GetBoundingBox();

        }

        /// <summary>
        /// returns bounding box of DXF file: [minX,minY, maxX,maxY]
        /// with rotation applied
        /// </summary>
        /// <param name="inAngle">rotation angle in degrees</param>
        /// <returns></returns>
        public List<Double> getActiveBoundBoxValuesWithRotation(double inAngle)
        {
            List<Double> boundBox = getActiveBoundBoxValues();
            double assumedRotationCenterX = (boundBox[0] + boundBox[2]) / 2;
            double assumedRotationCenterY = (boundBox[1] + boundBox[3]) / 2;
            if ((inAngle == 0) || (inAngle == 360))
            {
                return boundBox;
            }
            else
            {
                // rotation matrix is counter clockwise?
                Matrix rotationMatrix = new Matrix();
                rotationMatrix.SetIdentity();
                rotationMatrix.RotateAt(inAngle, assumedRotationCenterX, assumedRotationCenterY);
                boundBox[0] = Double.NaN; boundBox[1] = Double.NaN;
                boundBox[2] = Double.NaN; boundBox[3] = Double.NaN;
                foreach (var itemEntity in dxfFile.Entities)
                {
                    switch (itemEntity.EntityType)
                    {
                        case DxfEntityType.Line:
                            {
                                // calculate bound box for rotated line
                                Point P1Line = new Point((itemEntity as DxfLine).P1.X, (itemEntity as DxfLine).P1.Y);
                                Point P2Line = new Point((itemEntity as DxfLine).P2.X, (itemEntity as DxfLine).P2.Y);
                                Point P1LineRotated = rotationMatrix.Transform(P1Line);
                                Point P2LineRotated = rotationMatrix.Transform(P2Line);
                                if (Double.IsNaN(boundBox[0]) && Double.IsNaN(boundBox[1]) && Double.IsNaN(boundBox[2]) && Double.IsNaN(boundBox[3]))
                                {
                                    if (P1LineRotated.X < P2LineRotated.X)
                                    {
                                        boundBox[0] = P1LineRotated.X;
                                        boundBox[2] = P2LineRotated.X;
                                    }
                                    else
                                    {
                                        boundBox[2] = P1LineRotated.X;
                                        boundBox[0] = P2LineRotated.X;
                                    }
                                    if (P1LineRotated.Y < P2LineRotated.Y)
                                    {
                                        boundBox[1] = P1LineRotated.Y;
                                        boundBox[3] = P2LineRotated.Y;
                                    }
                                    else
                                    {
                                        boundBox[3] = P1LineRotated.Y;
                                        boundBox[1] = P2LineRotated.Y;
                                    }
                                }
                                else
                                {
                                    if (P1LineRotated.X < boundBox[0])
                                    {
                                        boundBox[0] = P1LineRotated.X;
                                    }
                                    if (P1LineRotated.X > boundBox[2])
                                    {
                                        boundBox[2] = P1LineRotated.X;
                                    }
                                    if (P1LineRotated.Y < boundBox[1])
                                    {
                                        boundBox[1] = P1LineRotated.Y;
                                    }
                                    if (P1LineRotated.Y > boundBox[3])
                                    {
                                        boundBox[3] = P1LineRotated.Y;
                                    }
                                    // ===============================
                                    if (P2LineRotated.X < boundBox[0])
                                    {
                                        boundBox[0] = P2LineRotated.X;
                                    }
                                    if (P2LineRotated.X > boundBox[2])
                                    {
                                        boundBox[2] = P2LineRotated.X;
                                    }
                                    if (P2LineRotated.Y < boundBox[1])
                                    {
                                        boundBox[1] = P2LineRotated.Y;
                                    }
                                    if (P2LineRotated.Y > boundBox[3])
                                    {
                                        boundBox[3] = P2LineRotated.Y;
                                    }

                                }
                                break;
                            }
                        case DxfEntityType.Arc:
                            {
                                double findNearestStraightAngle(double inAngle2)
                                {
                                    double retVal = 0;
                                    if ((inAngle2 >= 0) && (inAngle2 < 90))
                                    {
                                        retVal = 90;
                                    }
                                    else if ((inAngle2 >= 90) && (inAngle2 < 180))
                                    {
                                        retVal = 180;
                                    }
                                    else if ((inAngle2 >= 180) && (inAngle2 < 270))
                                    {
                                        retVal = 270;
                                    }
                                    else if ((inAngle2 >= 270) && (inAngle2 < 360))
                                    {
                                        retVal = 360;
                                    }
                                    else if ((inAngle2 >= 360) && (inAngle2 < 450))
                                    {
                                        retVal = 450;
                                    }
                                    else if ((inAngle2 >= 450) && (inAngle2 < 540))
                                    {
                                        retVal = 540;
                                    }
                                    else if ((inAngle2 >= 540) && (inAngle2 < 630))
                                    {
                                        retVal = 630;
                                    }
                                    else if ((inAngle2 >= 630) && (inAngle2 < 720))
                                    {
                                        retVal = 720;
                                    }
                                    return retVal;
                                }
                                double centerX = (itemEntity as DxfArc).Center.X;
                                double centerY = (itemEntity as DxfArc).Center.Y;
                                // rotate center of Arc
                                Point centerNew = rotationMatrix.Transform(new Point(centerX, centerY));
                                double radiusArc = (itemEntity as DxfArc).Radius;
                                // angle(s) of arc is kept during rotation, center may move, together with start and end points
                                // regarding angles. They are measured relatively to horizontal direction, so they may be ... 
                                // new angle = old angle+rotation angle
                                // I checked this in QCAD, it should work. Geometrically it makes sense
                                // ALSO. in DXF arc is rotated counterclockwise
                                double newStartAngle = ((itemEntity as DxfArc).StartAngle + inAngle) % 360;
                                double newEndAngle = ((itemEntity as DxfArc).EndAngle + inAngle) % 360;
                                if (newEndAngle < newStartAngle)
                                {
                                    // arc may be intersecting zero horizontal
                                    newEndAngle += 360;
                                }
                                List<Point> valuablePoints = new List<Point>();
                                Point startPoint = new Point();
                                startPoint.X = centerNew.X + Math.Cos(ConvertToRadians(newStartAngle)) * radiusArc;
                                startPoint.Y = centerNew.Y + Math.Sin(ConvertToRadians(newStartAngle)) * radiusArc;
                                valuablePoints.Add(startPoint);
                                double iteratorAngle = findNearestStraightAngle(newStartAngle);
                                while (iteratorAngle < newEndAngle)
                                {
                                    Point valuablePoint = new Point();
                                    valuablePoint.X = centerNew.X + Math.Cos(ConvertToRadians(iteratorAngle)) * radiusArc;
                                    valuablePoint.Y = centerNew.Y + Math.Sin(ConvertToRadians(iteratorAngle)) * radiusArc;
                                    valuablePoints.Add(valuablePoint);
                                    iteratorAngle += 90;
                                }
                                Point endPoint = new Point();
                                endPoint.X = centerNew.X + Math.Cos(ConvertToRadians(newEndAngle)) * radiusArc;
                                endPoint.Y = centerNew.Y + Math.Sin(ConvertToRadians(newEndAngle)) * radiusArc;
                                valuablePoints.Add(endPoint);
                                // now, let's get the ACTUAL bound box of transformed arc
                                List<Double> currentBBoxArc = new List<double>(new double[] { Double.NaN, Double.NaN, Double.NaN, Double.NaN });
                                foreach (var valuablePointArc in valuablePoints)
                                {
                                    if (Double.IsNaN(currentBBoxArc[0]) || valuablePointArc.X < currentBBoxArc[0])
                                    {
                                        currentBBoxArc[0] = valuablePointArc.X;
                                    }
                                    if (Double.IsNaN(currentBBoxArc[1]) || valuablePointArc.Y < currentBBoxArc[1])
                                    {
                                        currentBBoxArc[1] = valuablePointArc.Y;
                                    }
                                    if (Double.IsNaN(currentBBoxArc[2]) || valuablePointArc.X > currentBBoxArc[2])
                                    {
                                        currentBBoxArc[2] = valuablePointArc.X;
                                    }
                                    if (Double.IsNaN(currentBBoxArc[3]) || valuablePointArc.Y > currentBBoxArc[3])
                                    {
                                        currentBBoxArc[3] = valuablePointArc.Y;
                                    }
                                }
                                // now, merge arc bbox with general bbox
                                if (Double.IsNaN(boundBox[0]) && Double.IsNaN(boundBox[1]) && Double.IsNaN(boundBox[2]) && Double.IsNaN(boundBox[3]))
                                { //arc was first
                                    boundBox[0] = currentBBoxArc[0];
                                    boundBox[1] = currentBBoxArc[1];
                                    boundBox[2] = currentBBoxArc[2];
                                    boundBox[3] = currentBBoxArc[3];
                                }
                                else
                                {
                                    if (boundBox[0] > currentBBoxArc[0])
                                    {
                                        boundBox[0] = currentBBoxArc[0];
                                    }
                                    if (boundBox[1] > currentBBoxArc[1])
                                    {
                                        boundBox[1] = currentBBoxArc[1];
                                    }
                                    if (boundBox[2] < currentBBoxArc[2])
                                    {
                                        boundBox[2] = currentBBoxArc[2];
                                    }
                                    if (boundBox[3] < currentBBoxArc[3])
                                    {
                                        boundBox[3] = currentBBoxArc[3];
                                    }
                                }
                                break;
                            }
                    }
                }
                return boundBox;
            }

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

            boundBox = getActiveBoundBoxValues(); //include also rotation. but hmmm
            double bboxWidthPrimal = Math.Abs(boundBox[0] - boundBox[2]);
            double bboxHeightPrimal = Math.Abs(boundBox[1] - boundBox[3]);
            double scaleX = currentWidthMain / bboxWidthPrimal;
            double scaleY = currentHeightMain / bboxHeightPrimal;
            usedScale = scaleX < scaleY ? scaleX : scaleY;
            // adjust basic dimensions of canvas
            this.renderBaseDXF.Width = bboxWidthPrimal * usedScale;
            this.renderBaseDXF.Height = bboxHeightPrimal * usedScale;

            this.renderOnlyDXF.Width = bboxWidthPrimal * usedScale;
            this.renderOnlyDXF.Height = bboxHeightPrimal * usedScale;
            double usedScaleW = usedScale; double usedScaleH = usedScale;
            // for some reason mirror works weird when applied here
            /*
            if (isMirrored)
            {
                usedScaleW *= -1;
            }
            */
            double graphPlaneCenterX = this.renderOnlyDXF.Width / 2;
            double graphPlaneCenterY = this.renderOnlyDXF.Height / 2;
            // now - conjure proper transformation sequence
            // first - move center to zero
            TranslateTransform translocateOperationCenterStart = new TranslateTransform(-(boundBox[2] - boundBox[0]) /2, -(boundBox[3] - boundBox[1]) /2);
            // then - scale relatively to zero
            ScaleTransform scaleOperation = new ScaleTransform(usedScaleW, usedScaleH, 0, 0);
            // also - rotate. Rotate is performed on DXF Canvas!

            // next - move to center of screen
            TranslateTransform translocateOperationCenter = new TranslateTransform(graphPlaneCenterX, graphPlaneCenterY);

            TransformGroup groupOperation = new TransformGroup();
            groupOperation.Children.Add(translocateOperationCenterStart);
            groupOperation.Children.Add(scaleOperation);
            groupOperation.Children.Add(translocateOperationCenter);
            renderOnlyDXF.Children.Clear();
            foreach (DxfEntity entity in dxfFile.Entities)
            {
                DxfColor entityColor = entity.Color;
                int rgb = entityColor.ToRGB();
                SolidColorBrush usedColor = new SolidColorBrush(Color.FromRgb((byte)((rgb >> 16) & 0xff), (byte)((rgb >> 8) & 0xff), (byte)((rgb >> 0) & 0xff)));
                
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
                            lineGraphic.StrokeThickness =  1 / usedScale;
                            lineGraphic.RenderTransform = groupOperation;
                            this.renderOnlyDXF.Children.Add(lineGraphic);
                            break;
                        }
                    case DxfEntityType.Arc:
                        {
                            
                            DxfArc arcDxf = (DxfArc)entity;
                            // arc in dxf is counterclockwise
                            Arc arcGraphic = new Arc();
                            double correctedXCenter = arcDxf.Center.X;
                            double correctedYCenter = arcDxf.Center.Y;
                            // ayyy lmao that's a meme but it works. I have no idea why it worked, but it... uhh, it will backfire at some case
                            arcGraphic.StartAngle = UserControlScrollableDXFViewer.ConvertToRadians((arcDxf.EndAngle));
                            arcGraphic.EndAngle = UserControlScrollableDXFViewer.ConvertToRadians((arcDxf.StartAngle));
                            arcGraphic.Radius = arcDxf.Radius;
                            arcGraphic.Center = new Point(correctedXCenter, correctedYCenter);
                            arcGraphic.Stroke = Brushes.Black;
                            arcGraphic.StrokeThickness = 1 / usedScale;
                            arcGraphic.RenderTransform = groupOperation;
                            this.renderOnlyDXF.Children.Add(arcGraphic);
                            
                            break;
                        }
                }
            }

            /* ==== BOUNDARIES. USED FOR TESTING! ==== */
            /*
            Line lineGraphicB1 = new Line();
            lineGraphicB1.X1 = 0;
            lineGraphicB1.Y1 = 0;
            lineGraphicB1.X2 = bboxWidthPrimal;
            lineGraphicB1.Y2 = 0;
            lineGraphicB1.Stroke = Brushes.Lime;
            lineGraphicB1.RenderTransform = groupOperation;
            lineGraphicB1.StrokeThickness = 1 / usedScale;

            Line lineGraphicB2 = new Line();
            lineGraphicB2.X1 = 0;
            lineGraphicB2.Y1 = 0;
            lineGraphicB2.X2 = 0;
            lineGraphicB2.Y2 = bboxHeightPrimal;
            lineGraphicB2.Stroke = Brushes.DarkBlue;
            lineGraphicB2.RenderTransform = groupOperation;
            lineGraphicB2.StrokeThickness = 1 / usedScale;
            Line lineGraphicB3 = new Line();
            lineGraphicB3.X1 = 0;
            lineGraphicB3.Y1 = bboxHeightPrimal;
            lineGraphicB3.X2 = bboxWidthPrimal;
            lineGraphicB3.Y2 = bboxHeightPrimal;
            lineGraphicB3.Stroke = Brushes.DarkGreen;
            lineGraphicB3.RenderTransform = groupOperation;
            lineGraphicB3.StrokeThickness = 1 / usedScale;
            this.renderOnlyDXF.Children.Add(lineGraphicB3);
            this.renderOnlyDXF.Children.Add(lineGraphicB2);
            this.renderOnlyDXF.Children.Add(lineGraphicB1);

            Line lineGraphicC2 = new Line();
            lineGraphicC2.X1 = bboxWidthPrimal/2-5;
            lineGraphicC2.Y1 = bboxHeightPrimal / 2;
            lineGraphicC2.X2 = bboxWidthPrimal / 2 + 5;
            lineGraphicC2.Y2 = bboxHeightPrimal/2;
            lineGraphicC2.Stroke = Brushes.DarkBlue;
            lineGraphicC2.RenderTransform = groupOperation;
            lineGraphicC2.StrokeThickness = 1 / usedScale;

            this.renderOnlyDXF.Children.Add(lineGraphicC2);
            */
            //okay, so we may assign rotation to renderOnlyDxfCanvas
            // but at first we need to translate it to center of rotated bound box.... hmmm....
            applyRotationToDXFcanvas(rotationAngleDegrees, isMirrored);

            return boundBox;
        }
        
        public void applyRotationToDXFcanvas(double in_angleDeg, bool in_Mirrored)
        {
            // usedscale should NEVER be adjusted here! It was set up during the initial calculation of figure. Usedscale remains same for rotation angles
            
            List < Double> boundBoxRotated = getActiveBoundBoxValuesWithRotation(in_angleDeg);
            double bboxWidthRotated = Math.Abs(boundBoxRotated[0] - boundBoxRotated[2]);
            double bboxHeightRotated = Math.Abs(boundBoxRotated[1] - boundBoxRotated[3]);
            double rotatedCenterX = bboxWidthRotated * usedScale / 2;
            double rotatedCenterY = bboxHeightRotated * usedScale / 2;
            List<Double> boundBoxPrimal = getActiveBoundBoxValues(); // not rotated, raw
            double bboxWidthPrimal = Math.Abs(boundBoxPrimal[0] - boundBoxPrimal[2]);
            double bboxHeightPrimal = Math.Abs(boundBoxPrimal[1] - boundBoxPrimal[3]);
            double primalCenterX = bboxWidthPrimal * usedScale / 2;
            double primalCenterY = bboxHeightPrimal * usedScale / 2;

            // here I do align profile closely to boundaries [0;0]
            double translateDirectionX = (boundBoxPrimal[0] - boundBoxRotated[0])*usedScale;
            double translateDirectionY = (boundBoxPrimal[1] - boundBoxRotated[1])*usedScale;

            TransformGroup dxfCanvasTransform = new TransformGroup();            
            dxfCanvasTransform.Children.Add(new RotateTransform(in_angleDeg, this.renderBaseDXF.Width / 2, this.renderBaseDXF.Height / 2));
            dxfCanvasTransform.Children.Add(new TranslateTransform(translateDirectionX, translateDirectionY));
            if (in_Mirrored)
            {
                dxfCanvasTransform.Children.Add(new ScaleTransform(-1, 1));
                // kostelyaka. According to stack overflow, transform is not required, but... I still have to use it
                dxfCanvasTransform.Children.Add(new TranslateTransform(bboxWidthRotated*usedScale, 0));
            }
            this.renderOnlyDXF.RenderTransform = dxfCanvasTransform;
            this.renderBaseDXF.Width = bboxWidthRotated * usedScale;
            this.renderBaseDXF.Height = bboxHeightRotated * usedScale;
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
            origContentMouseDownPoint = e.GetPosition(zoomAndPanControl); //was renderBaseDXF

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
                Point curContentMousePoint = e.GetPosition(zoomAndPanControl); // was renderBaseDXF
                Vector dragOffset = (curContentMousePoint - origContentMouseDownPoint)*(1/ this.zoomAndPanControl.ContentScale);

                zoomAndPanControl.ContentOffsetX -= dragOffset.X;
                zoomAndPanControl.ContentOffsetY -= dragOffset.Y/2;

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
            zoomAndPanControl.ContentScale *= 1.15;

            foreach (var itemChild in (this.renderBaseDXF).Children)
            {
                if (itemChild is System.Windows.Shapes.Shape)
                {
                    (itemChild as System.Windows.Shapes.Shape).StrokeThickness = 1 / this.zoomAndPanControl.ContentScale * 1/usedScale;
                } else if (itemChild is Canvas)
                {
                    foreach (var itemChildInternal in (itemChild as Canvas).Children)
                    {
                        (itemChildInternal as System.Windows.Shapes.Shape).StrokeThickness = 1 / this.zoomAndPanControl.ContentScale * 1 / usedScale;
                    }
                }
            }

        }
        private void ZoomOut()
        {
            //zoomAndPanControl.ContentScale -= 0.05;
            zoomAndPanControl.ContentScale /= 1.15;
            foreach (var itemChild in (this.renderBaseDXF).Children)
            {
                if (itemChild is System.Windows.Shapes.Shape)
                {
                    (itemChild as System.Windows.Shapes.Shape).StrokeThickness = 1 / this.zoomAndPanControl.ContentScale * 1/usedScale;
                }
                else if (itemChild is Canvas)
                {
                    foreach (var itemChildInternal in (itemChild as Canvas).Children)
                    {
                        (itemChildInternal as System.Windows.Shapes.Shape).StrokeThickness = 1 / this.zoomAndPanControl.ContentScale * 1 / usedScale;
                    }
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
