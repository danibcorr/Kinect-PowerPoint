/*
*    Código realizado por: Daniel Bazo Correa y Clara Rubio Almagro
*    Fecha: 01/05/2023
*    Asignatura de Sistemas electrónicos interactivos
*    ETSIT Universidad de Málaga
*    
*    Descripción:
*    
*    Este programa contiene una solución completa para controlar
*    presentaciones de PowerPoint utilizando la Kinect, un dispositivo
*    de detección de movimiento desarrollado por Microsoft. La
*    solución incluye una aplicación de escritorio en C# que se
*    encarga de comunicarse con la Kinect, procesar los datos de
*    movimiento y enviar comandos a PowerPoint.
*    
*    Aplica el conocimiento adquirido en la asignatura como la
*    creación de controles por gestión de voz y gestos. Además,
*    incorpora la control del ratón por gestos y el envío de
*    comandos a la aplicación.
*/

namespace Microsoft.Samples.Kinect.ColorBasics
{
    /* ******************** LIBRERÍAS ******************** */
    
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using WindowsInput;
    using static Esqueleto;
    using static RGB;
    using static claseSpeech;
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using System.Collections.Generic;
    using System.Windows.Documents;
    using WindowsInput.Native;
    using System.Diagnostics;
    
    /* ******************** FIN LIBRERÍAS ******************** */

    public partial class MainWindow : Window
    {
        /* ******************** DECLARACIÓN DE VARIABLES ******************** */
        
        InputSimulator sim = new InputSimulator();          // Variable utilizada para la simulación de pulsaciones de teclas
        private KinectSensor sensor;                        // Variable para la activación del sensor Kinect

        // VARIABLES PARA AVANZAR O RETROCEDER DIAPOSITIVAS
        private readonly Brush brushManoAlzadaIzqda = Brushes.Aquamarine;       // Color de la elipse de la mano izquierda
        private readonly Brush brushManoAlzadaDerecha = Brushes.Yellow;         // Color de la elipse de la mano derecha
        const double JointThicknessAzul = 10, JointThicknessAmarillo = 20;      // Definición del radio de las elipses
        float miAlturaIzqda = 0, miAlturaDerecha = 0, miAlturaCabeza = 0;       // Variables de control de posición inicializadas
        
        // VARIABLE PARA EL CONTROL DE LAS DIAPOSITIVAS (SEMÁFOROS)
        bool control_der = false, control_izq = false, control_puntero = false, control_elegir = false;

        // VARIABLES DE CONTROL DE SONIDO
        private SpeechRecognitionEngine speechEngine;       // Variable de control del sonido
        private enum Direction                              // enum con los controles de voz a reconocer
        {
            empezar,        // Activa el modo presentación
            salir,          // Desactiva modo presentación
            puntero,        // Activa el control del ratón y el puntero
            elegir,         // Activa modo miniaturas de diapositivas
            esta,           // Para elegir la diapositiva a la que ir en modo miniatura
            inicio,         // Abre el programa
            fin             // Cierra el programa
        }
        private List<Span> recognitionSpans;                                // Define los span que se ven en la interfaz visual
        private const string MediumGreyBrushKey = "MediumGreyBrush";        // Define el color de los span en modo desactivados

        /* ******************** FIN DECLARACIÓN DE VARIABLES ******************** */

        public MainWindow()
        {
            InitializeComponent();      // Inicializa una nueva instancia de la clase MainWindow
        }

        /// Execute startup tasks
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            
            drawingGroup = new DrawingGroup();                  // Coloca Esqueleto en la ventana
            imageSource = new DrawingImage(drawingGroup);       // Crea una imagen original que podemos usar como control de imagen
            kinectEsqueleto.Source = imageSource;               // Muestra el dibujo usando la imagen original

