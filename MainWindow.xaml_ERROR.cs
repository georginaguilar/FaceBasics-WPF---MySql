//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.FaceBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Media.Media3D;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Face;
   // using System.Web.Script.Serialization;
    
    using MySql.Data.MySqlClient;
    //using StackExchange.Redis;
    using MongoDB.Bson;
    using MongoDB.Driver;
    using System.Text;


    //agregado
    class MyFace
    {
        public CameraSpacePoint eyeright;
        public CameraSpacePoint eyeleft;
        public CameraSpacePoint nose;
        public CameraSpacePoint mouth;

    }
        /// <summary>
        /// Interaction logic for MainWindow
        /// </summary>
        public partial class MainWindow : Window, INotifyPropertyChanged
        {

            //prueba de conexion
            //public ConnectionMultiplexer connectionRedis;
            //public IDatabase clientRedis;

            /// <summary>
            /// Thickness of face bounding box and face points
            /// Grosor de la caja de la cara de delimitación y puntos de la cara
            /// </summary>
            private const double DrawFaceShapeThickness = 8;

            /// <summary>
            /// Font size of face property text 
            /// Tamaño de la fuente de la cara de texto propiedad
            /// </summary>
            private const double DrawTextFontSize = 30;

            /// <summary>
            /// Radius of face point circle
            /// Radio de cara punto de círculo
            /// </summary>
            private const double FacePointRadius = 1.0;

            /// <summary>
            /// Text layout offset in X axis
            /// Diseño del texto compensado en X axiss
            /// </summary>
            private const float TextLayoutOffsetX = -0.1f;

            /// <summary>
            /// Text layout offset in Y axis
            /// Desplazamiento en el eje Y el diseño de texto
            /// </summary>
            private const float TextLayoutOffsetY = -0.15f;

            /// <summary>
            /// Face rotation display angle increment in degrees
            /// Cara pantalla rotación incremento de ángulo en grados
            /// </summary>
            private const double FaceRotationIncrementInDegrees = 5.0;

            /// <summary>
            /// Formatted text to indicate that there are no bodies/faces tracked in the FOV
            /// Texto con formato para indicar que no hay cuerpos / caras seguidas en el campo de visión, es decir que no detecta alguna cara
            /// </summary>
            private FormattedText textFaceNotTracked = new FormattedText(
                            "No bodies or faces are tracked ...",
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface("Georgia"),
                            DrawTextFontSize,
                            Brushes.White);

            /// <summary>
            /// Text layout for the no face tracked message
            /// Diseño del texto para el mensaje sin rostro rastreado
            /// </summary>
            private Point textLayoutFaceNotTracked = new Point(10.0, 10.0);

            /// <summary>
            /// Drawing group for body rendering output
            /// Grupo Dibujo para la salida del cuerpo de representación
            /// </summary>
            private DrawingGroup drawingGroup;

            /// <summary>
            /// Drawing image that we will display
            /// Dibujo imagen que se mostrará en pantalla
            /// </summary>
            private DrawingImage imageSource;

            /// <summary>
            /// Active Kinect sensor
            /// Sensor Kinect Activo
            /// </summary>
            private KinectSensor kinectSensor = null;

            /// <summary>
            /// Coordinate mapper to map one type of point to another
            /// Coordinar mapeador para asignar un tipo de punto a otro
            /// </summary>
            private CoordinateMapper coordinateMapper = null;

            /// <summary>
            /// Reader for body frames
            /// Reader para Framess cuerpo
            /// </summary>
            private BodyFrameReader bodyFrameReader = null;

            /// <summary>
            /// Array to store bodies
            /// Array para almacenamiento de cuerpos
            /// </summary>
            private Body[] bodies = null;

            /// <summary>
            /// Number of bodies tracked
            /// Número de cuerpos seguimiento
            /// </summary>
            private int bodyCount;

            /// <summary>
            /// Face frame sources
            /// almacenamiento de los frames de la cara
            /// </summary>
            private FaceFrameSource[] faceFrameSources = null;

            /// <summary>
            /// Face frame readers
            /// Lectores marco de la cara
            /// </summary>
            private FaceFrameReader[] faceFrameReaders = null;

            /// <summary>
            /// Storage for face frame results
            /// Almacenamiento de los resultados del marco cara
            /// </summary>
            private FaceFrameResult[] faceFrameResults = null;

            /// <summary>
            /// Width of display (color space)
            /// Ancho de pantalla ( espacio de color )
            /// </summary>
            private int displayWidth;

            /// <summary>
            /// Height of display (color space)
            /// Altura de la pantalla ( espacio de color )
            /// </summary>
            private int displayHeight;

            /// <summary>
            /// Display rectangle
            /// Pantalla rectángulo
            /// </summary>
            private Rect displayRect;

            /// <summary>
            /// List of brushes for each face tracked
            /// Lista de cepillos para cada cara de seguimiento
            /// </summary>
            private List<Brush> faceBrush;

            /// <summary>
            /// Current status text to display
            /// Texto de estado actual para mostrar
            /// </summary>
            private string statusText = null;

            /// <summary>
            /// Initializes a new instance of the MainWindow class.
            /// Inicializa una nueva instancia de la clase MainWindow .
            /// </summary>
            public MainWindow()
            {
                // one sensor is currently supported
                // Un sensor está soportado actualmente
                this.kinectSensor = KinectSensor.GetDefault();

                // get the coordinate mapper
                // Obtener el asignador de coordenadas
                this.coordinateMapper = this.kinectSensor.CoordinateMapper;

                // get the color frame details
                // Obtener los detalles del marco de color
                FrameDescription frameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;

                // set the display specifics
                // Establecer los detalles de visualización
                this.displayWidth = frameDescription.Width;
                this.displayHeight = frameDescription.Height;
                this.displayRect = new Rect(0.0, 0.0, this.displayWidth, this.displayHeight);

                // open the reader for the body frames
                // Abrir el lector para los marcos del cuerpo
                this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

                // wire handler for body frame arrival
                // Manejador de alambre para la llegada estructura del cuerpo
                this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;

                // set the maximum number of bodies that would be tracked by Kinect
                // Establecer el número máximo de cuerpos que serían seguidos por Kinect
                this.bodyCount = this.kinectSensor.BodyFrameSource.BodyCount;
                
                // allocate storage to store body objects
                // Asignar almacenamiento para almacenar objetos corporales(cuerpos)
                this.bodies = new Body[this.bodyCount];

                
                // specify the required face frame results
                // Especificar los resultados del marco cara requeridos
                FaceFrameFeatures faceFrameFeatures =
                    FaceFrameFeatures.BoundingBoxInColorSpace
                    | FaceFrameFeatures.PointsInColorSpace
                    | FaceFrameFeatures.RotationOrientation
                    | FaceFrameFeatures.FaceEngagement
                    | FaceFrameFeatures.Glasses
                    | FaceFrameFeatures.Happy
                    | FaceFrameFeatures.LeftEyeClosed
                    | FaceFrameFeatures.RightEyeClosed
                    | FaceFrameFeatures.LookingAway
                    | FaceFrameFeatures.MouthMoved
                    | FaceFrameFeatures.MouthOpen;


                //ORIGINAL
                var json = statusText;


                //var json = new JavaScriptSerializer().Serialize(fac);
                //label1.Content = String.Format("Face: {"+ cont +"}", json);

                //var fac = new FaceFrameFeatures();
                ////para imprimir txt
                //string filename = @"C:\Users\Georgina Aguilar\Documentos\Prueba.txt";
                //StreamWriter writer = File.CreateText(filename);

                //writer.WriteLineAsync(fac);
                //writer.Close();

                // create a face frame source + reader to track each face in the FOV
                // Crear una fuente marco frontal + lector a realizar un seguimiento de cada cara en el campo de visión
                this.faceFrameSources = new FaceFrameSource[this.bodyCount];
                this.faceFrameReaders = new FaceFrameReader[this.bodyCount];
                for (int i = 0; i < this.bodyCount; i++)
                {
                    // create the face frame source with the required face frame features and an initial tracking Id of 0
                    // Crear la fuente de marco de la cara con las características del marco cara requeridos y un ID de seguimiento inicial de 0
                    this.faceFrameSources[i] = new FaceFrameSource(this.kinectSensor, 0, faceFrameFeatures);

                    // open the corresponding reader
                    // Abre el lector correspondiente
                    this.faceFrameReaders[i] = this.faceFrameSources[i].OpenReader();
                }

                // allocate storage to store face frame results for each face in the FOV
                // Asignar almacenamiento a los resultados del marco almacenamiento de cara para cada cara en el campo de visión
                this.faceFrameResults = new FaceFrameResult[this.bodyCount];

                //imprimir datos para ver si funcionan labels
                //label2.Content = bodyCount;
                //label3.Content = ;
                //label4.Content = ;

                //MessageBox.Show("MOSTRANDO EN PANTALLA");

                // populate face result colors - one for each face index
                // poplamente los colores resultantes de la cara - uno para cada índice de cara
                this.faceBrush = new List<Brush>()
            {
                Brushes.White, 
                Brushes.Orange,
                Brushes.Green,
                Brushes.Red,
                Brushes.LightBlue,
                Brushes.Yellow
            };

                // set IsAvailableChanged event notifier
                // Conjunto IsAvailableChanged evento notificador
                this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

                // open the sensor
                // Abre el sensor
                this.kinectSensor.Open();

                // set the status text
                // Establecer el texto de estado
                this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                                : Properties.Resources.NoSensorStatusText;

                // Create the drawing group we'll use for drawing
                // Crear el grupo de dibujo usaremos para dibujar
                this.drawingGroup = new DrawingGroup();

                // Create an image source that we can use in our image control
                // Crea una fuente de imagen que podemos utilizar en nuestro control de imagen
                this.imageSource = new DrawingImage(this.drawingGroup);

                // use the window object as the view model in this simple example
                // Utilizar el objeto de ventana que el modelo de vista en este ejemplo simple
                this.DataContext = this;

                // initialize the components (controls) of the window
                // Inicializar los componentes (controles) de la ventana
                this.InitializeComponent();
            }

            /// <summary>
            /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
            /// /// Evento INotifyPropertyChangedPropertyChanged para permitir controles de la ventana se unan a los datos cambiante
            /// </summary>
            public event PropertyChangedEventHandler PropertyChanged;

            /// <summary>
            /// Gets the bitmap to display
            /// Obtiene el mapa de bits para mostrar
            /// </summary>
            public ImageSource ImageSource
            {
                get
                {
                    return this.imageSource;
                }
            }

            /// <summary>
            /// Gets or sets the current status text to display
            /// /// Obtiene o establece el texto de estado actual para mostrar
            /// </summary>
            public string StatusText
            {
                get
                {
                    return this.statusText;
                }

                set
                {
                    if (this.statusText != value)
                    {
                        this.statusText = value;

                        // notify any bound elements that the text has changed
                        // Notificar cualesquiera elementos unidos que el texto ha cambiado
                        if (this.PropertyChanged != null)
                        {
                            this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                        }
                    }
                }
            }

            /// <summary>
            /// Converts rotation quaternion to Euler angles 
            /// And then maps them to a specified range of values to control the refresh rate
            /// Convierte cuaternión rotación de los ángulos de Euler
            /// Y luego los asigna a un determinado rango de valores para controlar la frecuencia de actualización
            /// </summary>
            /// <param name="rotQuaternion">face rotation quaternion(Nuestros amigos los quaterniones son usados para representar rotacione)</param>
            /// <param name="pitch">rotation about the X-axis</param>
            /// <param name="yaw">rotation about the Y-axis</param>
            /// <param name="roll">rotation about the Z-axis</param>
            private static void ExtractFaceRotationInDegrees(Vector4 rotQuaternion, out int pitch, out int yaw, out int roll)
            {
                double x = rotQuaternion.X;
                double y = rotQuaternion.Y;
                double z = rotQuaternion.Z;
                double w = rotQuaternion.W;

                // convert face rotation quaternion to Euler angles in degrees
                // Convertir cara rotación cuaternión a ángulos de Euler en grados
                double yawD, pitchD, rollD;
                pitchD = Math.Atan2(2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z)) / Math.PI * 180.0;
                yawD = Math.Asin(2 * ((w * y) - (x * z))) / Math.PI * 180.0;
                rollD = Math.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z)) / Math.PI * 180.0;

                // clamp the values to a multiple of the specified increment to control the refresh rate
                // Sujetar los valores a un múltiplo de la incremento especificado para controlar la frecuencia de actualización
                double increment = FaceRotationIncrementInDegrees;
                pitch = (int)(Math.Floor((pitchD + ((increment / 2.0) * (pitchD > 0 ? 1.0 : -1.0))) / increment) * increment);
                yaw = (int)(Math.Floor((yawD + ((increment / 2.0) * (yawD > 0 ? 1.0 : -1.0))) / increment) * increment);
                roll = (int)(Math.Floor((rollD + ((increment / 2.0) * (rollD > 0 ? 1.0 : -1.0))) / increment) * increment);
            }

            /// <summary>
            /// Execute start up tasks
            /// Execute en marcha tareas
            /// </summary>
            /// <param name="sender">object sending the event</param>
            /// <param name="e">event arguments</param>
            private void MainWindow_Loaded(object sender, RoutedEventArgs e)
            {
                for (int i = 0; i < this.bodyCount; i++)
                {
                    if (this.faceFrameReaders[i] != null)
                    {
                        // wire handler for face frame arrival
                        // Manejador de alambre para la llegada marco de la cara
                        this.faceFrameReaders[i].FrameArrived += this.Reader_FaceFrameArrived;
                    }
                }

                if (this.bodyFrameReader != null)
                {
                    // wire handler for body frame arrival
                    // Manejador de alambre para la llegada estructura corporal
                    this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;
                }
            }

            /// <summary>
            /// Execute shutdown tasks
            /// Ejecutar tareas de apagado
            /// </summary>
            /// <param name="sender">object sending the event</param>
            /// <param name="e">event arguments</param>
            private void MainWindow_Closing(object sender, CancelEventArgs e)
            {
                for (int i = 0; i < this.bodyCount; i++)
                {
                    if (this.faceFrameReaders[i] != null)
                    {
                        // FaceFrameReader is IDisposable
                        this.faceFrameReaders[i].Dispose();
                        this.faceFrameReaders[i] = null;
                    }

                    if (this.faceFrameSources[i] != null)
                    {
                        // FaceFrameSource is IDisposable
                        this.faceFrameSources[i].Dispose();
                        this.faceFrameSources[i] = null;
                    }
                }

                if (this.bodyFrameReader != null)
                {
                    // BodyFrameReader is IDisposable
                    this.bodyFrameReader.Dispose();
                    this.bodyFrameReader = null;
                }

                if (this.kinectSensor != null)
                {
                    this.kinectSensor.Close();
                    this.kinectSensor = null;
                }
            }

            /// <summary>
            /// Handles the face frame data arriving from the sensor
            /// Maneja los datos del marco de la cara que llegan desde el sensor
            /// </summary>
            /// <param name="sender">object sending the event</param>
            /// <param name="e">event arguments</param>
            private void Reader_FaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
            {
                using (FaceFrame faceFrame = e.FrameReference.AcquireFrame())
                {
                    if (faceFrame != null)
                    {
                        // get the index of the face source from the face source array
                        // Obtener el índice de la fuente de la cara de la matriz de origen cara
                        int index = this.GetFaceSourceIndex(faceFrame.FaceFrameSource);

                        // check if this face frame has valid face frame results
                        // Comprobar si este marco frontal tiene validez resultsr marco de la cara
                        if (this.ValidateFaceBoxAndPoints(faceFrame.FaceFrameResult))
                        {
                            // store this face frame result to draw later
                            // Almacenar este resultado marco de la cara para sacar adelante
                            this.faceFrameResults[index] = faceFrame.FaceFrameResult;
                        }
                        else
                        {
                            // indicates that the latest face frame result from this reader is invalid
                            // Indica que el último resultado marco de la cara de este lector no es valido
                            this.faceFrameResults[index] = null;
                        }
                    }
                }
            }

            /// <summary>
            /// Returns the index of the face frame source
            /// Devuelve el índice de la fuente de marco de la cara
            /// </summary>
            /// <param name="faceFrameSource">the face frame source</param>
            /// <returns>the index of the face source in the face source array</returns>
            /// < retornos > el índice de la fuente de la cara en la matriz de origen cara </ retornos >
            private int GetFaceSourceIndex(FaceFrameSource faceFrameSource)
            {
                int index = -1;

                for (int i = 0; i < this.bodyCount; i++)
                {
                    if (this.faceFrameSources[i] == faceFrameSource)
                    {
                        index = i;
                        break;
                    }
                }

                return index;
            }

            /// <summary>
            /// Handles the body frame data arriving from the sensor
            /// Maneja los datos de caja de que lleguen desde el sensor
            /// </summary>
            /// <param name="sender">object sending the event</param>
            /// <param name="e">event arguments</param>
            private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
            {
                using (var bodyFrame = e.FrameReference.AcquireFrame())
                {
                    if (bodyFrame != null)
                    {
                        // update body data
                        // Actualización de datos del cuerpo
                        bodyFrame.GetAndRefreshBodyData(this.bodies);

                        using (DrawingContext dc = this.drawingGroup.Open())
                        {
                            // draw the dark background
                            // Dibujar el fondo oscuro
                            dc.DrawRectangle(Brushes.Black, null, this.displayRect);

                            bool drawFaceResult = false;

                            // iterate through each face source
                            // Iterar a través de cada fuente de la cara
                            for (int i = 0; i < this.bodyCount; i++)
                            {
                                // check if a valid face is tracked in this face source
                                // Comprobar si una cara válida se realiza un seguimiento de esta fuente de la cara
                                if (this.faceFrameSources[i].IsTrackingIdValid)
                                {
                                    // check if we have valid face frame results
                                    // Comprobar si tenemos resultados frame cara válidos
                                    if (this.faceFrameResults[i] != null)
                                    {
                                        // draw face frame results
                                        // Dibujar resultados frame cara
                                        this.DrawFaceFrameResults(i, this.faceFrameResults[i], dc);

                                        if (!drawFaceResult)
                                        {
                                            drawFaceResult = true;
                                        }
                                    }
                                }
                                else
                                {
                                    // check if the corresponding body is tracked 
                                    // Comprobar si el organismo correspondiente se rastrea
                                    if (this.bodies[i].IsTracked)
                                    {
                                        // update the face frame source to track this body
                                        // Actualizar el origen de marco frontal para realizar un seguimiento de este cuerpo
                                        this.faceFrameSources[i].TrackingId = this.bodies[i].TrackingId;
                                    }
                                }

                                //para imprimir txt
                                //string filename = @"C:\Users\MCC1\Documents\Prueba.txt";
                                //StreamWriter writer = File.CreateText(filename);

                                //writer.WriteLineAsync(json);
                                //writer.Close();

                                //                            StreamWriter writer = File.CreateText("newfile.txt");
                                //;                            await writer.WriteLineAsync("Body: \n {0}  \n", json);

                            }

                            if (!drawFaceResult)
                            {
                                // if no faces were drawn then this indicates one of the following:
                                // a body was not tracked 
                                // a body was tracked but the corresponding face was not tracked
                                // a body and the corresponding face was tracked though the face box or the face points were not valid
                                // si no hay rostros se elaboraron entonces esto indica una de las siguientes :
                                // Un cuerpo no fue localizado
                                // Un cuerpo fue localizado pero la cara correspondiente no fue rastreado
                                // Un cuerpo y la cara correspondiente fue localizado cuando el cuadro de la cara o de los puntos de la cara no eran válidas
                                dc.DrawText(
                                    this.textFaceNotTracked,
                                    this.textLayoutFaceNotTracked);
                            }

                            this.drawingGroup.ClipGeometry = new RectangleGeometry(this.displayRect);
                        }
                    }
                }
            }

            /// <summary>
            /// Draws face frame results
            /// Dibuja resultados frame cara
            /// </summary>
            /// <param name="faceIndex">the index of the face frame corresponding to a specific body in the FOV</param>
            /// <param name="faceResult">container of all face frame results</param>
            /// <param name="drawingContext">drawing context to render to</param>
            private void DrawFaceFrameResults(int faceIndex, FaceFrameResult faceResult, DrawingContext drawingContext)
            {
                // choose the brush based on the face index
                // Elegir el cepillo sobre la base del índice de cara
                Brush drawingBrush = this.faceBrush[0];
                if (faceIndex < this.bodyCount)
                {
                    drawingBrush = this.faceBrush[faceIndex];
                }

                Pen drawingPen = new Pen(drawingBrush, DrawFaceShapeThickness);

                // draw the face bounding box
                // Dibujar el cuadro delimitador cara
                var faceBoxSource = faceResult.FaceBoundingBoxInColorSpace;
                Rect faceBox = new Rect(faceBoxSource.Left, faceBoxSource.Top, faceBoxSource.Right - faceBoxSource.Left, faceBoxSource.Bottom - faceBoxSource.Top);
                drawingContext.DrawRectangle(null, drawingPen, faceBox);

                if (faceResult.FacePointsInColorSpace != null)
                {
                    // draw each face point
                    // Dibujar cada punto de la cara
                    foreach (PointF pointF in faceResult.FacePointsInColorSpace.Values)
                    {
                        drawingContext.DrawEllipse(null, drawingPen, new Point(pointF.X, pointF.Y), FacePointRadius, FacePointRadius);
                    }
                }
               
                string faceText = string.Empty;

                // extract each face property information and store it in faceText
                // Extraer información de cada propiedad face y almacenarlo en faceText
                if (faceResult.FaceProperties != null)
                {
                    foreach (var item in faceResult.FaceProperties)
                    {
                        faceText += item.Key.ToString() + " : ";

                        // consider a "maybe" as a "no" to restrict 
                        // the detection result refresh rate
                        // Considerar un "tal vez ", como un "no" para restringir
                        // El resultado frecuencia de actualización de detección
                        if (item.Value == DetectionResult.Maybe)
                        {
                            faceText += DetectionResult.Maybe + "\n";
                        }
                        else
                        {
                            faceText += item.Value.ToString() + "\n";
                        }
                    }
                }

                // extract face rotation in degrees as Euler angles
                // Extracto de la cara de rotación en grados como ángulos de Euler
                if (faceResult.FaceRotationQuaternion != null)
                {
                    int pitch, yaw, roll;
                    ExtractFaceRotationInDegrees(faceResult.FaceRotationQuaternion, out pitch, out yaw, out roll);
                    faceText += "FaceYaw : " + yaw + "\n" +
                                "FacePitch : " + pitch + "\n" +
                                "FacenRoll : " + roll + "\n";
                }

                // render the face property and face rotation information
                // Hacer que la información de propiedades de la cara y la rotación de la cara
                Point faceTextLayout;
                if (this.GetFaceTextPositionInColorSpace(faceIndex, out faceTextLayout))
                {
                    drawingContext.DrawText(
                            new FormattedText(
                                faceText,
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                new Typeface("Georgia"),
                                DrawTextFontSize,
                                drawingBrush),
                            faceTextLayout);
                }
            }

            /// <summary>
            /// Computes the face result text position by adding an offset to the corresponding 
            /// body's head joint in camera space and then by projecting it to screen space
            /// Calcula la posición del texto resultado cara al agregar un desplazamiento a la correspondiente
            /// Conjunta cabeza del cuerpo en el espacio de la cámara y luego proyectándola al espacio de la pantalla
            /// </summary>
            /// <param name="faceIndex">the index of the face frame corresponding to a specific body in the FOV</param>
            /// <param name="faceTextLayout">the text layout position in screen space</param>
            /// <returns>success or failure</returns>
            private bool GetFaceTextPositionInColorSpace(int faceIndex, out Point faceTextLayout)
            {
                faceTextLayout = new Point();
                bool isLayoutValid = false;

                Body body = this.bodies[faceIndex];
                if (body.IsTracked)
                {
                    var headJoint = body.Joints[JointType.Head].Position;

                    CameraSpacePoint textPoint = new CameraSpacePoint()
                    {
                        X = headJoint.X + TextLayoutOffsetX,
                        Y = headJoint.Y + TextLayoutOffsetY,
                        Z = headJoint.Z
                    };

                    //label2.Content = headJoint.X;
                    //label3.Content = headJoint.Y;
                    //label4.Content = headJoint.Z;

                    ColorSpacePoint textPointInColor = this.coordinateMapper.MapCameraPointToColorSpace(textPoint);

                    faceTextLayout.X = textPointInColor.X;
                    faceTextLayout.Y = textPointInColor.Y;
                    isLayoutValid = true;
                }

                //AGREGADO
                //label1.Content = faceTextLayout.X;
                //label2.Content = faceTextLayout.Y;
                //label3.Content = faceTextLayout.Z;
                //label4.Content = bodyCount;

                return isLayoutValid;
            }

            /// <summary>
            /// Validates face bounding box and face points to be within screen space
            /// Cara Valida cuadro delimitador y faciales puntos para estar dentro de espacio en la pantalla
            /// </summary>
            /// <param name="faceResult">the face frame result containing face box and points</param>
            /// <returns>success or failure</returns>
            private bool ValidateFaceBoxAndPoints(FaceFrameResult faceResult)
            {
                bool isFaceValid = faceResult != null;

                if (isFaceValid)
                {
                    var faceBox = faceResult.FaceBoundingBoxInColorSpace;
                    if (faceBox != null)
                    {
                        // check if we have a valid rectangle within the bounds of the screen space
                        isFaceValid = (faceBox.Right - faceBox.Left) > 0 &&
                                      (faceBox.Bottom - faceBox.Top) > 0 &&
                                      faceBox.Right <= this.displayWidth &&
                                      faceBox.Bottom <= this.displayHeight;

                        if (isFaceValid)
                        {
                            var facePoints = faceResult.FacePointsInColorSpace;
                            if (facePoints != null)
                            {
                                foreach (PointF pointF in facePoints.Values)
                                {
                                    // check if we have a valid face point within the bounds of the screen space
                                    bool isFacePointValid = pointF.X > 0.0f &&
                                                            pointF.Y > 0.0f &&
                                                            pointF.X < this.displayWidth &&
                                                            pointF.Y < this.displayHeight;

                                    if (!isFacePointValid)
                                    {
                                        isFaceValid = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                return isFaceValid;
            }

            /// <summary>
            /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
            /// </summary>
            /// <param name="sender">object sending the event</param>
            /// <param name="e">event arguments</param>
            private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
            {
                if (this.kinectSensor != null)
                {
                    // on failure, set the status text
                    this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                                    : Properties.Resources.SensorNotAvailableStatusText;
                }
            }
        }
    }
