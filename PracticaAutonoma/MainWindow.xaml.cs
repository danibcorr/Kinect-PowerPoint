namespace Microsoft.Samples.Kinect.ColorBasics
{
    // Librerias a utilizar
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using WindowsInput;
    using static Esqueleto;
    using static RGB;

    public partial class MainWindow : Window
    {
        // Variables código principal
        // Variable utilizada para la simulación de pulsaciones de teclas
        InputSimulator sim = new InputSimulator();
        // Variable para la activación del sensor Kinect
        private KinectSensor sensor;
        // Definición de variables para avanzar o retroceder diapositivas
        // Definición de los colores para las elipses de las manos
        private readonly Brush brushManoAlzadaIzqda = Brushes.Aquamarine;
        private readonly Brush brushManoAlzadaDerecha = Brushes.Yellow;
        // Definición de los radios de las elipses, el valor es el radio
        private const double JointThicknessAzul = 10, JointThicknessAmarillo = 20;
        private float miAlturaIzqda = 0, miAlturaDerecha = 0, miAlturaCabeza = 0;
        // Variables para el control de las diapositivas
        bool control_der = false, control_izq = false;


        /// Initializes a new instance of the MainWindow class.
        public MainWindow()
        {
            InitializeComponent();
        }

  
        /// Execute startup tasks
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Colocar Esqueleto en la ventana
            // Create the drawing group we'll use for drawing
            drawingGroup = new DrawingGroup();
            // Create an image source that we can use in our image control
            imageSource = new DrawingImage(drawingGroup);
            // Display the drawing using our image control
            kinectEsqueleto.Source = imageSource;

            // Look through all sensors and start the first connected one.
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Inicialización Camara RGB
                // Turn on the color stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                // Allocate space to put the pixels we'll receive
                colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                // This is the bitmap we'll display on-screen
                colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                // Add an event handler to be called whenever there is new color frame data
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;

                // Inicialización Esqueleto
                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();
                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }
        }

        /// Execute shutdown tasks
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        
        /// Event handler for Kinect sensor's ColorFrameReady event
        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null) return;

                byte[] colorData = new byte[colorFrame.PixelDataLength];

                colorFrame.CopyPixelDataTo(colorData);

                kinectRGB.Source = BitmapSource.Create(
                    colorFrame.Width, colorFrame.Height,
                    96, 96,
                    PixelFormats.Bgr32,
                    null,
                    colorData,
                    colorFrame.Width * colorFrame.BytesPerPixel);
            }
        }

        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            DrawBonesAndJoints(skel, dc, sensor, ref miAlturaIzqda,
                                 ref miAlturaDerecha, ref miAlturaCabeza, brushManoAlzadaIzqda, brushManoAlzadaDerecha, ref control_der,
                                 ref control_izq, JointThicknessAzul, JointThicknessAmarillo, sim);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            centerPointBrush,
                            null,
                            SkeletonPointToScreen(skel.Position, sensor),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }

                    dc.Close();
                }

                // prevent drawing outside of our render area
                drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }
    }
}