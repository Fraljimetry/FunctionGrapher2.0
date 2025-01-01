/// Date: 2023.4~5; 2024.9~11
/// Designer: Fraljimetry

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Reflection;
using System.Media;
using System.Text;
using WMPLib;

namespace FunctionGrapher2._0
{
    /// <summary>
    /// DISPLAY SECTION
    /// </summary>
    public partial class Graph : Form
    {
        // 1.PREPARATIONS
        #region Fields
        private static SoundPlayer ClickPlayer;
        private static WindowsMediaPlayer MediaPlayer;
        private static DateTime TimeNow = new();
        private static TimeSpan TimeCount = new();
        private static System.Windows.Forms.Timer GraphTimer, ColorTimer, WaitTimer, DisplayTimer;
        private static Graphics graphics;
        private static Rectangle rectangle, rect_mac, rect_mic; // rect_: slightly larger than the display regions
        private static Bitmap bmp_mac, bmp_mic, bmp_screen; // bmp_screen: snapshots
        private static readonly Bitmap BMP_PIXEL = new(1, 1);
        private static readonly Size SIZE_PIXEL = new(1, 1);
        private static readonly SolidBrush BACK_BRUSH = new(Color.Black);
        private static readonly Pen BDR_PEN = new(Color.Gray), _BDR_PEN = new(Color.White), AXES_PEN = new(Color.DarkGray, 4f);
        private static readonly Color CORRECT_GREEN = Argb(192, 255, 192), ERROR_RED = Argb(255, 192, 192),
            UNCHECK_YELLOW = Argb(255, 255, 128), READONLY_PURPLE = Argb(255, 192, 255),
            COMBO_BLUE = Argb(192, 255, 255), FOCUS_GRAY = Color.LightGray, BACKDROP_GRAY = Argb(64, 64, 64),
            CONTROL_GRAY = Argb(105, 105, 105), GRID_GRAY = Argb(75, 255, 255, 255), READONLY_GRAY = Color.Gainsboro,
            UPPER_GOLD = Color.Gold, LOWER_BLUE = Color.RoyalBlue, ZERO_BLUE = Color.Lime, POLE_PURPLE = Color.Magenta;

        private static float scaling_factor; // Font size adaptation
        private static readonly float GRID_WIDTH_1 = 3f, GRID_WIDTH_2 = 2f, CURVE_WIDTH_LIMIT = 20f;
        private static int display_elapsed, x_left, x_right, y_up, y_down, color_mode, contour_mode,
            loop_number, chosen_number, export_number, pixel_number, segment_number;
        private static readonly int X_LEFT_MAC = 620, X_RIGHT_MAC = 1520, Y_UP_MAC = 45, Y_DOWN_MAC = 945,
            X_LEFT_MIC = 1565, X_RIGHT_MIC = 1765, Y_UP_MIC = 745, Y_DOWN_MIC = 945, X_LEFT_CHECK = 1921,
            X_RIGHT_CHECK = 1922, Y_UP_CHECK = 1081, Y_DOWN_CHECK = 1082, REF_POS_1 = 9, REF_POS_2 = 27,
            WIDTH_IND = 22, HEIGHT_IND = 55, LEFT_SUPP = 11, TOP_SUPP = 45, GRID = 5, UPDATE = 5, REFRESH = 100, SLEEP = 200;
        private static double title_elapsed, pause_pos, epsilon, stride, mod_stride, arg_stride, stride_real, size_real, decay;
        private static readonly double STRIDE = 0.25, MOD = 0.25, ARG = Math.PI / 12, STRIDE_REAL = 1, EPS_REAL = 0.015,
            EPS_COMPLEX = 0.015, SIZE_REAL = 0.5, DECAY = 0.2, DEPTH = 2, CURVE_WIDTH = 5, INCREMENT = 0.01, TITLE = 0.01;
        private static int[] borders; // = new int[] { x_left, x_right, y_up, y_down };
        private static double[] scopes; // WARNING: scopes[3] - scopes[2] < 0 < borders[3] - borders[2]
        private static ComplexMatrix output_complex;
        private static RealMatrix output_real;

        private static bool is_flashing, is_paused = true, is_complex = true, delete_point = true, delete_coor, swap_colors,
            is_auto, freeze_graph, clicked, shade, axes_drawn_mac, axes_drawn_mic, is_main, activate_mouse, is_checking,
            error_input, error_address, is_resized, ctrl_pressed, sft_pressed, suppress_key_up, bdp_painted;
        private static readonly string ADDRESS_DEFAULT = @"C:\Users\Public", DATE = "Oct, 2024", STOCKPILE = "stockpile",
            INPUT_DEFAULT = "z", GENERAL_DEFAULT = "e", THICK_DEFAULT = "1", DENSE_DEFAULT = "1", MACRO = "MACRO",
            MICRO = "MICRO", ZERO = "0", REMIND_EXPORT = "Snapshot saved at", REMIND_STORE = "History stored at",
            CAPTION_DEFAULT = "Yours inputs will be shown here.", MISTAKES_HEAD = "\r\nCommon mistakes include:",
            WRONG_FORMAT = "THE INPUT IS IN A WRONG FORMAT.", WRONG_ADDRESS = "THE ADDRESS DOES NOT EXIST.",
            DISPLAY_ERROR = "UNAVAILABLE.", DRAFT_DEFAULT = "\r\nDetailed historical info is documented here.",
            TEMP_BGM_NAME = "background_music", MUSIC = "music", SOUND = "click sound", TIP = "ReadOnly",
            SEP_1 = new('>', 4), SEP_2 = new('<', 4), SEP = new('~', 4), _SEP = new('*', 20), TAB = new(' ', 4);
        private static readonly string[] CONTOUR_MODES = new string[] { "Cartesian (x,y)", "Polar (r,θ)" },
            COLOR_MODES = new string[] { "Commonplace", "Monochromatic", "Bichromatic", "Kaleidoscopic", "Miscellaneous" };
        #endregion