            // Mira todos los sensores y empieza por el que se haya conectado primero
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            // Comienza el sensor
            if (null != this.sensor)
            {
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

            //  INICIALIZACIÓN CÁMARA RGB
            this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);     // Enciende el flujo de color para recibir los marcos de colores
            colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];           // Asigna espacio para poner los píxeles que recibe
            colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);       // Este es el mapa de bits que mostraremos por pantalla
            this.sensor.ColorFrameReady += this.SensorColorFrameReady;                      // Añade un manejador de eventos para ser llamado cuando haya un nuevo píxel de color

            // INICIALIZACIÓN ESQUELETO
            this.sensor.SkeletonStream.Enable();                                    // Enciende el esqueleto para recibir marcos de esqueleto
            this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;        // Añade un manejador de eventos para ser llamado cuando haya un nuevo píxel de dato

            // INICIALIZACIÓN SENSOR DE AUDIO
            RecognizerInfo ri = GetKinectRecognizer();

            if (null != ri)
            {
                /* ******************** GESTIÓN DE AUDIO ******************** */

                // GUARDA ESPACIO PARA LOS SPAN DE LAS PALABRAS
                recognitionSpans = new List<Span> { empezarSpan, salirSpan, punteroSpan , elegirSpan , inicioSpan , finSpan};

                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                // ASIGNA UN ESTADO A CADA PALABRA QUE HA DE RECONOCER
                var directions = new Choices();
                directions.Add(new SemanticResultValue("empezar", "EMPEZAR"));
                directions.Add(new SemanticResultValue("salir", "SALIR"));
                directions.Add(new SemanticResultValue("puntero", "PUNTERO"));
                directions.Add(new SemanticResultValue("elegir", "ELEGIR"));
                directions.Add(new SemanticResultValue("esta", "ESTA"));
                directions.Add(new SemanticResultValue("inicio", "INICIO"));
                directions.Add(new SemanticResultValue("fin", "FIN"));
                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(directions);
                var g = new Grammar(gb);

                // CARGA LA GRAMÁTICA REQUERIDA
                speechEngine.LoadGrammar(g);
                speechEngine.SpeechRecognized += SpeechRecognized;
                speechEngine.SpeechRecognitionRejected += SpeechRejected;
                speechEngine.SetInputToAudioStream(sensor.AudioSource.Start(), new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                speechEngine.RecognizeAsync(RecognizeMode.Multiple);

                /* ******************** FIN GESTIÓN DE AUDIO ******************** */
            }
            else
            {

            }
        }


        // EJECUTA LAS TAREAS DE FINALIZACIÓN DEL PROGRAMA
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

        // FUNCIÓN QUE "DESACTIVA" LOS SPAN
        private void ClearRecognitionHighlights()
        {
            foreach (Span span in recognitionSpans)
            {
                span.Foreground = (Brush)this.Resources[MediumGreyBrushKey];        // Pone la palabra en gris
                span.FontWeight = FontWeights.Normal;                               // Disminuye el tamaño de fuente
            }
        }

        // MANEJADOR DE EVENTOS DE LAS PALABRAS
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            const double ConfidenceThreshold = 0.3;     // Velocidad a la que el sensor reconoce las palabras (umbral para no error)
            ClearRecognitionHighlights();               // Desactiva los span que hayan sido activados anteriormente

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                switch (e.Result.Semantics.Value.ToString())
                {
                    // COMIENZA EL MODO PRESENTACIÓN
                    case "EMPEZAR":

                        if(control_puntero == false)
                        {
                            // Descomentar siguiente línea si se está usando la versión instalada
                            sim.Keyboard.KeyPress(VirtualKeyCode.F5);                                 // Simulación de pulsación de tecla F5
                            // Descomentar siguiente línea si se está usando la versión online
                            //sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.F5);  // Simulación de pulsación de teclas CONTROL y F5
                            empezarSpan.Foreground = Brushes.DeepSkyBlue;                               // Pone azul el span correspondiente
                            empezarSpan.FontWeight = FontWeights.Bold;                                  // Aumenta la fuente del span
                        }
                        
                        break;

                    // SALE DEL MODO PRESENTACIÓN
                    case "SALIR":

                        if(control_puntero == false)
                        {
                            salirSpan.Foreground = Brushes.DeepSkyBlue;         // Pone azul el span correspondiente
                            salirSpan.FontWeight = FontWeights.Bold;            // Aumenta la fuente del span
                            sim.Keyboard.KeyPress(VirtualKeyCode.ESCAPE);       // Simulación de pulsación de tecla ESC
                        }

                        break;

                    // ACTIVA/DESACTIVA EL MODO PUNTERO
                    case "PUNTERO":

                        punteroSpan.Foreground = Brushes.DeepSkyBlue;           // Pone azul el span correspondiente
                        punteroSpan.FontWeight = FontWeights.Bold;              // Aumenta la fuente del span

                        // SI ESTABA ACTIVADO EL MODO PUNTERO SE DESACTIVA Y VICEVERSA
                        if (control_puntero == false)
                        {
                            control_puntero = true;
                        }
                        else
                        {
                            control_puntero = false;
                        }

                        break;

                    // ACTIVA EL MODO MINIATURA
                    case "ELEGIR":

                        if (control_puntero == false)
                        {
                            elegirSpan.Foreground = Brushes.DeepSkyBlue;        // Pone azul el span correspondiente
                            elegirSpan.FontWeight = FontWeights.Bold;           // Aumenta la fuente del span
                            sim.Keyboard.KeyPress(VirtualKeyCode.VK_G);         // Simulación de pulsación de tecla G
                            control_elegir = true;                              // Avisa de que está en modo miniatura
                        }

                        break;

                    // ELIGE LA DIAPOSITIVA EN EL MODO MINIATURA
                    case "ESTA":

                        // SOLO EJECUTA SI ESTÁ EN MODO MINIATURA
                        if(control_elegir == true && control_puntero == false)
                        {
                            elegirSpan.Foreground = Brushes.DeepSkyBlue;        // Pone azul el span correspondiente
                            elegirSpan.FontWeight = FontWeights.Bold;           // Aumenta la fuente del span
                            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);       // Simulación de pulsación de tecla ENTER
                            control_elegir = false;                             // Avisa de que ya no está en modo miniatura
                        }

                        break;

                    // COMIENZA EL PROGRAMA
                    case "INICIO":

                        if (control_puntero == false)
                        {
                            inicioSpan.Foreground = Brushes.DeepSkyBlue;        // Pone azul el span correspondiente
                            inicioSpan.FontWeight = FontWeights.Bold;           // Aumenta la fuente del span
                            Process.Start("powerpnt.exe");                      // Comienza el proceso que activa el programa
                        }

                        break;

                    // CIERRA EL PROGRAMA
                    case "FIN":

                        if (control_puntero == false)
                        {
                            finSpan.Foreground = Brushes.DeepSkyBlue;           // Pone azul el span correspondiente
                            finSpan.FontWeight = FontWeights.Bold;              // Aumenta la fuente del span
                            //sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LMENU, VirtualKeyCode.F4);      // Simulación de pulsación de teclas ALT y F4
                        }

                        break;
                }
            }
        }

        // SI NO RECONOCE PALABRA LIMPIA LOS SPAN
        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            ClearRecognitionHighlights();
        }


        /// MANEJADOR DE EVENTOS DEL SENSOR COLORFRAMEREADY
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

        // MANEJADOR DE EVENTOS DEL SENSOR
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];     // Guarda espacio para un nuevo esqueleto

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            // CONTROL DEL DIBUJO DEL ESQUELETO
            using (DrawingContext dc = drawingGroup.Open())
            {
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));       // Dibuja un fondo transparente para aplicar el tamaño de renderización

                if (skeletons.Length != 0)      // Si ve el esqueleto
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        // DIBUHA EL ESQUELETO DEPENDIENDO DE LA POSICIÓN DE LAS MANOS Y EL MODO EN EL QUE ESTÉ
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

                drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }
    }
}
