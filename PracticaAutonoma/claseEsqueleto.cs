using System.Windows;
using System.Windows.Media;
using Microsoft.Kinect;
using WindowsInput.Native;
using WindowsInput;
using System.Windows.Forms;

public static class Esqueleto
{
    // Variables para el esqueleto
    /// Width of output drawing
    public static float RenderWidth = 640.0f;
    /// Height of our output drawing
    public static float RenderHeight = 480.0f;
    /// Thickness of drawn joint lines
    public static double JointThickness = 3;
    /// Thickness of body center ellipse
    public static double BodyCenterThickness = 10;
    /// Thickness of clip edge rectangles
    public static double ClipBoundsThickness = 10;
    /// Brush used to draw skeleton center point
    public static Brush centerPointBrush = Brushes.Blue;
    /// Brush used for drawing joints that are currently tracked
    public static Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
    /// Brush used for drawing joints that are currently inferred
    public static Brush inferredJointBrush = Brushes.Yellow;
    /// Pen used for drawing bones that are currently tracked
    public static Pen trackedBonePen = new Pen(Brushes.Green, 6);
    /// Pen used for drawing bones that are currently inferred
    public static Pen inferredBonePen = new Pen(Brushes.Gray, 1);
    /// Drawing group for skeleton rendering output
    public static DrawingGroup drawingGroup;
    /// Drawing image that we will display
    public static DrawingImage imageSource;

    public static bool punteroActivo = false;

    /// Draws indicators to show which edges are clipping skeleton data
    public static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
    {
        if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
        {
            drawingContext.DrawRectangle(
                Brushes.Red,
                null,
                new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
        }

        if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
        {
            drawingContext.DrawRectangle(
                Brushes.Red,
                null,
                new Rect(0, 0, RenderWidth, ClipBoundsThickness));
        }

        if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
        {
            drawingContext.DrawRectangle(
                Brushes.Red,
                null,
                new Rect(0, 0, ClipBoundsThickness, RenderHeight));
        }

        if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
        {
            drawingContext.DrawRectangle(
                Brushes.Red,
                null,
                new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
        }
    }

    public static void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext, KinectSensor sensor, ref float miAlturaIzqda,
        ref float miAlturaDerecha, ref float miAlturaCabeza, Brush brushManoAlzadaIzqda, Brush brushManoAlzadaDerecha, ref bool control_der,
        ref bool control_izq, ref bool control_puntero, double JointThicknessAzul, double JointThicknessAmarillo, InputSimulator sim)
    {
        // Render Torso
        DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter, sensor);
        DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft, sensor);
        DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight, sensor);
        DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine, sensor);
        DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter, sensor);
        DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft, sensor);
        DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight, sensor);

        // Left Arm
        DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft, sensor);
        DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft, sensor);
        DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft, sensor);

        // Right Arm
        DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight, sensor);
        DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight, sensor);
        DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight, sensor);

        // Render Joints
        foreach (Joint joint in skeleton.Joints)
        {
            Brush drawBrush = null;

            if (joint.TrackingState == JointTrackingState.Tracked)
            {
                drawBrush = trackedJointBrush;
            }
            else if (joint.TrackingState == JointTrackingState.Inferred)
            {
                drawBrush = inferredJointBrush;
            }

            double miJointThickness = JointThickness;

            if (joint.JointType == JointType.HandLeft)
            {
                if (control_puntero == false)
                {
                    miAlturaIzqda = joint.Position.Y;
                    if (punteroActivo == true)
                    {
                        punteroActivo = false;
                        sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_L);
                    }
                    
                    if (miAlturaIzqda > miAlturaCabeza)
                    {
                        drawBrush = brushManoAlzadaIzqda;
                        miJointThickness = JointThicknessAzul;

                        if (control_izq == false)
                        {
                            control_izq = true;
                            sim.Keyboard.KeyPress(VirtualKeyCode.LEFT);
                        }
                    }
                    else
                    {
                        control_izq = false;
                    }
                }
                else
                {
                    if (punteroActivo == false)
                    {
                        // Activamos la pulsacion de teclas del laser
                        sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_L);
                        punteroActivo = true;
                    }

                    // Variables que representan las dimensiones máximas del espacio 3D del esqueleto de la Kinect.
                    const float SkeletonMaxX = 0.4f; // 0.4 metros en el eje X
                    const float SkeletonMaxY = 0.3f; // 0.3 metros en el eje Y

                    float cursorX = skeleton.Joints[JointType.HandLeft].Position.X;
                    float cursorY = skeleton.Joints[JointType.HandLeft].Position.Y;
                    float scaleX = (float)SystemInformation.PrimaryMonitorSize.Width / SkeletonMaxX;
                    float scaleY = (float)SystemInformation.PrimaryMonitorSize.Height / SkeletonMaxY;
                    int scaledX = (int)(skeleton.Joints[JointType.HandLeft].Position.X * scaleX);
                    int scaledY = (int)(skeleton.Joints[JointType.HandLeft].Position.Y * scaleY) * (-1);

                    KinectMouseController.KinectMouseMethods.SendMouseInput
                        (scaledX, scaledY, (int)SystemInformation.PrimaryMonitorSize.Width,
                        (int)SystemInformation.PrimaryMonitorSize.Height, false);
                }
            }
            else if (joint.JointType == JointType.HandRight)
            {
                miAlturaDerecha = joint.Position.Y;

                if (miAlturaDerecha > miAlturaCabeza)
                {
                    drawBrush = brushManoAlzadaDerecha;
                    miJointThickness = JointThicknessAmarillo;

                    if (control_der == false)
                    {
                        control_der = true;
                        sim.Keyboard.KeyPress(VirtualKeyCode.RIGHT);
                    }
                }
                else
                {
                    control_der = false;
                }
            }
            else if (joint.JointType == JointType.Head)
            {
                miAlturaCabeza = joint.Position.Y;
            }

            if (drawBrush != null)
            {
                drawingContext.DrawEllipse(drawBrush, null, SkeletonPointToScreen(joint.Position, sensor), miJointThickness, miJointThickness);
            }
        }
    }

    public static Point SkeletonPointToScreen(SkeletonPoint skelpoint, KinectSensor sensor)
    {
        // Convert point to depth space.  
        // We are not using depth directly, but we do want the points in our 640x480 output resolution.
        DepthImagePoint depthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
        return new Point(depthPoint.X, depthPoint.Y);
    }

    public static void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1, KinectSensor sensor)
    {
        Joint joint0 = skeleton.Joints[jointType0];
        Joint joint1 = skeleton.Joints[jointType1];

        // If we can't find either of these joints, exit
        if (joint0.TrackingState == JointTrackingState.NotTracked ||
            joint1.TrackingState == JointTrackingState.NotTracked)
        {
            return;
        }

        // Don't draw if both points are inferred
        if (joint0.TrackingState == JointTrackingState.Inferred &&
            joint1.TrackingState == JointTrackingState.Inferred)
        {
            return;
        }

        // We assume all drawn bones are inferred unless BOTH joints are tracked
        Pen drawPen = inferredBonePen;
        if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
        {
            drawPen = trackedBonePen;
        }

        drawingContext.DrawLine(drawPen, SkeletonPointToScreen(joint0.Position, sensor), SkeletonPointToScreen(joint1.Position, sensor));
    }
}
