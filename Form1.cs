// Date: 2023.4~5; 2024.9~11
// Designer: Fraljimetry

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Media;
using System.Reflection;
using WMPLib;
using System.Text;

namespace FunctionGrapher2._0
{
    public partial class Graph : Form
    {
        private static SoundPlayer _clickSoundPlayer;
        private static WindowsMediaPlayer _player;
        private static Bitmap bitmap;
        private static Rectangle rect;
        private static DateTime TimeNow = new();
        private static TimeSpan TimeCount = new();
        private static System.Windows.Forms.Timer GraphTimer, ColorTimer, WaitTimer, DisplayTimer;
        private static float ScalingFactor;
        private static int elapsedSeconds, x_left, x_right, y_up, y_down;
        private static readonly int X_LEFT_MAC = 620, X_RIGHT_MAC = 1520, Y_UP_MAC = 45, Y_DOWN_MAC = 945;
        private static readonly int X_LEFT_MIC = 1565, X_RIGHT_MIC = 1765, Y_UP_MIC = 745, Y_DOWN_MIC = 945;
        private static readonly int X_LEFT_CHECK = 1921, X_RIGHT_CHECK = 1922, Y_UP_CHECK = 1081, Y_DOWN_CHECK = 1082;
        private static readonly int REF_POS_1 = 9, REF_POS_2 = 27;
        private static int plot_loop, points_chosen, color_mode, contour_mode, point_number, times, export_number;
        private static double timeElapsed, _currentPosition;
        private static readonly double EPSILON = 0.03, STEPS = 0.25, DEVIATION = Math.PI / 12, EPS_DIFF_REAL = 0.5, EPS_DIFF_COMPLEX = 0.5, STEP_DIFF = 1, SIZE_DIFF = 1, PARAM_WIDTH = 5, INCREMENT_DEFAULT = 0.01, SHADE_DENSITY = 2;
        private static double epsilon, steps, deviation, raw_thickness, size_for_extremities, decay_rate;
        private static double[] scopes;
        private static int[] borders;
        private static bool waiting, commence_waiting, _isPaused = true, delete_coordinate, delete_point = true, swap_colors, complex_mode = true, auto_export, retain_graph, clicked, shade_rainbow, axes_drawn, Axes_drawn, is_main, drafted, main_drawn, text_changed, activate_mousemove, is_checking, address_error, is_resized, ctrlPressed, sftPressed, suppressKeyUp;
        private static readonly Color CORRECT_GREEN = Color.FromArgb(192, 255, 192), ERROR_RED = Color.FromArgb(255, 192, 192), UNCHECKED_YELLOW = Color.FromArgb(255, 255, 128), READONLY_PURPLE = Color.FromArgb(255, 192, 255), COMBO_BLUE = Color.FromArgb(192, 255, 255), FOCUS_GRAY = Color.LightGray, BACKDROP_GRAY = Color.FromArgb(64, 64, 64), CONTROL_GRAY = Color.FromArgb(105, 105, 105), GRID_GRAY = Color.FromArgb(75, 255, 255, 255), UPPER_GOLD = Color.Gold, LOWER_BLUE = Color.RoyalBlue, ZERO_BLUE = Color.Lime, POLE_PURPLE = Color.Magenta, READONLY_GRAY = Color.Gainsboro;
        private static readonly string ADDRESS_DEFAULT = @"C:\Users\Public", INPUT_DEFAULT = "z", GENERAL_DEFAULT = "e", THICKNESS_DEFAULT = "1", DENSENESS_DEFAULT = "1", DRAFT_DEFAULT = "Detailed historical info is documented here.\r\n\r\n", CAPTION_DEFAULT = "Yours inputs will be shown here.", BARREDCHARS = "_#!<>$%&@~:\'\"\\?=`[]{}\t";
        private static string[] ExampleString;
        private static ComplexMatrix output_table;
        private static DoubleMatrix Output_table;

        #region Initializations
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool BlockInput([MarshalAs(UnmanagedType.Bool)] bool fBlockIt);
        [DllImport("user32.dll")]
        static extern bool HideCaret(IntPtr hWnd);
        protected override void WndProc(ref Message m)
        {
            const int WM_NCLBUTTONDOWN = 0x00A1;
            const int HTCAPTION = 0x0002;
            if (m.Msg == WM_NCLBUTTONDOWN && m.WParam.ToInt32() == HTCAPTION) return;
            base.WndProc(ref m);
        } // Prevents dragging the titlebar
        public Graph()
        {
            InitializeComponent();
            SetTitleBarColor();
            InitializeMusicPlayer();
            LoadClickSound();
            AttachClickEvents(this);
            InitializeTimers();
            InitializeBitmap();
            BanMouseWheel();
        }
        private void Graph_Load(object sender, EventArgs e)
        {
            InitializeCombo();
            InitializeData();
            SetTDSB();
            ReduceFontSizeByScale(this);
            TextBoxFocus(sender, e);
        }
        private void Graph_Paint(object sender, PaintEventArgs e)
        {
            if (clicked) return;
            SetBackdrop(e);
            DefaultReference(true);
        }
        private int SetTitleBarColor()
        {
            // Set window attribute for title bar color
            int attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
            int value = 1;  // Set to 1 to apply immersive color mode
            return DwmSetWindowAttribute(Handle, attribute, ref value, sizeof(int));
        }
        private void InitializeMusicPlayer()
        {
            _player = new WindowsMediaPlayer();
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream soundStream = assembly.GetManifestResourceStream("FunctionGrapher2._0.calm-zimpzon-main-version-07-55-10844.wav");
            if (soundStream != null)
            {
                // Save the stream to a temporary file, since Windows Media Player cannot play directly from the stream
                string tempFile = Path.Combine(Path.GetTempPath(), "background_music.wav");
                using (FileStream fileStream = new(tempFile, FileMode.Create, FileAccess.Write)) // This is extremely sensitive
                    soundStream.CopyTo(fileStream);
                // Set the media file
                _player.URL = tempFile;
                _player.settings.setMode("loop", true); // Loop the music
                _player.controls.stop();
            }
            else MessageBox.Show("Error: Could not find embedded resource for music.");
        }
        private void PlayOrPause()
        {
            if (!_isPaused)
            {
                _currentPosition = _player.controls.currentPosition;
                _player.controls.pause();
                ColorTimer.Stop();
                TitleLabel.ForeColor = Color.White;
            }
            else
            {
                _player.controls.currentPosition = _currentPosition;
                _player.controls.play();
                ColorTimer.Start();
                timeElapsed = 0;
            }
            _isPaused = !_isPaused;
        }
        private static void LoadClickSound()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream soundStream = assembly.GetManifestResourceStream("FunctionGrapher2._0.bubble-sound-43207_[cut_0sec].wav");
            if (soundStream != null) _clickSoundPlayer = new SoundPlayer(soundStream);
            else MessageBox.Show("Error: Could not find embedded resource for click sound.");
        }
        private void AttachClickEvents(Control control)
        {
            control.Click += Control_Click;
            foreach (Control childControl in control.Controls) AttachClickEvents(childControl);
        }
        private void Control_Click(object sender, EventArgs e)
            => _clickSoundPlayer?.Play();
        private void InitializeTimers()
        {
            GraphTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            ColorTimer = new System.Windows.Forms.Timer { Interval = 50 };
            ColorTimer.Tick += ColorTimer_Tick;
            WaitTimer = new System.Windows.Forms.Timer { Interval = 500 };
            WaitTimer.Tick += WaitTimer_Tick;
            DisplayTimer = new System.Windows.Forms.Timer { Interval = 500 };
            DisplayTimer.Tick += DisplayTimer_Tick;
            timeElapsed = 0;
        }
        private void DisplayTimer_Tick(object sender, EventArgs e)
        {
            if (++elapsedSeconds % 2 == 0) TimeDisplay.Text = (elapsedSeconds / 2).ToString() + "s";
            PointNumDisplay.Text = (point_number + times).ToString();
        }
        private void WaitTimer_Tick(object sender, EventArgs e)
        {
            waiting = !waiting;
            if (!commence_waiting) return;
            PictureWait.Visible = waiting;
        }
        private void ColorTimer_Tick(object sender, EventArgs e)
        {
            timeElapsed += 0.01;
            TitleLabel.ForeColor = ObtainWheelCurve(timeElapsed % 1);
        }
        private void InitializeBitmap()
        {
            bitmap = new(Width, Height, PixelFormat.Format32bppArgb);
            rect = new(0, 0, Width, Height);
            DoubleBuffered = true;
            KeyPreview = true; // This is essential for shortcuts
        }
        private void BanMouseWheel()
        {
            ComboExamples.MouseWheel += ComboBox_MouseWheel;
            ComboFunctions.MouseWheel += ComboBox_MouseWheel;
            ComboSpecial.MouseWheel += ComboBox_MouseWheel;
            ComboColoring.MouseWheel += ComboBox_MouseWheel;
            ComboContour.MouseWheel += ComboBox_MouseWheel;
        }
        private void ComboBox_MouseWheel(object sender, MouseEventArgs e)
            => ((HandledMouseEventArgs)e).Handled = true;
        private void InitializeCombo()
        {
            Construct_Examples();
            ComboColoring_AddItem();
            ComboContour_AddItem();
            ComboExamples_AddItem();
            ComboFunctions_AddItem();
            ComboSpecial_AddItem();
        }
        private static void Construct_Examples()
        {
            ExampleString = new string[]
            {
                "F(1-10i,0.5i,i,zzzzz,100)",
                "z^(1+10i)cos((z-1)/(z^13+z+1))",
                "sum(-1+1/(1-z^n),n,1,100)",
                "prod(exp(1+2/(ze(-k/5)-1)),k,1,5)",
                "iterate((Z+1/Z)e(0.02),z,k,1,1000)",
                "iterate(exp(z^Z),z,k,1,100)",
                "iterateLoop(ZZ+z,0,k,1,100)",
                "comp(zz,sin(zZ),cos(z/Z))",
                "cos(xy)-cos(x)-cos(y)",
                "min(sin(xy),tan(x),tan(y))",
                "xround(y)-yround(x)",
                "y-x|IterateLoop(x^X,x,k,1,30,y-X)",
                "iterate1(kx/X+X/(y+k),sin(x+y),k,1,3)",
                "iterate2(k/X+k/Y,XY,sin(x+y),cos(x-y),k,1,10,2)",
                "comp1(xy,tan(X+x),Arth(X-y))",
                "comp2(xy,xx+yy,sin(X+Y),cos(X-Y),2)",
                "func(ga(x,100),0.0001)",
                "func(sum(sin(2^kx)/2^k,k,0,100),-pi,pi,0.001)",
                "func(beta(sinh(x),cosh(x),100),-2,2,0.00001)",
                "polar(sqrt(cos(2theta)),theta,0,2pi,0.0001)",
                "polar(cos(5k)cos(7k),k,0,2pi,0.001)",
                "loop(polar(0.1jcos(5k+0.7jpi),k,0,pi),j,1,10)",
                "param(cos(17k),cos(19k),k,0,pi,0.0001)",
                "loop(param(cos(m)^k,sin(m)^k,m,0,p/2),k,1,10)"
            };
        }
        private void ComboColoring_AddItem()
        {
            string[] coloringOptions = { "Commonplace", "Monochromatic", "Bichromatic", "Kaleidoscopic", "Miscellaneous" };
            ComboColoring.Items.AddRange(coloringOptions);
            ComboColoring.SelectedIndex = 4;
        }
        private void ComboContour_AddItem()
        {
            string[] contourOptions = { "Cartesian (x,y)", "Polar (r,θ)" };
            ComboContour.Items.AddRange(contourOptions);
            ComboContour.SelectedIndex = 1;
        }
        private void ComboExamples_AddItem()
        {
            for (int i = 0; i < 8; i++) ComboExamples.Items.Add(ExampleString[i]);
            ComboExamples.Items.Add(String.Empty);
            for (int i = 8; i < 16; i++) ComboExamples.Items.Add(ExampleString[i]);
            ComboExamples.Items.Add(String.Empty);
            for (int i = 16; i < 24; i++) ComboExamples.Items.Add(ExampleString[i]);
        }
        private void ComboFunctions_AddItem()
        {
            string[] functionOptions = { "floor()", "ceil()", "round()", "sgn()", "F()", "gamma()", "beta()", "zeta()", "mod()", "nCr()", "nPr()", "max()", "min()", "log()", "exp()", "sqrt()", "abs()", "factorial()", "arsinh()", "arcosh()", "artanh()", "arcsin()", "arccos()", "arctan()", "sinh()", "cosh()", "tanh()", "sin()", "cos()", "tan()", "conjugate()", "e()" };
            ComboFunctions.Items.AddRange(functionOptions);
        }
        private void ComboSpecial_AddItem()
        {
            string[] specialOptions = { "product()", "sum()", "iterate1()", "iterate2()", "composite1()", "composite2()", "iterateLoop()", "loop()", "iterate()", "composite()", "func()", "polar()", "param()" };
            ComboSpecial.Items.AddRange(specialOptions);
        }
        private void InitializeData()
        {
            InputString.Text = INPUT_DEFAULT;
            InputString.SelectionStart = InputString.Text.Length;
            DraftBox.Text = DRAFT_DEFAULT;
            CaptionBox.Text = CAPTION_DEFAULT;
            GeneralInput.Text = GENERAL_DEFAULT;
            ThickInput.Text = THICKNESS_DEFAULT;
            DenseInput.Text = DENSENESS_DEFAULT;
            AddressInput.Text = ADDRESS_DEFAULT;
            PictureIncorrect.Visible = false;
            PictureCorrect.Visible = true;
        }
        private void SetThicknessDenseness()
        {
            epsilon = EPSILON; steps = STEPS; deviation = DEVIATION;
            if (String.IsNullOrEmpty(ThickInput.Text)) ThickInput.Text = THICKNESS_DEFAULT;
            if (String.IsNullOrEmpty(DenseInput.Text)) DenseInput.Text = DENSENESS_DEFAULT;
            raw_thickness = RealSub.Obtain(ThickInput.Text);
            double temp = RealSub.Obtain(DenseInput.Text);
            epsilon = complex_mode ? EPSILON * EPS_DIFF_COMPLEX * raw_thickness : EPSILON * EPS_DIFF_REAL * raw_thickness;
            steps = STEPS / temp; deviation = DEVIATION / temp;
            decay_rate = 0.2 * raw_thickness;
            size_for_extremities = 0.5 * raw_thickness / (1 + raw_thickness);
        }
        private void SetScopesBorders()
        {
            if (String.IsNullOrEmpty(GeneralInput.Text)) GeneralInput.Text = GENERAL_DEFAULT;
            if (GeneralInput.Text != "0")
            {
                double temp_scope = RealSub.Obtain(GeneralInput.Text);
                scopes = new double[] { -temp_scope, temp_scope, temp_scope, -temp_scope };
                X_Left.Text = Y_Left.Text = (-temp_scope).ToString("#0.0000");
                X_Right.Text = Y_Right.Text = temp_scope.ToString("#0.0000");
            }
            else
            {
                if (String.IsNullOrEmpty(X_Left.Text)) X_Left.Text = "0";
                if (String.IsNullOrEmpty(X_Right.Text)) X_Right.Text = "0";
                if (String.IsNullOrEmpty(Y_Left.Text)) Y_Left.Text = "0";
                if (String.IsNullOrEmpty(Y_Right.Text)) Y_Right.Text = "0";
                scopes[0] = RealSub.Obtain(X_Left.Text);
                scopes[1] = RealSub.Obtain(X_Right.Text);
                scopes[2] = RealSub.Obtain(Y_Right.Text);
                scopes[3] = RealSub.Obtain(Y_Left.Text);
            }
            borders = new int[] { x_left, x_right, y_up, y_down };
        }
        private void SetTDSB() { SetThicknessDenseness(); SetScopesBorders(); }
        private static void ReduceFontSizeByScale(Control parent)
        {
            ScalingFactor = Graphics.FromHwnd(IntPtr.Zero).DpiX / 96f / 1.5f;
            foreach (Control ctrl in parent.Controls)
            {
                ctrl.Font = new Font(ctrl.Font.FontFamily, ctrl.Font.Size / ScalingFactor, ctrl.Font.Style);
                if (ctrl.Controls.Count > 0) ReduceFontSizeByScale(ctrl);
            }
        }
        private void TextBoxFocus(object sender, EventArgs e)
        {
            foreach (Control control in Controls.OfType<TextBox>())
                control.GotFocus += (sender, e) => { ((TextBox)sender).SelectionStart = ((TextBox)sender).Text.Length; };
        }
        private static void SetBackdrop(PaintEventArgs e)
        {
            DrawBackdrop(e.Graphics, new(Color.Gray, 1), 
                X_LEFT_MIC, Y_UP_MIC, X_RIGHT_MIC, Y_DOWN_MIC, new SolidBrush(Color.Black));
            DrawBackdrop(e.Graphics, new(Color.Gray, 1), 
                X_LEFT_MAC, Y_UP_MAC, X_RIGHT_MAC, Y_DOWN_MAC, new SolidBrush(Color.Black));
        }
        private static void DrawBackdrop(Graphics g, Pen pen, int xLeft, int yUp, int xRight, int yDown, Brush backBrush)
        {
            g.DrawLine(pen, xLeft, yUp, xRight, yUp);
            g.DrawLine(pen, xRight, yUp, xRight, yDown);
            g.DrawLine(pen, xRight, yDown, xLeft, yDown);
            g.DrawLine(pen, xLeft, yDown, xLeft, yUp);
            g.FillRectangle(backBrush, xLeft + 1, yUp + 1, xRight - xLeft - 1, yDown - yUp - 1);
        }
        #endregion

