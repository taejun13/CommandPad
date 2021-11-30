using Intel.RealSense;
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
using System.Drawing;

using System.Threading;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Net;
using System.IO;
using Image = System.Windows.Controls.Image;
using System.Windows.Interop;
using System.Drawing.Imaging;
using Point = System.Windows.Point;

using Microsoft.Ink;


namespace commandpad
{
    
    
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            label1.Content = "Trackpad Connection Waiting...";
            label2.Content = "Camera Connection Waiting...";
            label8.Content = "Handwriting recognition";

            TcpData tcp = new TcpData(this, theInkCanvas);
            Thread t1 = new Thread(new ThreadStart(tcp.connectTrackpad));
            Thread t2 = new Thread(new ThreadStart(tcp.connectCamera));

            t1.Start();
            t2.Start();
        }
        
        private void buttonClick(object sender, RoutedEventArgs e)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                theInkCanvas.Strokes.Save(ms);
                //var myInkCollector = new InkCollector();
                var ink = new Ink();
                ink.Load(ms.ToArray());
                using (RecognizerContext context = new RecognizerContext())
                {
                    UnicodeRange ucRange = new UnicodeRange('a', 26);
                    context.SetEnabledUnicodeRanges(new UnicodeRange[] { ucRange });
                  
                    if (ink.Strokes.Count > 0)
                    {
                        context.Strokes = ink.Strokes;
                        RecognitionStatus status;

                        var result = context.Recognize(out status);
                        if (status == RecognitionStatus.NoError)
                            textBox1.Text = result.TopString;
                        else
                            MessageBox.Show("Recognition failed");
                    }
                    else
                        MessageBox.Show("No stroke detected");
                }
            }
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            theInkCanvas.Strokes.Clear();
        }

        internal string invokeLabel1
        {
            get { return label1.Content.ToString(); }
            set { Dispatcher.Invoke(new Action(() => { label1.Content = value; })); }
        }
        internal string invokeLabel2
        {
            get { return label2.Content.ToString(); }
            set { Dispatcher.Invoke(new Action(() => { label2.Content = value; })); }
        }
        internal string invokeLabel3
        {
            get { return label3.Content.ToString(); }
            set { Dispatcher.Invoke(new Action(() => { label3.Content = value; })); }
        }
        internal string invokeLabel4
        {
            get { return label4.Content.ToString(); }
            set { Dispatcher.Invoke(new Action(() => { label4.Content = value; })); }
        }
        internal string invokeLabel5
        {
            get { return label5.Content.ToString(); }
            set { Dispatcher.Invoke(new Action(() => { label5.Content = value; })); }
        }
        internal string invokeLabel6
        {
            get { return label6.Content.ToString(); }
            set { Dispatcher.Invoke(new Action(() => { label6.Content = value; })); }
        }
        internal string invokeLabel7
        {
            get { return label7.Content.ToString(); }
            set { Dispatcher.Invoke(new Action(() => { label7.Content = value; })); }
        }
        internal void invokeInkCanvas(System.Windows.Ink.Stroke s)
        {
            Dispatcher.Invoke(new Action(() => { theInkCanvas.Strokes.Add(s); }));
        }
        internal void invokeTextBox1(String s)
        {
            Dispatcher.Invoke(new Action(() => { textBox1.Text = s; }));
        }
        internal void invokeStrokeSave(MemoryStream s)
        {
            Dispatcher.Invoke(new Action(() => { theInkCanvas.Strokes.Save(s); }));
        }
        internal void invokeStrokeAdd(System.Windows.Ink.Stroke s)
        {
            Dispatcher.Invoke(new Action(() => { theInkCanvas.Strokes.Add(s); }));
        }
        
        internal void invokeClear()
        {
            Dispatcher.Invoke(new Action(() => { theInkCanvas.Strokes.Clear(); }));
        }
        internal void invokeStrokeSave2(MemoryStream s)
        {
            Dispatcher.Invoke(new Action(() => { theInkCanvas2.Strokes.Save(s); }));
        }
        internal void invokeStrokeAdd2(System.Windows.Ink.Stroke s)
        {
            Dispatcher.Invoke(new Action(() => { theInkCanvas2.Strokes.Add(s); }));
        }
        internal void invokeClear2()
        {
            Dispatcher.Invoke(new Action(() => { theInkCanvas2.Strokes.Clear(); }));
        }

    }

    class TcpData
    {
        enum color {
            RED,
            BLUE
        };

        MainWindow target;
        InkCanvas canvas;
        StylusPointCollection pts = new StylusPointCollection();
        int lowerfinger = -1;   // 1 : red, 2 : blue
        
        long t1;
        long t2;
        long t3;
        long t4;

        public TcpData(MainWindow window, InkCanvas inkcanvas)
        {
            target = window;
            canvas = inkcanvas;
        }

        public void connectTrackpad()
        {
            byte[] buff = new byte[20];

            TcpListener listener = new TcpListener(IPAddress.Any, 5000);   // port for trackpad : 6000
            listener.Start();
            TcpClient tc = listener.AcceptTcpClient();  //accept client request for connection and assign TcpClient object
            NetworkStream stream = tc.GetStream();  //Get networkstream from TcpClient object
            target.invokeLabel1 = String.Format("Trackpad Connection Succeed!");

            int x = 0;
            int y = 0;
            int dx = 0;
            int dy = 0;
            int mode = 0;

            Boolean doubleTapped = false;

            //Repeatedly read TCP data with stream.Read();
            int nbytes = 0;
            while (true)
            {
                if ((nbytes = stream.Read(buff, 0, buff.Length)) != 0)
                {
                    byte[] x_bytes = { buff[0], buff[1], buff[2], buff[3] };
                    byte[] y_bytes = { buff[4], buff[5], buff[6], buff[7] };
                    byte[] dx_bytes = { buff[8], buff[9], buff[10], buff[11] };
                    byte[] dy_bytes = { buff[12], buff[13], buff[14], buff[15] };
                    byte[] mode_bytes = { buff[16], buff[17], buff[18], buff[19] };
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(x_bytes);
                        Array.Reverse(y_bytes);
                        Array.Reverse(dx_bytes);
                        Array.Reverse(dy_bytes);
                        Array.Reverse(mode_bytes);
                    }
                    x = BitConverter.ToInt32(x_bytes, 0);
                    y = BitConverter.ToInt32(y_bytes, 0);
                    dx = BitConverter.ToInt32(dx_bytes, 0);
                    dy = BitConverter.ToInt32(dy_bytes, 0);
                    mode = BitConverter.ToInt32(mode_bytes, 0);
                    
                    target.invokeLabel3 = String.Format("x : {0}, y : {1}", x, y);
                    target.invokeLabel4 = String.Format("dx : {0}, dy : {1}", dx, dy);
                    
                    //target.invokeLabel7 = "timestamp : " + timestamp;
                    if (mode == 1)
                    {
                        target.invokeLabel6 = "MotionEvent : ACTION_DOWN";

                        t1 = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        t4 = t1 - t2;

                        if (t3 < 100 && t4 < 100) {
                            target.invokeLabel7 = "time gap t4 : " + t4;
                            doubleTapped = true;
                            //WrapNative.LeftDown();
                            //WrapNative.LeftUp();
                            WrapNative.LeftDown();
                        }
                    }
                    else if (mode == 2)
                    {
                        target.invokeLabel6 = "MotionEvent : ACTION_MOVE";
                        if (doubleTapped)
                        {
                            WrapNative.LeftDown();
                        }
                    }
                    else if (mode == 3)
                    {
                        target.invokeLabel6 = "MotionEvent : ACTION_UP";
                        t2 = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond; ;
                        t3 = t2 - t1;
                        target.invokeLabel7 = "time gap : " + t3;

                        if (doubleTapped)
                        {
                            WrapNative.LeftUp();
                            doubleTapped = false;
                        }
                        else if (t3 < 100) {
                            WrapNative.LeftDown();
                        
                            WrapNative.LeftUp();
                        }
                    }

                    if (mode == 2) {    //MotionEvent.ACTION_MOVE
                        if (lowerfinger == (int)color.RED)
                        {
                            pts.Add(new StylusPoint(x * 0.3, y * 0.3));
                        }
                        else
                        {
                            WrapNative.Move((int)WrapNative.GetMousePosition().X + dx, (int)WrapNative.GetMousePosition().Y + dy);
                            pts.Add(new StylusPoint(x * 0.3, y * 0.3));
                        }

                    }
                    if (mode == 3) {    //MotionEvent.ACTION_UP
                        //recognize();
                        if (lowerfinger == (int)color.RED)
                        {
                            target.invokeClear2();
                            recognize();
                        }
                        else
                        {
                            target.invokeClear();
                            pts = new StylusPointCollection();
                        }
                    }
                }
            }
            //stream.Close();
            //tc.Close();
        }
        public void connectCamera()
        {
            byte[] buff = new byte[4];

            TcpListener listener = new TcpListener(IPAddress.Any, 6000);   // port for camera : 6000
            listener.Start();
            TcpClient tc = listener.AcceptTcpClient();  //accept client request for connection and assign TcpClient object
            NetworkStream stream = tc.GetStream();  //Get networkstream from TcpClient object
            target.invokeLabel2 = String.Format("Camera Connection Succeed!");

            //Repeatedly read TCP data with stream.Read();
            int nbytes = 0;
            while (true)
            {

                if ((nbytes = stream.Read(buff, 0, buff.Length)) != 0)
                {
                    byte[] lowerfinger_bytes = { buff[0], buff[1], buff[2], buff[3] };
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(lowerfinger_bytes);

                    lowerfinger = BitConverter.ToInt32(lowerfinger_bytes, 0);
                    if (lowerfinger == (int)color.RED)
                        target.invokeLabel5 = String.Format("lowerfinger : RED");
                    else if(lowerfinger == (int)color.BLUE)
                        target.invokeLabel5 = String.Format("lowerfinger : BLUE");
                                    }
            }
            //stream.Close();
            //tc.Close();
        }

        private void recognize()
        {
            if (pts.Count > 0) {
                System.Windows.Ink.Stroke s = new System.Windows.Ink.Stroke(pts);
                target.invokeStrokeAdd(s);
                target.invokeStrokeAdd2(s);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                target.invokeStrokeSave(ms);
                //var myInkCollector = new InkCollector();
                var ink = new Ink();
                ink.Load(ms.ToArray());

                using (RecognizerContext context = new RecognizerContext())
                {
                    UnicodeRange ucRange = new UnicodeRange('a', 26);
                    context.SetEnabledUnicodeRanges(new UnicodeRange[] { ucRange });

                    if (ink.Strokes.Count > 0)
                    {
                        context.Strokes = ink.Strokes;
                        RecognitionStatus status;

                        var result = context.Recognize(out status);

                        if (status == RecognitionStatus.NoError)
                        {
                            target.invokeTextBox1(result.TopString);
                            if (result.TopString == "p")
                            {
                                WrapNative.Ctrlp();
                            }
                            else if (result.TopString == "l")
                            {
                                WrapNative.Ctrll();
                            }
                            else if (result.TopString == "i")
                            {
                                WrapNative.Ctrli();
                            }
                            else if (result.TopString == "m")
                            {
                                WrapNative.Ctrlm();
                            }
                            else if (result.TopString == "e")
                            {
                                WrapNative.Ctrle();
                            }
                            else if (result.TopString == "s")
                            {
                                WrapNative.Ctrls();
                            }


                        }
                        else
                            MessageBox.Show("Recognition failed");
                    }
                    else
                        MessageBox.Show("No stroke detected");
                }
            }
            //Clear canvas & pts after recognition
            target.invokeClear();
            pts = new StylusPointCollection();
        }
    }
    
    public static class WrapNative
    {
        [Flags]
        public enum MouseFlag
        {
            ME_MOVE = 1, ME_LEFTDOWN = 2, ME_LEFTUP = 4, ME_RIGHTDOWN = 8,
            ME_RIGHTUP = 0x10, ME_MIDDLEDOWN = 0x20, ME_MIDDLEUP = 0x40, ME_WHEEL = 0x800,
            ME_ABSOULUTE = 8000
        }

        public enum Keys
        {
            VK_LCONTROL = 0xA2,
            VK_KEY_P = 0x50, VK_KEY_I = 0x49, VK_KEY_L = 0x4C,
            VK_KEY_E = 0x45, VK_KEY_M = 0x4D, VK_KEY_S = 0x53
        }

        private const int VK_KEYDOWN = 0x00;
        private const int VK_KEYUP = 0x02;

        [DllImport("user32.dll")]
        static extern void mouse_event(int flag, int dx, int dy, int buttons, int extra);
        [DllImport("user32.dll")]
        static extern bool GetCursorPos(ref Win32Point point);
        [DllImport("user32.dll")]
        static extern int SetCursorPos(int x, int y);
        [DllImport("user32.dll")]
        public static extern void keybd_event(uint vk, uint scan, uint flags, uint extraInfo);

        
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };
        
        public static Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }
        public static void Move(int x, int y)
        {
            SetCursorPos(x, y);
        }
        public static void Move(Point pt)
        {
            Move((int)pt.X, (int)pt.Y);
        }
        public static void LeftDown()
        {
            mouse_event((int)MouseFlag.ME_LEFTDOWN, 0, 0, 0, 0);
        }

        public static void LeftUp()
        {
            mouse_event((int)MouseFlag.ME_LEFTUP, 0, 0, 0, 0);
        }

        public static void Ctrlp()
        {
            keybd_event((byte)Keys.VK_LCONTROL, 0, VK_KEYDOWN, 0);
            keybd_event((byte)Keys.VK_KEY_P, 0, VK_KEYDOWN, 0);
            keybd_event((byte)Keys.VK_LCONTROL, 0, VK_KEYUP, 0);
            keybd_event((byte)Keys.VK_KEY_P, 0, VK_KEYUP, 0);
        }
        public static void Ctrll()
        {
            keybd_event((byte)Keys.VK_LCONTROL, 0, VK_KEYDOWN, 0);
            keybd_event((byte)Keys.VK_KEY_L, 0, VK_KEYDOWN, 0);
            keybd_event((byte)Keys.VK_LCONTROL, 0, VK_KEYUP, 0);
            keybd_event((byte)Keys.VK_KEY_L, 0, VK_KEYUP, 0);
        }
        public static void Ctrli()
        {
            keybd_event((byte)Keys.VK_LCONTROL, 0, VK_KEYDOWN, 0);
            keybd_event((byte)Keys.VK_KEY_I, 0, VK_KEYDOWN, 0);
            keybd_event((byte)Keys.VK_LCONTROL, 0, VK_KEYUP, 0);
            keybd_event((byte)Keys.VK_KEY_I, 0, VK_KEYUP, 0);
        }
        public static void Ctrlm()
        {
            keybd_event((byte)Keys.VK_LCONTROL, 0, VK_KEYDOWN, 0);
            keybd_event((byte)Keys.VK_KEY_M, 0, VK_KEYDOWN, 0);
            keybd_event((byte)Keys.VK_LCONTROL, 0, VK_KEYUP, 0);
            keybd_event((byte)Keys.VK_KEY_M, 0, VK_KEYUP, 0);
        }
        public static void Ctrle()
        {
            keybd_event((byte)Keys.VK_LCONTROL, 0, VK_KEYDOWN, 0);
            keybd_event((byte)Keys.VK_KEY_E, 0, VK_KEYDOWN, 0);
            keybd_event((byte)Keys.VK_LCONTROL, 0, VK_KEYUP, 0);
            keybd_event((byte)Keys.VK_KEY_E, 0, VK_KEYUP, 0);
        }
        public static void Ctrls()
        {
            keybd_event((byte)Keys.VK_LCONTROL, 0, VK_KEYDOWN, 0);
            keybd_event((byte)Keys.VK_KEY_S, 0, VK_KEYDOWN, 0);
            keybd_event((byte)Keys.VK_LCONTROL, 0, VK_KEYUP, 0);
            keybd_event((byte)Keys.VK_KEY_S, 0, VK_KEYUP, 0);
        }


    }
}