        #region Initilizations
        public Graph()
        {
            InitializeComponent();
            SetTitleBarColor(); ReduceFontSizeByScale(this, ref scaling_factor);
            InitializeMusicPlayer(); InitializeClickSound(); AttachClickEvents(this);
            InitializeTimers(); InitializeGraphics();
            InitializeCombo(); InitializeData();
            SetThicknessDensenessScopesBorders();
            BanMouseWheel();
        }
        private void Graph_Load(object sender, EventArgs e) => TextBoxFocus(sender, e);
        private void Graph_Paint(object sender, PaintEventArgs e)
        { if (!bdp_painted && !clicked) SubtitleBox_DoubleClick(sender, e); }
        //
        private int SetTitleBarColor()
        {
            int mode = 1;  // Set to 1 to apply immersive color mode
            const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20; // Desktop Window Manager (DWM)
            return DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref mode, sizeof(Int32));
        }
        public static void ReduceFontSizeByScale(Control parentCtrl, ref float scalingFactor)
        {
            scalingFactor = Graphics.FromHwnd(IntPtr.Zero).DpiX / 96f / 1.5f; // Originally my PC was scaled to 150%
            foreach (Control ctrl in parentCtrl.Controls)
            {
                ctrl.Font = new(ctrl.Font.FontFamily, ctrl.Font.Size / scalingFactor, ctrl.Font.Style);
                if (ctrl.Controls.Count > 0) ReduceFontSizeByScale(ctrl, ref scalingFactor);
            }
        } // Also used for Message Boxes, so scalingFactor should not be passed as a field
        private static Stream? GetStream(string file)
            => Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(Program).Namespace}.{file}.wav");
        private static void InitializeMusicClick(Stream? stream, string message, Action<Stream> setUpStream)
        { if (stream != null) setUpStream(stream); else GetMusicClickErrorBox(message); }
        private void InitializeMusicPlayer() => InitializeMusicClick(GetStream("bgm"), MUSIC, soundStream =>
        {
            MediaPlayer = new();
            // Saving the stream to a temp file, since Windows Media Player cannot play directly from the stream
            string tempFile = Path.Combine(Path.GetTempPath(), $"{TEMP_BGM_NAME}.wav");
            using FileStream fileStream = new(tempFile, FileMode.Create, FileAccess.Write); // "using" should not be removed
            soundStream.CopyTo(fileStream);
            MediaPlayer.URL = tempFile; // Setting the media file
            MediaPlayer.settings.setMode("loop", true); // Looping the music
            MediaPlayer.controls.stop();
        });
        private static void InitializeClickSound() => InitializeMusicClick(GetStream("click"), SOUND, soundStream =>
        { ClickPlayer = new(soundStream); });
        private static void AttachClickEvents(Control ctrl)
        {
            ctrl.Click += (sender, e) => ClickPlayer?.Play();
            foreach (Control childCtrl in ctrl.Controls) AttachClickEvents(childCtrl);
        }
        private void InitializeTimers()
        {
            static System.Windows.Forms.Timer setT(int interval) => new() { Interval = interval };
            GraphTimer = setT(1000); ColorTimer = setT(50); WaitTimer = setT(500); DisplayTimer = setT(1000 / UPDATE);

            ColorTimer.Tick += (sender, e) =>
            {
                title_elapsed += TITLE;
                TitleLabel.ForeColor = ObtainColorWheelCurve(title_elapsed % 1);
            };
            WaitTimer.Tick += (sender, e) =>
            {
                ReverseBool(ref is_flashing); // Cannnot pass properties as reference
                PictureWait.Visible = is_flashing;
            };
            DisplayTimer.Tick += (sender, e) =>
            {
                if (++display_elapsed % UPDATE == 0) SetText(TimeDisplay, (display_elapsed / UPDATE).ToString() + "s");
                SetText(PointNumDisplay, (pixel_number + segment_number).ToString()); // Refreshing $"{RATE}" times per second
            };
        }
        private void InitializeGraphics()
        {
            graphics = CreateGraphics();
            bmp_mac = bmp_mic = new(Width, Height, PixelFormat.Format32bppArgb);
            bmp_screen = new(Width - WIDTH_IND, Height - HEIGHT_IND);

            rectangle = new(0, 0, Width, Height);
            int indent = (int)(CURVE_WIDTH_LIMIT / 2), _indent = indent * 2,
                widthMac = X_RIGHT_MAC - X_LEFT_MAC, heightMac = Y_DOWN_MAC - Y_UP_MAC,
                widthMic = X_RIGHT_MIC - X_LEFT_MIC, heightMic = Y_DOWN_MIC - Y_UP_MIC;
            rect_mac = new(X_LEFT_MAC - indent, Y_UP_MAC - indent, widthMac + _indent, heightMac + _indent);
            rect_mic = new(X_LEFT_MIC - indent, Y_UP_MIC - indent, widthMic + _indent, heightMic + _indent);

            DoubleBuffered = KeyPreview = true; // Essential for shortcuts
        }
        private void InitializeCombo()
        {
            static void coloringContour_AddItem(ComboBox cbx, int index, string[] options)
            { cbx.Items.AddRange(options); cbx.SelectedIndex = index; }
            coloringContour_AddItem(ComboColoring, 4, COLOR_MODES);
            coloringContour_AddItem(ComboContour, 1, CONTOUR_MODES);

            void addExamples(string[] items) { foreach (string item in items) ComboExamples.Items.Add(item); }
            addExamples(ReplaceTags.EX_COMPLEX);
            ComboExamples.Items.Add(String.Empty);
            addExamples(ReplaceTags.EX_REAL);
            ComboExamples.Items.Add(String.Empty);
            addExamples(ReplaceTags.EX_CURVES);

            void functionsSpecial_AddItem(string[] options, bool isFunc)
            {
                string[] modifiedOptions = new string[options.Length]; int index = 0;
                foreach (string option in options) modifiedOptions[index++] = String.Concat(option, RecoverMultiply.LR_BRA);
                Action<string[]> addOptions = isFunc ? ComboFunctions.Items.AddRange : ComboSpecial.Items.AddRange;
                addOptions(modifiedOptions);
            }
            functionsSpecial_AddItem(ReplaceTags.FUNCTIONS, true);
            functionsSpecial_AddItem(ReplaceTags.SPECIALS, false);
        }
        private void RecoverInput()
        {
            SetText(InputString, INPUT_DEFAULT); SetText(AddressInput, ADDRESS_DEFAULT);
            SetText(GeneralInput, GENERAL_DEFAULT);
            SetText(ThickInput, THICK_DEFAULT); SetText(DenseInput, DENSE_DEFAULT);
            InputString_Focus();
        }
        private void InitializeData() { RecoverInput(); SetText(DraftBox, DRAFT_DEFAULT); SetText(CaptionBox, CAPTION_DEFAULT); }
        private void SetThicknessDensenessScopesBorders(bool autoFill = true)
        {
            FillEmpty(GeneralInput, GENERAL_DEFAULT);
            FillEmpty(ThickInput, THICK_DEFAULT); FillEmpty(DenseInput, DENSE_DEFAULT);

            TextBox[] tbxDetails = { X_Left, X_Right, Y_Right, Y_Left }; // Crucial ordering
            if (autoFill) foreach (var tbx in tbxDetails) FillEmpty(tbx, ZERO);

            double _dense = Obtain(DenseInput), _thick = Obtain(ThickInput);
            stride_real = STRIDE_REAL / _dense; stride = STRIDE / _dense;
            mod_stride = MOD / _dense; arg_stride = ARG / _dense;
            epsilon = (is_complex ? EPS_COMPLEX : EPS_REAL) * _thick; // For lines and complex extremities
            size_real = SIZE_REAL * _thick / (1 + _thick); // For real extremities
            decay = DECAY * _thick;

            int i = 0;
            if (!GeneralInput_Undo())
            {
                double _scope = Obtain(GeneralInput);
                scopes = new double[] { -_scope, _scope, _scope, -_scope }; // Remind the signs
                foreach (var tbx in tbxDetails) SetText(tbxDetails[i], scopes[i++].ToString("#0.0000"));
            }
            else foreach (var tbx in tbxDetails) scopes[i] = RealSub.Obtain(RecoverMultiply.Beautify(tbxDetails[i++].Text, false));
            if (InvalidScopesX() || InvalidScopesY()) MyString.ThrowException(); // The detailed exception is determined later

            borders = new int[] { x_left, x_right, y_up, y_down };
        }
        private void BanMouseWheel()
        {
            ComboBox[] comboBoxes = { ComboExamples, ComboFunctions, ComboSpecial, ComboColoring, ComboContour };
            foreach (var cbx in comboBoxes) cbx.MouseWheel += (sender, e) => ((HandledMouseEventArgs)e).Handled = true;
        } // The default wheeling clashes with the personalized combo boxes
        private void TextBoxFocus(object sender, EventArgs e)
        {
            foreach (var ctrl in Controls.OfType<TextBox>())
                ctrl.GotFocus += (sender, e) => { ((TextBox)sender).SelectionStart = ((TextBox)sender).Text.Length; };
        } // Forcing the caret to appear at the end of each textbox
        #endregion

        #region External Methods
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool BlockInput([MarshalAs(UnmanagedType.Bool)] bool fBlockIt);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool HideCaret(IntPtr hWnd); // Also used for Message Boxes
        protected override void WndProc(ref Message m) // Window Procedure
        {
            const int WM_NCLBTNDOWN = 0x00A1; // Window Message, Non-Client Left Button Down
            const int HTCAPTION = 0x0002; // Hit Test Caption
            if (m.Msg == WM_NCLBTNDOWN && m.WParam.ToInt32() == HTCAPTION) return; // Preventing dragging the title bar
            base.WndProc(ref m);
        } // Overriding WndProc to customize window behavior
        #endregion

        #region Shorthands
        private static int AddOne(int input) => input + 1;
        private static Color Swap(Color c1, Color c2) => swap_colors ? c1 : c2;
        private static Color Argb(int a, int r, int g, int b) => Color.FromArgb(a, r, g, b);
        public static Color Argb(int r, int g, int b) => Color.FromArgb(r, g, b); // Also used for Message Boxes
        public static double ArgRGB(double x, double y) => Double.IsNaN(x) && Double.IsNaN(y) ? -1 :
            y == 0 ? (x == 0 ? -1 : x > 0 ? 0 : Math.PI) : (y > 0 ? Math.Atan2(y, x) : Math.Atan2(y, x) + Math.Tau); // Sensitive checking
        private static int Frac(int input, double alpha) => (int)(input * alpha);
        private static bool IllegalRatio(double ratio) => ratio < 0 || ratio > 1;
        private static int GetRow(int[] borders) => borders[1] - borders[0];
        private static int GetColumn(int[] borders) => borders[3] - borders[2];
        private static double Get_Row() => scopes[1] - scopes[0];
        private static double Get_Column() => scopes[3] - scopes[2]; // The sign convention varies from place to place
        private static bool InvalidScopesX() => scopes[0] >= scopes[1];
        private static bool InvalidScopesY() => scopes[3] >= scopes[2];
        private static int[] GetBorders(int mode) => mode switch
        {
            1 => new int[] { X_LEFT_MAC, X_RIGHT_MAC, Y_UP_MAC, Y_DOWN_MAC },
            2 => new int[] { X_LEFT_MIC, X_RIGHT_MIC, Y_UP_MIC, Y_DOWN_MIC },
            3 => new int[] { X_LEFT_CHECK, X_RIGHT_CHECK, Y_UP_CHECK, Y_DOWN_CHECK }
        };
        private static Rectangle GetRect(int[] borders, int margin = 0)
            => new(borders[0] + margin, borders[2] + margin, GetRow(borders) - margin, GetColumn(borders) - margin);
        private static Bitmap GetBitmap(bool isMain) => isMain ? bmp_mac : bmp_mic;
        private static ref bool ReturnAxesDrawn(bool isMain) => ref (isMain ? ref axes_drawn_mac : ref axes_drawn_mic);
        private static void SetAxesDrawn(bool isMain, bool drawn) { ReturnAxesDrawn(isMain) = drawn; }
        private static void ReverseBool(ref bool isChecked) => isChecked = !isChecked;
        private static double Obtain(TextBox tbx) => RealSub.Obtain(tbx.Text);
        private static void SetText(TextBox tbx, string text) => tbx.Text = text;
        private static void FillEmpty(TextBox tbx, string text) { if (String.IsNullOrEmpty(tbx.Text)) SetText(tbx, text); }
        private void AddDraft(string text) => SetText(DraftBox, text + DraftBox.Text);
        private void SetScrollBars(bool enabled) => VScrollBarX.Enabled = VScrollBarY.Enabled = enabled;
        private bool GeneralInput_Undo() => GeneralInput.Text == ZERO;
        private void ComboExamples_Undo() => ComboExamples.SelectedIndex = -1;
        private void InputString_Focus() { InputString.Focus(); InputString.SelectionStart = InputString.Text.Length; }
        private bool NoInput() => String.IsNullOrEmpty(InputString.Text);
        private bool ProcessingGraphics() => InputString.ReadOnly;
        #endregion

        #region Auxiliary Drawings
        private static void DrawBackdrop(int[] borders)
        { graphics.DrawRectangle(BDR_PEN, GetRect(borders)); graphics.FillRectangle(BACK_BRUSH, GetRect(borders, 1)); }
        private static void DrawAxesGrids(int[] borders)
        {
            bdp_painted = true; // To prevent calling Graph_Paint afterwards
            static double calculateGrid(double range) => Math.Pow(GRID, Math.Floor(Math.Log(range / 2) / Math.Log(GRID)));
            double xGrid = calculateGrid(Get_Row()), yGrid = calculateGrid(-Get_Column()); // Remind the minus sign

            void drawGrids(double xGrid, double yGrid, float penWidth)
            {
                Pen gridPen = new(GRID_GRAY, penWidth); int pos;
                MyString.For((int)Math.Floor(scopes[3] / yGrid), (int)Math.Ceiling(scopes[2] / yGrid), i =>
                {
                    pos = LinearTransform(0, i * yGrid, borders).y;
                    if (pos > borders[2] && pos < borders[3]) graphics.DrawLine(gridPen, AddOne(borders[0]), pos, borders[1], pos);
                });
                MyString.For((int)Math.Floor(scopes[0] / xGrid), (int)Math.Ceiling(scopes[1] / xGrid), i =>
                {
                    pos = LinearTransform(i * xGrid, 0, borders).x;
                    if (pos > borders[0] && pos < borders[1]) graphics.DrawLine(gridPen, pos, borders[3], pos, AddOne(borders[2]));
                });
            }
            drawGrids(xGrid, yGrid, GRID_WIDTH_1); drawGrids(xGrid / GRID, yGrid / GRID, GRID_WIDTH_2);

            var (x, y) = LinearTransform(0.0, 0.0, borders);
            if (y > borders[2] && y < borders[3]) graphics.DrawLine(AXES_PEN, AddOne(borders[0]), y, borders[1], y);
            if (x > borders[0] && x < borders[1]) graphics.DrawLine(AXES_PEN, x, borders[3], x, AddOne(borders[2]));
        }
        private static void DrawBackdropAxesGrids(int[] borders, bool isMain, bool isFreezed = false)
        {
            if (!isFreezed) { DrawBackdrop(borders); SetAxesDrawn(isMain, false); }
            if (!delete_coor && !ReturnAxesDrawn(isMain)) { DrawAxesGrids(borders); SetAxesDrawn(isMain, true); }
        } // Sensitive
        private void DrawReferenceRectangles(Color color)
            => graphics.FillRectangle(new SolidBrush(color), VScrollBarX.Location.X - REF_POS_1, Y_UP_MIC + REF_POS_2,
                2 * (VScrollBarX.Width + REF_POS_1), VScrollBarX.Height - 2 * REF_POS_2);
        private void DrawScrollBar((double, double) xyCoor)
        {
            int range = VScrollBarX.Maximum - VScrollBarX.Minimum;
            VScrollBarX.Value = Frac(range, (xyCoor.Item1 - scopes[0]) / Get_Row());
            VScrollBarY.Value = Frac(range, (xyCoor.Item2 - scopes[3]) / -Get_Column()); // Remind the minus sign
        }
        #endregion

        // 2.GRAPHING
        #region Numerics
        private static (double, double) GetRatio(int[] borders) => (Get_Row() / GetRow(borders), Get_Column() / GetColumn(borders));
        private static (int x, int y) LinearTransform(double x, double y, int[] borders)
        {
            double _x = GetRow(borders) / Get_Row(), _y = GetColumn(borders) / Get_Column();
            return ((int)(borders[0] + (x - scopes[0]) * _x), (int)(borders[2] + (y - scopes[2]) * _y));
        }
        private static (double, double) LinearTransform(int x, int y, int[] borders) => LinearTransform(x, y, GetRatio(borders), borders);
        private static (double, double) LinearTransform(int x, int y, (double, double) _xy, int[] borders)
            => (scopes[0] + (x - borders[0]) * _xy.Item1, scopes[2] + (y - borders[2]) * _xy.Item2); // For optimization
        private static int LowerIndex(double a, double m) => (int)Math.Floor(a / m);
        private static double LowerDistance(double a, double m) => a - m * LowerIndex(a, m);
        private static double LowerRatio(double a, double m) => a == -0 ? 1 : LowerDistance(a, m) / m; // -0 is necessary
        private static double GetShade(double alpha) => (alpha - 1) / DEPTH + 1;
        private unsafe static (double, double) FiniteExtremities(RealMatrix output, int row, int column)
        {
            static double seekM(Func<double, double, double> function, double* ptr, int length)
            {
                double val = Double.NaN;
                for (int i = 0; i < length; i++, ptr++)
                { if (Double.IsNaN(*ptr)) continue; if (Double.IsNaN(val)) val = *ptr; else val = function(*ptr, val); }
                return val;
            }
            RealMatrix outputAtan = new(row, column), minMax = new(2, row); // outputAtan is necessary
            Parallel.For(0, row, r =>
            {
                double* destPtr = outputAtan.RowPtr(r), srcPtr = output.RowPtr(r);
                for (int c = 0; c < column; c++, destPtr++, srcPtr++) *destPtr = Math.Atan(*srcPtr);
                minMax[0, r] = seekM(Math.Min, outputAtan.RowPtr(r), column);
                minMax[1, r] = seekM(Math.Max, outputAtan.RowPtr(r), column);
            });
            return (seekM(Math.Min, minMax.RowPtr(0), row), seekM(Math.Max, minMax.RowPtr(1), row));
        } // To find the min and max of the atan'ed matrix to prevent infinitude
        private unsafe static (int, int, RealMatrix, RealMatrix) GetRowColumnCoor()
        {
            var (row, column) = (GetRow(borders), GetColumn(borders));
            RealMatrix xCoor = new(row, column), yCoor = new(row, column);
            int xLeft = AddOne(borders[0]), yUp = AddOne(borders[2]); var _xy = GetRatio(borders);
            Parallel.For(0, row, i => {
                double* xPtr = xCoor.RowPtr(i), yPtr = yCoor.RowPtr(i); int _xLeft = i + xLeft, _yUp = yUp; // Must NOT be pre-defined
                for (int j = 0; j < column; j++, _yUp++, xPtr++, yPtr++) (*xPtr, *yPtr) = LinearTransform(_xLeft, _yUp, _xy, borders);
            });
            return (row, column, xCoor, yCoor);
        }
        #endregion

        #region Rendering Core
        private static BitmapData GetBmpData(Bitmap bmp)
            => bmp.LockBits(rectangle, ImageLockMode.ReadWrite, bmp.PixelFormat);
        private unsafe static void ClearBitmap(Bitmap bmp)
        {
            int bpp = Image.GetPixelFormatSize(bmp.PixelFormat) / 8, width = rectangle.Width * bpp; // bpp: bytesPerPixel
            BitmapData data = GetBmpData(bmp); var ptr = (byte*)data.Scan0;
            Parallel.For(0, rectangle.Height, y => {
                byte* rowPtr = ptr + y * data.Stride + 3; // It suffices to set color.A to zero
                for (int x = 0; x < width; x += bpp, rowPtr += bpp) *rowPtr = 0;
            });
            bmp.UnlockBits(data);
        }
        private unsafe void SetPixelFast(int x, int y, byte* ptr, int stride, Color color)
        {
            pixel_number++;
            byte* _ptr = ptr + y * stride + x * 4;  // Assuming 32bpp (ARGB format)
            *_ptr = color.B; _ptr++; *_ptr = color.G; _ptr++; *_ptr = color.R; _ptr++; *_ptr = color.A;
        }
        private unsafe void RealSpecial(int i, int j, byte* ptr, int stride, Color _zero, Color _pole, double val, (double, double) mM)
        {
            if (delete_point) return;
            static double convexCombination(double x, double y) => x - size_real * (x - y);
            if (val < convexCombination(mM.Item1, mM.Item2)) SetPixelFast(i, j, ptr, stride, _zero);
            if (val > convexCombination(mM.Item2, mM.Item1)) SetPixelFast(i, j, ptr, stride, _pole);
        }
        private unsafe void ComplexSpecial(int i, int j, byte* ptr, int stride, Color _zero, Color _pole, double val)
        {
            if (delete_point) return;
            if (val < epsilon) SetPixelFast(i, j, ptr, stride, _zero);
            else if (val > 1 / epsilon) SetPixelFast(i, j, ptr, stride, _pole);
        }
        private static void LoopBase(Action<int, int, int, int, int, IntPtr> loop)
        {
            Bitmap bmp = GetBitmap(is_main); BitmapData data = GetBmpData(bmp);
            int xStart = AddOne(borders[0]), yStart = AddOne(borders[2]), xEnd = borders[1], yEnd = borders[3];
            try { for (int i = xStart; i < xEnd; i++) for (int j = yStart; j < yEnd; j++) loop(xStart, yStart, i, j, data.Stride, data.Scan0); }
            finally { bmp.UnlockBits(data); }
        }
        //
        private unsafe void RealLoop(RealMatrix output, Color _zero, Color _pole, Func<double, Color> etr, (double, double) mM)
            => LoopBase((xStart, yStart, i, j, stride, ptr) =>
            {
                double val = output[i - xStart, j - yStart]; var _ptr = (byte*)ptr;
                if (!Double.IsNaN(val))
                {
                    SetPixelFast(i, j, _ptr, stride, etr(val));
                    RealSpecial(i, j, _ptr, stride, _zero, _pole, Math.Atan(val), mM);
                }
            });
        private unsafe void ComplexLoop(ComplexMatrix output, Color _zero, Color _pole, Func<Complex, Color> etr)
            => LoopBase((xStart, yStart, i, j, stride, ptr) =>
            {
                Complex val = output[i - xStart, j - yStart]; var _ptr = (byte*)ptr;
                if (!Double.IsNaN(val.real) && !Double.IsNaN(val.imaginary))
                {
                    SetPixelFast(i, j, _ptr, stride, etr(val));
                    ComplexSpecial(i, j, _ptr, stride, _zero, _pole, Complex.Modulus(val));
                }
            });
        private static Func<double, Color> GetColorReal123(int mode) => val =>
        {
            Color func23(Color c1, Color c2) => val < 0 ? Swap(c1, c2) : (val > 0 ? Swap(c2, c1) : Color.Empty);
            return mode switch
            {
                1 => (Math.Abs(val) < epsilon) ? Swap(Color.Black, Color.White) : Color.Empty,
                2 => func23(Color.White, Color.Black),
                3 => func23(UPPER_GOLD, LOWER_BLUE)
            };
        };
        private static Func<double, Color> GetColorReal45(bool mode, (double, double) mM) => val => mode ?
            ObtainColorStrip(val, mM.Item1, mM.Item2) :
            ObtainColorStrip(val, mM.Item1, mM.Item2, GetShade(LowerRatio(val, stride_real)));
        private static Func<Complex, Color> GetColorComplex123(int mode, bool isReIm) => input =>
        {
            Complex val = isReIm ? input : Complex.Log(input); var (v1, v2) = (val.real, val.imaginary);
            double s1 = isReIm ? stride : mod_stride, s2 = isReIm ? stride : arg_stride;
            var (c1, c2) = mode switch
            {
                1 => (Color.White, Color.Black),
                2 => (Color.Black, Color.White),
                3 => (LOWER_BLUE, UPPER_GOLD)
            };
            bool draw = mode == 1 ? (Math.Min(LowerDistance(v1, s1), LowerDistance(v2, s2)) < epsilon)
                                                      : (LowerIndex(v1, s1) + LowerIndex(v2, s2)) % 2 == 0;
            return mode == 1 ? (draw ? Swap(c2, c1) : Color.Empty) : (draw ? Swap(c1, c2) : Swap(c2, c1));
        };
        private static Func<Complex, Color> GetColorComplex45(bool mode) => mode ? c => ObtainColorWheel(c, alpha: 1) : val =>
        {
            Complex _val = Complex.Log(val);
            double alpha = (LowerRatio(_val.real, mod_stride) + LowerRatio(_val.imaginary, arg_stride)) / 2;
            return ObtainColorWheel(val, GetShade(alpha));
        };
        private void RealLoop123(RealMatrix output, Color _zero, Color _pole, int mode, (double, double) mM)
            => RealLoop(output, _zero, _pole, GetColorReal123(mode), mM);
        private void RealLoop45(RealMatrix output, bool mode, (double, double) mM)
            => RealLoop(output, Color.Black, Color.White, GetColorReal45(mode, mM), mM);
        private void ComplexLoop123(ComplexMatrix output, Color _zero, Color _pole, int mode, bool isReIm)
            => ComplexLoop(output, _zero, _pole, GetColorComplex123(mode, isReIm));
        private void ComplexLoop45(ComplexMatrix output, bool mode)
            => ComplexLoop(output, Color.Black, Color.White, GetColorComplex45(mode));
        #endregion

        #region Rendering
        private void RealComputation()
        {
            Action<RealMatrix, (double, double)> realOperation = color_mode switch
            {
                1 => Real1,
                2 => Real2,
                3 => Real3,
                4 => Real4,
                5 => Real5
            };
            realOperation(output_real, FiniteExtremities(output_real, GetRow(borders), GetColumn(borders)));
        }
        private void Real1(RealMatrix output, (double, double) mM) => RealLoop123(output, ZERO_BLUE, POLE_PURPLE, 1, mM);
        private void Real2(RealMatrix output, (double, double) mM) => RealLoop123(output, ZERO_BLUE, POLE_PURPLE, 2, mM);
        private void Real3(RealMatrix output, (double, double) mM) => RealLoop123(output, Color.Black, Color.White, 3, mM);
        private void Real4(RealMatrix output, (double, double) mM) => RealLoop45(output, true, mM);
        private void Real5(RealMatrix output, (double, double) mM) => RealLoop45(output, false, mM);
        private void ComplexComputation()
        {
            bool isReIm = contour_mode == 1;
            Action<ComplexMatrix> complexOperation = color_mode switch
            {
                1 => isReIm ? Complex1_ReIm : Complex1_ModArg,
                2 => isReIm ? Complex2_ReIm : Complex2_ModArg,
                3 => isReIm ? Complex3_ReIm : Complex3_ModArg,
                4 => Complex4,
                5 => Complex5
            };
            complexOperation(output_complex);
        }
        private void Complex1_ReIm(ComplexMatrix output) => ComplexLoop123(output, ZERO_BLUE, POLE_PURPLE, 1, true);
        private void Complex2_ReIm(ComplexMatrix output) => ComplexLoop123(output, ZERO_BLUE, POLE_PURPLE, 2, true);
        private void Complex3_ReIm(ComplexMatrix output) => ComplexLoop123(output, Color.Black, Color.White, 3, true);
        private void Complex1_ModArg(ComplexMatrix output) => ComplexLoop123(output, ZERO_BLUE, POLE_PURPLE, 1, false);
        private void Complex2_ModArg(ComplexMatrix output) => ComplexLoop123(output, ZERO_BLUE, POLE_PURPLE, 2, false);
        private void Complex3_ModArg(ComplexMatrix output) => ComplexLoop123(output, Color.Black, Color.White, 3, false);
        private void Complex4(ComplexMatrix output) => ComplexLoop45(output, true);
        private void Complex5(ComplexMatrix output) => ComplexLoop45(output, false);
        #endregion

        #region Curves
        private (double, double, double) SetStartEndIncrement(string[] split, bool isPolar, bool isParam)
        {
            (double, double, double) initializeParamPolar(int relPos)
            {
                MyString.ThrowInvalidLengths(split, new int[] { relPos + 2, relPos + 3 });
                return (RealSub.Obtain(split[relPos]), RealSub.Obtain(split[relPos + 1]),
                    split.Length == relPos + 3 ? RealSub.Obtain(split[relPos + 2]) : INCREMENT);
            }
            if (isParam) return initializeParamPolar(3);
            else if (isPolar) return initializeParamPolar(2);
            else
            {
                MyString.ThrowInvalidLengths(split, new int[] { 0, 1, 2, 3, 4 }); double range = Obtain(GeneralInput);
                double getRange(TextBox tbx, bool minus) => GeneralInput_Undo() ? RealSub.Obtain(tbx.Text) : (minus ? -range : range);
                return (split.Length < 3 ? getRange(X_Left, true) : RealSub.Obtain(split[1]),
                    split.Length < 3 ? getRange(X_Right, false) : RealSub.Obtain(split[2]),
                    split.Length == 2 ? RealSub.Obtain(split[1]) : (split.Length == 4 ? RealSub.Obtain(split[3]) : INCREMENT));
            }
        }
        private unsafe static (RealMatrix, RealMatrix, int, bool) SetCurveValues(string[] split, bool isPolar, bool isParam,
            double start, double end, double increment)
        {
            string replace(string s, int index) => s.Replace(split[index], "x");
            string tag1 = ReplaceTags.FUNC_HEAD + ReplaceTags.COS, tag2 = ReplaceTags.FUNC_HEAD + ReplaceTags.SIN,
                input1 = isParam ? replace(split[0], 2) : isPolar ? replace($"({split[0]})*{tag1}({split[1]})", 1) : "x",
                input2 = isParam ? replace(split[1], 2) : isPolar ? replace($"({split[0]})*{tag2}({split[1]})", 1) : split[0];

            int length = (int)((end - start) / increment), _length = length + 2; // For safety
            RealMatrix partition = new(1, _length); double steps = start;
            double obtainCheck(string input) => RealSub.Obtain(input, steps);
            if (is_checking) { _ = obtainCheck(input1); _ = obtainCheck(input2); return (partition, partition, length, true); }

            double* partPtr = partition.Ptr();
            for (int i = 0; i < _length; i++, partPtr++, steps += increment) *partPtr = steps;
            RealMatrix obtain(string input) => new RealSub(input, partition, null, null, null, 1, _length).Obtain();
            return (obtain(input1), obtain(input2), length, false);
        }
        private unsafe void DrawCurve(RealMatrix val1, RealMatrix val2, int length)
        {
            var curveWidth = (float)Math.Min(CURVE_WIDTH * Obtain(ThickInput), CURVE_WIDTH_LIMIT);
            Pen dichoPen(Color c1, Color c2) => new(Swap(c1, c2), curveWidth);
            Pen vividPen = dichoPen(Color.Empty, Color.Empty), defaultPen = dichoPen(Color.Black, Color.White),
                blackPen = dichoPen(Color.White, Color.Black), whitePen = dichoPen(Color.Black, Color.White),
                bluePen = dichoPen(LOWER_BLUE, UPPER_GOLD), yellowPen = dichoPen(UPPER_GOLD, LOWER_BLUE),
                selectedPen = color_mode == 1 ? defaultPen : vividPen;

            Point pos = new(), posBuffer = new(); bool inRange, inRangeBuffer = false; int _ratio, reference = 0;
            double relativeSpeed = Obtain(DenseInput) / length, ratio; double* v1Ptr = val1.Ptr(), v2Ptr = val2.Ptr();

            for (int steps = 0; steps <= length; steps++, v1Ptr++, v2Ptr++)
            {
                (pos.X, pos.Y) = LinearTransform(*v1Ptr, *v2Ptr, borders);
                inRange = *v1Ptr > scopes[0] && *v1Ptr < scopes[1] && *v2Ptr > scopes[3] && *v2Ptr < scopes[2];
                if (inRangeBuffer && inRange)
                {
                    ratio = relativeSpeed * steps % 1;
                    selectedPen = color_mode switch
                    {
                        2 => ratio < 0.5 ? whitePen : blackPen,
                        3 => ratio < 0.5 ? bluePen : yellowPen,
                        _ => selectedPen
                    };
                    if (color_mode > 3) vividPen.Color = ObtainColorWheelCurve(ratio);
                    graphics.DrawLine(selectedPen, posBuffer, pos);

                    SetScrollBars(true); // Necessary for each loop
                    DrawScrollBar(LinearTransform(pos.X, pos.Y, borders));
                    _ratio = Frac(REFRESH, ratio);
                    if (reference != _ratio) DrawReferenceRectangles(selectedPen.Color);
                    reference = _ratio;
                }
                inRangeBuffer = inRange;
                posBuffer = pos;
                segment_number++; // Sensitive position
            }
        }
        //
        private void DisplayFPPBase(string input, bool isPolar = false, bool isParam = false)
        {
            string[] split = MyString.SplitString(input);
            var (start, end, increment) = SetStartEndIncrement(split, isPolar, isParam);
            MyString.ThrowException(start >= end);
            var (val1, val2, length, isChecking) = SetCurveValues(split, isPolar, isParam, start, end, increment);
            if (isChecking) return;
            DisplayBase(() => { DrawCurve(val1, val2, length); pixel_number += segment_number; segment_number = 0; });
        }
        private void DisplayFunction(string input) => DisplayFPPBase(input); // Necessary
        private void DisplayPolar(string input) => DisplayFPPBase(input, isPolar: true);
        private void DisplayParam(string input) => DisplayFPPBase(input, isParam: true);
        #endregion

        #region Graph Display
        private void DisplayBase(Action drawAction)
        {
            DrawBackdropAxesGrids(borders, is_main, freeze_graph);
            graphics.DrawRectangle(_BDR_PEN, GetRect(borders));
            drawAction();
            SetText(PointNumDisplay, pixel_number.ToString());
            if (is_auto) RunExport();
        }
        private void RunDisplayBase(Action computeAction)
        {
            if (is_checking) return; // Necessary
            ClearBitmap(bmp_mac); ClearBitmap(bmp_mic); // Necessary courtesy of ZAL
            computeAction();
            DisplayBase(() => { graphics.DrawImage(GetBitmap(is_main), 0, 0); });
        }
        private void DisplayRendering(string input)
        {
            var (row, column, xCoor, yCoor) = GetRowColumnCoor();
            if (is_complex) output_complex = new ComplexSub(input, xCoor, yCoor, row, column).Obtain();
            else output_real = new RealSub(input, xCoor, yCoor, null, null, row, column).Obtain();
            RunDisplayBase(is_complex ? ComplexComputation : RealComputation);
        }
        private RealMatrix DisplayItLoopReal(string input, RealMatrix x, RealMatrix y, RealMatrix X, int row, int column)
        {
            output_real = new RealSub(input, x, y, X, null, row, column).Obtain();
            RunDisplayBase(RealComputation);
            return output_real; // Presently not used
        }
        private ComplexMatrix DisplayItLoopComplex(string input, ComplexMatrix z, ComplexMatrix Z, int row, int column)
        {
            output_complex = new ComplexSub(input, z, Z, row, column).Obtain();
            RunDisplayBase(ComplexComputation);
            return output_complex;
        }
        private void DisplayIterateLoop(string[] split)
        {
            var (row, column, x, y) = GetRowColumnCoor();
            int int3 = RealSub.ToInt(split[3]), int4 = Math.Max(int3, RealSub.ToInt(split[4])); // Necessary
            string replaceLoop(int times) => MyString.ReplaceLoop(split, 0, 2, times);
            if (is_complex)
            {
                ComplexMatrix _initial = ComplexSub.InitilizeZ(x, y, row, column);
                ComplexMatrix _inherit = new ComplexSub(split[1], _initial, null, row, column).Obtain();
                MyString.ThrowInvalidLengths(split, new int[] { 5 });
                MyString.For(int3, is_checking ? int3 : int4, times =>
                { _inherit = DisplayItLoopComplex(replaceLoop(times), _initial, _inherit, row, column); });
            }
            else // The logic is slightly different between real and complex
            {
                RealMatrix _inherit = new RealSub(split[1], x, y, null, null, row, column).Obtain();
                MyString.ThrowInvalidLengths(split, new int[] { 6 });
                MyString.For(int3, is_checking ? int3 : int4, times =>
                {
                    _inherit = new RealSub(replaceLoop(times), x, y, _inherit, null, row, column).Obtain();
                    DisplayItLoopReal(split[5], x, y, _inherit, row, column);
                });
            }
        }
        private void DisplayLoop(string input)
        {
            input = ReplaceTags.ReplaceCurves(input); string[] split = MyString.SplitString(input); // Do not merge into one
            bool containsTag(string s) => input.Contains(String.Concat(ReplaceTags.FUNC_HEAD, s, ReplaceTags.UNDERLINE, '('));
            if (containsTag(ReplaceTags.ITLOOP)) { DisplayIterateLoop(split); return; }

            Action<string> displayMethod =
                containsTag(ReplaceTags.FUNC) ? DisplayFunction :
                containsTag(ReplaceTags.POLAR) ? DisplayPolar :
                containsTag(ReplaceTags.PARAM) ? DisplayParam : DisplayRendering;

            int int2 = RealSub.ToInt(split[2]), int3 = Math.Max(int2, RealSub.ToInt(split[3])); // Necessary
            MyString.For(int2, int3, times => { displayMethod(MyString.ReplaceLoop(split, 0, 1, times)); });
        }
        private void DisplayOnScreen()
        {
            if (NoInput()) return; // Necessary
            string[] split = MyString.SplitByChars(RecoverMultiply.Beautify(InputString.Text, is_complex), "|");
            for (int loops = 0; loops < split.Length; loops++)
            {
                bool containsTags(string s1, string s2) => MyString.ContainsAny(split[loops], new string[] { s1, s2 });
                Action<string> displayMethod =    // Should not pull outside of the loop
                    containsTags(MyString.LOOP_NAMES[0], MyString.LOOP_NAMES[1]) ? DisplayLoop :
                    containsTags(MyString.FUNC_NAMES[0], MyString.FUNC_NAMES[1]) ? DisplayFunction :
                    containsTags(MyString.FUNC_NAMES[2], MyString.FUNC_NAMES[3]) ? DisplayPolar :
                    containsTags(MyString.FUNC_NAMES[4], MyString.FUNC_NAMES[5]) ? DisplayParam : DisplayRendering;
                displayMethod(split[loops]);
            }
        }
        #endregion

        #region Color Extractors
        private static Color ObtainColorBase(double argument, double alpha, int decay) // alpha: brightness
        {
            if (IllegalRatio(alpha)) return Color.Empty; // Necessary
            double temp = argument * 3 / Math.PI; int proportion, region = argument < 0 ? -1 : (int)temp;
            if (region == 6) region = proportion = 0; else proportion = Frac(255, temp - region);

            Color getArgb(int r, int g, int b) => Argb(decay, r, g, b);
            return region switch
            {
                0 => getArgb(Frac(255, alpha), Frac(proportion, alpha), 0),
                1 => getArgb(Frac(255 - proportion, alpha), Frac(255, alpha), 0),
                2 => getArgb(0, Frac(255, alpha), Frac(proportion, alpha)),
                3 => getArgb(0, Frac(255 - proportion, alpha), Frac(255, alpha)),
                4 => getArgb(Frac(proportion, alpha), 0, Frac(255, alpha)),
                5 => getArgb(Frac(255, alpha), 0, Frac(255 - proportion, alpha)),
                _ => Color.Empty
            }; // The ARGB hexagon for standard domain coloring
        }
        private static Color ObtainColorWheel(Complex c, double alpha = 1)
            => ObtainColorBase(ArgRGB(c.real, c.imaginary), alpha, (int)(255 / (1 + decay * Complex.Modulus(shade ? c : Complex.ZERO))));
        private static Color ObtainColorWheelCurve(double alpha) => ObtainColorBase(alpha * Math.Tau, 1, 255);
        private static Color ObtainColorStrip(double d, double min, double max, double alpha = 1) // alpha: brightness
        {
            if (min == max) return Color.Empty; // Necessary
            double beta = (Math.Atan(d) - min) / (max - min);
            if (IllegalRatio(alpha) || IllegalRatio(beta)) return Color.Empty; // Necessary
            return beta < 0.5 ? Argb(Frac(Frac(510, beta), alpha), 0, Frac(255, alpha))
                                           : Argb(Frac(255, alpha), 0, Frac(255 - Frac(510, beta - 0.5), alpha));
        }
        #endregion

        // 3.INTERACTIONS
        #region Mouse Move & Mouse Down
        private void Graph_MouseMove(object sender, MouseEventArgs e)
        {
            if (!ActivateMoveDown()) return;
            CheckMoveDown(b => RunMouse(e, b, RunMouseMove, () =>
            { Cursor = Cursors.Default; DrawReferenceRectangles(SystemColors.ControlDark); SetScrollBars(false); }));
        }
        private void Graph_MouseDown(object sender, MouseEventArgs e)
        {
            if (!ActivateMoveDown()) return;
            CheckMoveDown(b => RunMouse(e, b, RunMouseDown, null));
        }
        private void RunMouseMove(MouseEventArgs e, int[] borders)
        {
            Cursor = Cursors.Cross;
            Graphics.FromImage(BMP_PIXEL).CopyFromScreen(Cursor.Position, Point.Empty, SIZE_PIXEL);
            DrawReferenceRectangles(BMP_PIXEL.GetPixel(0, 0));
            SetScrollBars(true);
            HandleMouseAction(e, borders, v => { DrawScrollBar(v); DisplayMouseMove(e, v.Item1, v.Item2); });
        }
        private void RunMouseDown(MouseEventArgs e, int[] borders)
        {
            chosen_number++;
            HandleMouseAction(e, borders, v => { DisplayMouseDown(e, v.Item1, v.Item2); });
        }
        //
        private bool ActivateMoveDown() => activate_mouse && !error_input && !is_checking && !NoInput();
        private static void RunMouse(MouseEventArgs e, int[] b, Action<MouseEventArgs, int[]> action, Action? _action)
        { if (e.X > b[0] && e.X < b[1] && e.Y > b[2] && e.Y < b[3]) action(e, b); else _action?.Invoke(); }
        private static void CheckMoveDown(Action<int[]> checkMouse) => checkMouse(GetBorders(is_main ? 1 : 2));
        private static void HandleMouseAction(MouseEventArgs e, int[] borders, Action<(double, double)> actionHandler)
            => actionHandler(LinearTransform(e.X, e.Y, borders));
        private void DisplayMouseMove(MouseEventArgs e, double xCoor, double yCoor)
        {
            static string trimForMove(double input) => MyString.TrimLargeDouble(input, 1000000);
            SetText(X_CoorDisplay, trimForMove(xCoor)); SetText(Y_CoorDisplay, trimForMove(yCoor));
            SetText(ModulusDisplay, trimForMove(Complex.Modulus(xCoor, yCoor)));
            SetText(AngleDisplay, MyString.GetAngle(xCoor, yCoor));

            if (!MyString.ContainsFuncName(InputString.Text))
            {
                if (is_complex)
                {
                    Complex c = output_complex[e.X - AddOne(borders[0]), e.Y - AddOne(borders[2])];
                    SetText(FunctionDisplay, $"[Re] {c.real}\r\n[Im] {c.imaginary}");
                }
                else SetText(FunctionDisplay, output_real[e.X - AddOne(borders[0]), e.Y - AddOne(borders[2])].ToString());
            }
            else SetText(FunctionDisplay, DISPLAY_ERROR);
        }
        private void DisplayMouseDown(MouseEventArgs e, double xCoor, double yCoor)
        {
            static string trimForDown(double input) => MyString.TrimLargeDouble(input, 100);
            string _xCoor = trimForDown(xCoor), _yCoor = trimForDown(yCoor),
                Modulus = trimForDown(Complex.Modulus(xCoor, yCoor)), Angle = MyString.GetAngle(xCoor, yCoor);

            string message = String.Empty;
            if (!MyString.ContainsFuncName(InputString.Text))
            {
                message += "\r\n\r\n";
                int x = e.X - AddOne(borders[0]), y = e.Y - AddOne(borders[2]);
                if (is_complex)
                {
                    Complex c = output_complex[x, y];
                    message += $"Re = {trimForDown(c.real)}\r\nIm = {trimForDown(c.imaginary)}";
                }
                else message += $"f(x, y) = {trimForDown(output_real[x, y])}";
            }
            AddDraft($"\r\n{SEP_1} Point {chosen_number} of No.{loop_number} {SEP_2}\r\n" +
                $"\r\nx = {_xCoor}\r\ny = {_yCoor}\r\n" + $"\r\nmod = {Modulus}\r\narg = {Angle}{message}\r\n");
        }
        #endregion

        #region Graphing Buttons
        private async void ConfirmButton_Click(object sender, EventArgs e) => await Async(() => RunConfirm_Click(sender, e));
        private async void PreviewButton_Click(object sender, EventArgs e) => await Async(() => RunPreview_Click(sender, e));
        private async void AllButton_Click(object sender, EventArgs e) => await Async(() =>
        {
            RunPreview_Click(sender, e);
            if (error_input) return; // To prevent the emergence of the second error box
            Invoke((MethodInvoker)(() => { StopTimers(); Thread.Sleep(SLEEP); StartTimers(); })); // Done on the UI Thread
            RunConfirm_Click(sender, e);
        });
        private void RunClick(object sender, EventArgs e, int[] borders, bool isMain, Action endAction)
        {
            try
            {
                Graph_DoubleClick(sender, e);
                SetTextboxButtonReadOnly(true);

                pixel_number = segment_number = export_number = 0;
                error_input = error_address = is_checking = false;
                clicked = true; loop_number++;

                PrepareSetDisplay(borders, isMain);
                endAction();
            }
            catch (Exception) { InputErrorBox(sender, e, WRONG_FORMAT); }
            finally
            {
                if (error_input) StopTimers();
                SetTextboxButtonReadOnly(false); // Necessary to revive the controls after errors
                SetScrollBars(false);
                PictureWait.Visible = false;
                GC.Collect(); // Releasing the unused memory, particularly those used for parenthesis splitting
            }
        }
        private void RunConfirm_Click(object sender, EventArgs e) => RunClick(sender, e, GetBorders(1), true, () => Ending(MACRO));
        private void RunPreview_Click(object sender, EventArgs e) => RunClick(sender, e, GetBorders(2), false, () => Ending(MICRO));
        //
        private async Task Async(Action runClick)
        {
            BlockInput(true);
            if (NoInput()) return;
            Clipboard.SetText(InputString.Text); // Invoked if !NoInput()
            StartTimers();
            await Task.Run(() => { Thread.CurrentThread.Priority = ThreadPriority.Highest; runClick(); });
            BlockInput(false);
        }
        private void StartTimers()
        {
            display_elapsed = 0;
            SetText(TimeDisplay, "0s");
            is_flashing = false; // Ensuring deferred emergence of the hourglass

            DisplayTimer.Start(); WaitTimer.Start(); GraphTimer.Start();
            TimeNow = DateTime.Now;
        }
        private static void StopTimers()
        {
            DisplayTimer.Stop(); WaitTimer.Stop(); GraphTimer.Stop();
            TimeCount = DateTime.Now - TimeNow;
        }
        private void SetTextboxButtonReadOnly(bool readOnly)
        {
            TextBox[] textBoxes = { InputString, GeneralInput, X_Left, X_Right, Y_Left, Y_Right, ThickInput, DenseInput, AddressInput };
            foreach (var tbx in textBoxes) tbx.ReadOnly = readOnly;
            Button[] buttons = { ConfirmButton, PreviewButton, AllButton };
            foreach (var btn in buttons) btn.Enabled = !readOnly;
            activate_mouse = !readOnly;
        }
        private void PrepareSetDisplay(int[] borders, bool isMain)
        {
            (x_left, x_right, y_up, y_down, is_main) = (borders[0], borders[1], borders[2], borders[3], isMain);
            SetThicknessDensenessScopesBorders();
            DisplayOnScreen();
        }
        private void Ending(string mode)
        {
            StopTimers();
            if (is_main) SetText(CaptionBox, $"{InputString.Text}\r\n" + CaptionBox.Text);

            SetText(TimeDisplay, $"{TimeCount:hh\\:mm\\:ss\\.fff}");
            AddDraft($"\r\n{SEP} No.{loop_number} [{mode}] {SEP}\r\n" + $"\r\n{InputString.Text}\r\n" +
                $"\r\nPixels: {PointNumDisplay.Text}\r\nDuration: {TimeDisplay.Text}\r\n");

            InputString_Focus();
            if (is_auto && !error_address) RunStore();
        }
        #endregion

        #region Export & Storage Buttons
        private void ExportButton_Click(object sender, EventArgs e) { Graph_DoubleClick(sender, e); RunExport(); }
        private void StoreButton_Click(object sender, EventArgs e) { Graph_DoubleClick(sender, e); RunStore(); }
        private void RunExport() => HandleExportStore(ExportGraph, REMIND_EXPORT);
        private void RunStore() => HandleExportStore(StoreHistory, REMIND_STORE);
        //
        private void HandleExportStore(Action exportStoreHandler, string prefix)
        {
            try
            {
                FillEmpty(AddressInput, ADDRESS_DEFAULT);
                exportStoreHandler();
                AddDraft($"\r\n{prefix}\r\n{DateTime.Now:HH_mm_ss}\r\n");
            }
            catch (Exception) { error_address = true; GetExportStoreErrorBox(); }
        }
        private string GetFileName(string suffix)
        {
            DateTime Date = DateTime.Now;
            return $@"{AddressInput.Text}\{Date:yyyy}_{Date.DayOfYear}_{Date:HH_mm_ss}_{suffix}";
        }
        private void ExportGraph()
        {
            export_number++;
            Graphics.FromImage(bmp_screen).CopyFromScreen(Left + LEFT_SUPP, Top + TOP_SUPP, 0, 0, bmp_screen.Size);
            bmp_screen.Save(GetFileName($"No.{export_number}.png"));
        }
        private void StoreHistory()
        {
            using StreamWriter writer = new(GetFileName($"{STOCKPILE}.txt")); // "using" should not be removed
            writer.Write(DraftBox.Text);
        }
        #endregion

        #region Checking Core & Shortcuts
        private void InputErrorBox(object sender, EventArgs e, string message)
        {
            error_input = true;
            bool temp = ProcessingGraphics();
            InputString.ReadOnly = false; CheckAll(sender, e); InputString.ReadOnly = temp; // Sensitive
            GetInputErrorBox(message);
        }
        private void CheckValidityCore(Action errorHandler)
        {
            try
            {
                is_checking = true;
                PrepareSetDisplay(GetBorders(3), false);

                bool noInput = NoInput(); // Should not return immediately if NoInput()
                InputLabel.ForeColor = noInput ? Color.White : CORRECT_GREEN;
                InputString.BackColor = noInput ? FOCUS_GRAY : CORRECT_GREEN;
                PictureCorrect.Visible = !noInput; PictureIncorrect.Visible = false;
            }
            catch (Exception) { errorHandler(); }
        }
        private void CheckAll(object sender, EventArgs e)
        {
            Action<object, EventArgs>[] checkActions =
            {
                GeneralInput_DoubleClick,
                Details_TextChanged, // Sensitive position
                InputString_DoubleClick,
                ThickInput_DoubleClick,
                DenseInput_DoubleClick,
                AddressInput_DoubleClick
            };
            foreach (var action in checkActions) action(sender, e);
        }
        //
        private void Graph_KeyUp(object sender, KeyEventArgs e)
        {
            HandleModifierKeys(e, false);
            if (suppress_key_up) return; // Should not merge with the next line
            if (HandleSpecialKeys(e)) return;
            HandleCtrlCombination(sender, e);
        }
        private void Graph_KeyDown(object sender, KeyEventArgs e)
        {
            HandleModifierKeys(e, true);
            if (!NoInput() && !ProcessingGraphics() && sft_pressed && e.KeyCode == Keys.Back)
                ExecuteSuppress(() =>
                {
                    AddDraft("\r\nDeleted: " + InputString.Text + "\r\n");
                    SetText(InputString, String.Empty);
                    InputString.Focus();
                }, e);
            else if (e.KeyCode == Keys.Delete) ExecuteSuppress(null, e); // Banning the original deletion
        }

        private static void HandleModifierKeys(KeyEventArgs e, bool isKeyDown)
        {
            if (e.KeyCode == Keys.ControlKey) { ctrl_pressed = isKeyDown; e.Handled = true; }
            else if (e.KeyCode == Keys.ShiftKey) { sft_pressed = isKeyDown; e.Handled = true; }
        }
        private bool HandleSpecialKeys(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape: ExecuteSuppress(Close, e); return true;
                case Keys.Oemtilde: ExecuteSuppress(() => PicturePlay_Click(null, e), e); return true;
                case Keys.Delete: ExecuteSuppress(() => { Graph_DoubleClick(null, e); Delete_Click(e); }, e); return true;
                default: return false;
            }
        }
        private void HandleCtrlCombination(object sender, KeyEventArgs e)
        {
            if (!ctrl_pressed) return;
            void restoreDefault(object sender, KeyEventArgs e)
            {
                RecoverInput(); ComboColoring.SelectedIndex = 4; ComboContour.SelectedIndex = 1;

                CheckBox[] checkFalse = { CheckAuto, CheckSwap, CheckPoints, CheckShade, CheckRetain };
                foreach (var cbx in checkFalse) cbx.Checked = false;
                CheckBox[] checkTrue = { CheckEdit, CheckComplex, CheckCoor };
                foreach (var cbx in checkTrue) cbx.Checked = true;
            }
            Action? shortcutHandler = e.KeyCode switch
            {
                Keys.K => () => StoreButton_Click(sender, e),
                Keys.S => () => ExportButton_Click(sender, e),
                Keys.R => () => Graph_DoubleClick(sender, e),
                Keys.D3 => () => ClearButton_Click(sender, e),
                Keys.D2 => () => PictureLogo_DoubleClick(sender, e),
                Keys.OemQuestion => () => TitleLabel_DoubleClick(sender, e),
                Keys.D when !ProcessingGraphics() => () => restoreDefault(sender, e),
                Keys.B when !ProcessingGraphics() => () => AllButton_Click(sender, e),
                Keys.P when !ProcessingGraphics() => () => PreviewButton_Click(sender, e),
                Keys.G when !ProcessingGraphics() => () => ConfirmButton_Click(sender, e),
                Keys.C when !ProcessingGraphics() && sft_pressed => () => CheckAll(sender, e),
                _ => null
            };
            if (shortcutHandler != null) ExecuteSuppress(shortcutHandler, e);
        }
        private static void ExecuteSuppress(Action? action, KeyEventArgs e)
        {
            suppress_key_up = true;
            action?.Invoke();
            e.Handled = e.SuppressKeyPress = true;
            suppress_key_up = false;
        }
        #endregion

        #region Dialogs
        private static void GetMusicClickErrorBox(string message)
            => MessageBox.Show($"Error: Could not find embedded resource for {message}.");
        private static void ShowBoxBase(Action<string, int, int> showMessage, string heading, string[] contents, int feed)
        {
            string content = heading, feeder = String.Concat(Enumerable.Repeat("\r\n", feed));
            for (int i = 0; i < contents.Length; i++) content += $"{feeder}{i + 1}. {contents[i]}";
            showMessage(content + "\r\n", 450, 300);
        }
        private static void ShowErrorBox(string message, string[] contents)
            => ShowBoxBase(MyMessageBox.ShowException, message + MISTAKES_HEAD + "\r\n", contents, 1);
        private static void GetInputErrorBox(string message) => ShowErrorBox(message, new string[]
        {
            "Misspelling of function/variable names.",
            "Incorrect grammar of special functions.",
            "Excess or deficiency of characters.",
            "Real/Complex mode confusion.",
            "Invalid other parameters."
        });
        private static void GetExportStoreErrorBox() => ShowErrorBox(WRONG_ADDRESS, new string[]
        {
            "Files not created beforehand.",
            "The address ending with \\.",
            "The address quoted automatically.",
            "The file storage being full."
        });
        //
        private static string GetComment(string input) => $"# {input}";
        private static string GetManual()
        {
            string content = $"DESIGNER: Fraljimetry\r\nDATE: {DATE}\r\nLOCATION: Xi'an, China";
            content += "\r\n\r\nThis software was developed in Visual Studio 2022, written in C#, " +
                "to visualize real/complex functions and equations with no more than two variables." +
                "\r\n\r\nTo bolster artistry and practicality, numerous modes are rendered, " +
                "making it possible to generate images tailored for users of various ends." +
                "\r\n\r\nNote: I wish the definitions of these operations are self-evident if you try yourself or refer to the examples.";

            static string subTitleContent(string subtitle, string cont) => $"\r\n\r\n{_SEP} {subtitle} {_SEP}" + cont;
            content += subTitleContent("ELEMENTS",
                "\r\n\r\n+ - * / ^ ( )" +
                "\r\n\r\nSin, Cos, Tan, Sinh, Cosh, Tanh," +
                "\r\nArcsin & Asin, Arccos & Acos, Arctan & Atan," +
                "\r\nArsinh & Asinh, Arcosh & Acosh, Artanh & Atanh," +
                "\r\n\r\nLog & Ln, Exp, Sqrt, Abs (f(x,y) & f(z))" +
                $"\r\n\r\nConjugate & Conj (f(z)), e(f(z)){TAB}{GetComment("e(z) := exp (2*pi*i*z).")}");
            content += subTitleContent("COMBINATORICS",
                "\r\n\r\nFloor, Ceil, Round, Sign & Sgn (double a)" +
                "\r\n\r\nMod (double a, double n), nCr, nPr (int n, int r)" +
                "\r\n\r\nMax, Min (double a, double b, ...), Factorial & Fact (int n)");
            content += subTitleContent("SPECIALTIES",
                $"\r\n\r\n{GetComment("D&C := double & Complex.")}" +
                "\r\n\r\nF (D&C a, D&C b, D&C c, f(x,y) & f(z)) & " +
                "\r\nF (D&C a, D&C b, D&C c, f(x,y) & f(z), int n)" +
                $"\r\n{GetComment("HyperGeometric Series (case-sensitive).")}" +
                "\r\n\r\nGamma & Ga (f(x,y) & f(z)) & " +
                "\r\nGamma & Ga (f(x,y) & f(z), int n)" +
                "\r\n\r\nBeta (f(x,y) & f(z), g(x,y) & g(z)) & " +
                "\r\nBeta (f(x,y) & f(z), g(x,y) & g(z), int n)" +
                "\r\n\r\nZeta (f(x,y) & f(z)) & " +
                $"\r\nZeta (f(x,y) & f(z), int n){TAB}{GetComment("This is a mess for n too large.")}");
            content += subTitleContent("REPETITIONS",
                $"\r\n\r\n{GetComment("Capitalizations represent substitutions of variables.")}" +
                "\r\n\r\nSum (f(x,y,k) & f(z,k), k, int a, int b)" +
                "\r\nProduct & Prod (f(x,y,k) & f(z,k), k, int a, int b)" +
                "\r\n\r\nIterate1 (f(x,y,X,k), g(x,y), k, int a, int b)" +
                "\r\nIterate2 (f1(x,y,X,Y,k), f2(...), g1(x,y), g2(...), k, int a, int b, 1&2)" +
                "\r\nIterate (f(z,Z,k), g(z), k, int a, int b)" +
                $"\r\n{GetComment("g: initial values; f: iterations.")}" +
                "\r\n\r\nComposite1 & Comp1(f(x,y), g1(x,y,X), ... , gn(x,y,X))" +
                $"\r\nComposite2 & Comp2\r\n{TAB}(f1(x,y), f2(...), g1(x,y,X,Y), h1(...), ... , gn(...), hn(...), 1&2)" +
                "\r\nComposite & Comp (f(z), g1(z,Z), ... , gn(z,Z))" +
                $"\r\n{GetComment("f: initial values; g: compositions.")}");
            content += subTitleContent("PLANAR CURVES",
                "\r\n\r\nFunc (f(x)) & " +
                "\r\nFunc (f(x), double increment) & " +
                "\r\nFunc (f(x), double a, double b) & " +
                "\r\nFunc (f(x), double a, double b, double increment)" +
                "\r\n\r\nPolar (f(θ), θ, double a, double b) & " +
                "\r\nPolar (f(θ), θ, double a, double b, double increment)" +
                "\r\n\r\nParam (f(u), g(u), u, double a, double b) & " +
                "\r\nParam (f(u), g(u), u, double a, double b, double increment)");
            content += subTitleContent("RECURSIONS",
                $"\r\n\r\n{GetComment("These methods should be combined with all above.")}" +
                "\r\n\r\nLoop (Input(k), k, int a, int b)" +
                "\r\n\r\nIterateLoop (f(x,y,X,k), g(x,y), k, int a, int b, h(x,y,X)) & " +
                "\r\nIterateLoop (f(z,Z,k), g(z), k, int a, int b)" +
                $"\r\n{GetComment("Displaying each roll of iteration.")}" +
                $"\r\n\r\n... | ... | ...{TAB}{GetComment("Displaying one by one.")}");
            content += subTitleContent("CONSTANTS", "\r\n\r\npi & p, e, gamma & ga & g, i");
            content += subTitleContent("SHORTCUTS", "\r\n");

            static string getShortcuts(string key, int blank, string meaning) => $"\r\n[{key}]" + new string('\t', blank) + meaning + ";";
            content += getShortcuts("Control + P", 2, "Graph in the MicroBox");
            content += getShortcuts("Control + G", 2, "Graph in the MacroBox");
            content += getShortcuts("Control + B", 2, "Graph in both regions");
            content += getShortcuts("Control + S", 2, "Save as a snapshot");
            content += getShortcuts("Control + K", 2, "Save the history as a .txt");
            content += getShortcuts("Control + Shift + C", 1, "Check all inputs");
            content += getShortcuts("Control + R", 2, "Erase all checks");
            content += getShortcuts("Control + D", 2, "Restore to default");
            content += getShortcuts("Shift + Back", 2, "Clear the InputBox");
            content += getShortcuts("Control + D2", 2, "View Fralji's profile");
            content += getShortcuts("Control + D3", 2, "Clear all ReadOnly controls");
            content += getShortcuts("Control + OemQuestion", 1, "See the manual");
            content += getShortcuts("Oemtilde", 2, "Play/pause the music");
            content += getShortcuts("Delete", 3, "Clear both regions");
            content += getShortcuts("Escape", 3, "Close Fraljiculator");
            return content + "\r\n\r\nClick [Tab] to witness the process of control design.";
        }
        private static string AddContact(string platform, string account, string note)
            => $"\r\n\r\n{platform}: {account}" + (note != String.Empty ? (new string(' ', 4) + GetComment(note)) : note);
        private static string GetProfile()
        {
            string content = "Dear math lovers & mathematicians:" +
                "\r\n\r\nHi! I'm Fralji, a content creator on Bilibili since July, 2021, right before entering college." +
                "\r\n\r\nI aim to deliver unique lectures on many branches of mathematics. " +
                "If you have any problem on the usage of this application, or anything concerning math, please reach to me via:";
            content += AddContact("Bilibili", "355884223", String.Empty);
            content += AddContact("Email", "frankjiiiiiiii@gmail.com", String.Empty);
            content += AddContact("Wechat", "F1r4a2n8k5y7", "recommended");
            content += AddContact("QQ", "472955101", String.Empty);
            content += AddContact("Facebook", "Fraljimetry", String.Empty);
            content += AddContact("Instagram", "shaodaji", "NOT recommended");
            return content + "\r\n\r\n" + new string(' ', 72) + $"{DATE}";
        }
        private void TitleLabel_DoubleClick(object sender, EventArgs e) => MyMessageBox.ShowFormal(GetManual(), 640, 480);
        private void PictureLogo_DoubleClick(object sender, EventArgs e) => MyMessageBox.ShowFormal(GetProfile(), 600, 450);
        private static void ShowCustomBox(string title, string[] contents)
            => ShowBoxBase(MyMessageBox.ShowCustom, $"[{title}]" + new string(' ', 12) + $"{DATE}", contents, 2);
        //
        private void InputLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("FORMULA INPUT", new string[]
        {
            "Space and enter keys are both OK. Unaccepted keys are banned, removed if pasted from the clipboard.",
            "Excessive ellipses of multiplication may result in ambiguity. Ex. \"gammax\" will produce a \"max\"."
        });
        private void AtLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("SAVING ADDRESS", new string[]
        {
            "Create a file for snapshot storage and paste the address here. It will be checked.",
            "PNG snapshots & history lists will be named in the respective formats: " +
            "\"yyyy_ddd_hh_mm_ss_No.#\" and \"yyyy_ddd_hh_mm_ss_stockpile\"."
        });
        private void GeneralLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("GENERAL SCOPE", new string[]
        {
            "The detailed scope effectuates only if the general scope is set to \"0\".",
            "Any legitimate variable-free algebraic expressions are acceptable, checked as in the input box."
        });
        private void DetailLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("DETAILED SCOPE", new string[]
        {
            "Reversing the endpoints to create the mirror effect is NOT supported.",
            "Any legitimate variable-free algebraic expressions are acceptable, checked as in the input box."
        });
        private void ThickLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("MAGNITUDE", new string[]
        {
            "Representing: (i) Width of planar curves, (ii) Size of special points, (iii) Decay rates of translucence.",
            "It should be appropriate according to the scale. Examples have been tweaked with much effort."
        });
        private void DenseLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("DENSITY", new string[]
        {
            "The density refers to:\r\n(i) Density of contours (real & complex),\r\n(ii) Relative speed of planar curves.",
            "It should be appropriate according to the scale. Examples have been tweaked with much effort."
        });
        private void DraftLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("HISTORY LIST", new string[]
        {
            "The input will be saved both in this box and in the clipboard.",
            "Clicked points, along with the time of snapshots & history storage, will also be recorded in detail."
        });

        private void ExampleLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("EXAMPLES", new string[]
        {
            "These examples serve to inform you of the multifarious legitimate grammar.",
            "Some renderings are elegant while others are chaotic. Elegance take time to explore and appreciate. Enjoy yourself!"
        });
        private void FunctionLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("FUNCTIONS", new string[]
        {
            "The two combo boxes contain regular and special operations respectively, the latter having complicated grammar.",
            "Select something in the input box and choose here to substitute your selection."
        });
        private void ModeLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("COLORING MODES", new string[]
        {
            "The spectrum of colors represents:\r\n(i) Arguments of meromorphic functions," +
            "\r\n(ii) Values of two-variable functions,\r\n(iii) Parameterizations of planar curves.",
            "The first three modes have swappable colorations, while the last two do not."
        });
        private void ContourLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("CONTOUR MODES", new string[]
        {
            "Both options apply to the complex version ONLY, for the contouring of meromorphic functions.",
            "Only the Polar option admits translucent display, representing the decay rate of modulus."
        });

        private void PointNumLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("PIXELS", new string[]
        {
            "Logging the number of points / line segments in the previous loop, almost proportional to time and iteration.",
            "Nullity often results from constancy, divergence, or undefinedness."
        });
        private void TimeLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("DURATION", new string[]
        {
            "The auto snapshot cannot capture updates here on time, but it will be saved in the history list along with the pixels.",
            "This value is a precious embodiment of optimization, refered for appropriate iterations and others."
        });
        private void PreviewLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("MICROCOSM", new string[]
        {
            "Since graphing cannot pause manually during the process, a preview of results is necessary for time estimation.",
            "It differs from the main graph only in sharpness. Graphing here is around 20 times faster (less after optimization)."
        });
        #endregion

        #region Index Change & Check Change
        private void SetValuesForSelectedIndex(int index)
        {
            int _color = 3; string _general = "1.1", _thick = THICK_DEFAULT, _dense = DENSE_DEFAULT;
            bool _points = false, _retain = false, _shade = false;
            int complexL = ReplaceTags.EX_COMPLEX.Length, realL = ReplaceTags.EX_REAL.Length,
                curveL = ReplaceTags.EX_CURVES.Length;

            InputString.ReadOnly = true; // Necessary
            void setDetails(string xL, string xR, string yL, string yR)
            { SetText(X_Left, xL); SetText(X_Right, xR); SetText(Y_Left, yL); SetText(Y_Right, yR); }
            if (index < complexL)
                switch (index)
                {
                    case 0: _color = 4; _points = true; break;
                    case 1: _general = "1.2"; break;
                    case 2: _color = 2; _points = true; break;
                    case 3: _general = "pi/2"; _color = 4; break;
                    case 4: _general = "4"; _thick = "0.1"; _shade = true; break;
                    case 5: _general = "3"; break;
                    case 6: _general = "0"; setDetails("-1.6", "0.6", "-1.1", "1.1"); break;
                    case 7: _general = "2"; _color = 4; _shade = true; break;
                }
            else if (index > complexL && index < complexL + realL + 1)
                switch (index - complexL - 1)
                {
                    case 0: _general = "4pi"; _thick = "0.1"; _color = 2; _points = true; break;
                    case 1: _general = "2pi"; _color = 4; break;
                    case 2: _general = "2.5"; _thick = "0.3"; _color = 1; _points = true; break;
                    case 3: _general = "0"; setDetails("0", "1", "0", "1"); _thick = "0.2"; _color = 0; _retain = true; break;
                    case 4: _general = "10"; _thick = "0.1"; _color = 1; _points = true; break;
                    case 5: _general = "5"; _color = 1; break;
                    case 6: _general = "3"; break;
                    case 7: _general = "4"; _thick = "0.05"; _color = 2; _points = true; break;
                }
            else if (index > complexL + realL + 1 && index < complexL + realL + curveL + 2)
                switch (index - complexL - realL - 2)
                {
                    case 0: _general = "5.5"; break;
                    case 1: _general = "pi"; _thick = "0.5"; _dense = "10"; _color = 2; break;
                    case 2: _general = "3"; _color = 0; break;
                    case 3: _dense = "100"; _color = 1; break;
                    case 4: _thick = "0.5"; break;
                    case 5: _thick = "0.5"; _dense = "10"; _retain = true; break;
                    case 6: _thick = "0.5"; break;
                    case 7: _general = "0"; setDetails("-0.2", "1.2", "-0.2", "1.2"); _thick = "0.5"; _color = 0; _retain = true; break;
                }
            else ComboExamples_Undo();
            InputString.ReadOnly = false; // Necessary

            SetText(GeneralInput, _general); SetText(ThickInput, _thick); SetText(DenseInput, _dense);
            CheckPoints.Checked = _points; CheckRetain.Checked = _retain; CheckShade.Checked = _shade;
            CheckComplex.Checked = ComboExamples.SelectedIndex < complexL;
            ComboColoring.SelectedIndex = _color;
        }
        private void Combo_SelectionChanged(string selectedItem)
        {
            if (ProcessingGraphics()) return;
            SetText(InputString, MyString.Replace(InputString.Text, selectedItem, InputString.SelectionStart,
                InputString.SelectionStart + InputString.SelectionLength - 1));
            InputString.Focus(); InputString.SelectionStart--; // Should not place before .Focus()
        }

        private void ComboExamples_SelectedIndexChanged(object sender, EventArgs e)
        {
            string? selection = ComboExamples.SelectedItem?.ToString();
            if (ProcessingGraphics() || String.IsNullOrEmpty(selection) || ComboExamples.SelectedIndex == -1) return;
            SetText(InputString, selection);
            SetValuesForSelectedIndex(ComboExamples.SelectedIndex);
            ComboExamples_Undo(); // To prevent repetitious call
            Delete_Click(e);
            InputString_Focus();
        }
        private void ComboFunctions_SelectedIndexChanged(object sender, EventArgs e)
            => Combo_SelectionChanged(ComboFunctions.SelectedItem.ToString());
        private void ComboSpecial_SelectedIndexChanged(object sender, EventArgs e)
            => Combo_SelectionChanged(ComboSpecial.SelectedItem.ToString());
        private void ComboColoring_SelectedIndexChanged(object sender, EventArgs e)
            => color_mode = AddOne(ComboColoring.SelectedIndex);
        private void ComboContour_SelectedIndexChanged(object sender, EventArgs e)
            => contour_mode = AddOne(ComboContour.SelectedIndex);
        //
        private void CheckComplex_CheckedChanged(object sender, EventArgs e) => ReverseBool(ref is_complex);
        private void CheckSwap_CheckedChanged(object sender, EventArgs e) => ReverseBool(ref swap_colors);
        private void CheckCoor_CheckedChanged(object sender, EventArgs e) => ReverseBool(ref delete_coor);
        private void CheckPoints_CheckedChanged(object sender, EventArgs e) => ReverseBool(ref delete_point);
        private void CheckShade_CheckedChanged(object sender, EventArgs e) => ReverseBool(ref shade);
        private void CheckRetain_CheckedChanged(object sender, EventArgs e) => ReverseBool(ref freeze_graph);
        private void CheckAuto_CheckedChanged(object sender, EventArgs e) => ReverseBool(ref is_auto);
        private void CheckEdit_CheckedChanged(object sender, EventArgs e)
        {
            DraftBox.ReadOnly = !DraftBox.ReadOnly; // Cannnot pass properties as reference
            DraftBox.BackColor = DraftBox.ReadOnly ? Color.Black : SystemColors.ControlDarkDark;
            DraftBox.ForeColor = DraftBox.ReadOnly ? READONLY_GRAY : Color.White;
            DraftBox.ScrollBars = DraftBox.ReadOnly ? ScrollBars.None : ScrollBars.Vertical;
        }
        #endregion

        // 4.SPECIAL EFFECTS
        #region Click & Mouse Down & Text Changed
        private void Delete_Click(int[] borders, bool isMain)
        {
            Details_TextChanged(null, EventArgs.Empty); // So that axes and grids are drawn correctly
            ClearBitmap(GetBitmap(isMain));
            Invalidate(isMain ? rect_mac : rect_mic); Update(); // To clear the overflowed curves
            DrawBackdropAxesGrids(borders, isMain);
        } // Sensitive
        private void Delete_Click(EventArgs e) { DeleteMain_Click(this, e); DeletePreview_Click(this, e); }
        private void DeleteMain_Click(object sender, EventArgs e) => Delete_Click(GetBorders(1), true);
        private void DeletePreview_Click(object sender, EventArgs e) => Delete_Click(GetBorders(2), false);
        private void ClearButton_Click(object sender, EventArgs e)
        {
            loop_number = chosen_number = 0;
            TextBox[] textBoxes = { DraftBox, PointNumDisplay, TimeDisplay, X_CoorDisplay, Y_CoorDisplay,
                ModulusDisplay, AngleDisplay, FunctionDisplay, CaptionBox };
            foreach (var tbx in textBoxes) SetText(tbx, String.Empty);
            InputString_Focus();
        }
        private void PicturePlay_Click(object sender, EventArgs e)
        {
            if (is_paused)
            {
                MediaPlayer.controls.currentPosition = pause_pos;
                MediaPlayer.controls.play();
                ColorTimer.Start();
            }
            else
            {
                pause_pos = MediaPlayer.controls.currentPosition;
                MediaPlayer.controls.pause();
                ColorTimer.Stop();
                TitleLabel.ForeColor = Color.White;
            }
            ReverseBool(ref is_paused);
        }
        private void PictureIncorrect_Click(object sender, EventArgs e)
        { if (!ProcessingGraphics()) CheckValidityCore(() => InputErrorBox(sender, e, WRONG_FORMAT)); }
        //
        private void PointNumDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(PointNumDisplay.Handle);
        private void TimeDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(TimeDisplay.Handle);
        private void X_CoorDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(X_CoorDisplay.Handle);
        private void Y_CoorDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(Y_CoorDisplay.Handle);
        private void ModulusDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(ModulusDisplay.Handle);
        private void AngleDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(AngleDisplay.Handle);
        private void FunctionDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(FunctionDisplay.Handle);
        private void CaptionBox_MouseDown(object sender, MouseEventArgs e) => HideCaret(CaptionBox.Handle);
        private void DraftBox_MouseDown(object sender, MouseEventArgs e) { if (DraftBox.ReadOnly) HideCaret(DraftBox.Handle); }
        //
        private void Graph_DoubleClick(object sender, EventArgs e)
        {
            InputString.BackColor = FOCUS_GRAY;
            Label[] labels = { InputLabel, AtLabel, GeneralLabel, DetailLabel, X_Scope, Y_Scope, ThickLabel, DenseLabel,
                ExampleLabel, FunctionLabel, ModeLabel, ContourLabel };
            foreach (var lbl in labels) lbl.ForeColor = Color.White;
            PictureIncorrect.Visible = PictureCorrect.Visible = is_checking = false;
        }
        private void SubtitleBox_DoubleClick(object sender, EventArgs e)
        {
            DrawBackdrop(GetBorders(1)); DrawBackdrop(GetBorders(2));
            SetAxesDrawn(true, false); SetAxesDrawn(false, false);
            DrawReferenceRectangles(SystemColors.ControlDark);
        }
        private void InputString_DoubleClick(object sender, EventArgs e) => InputString_TextChanged(sender, e);
        private void AddressInput_DoubleClick(object sender, EventArgs e) => AddressInput_TextChanged(sender, e);
        private void GeneralInput_DoubleClick(object sender, EventArgs e) => GeneralInput_TextChanged(sender, e);
        private void X_Left_DoubleClick(object sender, EventArgs e) => X_Left_TextChanged(sender, e);
        private void X_Right_DoubleClick(object sender, EventArgs e) => X_Right_TextChanged(sender, e);
        private void Y_Left_DoubleClick(object sender, EventArgs e) => Y_Left_TextChanged(sender, e);
        private void Y_Right_DoubleClick(object sender, EventArgs e) => Y_Right_TextChanged(sender, e);
        private void ThickInput_DoubleClick(object sender, EventArgs e) => ThickInput_TextChanged(sender, e);
        private void DenseInput_DoubleClick(object sender, EventArgs e) => DenseInput_TextChanged(sender, e);
        //
        private void MiniChecks(TextBox[] textBoxes, Label lbl)
        {
            try
            {
                if (ProcessingGraphics()) return; bool noSomeInput = false;
                foreach (var tbx in textBoxes)
                {
                    bool noInput = String.IsNullOrEmpty(tbx.Text); noSomeInput = noSomeInput || noInput;
                    if (!noInput) _ = RealSub.Obtain(RecoverMultiply.Beautify(tbx.Text, false)); // For checking
                }
                lbl.ForeColor = noSomeInput ? Color.White : CORRECT_GREEN; // White if any being null or empty
            }
            catch (Exception) { lbl.ForeColor = ERROR_RED; }
        }
        private void MiniChecks(TextBox tbx, Label lbl) => MiniChecks(new TextBox[] { tbx }, lbl);
        private void Details_TextChanged(object sender, EventArgs e)
        {
            if (ProcessingGraphics()) return;
            MiniChecks(new TextBox[] { X_Left, X_Right, Y_Left, Y_Right }, DetailLabel);
            if (scopes == null) return; // Necessary for the initialization

            void checkScopes(bool b1, bool b2, Color c) { if (b1) X_Scope.ForeColor = c; if (b2) Y_Scope.ForeColor = c; }
            try { SetThicknessDensenessScopesBorders(false); }
            catch (Exception) { checkScopes(InvalidScopesX(), InvalidScopesY(), ERROR_RED); }
            finally { checkScopes(!InvalidScopesX(), !InvalidScopesY(), CORRECT_GREEN); } // Should not declare the bools ahead
        } //Sensitive

        private void X_Left_TextChanged(object sender, EventArgs e) => Details_TextChanged(sender, e);
        private void X_Right_TextChanged(object sender, EventArgs e) => Details_TextChanged(sender, e);
        private void Y_Left_TextChanged(object sender, EventArgs e) => Details_TextChanged(sender, e);
        private void Y_Right_TextChanged(object sender, EventArgs e) => Details_TextChanged(sender, e);
        private void GeneralInput_TextChanged(object sender, EventArgs e) => MiniChecks(GeneralInput, GeneralLabel);
        private void ThickInput_TextChanged(object sender, EventArgs e) => MiniChecks(ThickInput, ThickLabel);
        private void DenseInput_TextChanged(object sender, EventArgs e) => MiniChecks(DenseInput, DenseLabel);
        private void InputString_TextChanged(object sender, EventArgs e)
        {
            if (ProcessingGraphics()) return;
            static int removeSomeKeys(TextBox tbx)
            {
                int caretPosition = tbx.Text.Length - tbx.SelectionStart - tbx.SelectionLength; // Necessary
                foreach (char c in RecoverMultiply.BARRED_CHARS) SetText(tbx, tbx.Text.Replace(c, ' '));
                return tbx.Text.Length - caretPosition;
            }
            int pos = removeSomeKeys(InputString); // Necessary
            CheckComplex.Checked = MyString.ContainsAny(MyString.ReplaceConfusion(InputString.Text), RecoverMultiply._ZZ_);
            CheckValidityCore(() =>
            {
                InputString.BackColor = InputLabel.ForeColor = ERROR_RED;
                PictureIncorrect.Visible = true; PictureCorrect.Visible = false;
            });
            InputString.SelectionStart = pos;
        }
        private void AddressInput_TextChanged(object sender, EventArgs e)
        {
            if (ProcessingGraphics()) return;
            if (String.IsNullOrEmpty(AddressInput.Text)) AtLabel.ForeColor = Color.White;
            else AtLabel.ForeColor = Directory.Exists(AddressInput.Text) ? CORRECT_GREEN : ERROR_RED;
        }
        //
        private static void BanDoubleClick(TextBox tbx, MouseEventArgs e)
        { tbx.SelectionStart = tbx.GetCharIndexFromPosition(e.Location); tbx.SelectionLength = 0; } // To ban the default selection
        private void InputString_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(InputString, e);
        private void AddressInput_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(AddressInput, e);
        private void GeneralInput_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(GeneralInput, e);
        private void X_Left_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(X_Left, e);
        private void X_Right_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(X_Right, e);
        private void Y_Left_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(Y_Left, e);
        private void Y_Right_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(Y_Right, e);
        private void ThickInput_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(ThickInput, e);
        private void DenseInput_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(DenseInput, e);
        #endregion

        #region Key Press & Key Down
        private void BarSomeKeys(object sender, KeyPressEventArgs e)
        { if (RecoverMultiply.BARRED_CHARS.Contains(e.KeyChar)) e.Handled = true; }
        private void InputString_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void GeneralInput_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void X_Left_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void X_Right_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void Y_Left_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void Y_Right_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void ThickInput_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void DenseInput_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        //
        private static void AutoKeyDown(TextBox tbx, KeyEventArgs e)
        {
            if (tbx.ReadOnly) return; int caretPosition = tbx.SelectionStart; // Necessary
            void selectSuppress(int pos) { tbx.SelectionStart = caretPosition + pos; e.SuppressKeyPress = true; }
            void insertSelectSuppress(string insertion, int pos)
            { SetText(tbx, tbx.Text.Insert(caretPosition, insertion)); selectSuppress(pos); }

            if (!MyString.CheckParenthesis(tbx.Text.AsSpan(caretPosition, tbx.SelectionLength))) selectSuppress(0);
            else if (e.KeyCode == Keys.D9 && (ModifierKeys & Keys.Shift) != 0)
            {
                if (tbx.SelectionLength == 0) insertSelectSuppress(RecoverMultiply.LR_BRA, 1);
                else
                {
                    string selectedText = tbx.Text.Substring(caretPosition, tbx.SelectionLength);
                    SetText(tbx, tbx.Text.Remove(caretPosition, tbx.SelectionLength));
                    insertSelectSuppress("(" + selectedText + ")", selectedText.Length + 2);
                }
            }
            else if (e.KeyCode == Keys.D0 && (ModifierKeys & Keys.Shift) != 0)
            {
                if (tbx.SelectionLength > 0) selectSuppress(0);
                else if (caretPosition == 0) selectSuppress(0);
                else if (RecoverMultiply.IsOpen(tbx.Text[caretPosition - 1])) selectSuppress(1);
            }
            else if (e.KeyCode == Keys.Oemcomma) insertSelectSuppress(", ", 2);
            else if (e.KeyCode == Keys.OemPipe) insertSelectSuppress(" | ", 3);
            else if (e.KeyCode == Keys.Back)
            {
                if (caretPosition == 0 || !MyString.CheckParenthesis(tbx.Text) || tbx.SelectionLength > 0) return;
                else if (RecoverMultiply.IsOpen(tbx.Text[caretPosition - 1]))
                {
                    if (RecoverMultiply.IsClose(tbx.Text[caretPosition])) SetText(tbx, tbx.Text.Remove(caretPosition - 1, 2));
                    selectSuppress(-1);
                }
                else if (tbx.Text[caretPosition - 1] == ')') selectSuppress(-1);
            }
        } // Sensitive
        private void InputString_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(InputString, e);
        private void GeneralInput_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(GeneralInput, e);
        private void X_Left_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(X_Left, e);
        private void X_Right_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(X_Right, e);
        private void Y_Left_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(Y_Left, e);
        private void Y_Right_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(Y_Right, e);
        private void ThickInput_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(ThickInput, e);
        private void DenseInput_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(DenseInput, e);

        private static void Combo_KeyDown(KeyEventArgs e) // To ban the default keyboard search
            => e.SuppressKeyPress = e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z;
        private void ComboExamples_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(e);
        private void ComboFunctions_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(e);
        private void ComboSpecial_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(e);
        private void ComboColoring_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(e);
        private void ComboContour_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(e);
        #endregion

        #region Mouse Hover & Mouse Leave
        private static void SetFont(Label lbl) => lbl.ForeColor = lbl.ForeColor == Color.White ? UNCHECK_YELLOW : lbl.ForeColor;
        private static void RecoverFont(Label lbl) => lbl.ForeColor = lbl.ForeColor == UNCHECK_YELLOW ? Color.White : lbl.ForeColor;
        private static void HoverEffect(TextBox tbx, Label lbl)
        { tbx.BackColor = lbl.ForeColor == Color.White ? FOCUS_GRAY : lbl.ForeColor; tbx.ForeColor = Color.Black; SetFont(lbl); }
        private static void LeaveEffect(TextBox tbx, Label lbl)
        { tbx.BackColor = CONTROL_GRAY; tbx.ForeColor = Color.White; RecoverFont(lbl); }

        private void InputString_MouseHover(object sender, EventArgs e) => SetFont(InputLabel);
        private void InputString_MouseLeave(object sender, EventArgs e) => RecoverFont(InputLabel);
        private void AddressInput_MouseHover(object sender, EventArgs e) => HoverEffect(AddressInput, AtLabel);
        private void AddressInput_MouseLeave(object sender, EventArgs e) => LeaveEffect(AddressInput, AtLabel);
        private void GeneralInput_MouseHover(object sender, EventArgs e) => HoverEffect(GeneralInput, GeneralLabel);
        private void GeneralInput_MouseLeave(object sender, EventArgs e) => LeaveEffect(GeneralInput, GeneralLabel);
        private void X_Left_MouseHover(object sender, EventArgs e) => HoverEffect(X_Left, DetailLabel);
        private void X_Left_MouseLeave(object sender, EventArgs e) => LeaveEffect(X_Left, DetailLabel);
        private void X_Right_MouseHover(object sender, EventArgs e) => HoverEffect(X_Right, DetailLabel);
        private void X_Right_MouseLeave(object sender, EventArgs e) => LeaveEffect(X_Right, DetailLabel);
        private void Y_Left_MouseHover(object sender, EventArgs e) => HoverEffect(Y_Left, DetailLabel);
        private void Y_Left_MouseLeave(object sender, EventArgs e) => LeaveEffect(Y_Left, DetailLabel);
        private void Y_Right_MouseHover(object sender, EventArgs e) => HoverEffect(Y_Right, DetailLabel);
        private void Y_Right_MouseLeave(object sender, EventArgs e) => LeaveEffect(Y_Right, DetailLabel);
        private void ThickInput_MouseHover(object sender, EventArgs e) => HoverEffect(ThickInput, ThickLabel);
        private void ThickInput_MouseLeave(object sender, EventArgs e) => LeaveEffect(ThickInput, ThickLabel);
        private void DenseInput_MouseHover(object sender, EventArgs e) => HoverEffect(DenseInput, DenseLabel);
        private void DenseInput_MouseLeave(object sender, EventArgs e) => LeaveEffect(DenseInput, DenseLabel);
        private void DraftBox_MouseHover(object sender, EventArgs e)
        {
            if (DraftBox.ReadOnly)
            {
                DraftLabel.ForeColor = READONLY_PURPLE;
                toolTip_ReadOnly.SetToolTip(DraftBox, TIP);
            }
            else
            {
                DraftBox.BackColor = FOCUS_GRAY;
                toolTip_ReadOnly.SetToolTip(DraftBox, String.Empty);
                SetFont(DraftLabel);
            }
            DraftBox.ForeColor = DraftBox.ReadOnly ? Color.White : Color.Black;
        }
        private void DraftBox_MouseLeave(object sender, EventArgs e)
        {
            if (!DraftBox.ReadOnly) DraftBox.BackColor = CONTROL_GRAY;
            DraftBox.ForeColor = DraftBox.ReadOnly ? READONLY_GRAY : Color.White;
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
        private void CheckAuto_MouseHover(object sender, EventArgs e) => CheckAuto.ForeColor = COMBO_BLUE;
        private void CheckAuto_MouseLeave(object sender, EventArgs e) => CheckAuto.ForeColor = Color.White;
        private void CheckEdit_MouseHover(object sender, EventArgs e) => CheckEdit.ForeColor = COMBO_BLUE;
        private void CheckEdit_MouseLeave(object sender, EventArgs e) => CheckEdit.ForeColor = Color.White;

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

        private void SubtitleBox_MouseHover(object sender, EventArgs e) => SubtitleBox.ForeColor = ERROR_RED;
        private void SubtitleBox_MouseLeave(object sender, EventArgs e) => SubtitleBox.ForeColor = Color.White;
        private void CaptionBox_MouseHover(object sender, EventArgs e) => CaptionBox.ForeColor = Color.White;
        private void CaptionBox_MouseLeave(object sender, EventArgs e) => CaptionBox.ForeColor = READONLY_GRAY;
        private void PreviewLabel_MouseHover(object sender, EventArgs e) => PreviewLabel.ForeColor = READONLY_PURPLE;
        private void PreviewLabel_MouseLeave(object sender, EventArgs e) => PreviewLabel.ForeColor = Color.White;
        private void X_Bar_MouseHover(object sender, EventArgs e) => X_Bar.ForeColor = READONLY_PURPLE;
        private void X_Bar_MouseLeave(object sender, EventArgs e) => X_Bar.ForeColor = Color.White;
        private void Y_Bar_MouseHover(object sender, EventArgs e) => Y_Bar.ForeColor = READONLY_PURPLE;
        private void Y_Bar_MouseLeave(object sender, EventArgs e) => Y_Bar.ForeColor = Color.White;

        private static void ResizeControl(PictureBox pbx, int delta, bool isLarge)
        {
            if (isLarge ? is_resized : !is_resized) return; // To prevent repetitious call
            var (_location, _size) = isLarge ? (-delta, 2 * delta) : (delta, -2 * delta);
            pbx.Location = new(pbx.Location.X + _location, pbx.Location.Y + _location);
            pbx.Size = new(pbx.Width + _size, pbx.Height + _size);
            is_resized = isLarge;
        }
        private static void EnlargePicture(PictureBox pbx, int increment) => ResizeControl(pbx, increment, true);
        private static void ShrinkPicture(PictureBox pbx, int decrement) => ResizeControl(pbx, decrement, false);

        private void PictureLogo_MouseHover(object sender, EventArgs e) => EnlargePicture(PictureLogo, 5);
        private void PictureLogo_MouseLeave(object sender, EventArgs e) => ShrinkPicture(PictureLogo, 5);
        private void PicturePlay_MouseHover(object sender, EventArgs e) => EnlargePicture(PicturePlay, 2);
        private void PicturePlay_MouseLeave(object sender, EventArgs e) => ShrinkPicture(PicturePlay, 2);
        private void PictureIncorrect_MouseHover(object sender, EventArgs e) => EnlargePicture(PictureIncorrect, 2);
        private void PictureIncorrect_MouseLeave(object sender, EventArgs e) => ShrinkPicture(PictureIncorrect, 2);

        private void ExportButton_MouseHover(object sender, EventArgs e) => AddressInput_DoubleClick(sender, e);
        private void StoreButton_MouseHover(object sender, EventArgs e) => AddressInput_DoubleClick(sender, e);
        #endregion
    } /// The visualization interface
    public class MyMessageBox : Form
    {
        private static Button btnOk;
        private static TextBox txtMessage;
        private static readonly Color BACKDROP_GRAY = Graph.Argb(64, 64, 64),
            FORMAL_FONT = Graph.Argb(224, 224, 224), CUSTOM_FONT = Color.Turquoise, EXCEPTION_FONT = Color.LightPink,
            FORMAL_BUTTON = Color.Black, CUSTOM_BUTTON = Color.DarkBlue, EXCEPTION_BUTTON = Color.DarkRed;

        private static float scaling_factor;
        private static readonly float MSG_TXT_SIZE = 10f, BTN_TXT_SIZE = 7f;
        private static readonly int DIST = 10, BTN_SIZE = 25, BORDER = 10; // DIST = dist(btnOk, txtMessage)
        private static bool is_resized;
        private static readonly string MSG_FONT = "Microsoft YaHei UI", BTN_FONT = "Microsoft YaHei UI", BTN_TXT = "OK";

        private static void BtnOk_MouseEnterLeave(bool isEnter)
        {
            if (isEnter ? is_resized : !is_resized) return; // To prevent repetitious call
            var (_size, _location, _font) = isEnter ? (2, -1, 1f) : (-2, 1, -1f);
            btnOk.Size = new(btnOk.Width + _size, btnOk.Height + _size);
            btnOk.Location = new(btnOk.Location.X + _location, btnOk.Location.Y + _location);
            btnOk.Font = new(btnOk.Font.FontFamily, btnOk.Font.Size + _font, btnOk.Font.Style);
            is_resized = isEnter;
        } // Analogous to Graph.ResizeControl
        private void BtnOk_MouseEnter(object sender, EventArgs e) => BtnOk_MouseEnterLeave(true);
        private void BtnOk_MouseLeave(object sender, EventArgs e) => BtnOk_MouseEnterLeave(false);
        private void Form_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) Close(); }

        private void SetUpForm(int width, int height)
        {
            FormBorderStyle = FormBorderStyle.None; TopMost = true; Size = new(width, height);
            StartPosition = FormStartPosition.CenterScreen; BackColor = SystemColors.ControlDark;
        }
        private static void SetUpTextBox(string message, int width, int height, Color txtColor)
        {
            txtMessage = new()
            {
                Text = message,
                Font = new(MSG_FONT, MSG_TXT_SIZE, FontStyle.Regular),
                ForeColor = txtColor,
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = BACKDROP_GRAY,
                ScrollBars = ScrollBars.Vertical
            };
            txtMessage.SetBounds(BORDER, BORDER, width - BORDER * 2, height - BORDER - 2 * DIST - BTN_SIZE);
            txtMessage.SelectionStart = message.Length; txtMessage.SelectionLength = 0;
            txtMessage.GotFocus += (sender, e) => { Graph.HideCaret(txtMessage.Handle); }; // This works well!
        }
        private void SetUpButton(int width, int height, Color btnColor, Color btnTxtColor)
        {
            btnOk = new()
            {
                Size = new(BTN_SIZE * 2, BTN_SIZE),
                Location = new(width / 2 - BTN_SIZE, height - DIST - BTN_SIZE),
                BackColor = btnColor,
                ForeColor = btnTxtColor,
                Font = new(BTN_FONT, BTN_TXT_SIZE, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Text = BTN_TXT,
            };
            btnOk.FlatAppearance.BorderSize = 0; btnOk.Click += (sender, e) => { Close(); };
            btnOk.MouseEnter += BtnOk_MouseEnter; btnOk.MouseLeave += BtnOk_MouseLeave;
        }
        private void Setup(string message, int width, int height, Color txtColor, Color btnColor, Color btnTxtColor)
        {
            SetUpForm(width, height);
            SetUpTextBox(message, width, height, txtColor);
            SetUpButton(width, height, btnColor, btnTxtColor);
            Controls.Add(txtMessage); Controls.Add(btnOk);

            Graph.ReduceFontSizeByScale(this, ref scaling_factor);
            KeyPreview = true; KeyDown += new(Form_KeyDown);
        }
        private static void Display(string message, int width, int height, Color txtColor, Color btnColor, Color btnTxtColor)
        {
            MyMessageBox box = new();
            box.Setup(message, width, height, txtColor, btnColor, btnTxtColor);
            box.ShowDialog();
        }

        public static void ShowFormal(string message, int width, int height)
            => Display(message, width, height, FORMAL_FONT, FORMAL_BUTTON, Color.White);
        public static void ShowCustom(string message, int width, int height)
            => Display(message, width, height, CUSTOM_FONT, CUSTOM_BUTTON, Color.White);
        public static void ShowException(string message, int width, int height)
            => Display(message, width, height, EXCEPTION_FONT, EXCEPTION_BUTTON, Color.White);
    } /// Customized box construction

    /// <summary>
    /// TOOLKIT SECTION
    /// </summary>
    public class MyString
    {
        public static readonly string[] FUNC_NAMES = new string[] { "Func(", "func(", "Polar(", "polar(", "Param(", "param(" };
        public static readonly string[] LOOP_NAMES = new string[] { "Loop(", "loop(" };

        private static readonly List<string> CONFUSION = new() { "Zeta", "zeta" };
        private static readonly char SUB_CHAR = ';';

        #region Reckoning
        protected static int CountChars(ReadOnlySpan<char> input, string charsToCheck)
        {
            HashSet<char> charSet = new(charsToCheck); int count = 0;
            foreach (char c in input) if (charSet.Contains(c)) count++;
            return count;
        }
        protected static (int, int, int, int) PrepareLoop(ReadOnlySpan<char> temp) => (CountChars(temp, "("), temp.Length - 1, 0, -1);
        public static bool ContainsAny(ReadOnlySpan<char> input, string charsToCheck)
        {
            HashSet<char> charSet = new(charsToCheck);
            foreach (char c in input) if (charSet.Contains(c)) return true;
            return false;
        }
        public static bool ContainsAny(string input, string[] stringsToCheck)
        {
            foreach (string s in stringsToCheck) if (input.Contains(s)) return true;
            return false;
        }
        public static bool ContainsFuncName(string input) => ContainsAny(input, FUNC_NAMES);
        #endregion

        #region Parentheses
        private static int PairedParenthesis(ReadOnlySpan<char> input, int n)
        {
            for (int i = n + 1, count = 1; ; i++)
            {
                if (RecoverMultiply.IsOpen(input[i])) count++; else if (RecoverMultiply.IsClose(input[i])) count--;
                if (count == 0) return i;
            }
        }
        protected static (int, int, string) PrepareSeriesSub(string input)
        {
            int i = input.IndexOf(ReplaceTags.UNDERLINE), end = PairedParenthesis(input, i + 1);
            return (i, end, BraFreePart(input, i + 1, end));
        }
        protected static void ResetBeginEnd(ReadOnlySpan<char> temp, ref int begin, ref int end)
        {
            static (int, int) innerBra(ReadOnlySpan<char> input, int start)
            {
                for (int i = start, j = -1; ; i--)
                { if (RecoverMultiply.IsClose(input[i])) j = i; else if (RecoverMultiply.IsOpen(input[i])) return (i, j); }
            }
            static int pairedInnerBra(ReadOnlySpan<char> input, int n) { for (int i = n + 1; ; i++) if (input[i] == ')') return i; }

            (begin, end) = innerBra(temp, begin); if (end == -1) end = pairedInnerBra(temp, begin);
        }
        public static bool CheckParenthesis(ReadOnlySpan<char> input)
        {
            int sum = 0;
            foreach (char c in input)
            {
                if (RecoverMultiply.IsOpen(c)) sum++; else if (RecoverMultiply.IsClose(c)) sum--;
                if (sum < 0) return false;
            }
            return sum == 0;
        }
        private static string SubBase(int n, char c1, char c2) => String.Concat(c1, n.ToString(), c2);
        protected static string BracketSub(int n) => SubBase(n, '[', ']');
        #endregion

        #region Replacement
        private static string Extract(string input, int begin, int end) => input.AsSpan(begin, end - begin + 1).ToString();
        protected static string BraFreePart(string temp, int begin, int end) => Extract(temp, begin + 1, end - 1);
        protected static string TryBraNum(string input) => BraFreePart(input, 0, input.Length - 1);
        public static string Replace(string original, string replacement, int begin, int end)
            => String.Create(begin + replacement.Length + original.Length - end - 1,
                (original, replacement, begin, end), (span, state) =>
                {
                    var (orig, repl, b, e) = state;
                    orig.AsSpan(0, b).CopyTo(span); // Copying the beginning
                    repl.AsSpan().CopyTo(span[b..]); // Copying the replacement
                    orig.AsSpan(e + 1).CopyTo(span[(b + repl.Length)..]); // Copying the remaining
                });
        public static string ReplaceLoop(string[] split, int a, int b, int i) => split[a].Replace(split[b], SubBase(i, '(', ')'));
        protected static void SubstituteTemp(ref string temp, ref int begin, int end, ref int tagL, ref int count)
        { begin -= tagL + 1; tagL = -1; temp = Replace(temp, BracketSub(count++), begin--, end); }
        private static string ReplaceInterior(string input, char c, char replacement)
        {
            if (!input.Contains(ReplaceTags.UNDERLINE)) return input;
            StringBuilder result = new(input);
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] != ReplaceTags.UNDERLINE) continue;
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
        protected static string[] ReplaceRecover(string input)
            => SplitByChars(ReplaceInterior(input, ',', SUB_CHAR), ",").Select(part => part.Replace(SUB_CHAR, ',')).ToArray();
        protected static string ReplaceSubstrings(string input, List<string> substrings, string replacement)
           => System.Text.RegularExpressions.Regex.Replace(input, String.Join("|", substrings), replacement);
        public static string ReplaceConfusion(string input) => ReplaceSubstrings(input, CONFUSION, String.Empty);
        #endregion

        #region Miscellaneous
        public static string[] SplitString(string input)
            => ReplaceRecover(BraFreePart(input, input.IndexOf('('), PairedParenthesis(input, input.IndexOf('('))));
        public static string[] SplitByChars(string input, string delimiters)
        {
            List<string> segments = new();
            StringBuilder currentSegment = new();
            HashSet<char> delimiterSet = new(delimiters);
            for (int i = 0; i < input.Length; i++)
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
        protected static string TrimStartChar(string input, char c)
        {
            int startIndex = 0, length = input.Length;
            while (startIndex < length && input[startIndex] == c) startIndex++;

            if (startIndex == length) return String.Empty;
            StringBuilder result = new(length - startIndex);
            return result.Append(input, startIndex, length - startIndex).ToString();
        }
        public static string TrimLargeDouble(double input, double threshold)
            => Math.Abs(input) < threshold ? input.ToString("#0.000000") : input.ToString("E3");
        public static string GetAngle(double x, double y) => (Graph.ArgRGB(x, y) / Math.PI).ToString("#0.00000") + " * PI";
        public static void ThrowException(bool error = true) { if (error) throw new Exception(); }
        public static void ThrowInvalidLengths(string[] split, int[] length) => ThrowException(!length.Contains(split.Length));
        public static void For(int start, int end, Action<int> action) { for (int i = start; i <= end; i++) action(i); }
        #endregion
    } /// General simplifications
    public class RealComplex : MyString
    {
        protected static readonly double GAMMA = 0.5772156649015329;
        protected static readonly int THRESHOLD = 10, STEP = 1;
        protected static readonly string SUB_CHARS = ":;", IJ_ = String.Concat(I_, J_);

        protected const char _A = 'a', A_ = 'A', B_ = 'B', C_ = 'C', _C = 'c', _D_ = '$', E = 'e', E_ = 'E', _F = 'f', F_ = 'F', _F_ = '!', G = 'g',
            G_ = 'G', _H = 'h', I = 'i', I_ = 'I', J_ = 'J', _L = 'l', M_ = 'M', MAX = '>', MIN = '<', MODE_1 = '1', MODE_2 = '2', P = 'p', P_ = 'P',
            _Q = 'q', _R = 'r', S_ = 'S', _S = 's', SB = '[', SP = '#', _T = 't', _X = 'x', X_ = 'X', _Y = 'y', Y_ = 'Y', _Z = 'z', Z_ = 'Z', _Z_ = 'Z';

        protected static RealMatrix ModeChooser(string mode, RealMatrix output_1, RealMatrix output_2, int row, int column)
        {
            switch (Char.Parse(mode))
            {
                case MODE_1: return output_1;
                case MODE_2: return output_2;
                default: ThrowException(); return new(row, column);
            }
        }
        protected static (string[], StringBuilder) PrepareBreakPSMD(string input, string signs, char sign, int THRESHOLD)
        {
            StringBuilder signsBuilder = new(), result = new(input);
            for (int i = 0, flag = 0; i < result.Length; i++)
            {
                if (!signs.Contains(result[i])) continue;
                if (++flag % THRESHOLD == 0)
                {
                    char replacement = result[i] == sign ? SUB_CHARS[0] : SUB_CHARS[1]; // Necessary
                    result.Remove(i, 1).Insert(i, replacement);
                    signsBuilder.Append(result[i]);
                }
            }
            return (SplitByChars(result.ToString(), SUB_CHARS), signsBuilder);
        }
        protected static string[] PrepareBreakPower(string input, int THRESHOLD)
        {
            StringBuilder result = new(input);
            for (int i = 0, flag = 0; i < result.Length; i++)
            {
                if (result[i] != '^') continue;
                if (++flag % THRESHOLD == 0) result.Remove(i, 1).Insert(i, SUB_CHARS[0]);
            }
            return SplitByChars(result.ToString(), SUB_CHARS[0].ToString());
        }
        protected static (string[], StringBuilder) GetPlusSubtractComponents(string input)
        {
            bool begins_minus = input[0] == '-'; input = TrimStartChar(input, '-');
            string[] temp_split = SplitByChars(input, "+-"); input = String.Concat(begins_minus ? '-' : '+', input); // Sensitive

            StringBuilder psBuilder = new();
            for (int i = 0; i < input.Length; i++) if ("+-".Contains(input[i])) psBuilder.Append(input[i]);
            return (temp_split, psBuilder);
        }
        protected static (string[], StringBuilder) GetMultiplyDivideComponents(string tmpSplit)
        {
            string[] split = SplitByChars(tmpSplit, "*/");
            StringBuilder mdBuilder = new();
            for (int i = 0; i < tmpSplit.Length; i++) if ("*/".Contains(tmpSplit[i])) mdBuilder.Append(tmpSplit[i]);
            return (split, mdBuilder);
        }
    } /// Commonalities for RealSub & ComplexSub
    public class ReplaceTags : RealComplex
    {
        public static readonly string[] FUNCTIONS = new string[]
            { "floor", "ceil", "round", "sgn", "F", "gamma", "beta", "zeta", "mod", "nCr", "nPr",
                "max", "min", "log", "exp", "sqrt", "abs", "factorial", "arsinh", "arcosh", "artanh",
                "arcsin", "arccos", "arctan", "sinh", "cosh", "tanh", "sin", "cos", "tan", "conjugate", "e" };
        public static readonly string[] SPECIALS = new string[]
            { "product", "sum", "iterate1", "iterate2", "composite1", "composite2",
                "iterateLoop", "loop", "iterate", "composite", "func", "polar", "param" };
        public static readonly string[] EX_COMPLEX = new string[]
        {
            "F(1-10i,0.5i,i,zzzzz,100)",
            "z^(1+10i)cos((z-1)/(z^13+z+1))",
            "sum(-1+1/(1-z^n),n,1,100)",
            "prod(exp(1+2/(ze(-k/5)-1)),k,1,5)",
            "iterate((Z+1/Z)e(0.02),z,k,1,1000)",
            "iterate(exp(z^Z),z,k,1,100)",
            "iterateLoop(ZZ+z,0,k,1,100)",
            "comp(zz,sin(zZ),cos(z/Z))"
        };
        public static readonly string[] EX_REAL = new string[]
        {
            "cos(xy)-cos(x)-cos(y)",
            "min(sin(xy),tan(x),tan(y))",
            "xround(y)-yround(x)",
            "y-x|IterateLoop(x^X,x,k,1,30,y-X)",
            "iterate1(kx/X+X/(y+k),sin(x+y),k,1,3)",
            "iterate2(k/X+k/Y,XY,sin(x+y),cos(x-y),k,1,10,2)",
            "comp1(xy,tan(X+x),Artanh(X-y))",
            "comp2(xy,xx+yy,sin(X+Y),cos(X-Y),2)"
        };
        public static readonly string[] EX_CURVES = new string[]
        {
            "func(ga(x,100),0.0001)",
            "func(sum(sin(2^kx)/2^k,k,0,100),-pi,pi,0.001)",
            "func(beta(sinh(x),cosh(x),100),-2,2,0.00001)",
            "polar(sqrt(cos(2theta)),theta,0,2pi,0.0001)",
            "polar(cos(5k)cos(7k),k,0,2pi,0.001)",
            "loop(polar(0.1jcos(5k+0.7jpi),k,0,pi),j,1,10)",
            "param(cos(17k),cos(19k),k,0,pi,0.0001)",
            "loop(param(cos(m)^k,sin(m)^k,m,0,p/2),k,1,10)"
        };

        public static readonly char FUNC_HEAD = '~', UNDERLINE = '_', DOLLAR = _D_;
        public static readonly string FUNC = "α", POLAR = "β", PARAM = "γ", ITLOOP = "δ",
            LOG = _L.ToString(), EXP = E_.ToString(), SQRT = _Q.ToString(), ABS = _A.ToString(), FACT = _F_.ToString(),
            SIN = _S.ToString(), COS = _C.ToString(), TAN = _T.ToString(), // This should come first
            AS = String.Concat(_A, SIN), AC = String.Concat(_A, COS), AT = String.Concat(_A, TAN),
            SH = String.Concat(SIN, _H), CH = String.Concat(COS, _H), TH = String.Concat(TAN, _H),
            ASH = String.Concat(AS, _H), ACH = String.Concat(AC, _H), ATH = String.Concat(AT, _H),
            PROD = P_.ToString(), SUM = S_.ToString(), F = F_.ToString(),
            GA = G_.ToString(), BETA = B_.ToString(), ZETA = _Z_.ToString(),
            FLOOR = _F.ToString(), CEIL = _C.ToString(), ROUND = _R.ToString(), SIGN = _S.ToString(),
            MOD = M_.ToString(), NCR = C_.ToString(), NPR = A_.ToString(), _MAX = MAX.ToString(), _MIN = MIN.ToString(),
            IT = I_.ToString(), IT1 = String.Concat(MODE_1, IT), IT2 = String.Concat(MODE_2, IT),
            COMP = J_.ToString(), COMP1 = String.Concat(MODE_1, COMP), COMP2 = String.Concat(MODE_2, COMP),
            CONJ = J_.ToString(), E_SP = String.Concat(EXP, SP),
            PI = P.ToString(), _GA = G.ToString();
        private static Dictionary<string, string> Concat(Dictionary<string, string> s1, Dictionary<string, string> s2)
            => s1.Concat(s2).ToDictionary(pair => pair.Key, pair => pair.Value); // Series first, Standard next
        private static readonly Dictionary<string, string> COMMON_STANDARD = new()
        {
            { "log", LOG }, { "Log", LOG }, { "ln", LOG }, { "Ln", LOG },
            { "exp", EXP }, { "Exp", EXP },
            { "sqrt", SQRT }, { "Sqrt", SQRT },
            { "abs", ABS }, { "Abs", ABS },
            { "factorial", FACT }, { "Factorial", FACT }, { "fact", FACT }, { "Fact", FACT },
            { "arsinh", ASH }, { "Arsinh", ASH }, { "asinh", ASH }, { "Asinh", ASH },
            { "arcosh", ACH }, { "Arcosh", ACH }, { "acosh", ACH }, { "Acosh", ACH },
            { "artanh", ATH }, { "Artanh", ATH }, { "atanh", ATH }, { "Atanh", ATH },
            { "arcsin", AS }, { "Arcsin", AS }, { "asin", AS }, { "Asin", AS },
            { "arccos", AC }, { "Arccos", AC }, { "acos", AC }, { "Acos", AC },
            { "arctan", AT }, { "Arctan", AT }, { "atan", AT }, { "Atan", AT },
            { "sinh", SH }, { "Sinh", SH },
            { "cosh", CH }, { "Cosh", CH },
            { "tanh", TH }, { "Tanh", TH },
            { "sin", SIN }, { "Sin", SIN },
            { "cos", COS }, { "Cos", COS },
            { "tan", TAN }, { "Tan", TAN }
        };
        private static readonly Dictionary<string, string> COMMON_SERIES = AddSuffix(new()
        {
            { "Product", PROD }, { "product", PROD }, { "Prod", PROD }, { "prod", PROD },
            { "Sum", SUM }, { "sum", SUM },
            { "F", F },
            { "Gamma", GA }, { "gamma", GA }, { "Ga", GA }, { "ga", GA },
            { "Beta", BETA }, { "beta", BETA },
            { "Zeta", ZETA }, { "zeta", ZETA }
        }, UNDERLINE);
        private static readonly Dictionary<string, string> COMMON = Concat(COMMON_SERIES, COMMON_STANDARD);
        private static readonly Dictionary<string, string> REAL_STANDARD = AddSuffix(new()
        {
            { "Floor", FLOOR }, { "floor", FLOOR },
            { "Ceil", CEIL }, { "ceil", CEIL },
            { "Round", ROUND }, { "round", ROUND },
            { "Sign", SIGN }, { "sign", SIGN }, { "Sgn", SIGN }, { "sgn", SIGN }
        }, DOLLAR);
        private static readonly Dictionary<string, string> REAL_SERIES = AddSuffix(new()
        {
            { "Mod", MOD }, { "mod", MOD },
            { "nCr", NCR }, { "nPr", NPR },
            { "Max", _MAX }, { "max", _MAX }, { "Min", _MIN }, { "min", _MIN },
            { "Iterate1", IT1 }, { "iterate1", IT1 }, { "Iterate2", IT2 }, { "iterate2", IT2 },
            { "Composite1", COMP1 }, { "composite1", COMP1 }, { "Comp1", COMP1 }, { "comp1", COMP1 },
            { "Composite2", COMP2 }, { "composite2", COMP2 }, { "Comp2", COMP2 }, { "comp2", COMP2 }
        }, UNDERLINE);
        private static readonly Dictionary<string, string> REAL = Concat(REAL_SERIES, REAL_STANDARD);
        private static readonly Dictionary<string, string> COMPLEX_STANDARD = new()
        { { "conjugate", CONJ }, { "Conjugate", CONJ }, { "conj", CONJ }, { "Conj", CONJ }, { "e", E_SP } };
        private static readonly Dictionary<string, string> COMPLEX_SERIES = AddSuffix(new()
        {
            { "Iterate", IT }, { "iterate", IT },
            { "Composite", COMP }, { "composite", COMP }, { "Comp", COMP }, { "comp", COMP }
        }, UNDERLINE);
        private static readonly Dictionary<string, string> COMPLEX = Concat(COMPLEX_SERIES, COMPLEX_STANDARD);
        private static readonly Dictionary<string, string> CONSTANTS = new()
        { { "pi", PI }, { "Pi", PI }, { "gamma", _GA }, { "Gamma", _GA }, { "ga", _GA }, { "Ga", _GA } };
        private static readonly Dictionary<string, string> TAGS = AddSuffix(new()
        {
            { "Func", FUNC }, { "func", FUNC },
            { "Polar", POLAR }, { "polar", POLAR },
            { "Param", PARAM }, { "param", PARAM },
            { "IterateLoop", ITLOOP }, { "iterateLoop", ITLOOP }
        }, UNDERLINE);

        private static Dictionary<string, string> AddPrefixSuffix(Dictionary<string, string> dic)
        {
            Dictionary<string, string> Dic = new();
            foreach (var kvp in dic) Dic[String.Concat(kvp.Key, '(')] = String.Concat(FUNC_HEAD, kvp.Value, '(');
            return Dic;
        }
        private static Dictionary<string, string> AddSuffix(Dictionary<string, string> dic, char suffix)
        {
            Dictionary<string, string> Dic = new();
            foreach (var kvp in dic) Dic[kvp.Key] = String.Concat(kvp.Value, suffix);
            return Dic;
        }
        private static string ReplaceBase(string input, Dictionary<string, string> dic)
        {
            foreach (var pair in dic) input = input.Replace(pair.Key, pair.Value);
            return input;
        }
        private static string ReplaceCommon(string input) => ReplaceConstant(ReplaceBase(input, AddPrefixSuffix(COMMON)));
        private static string ReplaceConstant(string input) => ReplaceBase(input, CONSTANTS);

        protected static string ReplaceReal(string input) => ReplaceCommon(ReplaceBase(input, AddPrefixSuffix(REAL)));
        protected static string ReplaceComplex(string input) => ReplaceCommon(ReplaceBase(input, AddPrefixSuffix(COMPLEX)));
        public static string ReplaceCurves(string input) => ReplaceBase(input, AddPrefixSuffix(TAGS));
    } /// Function name interpretors
    public class RecoverMultiply : ReplaceTags
    {
        public static readonly string _ZZ_ = String.Concat(_Z, Z_), LR_BRA = "()",
            BARRED_CHARS = String.Concat("\t!\"#$%&\':;<=>?@[\\]_`{}~", FUNC, POLAR, PARAM, ITLOOP);

        private static readonly string VAR_REAL = String.Concat(_X, _Y, X_, Y_), VAR_COMPLEX = String.Concat(_Z, Z_, I),
            CONST = String.Concat(E, P, G), ARITH = "+-*/^(,|"; // Function heads preceded by these require no anterior recovery
        private static readonly List<string> ENTER_BLANK = new() { "\n", "\r", " " };

        public static string Beautify(string input, bool isComplex)
        {
            ThrowException(!CheckParenthesis(input) || input.Contains(LR_BRA) || ContainsAny(input, BARRED_CHARS));
            Func<string, string> replaceTags = isComplex ? ReplaceComplex : ReplaceReal;
            return replaceTags(ReplaceSubstrings(input, ENTER_BLANK, String.Empty));
        }
        protected static string Recover(string input, bool isComplex)
        {
            int length = input.Length; if (length == 1) return input;
            StringBuilder sb = new(length * 2); // The longest possible length
            sb.Append(input[0]);
            for (int i = 1; i < length; i++) // Should not use parallel
            {
                if (AddOrNot(input[i - 1], input[i], isComplex)) sb.Append('*');
                sb.Append(input[i]);
            }
            return sb.ToString();
        }

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
        } // Sensitive
        private static bool IsVar(char c, bool isComplex) => isComplex ? IsVarComplex(c) : IsVarReal(c);
        private static bool IsVarReal(char c) => VAR_REAL.Contains(c);
        private static bool IsVarComplex(char c) => VAR_COMPLEX.Contains(c);
        private static bool IsConst(char c) => CONST.Contains(c);
        private static bool IsArithmetic(char c) => ARITH.Contains(c);
        private static bool IsFunctionHead(char c) => c == FUNC_HEAD;
        public static bool IsOpen(char c) => c == '(';
        public static bool IsClose(char c) => c == ')';
    } /// Recovery of omitted '*'

    /// <summary>
    /// COMPUTATION SECTION
    /// </summary>
    public class ComplexSub : RecoverMultiply
    {
        #region Fields & Constructors
        private static readonly int STRUCTSIZE = Marshal.SizeOf<Complex>();
        private readonly uint columnSIZE, strideSIZE, residueSIZE; // For copying
        private readonly int row, column, span, bulk; // For parallel copy chunks
        private readonly ComplexMatrix z;
        private readonly ComplexMatrix[] braValues; // To store values between parentheses pairs

        private int count; // To log parentheses
        private ComplexMatrix Z; // For substitution
        private string input;

        public ComplexSub(string input, ComplexMatrix? z, ComplexMatrix? Z, int row, int column)
        {
            ThrowException(String.IsNullOrEmpty(input));
            this.input = Recover(input, true); braValues = new ComplexMatrix[CountChars(this.input, "(")];
            this.row = row; this.column = column; span = row / STEP; bulk = span * STEP; int temp = column * STRUCTSIZE;
            columnSIZE = (uint)temp; strideSIZE = (uint)(temp * STEP); residueSIZE = (uint)(temp * (row - bulk));

            if (z != null) this.z = (ComplexMatrix)z; if (Z != null) this.Z = (ComplexMatrix)Z;
        }
        public ComplexSub(string input, RealMatrix real, RealMatrix imaginary, int row, int column)
            : this(input, InitilizeZ(real, imaginary, row, column), null, row, column) { }
        private ComplexSub ObtainSub(string input, ComplexMatrix? Z) => new(input, z, Z, row, column);
        private ComplexMatrix ObtainValue(string input) => new ComplexSub(input, z, Z, row, column).Obtain();
        #endregion

        #region Calculations
        private unsafe ComplexMatrix Hypergeometric(string[] split)
        {
            ThrowInvalidLengths(split, new int[] { 4, 5 });
            int n = split.Length == 5 ? RealSub.ToInt(split[4]) : 100;
            ComplexMatrix sum = new(row, column), product = Const(Complex.ONE);
            ComplexMatrix obtain(int index) => ObtainValue(split[index]);
            ComplexMatrix _a = obtain(0), _b = obtain(1), _c = obtain(2), _input = obtain(3);
            Parallel.For(0, row, r =>
            {
                Complex* prodPtr = product.RowPtr(r), sumPtr = sum.RowPtr(r), inputPtr = _input.RowPtr(r);
                Complex* aPtr = _a.RowPtr(r), bPtr = _b.RowPtr(r), cPtr = _c.RowPtr(r);
                for (int c = 0; c < column; c++, prodPtr++, sumPtr++, inputPtr++, aPtr++, bPtr++, cPtr++)
                {
                    for (int i = 0; i <= n; i++)
                    {
                        Complex temp = new(i - 1);
                        if (i != 0) *prodPtr *= *inputPtr * (*aPtr + temp) * (*bPtr + temp) / (*cPtr + temp) / new Complex(i);
                        *sumPtr += *prodPtr;
                    }
                }
            });
            return sum;
        }
        private unsafe ComplexMatrix Gamma(string[] split)
        {
            ThrowInvalidLengths(split, new int[] { 1, 2 });
            int n = split.Length == 2 ? RealSub.ToInt(split[1]) : 100;
            Complex tmp1 = Complex.ONE, tmpMG = new(-GAMMA);
            ComplexMatrix product = Const(tmp1), _input = ObtainValue(split[0]), output = new(row, column);
            Parallel.For(0, row, r =>
            {
                Complex* prodPtr = product.RowPtr(r), inputPtr = _input.RowPtr(r), outPtr = output.RowPtr(r);
                for (int c = 0; c < column; c++, prodPtr++, inputPtr++, outPtr++)
                {
                    for (int i = 1; i <= n; i++)
                    {
                        Complex temp = *inputPtr / new Complex(i);
                        *prodPtr *= Complex.Exp(temp) / (tmp1 + temp);
                    }
                    *outPtr = *prodPtr * Complex.Exp(tmpMG * *inputPtr) / *inputPtr;
                }
            });
            return output;
        }
        private unsafe ComplexMatrix Beta(string[] split)
        {
            ThrowInvalidLengths(split, new int[] { 2, 3 });
            int n = split.Length == 3 ? RealSub.ToInt(split[2]) : 100;
            Complex tmp1 = Complex.ONE;
            ComplexMatrix obtain(int index) => ObtainValue(split[index]);
            ComplexMatrix product = Const(tmp1), input1 = obtain(0), input2 = obtain(1), output = new(row, column);
            Parallel.For(0, row, r =>
            {
                Complex* prodPtr = product.RowPtr(r), outPtr = output.RowPtr(r);
                Complex* input1Ptr = input1.RowPtr(r), input2Ptr = input2.RowPtr(r);
                for (int c = 0; c < column; c++, prodPtr++, input1Ptr++, input2Ptr++, outPtr++)
                {
                    for (int i = 1; i <= n; i++)
                    {
                        Complex temp = new(i);
                        *prodPtr *= tmp1 + *input1Ptr * *input2Ptr / (temp * (temp + *input1Ptr + *input2Ptr));
                    }
                    *outPtr = (*input1Ptr + *input2Ptr) / (*input1Ptr * *input2Ptr) / *prodPtr;
                }
            });
            return output;
        }
        private unsafe ComplexMatrix Zeta(string[] split)
        {
            ThrowInvalidLengths(split, new int[] { 1, 2 });
            int n = split.Length == 2 ? RealSub.ToInt(split[1]) : 50;
            Complex tmp0 = Complex.ZERO, tmp1 = Complex.ONE, tmp2 = Complex.TWO;
            ComplexMatrix sum = new(row, column), Sum = new(row, column), Coefficient = Const(tmp1), coefficient = Const(tmp1);
            ComplexMatrix _input = ObtainValue(split[0]);
            Parallel.For(0, row, r =>
            {
                Complex* coeffPtr = coefficient.RowPtr(r), CoeffPtr = Coefficient.RowPtr(r);
                Complex* SumPtr = Sum.RowPtr(r), sumPtr = sum.RowPtr(r), inputPtr = _input.RowPtr(r);
                for (int c = 0; c < column; c++, CoeffPtr++, coeffPtr++, SumPtr++, sumPtr++, inputPtr++)
                {
                    for (int i = 0; i <= n; i++)
                    {
                        *CoeffPtr /= tmp2; *coeffPtr = tmp1; *SumPtr = tmp0;
                        for (int j = 0; j <= i; j++)
                        {
                            *SumPtr += *coeffPtr / ((new Complex(j + 1)) ^ *inputPtr);
                            *coeffPtr *= new Complex((double)(j - i) / (double)(j + 1)); // (double) is not redundant
                        }
                        *SumPtr *= *CoeffPtr; *sumPtr += *SumPtr;
                    }
                    *sumPtr /= tmp1 - (tmp2 ^ (tmp1 - *inputPtr));
                }
            });
            return sum;
        }

        private ComplexMatrix Sum(string[] split)
        {
            ThrowInvalidLengths(split, new int[] { 4 });
            ComplexSub Buffer = ObtainSub(ReplaceLoop(split, 0, 1, 0), new(row, column));
            For(RealSub.ToInt(split[2]), RealSub.ToInt(split[3]), i =>
            { Buffer.input = Recover(ReplaceLoop(split, 0, 1, i), true); Buffer.count = 0; Plus(Buffer.Obtain(), Buffer.Z); });
            return Buffer.Z;
        }
        private ComplexMatrix Product(string[] split)
        {
            ThrowInvalidLengths(split, new int[] { 4 });
            ComplexSub Buffer = ObtainSub(ReplaceLoop(split, 0, 1, 0), Const(Complex.ONE));
            For(RealSub.ToInt(split[2]), RealSub.ToInt(split[3]), i =>
            { Buffer.input = Recover(ReplaceLoop(split, 0, 1, i), true); Buffer.count = 0; Multiply(Buffer.Obtain(), Buffer.Z); });
            return Buffer.Z;
        }
        private ComplexMatrix Iterate(string[] split)
        {
            ThrowInvalidLengths(split, new int[] { 5 });
            ComplexSub Buffer = ObtainSub(ReplaceLoop(split, 0, 2, 0), ObtainValue(split[1]));
            For(RealSub.ToInt(split[3]), RealSub.ToInt(split[4]), i =>
            { Buffer.input = Recover(ReplaceLoop(split, 0, 2, i), true); Buffer.count = 0; Buffer.Z = Buffer.Obtain(); });
            return Buffer.Z;
        }
        private ComplexMatrix Composite(string[] split)
        {
            ComplexMatrix val = ObtainValue(split[0]);
            for (int i = 1; i < split.Length; i++) val = ObtainSub(split[i], val).Obtain();
            return val;
        }
        #endregion

        #region Elements
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe static ComplexMatrix InitilizeZ(RealMatrix x, RealMatrix y, int row, int column)
        {
            ComplexMatrix z = new(row, column);
            Parallel.For(0, row, i => {
                Complex* zPtr = z.RowPtr(i); double* xPtr = x.RowPtr(i), yPtr = y.RowPtr(i);
                for (int j = 0; j < column; j++, zPtr++, xPtr++, yPtr++) *zPtr = new(*xPtr, *yPtr);
            });
            return z;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe ComplexMatrix Const(Complex c)
        {
            ComplexMatrix output = new(row, column); Complex* srcPtr = output.Ptr();
            for (int q = 0; q < column; q++, srcPtr++) *srcPtr = c; srcPtr = output.Ptr();
            Parallel.For(1, row, p => { Unsafe.CopyBlock(output.RowPtr(p), srcPtr, columnSIZE); });
            return output;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe ComplexMatrix Copy(ComplexMatrix src)
        {
            ComplexMatrix output = new(row, column);
            Parallel.For(0, span, p => { Unsafe.CopyBlock(output.RowPtr(p * STEP), src.RowPtr(p * STEP), strideSIZE); });
            if (residueSIZE != 0) Unsafe.CopyBlock(output.RowPtr(bulk), src.RowPtr(bulk), residueSIZE);
            return output;
        } // Passing matrices to mutable variables
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe ComplexMatrix Negate(ComplexMatrix src)
        {
            ComplexMatrix output = new(row, column);
            Parallel.For(0, row, p => {
                Complex* destPtr = output.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr = -*srcPtr;
            });
            return output;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Plus(ComplexMatrix src, ComplexMatrix dest) => Parallel.For(0, row, p =>
        {
            Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr += *srcPtr;
        });
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Subtract(ComplexMatrix src, ComplexMatrix dest) => Parallel.For(0, row, p =>
        {
            Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr -= *srcPtr;
        });
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Multiply(ComplexMatrix src, ComplexMatrix dest) => Parallel.For(0, row, p =>
        {
            Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr *= *srcPtr;
        });
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Divide(ComplexMatrix src, ComplexMatrix dest) => Parallel.For(0, row, p =>
        {
            Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr /= *srcPtr;
        });
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Power(ComplexMatrix src, ComplexMatrix dest) => Parallel.For(0, row, p =>
        {
            Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr = *srcPtr ^ *destPtr;
        });
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void FuncSub(ComplexMatrix val, Func<Complex, Complex> function) => Parallel.For(0, row, r =>
        {
            Complex* valuePtr = val.RowPtr(r);
            for (int c = 0; c < column; c++, valuePtr++) *valuePtr = function(*valuePtr);
        });
        #endregion

        #region Assembly
        private ComplexMatrix Transform(string input) => input[0] switch
        {
            SB => braValues[Int32.Parse(TryBraNum(input))],
            _Z => z,
            Z_ => Z,
            I => Const(Complex.I),
            E => Const(new(Math.E)),
            P => Const(new(Math.PI)),
            G => Const(new(GAMMA)),
            _ => Const(new(Double.Parse(input)))
        };
        private ComplexMatrix BreakPower(string input)
        {
            string[] power_chunks = PrepareBreakPower(input, THRESHOLD);
            ComplexMatrix tower = Copy(PowerCore(power_chunks[^1]));
            for (int k = power_chunks.Length - 2; k >= 0; k--)
            {
                string[] power_split = SplitByChars(power_chunks[k], "^"); // Special for power
                for (int m = power_split.Length - 1; m >= 0; m--) Power(Transform(power_split[m]), tower);
            }
            return tower;
        }
        private ComplexMatrix PowerCore(string input)
        {
            if (!input.Contains('^')) return Transform(input);
            if (CountChars(input, "^") > THRESHOLD) return BreakPower(input);

            string[] power_split = SplitByChars(input, "^"); ComplexMatrix tower = Copy(Transform(power_split[^1]));
            for (int k = power_split.Length - 2; k >= 0; k--) Power(Transform(power_split[k]), tower);
            return tower;
        }
        private ComplexMatrix BreakMultiplyDivide(string input)
        {
            var (prod_chunks, signs) = PrepareBreakPSMD(String.Concat('*', input), "*/", '*', THRESHOLD);
            ComplexMatrix product = Copy(MultiplyDivideCore(TrimStartChar(prod_chunks[0], '*')));
            for (int j = 1; j < prod_chunks.Length; j++)
                Multiply(MultiplyDivideCore(signs[j - 1] == SUB_CHARS[0] ? prod_chunks[j] : String.Concat("1/", prod_chunks[j])), product);
            return product;
        }
        private ComplexMatrix MultiplyDivideCore(string input)
        {
            if (!ContainsAny(input, "*/")) return PowerCore(input);
            if (CountChars(input, "*/") > THRESHOLD) return BreakMultiplyDivide(input);

            var (product_split, mdBuilder) = GetMultiplyDivideComponents(input);
            ComplexMatrix product = Copy(PowerCore(product_split[0]));
            for (int j = 1; j < product_split.Length; j++)
            {
                Action<ComplexMatrix, ComplexMatrix> operation = mdBuilder[j - 1] == '*' ? Multiply : Divide;
                operation(PowerCore(product_split[j]), product);
            }
            return product;
        }
        private ComplexMatrix BreakPlusSubtract(string input)
        {
            var (sum_chunks, signs) = PrepareBreakPSMD(input[0] == '-' ? input : String.Concat('+', input), "+-", '+', THRESHOLD);
            ComplexMatrix sum = Copy(PlusSubtractCore(TrimStartChar(sum_chunks[0], '+')));
            for (int i = 1; i < sum_chunks.Length; i++)
                Plus(PlusSubtractCore(signs[i - 1] == SUB_CHARS[0] ? sum_chunks[i] : String.Concat('-', sum_chunks[i])), sum);
            return sum;
        }
        private ComplexMatrix PlusSubtractCore(string input)
        {
            if (!ContainsAny(input, "+-")) return MultiplyDivideCore(input);
            if (CountChars(input, "+-") > THRESHOLD) return BreakPlusSubtract(input);

            var (sum_split, psBuilder) = GetPlusSubtractComponents(input);
            Func<ComplexMatrix, ComplexMatrix> function = psBuilder[0] == '+' ? Copy : Negate; // Special for "+-"
            ComplexMatrix sum = function(MultiplyDivideCore(sum_split[0]));
            for (int i = 1; i < sum_split.Length; i++)
            {
                Action<ComplexMatrix, ComplexMatrix> operation = psBuilder[i] == '+' ? Plus : Subtract;
                operation(MultiplyDivideCore(sum_split[i]), sum);
            }
            return sum;
        }
        private ComplexMatrix ComputeBraFreePart(string input)
        {
            if (Int32.TryParse(input, out int result)) return Const(new(result)); // Double.TryParse is much slower
            if (input[0] == SB && Int32.TryParse(TryBraNum(input), out int _result)) return braValues[_result];
            return Copy(PlusSubtractCore(input)); // Necessary
        }

        private string SeriesSub(string input)
        {
            var (i, end, temp) = PrepareSeriesSub(input);
            Func<string[], ComplexMatrix> braFunc = input[i - 1] switch
            {
                F_ => Hypergeometric,
                G_ => Gamma,
                B_ => Beta,
                _Z_ => Zeta,
                S_ => Sum,
                P_ => Product,
                I_ => Iterate,
                J_ => Composite
            };
            braValues[count] = braFunc(ReplaceRecover(temp));
            return Replace(input, BracketSub(count++), i - 2, end);
        }
        private void SubCore(string temp, int begin, ComplexMatrix subValue, ref int tagL)
        {
            if (begin == 0) return;
            bool isA = begin > 1 ? temp[begin - 2] != _A : false; // Should not simplify
            switch (temp[begin - 1])
            {
                case _S: FuncSub(subValue, isA ? Complex.Sin : Complex.Asin); tagL = isA ? 1 : 2; break;
                case _C: FuncSub(subValue, isA ? Complex.Cos : Complex.Acos); tagL = isA ? 1 : 2; break;
                case _T: FuncSub(subValue, isA ? Complex.Tan : Complex.Atan); tagL = isA ? 1 : 2; break;
                case _H:
                    bool IsA = temp[begin - 3] != _A; // Needn't check because of ~
                    FuncSub(subValue, temp[begin - 2] switch
                    {
                        _S => IsA ? Complex.Sinh : Complex.Asinh,
                        _C => IsA ? Complex.Cosh : Complex.Acosh,
                        _T => IsA ? Complex.Tanh : Complex.Atanh
                    }); tagL = IsA ? 2 : 3; break;
                case _A: FuncSub(subValue, c => new(Complex.Modulus(c))); tagL = 1; break;
                case J_: FuncSub(subValue, Complex.Conjugate); tagL = 1; break;
                case I_: FuncSub(subValue, Complex.Log); tagL = 1; break;
                case E_: FuncSub(subValue, Complex.Exp); tagL = 1; break;
                case SP: FuncSub(subValue, Complex.Ei); tagL = 2; break; // Special for complex
                case _Q: FuncSub(subValue, Complex.Sqrt); tagL = 1; break;
                case _F_: FuncSub(subValue, Complex.Factorial); tagL = 1; break;
                default: break;
            }
        }
        public ComplexMatrix Obtain()
        {
            if (!input.Contains('(')) return ComputeBraFreePart(input);
            string temp = input; ComplexMatrix subValue;
            while (temp.Contains(UNDERLINE)) temp = SeriesSub(temp);

            var (length, begin, end, tagL) = PrepareLoop(temp);
            for (int i = 0; i < length; i++)
            {
                ResetBeginEnd(temp, ref begin, ref end);
                subValue = ComputeBraFreePart(BraFreePart(temp, begin, end));
                SubCore(temp, begin, subValue, ref tagL);
                braValues[count] = subValue;
                SubstituteTemp(ref temp, ref begin, end, ref tagL, ref count);
            }
            return ComputeBraFreePart(temp);
        }
        #endregion
    } /// Computing complex-variable expressions
    public class RealSub : RecoverMultiply
    {
        #region Fields & Constructors
        private static readonly int STRUCTSIZE = Marshal.SizeOf<Double>();
        private readonly uint columnSIZE, strideSIZE, residueSIZE; // For copying
        private readonly int row, column, span, bulk; // For parallel copy chunks
        private readonly RealMatrix x, y;
        private readonly RealMatrix[] braValues; // To store values between parentheses pairs

        private int count; // To log parentheses
        private RealMatrix X, Y; // For substitution
        private string input;

        public RealSub(string input, RealMatrix? x, RealMatrix? y, RealMatrix? X, RealMatrix? Y, int row, int column)
        {
            ThrowException(String.IsNullOrEmpty(input));
            this.input = Recover(input, false); braValues = new RealMatrix[CountChars(this.input, "(")];
            this.row = row; this.column = column; span = row / STEP; bulk = span * STEP; int temp = column * STRUCTSIZE;
            columnSIZE = (uint)temp; strideSIZE = (uint)(temp * STEP); residueSIZE = (uint)(temp * (row - bulk));

            if (x != null) this.x = (RealMatrix)x; if (y != null) this.y = (RealMatrix)y;
            if (X != null) this.X = (RealMatrix)X; if (Y != null) this.Y = (RealMatrix)Y;
        }
        private RealSub ObtainSub(string input, RealMatrix? X, RealMatrix? Y) => new(input, x, y, X, Y, row, column);
        private RealMatrix ObtainValue(string input) => new RealSub(input, x, y, X, Y, row, column).Obtain();
        public static double Obtain(string input, double x = 0.0) => new RealSub(input, new(x), null, null, null, 1, 1).Obtain()[0, 0];
        public static int ToInt(string input) => (int)Obtain(input); // Often bound to MyString.For
        #endregion

        #region Basic Calculations
        public static double Factorial(double n) => n < 0 ? Double.NaN : (Math.Floor(n) == 0 ? 1 : Math.Floor(n) * Factorial(n - 1));
        private static double Mod(double a, double n) => n != 0 ? a % Math.Abs(n) : Double.NaN;
        private static double Combination(double n, double r)
        {
            if (n == r || r == 0) return 1;
            else if (r > n && n >= 0 || 0 > r && r > n || n >= 0 && 0 > r) return 0;
            else if (n > 0) return Combination(n - 1, r - 1) + Combination(n - 1, r);
            else if (r > 0) return Combination(n + 1, r) - Combination(n, r - 1);
            else return Combination(n + 1, r + 1) - Combination(n, r + 1);
        } // Generalized Pascal's triangle
        private static double Permutation(double n, double r)
        {
            if (r < 0) return 0;
            else if (r == 0) return 1;
            else return (n - r + 1) * Permutation(n, r - 1);
        }

        private unsafe RealMatrix ProcessMCP(string[] split, Func<double, double, double> function)
        {
            ThrowInvalidLengths(split, new int[] { 2 });
            RealMatrix input1 = ObtainValue(split[0]), input2 = ObtainValue(split[1]), output = new(row, column);
            Parallel.For(0, row, r => {
                double* input1Ptr = input1.RowPtr(r), input2Ptr = input2.RowPtr(r), outPtr = output.RowPtr(r);
                for (int c = 0; c < column; c++, outPtr++, input1Ptr++, input2Ptr++) *outPtr = function(*input1Ptr, *input2Ptr);
            });
            return output;
        }
        private unsafe RealMatrix ProcessMinMax(string[] split, Func<double[], double> function)
        {
            RealMatrix[] val = new RealMatrix[split.Length];
            for (int i = 0; i < val.Length; i++) val[i] = ObtainValue(split[i]);

            RealMatrix output = new(row, column);
            Parallel.For(0, row, () => new double[split.Length], (r, state, minMax) =>
            {
                double* outputPtr = output.RowPtr(r);
                for (int c = 0; c < column; c++, outputPtr++)
                {
                    for (int i = 0; i < split.Length; i++) minMax[i] = val[i][r, c];
                    *outputPtr = function(minMax);
                }
                return minMax;
            }, _ => { });
            return output;
        }

        private RealMatrix Mod(string[] split) => ProcessMCP(split, (a, b) => Mod(a, b));
        private RealMatrix Combination(string[] split) => ProcessMCP(split, (a, b) => Combination(Math.Floor(a), Math.Floor(b)));
        private RealMatrix Permutation(string[] split) => ProcessMCP(split, (a, b) => Permutation(Math.Floor(a), Math.Floor(b)));
        private RealMatrix Max(string[] split) => ProcessMinMax(split, val => val.Max());
        private RealMatrix Min(string[] split) => ProcessMinMax(split, val => val.Min());
        #endregion

        #region Additional Calculations
        private unsafe RealMatrix Hypergeometric(string[] split)
        {
            ThrowInvalidLengths(split, new int[] { 4, 5 });
            int n = split.Length == 5 ? ToInt(split[4]) : 100;
            RealMatrix sum = new(row, column), product = Const(1);
            RealMatrix obtain(int index) => ObtainValue(split[index]);
            RealMatrix _a = obtain(0), _b = obtain(1), _c = obtain(2), _input = obtain(3);
            Parallel.For(0, row, r =>
            {
                double* prodPtr = product.RowPtr(r), sumPtr = sum.RowPtr(r), inputPtr = _input.RowPtr(r);
                double* aPtr = _a.RowPtr(r), bPtr = _b.RowPtr(r), cPtr = _c.RowPtr(r);
                for (int c = 0; c < column; c++, prodPtr++, sumPtr++, inputPtr++, aPtr++, bPtr++, cPtr++)
                {
                    for (int i = 0; i <= n; i++)
                    {
                        double temp = i - 1;
                        if (i != 0) *prodPtr *= *inputPtr * (*aPtr + temp) * (*bPtr + temp) / (*cPtr + temp) / i;
                        *sumPtr += *prodPtr;
                    }
                }
            });
            return sum;
        }
        private unsafe RealMatrix Gamma(string[] split)
        {
            ThrowInvalidLengths(split, new int[] { 1, 2 });
            int n = split.Length == 2 ? ToInt(split[1]) : 100;
            RealMatrix product = Const(1), _input = ObtainValue(split[0]), output = new(row, column);
            Parallel.For(0, row, r =>
            {
                double* prodPtr = product.RowPtr(r), inputPtr = _input.RowPtr(r), outPtr = output.RowPtr(r);
                for (int c = 0; c < column; c++, prodPtr++, inputPtr++, outPtr++)
                {
                    for (int i = 1; i <= n; i++)
                    {
                        double temp = *inputPtr / i;
                        *prodPtr *= Math.Exp(temp) / (1 + temp);
                    }
                    *outPtr = *prodPtr * Math.Exp(-GAMMA * *inputPtr) / *inputPtr;
                }
            });
            return output;
        }
        private unsafe RealMatrix Beta(string[] split)
        {
            ThrowInvalidLengths(split, new int[] { 2, 3 });
            int n = split.Length == 3 ? ToInt(split[2]) : 100;
            RealMatrix obtain(int index) => ObtainValue(split[index]);
            RealMatrix product = Const(1), input1 = obtain(0), input2 = obtain(1), output = new(row, column);
            Parallel.For(0, row, r =>
            {
                double* prodPtr = product.RowPtr(r), outPtr = output.RowPtr(r);
                double* input1Ptr = input1.RowPtr(r), input2Ptr = input2.RowPtr(r);
                for (int c = 0; c < column; c++, prodPtr++, input1Ptr++, input2Ptr++, outPtr++)
                {
                    for (int i = 1; i <= n; i++) *prodPtr *= 1 + *input1Ptr * *input2Ptr / (i * (i + *input1Ptr + *input2Ptr));
                    *outPtr = (*input1Ptr + *input2Ptr) / (*input1Ptr * *input2Ptr) / *prodPtr;
                }
            });
            return output;
        }
        private unsafe RealMatrix Zeta(string[] split)
        {
            ThrowInvalidLengths(split, new int[] { 1, 2 });
            int n = split.Length == 2 ? ToInt(split[1]) : 50;
            RealMatrix sum = new(row, column), Sum = new(row, column), Coefficient = Const(1), coefficient = Const(1);
            RealMatrix _input = ObtainValue(split[0]);
            Parallel.For(0, row, r =>
            {
                double* coeffPtr = coefficient.RowPtr(r), CoeffPtr = Coefficient.RowPtr(r);
                double* SumPtr = Sum.RowPtr(r), sumPtr = sum.RowPtr(r), inputPtr = _input.RowPtr(r);
                for (int c = 0; c < column; c++, CoeffPtr++, coeffPtr++, SumPtr++, sumPtr++, inputPtr++)
                {
                    for (int i = 0; i <= n; i++)
                    {
                        *CoeffPtr /= 2; *coeffPtr = 1; *SumPtr = 0;
                        for (int j = 0; j <= i; j++)
                        {
                            *SumPtr += *coeffPtr / Math.Pow(j + 1, *inputPtr);
                            *coeffPtr *= (double)(j - i) / (double)(j + 1); // (double) is not redundant
                        }
                        *SumPtr *= *CoeffPtr; *sumPtr += *SumPtr;
                    }
                    *sumPtr /= 1 - Math.Pow(2, 1 - *inputPtr);
                }
            });
            return sum;
        }

        private RealMatrix Sum(string[] split)
        {
            ThrowInvalidLengths(split, new int[] { 4 });
            RealSub Buffer = ObtainSub(ReplaceLoop(split, 0, 1, 0), new(row, column), null);
            For(ToInt(split[2]), ToInt(split[3]), i =>
            { Buffer.input = Recover(ReplaceLoop(split, 0, 1, i), false); Buffer.count = 0; Plus(Buffer.Obtain(), Buffer.X); });
            return Buffer.X;
        }
        private RealMatrix Product(string[] split)
        {
            ThrowInvalidLengths(split, new int[] { 4 });
            RealSub Buffer = ObtainSub(ReplaceLoop(split, 0, 1, 0), Const(1), null);
            For(ToInt(split[2]), ToInt(split[3]), i =>
            { Buffer.input = Recover(ReplaceLoop(split, 0, 1, i), false); Buffer.count = 0; Multiply(Buffer.Obtain(), Buffer.X); });
            return Buffer.X;
        }
        private RealMatrix Iterate1(string[] split)
        {
            ThrowInvalidLengths(split, new int[] { 5 });
            RealSub Buffer = ObtainSub(ReplaceLoop(split, 0, 2, 0), ObtainValue(split[1]), null);
            For(ToInt(split[3]), ToInt(split[4]), i =>
            { Buffer.input = Recover(ReplaceLoop(split, 0, 2, i), false); Buffer.count = 0; Buffer.X = Buffer.Obtain(); });
            return Buffer.X;
        }
        private RealMatrix Iterate2(string[] split)
        {
            ThrowInvalidLengths(split, new int[] { 8 });
            RealSub obtainSub(string s) => ObtainSub(s, ObtainValue(split[2]), ObtainValue(split[3]));
            RealSub Buffer1 = obtainSub(ReplaceLoop(split, 0, 4, 0)), Buffer2 = obtainSub(ReplaceLoop(split, 1, 4, 0));

            string obtainInput(int s, int i) => Recover(ReplaceLoop(split, s, 4, i), false);
            For(ToInt(split[5]), ToInt(split[6]), i =>
            {
                Buffer1.input = obtainInput(0, i); Buffer2.input = obtainInput(1, i);
                Buffer1.count = Buffer2.count = 0;
                RealMatrix temp1 = Buffer1.Obtain(), temp2 = Buffer2.Obtain(); // Necessary
                Buffer1.X = Buffer2.X = temp1; Buffer1.Y = Buffer2.Y = temp2;
            });
            return ModeChooser(split[^1], Buffer1.X, Buffer1.Y, row, column);
        }
        private RealMatrix Composite1(string[] split)
        {
            RealMatrix val = ObtainValue(split[0]);
            for (int i = 1; i < split.Length; i++) val = ObtainSub(split[i], val, null).Obtain();
            return val;
        }
        private RealMatrix Composite2(string[] split)
        {
            ThrowException(split.Length % 2 == 0); int length = split.Length / 2 - 1;
            string[] comp_1 = new string[length], comp_2 = new string[length];
            for (int i = 0, j = 2; i < length; i++) { comp_1[i] = split[j++]; comp_2[i] = split[j++]; }

            RealMatrix val_1 = ObtainValue(split[0]), val_2 = ObtainValue(split[1]);
            for (int i = 0; i < length; i++)
            {
                RealMatrix temp_1 = val_1, temp_2 = val_2; // Necessary
                RealMatrix getValue(string s) => ObtainSub(s, temp_1, temp_2).Obtain();
                val_1 = getValue(comp_1[i]); val_2 = getValue(comp_2[i]);
            }
            return ModeChooser(split[^1], val_1, val_2, row, column);
        }
        #endregion

        #region Elements
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe RealMatrix Const(double c)
        {
            RealMatrix output = new(row, column); double* srcPtr = output.Ptr();
            for (int q = 0; q < column; q++, srcPtr++) *srcPtr = c; srcPtr = output.Ptr();
            Parallel.For(1, row, p => { Unsafe.CopyBlock(output.RowPtr(p), srcPtr, columnSIZE); });
            return output;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe RealMatrix Copy(RealMatrix src)
        {
            RealMatrix output = new(row, column);
            Parallel.For(0, span, p => { Unsafe.CopyBlock(output.RowPtr(p * STEP), src.RowPtr(p * STEP), strideSIZE); });
            if (residueSIZE != 0) Unsafe.CopyBlock(output.RowPtr(bulk), src.RowPtr(bulk), residueSIZE);
            return output;
        } // Passing matrices to mutable variables
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe RealMatrix Negate(RealMatrix src)
        {
            RealMatrix output = new(row, column);
            Parallel.For(0, row, p => {
                double* destPtr = output.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr = -*srcPtr;
            });
            return output;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Plus(RealMatrix src, RealMatrix dest) => Parallel.For(0, row, p =>
        {
            double* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr += *srcPtr;
        });
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Subtract(RealMatrix src, RealMatrix dest) => Parallel.For(0, row, p =>
        {
            double* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr -= *srcPtr;
        });
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Multiply(RealMatrix src, RealMatrix dest) => Parallel.For(0, row, p =>
        {
            double* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr *= *srcPtr;
        });
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Divide(RealMatrix src, RealMatrix dest) => Parallel.For(0, row, p =>
        {
            double* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr /= *srcPtr;
        });
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Power(RealMatrix src, RealMatrix dest) => Parallel.For(0, row, p =>
        {
            double* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < column; q++, destPtr++, srcPtr++) *destPtr = Math.Pow(*srcPtr, *destPtr);
        });
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void FuncSub(RealMatrix val, Func<double, double> function) => Parallel.For(0, row, r =>
        {
            double* valuePtr = val.RowPtr(r);
            for (int c = 0; c < column; c++, valuePtr++) *valuePtr = function(*valuePtr);
        });
        #endregion

        #region Assembly
        private RealMatrix Transform(string input) => input[0] switch
        {
            SB => braValues[Int32.Parse(TryBraNum(input))],
            _X => x,
            _Y => y,
            X_ => X,
            Y_ => Y,
            E => Const(Math.E),
            P => Const(Math.PI),
            G => Const(GAMMA),
            _ => Const(Double.Parse(input))
        };
        private RealMatrix BreakPower(string input)
        {
            string[] power_chunks = PrepareBreakPower(input, THRESHOLD);
            RealMatrix tower = Copy(PowerCore(power_chunks[^1]));
            for (int k = power_chunks.Length - 2; k >= 0; k--)
            {
                string[] power_split = SplitByChars(power_chunks[k], "^"); // Special for power
                for (int m = power_split.Length - 1; m >= 0; m--) Power(Transform(power_split[m]), tower);
            }
            return tower;
        }
        private RealMatrix PowerCore(string input)
        {
            if (!input.Contains('^')) return Transform(input);
            if (CountChars(input, "^") > THRESHOLD) return BreakPower(input);

            string[] power_split = SplitByChars(input, "^"); RealMatrix tower = Copy(Transform(power_split[^1]));
            for (int k = power_split.Length - 2; k >= 0; k--) Power(Transform(power_split[k]), tower);
            return tower;
        }
        private RealMatrix BreakMultiplyDivide(string input)
        {
            var (prod_chunks, signs) = PrepareBreakPSMD(String.Concat('*', input), "*/", '*', THRESHOLD);
            RealMatrix product = Copy(MultiplyDivideCore(TrimStartChar(prod_chunks[0], '*')));
            for (int j = 1; j < prod_chunks.Length; j++)
                Multiply(MultiplyDivideCore(signs[j - 1] == SUB_CHARS[0] ? prod_chunks[j] : String.Concat("1/", prod_chunks[j])), product);
            return product;
        }
        private RealMatrix MultiplyDivideCore(string input)
        {
            if (!ContainsAny(input, "*/")) return PowerCore(input);
            if (CountChars(input, "*/") > THRESHOLD) return BreakMultiplyDivide(input);

            var (product_split, mdBuilder) = GetMultiplyDivideComponents(input);
            RealMatrix product = Copy(PowerCore(product_split[0]));
            for (int j = 1; j < product_split.Length; j++)
            {
                Action<RealMatrix, RealMatrix> operation = mdBuilder[j - 1] == '*' ? Multiply : Divide;
                operation(PowerCore(product_split[j]), product);
            }
            return product;
        }
        private RealMatrix BreakPlusSubtract(string input)
        {
            var (sum_chunks, signs) = PrepareBreakPSMD(input[0] == '-' ? input : String.Concat('+', input), "+-", '+', THRESHOLD);
            RealMatrix sum = Copy(PlusSubtractCore(TrimStartChar(sum_chunks[0], '+')));
            for (int i = 1; i < sum_chunks.Length; i++)
                Plus(PlusSubtractCore(signs[i - 1] == SUB_CHARS[0] ? sum_chunks[i] : String.Concat('-', sum_chunks[i])), sum);
            return sum;
        }
        private RealMatrix PlusSubtractCore(string input)
        {
            if (!ContainsAny(input, "+-")) return MultiplyDivideCore(input);
            if (CountChars(input, "+-") > THRESHOLD) return BreakPlusSubtract(input);

            var (sum_split, psBuilder) = GetPlusSubtractComponents(input);
            Func<RealMatrix, RealMatrix> function = psBuilder[0] == '+' ? Copy : Negate; // Special for "+-"
            RealMatrix sum = function(MultiplyDivideCore(sum_split[0]));
            for (int i = 1; i < sum_split.Length; i++)
            {
                Action<RealMatrix, RealMatrix> operation = psBuilder[i] == '+' ? Plus : Subtract;
                operation(MultiplyDivideCore(sum_split[i]), sum);
            }
            return sum;
        }
        private RealMatrix ComputeBraFreePart(string input)
        {
            if (Int32.TryParse(input, out int result)) return Const(result); // Double.TryParse is much slower
            if (input[0] == SB && Int32.TryParse(TryBraNum(input), out int _result)) return braValues[_result];
            return Copy(PlusSubtractCore(input)); // Necessary
        }

        private string SeriesSub(string input)
        {
            var (i, end, temp) = PrepareSeriesSub(input);
            Func<string[], RealMatrix> braFunc = input[i - 1] switch
            {
                M_ => Mod,
                C_ => Combination,
                A_ => Permutation,
                MAX => Max,
                MIN => Min,
                F_ => Hypergeometric,
                G_ => Gamma,
                B_ => Beta,
                _Z_ => Zeta,
                S_ => Sum,
                P_ => Product,
                I_ when input[i - 2] == MODE_1 => Iterate1,
                I_ when input[i - 2] == MODE_2 => Iterate2,
                J_ when input[i - 2] == MODE_1 => Composite1,
                J_ when input[i - 2] == MODE_2 => Composite2
            };
            braValues[count] = braFunc(ReplaceRecover(temp));
            return Replace(input, BracketSub(count++), i - (IJ_.Contains(input[i - 1]) ? 3 : 2), end);
        }
        private void SubCore(string temp, int begin, RealMatrix subValue, ref int tagL)
        {
            if (begin == 0) return;
            bool isA = begin > 1 ? temp[begin - 2] != _A : false; // Should not simplify
            switch (temp[begin - 1])
            {
                case _S: FuncSub(subValue, isA ? Math.Sin : Math.Asin); tagL = isA ? 1 : 2; break;
                case _C: FuncSub(subValue, isA ? Math.Cos : Math.Acos); tagL = isA ? 1 : 2; break;
                case _T: FuncSub(subValue, isA ? Math.Tan : Math.Atan); tagL = isA ? 1 : 2; break;
                case _H:
                    bool IsA = temp[begin - 3] != _A; // Needn't check ahead because of ~
                    FuncSub(subValue, temp[begin - 2] switch
                    {
                        _S => IsA ? Math.Sinh : Math.Asinh,
                        _C => IsA ? Math.Cosh : Math.Acosh,
                        _T => IsA ? Math.Tanh : Math.Atanh
                    }); tagL = IsA ? 2 : 3; break;
                case _A: FuncSub(subValue, Math.Abs); tagL = 1; break;
                case _L: FuncSub(subValue, Math.Log); tagL = 1; break;
                case E_: FuncSub(subValue, Math.Exp); tagL = 1; break;
                case _Q: FuncSub(subValue, Math.Sqrt); tagL = 1; break;
                case _F_: FuncSub(subValue, Factorial); tagL = 1; break;
                case _D_: // Special for real
                    FuncSub(subValue, temp[begin - 2] switch
                    {
                        _F => Math.Floor,
                        _C => Math.Ceiling,
                        _R => Math.Round,
                        _S => r => (double)Math.Sign(r) // (double) is not redundant
                    }); tagL = 2; break;
                default: break;
            }
        }
        public RealMatrix Obtain()
        {
            if (!input.Contains('(')) return ComputeBraFreePart(input);
            string temp = input; RealMatrix subValue;
            while (temp.Contains(UNDERLINE)) temp = SeriesSub(temp);

            var (length, begin, end, tagL) = PrepareLoop(temp);
            for (int i = 0; i < length; i++)
            {
                ResetBeginEnd(temp, ref begin, ref end);
                subValue = ComputeBraFreePart(BraFreePart(temp, begin, end));
                SubCore(temp, begin, subValue, ref tagL);
                braValues[count] = subValue;
                SubstituteTemp(ref temp, ref begin, end, ref tagL, ref count);
            }
            return ComputeBraFreePart(temp);
        }
        #endregion
    } /// Computing real-variable expressions

    /// <summary>
    /// STRUCTURE SECTION
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Complex // Manually inlinined to reduce overhead
    {
        public readonly double real, imaginary;
        public static readonly Complex ZERO = new(0), HALF = new(0.5), ONE = new(1), TWO = new(2), I = new(0, 1), TWOI = new(0, 2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Complex(double real, double imaginary = 0.0) { this.real = real; this.imaginary = imaginary; }

        #region Operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator -(Complex c) => new(-c.real, -c.imaginary);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator +(Complex c1, Complex c2) => new(c1.real + c2.real, c1.imaginary + c2.imaginary);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator -(Complex c1, Complex c2) => new(c1.real - c2.real, c1.imaginary - c2.imaginary);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator *(Complex c1, Complex c2)
        {
            double re1 = c1.real, im1 = c1.imaginary, re2 = c2.real, im2 = c2.imaginary;
            return new(re1 * re2 - im1 * im2, re1 * im2 + im1 * re2);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator /(Complex c1, Complex c2)
        {
            double re1 = c1.real, im1 = c1.imaginary, re2 = c2.real, im2 = c2.imaginary, mod = re2 * re2 + im2 * im2;
            return new((re1 * re2 + im1 * im2) / mod, (im1 * re2 - re1 * im2) / mod);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator ^(Complex c1, Complex c2)
        {
            double re = c1.real, im = c1.imaginary;
            if (re == 0 && im == 0) return ZERO; // Necessary apriori checking
            Complex c3 = c2 * new Complex(Math.Log(re * re + im * im) / 2, Math.Atan2(im, re));
            double mod = Math.Exp(c3.real); var unit = Math.SinCos(c3.imaginary);
            return new(mod * unit.Cos, mod * unit.Sin);
        }
        #endregion

        #region Elementary Functions
        public static Complex Log(Complex c)
        {
            double re = c.real, im = c.imaginary;
            return new(Math.Log(re * re + im * im) / 2, Math.Atan2(im, re));
        }
        public static Complex Exp(Complex c)
        {
            double mod = Math.Exp(c.real); var unit = Math.SinCos(c.imaginary);
            return new(mod * unit.Cos, mod * unit.Sin);
        }
        public static Complex Ei(Complex c)
        {
            double mod = Math.Exp(-Math.Tau * c.imaginary); var unit = Math.SinCos(Math.Tau * c.real);
            return new(mod * unit.Cos, mod * unit.Sin);
        }
        public static Complex Sin(Complex c)
        {
            double mod = Math.Exp(-c.imaginary); var unit = Math.SinCos(c.real);
            Complex c0 = new(mod * unit.Cos, mod * unit.Sin); return (c0 - ONE / c0) / TWOI;
        }
        public static Complex Cos(Complex c)
        {
            double mod = Math.Exp(-c.imaginary); var unit = Math.SinCos(c.real);
            Complex c0 = new(mod * unit.Cos, mod * unit.Sin); return (c0 + ONE / c0) / TWO;
        }
        public static Complex Tan(Complex c)
        {
            double mod = Math.Exp(-2 * c.imaginary); var unit = Math.SinCos(2 * c.real);
            Complex c0 = new(mod * unit.Cos, mod * unit.Sin); return I * (-ONE + TWO / (ONE + c0));
        }
        public static Complex Asin(Complex c) // Remind the parentheses
        {
            Complex c0 = I * c + ((ONE - c * c) ^ HALF); double re = c0.real, im = c0.imaginary;
            return new(Math.Atan2(im, re), -Math.Log(re * re + im * im) / 2);
        }
        public static Complex Acos(Complex c) // Remind the parentheses
        {
            Complex c0 = I * c + ((ONE - c * c) ^ HALF); double re = c0.real, im = c0.imaginary;
            return new(Math.PI / 2 - Math.Atan2(im, re), Math.Log(re * re + im * im) / 2);
        } // Wolfram convention
        public static Complex Atan(Complex c)
        {
            Complex c0 = -ONE + TWOI / (I + c); double re = c0.real, im = c0.imaginary;
            return new(Math.Atan2(im, re) / 2, -Math.Log(re * re + im * im) / 4);
        }
        public static Complex Sinh(Complex c)
        {
            double mod = Math.Exp(c.real); var unit = Math.SinCos(c.imaginary);
            Complex c0 = new(mod * unit.Cos, mod * unit.Sin); return (c0 - ONE / c0) / TWO;
        }
        public static Complex Cosh(Complex c)
        {
            double mod = Math.Exp(c.real); var unit = Math.SinCos(c.imaginary);
            Complex c0 = new(mod * unit.Cos, mod * unit.Sin); return (c0 + ONE / c0) / TWO;
        }
        public static Complex Tanh(Complex c)
        {
            double mod = Math.Exp(2 * c.real); var unit = Math.SinCos(2 * c.imaginary);
            Complex c0 = new(mod * unit.Cos, mod * unit.Sin); return ONE - TWO / (ONE + c0);
        }
        public static Complex Asinh(Complex c) // Remind the parentheses
        {
            Complex c0 = c + ((ONE + c * c) ^ HALF); double re = c0.real, im = c0.imaginary;
            return new(Math.Log(re * re + im * im) / 2, Math.Atan2(im, re));
        }
        public static Complex Acosh(Complex c) // Remind the parentheses
        {
            Complex c0 = c + ((c + ONE) ^ HALF) * ((c - ONE) ^ HALF); double re = c0.real, im = c0.imaginary;
            return new(Math.Log(re * re + im * im) / 2, Math.Atan2(im, re));
        } // Wolfram convention
        public static Complex Atanh(Complex c)
        {
            Complex c0 = -ONE + TWO / (ONE - c); double re = c0.real, im = c0.imaginary;
            return new(Math.Log(re * re + im * im) / 4, Math.Atan2(im, re) / 2);
        }
        public static Complex Sqrt(Complex c) => c ^ HALF;
        public static double Modulus(double x, double y) => Modulus(new(x, y));
        public static double Modulus(Complex c) => Math.Sqrt(c.real * c.real + c.imaginary * c.imaginary);
        public static Complex Conjugate(Complex c) => new(c.real, -c.imaginary);
        public static Complex Factorial(Complex c) => new(RealSub.Factorial(c.real));
        #endregion
    } /// Optimized double-entried complex numbers
    public readonly struct ComplexMatrix
    {
        private readonly Complex[] data; private readonly int rows, columns;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComplexMatrix(int rows, int columns) { data = new Complex[rows * columns]; this.rows = rows; this.columns = columns; }
        public Complex this[int row, int column]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => data[row * columns + column];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => data[row * columns + column] = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe Complex* Ptr() { fixed (Complex* ptr = &data[0]) { return ptr; } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe Complex* RowPtr(int row) { fixed (Complex* ptr = &data[row * columns]) { return ptr; } }
    } /// Optimized complex matrices
    public readonly struct RealMatrix
    {
        private readonly double[] data; private readonly int rows, columns;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RealMatrix(int rows, int columns) { data = new double[rows * columns]; this.rows = rows; this.columns = columns; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RealMatrix(double x) { data = new double[] { x }; rows = columns = 1; } // Special for real
        public double this[int row, int column]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => data[row * columns + column];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => data[row * columns + column] = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe double* Ptr() { fixed (double* ptr = &data[0]) { return ptr; } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe double* RowPtr(int row) { fixed (double* ptr = &data[row * columns]) { return ptr; } }
    } /// Optimized real matrices
}