        #region Pre-Graphing
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int[] Transform(double x, double y, double[] scopes, int[] borders)
        {
            double _x = (borders[1] - borders[0]) / (scopes[1] - scopes[0]);
            double _y = (borders[3] - borders[2]) / (scopes[3] - scopes[2]);
            return new int[] { (int)(borders[0] + (x - scopes[0]) * _x), (int)(borders[2] + (y - scopes[2]) * _y) };
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double[] InverseTransform(int a, int b, double[] scopes, int[] borders)
        {
            double _x = (scopes[1] - scopes[0]) / (borders[1] - borders[0]);
            double _y = (scopes[3] - scopes[2]) / (borders[3] - borders[2]);
            return new double[] { scopes[0] + (a - borders[0]) * _x, scopes[2] + (b - borders[2]) * _y };
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double LowerDistance(double a, double m) => a - m * LowerIndex(a, m);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LowerIndex(double a, double m) => a >= 0 ? (int)(a / m) : (int)((a / m) + (int)(-a / m) + 1) - (int)(-a / m) - 1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double LowerRatio(double a, double m) => a == -0 ? 1 : (a - m * LowerIndex(a, m)) / m;
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe static double[] FiniteExtremities(DoubleMatrix output, int row, int column)
        {
            double min = Double.NaN, max = Double.NaN;
            int size = row * column; double* outPtr = output.Ptr();
            for (int i = 0; i < size; i++, outPtr++) // Should not use parallel
            {
                if (Double.IsNaN(*outPtr)) continue;
                double atanValue = Math.Atan(*outPtr);
                if (Double.IsNaN(min)) min = max = atanValue; // This is necessitous
                else
                {
                    max = Math.Max(atanValue, max);
                    min = Math.Min(atanValue, min);
                }
            }
            return new double[] { min, max };
        }
        private void RealComputation(int row, int column)
        {
            switch (color_mode)
            {
                case 1: Real1(Output_table, row, column); break;
                case 2: Real2(Output_table, row, column); break;
                case 3: Real3(Output_table, row, column); break;
                case 4: Real4(Output_table, row, column); break;
                case 5: Real5(Output_table, row, column); break;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void RealSpecial(int i, int j, DoubleMatrix output, int _row, int _column, double[] MinMax, byte* ptr, int stride, Color zeroColor, Color poleColor)
        {
            if (delete_point) return;
            double value = Math.Atan(output[_row, _column]);
            if (value < (1 - size_for_extremities) * MinMax[0] + size_for_extremities * MinMax[1])
                SetPixelFast(i, j, ptr, stride, zeroColor);
            if (value > (1 - size_for_extremities) * MinMax[1] + size_for_extremities * MinMax[0])
                SetPixelFast(i, j, ptr, stride, poleColor);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ProcessReal(int i, int j, DoubleMatrix output, double[] MinMax, byte* ptr, int stride, Color zeroColor, Color poleColor, Func<double, Color> colorSelector)
        {
            int _row = i - (x_left + 1), _column = j - (y_up + 1);
            double value = output[_row, _column];
            if (Double.IsNaN(value)) return;
            Color pixelColor = colorSelector(value);
            if (pixelColor != Color.Empty) SetPixelFast(i, j, ptr, stride, pixelColor);
            RealSpecial(i, j, output, _row, _column, MinMax, ptr, stride, zeroColor, poleColor);
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void RealLoop(DoubleMatrix output, int row, int column, Color zeroColor, Color poleColor, Func<double, Color> colorSelector)
        {
            int xStart = x_left + 1, yStart = y_up + 1, xEnd = x_right, yEnd = y_down;
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            byte* ptr = (byte*)bmpData.Scan0;
            try
            {
                double[] MinMax = FiniteExtremities(output, row, column);
                for (int i = xStart; i < xEnd; i++) for (int j = yStart; j < yEnd; j++)
                        ProcessReal(i, j, output, MinMax, ptr, bmpData.Stride, zeroColor, poleColor, colorSelector);
            }
            finally { bitmap.UnlockBits(bmpData); }
        }
        private void Real1(DoubleMatrix output, int row, int column)
            => RealLoop(output, row, column, ZERO_BLUE, POLE_PURPLE, value =>
        (Math.Abs(value) < epsilon) ? (swap_colors ? Color.Black : Color.White) : Color.Empty);
        private void Real2(DoubleMatrix output, int row, int column)
        {
            Color trueColor = swap_colors ? Color.Black : Color.White;
            Color falseColor = swap_colors ? Color.White : Color.Black;
            RealLoop(output, row, column, ZERO_BLUE, POLE_PURPLE, value =>
            value < 0 ? falseColor : (value > 0 ? trueColor : Color.Empty));
        }
        private void Real3(DoubleMatrix output, int row, int column)
        {
            Color trueColor = swap_colors ? LOWER_BLUE : UPPER_GOLD;
            Color falseColor = swap_colors ? UPPER_GOLD : LOWER_BLUE;
            RealLoop(output, row, column, Color.Black, Color.White, value =>
            value < 0 ? falseColor : (value > 0 ? trueColor : Color.Empty));
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Real4(DoubleMatrix output, int row, int column)
        {
            int xStart = x_left + 1, yStart = y_up + 1, xEnd = x_right, yEnd = y_down;
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            byte* ptr = (byte*)bmpData.Scan0;
            try
            {
                double[] MinMax = FiniteExtremities(output, row, column);
                for (int i = xStart; i < xEnd; i++)
                {
                    for (int j = yStart, _row = i - xStart; j < yEnd; j++)
                    {
                        int _column = j - yStart;
                        double value = output[_row, _column];
                        if (Double.IsNaN(value)) continue;
                        Color pixelColor = ObtainStrip(Math.Atan(value), MinMax[0], MinMax[1]);
                        SetPixelFast(i, j, ptr, bmpData.Stride, pixelColor);
                        RealSpecial(i, j, output, _row, _column, MinMax, ptr, bmpData.Stride, Color.Black, Color.White);
                    }
                }
            }
            finally { bitmap.UnlockBits(bmpData); }
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Real5(DoubleMatrix output, int row, int column)
        {
            int xStart = x_left + 1, yStart = y_up + 1, xEnd = x_right, yEnd = y_down;
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            byte* ptr = (byte*)bmpData.Scan0;
            try
            {
                double[] MinMax = FiniteExtremities(output, row, column);
                for (int i = xStart; i < xEnd; i++)
                {
                    for (int j = yStart, _row = i - xStart; j < yEnd; j++)
                    {
                        int _column = j - yStart;
                        double value = output[_row, _column];
                        if (Double.IsNaN(value)) continue;
                        double alpha = Math.Clamp(LowerRatio(value, raw_thickness), 0, 1); // The clamp is necessary here
                        Color pixelColor = ObtainStripAlpha(Math.Atan(value), (alpha - 1) / SHADE_DENSITY + 1, MinMax[0], MinMax[1]);
                        SetPixelFast(i, j, ptr, bmpData.Stride, pixelColor);
                        RealSpecial(i, j, output, _row, _column, MinMax, ptr, bmpData.Stride, Color.Black, Color.White);
                    }
                }
            }
            finally { bitmap.UnlockBits(bmpData); }
        }
        private void ComplexComputation()
        {
            switch (color_mode, contour_mode)
            {
                case (1, 1): Complex1_ReIm(output_table); break;
                case (2, 1): Complex2_ReIm(output_table); break;
                case (3, 1): Complex3_ReIm(output_table); break;
                case (1, _): Complex1_ModArg(output_table); break;
                case (2, _): Complex2_ModArg(output_table); break;
                case (3, _): Complex3_ModArg(output_table); break;
                case (4, _): Complex4(output_table); break;
                case (5, _): Complex5(output_table); break;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ComplexSpecial(int i, int j, ComplexMatrix output, int _row, int _column, byte* ptr, int stride, Color zeroColor, Color poleColor)
        {
            if (delete_point) return;
            double modulus = Complex.Modulus(output[_row, _column]);
            if (modulus < epsilon * SIZE_DIFF) SetPixelFast(i, j, ptr, stride, zeroColor);
            else if (modulus > 1 / (epsilon * SIZE_DIFF)) SetPixelFast(i, j, ptr, stride, poleColor);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ProcessComplexData(int i, int j, ComplexMatrix output, byte* ptr, int stride, Color trueColor, Color falseColor, Color zeroColor, Color poleColor, bool mode1, Func<Complex, (double, double)> valueExtractor, double stepFactor1, double stepFactor2)
        {
            int _row = i - (x_left + 1), _column = j - (y_up + 1);
            Complex value = output[_row, _column];
            if (Double.IsNaN(value.real) || Double.IsNaN(value.imaginary)) return;
            (double value1, double value2) = valueExtractor(value);
            if (mode1)
            {
                if (LowerDistance(value1, stepFactor1) < epsilon || LowerDistance(value2, stepFactor2) < epsilon)
                    SetPixelFast(i, j, ptr, stride, swap_colors ? falseColor : trueColor);
            }
            else SetPixelFast(i, j, ptr, stride, (LowerIndex(value1, stepFactor1) + LowerIndex(value2, stepFactor2)) % 2 == 0 ? trueColor : falseColor);
            ComplexSpecial(i, j, output, _row, _column, ptr, stride, zeroColor, poleColor);
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void ProcessComplexLoop(ComplexMatrix output, Color trueColor, Color falseColor, Color zeroColor, Color poleColor, bool mode1, Func<Complex, (double, double)> valueExtractor, double stepFactor1, double stepFactor2)
        {
            int xStart = x_left + 1, yStart = y_up + 1, xEnd = x_right, yEnd = y_down;
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            byte* ptr = (byte*)bmpData.Scan0;
            try
            {
                for (int i = xStart; i < xEnd; i++) for (int j = yStart; j < yEnd; j++)
                        ProcessComplexData(i, j, output, ptr, bmpData.Stride, trueColor, falseColor, zeroColor, poleColor, mode1, valueExtractor, stepFactor1, stepFactor2);
            }
            finally { bitmap.UnlockBits(bmpData); }
        }
        private unsafe void ComplexReImLoop(ComplexMatrix output, Color trueColor, Color falseColor, Color zeroColor, Color poleColor, bool mode1)
            => ProcessComplexLoop(output, trueColor, falseColor, zeroColor, poleColor, mode1, c => (c.real, c.imaginary), steps, steps);
        private unsafe void ComplexModArgLoop(ComplexMatrix output, Color trueColor, Color falseColor, Color zeroColor, Color poleColor, bool mode1)
            => ProcessComplexLoop(output, trueColor, falseColor, zeroColor, poleColor, mode1, c =>
            (Complex.Log(c).real, Math.Atan2(c.imaginary, c.real)), steps * STEP_DIFF, deviation);
        private void Complex1_ReIm(ComplexMatrix output)
            => ComplexReImLoop(output, Color.White, Color.Black, ZERO_BLUE, POLE_PURPLE, true);
        private void Complex2_ReIm(ComplexMatrix output)
        {
            Color trueColor = swap_colors ? Color.Black : Color.White;
            Color falseColor = swap_colors ? Color.White : Color.Black;
            ComplexReImLoop(output, trueColor, falseColor, ZERO_BLUE, POLE_PURPLE, false);
        }
        private void Complex3_ReIm(ComplexMatrix output)
        {
            Color trueColor = swap_colors ? LOWER_BLUE : UPPER_GOLD;
            Color falseColor = swap_colors ? UPPER_GOLD : LOWER_BLUE;
            ComplexReImLoop(output, trueColor, falseColor, Color.Black, Color.White, false);
        }
        private void Complex1_ModArg(ComplexMatrix output)
            => ComplexModArgLoop(output, Color.White, Color.Black, ZERO_BLUE, POLE_PURPLE, true);
        private void Complex2_ModArg(ComplexMatrix output)
        {
            Color trueColor = swap_colors ? Color.Black : Color.White;
            Color falseColor = swap_colors ? Color.White : Color.Black;
            ComplexModArgLoop(output, trueColor, falseColor, ZERO_BLUE, POLE_PURPLE, false);
        }
        private void Complex3_ModArg(ComplexMatrix output)
        {
            Color trueColor = swap_colors ? LOWER_BLUE : UPPER_GOLD;
            Color falseColor = swap_colors ? UPPER_GOLD : LOWER_BLUE;
            ComplexModArgLoop(output, trueColor, falseColor, Color.Black, Color.White, false);
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Complex4(ComplexMatrix output)
        {
            int xStart = x_left + 1, yStart = y_up + 1, xEnd = x_right, yEnd = y_down;
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            byte* ptr = (byte*)bmpData.Scan0;
            try
            {
                for (int i = xStart; i < xEnd; i++)
                {
                    for (int j = yStart, _row = i - xStart; j < yEnd; j++)
                    {
                        int _column = j - yStart;
                        Complex value = output[_row, _column];
                        if (Double.IsNaN(value.real) || Double.IsNaN(value.imaginary)) continue;
                        Color pixelColor = shade_rainbow ? ObtainWheelAlpha(value) : ObtainWheel(value);
                        SetPixelFast(i, j, ptr, bmpData.Stride, pixelColor);
                        ComplexSpecial(i, j, output, _row, _column, ptr, bmpData.Stride, Color.Black, Color.White);
                    }
                }
            }
            finally { bitmap.UnlockBits(bmpData); }
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Complex5(ComplexMatrix output)
        {
            int xStart = x_left + 1, yStart = y_up + 1, xEnd = x_right, yEnd = y_down;
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            byte* ptr = (byte*)bmpData.Scan0;
            try
            {
                for (int i = xStart; i < xEnd; i++)
                {
                    for (int j = yStart, _row = i - xStart; j < yEnd; j++)
                    {
                        int _column = j - yStart;
                        Complex value = output[_row, _column];
                        if (Double.IsNaN(value.real) || Double.IsNaN(value.imaginary)) continue;
                        double _modulus = Complex.Log(value).real;
                        double _argument = Math.Atan2(value.imaginary, value.real);
                        double alpha = (LowerRatio(_modulus, steps * STEP_DIFF) + LowerRatio(_argument, deviation)) / 2;
                        double normalAlpha = (alpha - 1) / SHADE_DENSITY + 1;
                        Color pixelColor = shade_rainbow ? ObtainWheelAlpha(value, normalAlpha) : ObtainWheel(value, normalAlpha);
                        SetPixelFast(i, j, ptr, bmpData.Stride, pixelColor);
                        ComplexSpecial(i, j, output, _row, _column, ptr, bmpData.Stride, Color.Black, Color.White);
                    }
                }
            }
            finally { bitmap.UnlockBits(bmpData); }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SetPixelFast(int x, int y, byte* ptr, int stride, Color color)
        {
            point_number++;
            int pixelIndex = y * stride + x * 4; // Assuming 32bpp (ARGB format)
            ptr[pixelIndex] = color.B;
            ptr[pixelIndex + 1] = color.G;
            ptr[pixelIndex + 2] = color.R;
            ptr[pixelIndex + 3] = color.A;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe static void ClearBitmap(Bitmap bitmap, Rectangle rect)
        {
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
            int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            int height = rect.Height, widthInBytes = rect.Width * bytesPerPixel, stride = bmpData.Stride;
            byte* ptr = (byte*)bmpData.Scan0;
            Parallel.For(0, height, y => {
                byte* row = ptr + y * stride;
                for (int x = 0; x < widthInBytes; x += bytesPerPixel) row[x + 3] = 0;
            });
            bitmap.UnlockBits(bmpData);
        }
        private static void BeginRendering() { if (!retain_graph) ClearBitmap(bitmap, rect); }
        private void EndRendering()
        {
            if (is_checking) return;
            PointNumDisplay.Text = point_number.ToString();
            if (auto_export) RunExportButton_Click();
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe static (DoubleMatrix xCoor, DoubleMatrix yCoor) SetCoor(int row, int column)
        {
            DoubleMatrix xCoor = new(row, column), yCoor = new(row, column);
            Parallel.For(0, row, i =>
            {
                double* xCoorPtr = xCoor.RowPtr(i), yCoorPtr = yCoor.RowPtr(i);
                for (int j = 0; j < column; j++, xCoorPtr++, yCoorPtr++)
                {
                    double[] coor = InverseTransform(i + x_left + 1, j + y_up + 1, scopes, borders);
                    *xCoorPtr = coor[0]; *yCoorPtr = coor[1];
                }
            });
            return (xCoor, yCoor);
        }
        private static void Computation(string input, int row, int column)
        {
            (DoubleMatrix xCoor, DoubleMatrix yCoor) = SetCoor(row, column);
            if (!complex_mode) Output_table = new RealSub(input, xCoor, yCoor, row, column).Obtain();
            else output_table = new ComplexSub(input, xCoor, yCoor, row, column).Obtain();
        }
        private static void PrepareAxes(Graphics g)
        {
            if (is_checking) return;
            DrawBorders(g);
            if (!retain_graph)
            {
                g.FillRectangle(new SolidBrush(Color.Black), x_left + 1, y_up + 1, x_right - x_left - 1, y_down - y_up - 1);
                SetAxesDrawn(false);
            }
            if (!delete_coordinate && !(is_main ? Axes_drawn : axes_drawn))
            {
                DrawAxesGrid(g, scopes, borders);
                SetAxesDrawn(true);
            }
        }
        private static void DrawBorders(Graphics g)
        {
            Pen BlackPen = new(Color.White, 1);
            g.DrawLine(BlackPen, x_left, y_up, x_right, y_up);
            g.DrawLine(BlackPen, x_right, y_up, x_right, y_down);
            g.DrawLine(BlackPen, x_right, y_down, x_left, y_down);
            g.DrawLine(BlackPen, x_left, y_down, x_left, y_up);
        }
        private static void DrawAxesGrid(Graphics g, double[] scopes, int[] borders)
        {
            double xGrid = Math.Pow(5, Math.Floor(Math.Log((scopes[1] - scopes[0]) / 2) / Math.Log(5)));
            double yGrid = Math.Pow(5, Math.Floor(Math.Log((scopes[2] - scopes[3]) / 2) / Math.Log(5)));
            DrawGrid(g, scopes, borders, xGrid, yGrid, 3);
            DrawGrid(g, scopes, borders, xGrid / 5, yGrid / 5, 2);
            DrawAxes(g, scopes, borders);
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void DrawGrid(Graphics g, double[] scopes, int[] borders, double xGrid, double yGrid, float penWidth)
        {
            Pen gridPen = new(GRID_GRAY, penWidth);
            for (int i = (int)Math.Floor(scopes[3] / yGrid); i <= (int)Math.Ceiling(scopes[2] / yGrid); i++)
            {
                int gridPosition = Transform(0, i * yGrid, scopes, borders)[1];
                if (gridPosition > borders[2] && gridPosition < borders[3])
                    g.DrawLine(gridPen, borders[0] + 1, gridPosition, borders[1], gridPosition);
            }
            for (int i = (int)Math.Floor(scopes[0] / xGrid); i <= (int)Math.Ceiling(scopes[1] / xGrid); i++)
            {
                int gridPosition = Transform(i * xGrid, 0, scopes, borders)[0];
                if (gridPosition > borders[0] && gridPosition < borders[1])
                    g.DrawLine(gridPen, gridPosition, borders[3], gridPosition, borders[2] + 1);
            }
        }
        private static void DrawAxes(Graphics g, double[] scopes, int[] borders)
        {
            Pen axisPen = new(Color.DarkGray, 4);
            int[] axisPositions = Transform(0, 0, scopes, borders);
            if (axisPositions[1] > borders[2] && axisPositions[1] < borders[3])
                g.DrawLine(axisPen, borders[0] + 1, axisPositions[1], borders[1], axisPositions[1]);
            if (axisPositions[0] > borders[0] && axisPositions[0] < borders[1])
                g.DrawLine(axisPen, axisPositions[0], borders[3], axisPositions[0], borders[2] + 1);
        }
        private static void SetAxesDrawn(bool drawn)
        {
            if (is_main) Axes_drawn = drawn;
            else axes_drawn = drawn;
        }
        #endregion

        #region Graphing
        private void DisplayBase(Action renderModes)
        {
            if (is_checking) return;
            Graphics g = CreateGraphics();
            BeginRendering();
            renderModes();  // Dynamically render complex or real modes
            PrepareAxes(g);
            g.DrawImage(bitmap, 0, 0);
            EndRendering();
        }
        private ComplexMatrix DisplayMini(string input, ComplexMatrix z, ComplexMatrix Z)
        {
            int row = x_right - x_left, column = y_down - y_up;
            output_table = new ComplexSub(input, z, Z, row, column).Obtain();
            DisplayBase(ComplexComputation);
            return output_table;
        }
        private DoubleMatrix DisplayMini(string input, DoubleMatrix x, DoubleMatrix y, DoubleMatrix X)
        {
            int row = x_right - x_left, column = y_down - y_up;
            Output_table = new RealSub(input, x, y, X, new(row, column), row, column).Obtain();
            DisplayBase(() => RealComputation(row, column));
            return Output_table;
        }
        private void DisplayPro(string input)
        {
            int row = x_right - x_left, column = y_down - y_up;
            Computation(input, row, column);
            DisplayBase(() => {
                if (!complex_mode) RealComputation(row, column);
                else ComplexComputation();
            });
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void DisplayBase(string input, bool isPolar = false, bool isParam = false)
        {
            times = 0;
            string[] split = MyString.SplitString(input);
            Graphics g = CreateGraphics();
            PrepareAxes(g);
            int penWidth = (int)(PARAM_WIDTH * RealSub.Obtain(ThickInput.Text));
            Pen curve_pen = new(swap_colors ? Color.Black : Color.White, penWidth);
            Pen colorful_pen = new(Color.Black, penWidth);
            Pen black_pen = new(swap_colors ? Color.White : Color.Black, penWidth);
            Pen white_pen = new(swap_colors ? Color.Black : Color.White, penWidth);
            Pen blue_pen = new(swap_colors ? LOWER_BLUE : UPPER_GOLD, penWidth);
            Pen yellow_pen = new(swap_colors ? UPPER_GOLD : LOWER_BLUE, penWidth);
            double relative_speed = RealSub.Obtain(DenseInput.Text);
            double start = 0, ending = 0, increment = INCREMENT_DEFAULT;
            if (isParam)
            {
                start = RealSub.Obtain(split[3]);
                ending = RealSub.Obtain(split[4]);
                if (split.Length > 5) increment = RealSub.Obtain(split[5]);
            }
            else if (isPolar)
            {
                start = RealSub.Obtain(split[2]);
                ending = RealSub.Obtain(split[3]);
                if (split.Length > 4) increment = RealSub.Obtain(split[4]);
            }
            else
            {
                double range = RealSub.Obtain(GeneralInput.Text);
                double range_1 = (GeneralInput.Text == "0") ? RealSub.Obtain(X_Left.Text) : -range;
                double range_2 = (GeneralInput.Text == "0") ? RealSub.Obtain(X_Right.Text) : range;
                switch (split.Length)
                {
                    case 1:
                        start = range_1;
                        ending = range_2;
                        break;
                    case 2:
                        increment = RealSub.Obtain(split[1]);
                        start = range_1;
                        ending = range_2;
                        break;
                    case 3:
                        start = RealSub.Obtain(split[1]);
                        ending = RealSub.Obtain(split[2]);
                        break;
                    case 4:
                        start = RealSub.Obtain(split[1]);
                        ending = RealSub.Obtain(split[2]);
                        increment = RealSub.Obtain(split[3]);
                        break;
                }
            }
            int Length = (int)Math.Abs((start - ending) / increment) + 2;
            double[,] coor = new double[2, Length]; // Efficient memory access
            bool[] in_range = new bool[Length];
            int[,] pos = new int[2, Length]; // Efficient memory access
            double xCoor_left = InverseTransform(x_left, 0, scopes, borders)[0];
            double xCoor_right = InverseTransform(x_right, 0, scopes, borders)[0];
            double yCoor_up = InverseTransform(0, y_up, scopes, borders)[1];
            double yCoor_down = InverseTransform(0, y_down, scopes, borders)[1];
            string input_1 = "x";
            string input_2 = split[0];
            if (isPolar || isParam)
            {
                input_1 = isPolar ? $"({split[0]})*~c({split[1]})".Replace(split[1], "x") : split[0].Replace(split[2], "x");
                input_2 = isPolar ? $"({split[0]})*~s({split[1]})".Replace(split[1], "x") : split[1].Replace(split[2], "x");
            }
            DoubleMatrix Steps = new(1, Length); // Efficient memory access
            double temp = start;
            if (is_checking)
            {
                coor[0, 0] = RealSub.Obtain(input_1, temp);
                coor[1, 0] = RealSub.Obtain(input_2, temp);
                return;
            }
            else
            {
                for (int i = 0; i < Length; i++, temp += increment) Steps[0, i] = temp;
                DoubleMatrix value_1 = new RealSub(input_1, Steps, 1, Length).Obtain();
                DoubleMatrix value_2 = new RealSub(input_2, Steps, 1, Length).Obtain();
                for (int i = 0; i < Length; i++)
                {
                    coor[0, i] = value_1[0, i];
                    coor[1, i] = value_2[0, i];
                }
            }
            int reference = 0;
            for (double steps = start; steps <= ending; steps += increment)
            {
                pos[0, times] = Transform(coor[0, times], 0, scopes, borders)[0];
                pos[1, times] = Transform(0, coor[1, times], scopes, borders)[1];
                in_range[times] = coor[0, times] > xCoor_left && coor[0, times] < xCoor_right &&
                                                 coor[1, times] > yCoor_down && coor[1, times] < yCoor_up;
                if (times > 0 && in_range[times - 1] && in_range[times])
                {
                    double ratio = relative_speed * (steps - start) / (ending - start) % 1;
                    Pen selectedPen = color_mode switch
                    {
                        1 => curve_pen,
                        2 => ratio < 0.5 ? white_pen : black_pen,
                        3 => ratio < 0.5 ? blue_pen : yellow_pen,
                        _ => colorful_pen
                    };
                    if (color_mode != 1 && color_mode != 2 && color_mode != 3) colorful_pen.Color = ObtainWheelCurve(ratio);
                    g.DrawLine(selectedPen, pos[0, times - 1], pos[1, times - 1], pos[0, times], pos[1, times]);
                    VScrollBarX.Enabled = VScrollBarY.Enabled = true; // This is necessary for each loop
                    ScrollMoving(pos[0, times], pos[1, times]);
                    if (reference != (int)(ratio * 100)) DrawReferenceRectangles(selectedPen.Color);
                    reference = (int)(ratio * 100);
                }
                times++; // This is a sensitive position
            }
            point_number += times; times = 0;
            EndRendering();
        }
        private void DisplayFunction(string input) => DisplayBase(input);
        private void DisplayPolar(string input) => DisplayBase(input, isPolar: true);
        private void DisplayParam(string input) => DisplayBase(input, isParam: true);
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void DisplayLoop(string input)
        {
            input = MyString.ReplaceTagCurves(input); // This is necessary
            string[] split = MyString.SplitString(input);
            if (input.Contains('δ')) { DisplayIterateLoop(split); return; }
            Action<string> displayMethod = 
                input.Contains('α') ? DisplayFunction : input.Contains('β') ? DisplayPolar : input.Contains('γ') ? DisplayParam : DisplayPro;
            for (int times = MyString.ToInt(split[2]), int3 = MyString.ToInt(split[3]); times <= int3; times++)
                displayMethod(split[0].Replace(split[1], MyString.IndexSub(times)));
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void DisplayIterateLoop(string[] split)
        {
            int row = x_right - x_left, column = y_down - y_up;
            (DoubleMatrix xCoor, DoubleMatrix yCoor) = SetCoor(row, column);
            if (complex_mode)
            {
                ComplexMatrix table_initial = new(row, column);
                Parallel.For(0, row, i => {
                    double* xCoorPtr = xCoor.RowPtr(i), yCoorPtr = yCoor.RowPtr(i); Complex* tablePtr = table_initial.RowPtr(i);
                    for (int j = 0; j < column; j++,xCoorPtr++, yCoorPtr++, tablePtr++) *tablePtr = new(*xCoorPtr, *yCoorPtr);
                });
                ComplexMatrix table_inherit = new ComplexSub(split[1], table_initial, row, column).Obtain();
                if (split.Length != 5) throw new FormatException();
                for (int times = MyString.ToInt(split[3]), int4 = MyString.ToInt(split[4]); times <= int4; times++)
                {
                    string temp_string = split[0].Replace(split[2], MyString.IndexSub(times));
                    table_inherit = DisplayMini(temp_string, table_initial, table_inherit);
                    if (is_checking) break;
                }
            }
            else
            {
                DoubleMatrix table_inherit = new RealSub(split[1], xCoor, yCoor, row, column).Obtain();
                if (split.Length != 6) throw new FormatException();
                for (int times = MyString.ToInt(split[3]), int4 = MyString.ToInt(split[4]); times <= int4; times++)
                {
                    string temp = split[0].Replace(split[2], MyString.IndexSub(times));
                    table_inherit = new RealSub(temp, xCoor, yCoor, table_inherit, table_inherit, row, column).Obtain();
                    DisplayMini(split[5], xCoor, yCoor, table_inherit);
                    if (is_checking) break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculateAlpha(Complex input) => (int)(255 / (1 + decay_rate * Complex.Modulus(input)));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int region, int proportion) CalculatePhase(double argument)
        {
            int proportion, region = argument < 0 ? -1 : (int)(3 * argument / Math.PI);
            if (region > 5) { region = proportion = 0; }
            else proportion = (int)(255 * (argument - region * (Math.PI / 3)) / (Math.PI / 3));
            return (region, proportion);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Color Obtain(int region, int proportion, int alpha) => ObtainAlpha(region, proportion, 1, alpha);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Color ObtainAlpha(int region, int proportion, double alpha, int beta) => region switch
        {
            0 => Color.FromArgb(beta, (int)(255 * alpha), (int)(proportion * alpha), 0),
            1 => Color.FromArgb(beta, (int)((255 - proportion) * alpha), (int)(255 * alpha), 0),
            2 => Color.FromArgb(beta, 0, (int)(255 * alpha), (int)(proportion * alpha)),
            3 => Color.FromArgb(beta, 0, (int)((255 - proportion) * alpha), (int)(255 * alpha)),
            4 => Color.FromArgb(beta, (int)(proportion * alpha), 0, (int)(255 * alpha)),
            5 => Color.FromArgb(beta, (int)(255 * alpha), 0, (int)((255 - proportion) * alpha)),
            _ => Color.Empty
        };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Color ObtainWheel(Complex input) => ObtainWheelInternal(false, input, 255);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Color ObtainWheel(Complex input, double alpha)
            => ObtainWheelInternal(true, input, 255, Math.Clamp(alpha, 0, 1));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Color ObtainWheelAlpha(Complex input) => ObtainWheelInternal(false, input, CalculateAlpha(input));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Color ObtainWheelAlpha(Complex input, double alpha)
            => ObtainWheelInternal(true, input, CalculateAlpha(input), Math.Clamp(alpha, 0, 1));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Color ObtainWheelInternal(bool isAlpha, Complex input, int calculatedAlpha, double alpha = 0)
        {
            (int color_region, int temp_proportion) = CalculatePhase(ComplexSub.ArgumentForRGB(input));
            return !isAlpha ? Obtain(color_region, temp_proportion, calculatedAlpha) 
                : ObtainAlpha(color_region, temp_proportion, alpha, calculatedAlpha);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Color ObtainWheelCurve(double alpha)
        {
            (int color_region, int temp_proportion) = CalculatePhase(Math.Clamp(alpha, 0, 1) * Math.Tau);
            return Obtain(color_region, temp_proportion, 255);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Color GetColorFromAlpha(double alpha, double beta, bool useBeta)
        {
            if (alpha <= 0.5) // From blue to purple
            {
                int red = (int)(510 * alpha), green = 0, blue = 255;
                return Color.FromArgb(useBeta ? (int)(red * beta) : red, green, (int)(blue * (useBeta ? beta : 1)));
            }
            else // From purple to red
            {
                int temp = 255 - (int)(510 * (alpha - 0.5));
                return Color.FromArgb((int)(255 * (useBeta ? beta : 1)), 0, (int)(temp * (useBeta ? beta : 1)));
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Color ObtainBase(double input, double beta, double min, double max, bool useBeta)
        {
            double alpha = (input - min) / (max - min);
            if (alpha >= 0 && alpha <= 1) return GetColorFromAlpha(alpha, useBeta ? beta : 1, useBeta);
            return Color.Empty;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Color ObtainStrip(double input, double min, double max) => ObtainBase(input, 1, min, max, false);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Color ObtainStripAlpha(double input, double beta, double min, double max)
            => ObtainBase(input, beta, min, max, true);
        #endregion

        #region Mouse
        private void Graph_MouseMove(object sender, MouseEventArgs e)
        {
            if (!(activate_mousemove && clicked && !text_changed && !String.IsNullOrEmpty(InputString.Text))) return;
            if (!is_main && e.X > X_LEFT_MIC && e.X < X_RIGHT_MIC && e.Y > Y_UP_MIC && e.Y < Y_DOWN_MIC)
                RunMouseMove(sender, e, X_LEFT_MIC, X_RIGHT_MIC, Y_UP_MIC, Y_DOWN_MIC);
            else if (is_main && e.X > X_LEFT_MAC && e.X < X_RIGHT_MAC && e.Y > Y_UP_MAC && e.Y < Y_DOWN_MAC)
                RunMouseMove(sender, e, X_LEFT_MAC, X_RIGHT_MAC, Y_UP_MAC, Y_DOWN_MAC);
            else DefaultReference(false);
        }
        private void Graph_MouseDown(object sender, MouseEventArgs e)
        {
            if (!(activate_mousemove && clicked && !text_changed && !String.IsNullOrEmpty(InputString.Text))) return;
            if (!is_main && e.X > X_LEFT_MIC && e.X < X_RIGHT_MIC && e.Y > Y_UP_MIC && e.Y < Y_DOWN_MIC)
                RunMouseDown(e, X_LEFT_MIC, X_RIGHT_MIC, Y_UP_MIC, Y_DOWN_MIC);
            else if (is_main && e.X > X_LEFT_MAC && e.X < X_RIGHT_MAC && e.Y > Y_UP_MAC && e.Y < Y_DOWN_MAC)
                RunMouseDown(e, X_LEFT_MAC, X_RIGHT_MAC, Y_UP_MAC, Y_DOWN_MAC);
        }
        private static void HandleMouseAction(MouseEventArgs e, int x_left, int x_right, int y_up, int y_down, Action<double, double> action)
        {
            int[] borders = new int[] { x_left, x_right, y_up, y_down };
            action(InverseTransform(e.X, e.Y, scopes, borders)[0], InverseTransform(e.X, e.Y, scopes, borders)[1]);
        }
        private void RunMouseMove(object sender, MouseEventArgs e, int x_left, int x_right, int y_up, int y_down)
        {
            SetReference(sender, e);
            HandleMouseAction(e, x_left, x_right, y_up, y_down, (xCoor, yCoor)
                => { ScrollMoving(xCoor, yCoor); DisplayMouseMove(e, xCoor, yCoor); });
        }
        private void RunMouseDown(MouseEventArgs e, int x_left, int x_right, int y_up, int y_down)
        {
            points_chosen++;
            HandleMouseAction(e, x_left, x_right, y_up, y_down, (xCoor, yCoor) => { DisplayMouseDown(e, xCoor, yCoor); });
        }
        private void DisplayMouseMove(MouseEventArgs e, double xCoor, double yCoor)
        {
            X_CoorDisplay.Text = MyString.TrimLargeDouble(xCoor, 1000000);
            Y_CoorDisplay.Text = MyString.TrimLargeDouble(yCoor, 1000000);
            ModulusDisplay.Text = MyString.TrimLargeDouble(Math.Sqrt(xCoor * xCoor + yCoor * yCoor), 1000000);
            AngleDisplay.Text = (ComplexSub.ArgumentForRGB(xCoor, yCoor) / Math.PI).ToString("#0.00000") + " * PI";
            if (!MyString.ContainFunctionName(InputString.Text))
            {
                if (!complex_mode) DisplayValuesReal(e.X, e.Y);
                else DisplayValuesComplex(e.X, e.Y);
            }
            else FunctionDisplay.Text = "Unavailable in this mode.";
        }
        private void DisplayMouseDown(MouseEventArgs e, double xCoor, double yCoor)
        {
            string X_Coor = MyString.TrimLargeDouble(xCoor, 100);
            string Y_Coor = MyString.TrimLargeDouble(yCoor, 100);
            string Modulus = MyString.TrimLargeDouble(Math.Sqrt(xCoor * xCoor + yCoor * yCoor), 100);
            string Angle = (ComplexSub.ArgumentForRGB(xCoor, yCoor) / Math.PI).ToString("#0.000000") + " * PI";
            if (!MyString.ContainFunctionName(InputString.Text))
            {
                string ValueDisplay;
                if (complex_mode)
                {
                    Complex clicked_value = output_table[e.X - 1 - x_left, e.Y - 1 - y_up];
                    ValueDisplay = $"Re = {MyString.TrimLargeDouble(clicked_value.real, 100)}\r\n" +
                        $"Im = {MyString.TrimLargeDouble(clicked_value.imaginary, 100)}";
                }
                else
                {
                    double clicked_value = Output_table[e.X - 1 - x_left, e.Y - 1 - y_up];
                    ValueDisplay = $"f(x, y) = {MyString.TrimLargeDouble(clicked_value, 100)}";
                }
                DraftBox.Text = $"\r\n>>>>> POINT_{points_chosen} <<<<<\r\n\r\nx = {X_Coor}\r\ny = {Y_Coor}" +
                    $"\r\n\r\nmod = {Modulus}\r\narg = {Angle}\r\n\r\n{ValueDisplay}\r\n" + DraftBox.Text;
            }
            else DraftBox.Text = $"\r\n>>>>> POINT_{points_chosen} <<<<<\r\n\r\nx = {X_Coor}\r\ny = {Y_Coor}" +
                    $"\r\n\r\nmod = {Modulus}\r\narg = {Angle}\r\n" + DraftBox.Text;
        }
        private void DisplayValuesComplex(int xCoor, int yCoor)
        {
            Complex clicked_value = output_table[xCoor - 1 - x_left, yCoor - 1 - y_up];
            FunctionDisplay.Text = $"[Re] {clicked_value.real}\r\n[Im] {clicked_value.imaginary}";
        }
        private void DisplayValuesReal(int xCoor, int yCoor)
            => FunctionDisplay.Text = Output_table[xCoor - 1 - x_left, yCoor - 1 - y_up].ToString();
        private void DrawReferenceRectangles(Color color)
            => CreateGraphics().FillRectangle(new SolidBrush(color), VScrollBarX.Location.X - REF_POS_1, Y_UP_MIC + REF_POS_2,
                2 * (VScrollBarX.Width + REF_POS_1), VScrollBarX.Height - 2 * REF_POS_2);
        private void DefaultReference(bool isInitial)
        {
            if (!isInitial) Cursor = Cursors.Default;
            DrawReferenceRectangles(SystemColors.ControlDark);
            if (!isInitial) VScrollBarX.Enabled = VScrollBarY.Enabled = false;
        }
        private void SetReference(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Cross;
            DrawReferenceRectangles(GetMouseColor(sender, e));
            VScrollBarX.Enabled = VScrollBarY.Enabled = true;
        }
        private static Color GetMouseColor(object sender, EventArgs e)
        {
            Bitmap bmp = new(1, 1);
            Graphics.FromImage(bmp).CopyFromScreen(Cursor.Position, Point.Empty, new Size(1, 1));
            return bmp.GetPixel(0, 0);
        }
        private void ScrollMoving(double xCoor, double yCoor)
        {
            VScrollBarX.Value = (int)((VScrollBarX.Maximum - VScrollBarX.Minimum) * (xCoor - scopes[0]) / (scopes[1] - scopes[0]));
            VScrollBarY.Value = (int)((VScrollBarY.Maximum - VScrollBarY.Minimum) * (yCoor - scopes[3]) / (scopes[2] - scopes[3]));
        }
        private void ScrollMoving(int xPos, int yPos)
            => ScrollMoving(InverseTransform(xPos, yPos, scopes, borders)[0], InverseTransform(xPos, yPos, scopes, borders)[1]);
        private async void ConfirmButton_Click(object sender, EventArgs e)
            => await ExecuteAsync(() => RunConfirmButton_Click(sender, e));
        private async void PreviewButton_Click(object sender, EventArgs e)
            => await ExecuteAsync(() => RunPreviewButton_Click(sender, e));
        private async void AllButton_Click(object sender, EventArgs e)
            => await ExecuteAsync(() => RunAllButton_Click(sender, e));
        private void RunButtonClick(Action endAction, int xLeft, int xRight, int yUp, int yDown, bool isMain)
        {
            try
            {
                PrepareGraphing();
                PrepareScopes(xLeft, xRight, yUp, yDown, isMain);
                SetTDSB();
                DisplayOnScreen();
                endAction();
            }
            catch (Exception) { ErrorBox("THE INPUT IS IN A WRONG FORMAT."); }
        }
        private void RunConfirmButton_Click(object sender, EventArgs e)
            => RunButtonClick(EndMacro, X_LEFT_MAC, X_RIGHT_MAC, Y_UP_MAC, Y_DOWN_MAC, true);
        private void RunPreviewButton_Click(object sender, EventArgs e)
            => RunButtonClick(EndMicro, X_LEFT_MIC, X_RIGHT_MIC, Y_UP_MIC, Y_DOWN_MIC, false);
        private void RunAllButton_Click(object sender, EventArgs e)
        {
            try
            {
                PrepareGraphing();
                PrepareScopes(X_LEFT_MIC, X_RIGHT_MIC, Y_UP_MIC, Y_DOWN_MIC, false);
                SetTDSB();
                DisplayOnScreen();
                MiddleAll();
                PrepareScopes(X_LEFT_MAC, X_RIGHT_MAC, Y_UP_MAC, Y_DOWN_MAC, true);
                SetTDSB();
                DisplayOnScreen();
                EndMacro();
            }
            catch (Exception) { ErrorBox("THE INPUT IS IN A WRONG FORMAT."); }
        }
        private async Task ExecuteAsync(Action action)
        {
            BlockInput(true);
            PrepareAsync();
            await Task.Run(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
                action();
            });
            RunCopyToClipboard();
            BlockInput(false);
        }
        private void PrepareAsync()
        {
            elapsedSeconds = 0;
            TimeDisplay.Text = "0s";
            if (!String.IsNullOrEmpty(InputString.Text))
            {
                DisplayTimer.Start(); WaitTimer.Start(); GraphTimer.Start();
                TimeNow = DateTime.Now;
                waiting = false;
            }
            else PointNumDisplay.Text = "0";
        }
        private void CopyToClipboard()
        {
            if (String.IsNullOrEmpty(InputString.Text)) return;
            Clipboard.SetText(InputString.Text);
        }
        private void RunCopyToClipboard()
        {
            // Ensure clipboard operation is done on the UI thread
            if (InvokeRequired) Invoke((MethodInvoker)delegate { CopyToClipboard(); });
            else CopyToClipboard();
        }
        private void PrepareGraphing()
        {
            if (String.IsNullOrEmpty(InputString.Text)) return;
            RestoreMelancholy();
            DisableControls();
            point_number = times = export_number = 0;
            address_error = is_checking = text_changed = false;
            clicked = true; plot_loop++;
        }
        private static void PrepareScopes(int xLeft, int xRight, int yUp, int yDown, bool isMain)
        { is_main = isMain; x_left = xLeft; x_right = xRight; y_up = yUp; y_down = yDown; }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void DisplayOnScreen()
        {
            if (String.IsNullOrEmpty(InputString.Text)) return;
            string[] split = MyString.SplitByChars(RecoverMultiply.BeautifyInput(InputString.Text, complex_mode), new char[] { '|' });
            for (int loops = 0, length = split.Length; loops < length; loops++)
            {
                string splitLoops = split[loops];
                bool temp = !MyString.ContainsAny(splitLoops, new string[] { "Loop", "loop" }) && !complex_mode;
                if (MyString.ContainsAny(splitLoops, new string[] { "Func", "func" }) && temp) DisplayFunction(splitLoops);
                else if (MyString.ContainsAny(splitLoops, new string[] { "Polar", "polar" }) && temp) DisplayPolar(splitLoops);
                else if (MyString.ContainsAny(splitLoops, new string[] { "Param", "param" }) && temp) DisplayParam(splitLoops);
                else if (MyString.ContainsAny(splitLoops, new string[] { "Loop", "loop" })) DisplayLoop(splitLoops);
                else DisplayPro(splitLoops);
            }
        }
        private void TimeDraft(string mode, int plotLoop)
        {
            TimeDisplay.Text = $"{TimeCount:hh\\:mm\\:ss\\.fff}";
            if (!drafted) DraftBox.Text = String.Empty; drafted = true;
            DraftBox.Text = $"\r\n>>>> {plotLoop}_{mode} <<<<\r\n\r\n{InputString.Text}\r\n\r\n" +
                $"Pixels: {PointNumDisplay.Text}\r\nDuration: {TimeDisplay.Text}\r\n" + DraftBox.Text;
        }
        private void MiddleAll()
        {
            GraphTimer.Stop();
            TimeCount = DateTime.Now - TimeNow;
            TimeDraft("MICRO", plot_loop);
            plot_loop++;
            GraphTimer.Start();
            TimeNow = DateTime.Now;
        }
        private void EndProcess(string mode, int plotLoop, bool updateCaption)
        {
            ActivateControls();
            main_drawn = updateCaption;
            if (main_drawn) CaptionBox.Text = $"{InputString.Text}\r\n" + CaptionBox.Text;
            TimeDraft(mode, plotLoop);
            InputString.Focus(); InputString.SelectionStart = InputString.Text.Length;
            if (auto_export && !address_error) RunStoreButton_Click();
        }
        private void EndMicro() => EndProcess("MICRO", plot_loop, false);
        private void EndMacro() => EndProcess("MACRO", plot_loop, true);
        private void SetTextboxButton(bool readOnly)
        {
            TextBox[] textBoxes = new[] 
            { InputString, GeneralInput, X_Left, X_Right, Y_Left, Y_Right, ThickInput, DenseInput, AddressInput };
            foreach (TextBox textBox in textBoxes) textBox.ReadOnly = readOnly;
            ConfirmButton.Enabled = PreviewButton.Enabled = AllButton.Enabled = !readOnly;
            activate_mousemove = !readOnly; commence_waiting = readOnly;
        }
        private void DisableControls() => SetTextboxButton(true);
        private void ActivateControls()
        {
            DisplayTimer.Stop(); WaitTimer.Stop(); GraphTimer.Stop(); // These shall be here
            TimeCount = DateTime.Now - TimeNow;
            SetTextboxButton(false);
            PictureWait.Visible = VScrollBarX.Enabled = VScrollBarY.Enabled = false;
            GC.Collect();
        }
        private void ErrorBox(string message)
        {
            if (!is_checking) clicked = false;
            ExceptionMessageBox.Show(message +
                "\r\nCommon mistakes include:" +
                "\r\n\r\n1. Misspelling of function/variable names;" +
                "\r\n2. Incorrect grammar of special functions;" +
                "\r\n3. Excess or deficiency of characters;" +
                "\r\n4. Real/Complex mode confusion;", 450, 300);
            if (!is_checking) ActivateControls();
        }
        private void ExportButton_Click(object sender, EventArgs e)
        { RestoreMelancholy(); RunExportButton_Click(); }
        private void StoreButton_Click(object sender, EventArgs e)
        { RestoreMelancholy(); RunStoreButton_Click(); }
        private void RunExportButton_Click() => HandleExportOrStore(ExportGraph, "Snapshot saved at");
        private void RunStoreButton_Click() => HandleExportOrStore(ExportHistory, "History stored at");
        private void HandleExportOrStore(Action exportAction, string messagePrefix)
        {
            try
            {
                if (String.IsNullOrEmpty(AddressInput.Text)) AddressInput.Text = ADDRESS_DEFAULT;
                exportAction();
                if (!drafted) DraftBox.Text = String.Empty;
                DraftBox.Text = $"\r\n{messagePrefix} \r\n{DateTime.Now:HH_mm_ss}\r\n" + DraftBox.Text;
                drafted = true;
            }
            catch (Exception)
            {
                clicked = false; address_error = true;
                ExceptionMessageBox.Show("THE ADDRESS DOES NOT EXIST." +
                    "\r\nCommon mistakes include:" +
                    "\r\n\r\n1. Files not created beforehand;" +
                    "\r\n2. The address ending with \\;" +
                    "\r\n3. The address quoted automatically;" +
                    "\r\n4. The file storage being full;", 450, 300);
            }
        }
        private void ExportGraph()
        {
            export_number++;
            Bitmap bmp = new(Width - 22, Height - 55);
            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(Left + 11, Top + 45, 0, 0, bmp.Size);
            bmp.Save($@"{AddressInput.Text}\{DateTime.Now:yyyy}_{DateTime.Now.DayOfYear}_{DateTime.Now:HH}_{DateTime.Now:mm}_{DateTime.Now:ss}_No.{export_number}.png");
        }
        private void ExportHistory()
        {
            StreamWriter writer = new($@"{AddressInput.Text}\{DateTime.Now:yyyy}_{DateTime.Now.DayOfYear}_{DateTime.Now:HH}_{DateTime.Now:mm}_{DateTime.Now:ss}_stockpile.txt");
            writer.Write(DraftBox.Text);
        }
        #endregion

        #region Keys
        private void FalseColor()
        {
            InputString.BackColor = InputLabel.ForeColor = ERROR_RED;
            PictureIncorrect.Visible = true; PictureCorrect.Visible = false;
        }
        private void CheckValidity() => CheckValidityCore(FalseColor);
        private void CheckValidityDetailed() => CheckValidityCore(() => ErrorBox("THE INPUT IS IN A WRONG FORMAT."));
        private void CheckValidityCore(Action errorHandler)
        {
            try
            {
                is_checking = true;
                PrepareScopes(X_LEFT_CHECK, X_RIGHT_CHECK, Y_UP_CHECK, Y_DOWN_CHECK, false);
                SetTDSB();
                if (String.IsNullOrEmpty(InputString.Text))
                {
                    InputString.BackColor = FOCUS_GRAY;
                    InputLabel.ForeColor = Color.White;
                    PictureIncorrect.Visible = PictureCorrect.Visible = false;
                }
                else
                {
                    DisplayOnScreen();
                    InputString.BackColor = InputLabel.ForeColor = CORRECT_GREEN;
                    PictureIncorrect.Visible = false; PictureCorrect.Visible = true;
                }
                activate_mousemove = false;
                VScrollBarX.Enabled = VScrollBarY.Enabled = false;
            }
            catch (Exception) { errorHandler(); }
        }
        private void MiniChecks(Control Ctrl, Control ctrl)
        {
            try
            {
                if (InputString.ReadOnly) return;
                if (String.IsNullOrEmpty(Ctrl.Text)) ctrl.ForeColor = Color.White;
                else
                {
                    double temp = RealSub.Obtain(Ctrl.Text); // For checking
                    ctrl.ForeColor = CORRECT_GREEN;
                }
            }
            catch (Exception) { ctrl.ForeColor = ERROR_RED; }
        }
        private void CheckAll(object sender, EventArgs e)
        {
            InputString_DoubleClick(sender, e);
            GeneralInput_DoubleClick(sender, e);
            X_Left_DoubleClick(sender, e);
            X_Right_DoubleClick(sender, e);
            Y_Left_DoubleClick(sender, e);
            Y_Right_DoubleClick(sender, e);
            ThickInput_DoubleClick(sender, e);
            DenseInput_DoubleClick(sender, e);
            AddressInput_DoubleClick(sender, e);
        }
        private void AutoCheckComplex()
        {
            string input = InputString.Text;
            input = MyString.ReplaceComplexConfusion(input);
            CheckComplex.Checked = MyString.ContainsAny(input, new char[] { 'z', 'Z' });
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BarSomeKeys(object sender, KeyPressEventArgs e)
        { if (BARREDCHARS.Contains(e.KeyChar)) e.Handled = true; }
        private static void AutoKeyDown(TextBox ctrl, KeyEventArgs e)
        {
            if (ctrl.ReadOnly) return;
            int caretPosition = ctrl.SelectionStart; // This is a necessary intermediate variable
            if (!MyString.CheckParenthesis(ctrl.Text.AsSpan(caretPosition, ctrl.SelectionLength)))
                SelectSuppress(ctrl, e, caretPosition, 0);
            else if (e.KeyCode == Keys.D9 && (ModifierKeys & Keys.Shift) != 0)
            {
                if (ctrl.SelectionLength == 0)
                {
                    ctrl.Text = ctrl.Text.Insert(caretPosition, "()");
                    SelectSuppress(ctrl, e, caretPosition, 1);
                }
                else
                {
                    string selectedText = ctrl.Text.Substring(caretPosition, ctrl.SelectionLength);
                    ctrl.Text = ctrl.Text.Remove(caretPosition, ctrl.SelectionLength).Insert(caretPosition, "(" + selectedText + ")");
                    SelectSuppress(ctrl, e, caretPosition, selectedText.Length + 2);
                }
            }
            else if (e.KeyCode == Keys.D0 && (ModifierKeys & Keys.Shift) != 0)
            {
                if (ctrl.SelectionLength > 0) SelectSuppress(ctrl, e, caretPosition, 0);
                else if (caretPosition == 0) SelectSuppress(ctrl, e, caretPosition, 0);
                else if (ctrl.Text[caretPosition - 1] == '(') SelectSuppress(ctrl, e, caretPosition, 1);
            }
            else if (e.KeyCode == Keys.Oemcomma)
            {
                ctrl.Text = ctrl.Text.Insert(caretPosition, ", ");
                SelectSuppress(ctrl, e, caretPosition, 2);
            }
            else if (e.KeyCode == Keys.OemPipe)
            {
                ctrl.Text = ctrl.Text.Insert(caretPosition, " | ");
                SelectSuppress(ctrl, e, caretPosition, 3);
            }
            else if (e.KeyCode == Keys.Back)
            {
                if (caretPosition == 0 || !MyString.CheckParenthesis(ctrl.Text) || ctrl.SelectionLength > 0) return;
                else if (ctrl.Text[caretPosition - 1] == '(')
                {
                    if (ctrl.Text[caretPosition] == ')') ctrl.Text = ctrl.Text.Remove(caretPosition - 1, 2);
                    SelectSuppress(ctrl, e, caretPosition, -1);
                }
                else if (ctrl.Text[caretPosition - 1] == ')') SelectSuppress(ctrl, e, caretPosition, -1);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SelectSuppress(TextBox ctrl, KeyEventArgs e, int caretPosition, int caretMove)
        {
            ctrl.SelectionStart = caretPosition + caretMove;
            e.SuppressKeyPress = true;
        }
        private void Graph_KeyUp(object sender, KeyEventArgs e)
        {
            HandleModifierKeys(e, false);
            if (suppressKeyUp) return;
            if (HandleSpecialKeys(e)) return;
            HandleCtrlCombination(sender, e);
        }
        private void Graph_KeyDown(object sender, KeyEventArgs e)
        {
            HandleModifierKeys(e, true);
            if (sftPressed && e.KeyCode == Keys.Back && !InputString.ReadOnly)
                ExecuteWithSuppression(() => SubtitleBox_DoubleClick(sender, e), e);
            else if (e.KeyCode == Keys.Delete) ExecuteWithSuppression(null, e);
        }
        private static void HandleModifierKeys(KeyEventArgs e, bool isKeyDown)
        {
            if (e.KeyCode == Keys.ControlKey) { ctrlPressed = isKeyDown; e.Handled = true; }
            else if (e.KeyCode == Keys.ShiftKey) { sftPressed = isKeyDown; e.Handled = true; }
        }
        private bool HandleSpecialKeys(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape: ExecuteWithSuppression(() => Close(), e); return true;
                case Keys.Oemtilde: ExecuteWithSuppression(() => PlayOrPause(), e); return true;
                case Keys.Delete: ExecuteWithSuppression(() => PressDelete(e), e); return true;
                default: return false;
            }
        }
        private void HandleCtrlCombination(object sender, KeyEventArgs e)
        {
            if (!ctrlPressed) return;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            Action action = e.KeyCode switch
            {
                Keys.D3 => () => ClearButton_Click(sender, e),
                Keys.D2 => () => PictureLogo_DoubleClick(sender, e),
                Keys.OemQuestion => () => TitleLabel_DoubleClick(sender, e),
                Keys.P when PreviewButton.Enabled => () => PreviewButton_Click(sender, e),
                Keys.G when ConfirmButton.Enabled => () => ConfirmButton_Click(sender, e),
                Keys.B when !InputString.ReadOnly => () => AllButton_Click(sender, e),
                Keys.S when ExportButton.Enabled => () => ExportButton_Click(sender, e),
                Keys.K => () => StoreButton_Click(sender, e),
                Keys.R => () => Graph_DoubleClick(sender, e),
                Keys.D => () => RestoreDefault(),
                Keys.C when sftPressed && !InputString.ReadOnly => () => CheckAll(sender, e),
                _ => null
            };
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            if (action != null) ExecuteWithSuppression(action, e);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ExecuteWithSuppression(Action action, KeyEventArgs e)
        {
            suppressKeyUp = true;
            action?.Invoke();
            e.Handled = true;
            e.SuppressKeyPress = true;
            suppressKeyUp = false;
        }
        private void PressDelete(KeyEventArgs e)
        {
            RestoreMelancholy();
            DeleteMain_Click(this, e);
            DeletePreview_Click(this, e);
        }
        private void Graph_DoubleClick(object sender, EventArgs e) => RestoreMelancholy();
        private void RestoreMelancholy()
        {
            InputString.BackColor = FOCUS_GRAY;
            Label[] labels = new[] { InputLabel, AtLabel, GeneralLabel, DetailLabel, X_Scope, Y_Scope, ThickLabel, DenseLabel, ExampleLabel, FunctionLabel, ModeLabel, ContourLabel };
            foreach (Label label in labels) label.ForeColor = Color.White;
            PictureIncorrect.Visible = PictureCorrect.Visible = false;
            is_checking = false;
        }
        private void RestoreDefault()
        {
            InputString.Text = INPUT_DEFAULT;
            InputString.SelectionStart = InputString.Text.Length;
            GeneralInput.Text = GENERAL_DEFAULT;
            ThickInput.Text = THICKNESS_DEFAULT;
            DenseInput.Text = DENSENESS_DEFAULT;
            AddressInput.Text = ADDRESS_DEFAULT;
            ComboColoring.SelectedIndex = 4;
            ComboContour.SelectedIndex = 1;
            CheckAuto.Checked = CheckSwap.Checked = CheckPoints.Checked = CheckShade.Checked = CheckRetain.Checked = false;
            CheckEdit.Checked = CheckComplex.Checked = CheckCoor.Checked = true;
        }
        #endregion

        #region Micellaneous
        private void ComboColoring_SelectedIndexChanged(object sender, EventArgs e)
            => color_mode = ComboColoring.SelectedIndex + 1;
        private void ComboContour_SelectedIndexChanged(object sender, EventArgs e)
            => contour_mode = ComboContour.SelectedIndex + 1;
        private void ComboExamples_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ComboExamples.SelectedIndex == -1 || InputString.ReadOnly || 
                String.IsNullOrEmpty(ComboExamples.SelectedItem.ToString())) return;
            InputString.Text = ComboExamples.SelectedItem.ToString();
            CheckComplex.Checked = ComboExamples.SelectedIndex < 8;
            SetValuesForSelectedIndex(ComboExamples.SelectedIndex);
            DeleteMain_Click(this, e);
            DeletePreview_Click(this, e);
            ComboExamples.SelectedIndex = -1;
            InputString.Focus();
            InputString.SelectionStart = InputString.Text.Length;
        }
        private void SetValuesForSelectedIndex(int index)
        {
            string generalScope;
            string thickness = "1";
            string denseness = "1";
            int colorIndex = 3;
            bool pointsChecked = false, retainChecked = false, shadeChecked = false;
            switch (index)
            {
                case 0: generalScope = "1.1"; colorIndex = 4; pointsChecked = true; break;
                case 1: generalScope = "1.2"; colorIndex = 3; break;
                case 2: generalScope = "1.1"; colorIndex = 2; pointsChecked = true; break;
                case 3: generalScope = "pi/2"; colorIndex = 4; break;
                case 4: generalScope = "4"; thickness = "0.1"; colorIndex = 3; shadeChecked = true; break;
                case 5: generalScope = "3"; break;
                case 6: generalScope = "0"; X_Left.Text = "-1.6"; X_Right.Text = "0.6"; Y_Left.Text = "-1.1"; Y_Right.Text = "1.1"; break;
                case 7: generalScope = "2"; colorIndex = 4; shadeChecked = true; break;
                case 9: generalScope = "4pi"; thickness = "0.1"; colorIndex = 2; pointsChecked = true; break;
                case 10: generalScope = "2pi"; colorIndex = 4; break;
                case 11: generalScope = "2.5"; thickness = "0.3"; colorIndex = 1; pointsChecked = true; break;
                case 12: generalScope = "0"; X_Left.Text = "0"; X_Right.Text = "1"; Y_Left.Text = "0"; Y_Right.Text = "1"; thickness = "0.2"; colorIndex = 0; retainChecked = true; break;
                case 13: generalScope = "10"; thickness = "0.1"; colorIndex = 1; pointsChecked = true; break;
                case 14: generalScope = "5"; colorIndex = 1; break;
                case 15: generalScope = "3"; break;
                case 16: generalScope = "4"; thickness = "0.05"; colorIndex = 2; pointsChecked = true; break;
                case 18: generalScope = "5.5"; break;
                case 19: generalScope = "pi"; thickness = "0.5"; denseness = "10"; colorIndex = 2; break;
                case 20: generalScope = "3"; colorIndex = 0; break;
                case 21: generalScope = "1.1"; denseness = "100"; colorIndex = 1; break;
                case 22: generalScope = "1.1"; thickness = "0.5"; colorIndex = 3; break;
                case 23: generalScope = "1.1"; thickness = "0.5"; denseness = "10"; retainChecked = true; break;
                case 24: generalScope = "1.1"; thickness = "0.5"; colorIndex = 3; break;
                case 25: generalScope = "0"; X_Left.Text = "-0.2"; X_Right.Text = "1.2"; Y_Left.Text = "-0.2"; Y_Right.Text = "1.2"; thickness = "0.5"; colorIndex = 0; retainChecked = true; break;
                default: ComboExamples.SelectedIndex = -1; return;
            }
            GeneralInput.Text = generalScope;
            ThickInput.Text = thickness;
            DenseInput.Text = denseness;
            ComboColoring.SelectedIndex = colorIndex;
            CheckPoints.Checked = pointsChecked;
            CheckRetain.Checked = retainChecked;
            CheckShade.Checked = shadeChecked;
        }
        private void ComboSelectionChanged(string selectedItem)
        {
            if (InputString.ReadOnly) return;
            InputString.Text = MyString.Replace(InputString.Text, selectedItem, InputString.SelectionStart, InputString.SelectionStart + InputString.SelectionLength - 1);
            InputString.SelectionStart += selectedItem.Length - 1;
            InputString.Focus();
        }
        private void ComboFunctions_SelectedIndexChanged(object sender, EventArgs e)
            => ComboSelectionChanged(ComboFunctions.SelectedItem.ToString());
        private void ComboSpecial_SelectedIndexChanged(object sender, EventArgs e)
            => ComboSelectionChanged(ComboSpecial.SelectedItem.ToString());
        private void CheckCoor_CheckedChanged(object sender, EventArgs e) => delete_coordinate = !delete_coordinate;
        private void CheckSwap_CheckedChanged(object sender, EventArgs e) => swap_colors = !swap_colors;
        private void CheckComplex_CheckedChanged(object sender, EventArgs e) => complex_mode = !complex_mode;
        private void CheckPoints_CheckedChanged(object sender, EventArgs e) => delete_point = !delete_point;
        private void CheckRetain_CheckedChanged(object sender, EventArgs e) => retain_graph = !retain_graph;
        private void CheckShade_CheckedChanged(object sender, EventArgs e) => shade_rainbow = !shade_rainbow;
        private void CheckAuto_CheckedChanged(object sender, EventArgs e)
        {
            auto_export = !auto_export;
            if (CheckAuto.Checked) AddressInput_DoubleClick(sender, e);
        }
        private void CheckEdit_CheckedChanged(object sender, EventArgs e)
        {
            DraftBox.ReadOnly = !DraftBox.ReadOnly;
            if (DraftBox.ReadOnly)
            {
                DraftBox.BackColor = Color.Black;
                DraftBox.ForeColor = READONLY_GRAY;
                DraftBox.ScrollBars = ScrollBars.None;
            }
            else
            {
                DraftBox.BackColor = SystemColors.ControlDarkDark;
                DraftBox.ForeColor = Color.White;
                DraftBox.ScrollBars = ScrollBars.Vertical;
            }
        }
        private void TitleLabel_DoubleClick(object sender, EventArgs e)
            => FormalMessageBox.Show("DESIGNER: Fraljimetry\r\nDATE: Oct, 2024\r\nLOCATION: Xi'an, China" +
                "\r\n\r\nThis software was developed in Visual Studio 2022, written in C# Winform, to visualize real or complex functions or equations with no more than two variables. To bolster artistry and practicality, numerous modes are rendered, making it possible to generate images that fit users' needs perfectly." +
                "\r\n\r\n(I wish the definitions of these operations are self-evident if you try some inputs yourself or refer to the examples.)" +
                "\r\n\r\n********** ELEMENTARY **********" +
                "\r\n\r\n+ - * / ^ ( )" +
                "\r\n\r\nLog/Ln, Exp, Sqrt, Abs, Sin, Cos, Tan, Sinh/Sh, Cosh/Ch, Tanh/Th, Arcsin/Asin, Arccos/Acos, Arctan/Atan, Arsinh/Arsh, Arccosh/Arch, Arctanh/Arth (f(x,y)/f(z))" +
                "\r\n\r\nConjugate/Conj(f(z)), e(f(z))    // e(z)=exp(2*pi*i*z)" +
                "\r\n\r\n********** COMBINATORICS **********" +
                "\r\n\r\nFloor(double a), Ceil(double a), Round(double a), " +
                "\r\nSign/Sgn(double a)" +
                "\r\n\r\nMod(double a, double n), nCr(int n, int r), nPr(int n, int r)" +
                "\r\n\r\nMax(double a, double b, ...), Min(double a, double b, ...), Factorial/Fact(int n)" +
                "\r\n\r\n********** SPECIAL FUNCTIONS **********" +
                "\r\n\r\nF(double/Complex a, double/Complex b, double/Complex c, f(x,y)/f(z)) / " +
                "\r\nF(double/Complex a, double/Complex b, double/Complex c, f(x,y)/f(z), int n)" +
                "\r\n// HyperGeometric Series." +
                "\r\n\r\nGamma/Ga(f(x,y)/f(z)) / Gamma/Ga(f(x,y)/f(z), int n)" +
                "\r\n\r\nBeta(f(x,y)/f(z), g(x,y)/g(z)) / " +
                "\r\nBeta(f(x,y)/f(z), g(x,y)/g(z), int n)" +
                "\r\n\r\nZeta(f(x,y)/f(z)) / Zeta(f(x,y)/f(z), int n)" +
                "\r\n// This is a mess for n too large." +
                "\r\n\r\n********** REPETITIOUS OPERATIONS **********" +
                "\r\n// Capitalizations represent substitutions of variables." +
                "\r\n\r\nSum(f(x,y,k)/f(z,k), k , int a, int b)" +
                "\r\nProduct/Prod(f(x,y,k)/f(z,k), k , int a, int b)" +
                "\r\n\r\nIterate1(f(x,y,X,k), g(x,y), k , int a, int b)" +
                "\r\nIterate2(f_1(x,y,X,Y,k), f_2(x,y,X,Y,k), g_1(x,y), g_2(x,y), k , int a, int b, int choice)" +
                "\r\nIterate(f(z,Z,k), g(z), k , int a, int b)" +
                "\r\n// g's are the initial values and f's are the iterations." +
                "\r\n\r\nComposite1/Comp1(f(x,y), g_1(x,y,X), ...,g_n(x,y,X))" +
                "\r\nComposite2/Comp2(f_1(x,y), f_2(x,y), g_1(x,y,X,Y), h_1(x,y,X,Y), ..., g_n(x,y,X,Y), h_n(x,y,X,Y), int choice)" +
                "\r\nComposite/Comp(f(z), g_1(z,Z), ..., g_n(z,Z))" +
                "\r\n// f's are the initial values and g's are the compositions." +
                "\r\n\r\n********** PLANAR CURVES **********" +
                "\r\n\r\nFunc(f(x)) / Func(f(x), double increment) / Func(f(x), double a, double b) / Func(f(x), double a, double b, double increment)" +
                "\r\n\r\nPolar(f(θ), θ, double a, double b) / Polar(f(θ), θ, double a, double b, double increment)" +
                "\r\n\r\nParam(f(u), g(u), u, double a, double b) / Param(f(u), g(u), u, double a, double b, double increment)" +
                "\r\n\r\n********** RECURSIONS **********" +
                "\r\n// These methods should be combined with the former." +
                "\r\n\r\nLoop(Input(k), k , int a, int b)" +
                "\r\n\r\nIterateLoop(f(x,y,X,k), g(x,y), k, int a, int b) / " +
                "\r\nIterateLoop(f(z,Z,k), g(z), k, int a, int b, h(x,y,X))" +
                "\r\n// Displaying each step of iteration." +
                "\r\n\r\n...|...|...    // Displaying one by one." +
                "\r\n\r\n********** CONSTANTS **********" +
                "\r\n\r\nPi, e, Gamma/Ga" +
                "\r\n\r\n(The following are shortcuts for instant operations.)" +
                "\r\n\r\n********** SHORTCUTS **********" +
                "\r\n" +
                "\r\n[Control + P] Graph in the MicroBox;" +
                "\r\n[Control + G] Graph in the MacroBox;" +
                "\r\n[Control + B] Graph in both regions;" +
                "\r\n[Control + S] Save as a snapshot;" +
                "\r\n[Control + K] Save the history as a txt file;" +
                "\r\n[Control + Shift + C] Check all inputs;" +
                "\r\n[Control + R] Erase all checks;" +
                "\r\n[Control + D] Restore to the default state;" +
                "\r\n[Shift + Back] Clear the InputBox;" +
                "\r\n[Control + D2] See the profile of Fraljimetry;" +
                "\r\n[Contorl + D3] Clear all ReadOnly displays;" +
                "\r\n[Control + OemQuestion] See the manual;" +
                "\r\n[Oemtilde] Play or pause the ambient music;" +
                "\r\n[Delete] Clear both graphing regions;" +
                "\r\n[Escape] Close Fraljiculator;" +
                "\r\n\r\nClick [Tab] to witness the process of control design.", 600, 450);
        private void PictureLogo_DoubleClick(object sender, EventArgs e)
            => FormalMessageBox.Show("Dear math lovers and mathematicians:" +
                "\r\n    Hi! I'm Fralji, a video uploader on Bilibili since July, 2021, right before entering college." +
                "\r\n    I aim to create unique lectures on many branches of mathematics. If you have any problem on the usage of this application, please contact me via one the following:" +
                "\r\n\r\nBilibili: 355884223" +
                "\r\n\r\nEmail: frankjiiiiiiii@gmail.com" +
                "\r\n\r\nWechat: F1r4a2n8k5y7 (recommended)" +
                "\r\n\r\nQQ: 472955101" +
                "\r\n\r\nFacebook: Fraljimetry" +
                "\r\n\r\nInstagram: shaodaji (NOT recommended)", 600, 450);
        private static void ShowCustomMessageBox(string title, string message)
            => CustomMessageBox.Show(title + "\r\n\r\n" + message, 450, 300);
        private void GeneralLabel_DoubleClick(object sender, EventArgs e)
            => ShowCustomMessageBox("[GENERAL SCOPE]", "The detailed scope effectuates only if the general scope is set to zero." + "\r\n\r\n" + "Any legitimate variable-free algebraic expressions are acceptable in this box, and will be checked as in the input box.");
        private void AtLabel_DoubleClick(object sender, EventArgs e)
            => ShowCustomMessageBox("[SAVING ADDRESS]", "You can create a file for snapshot storage and paste the address here." + "\r\n\r\n" + "The png snapshot will be named according to the current time.");
        private void ModeLabel_DoubleClick(object sender, EventArgs e)
            => ShowCustomMessageBox("[COLORING MODES]", "The spectrum of colors represents the argument of meromorphic functions, the value of two-variable functions, or the parameterization of planar curves." + "\r\n\r\n" + "The first three modes have swappable colorations, while the last two do not.");
        private void ContourLabel_DoubleClick(object sender, EventArgs e)
            => ShowCustomMessageBox("[CONTOUR MODES]", "Both options apply to the complex version only, for the contouring of meromorphic functions." + "\r\n\r\n" + "Only the Polar option admits translucent display, which represents the decay rate of modulus.");
        private void ThickLabel_DoubleClick(object sender, EventArgs e)
            => ShowCustomMessageBox("[MAGNITUDE]", "This represents the width of curves, the size of special points, or the decay rate of translucence." + "\r\n\r\n" + "It should be appropriate according to the scale. The examples have been tweaked with much effort.");
        private void InputLabel_DoubleClick(object sender, EventArgs e)
            => ShowCustomMessageBox("[FORMULA INPUT]", "Space and enter keys are both accepted. Unaccepted keys have been banned." + "\r\n\r\n" + "Try to use longer names for temporary parameters to avoid collapse of the interpreter.");
        private void DraftLabel_DoubleClick(object sender, EventArgs e)
            => ShowCustomMessageBox("[HISTORY LIST]", "The input will be saved both here and in the clipboard." + "\r\n\r\n" + "The clicked points will also be recorded with detailed information, along with the time of snapshots and history storage.");
        private void PreviewLabel_DoubleClick(object sender, EventArgs e)
            => ShowCustomMessageBox("[MICROCOSM]", "Since graphing cannot pause manually during the process, you may glimpse the result here." + "\r\n\r\n" + "It differs from the main graph only in size. It is estimated that graphing here is around 20 times faster.");
        private void TimeLabel_DoubleClick(object sender, EventArgs e)
            => ShowCustomMessageBox("[DURATION]", "The auto snapshot cannot capture updates here on time, but it will be saved in the history list along with the pixels." + "\r\n\r\n" + "This value is precious as an embodiment of the input structure for future reference.");
        private void ExampleLabel_DoubleClick(object sender, EventArgs e)
            => ShowCustomMessageBox("[EXAMPLES]", "These examples mainly inform you of the various types of legitimate grammar." + "\r\n\r\n" + "Some renderings are elegant while others are chaotic. Elegant graphs take time to explore: the essence of this app.");
        private void FunctionLabel_DoubleClick(object sender, EventArgs e)
            => ShowCustomMessageBox("[FUNCTIONS]", "The two combo boxes contain regular and special operations respectively. The latter tends to have complicated grammar." + "\r\n\r\n" + "Select the content in the input box and choose here for substitution.");
        private void PointNumLabel_DoubleClick(object sender, EventArgs e)
            => ShowCustomMessageBox("[PIXELS]", "This box logs the number of points or line segments throughout the previous loop, which is almost proportional to time and iteration." + "\r\n\r\n" + "Nullity often results from divergence or undefinedness.");
        private void DenseLabel_DoubleClick(object sender, EventArgs e)
            => ShowCustomMessageBox("[DENSITY]", "It refers to the density of contours or the relative speed of planar curves with respect to parameterizations." + "\r\n\r\n" + "It should be appropriate according to the scale. The examples have been tweaked with much effort.");
        private void DetailLabel_DoubleClick(object sender, EventArgs e)
            => ShowCustomMessageBox("[DETAILED SCOPE]", "You can even reverse the endpoints to create the mirror effect." + "\r\n\r\n" + "Any legitimate variable-free algebraic expressions are acceptable in this box, and will be checked as in the input box.");
        private void ClearButton_Click(object sender, EventArgs e)
        {
            foreach (TextBox control in new[] { DraftBox, PointNumDisplay, TimeDisplay, X_CoorDisplay, Y_CoorDisplay, ModulusDisplay, AngleDisplay, FunctionDisplay, CaptionBox })
                control.Text = String.Empty;
            plot_loop = points_chosen = 0;
            drafted = false;
        }
        private void Delete_Click(int xLeft, int yUp, int xRight, int yDown, bool isMain)
        {
            SetTDSB(); // this is necessary
            ClearBitmap(bitmap, new Rectangle(xLeft, yUp, xRight - xLeft, yDown - yUp));
            Graphics g = CreateGraphics();
            DrawBackdrop(g, new(Color.Gray, 1), xLeft, yUp, xRight, yDown, new SolidBrush(Color.Black));
            if (CheckCoor.Checked) DrawAxesGrid(g, scopes, new int[] { xLeft, xRight, yUp, yDown });
            axes_drawn = isMain ? axes_drawn : CheckCoor.Checked;
            Axes_drawn = isMain ? CheckCoor.Checked : Axes_drawn;
        }
        private void DeleteMain_Click(object sender, EventArgs e)
            => Delete_Click(X_LEFT_MAC, Y_UP_MAC, X_RIGHT_MAC, Y_DOWN_MAC, true);
        private void DeletePreview_Click(object sender, EventArgs e)
            => Delete_Click(X_LEFT_MIC, Y_UP_MIC, X_RIGHT_MIC, Y_DOWN_MIC, false);
        private static void SetFontStyle(Label ctrl)
            => ctrl.ForeColor = ctrl.ForeColor == Color.White ? UNCHECKED_YELLOW : ctrl.ForeColor;
        private static void RecoverFontStyle(Label ctrl)
            => ctrl.ForeColor = ctrl.ForeColor == UNCHECKED_YELLOW ? Color.White : ctrl.ForeColor;
        private static void Input_MouseHover(Control input, Label label)
        {
            input.BackColor = label.ForeColor == Color.White ? FOCUS_GRAY : label.ForeColor;
            input.ForeColor = Color.Black;
            SetFontStyle(label);
        }
        private static void Input_MouseLeave(Control input, Label label)
        {
            input.BackColor = CONTROL_GRAY;
            input.ForeColor = Color.White;
            RecoverFontStyle(label);
        }
        private void GeneralInput_MouseHover(object sender, EventArgs e) => Input_MouseHover(GeneralInput, GeneralLabel);
        private void GeneralInput_MouseLeave(object sender, EventArgs e) => Input_MouseLeave(GeneralInput, GeneralLabel);
        private void X_Left_MouseHover(object sender, EventArgs e) => Input_MouseHover(X_Left, DetailLabel);
        private void X_Left_MouseLeave(object sender, EventArgs e) => Input_MouseLeave(X_Left, DetailLabel);
        private void X_Right_MouseHover(object sender, EventArgs e) => Input_MouseHover(X_Right, DetailLabel);
        private void X_Right_MouseLeave(object sender, EventArgs e) => Input_MouseLeave(X_Right, DetailLabel);
        private void Y_Left_MouseHover(object sender, EventArgs e) => Input_MouseHover(Y_Left, DetailLabel);
        private void Y_Left_MouseLeave(object sender, EventArgs e) => Input_MouseLeave(Y_Left, DetailLabel);
        private void Y_Right_MouseHover(object sender, EventArgs e) => Input_MouseHover(Y_Right, DetailLabel);
        private void Y_Right_MouseLeave(object sender, EventArgs e) => Input_MouseLeave(Y_Right, DetailLabel);
        private void ThickInput_MouseHover(object sender, EventArgs e) => Input_MouseHover(ThickInput, ThickLabel);
        private void ThickInput_MouseLeave(object sender, EventArgs e) => Input_MouseLeave(ThickInput, ThickLabel);
        private void DenseInput_MouseHover(object sender, EventArgs e) => Input_MouseHover(DenseInput, DenseLabel);
        private void DenseInput_MouseLeave(object sender, EventArgs e) => Input_MouseLeave(DenseInput, DenseLabel);
        private void AddressInput_MouseHover(object sender, EventArgs e) => Input_MouseHover(AddressInput, AtLabel);
        private void AddressInput_MouseLeave(object sender, EventArgs e) => Input_MouseLeave(AddressInput, AtLabel);
        private void DraftBox_MouseHover(object sender, EventArgs e)
        {
            if (!DraftBox.ReadOnly)
            {
                DraftBox.BackColor = FOCUS_GRAY;
                DraftBox.ForeColor = Color.Black;
                toolTip_ReadOnly.SetToolTip(DraftBox, String.Empty);
                SetFontStyle(DraftLabel);
            }
            else
            {
                toolTip_ReadOnly.SetToolTip(DraftBox, "ReadOnly");
                DraftLabel.ForeColor = READONLY_PURPLE;
                DraftBox.ForeColor = Color.White;
            }
        }
        private void DraftBox_MouseLeave(object sender, EventArgs e)
        {
            if (!DraftBox.ReadOnly)
            {
                DraftBox.BackColor = CONTROL_GRAY;
                DraftBox.ForeColor = Color.White;
            }
            else DraftBox.ForeColor = READONLY_GRAY;
            DraftLabel.ForeColor = Color.White;
        }
        private void ComboExamples_MouseHover(object sender, EventArgs e) => ExampleLabel.ForeColor = COMBO_BLUE;
        private void ComboExamples_MouseLeave(object sender, EventArgs e) => ExampleLabel.ForeColor = Color.White;
        private void ComboFunctions_MouseHover(object sender, EventArgs e) => FunctionLabel.ForeColor = COMBO_BLUE;
        private void ComboFunctions_MouseLeave(object sender, EventArgs e) => FunctionLabel.ForeColor = Color.White;
        private void ComboSpecial_MouseHover(object sender, EventArgs e) => FunctionLabel.ForeColor = COMBO_BLUE;
        private void ComboSpecial_MouseLeave(object sender, EventArgs e) => FunctionLabel.ForeColor = Color.White;
        private void ComboColoring_MouseHover(object sender, EventArgs e) => ModeLabel.ForeColor = COMBO_BLUE;
        private void ComboColoring_MouseLeave(object sender, EventArgs e) => ModeLabel.ForeColor = Color.White;
        private void ComboContour_MouseHover(object sender, EventArgs e) => ContourLabel.ForeColor = COMBO_BLUE;
        private void ComboContour_MouseLeave(object sender, EventArgs e) => ContourLabel.ForeColor = Color.White;
        private void InputString_TextChanged(object sender, EventArgs e)
        {
            if (InputString.ReadOnly) return;
            is_checking = text_changed = true;
            AutoCheckComplex();
            CheckValidity();
        }
        private void InputString_DoubleClick(object sender, EventArgs e) => InputString_TextChanged(sender, e);
        private void AddressInput_TextChanged(object sender, EventArgs e)
        {
            if (InputString.ReadOnly) return;
            if (String.IsNullOrEmpty(AddressInput.Text)) AtLabel.ForeColor = Color.White;
            else AtLabel.ForeColor = Directory.Exists(AddressInput.Text) ? CORRECT_GREEN : ERROR_RED;
        }
        private void AddressInput_DoubleClick(object sender, EventArgs e) => AddressInput_TextChanged(sender, e);
        private void GeneralInput_TextChanged(object sender, EventArgs e) => MiniChecks(GeneralInput, GeneralLabel);
        private void GeneralInput_DoubleClick(object sender, EventArgs e) => GeneralInput_TextChanged(sender, e);
        private void ColorOfDetails()
        {
            if (X_Scope.ForeColor == CORRECT_GREEN && Y_Scope.ForeColor == CORRECT_GREEN)
                DetailLabel.ForeColor = CORRECT_GREEN;
            if (X_Scope.ForeColor == ERROR_RED || Y_Scope.ForeColor == ERROR_RED)
                DetailLabel.ForeColor = ERROR_RED;
        }
        private void X_Left_TextChanged(object sender, EventArgs e) { MiniChecks(X_Left, X_Scope); ColorOfDetails(); }
        private void X_Left_DoubleClick(object sender, EventArgs e) => X_Left_TextChanged(sender, e);
        private void X_Right_TextChanged(object sender, EventArgs e) { MiniChecks(X_Right, X_Scope); ColorOfDetails(); }
        private void X_Right_DoubleClick(object sender, EventArgs e) => X_Right_TextChanged(sender, e);
        private void Y_Left_TextChanged(object sender, EventArgs e) { MiniChecks(Y_Left, Y_Scope); ColorOfDetails(); }
        private void Y_Left_DoubleClick(object sender, EventArgs e) => Y_Left_TextChanged(sender, e);
        private void Y_Right_TextChanged(object sender, EventArgs e) { MiniChecks(Y_Right, Y_Scope); ColorOfDetails(); }
        private void Y_Right_DoubleClick(object sender, EventArgs e) => Y_Right_TextChanged(sender, e);
        private void ThickInput_TextChanged(object sender, EventArgs e) => MiniChecks(ThickInput, ThickLabel);
        private void ThickInput_DoubleClick(object sender, EventArgs e) => ThickInput_TextChanged(sender, e);
        private void DenseInput_TextChanged(object sender, EventArgs e) => MiniChecks(DenseInput, DenseLabel);
        private void DenseInput_DoubleClick(object sender, EventArgs e) => DenseInput_TextChanged(sender, e);
        private static void BanDoubleClick(TextBox ctrl, MouseEventArgs e)
        { ctrl.SelectionStart = ctrl.GetCharIndexFromPosition(e.Location); ctrl.SelectionLength = 0; }
        private void InputString_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(InputString, e);
        private void GeneralInput_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(GeneralInput, e);
        private void X_Left_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(X_Left, e);
        private void X_Right_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(X_Right, e);
        private void Y_Left_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(Y_Left, e);
        private void Y_Right_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(Y_Right, e);
        private void ThickInput_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(ThickInput, e);
        private void DenseInput_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(DenseInput, e);
        private void AddressInput_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(AddressInput, e);
        private void InputString_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(InputString, e);
        private void InputString_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void GeneralInput_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(GeneralInput, e);
        private void GeneralInput_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void X_Left_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(X_Left, e);
        private void X_Left_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void X_Right_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(X_Right, e);
        private void X_Right_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void Y_Left_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(Y_Left, e);
        private void Y_Left_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void Y_Right_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(Y_Right, e);
        private void Y_Right_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void ThickInput_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(ThickInput, e);
        private void ThickInput_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void DenseInput_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(DenseInput, e);
        private void DenseInput_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void CheckComplex_MouseHover(object sender, EventArgs e) => CheckComplex.ForeColor = COMBO_BLUE;
        private void CheckComplex_MouseLeave(object sender, EventArgs e) => CheckComplex.ForeColor = Color.White;
        private void CheckSwap_MouseHover(object sender, EventArgs e) => CheckSwap.ForeColor = COMBO_BLUE;
        private void CheckSwap_MouseLeave(object sender, EventArgs e) => CheckSwap.ForeColor = Color.White;
        private void CheckCoor_MouseHover(object sender, EventArgs e) => CheckCoor.ForeColor = COMBO_BLUE;
        private void CheckCoor_MouseLeave(object sender, EventArgs e) => CheckCoor.ForeColor = Color.White;
        private void CheckPoints_MouseHover(object sender, EventArgs e) => CheckPoints.ForeColor = COMBO_BLUE;
        private void CheckPoints_MouseLeave(object sender, EventArgs e) => CheckPoints.ForeColor = Color.White;
        private void CheckShade_MouseHover(object sender, EventArgs e) => CheckShade.ForeColor = COMBO_BLUE;
        private void CheckShade_MouseLeave(object sender, EventArgs e) => CheckShade.ForeColor = Color.White;
        private void CheckRetain_MouseHover(object sender, EventArgs e) => CheckRetain.ForeColor = COMBO_BLUE;
        private void CheckRetain_MouseLeave(object sender, EventArgs e) => CheckRetain.ForeColor = Color.White;
        private void CheckEdit_MouseHover(object sender, EventArgs e) => CheckEdit.ForeColor = COMBO_BLUE;
        private void CheckEdit_MouseLeave(object sender, EventArgs e) => CheckEdit.ForeColor = Color.White;
        private void CheckAuto_MouseHover(object sender, EventArgs e) => CheckAuto.ForeColor = COMBO_BLUE;
        private void CheckAuto_MouseLeave(object sender, EventArgs e) => CheckAuto.ForeColor = Color.White;
        private void SubtitleBox_DoubleClick(object sender, EventArgs e)
        { if (InputString.ReadOnly) return; Clipboard.SetText(InputString.Text); InputString.Text = String.Empty; }
        private void SubtitleBox_MouseHover(object sender, EventArgs e) => SubtitleBox.ForeColor = ERROR_RED;
        private void SubtitleBox_MouseLeave(object sender, EventArgs e) => SubtitleBox.ForeColor = Color.White;
        private void PointNumDisplay_MouseHover(object sender, EventArgs e)
        { PointNumLabel.ForeColor = READONLY_PURPLE; PointNumDisplay.ForeColor = Color.White; }
        private void PointNumDisplay_MouseLeave(object sender, EventArgs e)
        { PointNumLabel.ForeColor = Color.White; PointNumDisplay.ForeColor = READONLY_GRAY; }
        private void TimeDisplay_MouseHover(object sender, EventArgs e)
        { TimeLabel.ForeColor = READONLY_PURPLE; TimeDisplay.ForeColor = Color.White; }
        private void TimeDisplay_MouseLeave(object sender, EventArgs e)
        { TimeLabel.ForeColor = Color.White; TimeDisplay.ForeColor = READONLY_GRAY; }
        private void X_CoorDisplay_MouseHover(object sender, EventArgs e)
        { X_Coor.ForeColor = READONLY_PURPLE; X_CoorDisplay.ForeColor = Color.White; }
        private void X_CoorDisplay_MouseLeave(object sender, EventArgs e)
        { X_Coor.ForeColor = Color.White; X_CoorDisplay.ForeColor = READONLY_GRAY; }
        private void Y_CoorDisplay_MouseHover(object sender, EventArgs e)
        { Y_Coor.ForeColor = READONLY_PURPLE; Y_CoorDisplay.ForeColor = Color.White; }
        private void Y_CoorDisplay_MouseLeave(object sender, EventArgs e)
        { Y_Coor.ForeColor = Color.White; Y_CoorDisplay.ForeColor = READONLY_GRAY; }
        private void ModulusDisplay_MouseHover(object sender, EventArgs e)
        { Modulus.ForeColor = READONLY_PURPLE; ModulusDisplay.ForeColor = Color.White; }
        private void ModulusDisplay_MouseLeave(object sender, EventArgs e)
        { Modulus.ForeColor = Color.White; ModulusDisplay.ForeColor = READONLY_GRAY; }
        private void AngleDisplay_MouseHover(object sender, EventArgs e)
        { Angle.ForeColor = READONLY_PURPLE; AngleDisplay.ForeColor = Color.White; }
        private void AngleDisplay_MouseLeave(object sender, EventArgs e)
        { Angle.ForeColor = Color.White; AngleDisplay.ForeColor = READONLY_GRAY; }
        private void FunctionDisplay_MouseHover(object sender, EventArgs e)
        { ValueLabel.ForeColor = READONLY_PURPLE; FunctionDisplay.ForeColor = Color.White; }
        private void FunctionDisplay_MouseLeave(object sender, EventArgs e)
        { ValueLabel.ForeColor = Color.White; FunctionDisplay.ForeColor = READONLY_GRAY; }
        private void InputString_MouseHover(object sender, EventArgs e) => SetFontStyle(InputLabel);
        private void InputString_MouseLeave(object sender, EventArgs e) => RecoverFontStyle(InputLabel);
        private void VScrollBarX_MouseHover(object sender, EventArgs e) => X_Bar.ForeColor = READONLY_PURPLE;
        private void VScrollBarX_MouseLeave(object sender, EventArgs e) => X_Bar.ForeColor = Color.White;
        private void VScrollBarY_MouseHover(object sender, EventArgs e) => Y_Bar.ForeColor = READONLY_PURPLE;
        private void VScrollBarY_MouseLeave(object sender, EventArgs e) => Y_Bar.ForeColor = Color.White;
        private void CaptionBox_MouseHover(object sender, EventArgs e) => CaptionBox.ForeColor = Color.White;
        private void CaptionBox_MouseLeave(object sender, EventArgs e) => CaptionBox.ForeColor = READONLY_GRAY;
        private void PictureLogo_MouseHover(object sender, EventArgs e) => EnlargePicture(PictureLogo, 5);
        private void PictureLogo_MouseLeave(object sender, EventArgs e) => ShrinkPicture(PictureLogo, 5);
        private void PreviewLabel_MouseHover(object sender, EventArgs e) => PreviewLabel.ForeColor = READONLY_PURPLE;
        private void PreviewLabel_MouseLeave(object sender, EventArgs e) => PreviewLabel.ForeColor = Color.White;
        private void X_Bar_MouseHover(object sender, EventArgs e) => X_Bar.ForeColor = READONLY_PURPLE;
        private void X_Bar_MouseLeave(object sender, EventArgs e) => X_Bar.ForeColor = Color.White;
        private void Y_Bar_MouseHover(object sender, EventArgs e) => Y_Bar.ForeColor = READONLY_PURPLE;
        private void Y_Bar_MouseLeave(object sender, EventArgs e) => Y_Bar.ForeColor = Color.White;
        private void CaptionBox_MouseDown(object sender, MouseEventArgs e) => HideCaret(CaptionBox.Handle);
        private void PointNumDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(PointNumDisplay.Handle);
        private void TimeDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(TimeDisplay.Handle);
        private void X_CoorDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(X_CoorDisplay.Handle);
        private void Y_CoorDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(Y_CoorDisplay.Handle);
        private void ModulusDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(ModulusDisplay.Handle);
        private void AngleDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(AngleDisplay.Handle);
        private void FunctionDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(FunctionDisplay.Handle);
        private void DraftBox_MouseDown(object sender, MouseEventArgs e) { if (DraftBox.ReadOnly) HideCaret(DraftBox.Handle); }
        private void PicturePlay_Click(object sender, EventArgs e) => PlayOrPause();
        private void PicturePlay_MouseHover(object sender, EventArgs e) => EnlargePicture(PicturePlay, 2);
        private void PicturePlay_MouseLeave(object sender, EventArgs e) => ShrinkPicture(PicturePlay, 2);
        private void PictureIncorrect_Click(object sender, EventArgs e) { if (!InputString.ReadOnly) CheckValidityDetailed(); }
        private void PictureIncorrect_MouseHover(object sender, EventArgs e) => EnlargePicture(PictureIncorrect, 2);
        private void PictureIncorrect_MouseLeave(object sender, EventArgs e) => ShrinkPicture(PictureIncorrect, 2);
        private void ExportButton_MouseHover(object sender, EventArgs e) => AddressInput_DoubleClick(sender, e);
        private void StoreButton_MouseHover(object sender, EventArgs e) => AddressInput_DoubleClick(sender, e);
        private static void EnlargePicture(Control ctrl, int increment)
        {
            if (is_resized) return;
            ctrl.Location = new Point(ctrl.Location.X - increment, ctrl.Location.Y - increment);
            ctrl.Size = new Size(ctrl.Width + 2 * increment, ctrl.Height + 2 * increment);
            is_resized = true;
        }
        private static void ShrinkPicture(Control ctrl, int decrement)
        {
            if (!is_resized) return;
            ctrl.Location = new Point(ctrl.Location.X + decrement, ctrl.Location.Y + decrement);
            ctrl.Size = new Size(ctrl.Width - 2 * decrement, ctrl.Height - 2 * decrement);
            is_resized = false;
        }
        private void Combo_KeyDown(object sender, KeyEventArgs e)
            => e.SuppressKeyPress = e.Control && e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z;
        private void ComboColoring_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(sender, e);
        private void ComboContour_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(sender, e);
        private void ComboExamples_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(sender, e);
        private void ComboFunctions_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(sender, e);
        private void ComboSpecial_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(sender, e);
        #endregion
    }
    public class BaseMessageBox : Form
    {
        protected static readonly Color BACKDROP_GRAY = Color.FromArgb(64, 64, 64);
        public static TextBox txtMessage;
        public static Button btnOk;
        [DllImport("user32.dll")]
        static extern bool HideCaret(IntPtr hWnd);
        private static bool isBtnOkResized = false;
        protected float originalFontSize, ScalingFactor;
        protected void ReduceFontSizeByScale(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                Font currentFont = ctrl.Font;
                float newFontSize = currentFont.Size / ScalingFactor;
                ctrl.Font = new Font(currentFont.FontFamily, newFontSize, currentFont.Style);
                if (ctrl.Controls.Count > 0) ReduceFontSizeByScale(ctrl);
            }
        }
        protected void BtnOk_MouseEnter(object sender, EventArgs e)
        {
            if (sender is Button btn && !isBtnOkResized)
            {
                btn.Size = new Size(btn.Width + 2, btn.Height + 2);
                btn.Location = new Point(btn.Location.X - 1, btn.Location.Y - 1);
                originalFontSize = btn.Font.Size;
                btn.Font = new Font(btn.Font.FontFamily, originalFontSize + 1f, btn.Font.Style);
                isBtnOkResized = true;
            }
        }
        protected void BtnOk_MouseLeave(object sender, EventArgs e)
        {
            if (sender is Button btn && isBtnOkResized)
            {
                btn.Size = new Size(btn.Width - 2, btn.Height - 2);
                btn.Location = new Point(btn.Location.X + 1, btn.Location.Y + 1);
                btn.Font = new Font(btn.Font.FontFamily, originalFontSize, btn.Font.Style);
                isBtnOkResized = false;
            }
        }
        protected static void TxtMessage_MouseDown(object sender, MouseEventArgs e)
        { if (txtMessage != null && txtMessage.Handle != IntPtr.Zero) HideCaret(txtMessage.Handle); }
        protected void Form_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) Close(); }
        protected void SetUpForm(int width, int height)
        {
            FormBorderStyle = FormBorderStyle.None; TopMost = true; Size = new Size(width, height);
            StartPosition = FormStartPosition.CenterScreen; BackColor = SystemColors.ControlDark;
        }
        protected static void SetUpTextBox(int border, string message, int width, int height, Color textColor)
        {
            txtMessage = new()
            {
                Text = message,
                Font = new Font("Microsoft YaHei UI", 10, FontStyle.Regular),
                ForeColor = textColor,
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = BACKDROP_GRAY,
                ScrollBars = ScrollBars.Vertical
            };
            txtMessage.SetBounds(10, 10, width - 20, height - border * 2 - 10);
            txtMessage.SelectionStart = message.Length; txtMessage.SelectionLength = 0;
            txtMessage.MouseDown += TxtMessage_MouseDown;
        }
        protected void SetUpButton(int border, int width, int height, Color buttonColor, Color buttonTextColor)
        {
            btnOk = new()
            {
                Size = new Size(50, 25),
                Location = new Point(width / 2 - 25, height - border / 2 - 25),
                BackColor = buttonColor,
                ForeColor = buttonTextColor,
                Font = new Font("Microsoft YaHei UI", 7, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Text = "OK",
            };
            btnOk.FlatAppearance.BorderSize = 0; btnOk.Click += (sender, e) => { Close(); };
            btnOk.MouseEnter += BtnOk_MouseEnter; btnOk.MouseLeave += BtnOk_MouseLeave;
        }
        protected void Setup(string message, int width, int height, Color textColor, Color buttonColor, Color buttonTextColor)
        {
            int border = 23;
            SetUpForm(width, height);
            SetUpTextBox(border, message, width, height, textColor);
            SetUpButton(border, width, height, buttonColor, buttonTextColor);
            Controls.Add(txtMessage); Controls.Add(btnOk);
            ScalingFactor = Graphics.FromHwnd(IntPtr.Zero).DpiX / 96f / 1.5f;
            ReduceFontSizeByScale(this);
            KeyPreview = true; KeyDown += new KeyEventHandler(Form_KeyDown);
            Load += (sender, e) => { HideCaret(txtMessage.Handle); };
        }
        public static void Display(string message, int width, int height, Color textColor, Color buttonColor, Color buttonTextColor)
        {
            BaseMessageBox box = new();
            box.Setup(message, width, height, textColor, buttonColor, buttonTextColor);
            box.ShowDialog();
        }
    }
    public class FormalMessageBox : BaseMessageBox
    { public static void Show(string message, int width, int height)
            => Display(message, width, height, Color.FromArgb(224, 224, 224), Color.Black, Color.White); } // Title & Profile
    public class CustomMessageBox : BaseMessageBox
    { public static void Show(string message, int width, int height)
            => Display(message, width, height, Color.Turquoise, Color.DarkBlue, Color.White); } // Instructions
    public class ExceptionMessageBox : BaseMessageBox
    { public static void Show(string message, int width, int height)
            => Display(message, width, height, Color.LightPink, Color.DarkRed, Color.White); } // Exceptions
    public class MyString
    {
        #region Reckoning
        public static int CountChar(ReadOnlySpan<char> input, char c)
        {
            int count = 0;
            foreach (char ch in input) if (ch == c) count++;
            return count;
        }
        public static bool ContainsAny(string input, char[] charsToCheck)
        {
            HashSet<char> charSet = new(charsToCheck);
            foreach (char c in input) if (charSet.Contains(c)) return true;
            return false;
        }
        public static bool ContainsAny(string input, string[] stringsToCheck)
        {
            foreach (string str in stringsToCheck) if (input.Contains(str)) return true;
            return false;
        }
        public static bool ContainFunctionName(string input)
            => ContainsAny(input, new string[] { "Func", "func", "Polar", "polar", "Param", "param" });
        #endregion

        #region Parenthesis
        public static int PairedParenthesis(ReadOnlySpan<char> input, int n)
        {
            for (int i = n + 1, countBracket = 1; ; i++)
            {
                if (input[i] == '(') countBracket++; else if (input[i] == ')') countBracket--;
                if (countBracket == 0) return i;
            }
        }
        public static int PairedInnerParenthesis(ReadOnlySpan<char> input, int n)
        { for (int i = n + 1; ; i++) if (input[i] == ')') return i; }
        public static bool CheckParenthesis(ReadOnlySpan<char> input)
        {
            int sum = 0;
            foreach (char c in input)
            {
                if (c == '(') sum++; else if (c == ')') sum--;
                if (sum < 0) return false;
            }
            return sum == 0;
        }
        public static (int, int) InnerParenthesis(ReadOnlySpan<char> input, int start)
        {
            for (int i = start, j = -1; ; i--)
            { if (input[i] == ')') j = i; else if (input[i] == '(') return (i, j); }
        }
        public static string BracketSub(int n) => String.Concat("[", n.ToString(), "]");
        public static string IndexSub(int n) => String.Concat("(", n.ToString(), ")");
        #endregion

        #region Manipulations
        public static string Extract(string input, int begin, int end) => input.AsSpan(begin, end - begin + 1).ToString();
        public static string Replace(string original, string replacement, int begin, int end)
        {
            int resultLength = begin + replacement.Length + (original.Length - end - 1);
            return String.Create(resultLength, (original, replacement, begin, end), (span, state) => {
                (string orig, string repl, int b, int e) = state;
                orig.AsSpan(0, b).CopyTo(span); // Copy the beginning
                repl.AsSpan().CopyTo(span[b..]); // Copy the replacement
                orig.AsSpan(e + 1).CopyTo(span[(b + repl.Length)..]); // Copy the remaining
            });
        }
        private static string ReplaceInterior(string input, char c, char replacement)
        {
            if (!input.Contains('_')) return input;
            StringBuilder result = new(input);
            for (int i = 0, length = result.Length; i < length; i++)
            {
                if (result[i] != '_') continue;
                int endIndex = PairedParenthesis(input, i + 1);
                for (int j = i + 1; j < endIndex; j++)
                {
                    if (result[j] != c) continue;
                    result.Remove(j, 1).Insert(j, replacement);
                }
                i = endIndex;
            }
            return result.ToString();
        }
        public static string[] ReplaceRecover(string input)
            => SplitByChars(ReplaceInterior(input, ',', ';'), new char[] { ',' }).Select(part => part.Replace(';', ',')).ToArray();
        public static string[] SplitString(string input)
            => ReplaceRecover(Extract(input, input.IndexOf('(') + 1, PairedParenthesis(input, input.IndexOf('(')) - 1));
        public static string[] SplitByChars(string input, char[] delimiters)
        {
            List<string> segments = new();
            StringBuilder currentSegment = new();
            HashSet<char> delimiterSet = new(delimiters);
            for (int i = 0, length = input.Length; i < length; i++)
            {
                if (delimiterSet.Contains(input[i]))
                {
                    segments.Add(currentSegment.ToString());
                    currentSegment.Clear();
                }
                else currentSegment.Append(input[i]);
            }
            segments.Add(currentSegment.ToString());
            return segments.ToArray();
        }
        public static string TrimStartChars(string input, char[] charsToTrim)
        {
            int startIndex = 0, length = input.Length;
            HashSet<char> trimSet = new(charsToTrim);
            while (startIndex < length && trimSet.Contains(input[startIndex])) startIndex++;
            if (startIndex == length) return String.Empty;
            StringBuilder result = new(length - startIndex);
            return result.Append(input, startIndex, length - startIndex).ToString();
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static (StringBuilder, string[]) PlusMultiplyBreaker(string input, string signs, char sign, int THRESHOLD)
        {
            StringBuilder signsBuilder = new(), result = new(input);
            for (int i = 0, flag = 0, length = result.Length; i < length; i++)
            {
                if (!signs.Contains(result[i])) continue;
                if (++flag % THRESHOLD == 0)
                {
                    string replacement = result[i] == sign ? ":" : ";"; // necessary
                    result.Remove(i, 1).Insert(i, replacement);
                    signsBuilder.Append(result[i]);
                }
            }
            return (signsBuilder, SplitByChars(result.ToString(), new char[] { ':', ';' }));
        }
        #endregion

        #region Substitutions
        public static string ReplaceSubstrings(string input, List<string> substrings, string replacement)
            => System.Text.RegularExpressions.Regex.Replace(input, String.Join("|", substrings), replacement);
        public static string ReplaceTagReal(string input)
        {
            Dictionary<string, string> replacements = new()
            {
                { "Floor", "~f$" }, { "floor", "~f$" },
                { "Ceil", "~c$" }, { "ceil", "~c$" },
                { "Round", "~r$" }, { "round", "~r$" },
                { "Sign", "~s$" }, { "sign", "~s$" }, { "Sgn", "~s$" }, { "sgn", "~s$" },
                { "Mod", "~M_" }, { "mod", "~M_" },
                { "nCr", "~C_" }, { "nPr", "~A_" },
                { "Max", "~>_" }, { "max", "~>_" }, { "Min", "~<_" }, { "min", "~<_" },
                { "Iterate1", "~1I_" }, { "iterate1", "~1I_" }, { "Iterate2", "~2I_" }, { "iterate2", "~2I_" },
                { "Composite1", "~1J_" }, { "composite1", "~1J_" }, { "Composite2", "~2J_" }, { "composite2", "~2J_" },
                { "Comp1", "~1J_" }, { "comp1", "~1J_" }, { "Comp2", "~2J_" }, { "comp2", "~2J_" }
            };
            foreach (var pair in replacements) input = input.Replace(pair.Key, pair.Value);
            return ReplaceTagCommon(input);
        }
        public static string ReplaceTagComplex(string input)
        {
            Dictionary<string, string> replacements = new()
            {
                { "Iterate(", "~I_(" }, { "iterate(", "~I_(" },
                { "Composite", "~J_" }, { "composite", "~J_" }, { "Comp", "~J_" }, { "comp", "~J_" },
                { "conjugate", "~J" }, { "Conjugate", "~J" }, { "conj", "~J" }, { "Conj", "~J" },
                { "e(", "~E#(" }
            };
            foreach (var replacement in replacements) input = input.Replace(replacement.Key, replacement.Value);
            return ReplaceTagCommon(input);
        }
        public static string ReplaceTagCommon(string input)
        {
            Dictionary<string, string> replacements = new()
            {
                { "Product", "~P_" }, { "product", "~P_" }, { "Prod", "~P_" }, { "prod", "~P_" },
                { "Sum", "~S_" }, { "sum", "~S_" },
                { "F(", "~F_(" },
                { "Gamma(", "~G_(" }, { "gamma(", "~G_(" }, { "Ga(", "~G_(" }, { "ga(", "~G_(" },
                { "Beta", "~B_" }, { "beta", "~B_" },
                { "Zeta", "~Z_" }, { "zeta", "~Z_" },
                { "log", "~l" }, { "Log", "~l" }, { "ln", "~l" }, { "Ln", "~l" },
                { "exp", "~E" }, { "Exp", "~E" },
                { "sqrt", "~q" }, { "Sqrt", "~q" },
                { "abs", "~a" }, { "Abs", "~a" },
                { "factorial", "~!" }, { "Factorial", "~!" }, { "fact", "~!" }, { "Fact", "~!" },
                { "pi", "p" }, { "Pi", "p" },
                { "gamma", "g" }, { "Gamma", "g" }, { "ga", "g" }, { "Ga", "g" },
                { "arcsinh", "~ash" }, { "Arcsinh", "~ash" }, { "arcsh", "~ash" }, { "Arcsh", "~ash" }, { "arsinh", "~ash" }, { "Arsinh", "~ash" }, { "arsh", "~ash" }, { "Arsh", "~ash" },
                { "arccosh", "~ach" }, { "Arccosh", "~ach" }, { "arcch", "~ach" }, { "Arcch", "~ach" }, { "arcosh", "~ach" }, { "Arcosh", "~ach" }, { "arch", "~ach" }, { "Arch", "~ach" },
                { "arctanh", "~ath" }, { "Arctanh", "~ath" }, { "arcth", "~ath" }, { "Arcth", "~ath" }, { "artanh", "~ath" }, { "Artanh", "~ath" }, { "arth", "~ath" }, { "Arth", "~ath" },
                { "arcsin", "~as" }, { "Arcsin", "~as" }, { "asin", "~as" }, { "Asin", "~as" },
                { "arccos", "~ac" }, { "Arccos", "~ac" }, { "acos", "~ac" }, { "Acos", "~ac" },
                { "arctan", "~at" }, { "Arctan", "~at" }, { "atan", "~at" }, { "Atan", "~at" },
                { "sinh", "~sh" }, { "Sinh", "~sh" },
                { "cosh", "~ch" }, { "Cosh", "~ch" },
                { "tanh", "~th" }, { "Tanh", "~th" },
                { "sin", "~s" }, { "Sin", "~s" },
                { "cos", "~c" }, { "Cos", "~c" },
                { "tan", "~t" }, { "Tan", "~t" }
            };
            foreach (var replacement in replacements) input = input.Replace(replacement.Key, replacement.Value);
            return input;
        }
        public static string ReplaceTagCurves(string input)
        {
            Dictionary<string, string> replacements = new()
            {
                { "func", "α_" }, { "Func", "α_" },
                { "polar", "β_" }, { "Polar", "β_" },
                { "param", "γ_" }, { "Param", "γ_" },
                { "iterateLoop", "δ_" }, { "IterateLoop", "δ_" }
            };
            foreach (var replacement in replacements) input = input.Replace(replacement.Key, replacement.Value);
            return input;
        }
        public static string ReplaceComplexConfusion(string input)
            => ReplaceSubstrings(input, new List<string> { "Zeta", "zeta" }, String.Empty);
        public static string TrimLargeDouble(double input, double threshold)
            => Math.Abs(input) < threshold ? input.ToString("#0.000000") : input.ToString("E3");
        public static int ToInt(string input) => (int)RealSub.Obtain(input);
        #endregion
    }
    public class RecoverMultiply
    {
        public static string BeautifyInput(string input, bool isComplex)
        {
            if (!MyString.CheckParenthesis(input) || input.Contains("()")
                || MyString.ContainsAny(input, "_#!<>$%&@~:\'\"\\?=`[]{}\t".ToCharArray())) throw new FormatException();
            input = MyString.ReplaceSubstrings(input, new List<string> { "\n", "\r", " " }, String.Empty);
            return isComplex ? MyString.ReplaceTagComplex(input) : MyString.ReplaceTagReal(input);
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static string Recover(string input, bool isComplex)
        {
            int length = input.Length; if (length == 1) return input;
            StringBuilder sb = new(length * 2);
            sb.Append(input[0]); length--;
            for (int i = 0; i < length; i++)
            {
                if (AddOrNot(input[i], input[i + 1], isComplex)) sb.Append('*');
                sb.Append(input[i + 1]);
            }
            return sb.ToString();
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static bool AddOrNot(char c1, char c2, bool isComplex)
        {
            bool b1 = (IsConst(c1) || Char.IsNumber(c1)) && (IsConst(c2) || IsVar(c2, isComplex));
            bool b2 = (IsConst(c1) || IsVar(c1, isComplex)) && (IsConst(c2) || Char.IsNumber(c2));
            bool b3 = IsVar(c1, isComplex) && IsVar(c2, isComplex);
            bool b4 = (Char.IsNumber(c1) || IsConst(c1) || IsVar(c1, isComplex)) && IsOpen(c2);
            bool b5 = IsClose(c1) && (Char.IsNumber(c2) || IsConst(c2) || IsVar(c2, isComplex));
            bool b6 = IsClose(c1) && IsOpen(c2);
            bool b7 = !IsArithmetic(c1) && IsFunctionHead(c2);
            return b1 || b2 || b3 || b4 || b5 || b6 || b7;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsVar(char c, bool isComplex) => isComplex ? IsVarComplex(c) : IsVarReal(c);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsVarReal(char c) => "xyXY".Contains(c);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsVarComplex(char c) => "zZi".Contains(c);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsConst(char c) => "epg".Contains(c);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsArithmetic(char c) => "+-*/^(,|".Contains(c);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOpen(char c) => c == '(';
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsClose(char c) => c == ')';
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFunctionHead(char c) => c == '~';
    }
    public readonly struct DoubleMatrix
    {
        private readonly double[] data; private readonly int Rows, Columns;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DoubleMatrix(int rows, int columns) { data = new double[rows * columns]; Rows = rows; Columns = columns; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DoubleMatrix(double x) { data = new double[] { x }; Rows = Columns = 1; }
        public double this[int row, int column]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => data[row * Columns + column];
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => data[row * Columns + column] = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe double* Ptr() { fixed (double* ptr = &data[0]) { return ptr; } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe double* RowPtr(int row) { fixed (double* ptr = &data[row * Columns]) { return ptr; } }
    }
    public class RealSub
    {
        private const double GAMMA = 0.57721566490153286060651209008240243;
        private const int THRESHOLD = 10, STRUCTSIZE = 8; // Marshal.SizeOf<Double>()
        private string input;
        private int row, column, count;
        private uint columnSIZE;
        private DoubleMatrix x, y, X, Y;
        private DoubleMatrix[] braValues;

        #region Constructors
        private void Initialize(string input, int row, int column)
        {
            if (String.IsNullOrEmpty(input)) throw new FormatException();
            this.input = RecoverMultiply.Recover(input, false);
            braValues = new DoubleMatrix[MyString.CountChar(input, '(')];
            x = y = X = Y = new(row, column);
            this.row = row; this.column = column; columnSIZE = (uint)(column * STRUCTSIZE);
        }
        private void PopulateX(DoubleMatrix x, DoubleMatrix? y) { this.x = x; if (y.HasValue) { this.y = y.Value; } }
        private void PopulateXNew(DoubleMatrix X, DoubleMatrix Y) { this.X = X; this.Y = Y; }
        public RealSub(string input, double x = 0, double y = 0, double X = 0, double Y = 0)
        { Initialize(input, 1, 1); PopulateX(new(x), new(y)); PopulateXNew(new(X), new(Y)); }
        public RealSub(string input, int row, int column) => Initialize(input, row, column);
        public RealSub(string input, DoubleMatrix x, int row, int column) : this(input, row, column) => PopulateX(x, null);
        public RealSub(string input, DoubleMatrix x, DoubleMatrix y, int row, int column) : this(input, row, column)
            => PopulateX(x, y);
        public RealSub(string input, DoubleMatrix x, DoubleMatrix y, DoubleMatrix X, DoubleMatrix Y, int row, int column)
            : this(input, row, column) { PopulateX(x, y); PopulateXNew(X, Y); }
        #endregion

        #region Basic Calculations
        private static double MySign(double value) => Math.Sign(value);
        public static double Factorial(double n) => n < 0 ? Double.NaN : Math.Floor(n) == 0 ? 1 : Math.Floor(n) * Factorial(n - 1);
        private static double Mod(double a, double n) => n != 0 ? a % Math.Abs(n) : Double.NaN;
        private static double Combination(double n, double r)
        {
            if (n == r || r == 0) return 1;
            else if (r > n && n >= 0 || 0 > r && r > n || n >= 0 && 0 > r) return 0;
            else if (n > 0) return Combination(n - 1, r - 1) + Combination(n - 1, r);
            else if (r > 0) return Combination(n + 1, r) - Combination(n, r - 1);
            else return Combination(n + 1, r + 1) - Combination(n, r + 1);
        }
        private static double Permutation(double n, double r)
        {
            if (r < 0) return 0;
            else if (r == 0) return 1;
            else return (n - r + 1) * Permutation(n, r - 1);
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe DoubleMatrix ProcessMCP(string input, Func<double, double, double> operation)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length != 2) throw new FormatException();
            DoubleMatrix temp_1 = new RealSub(split[0], x, y, X, Y, row, column).Obtain();
            DoubleMatrix temp_2 = new RealSub(split[1], x, y, X, Y, row, column).Obtain();
            DoubleMatrix output = new(row, column);
            Parallel.For(0, row, r => {
                double* temp1Ptr = temp_1.RowPtr(r), temp2Ptr = temp_2.RowPtr(r), outPtr = output.RowPtr(r);
                for (int c = 0; c < column; c++, outPtr++, temp1Ptr++, temp2Ptr++) *outPtr = operation(*temp1Ptr, *temp2Ptr);
            });
            return output;
        }
        private DoubleMatrix Mod(string input) => ProcessMCP(input, (a, b) => Mod(a, b));
        private DoubleMatrix Combination(string input) => ProcessMCP(input, (a, b) => Combination(Math.Floor(a), Math.Floor(b)));
        private DoubleMatrix Permutation(string input) => ProcessMCP(input, (a, b) => Permutation(Math.Floor(a), Math.Floor(b)));
        private static double Max(double[] input)
        {
            if (input.Length == 1) return input[0];
            return Math.Max(input[0], Max(input.Skip(1).ToArray()));
        }
        private static double Min(double[] input)
        {
            if (input.Length == 1) return input[0];
            return Math.Min(input[0], Min(input.Skip(1).ToArray()));
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe DoubleMatrix ProcessMinMax(string input, Func<double[], double> operation)
        {
            string[] split = MyString.ReplaceRecover(input); int length = split.Length;
            DoubleMatrix[] output = new DoubleMatrix[length];
            for (int i = 0, _length = output.Length; i < _length; i++) output[i] = new RealSub(split[i], x, y, X, Y, row, column).Obtain();
            DoubleMatrix Output = new(row, column);
            Parallel.For(0, row, () => new double[length], (r, state, values) =>
            {
                double* OutPtr = Output.RowPtr(r);
                for (int c = 0; c < column; c++, OutPtr++)
                {
                    for (int i = 0; i < length; i++) values[i] = output[i][r, c];
                    *OutPtr = operation(values);
                }
                return values;
            }, _ => { });
            return Output;
        }
        private DoubleMatrix Max(string input) => ProcessMinMax(input, values => values.Max());
        private DoubleMatrix Min(string input) => ProcessMinMax(input, values => values.Min());
        #endregion

        #region Additional Calculations
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe DoubleMatrix Hypergeometric(string input)
        {
            string[] split = MyString.ReplaceRecover(input); int length = split.Length;
            if (length > 5 || length < 4) throw new FormatException();
            int n = length == 5 ? MyString.ToInt(split[4]) : 100;
            DoubleMatrix sum = new(row, column), product = Const(1);
            DoubleMatrix input_new = new RealSub(split[3], x, y, X, Y, row, column).Obtain();
            DoubleMatrix _a = new RealSub(split[0], row, column).Obtain();
            DoubleMatrix _b = new RealSub(split[1], row, column).Obtain();
            DoubleMatrix _c = new RealSub(split[2], row, column).Obtain();
            Parallel.For(0, row, r => {
                double* prodPtr = product.RowPtr(r), sumPtr = sum.RowPtr(r), inputPtr = input_new.RowPtr(r);
                double* aPtr = _a.RowPtr(r), bPtr = _b.RowPtr(r), cPtr = _c.RowPtr(r);
                for (int c = 0; c < column; c++, prodPtr++, sumPtr++, inputPtr++, aPtr++, bPtr++, cPtr++)
                    for (int i = 1; i <= n; i++)
                    {
                        if(i != 0) *prodPtr *= *inputPtr * (*aPtr + i - 1) * (*bPtr + i - 1) / (*cPtr + i - 1) / i;
                        *sumPtr += *prodPtr;
                    }
            });
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe DoubleMatrix Gamma(string input)
        {
            string[] split = MyString.ReplaceRecover(input); int length = split.Length;
            if (length > 2) throw new FormatException();
            int n = length == 2 ? MyString.ToInt(split[1]) : 100;
            DoubleMatrix product = Const(1), temp_value = new RealSub(split[0], x, y, X, Y, row, column).Obtain();
            Parallel.For(0, row, r => {
                double* tempPtr = temp_value.RowPtr(r), prodPtr = product.RowPtr(r);
                for (int c = 0; c < column; c++, prodPtr++, tempPtr++) for (int i = 1; i <= n; i++)
                        *prodPtr *= Math.Exp(*tempPtr / i) / (1 + *tempPtr / i);
            });
            DoubleMatrix output = new(row, column);
            Parallel.For(0, row, r => {
                double* outPtr = output.RowPtr(r), prodPtr = product.RowPtr(r), tempPtr = temp_value.RowPtr(r);
                for (int c = 0; c < column; c++, outPtr++, prodPtr++, tempPtr++)
                    *outPtr = *prodPtr * Math.Exp(-GAMMA * *tempPtr) / *tempPtr;
            });
            return output;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe DoubleMatrix Beta(string input)
        {
            string[] split = MyString.ReplaceRecover(input); int length = split.Length;
            if (length > 3 || length < 2) throw new FormatException();
            int n = length == 3 ? MyString.ToInt(split[2]) : 100;
            DoubleMatrix product = Const(1);
            DoubleMatrix temp_1 = new RealSub(split[0], x, y, X, Y, row, column).Obtain();
            DoubleMatrix temp_2 = new RealSub(split[1], x, y, X, Y, row, column).Obtain();
            Parallel.For(0, row, r => {
                double* temp1Ptr = temp_1.RowPtr(r), temp2Ptr = temp_2.RowPtr(r), prodPtr = product.RowPtr(r);
                for (int c = 0; c < column; c++, prodPtr++, temp1Ptr++, temp2Ptr++) for (int i = 1; i <= n; i++)
                        *prodPtr *= 1 + *temp1Ptr * *temp2Ptr / (i * (i + *temp1Ptr + *temp2Ptr));
            });
            DoubleMatrix output = new(row, column);
            Parallel.For(0, row, r => {
                double* outPtr = output.RowPtr(r), prodPtr = product.RowPtr(r), 
                temp1Ptr = temp_1.RowPtr(r), temp2Ptr = temp_2.RowPtr(r);
                for (int c = 0; c < column; c++, outPtr++, temp1Ptr++, temp2Ptr++, prodPtr++)
                    *outPtr = (*temp1Ptr + *temp2Ptr) / (*temp1Ptr * *temp2Ptr) / *prodPtr;
            });
            return output;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe DoubleMatrix Zeta(string input)
        {
            string[] split = MyString.ReplaceRecover(input); int length = split.Length;
            if (length > 2) throw new FormatException();
            int n = length == 2 ? MyString.ToInt(split[1]) : 50;
            DoubleMatrix sum = new(row, column), Sum = new(row, column), Coefficient = Const(1), coefficient = Const(1);
            DoubleMatrix temp_value = new RealSub(split[0], x, y, X, Y, row, column).Obtain();
            Parallel.For(0, row, r =>
            {
                double* coeffPtr = coefficient.RowPtr(r), CoeffPtr = Coefficient.RowPtr(r);
                double* SumPtr = Sum.RowPtr(r), sumPtr = sum.RowPtr(r);
                double* tempPtr = temp_value.RowPtr(r);
                for (int c = 0; c < column; c++, CoeffPtr++, coeffPtr++, SumPtr++, sumPtr++, tempPtr++)
                {
                    for (int i = 0; i <= n; i++)
                    {
                        *CoeffPtr /= 2; *coeffPtr = 1; *SumPtr = 0;
                        for (int j = 0; j <= i; j++)
                        {
                            *SumPtr += *coeffPtr / Math.Pow(j + 1, *tempPtr);
                            *coeffPtr *= (double)(j - i) / (double)(j + 1); // Double is not redundant here
                        }
                        *SumPtr *= *CoeffPtr; *sumPtr += *SumPtr;
                    }
                    *sumPtr /= 1 - Math.Pow(2, 1 - *tempPtr);
                }
            });
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private DoubleMatrix Sum(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length != 4) throw new FormatException();
            DoubleMatrix sum = new(row, column);
            for (int i = MyString.ToInt(split[2]), int3 = MyString.ToInt(split[3]); i <= int3; i++)
                Plus(new RealSub(split[0].Replace(split[1], MyString.IndexSub(i)), x, y, X, Y, row, column).Obtain(), sum);
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private DoubleMatrix Product(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length != 4) throw new FormatException();
            DoubleMatrix product = Const(1);
            for (int i = MyString.ToInt(split[2]), int3 = MyString.ToInt(split[3]); i <= int3; i++)
                Multiply(new RealSub(split[0].Replace(split[1], MyString.IndexSub(i)), x, y, X, Y, row, column).Obtain(), product);
            return product;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private DoubleMatrix Iterate1(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length != 5) throw new FormatException();
            DoubleMatrix value = new RealSub(split[1], x, y, row, column).Obtain();
            DoubleMatrix temp = new(row, column);
            for (int i = MyString.ToInt(split[3]), int4 = MyString.ToInt(split[4]); i <= int4; i++)
                value = new RealSub(split[0].Replace(split[2], MyString.IndexSub(i)), x, y, value, temp, row, column).Obtain();
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private DoubleMatrix Iterate2(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length != 8) throw new FormatException();
            DoubleMatrix value1 = new RealSub(split[2], x, y, row, column).Obtain();
            DoubleMatrix value2 = new RealSub(split[3], x, y, row, column).Obtain();
            for (int i = MyString.ToInt(split[5]), int6 = MyString.ToInt(split[6]); i <= int6; i++)
            {
                DoubleMatrix temp1 = value1, temp2 = value2;
                value1 = new RealSub(split[0].Replace(split[4], MyString.IndexSub(i)), x, y, temp1, temp2, row, column).Obtain();
                value2 = new RealSub(split[1].Replace(split[4], MyString.IndexSub(i)), x, y, temp1, temp2, row, column).Obtain();
            }
            return split[7] == "1" ? value1 : value2;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private DoubleMatrix Composite1(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            DoubleMatrix value = new RealSub(split[0], x, y, row, column).Obtain();
            DoubleMatrix temp = new(row, column);
            for (int i = 0, length = split.Length - 1; i < length; i++) value = new RealSub(split[i + 1], x, y, value, temp, row, column).Obtain();
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private DoubleMatrix Composite2(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            int length = split.Length, _length = (length / 2) - 1;
            if (length % 2 == 0) throw new FormatException();
            string[] comp_1 = new string[_length], comp_2 = new string[_length];
            for (int i = 0; i < _length; i++) { comp_1[i] = split[2 * i + 2]; comp_2[i] = split[2 * i + 3]; }
            DoubleMatrix[] value = new DoubleMatrix[2];
            value[0] = new RealSub(split[0], x, y, row, column).Obtain();
            value[1] = new RealSub(split[1], x, y, row, column).Obtain();
            for (int i = 0; i < _length; i++)
            {
                DoubleMatrix temp_1 = value[0], temp_2 = value[1];
                value[0] = new RealSub(comp_1[i], x, y, temp_1, temp_2, row, column).Obtain();
                value[1] = new RealSub(comp_2[i], x, y, temp_1, temp_2, row, column).Obtain();
            }
            return split[^1] == "1" ? value[0] : value[1];
        }
        #endregion

        #region Elements
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe DoubleMatrix Const(double c)
        {
            DoubleMatrix output = new(row, column); double* srcPtr = output.Ptr();
            for (int q = 0; q < column; q++, srcPtr++) *srcPtr = c; srcPtr = output.Ptr();
            Parallel.For(1, row, p => { double* destPtr = output.RowPtr(p);
                Unsafe.CopyBlock(destPtr, srcPtr, columnSIZE); });
            return output;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Copy(DoubleMatrix src, DoubleMatrix dest)
        {
            Parallel.For(0, row, p => { double* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                Unsafe.CopyBlock(destPtr, srcPtr, columnSIZE); });
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Negate(DoubleMatrix src, DoubleMatrix dest)
        {
            Parallel.For(0, row, p => { double* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr = -*srcPtr; });
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Plus(DoubleMatrix src, DoubleMatrix dest)
        {
            Parallel.For(0, row, p => { double* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr +=*srcPtr; });
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Subtract(DoubleMatrix src, DoubleMatrix dest)
        {
            Parallel.For(0, row, p => { double* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr -= *srcPtr; });
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Multiply(DoubleMatrix src, DoubleMatrix dest)
        {
            Parallel.For(0, row, p => { double* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr *= *srcPtr; });
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Divide(DoubleMatrix src, DoubleMatrix dest)
        {
            Parallel.For(0, row, p => { double* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr /= *srcPtr; });
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Power(DoubleMatrix src, DoubleMatrix dest)
        {
            Parallel.For(0, row, p => { double* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr = Math.Pow(*srcPtr, *destPtr); });
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void FuncSub(DoubleMatrix values, Func<double, double> function)
        {
            Parallel.For(0, row, r => { double* valuesPtr = values.RowPtr(r);
                for (int c = 0; c < column; c++, valuesPtr++) *valuesPtr = function(*valuesPtr); });
        }
        #endregion

        #region Assembly
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DoubleMatrix Transform(string input)
        {
            if (input[0] == '[') return braValues[Int32.Parse(MyString.Extract(input, 1, input.IndexOf(']') - 1))];
            return input[0] switch
            {
                'x' => x, 'y' => y, 'X' => X, 'Y' => Y,
                'e' => Const(Math.E),
                'p' => Const(Math.PI),
                'g' => Const(GAMMA),
                _ => Const(Double.Parse(input))
            };
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private DoubleMatrix BreakPlusSubtract(string input)
        {
            var (signs, chunks) = MyString.PlusMultiplyBreaker(input[0] == '-' ? input : String.Concat('+', input), "+-", '+', THRESHOLD);
            DoubleMatrix sum = BraFreePart(MyString.TrimStartChars(chunks[0], new char[] { '+' }));
            for (int i = 1, length = chunks.Length; i < length; i++)
                Plus(BraFreePart(signs[i - 1] == ':' ? chunks[i] : String.Concat('-', chunks[i])), sum);
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private DoubleMatrix PlusSubtractCore(string input)
        {
            if (!MyString.ContainsAny(input, new char[] { '+', '-' })) return MultiplyDivideCore(input);
            if (MyString.CountChar(input, '+') + MyString.CountChar(input, '-') > THRESHOLD) return BreakPlusSubtract(input);
            bool begins_minus = input[0] == '-';
            input = MyString.TrimStartChars(input, new char[] { '-' });
            string[] temp_split = MyString.SplitByChars(input, new char[] { '+', '-' });
            input = String.Concat(begins_minus ? '-' : '+', input);
            StringBuilder psBuilder = new();
            for (int i = 0, length = input.Length; i < length; i++) if ("+-".Contains(input[i])) psBuilder.Append(input[i]);
            DoubleMatrix sum = new(row, column), term = new(row, column);
            for (int i = 0, _length = temp_split.Length; i < _length; i++)
            {
                term = MultiplyDivideCore(temp_split[i]); bool tmp = psBuilder[i] == '+';
                Action<DoubleMatrix, DoubleMatrix> operation = i == 0 ? (tmp ? Copy : Negate) : (tmp ? Plus : Subtract);
                operation(term, sum);
            }
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private DoubleMatrix BreakMultiplyDivide(string input)
        {
            var (signs, chunks) = MyString.PlusMultiplyBreaker(String.Concat('*', input), "*/", '*', THRESHOLD);
            DoubleMatrix product = BraFreePart(MyString.TrimStartChars(chunks[0], new char[] { '*' }));
            for (int i = 1, length = chunks.Length; i < length; i++)
                Multiply(BraFreePart(signs[i - 1] == ':' ? chunks[i] : String.Concat("1/", chunks[i])), product);
            return product;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private DoubleMatrix MultiplyDivideCore(string tmpSplit)
        {
            DoubleMatrix term = new(row, column);
            if (!MyString.ContainsAny(tmpSplit, new char[] { '*', '/' })) Copy(PowerCore(tmpSplit), term);
            else if (MyString.CountChar(tmpSplit, '*') + MyString.CountChar(tmpSplit, '/') > THRESHOLD)
                return BreakMultiplyDivide(tmpSplit);
            else
            {
                string[] split = MyString.SplitByChars(tmpSplit, new char[] { '*', '/' });
                StringBuilder mdBuilder = new();
                for (int k = 0, length = tmpSplit.Length; k < length; k++) if ("*/".Contains(tmpSplit[k])) mdBuilder.Append(tmpSplit[k]);
                Copy(PowerCore(split[0]), term);
                for (int k = 1, _length = split.Length; k < _length; k++)
                {
                    Action<DoubleMatrix, DoubleMatrix> operation = mdBuilder[k - 1] == '*' ? Multiply : Divide;
                    operation(PowerCore(split[k]), term);
                }
            }
            return term;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private DoubleMatrix BreakPower(string input)
        {
            StringBuilder result = new(input);
            for (int i = 0, flag = 0, length = result.Length; i < length; i++)
            {
                if (result[i] != '^') continue;
                if (++flag % THRESHOLD == 0) result.Remove(i, 1).Insert(i, ":");
            }
            string[] chunks = MyString.SplitByChars(result.ToString(), new char[] { ':' });
            DoubleMatrix term = new(row, column);
            Copy(PowerCore(chunks[^1]), term);
            for (int m = chunks.Length - 2; m >= 0; m--)
            {
                string[] split = MyString.SplitByChars(chunks[m], new char[] { '^' });
                for (int t = split.Length - 1; t >= 0; t--) Power(Transform(split[t]), term);
            }
            return term;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private DoubleMatrix PowerCore(string split)
        {
            if (!split.Contains('^')) return Transform(split);
            else if (MyString.CountChar(split, '^') > THRESHOLD) return BreakPower(split);
            else
            {
                string[] inner_string = MyString.SplitByChars(split, new char[] { '^' });
                DoubleMatrix tower = new(row, column);
                Copy(Transform(inner_string[^1]), tower);
                for (int m = inner_string.Length - 2; m >= 0; m--) Power(Transform(inner_string[m]), tower);
                return tower;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private DoubleMatrix BraFreePart(string input)
        {
            if (Int32.TryParse(input, out int result)) return Const(result); // Double is slower
            if (input[0] == '[' && Int32.TryParse(MyString.Extract(input, 1, input.Length - 2), out int newResult))
                return braValues[newResult];
            return PlusSubtractCore(input);
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private string SeriesSub(string input)
        {
            if (!input.Contains('_')) return input;
            int i = input.IndexOf('_'), end = MyString.PairedParenthesis(input, i + 1);
            string temp = MyString.Extract(input, i + 2, end - 1);
            braValues[count] = input[i - 1] switch
            {
                'S' => Sum(temp),
                'P' => Product(temp),
                'F' => Hypergeometric(temp),
                'G' => Gamma(temp),
                'B' => Beta(temp),
                'Z' => Zeta(temp),
                'M' => Mod(temp),
                'C' => Combination(temp),
                'A' => Permutation(temp),
                '>' => Max(temp),
                '<' => Min(temp),
                'I' when input[i - 2] == '1' => Iterate1(temp),
                'I' when input[i - 2] == '2' => Iterate2(temp),
                'J' when input[i - 2] == '1' => Composite1(temp),
                'J' when input[i - 2] == '2' => Composite2(temp),
            };
            return MyString.Replace(input, MyString.BracketSub(count++), i - ("IJ".Contains(input[i - 1]) ? 3 : 2), end);
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public DoubleMatrix Obtain()
        {
            string temp = input; DoubleMatrix subValue; Func<double, double> f;
            do { temp = SeriesSub(temp); } while (temp.Contains('_'));
            int length = MyString.CountChar(temp, '('), begin = temp.Length - 1, end;
            for (int i = 0, tagL = -1; i < length; i++) // Because of ~ as the head of each tag
            {
                (begin, end) = MyString.InnerParenthesis(temp, begin);
                if (end == -1) end = MyString.PairedInnerParenthesis(temp, begin);
                subValue = BraFreePart(MyString.Extract(temp, begin + 1, end - 1));
                if (begin > 0)
                {
                    bool isA = begin > 1 ? temp[begin - 2] != 'a' : false; // The check is not redundant
                    switch (temp[begin - 1])
                    {
                        case 's': f = isA ? Math.Sin : Math.Asin; FuncSub(subValue, f); tagL = isA ? 1 : 2; break;
                        case 'c': f = isA ? Math.Cos : Math.Acos; FuncSub(subValue, f); tagL = isA ? 1 : 2; break;
                        case 't': f = isA ? Math.Tan : Math.Atan; FuncSub(subValue, f); tagL = isA ? 1 : 2; break;
                        case 'h':
                            bool IsA = temp[begin - 3] != 'a'; // Don't need check because of ~
                            switch (temp[begin - 2])
                            {
                                case 's': f = IsA ? Math.Sinh : Math.Asinh; FuncSub(subValue, f); tagL = IsA ? 2 : 3; break;
                                case 'c': f = IsA ? Math.Cosh : Math.Acosh; FuncSub(subValue, f); tagL = IsA ? 2 : 3; break;
                                case 't': f = IsA ? Math.Tanh : Math.Atanh; FuncSub(subValue, f); tagL = IsA ? 2 : 3; break;
                            }
                            break;
                        case 'a': FuncSub(subValue, Math.Abs); tagL = 1; break;
                        case 'l': FuncSub(subValue, Math.Log); tagL = 1; break;
                        case 'E': FuncSub(subValue, Math.Exp); tagL = 1; break;
                        case 'q': FuncSub(subValue, Math.Sqrt); tagL = 1; break;
                        case '!': FuncSub(subValue, Factorial); tagL = 1; break;
                        case '$': // Special for real
                            switch (temp[begin - 2])
                            {
                                case 'f': FuncSub(subValue, Math.Floor); tagL = 2; break;
                                case 'c': FuncSub(subValue, Math.Ceiling); tagL = 2; break;
                                case 'r': FuncSub(subValue, Math.Round); tagL = 2; break;
                                case 's': FuncSub(subValue, MySign); tagL = 2; break;
                            }
                            break;
                        default: break;
                    }
                }
                braValues[count] = subValue;
                begin -= tagL + 1; tagL = -1;
                temp = MyString.Replace(temp, MyString.BracketSub(count++), begin--, end);
            }
            return BraFreePart(temp);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Obtain(string input, double x = 0.0) => new RealSub(input, x).Obtain()[0, 0];
        #endregion
    }
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Complex
    {
        public readonly double real, imaginary;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Complex(double real, double imaginary = 0.0) { this.real = real; this.imaginary = imaginary; }

        #region Operations
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator -(Complex input) => new(-input.real, -input.imaginary);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator +(Complex input_1, Complex input_2)
            => new(input_1.real + input_2.real, input_1.imaginary + input_2.imaginary);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator -(Complex input_1, Complex input_2)
            => new(input_1.real - input_2.real, input_1.imaginary - input_2.imaginary);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator *(Complex input_1, Complex input_2)
        {
            double real1 = input_1.real, imag1 = input_1.imaginary;
            double real2 = input_2.real, imag2 = input_2.imaginary;
            return new(real1 * real2 - imag1 * imag2, real1 * imag2 + imag1 * real2);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator /(Complex input_1, Complex input_2)
        {
            double real1 = input_1.real, imag1 = input_1.imaginary;
            double real2 = input_2.real, imag2 = input_2.imaginary;
            double modSquare = real2 * real2 + imag2 * imag2;
            return new Complex((real1 * real2 + imag1 * imag2) / modSquare, (imag1 * real2 - real1 * imag2) / modSquare);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator ^(Complex input_1, Complex input_2) => Pow(input_1, input_2);
        #endregion

        #region Elementary Functions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Pow(Complex input_1, Complex input_2)
        {
            if (input_1.real == 0 && input_1.imaginary == 0) return new(0);
            return Exp(input_2 * Log(input_1));
        } // Extremely sensitive, mustn't move a hair.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Log(Complex input) => new(Math.Log(Modulus(input)), Math.Atan2(input.imaginary, input.real));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Exp(Complex input)
        {
            double expReal = Math.Exp(input.real), imag = input.imaginary;
            return new(expReal * Math.Cos(imag), expReal * Math.Sin(imag));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Ei(Complex input) => Exp(new Complex(0, Math.Tau) * input);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Sin(Complex input)
        { Complex temp = Exp(new Complex(0, 1) * input); return (temp - new Complex(1) / temp) / new Complex(0, 2); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Cos(Complex input)
        { Complex temp = Exp(new Complex(0, 1) * input); return (temp + new Complex(1) / temp) / new Complex(2); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Tan(Complex input)
            => new Complex(0, 1) * (new Complex(-1) + new Complex(2) / (new Complex(1) + Exp(new Complex(0, 2) * input)));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Asin(Complex input)
            => new Complex(0, -1) * Log(new Complex(0, 1) * input + Sqrt(new Complex(1) - input * input));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Acos(Complex input)
            => new Complex(0, -1) * Log(input + new Complex(0, 1) * Sqrt(new Complex(1) - input * input));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Atan(Complex input)
            => Log(new Complex(-1) + new Complex(0, 2) / (new Complex(0, 1) + input)) / new Complex(0, 2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Sinh(Complex input)
        { Complex temp = Exp(input); return (temp - new Complex(1) / temp) / new Complex(2); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Cosh(Complex input)
        { Complex temp = Exp(input); return (temp + new Complex(1) / temp) / new Complex(2); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Tanh(Complex input)
            => new Complex(1) - new Complex(2) / (new Complex(1) + Exp(new Complex(2) * input));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Asinh(Complex input) => Log(input + Sqrt(new Complex(1) + input * input));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Acosh(Complex input) => Log(input + Sqrt(new Complex(-1) + input * input));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Atanh(Complex input)
            => Log(new Complex(-1) + new Complex(2) / (new Complex(1) - input)) / new Complex(2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Sqrt(Complex input) => input ^ new Complex(0.5);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex ModulusComplex(Complex input) => new(Modulus(input));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Modulus(Complex input)
        {
            double real = input.real, imag = input.imaginary;
            return Math.Sqrt(real * real + imag * imag);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Conjugate(Complex input) => new(input.real, -input.imaginary);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Factorial(Complex input) => new(RealSub.Factorial(input.real));
        #endregion
    }
    public readonly struct ComplexMatrix
    {
        private readonly Complex[] data; private readonly int Rows, Columns;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComplexMatrix(int rows, int columns) { data = new Complex[rows * columns]; Rows = rows; Columns = columns; }
        public Complex this[int row, int column]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => data[row * Columns + column];
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => data[row * Columns + column] = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe Complex* Ptr() { fixed (Complex* ptr = &data[0]) { return ptr; } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe Complex* RowPtr(int row) { fixed (Complex* ptr = &data[row * Columns]) { return ptr; } }
    }
    public class ComplexSub
    {
        private const double GAMMA = 0.57721566490153286060651209008240243;
        private const int THRESHOLD = 10, STRUCTSIZE = 16; // Marshal.SizeOf<Complex>()
        private string input;
        private int row, column, count;
        private uint columnSIZE;
        private ComplexMatrix z, Z;
        private ComplexMatrix[] braValues;

        #region Constructors
        private void Initialize(string input, int row, int column)
        {
            if (String.IsNullOrEmpty(input)) throw new FormatException();
            this.input = RecoverMultiply.Recover(input, true);
            braValues = new ComplexMatrix[MyString.CountChar(this.input, '(')];
            z = Z = new(row, column);
            this.row = row; this.column = column; columnSIZE = (uint)(column * STRUCTSIZE);
        }
        public ComplexSub(string input, int row, int column) => Initialize(input, row, column);
        public ComplexSub(string input, ComplexMatrix z, int row, int column) : this(input, row, column) => this.z = z;
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe ComplexSub(string input, DoubleMatrix real, DoubleMatrix imaginary, int row, int column) 
            : this(input, row, column)
        {
            Parallel.For(0, row, i => {
                double* realPtr = real.RowPtr(i), imaginaryPtr = imaginary.RowPtr(i); Complex* zPtr = z.RowPtr(i);
                for (int j = 0; j < column; j++, realPtr++, imaginaryPtr++, zPtr++) *zPtr = new Complex(*realPtr, *imaginaryPtr);
            });
        }
        public ComplexSub(string input, ComplexMatrix z, ComplexMatrix Z, int row, int column) : this(input, row, column)
        { this.z = z; this.Z = Z; }
        #endregion

        #region Calculations
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ArgumentForRGB(Complex input) => ArgumentForRGB(input.real, input.imaginary);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ArgumentForRGB(double x, double y)
        {
            if (Double.IsNaN(x) && Double.IsNaN(y)) return -1;
            return y == 0 ? x == 0 ? -1 : x > 0 ? 0 : Math.PI : y > 0 ? Math.Atan2(y, x) : Math.Atan2(y, x) + Math.Tau;
        } // Extremely sensitive, mustn't move a hair.
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe ComplexMatrix Hypergeometric(string input)
        {
            string[] split = MyString.ReplaceRecover(input); int length = split.Length;
            if (length > 5 || length < 4) throw new FormatException();
            int n = length == 5 ? MyString.ToInt(split[4]) : 100;
            ComplexMatrix sum = new(row, column), product = Const(new Complex(1));
            ComplexMatrix input_new = new ComplexSub(split[3], z, Z, row, column).Obtain();
            ComplexMatrix _a = new ComplexSub(split[0], row, column).Obtain();
            ComplexMatrix _b = new ComplexSub(split[1], row, column).Obtain();
            ComplexMatrix _c = new ComplexSub(split[2], row, column).Obtain();
            Parallel.For(0, row, r => {
                Complex* prodPtr = product.RowPtr(r), sumPtr = sum.RowPtr(r), inputPtr = input_new.RowPtr(r);
                Complex* aPtr = _a.RowPtr(r), bPtr = _b.RowPtr(r), cPtr = _c.RowPtr(r);
                for (int c = 0; c < column; c++, prodPtr++, sumPtr++, inputPtr++, aPtr++, bPtr++, cPtr++)
                    for (int i = 0; i <= n; i++)
                    {
                        Complex temp = new(i - 1);
                        if (i != 0) *prodPtr *= *inputPtr * (*aPtr + temp) * (*bPtr + temp) / (*cPtr + temp) / new Complex(i);
                        *sumPtr += *prodPtr;
                    }
            });
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe ComplexMatrix Gamma(string input)
        {
            string[] split = MyString.ReplaceRecover(input); int length = split.Length;
            if (length > 2) throw new FormatException();
            int n = length == 2 ? MyString.ToInt(split[1]) : 100;
            Complex tmp1 = new(1), tmpMG = new(-GAMMA);
            ComplexMatrix product = Const(tmp1), temp_value = new ComplexSub(split[0], z, Z, row, column).Obtain();
            Parallel.For(0, row, r => {
                Complex* prodPtr = product.RowPtr(r), tempPtr = temp_value.RowPtr(r);
                for (int c = 0; c < column; c++, prodPtr++, tempPtr++)
                {
                    for (int i = 1; i <= n; i++)
                    { Complex temp = *tempPtr / new Complex(i); *prodPtr *= Complex.Exp(temp) / (tmp1 + temp); }
                }
            });
            ComplexMatrix output = new(row, column);
            Parallel.For(0, row, r => {
                Complex* outPtr = output.RowPtr(r), prodPtr = product.RowPtr(r), tempPtr = temp_value.RowPtr(r);
                for (int c = 0; c < column; c++, outPtr++, prodPtr++, tempPtr++)
                    *outPtr = *prodPtr * Complex.Exp(tmpMG * *tempPtr) / *tempPtr;
            });
            return output;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe ComplexMatrix Beta(string input)
        {
            string[] split = MyString.ReplaceRecover(input); int length = split.Length;
            if (length > 3 || length < 2) throw new FormatException();
            int n = length == 3 ? MyString.ToInt(split[2]) : 100;
            Complex tmp1 = new(1);
            ComplexMatrix product = Const(tmp1);
            ComplexMatrix temp_1 = new ComplexSub(split[0], z, Z, row, column).Obtain();
            ComplexMatrix temp_2 = new ComplexSub(split[1], z, Z, row, column).Obtain();
            Parallel.For(0, row, r => {
                Complex* prodPtr = product.RowPtr(r), temp1Ptr = temp_1.RowPtr(r), temp2Ptr = temp_2.RowPtr(r);
                for (int c = 0; c < column; c++, prodPtr++, temp1Ptr++, temp2Ptr++)
                {
                    for (int i = 1; i <= n; i++)
                    { Complex temp = new(i); *prodPtr *= tmp1 + *temp1Ptr * *temp2Ptr / (temp * (temp + *temp1Ptr + *temp2Ptr)); }
                }
            });
            ComplexMatrix output = new(row, column);
            Parallel.For(0, row, r => {
                Complex* outPtr = output.RowPtr(r), prodPtr = product.RowPtr(r),
                temp1Ptr = temp_1.RowPtr(r), temp2Ptr = temp_2.RowPtr(r);
                for (int c = 0; c < column; c++, outPtr++, temp1Ptr++, temp2Ptr++, prodPtr++)
                    *outPtr = (*temp1Ptr + *temp2Ptr) / (*temp1Ptr * *temp2Ptr) / *prodPtr;
            });
            return output;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe ComplexMatrix Zeta(string input)
        {
            string[] split = MyString.ReplaceRecover(input); int length = split.Length;
            if (length > 2) throw new FormatException();
            int n = length == 2 ? MyString.ToInt(split[1]) : 50;
            Complex tmp0 = new(0), tmp1 = new(1), tmp2 = new(2);
            ComplexMatrix sum = new(row, column), Sum = new(row, column), Coefficient = Const(tmp1), coefficient = Const(tmp1);
            ComplexMatrix temp_value = new ComplexSub(split[0], z, Z, row, column).Obtain();
            Parallel.For(0, row, r =>
            {
                Complex* coeffPtr = coefficient.RowPtr(r), CoeffPtr = Coefficient.RowPtr(r);
                Complex* SumPtr = Sum.RowPtr(r), sumPtr = sum.RowPtr(r);
                Complex* tempPtr = temp_value.RowPtr(r);
                for (int c = 0; c < column; c++, CoeffPtr++, coeffPtr++, SumPtr++, sumPtr++, tempPtr++)
                {
                    for (int i = 0; i <= n; i++)
                    {
                        *CoeffPtr /= tmp2; *coeffPtr = tmp1; *SumPtr = tmp0;
                        for (int j = 0; j <= i; j++)
                        {
                            *SumPtr += *coeffPtr / ((new Complex(j + 1)) ^ *tempPtr);
                            *coeffPtr *= new Complex((double)(j - i) / (double)(j + 1)); // Double is not redundant here
                        }
                        *SumPtr *= *CoeffPtr; *sumPtr += *SumPtr;
                    }
                    *sumPtr /= tmp1 - (tmp2 ^ (tmp1 - *tempPtr));
                }
            });
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private ComplexMatrix Sum(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length != 4) throw new FormatException();
            ComplexSub Buffer = new(split[0].Replace(split[1], MyString.IndexSub(0)), z, row, column);
            for (int i = MyString.ToInt(split[2]), int3 = MyString.ToInt(split[3]); i <= int3; i++)
            {
                Buffer.input = RecoverMultiply.Recover(split[0].Replace(split[1], MyString.IndexSub(i)), true);
                Buffer.count = 0; Plus(Buffer.Obtain(), Buffer.Z);
            }
            return Buffer.Z;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private ComplexMatrix Product(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length != 4) throw new FormatException();
            ComplexSub Buffer = new(split[0].Replace(split[1], MyString.IndexSub(0)), z, row, column) { Z = Const(new(1)) };
            for (int i = MyString.ToInt(split[2]), int3 = MyString.ToInt(split[3]); i <= int3; i++)
            {
                Buffer.input = RecoverMultiply.Recover(split[0].Replace(split[1], MyString.IndexSub(i)), true);
                Buffer.count = 0; Multiply(Buffer.Obtain(), Buffer.Z);
            }
            return Buffer.Z;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private ComplexMatrix Iterate(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length != 5) throw new FormatException();
            ComplexSub Buffer = new(split[0].Replace(split[2], MyString.IndexSub(0)), z, row, column)
            { Z = new ComplexSub(split[1], z, row, column).Obtain() };
            for (int i = MyString.ToInt(split[3]), int4 = MyString.ToInt(split[4]); i <= int4; i++)
            {
                Buffer.input = RecoverMultiply.Recover(split[0].Replace(split[2], MyString.IndexSub(i)), true);
                Buffer.count = 0; Buffer.Z = Buffer.Obtain();
            }
            return Buffer.Z;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private ComplexMatrix Composite(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            ComplexMatrix value = new ComplexSub(split[0], z, row, column).Obtain();
            for (int i = 1, length = split.Length; i < length; i++) value = new ComplexSub(split[i], z, value, row, column).Obtain();
            return value;
        }
        #endregion

        #region Elements
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe ComplexMatrix Const(Complex c)
        {
            ComplexMatrix output = new(row, column); Complex* srcPtr = output.Ptr();
            for (int q = 0; q < column; q++, srcPtr++) *srcPtr = c; srcPtr = output.Ptr();
            Parallel.For(1, row, p => { Complex* destPtr = output.RowPtr(p);
                Unsafe.CopyBlock(destPtr, srcPtr, columnSIZE); });
            return output;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Copy(ComplexMatrix src, ComplexMatrix dest)
        {
            Parallel.For(0, row, p => { Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                Unsafe.CopyBlock(destPtr, srcPtr, columnSIZE); });
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Negate(ComplexMatrix src, ComplexMatrix dest)
        {
            Parallel.For(0, row, p => { Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr = -*srcPtr; });
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Plus(ComplexMatrix src, ComplexMatrix dest)
        {
            Parallel.For(0, row, p => { Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr +=*srcPtr; });
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Subtract(ComplexMatrix src, ComplexMatrix dest)
        {
            Parallel.For(0, row, p => { Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr -= *srcPtr; });
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Multiply(ComplexMatrix src, ComplexMatrix dest)
        {
            Parallel.For(0, row, p => { Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr *= *srcPtr; });
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Divide(ComplexMatrix src, ComplexMatrix dest)
        {
            Parallel.For(0, row, p => { Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr /= *srcPtr; });
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Power(ComplexMatrix src, ComplexMatrix dest)
        {
            Parallel.For(0, row, p => { Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr = *srcPtr ^ *destPtr; });
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void FuncSub(ComplexMatrix values, Func<Complex, Complex> function)
        {
            Parallel.For(0, row, r => { Complex* valuesPtr = values.RowPtr(r);
                for (int c = 0; c < column; c++, valuesPtr++) *valuesPtr = function(*valuesPtr); });
        }
        #endregion

        #region Assembly
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ComplexMatrix Transform(string input)
        {
            if (input[0] == '[') return braValues[Int32.Parse(MyString.Extract(input, 1, input.IndexOf(']') - 1))];
            return input[0] switch
            {
                'z' => z, 'Z' => Z,
                'i' => Const(new Complex(0, 1)),
                'e' => Const(new Complex(Math.E)),
                'p' => Const(new Complex(Math.PI)),
                'g' => Const(new Complex(GAMMA)),
                _ => Const(new Complex(Double.Parse(input)))
            };
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private ComplexMatrix BreakPlusSubtract(string input)
        {
            var (signs, chunks) = MyString.PlusMultiplyBreaker(input[0] == '-' ? input : String.Concat('+', input), "+-", '+', THRESHOLD);
            ComplexMatrix sum = BraFreePart(MyString.TrimStartChars(chunks[0], new char[] { '+' }));
            for (int i = 1, length = chunks.Length; i < length; i++)
                Plus(BraFreePart(signs[i - 1] == ':' ? chunks[i] : String.Concat('-', chunks[i])), sum);
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private ComplexMatrix PlusSubtractCore(string input)
        {
            if (!MyString.ContainsAny(input, new char[] { '+', '-' })) return MultiplyDivideCore(input);
            if (MyString.CountChar(input, '+') + MyString.CountChar(input, '-') > THRESHOLD) return BreakPlusSubtract(input);
            bool begins_minus = input[0] == '-';
            input = MyString.TrimStartChars(input, new char[] { '-' });
            string[] temp_split = MyString.SplitByChars(input, new char[] { '+', '-' });
            input = String.Concat(begins_minus ? '-' : '+', input);
            StringBuilder psBuilder = new();
            for (int i = 0, length = input.Length; i < length; i++) if ("+-".Contains(input[i])) psBuilder.Append(input[i]);
            ComplexMatrix sum = new(row, column), term = new(row, column);
            for (int i = 0, _length = temp_split.Length; i < _length; i++)
            {
                term = MultiplyDivideCore(temp_split[i]); bool tmp = psBuilder[i] == '+';
                Action<ComplexMatrix, ComplexMatrix> operation = i == 0 ? (tmp ? Copy : Negate) : (tmp ? Plus : Subtract);
                operation(term, sum);
            }
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private ComplexMatrix BreakMultiplyDivide(string input)
        {
            var (signs, chunks) = MyString.PlusMultiplyBreaker(String.Concat('*', input), "*/", '*', THRESHOLD);
            ComplexMatrix product = BraFreePart(MyString.TrimStartChars(chunks[0], new char[] { '*' }));
            for (int i = 1, length = chunks.Length; i < length; i++)
                Multiply(BraFreePart(signs[i - 1] == ':' ? chunks[i] : String.Concat("1/", chunks[i])), product);
            return product;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private ComplexMatrix MultiplyDivideCore(string tmpSplit)
        {
            ComplexMatrix term = new(row, column);
            if (!MyString.ContainsAny(tmpSplit, new char[] { '*', '/' })) Copy(PowerCore(tmpSplit), term);
            else if (MyString.CountChar(tmpSplit, '*') + MyString.CountChar(tmpSplit, '/') > THRESHOLD)
                return BreakMultiplyDivide(tmpSplit);
            else
            {
                string[] split = MyString.SplitByChars(tmpSplit, new char[] { '*', '/' });
                StringBuilder mdBuilder = new();
                for (int k = 0, length = tmpSplit.Length; k < length; k++) if ("*/".Contains(tmpSplit[k])) mdBuilder.Append(tmpSplit[k]);
                Copy(PowerCore(split[0]), term);
                for (int k = 1, _length = split.Length; k < _length; k++)
                {
                    Action<ComplexMatrix, ComplexMatrix> operation = mdBuilder[k - 1] == '*' ? Multiply : Divide;
                    operation(PowerCore(split[k]), term);
                }
            }
            return term;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private ComplexMatrix BreakPower(string input)
        {
            StringBuilder result = new(input);
            for (int i = 0, flag = 0, length = result.Length; i < length; i++)
            {
                if (result[i] != '^') continue;
                if (++flag % THRESHOLD == 0) result.Remove(i, 1).Insert(i, ":");
            }
            string[] chunks = MyString.SplitByChars(result.ToString(), new char[] { ':' });
            ComplexMatrix term = new(row, column);
            Copy(PowerCore(chunks[^1]), term);
            for (int m = chunks.Length - 2; m >= 0; m--)
            {
                string[] split = MyString.SplitByChars(chunks[m], new char[] { '^' });
                for (int t = split.Length - 1; t >= 0; t--) Power(Transform(split[t]), term);
            }
            return term;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private ComplexMatrix PowerCore(string split)
        {
            if (!split.Contains('^')) return Transform(split);
            else if (MyString.CountChar(split, '^') > THRESHOLD) return BreakPower(split);
            else
            {
                string[] inner_string = MyString.SplitByChars(split, new char[] { '^' });
                ComplexMatrix tower = new(row, column);
                Copy(Transform(inner_string[^1]), tower);
                for (int m = inner_string.Length - 2; m >= 0; m--) Power(Transform(inner_string[m]), tower);
                return tower;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private ComplexMatrix BraFreePart(string input)
        {
            if (Int32.TryParse(input, out int result)) return Const(new Complex(result)); // Double is slower
            if (input[0] == '[' && Int32.TryParse(MyString.Extract(input, 1, input.Length - 2), out int newResult))
                return braValues[newResult];
            return PlusSubtractCore(input);
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private string SeriesSub(string input)
        {
            if (!input.Contains('_')) return input;
            int i = input.IndexOf('_'), end = MyString.PairedParenthesis(input, i + 1);
            string temp = MyString.Extract(input, i + 2, end - 1);
            braValues[count] = input[i - 1] switch
            {
                'S' => Sum(temp),
                'P' => Product(temp),
                'F' => Hypergeometric(temp),
                'G' => Gamma(temp),
                'B' => Beta(temp),
                'Z' => Zeta(temp),
                'I' => Iterate(temp),
                'J' => Composite(temp)
            };
            return MyString.Replace(input, MyString.BracketSub(count++), i - 2, end);
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public ComplexMatrix Obtain()
        {
            string temp = input; ComplexMatrix subValue; Func<Complex, Complex> f;
            do { temp = SeriesSub(temp); } while (temp.Contains('_'));
            int length = MyString.CountChar(temp, '('), begin = temp.Length - 1, end;
            for (int i = 0, tagL = -1; i < length; i++) // Because of ~ as the head of each tag
            {
                (begin, end) = MyString.InnerParenthesis(temp, begin);
                if (end == -1) end = MyString.PairedInnerParenthesis(temp, begin);
                subValue = BraFreePart(MyString.Extract(temp, begin + 1, end - 1));
                if (begin > 0)
                {
                    bool isA = begin > 1 ? temp[begin - 2] != 'a' : false; // The check is not redundant
                    switch (temp[begin - 1])
                    {
                        case 's': f = isA ? Complex.Sin : Complex.Asin; FuncSub(subValue, f); tagL = isA ? 1 : 2; break;
                        case 'c': f = isA ? Complex.Cos : Complex.Acos; FuncSub(subValue, f); tagL = isA ? 1 : 2; break;
                        case 't': f = isA ? Complex.Tan : Complex.Atan; FuncSub(subValue, f); tagL = isA ? 1 : 2; break;
                        case 'h':
                            bool IsA = temp[begin - 3] != 'a'; // Don't need check because of ~
                            switch (temp[begin - 2])
                            {
                                case 's': f = IsA ? Complex.Sinh : Complex.Asinh; FuncSub(subValue, f); tagL = IsA ? 2 : 3; break;
                                case 'c': f = IsA ? Complex.Cosh : Complex.Acosh; FuncSub(subValue, f); tagL = IsA ? 2 : 3; break;
                                case 't': f = IsA ? Complex.Tanh : Complex.Atanh; FuncSub(subValue, f); tagL = IsA ? 2 : 3; break;
                            }
                            break;
                        case 'a': FuncSub(subValue, Complex.ModulusComplex); tagL = 1; break;
                        case 'J': FuncSub(subValue, Complex.Conjugate); tagL = 1; break;
                        case 'l': FuncSub(subValue, Complex.Log); tagL = 1; break;
                        case 'E': FuncSub(subValue, Complex.Exp); tagL = 1; break;
                        case '#': FuncSub(subValue, Complex.Ei); tagL = 2; break; // Special for complex
                        case 'q': FuncSub(subValue, Complex.Sqrt); tagL = 1; break;
                        case '!': FuncSub(subValue, Complex.Factorial); tagL = 1; break;
                        default: break;
                    }
                }
                braValues[count] = subValue;
                begin -= tagL + 1; tagL = -1;
                temp = MyString.Replace(temp, MyString.BracketSub(count++), begin--, end);
            }
            return BraFreePart(temp);
        }
        #endregion
    }
}