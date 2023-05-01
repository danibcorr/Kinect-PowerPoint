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
    using static claseSpeech;
    // Audio
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using System.Collections.Generic;
    using System.Windows.Documents;
    using WindowsInput.Native;
    using System;
    using System.ComponentModel;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

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
        const double JointThicknessAzul = 10, JointThicknessAmarillo = 20;
        float miAlturaIzqda = 0, miAlturaDerecha = 0, miAlturaCabeza = 0;
        // Variables para el control de las diapositivas
        bool control_der = false, control_izq = false, control_puntero = false, control_elegir = false;

        private SpeechRecognitionEngine speechEngine;
        private enum Direction
        {
            empezar,
            salir,
            puntero,
            elegir,
            esta,
            inicio,
            fin
            // para añadir un nuevo comando de voz, añadir palabra aquí
        }
        private List<Span> recognitionSpans;
        private const string MediumGreyBrushKey = "MediumGreyBrush";

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

            if (null == this.sensor)
            {
                return;
            }

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

            // Inicialización sensor Audio
            RecognizerInfo ri = GetKinectRecognizer();

            if (null != ri)
            {
                recognitionSpans = new List<Span> { empezarSpan, salirSpan, punteroSpan , elegirSpan , inicioSpan , finSpan};

                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                var directions = new Choices();
                directions.Add(new SemanticResultValue("empezar", "EMPEZAR"));
                directions.Add(new SemanticResultValue("salir", "SALIR"));
                directions.Add(new SemanticResultValue("puntero", "PUNTERO"));
                directions.Add(new SemanticResultValue("elegir", "ELEGIR"));
                directions.Add(new SemanticResultValue("esta", "ESTA"));
                directions.Add(new SemanticResultValue("inicio", "INICIO"));
                directions.Add(new SemanticResultValue("fin", "FIN"));
                // para añadir un nuevo comando de voz, añadir palabra aquí

                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(directions);

                var g = new Grammar(gb);

                speechEngine.LoadGrammar(g);

                speechEngine.SpeechRecognized += SpeechRecognized;
                speechEngine.SpeechRecognitionRejected += SpeechRejected;

                speechEngine.SetInputToAudioStream(sensor.AudioSource.Start(), new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
            else
            {

            }
        }

        /// Execute shutdown tasks
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.AudioSource.Stop();
                this.sensor.Stop();
                this.sensor = null;
            }

            if (null != this.speechEngine)
            {
                this.speechEngine.SpeechRecognized -= SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected -= SpeechRejected;
                this.speechEngine.RecognizeAsyncStop();
            }
        }

        private void ClearRecognitionHighlights()
        {
            foreach (Span span in recognitionSpans)
            {
                span.Foreground = (Brush)this.Resources[MediumGreyBrushKey];
                span.FontWeight = FontWeights.Normal;
            }
        }

        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.3;

            ClearRecognitionHighlights();

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                switch (e.Result.Semantics.Value.ToString())
                {
                    // para añadir un nuevo comando de voz, añadir case aquí

                    case "EMPEZAR":
                        if(control_puntero == false)
                        {
                            // Descomentar siguiente línea si se está usando la versión instalada
                            //sim.Keyboard.KeyPress(VirtualKeyCode.F5);
                            // Descomentar siguiente línea si se está usando la versión online
                            sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.F5);
                            empezarSpan.Foreground = Brushes.DeepSkyBlue;
                            empezarSpan.FontWeight = FontWeights.Bold;
                        }
                        
                        break;

                    case "SALIR":
                        if(control_puntero == false)
                        {
                            salirSpan.Foreground = Brushes.DeepSkyBlue;
                            salirSpan.FontWeight = FontWeights.Bold;
                            sim.Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
                        }
                        break;

                    case "PUNTERO":

                        punteroSpan.Foreground = Brushes.DeepSkyBlue;
                        punteroSpan.FontWeight = FontWeights.Bold;
                        
                        if (control_puntero == false)
                        {
                            control_puntero = true;
                        }
                        else
                        {
                            control_puntero = false;
                        }

                        break;

                    case "ELEGIR":
                        if (control_puntero == false)
                        {
                            elegirSpan.Foreground = Brushes.DeepSkyBlue;
                            elegirSpan.FontWeight = FontWeights.Bold;
                            sim.Keyboard.KeyPress(VirtualKeyCode.VK_G);
                            control_elegir = true;
                        }
                        break;

                    case "ESTA":
                        if(control_elegir)
                        {
                            elegirSpan.Foreground = Brushes.DeepSkyBlue;
                            elegirSpan.FontWeight = FontWeights.Bold;
                            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                            control_elegir = false;
                        }
                        break;

                    case "INICIO":
                        if (control_puntero == false)
                        {
                            inicioSpan.Foreground = Brushes.DeepSkyBlue;
                            inicioSpan.FontWeight = FontWeights.Bold;
                            Process.Start("powerpnt.exe");
                        }
                        break;

                    case "FIN":
                        if (control_puntero == false)
                        {
                            finSpan.Foreground = Brushes.DeepSkyBlue;
                            finSpan.FontWeight = FontWeights.Bold;
                            //sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LMENU, VirtualKeyCode.F4);
                        }
                        break;
                }
            }
        }

        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            ClearRecognitionHighlights();
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
                                 ref control_izq, ref control_puntero, JointThicknessAzul, JointThicknessAmarillo, sim);
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
