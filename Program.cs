using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace KameraApp
{
    public partial class Form1 : Form
    {

        [DllImport("avicap32.dll", EntryPoint = "capCreateCaptureWindowA")]
        public static extern IntPtr capCreateCaptureWindowA(string name, int style, int x, int y, int w, int h, IntPtr parent, int id);

        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        public static extern bool SendMessageStr(IntPtr hWnd, int Msg, int wParam, string lParam);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("avicap32.dll")]
        public static extern bool capGetDriverDescriptionA(short wDriverIndex, StringBuilder lpszName, int cbName, StringBuilder lpszVer, int cbVer);

        [StructLayout(LayoutKind.Sequential)]   
        public struct CAPTUREPARMS
        {
            public int dwRequestMicroSecPerFrame;
            public int fMakeUserHitOKToCapture;
            public int wPercentDropForError;
            public int fYield;
            public int dwIndexSize;
            public int wChunkGranularity;
            public int fUsingDOSMemory;
            public int wNumVideoRequested;
            public int fCaptureAudio;
            public int fMCIControl;
            public int fStepMCIDevice;
            public int dwMCIStartTime;
            public int dwMCIStopTime;
            public int fStepCaptureAt2x;
            public int wStepCaptureAverageFrames;
            public int dwAudioBufferSize;
            public int fDisableWriteCache;
            public int AVStreamMaster;
        }

        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        public static extern bool SendMessageStruct(IntPtr hWnd, int Msg, int wParam, ref CAPTUREPARMS lParam);


        const int WM_CAP_START = 1024;
        const int WM_CAP_DRIVER_CONNECT = WM_CAP_START + 10;
        const int WM_CAP_DRIVER_DISCONNECT = WM_CAP_START + 11;
        const int WM_CAP_SAVEDIB = WM_CAP_START + 25;
        const int WM_CAP_SET_PREVIEW = WM_CAP_START + 50;
        const int WM_CAP_SET_PREVIEWRATE = WM_CAP_START + 52;
        const int WM_CAP_SET_SCALE = WM_CAP_START + 53;
        const int WM_CAP_FILE_SET_CAPTURE_FILEA = WM_CAP_START + 20;
        const int WM_CAP_SEQUENCE = WM_CAP_START + 62;
        const int WM_CAP_STOP = WM_CAP_START + 68;
        const int WM_CAP_SET_SEQUENCE_SETUP = WM_CAP_START + 64;
        const int WM_CAP_GET_SEQUENCE_SETUP = WM_CAP_START + 65;
  
        const int WM_CAP_DLG_VIDEOSOURCE = WM_CAP_START + 42; 

        const int WS_CHILD = 0x40000000;
        const int WS_VISIBLE = 0x10000000;

        private IntPtr hCamera = IntPtr.Zero;
        private int baseW = 640;
        private int baseHeight = 480;

        private PictureBox pbPodglad;
        private ComboBox cmbCameras;
        private Button btnConnect;
        private Button btnPhoto;
        private Button btnRecord;
        private Button btnStopRecord;
        private Button btnSettings;

        private TrackBar trbZoom;
        private Label lblZoomInfo;

        public Form1()
        {
            InitializeComponent();
            this.Text = "Kamera USB - Projekt";
            this.Size = new Size(850, 550);
            SetupUI();
            FindCameras();
        }

        private void SetupUI()
        {
            pbPodglad = new PictureBox();
            pbPodglad.Location = new Point(10, 10);
            pbPodglad.Size = new Size(baseW, baseHeight);
            pbPodglad.BorderStyle = BorderStyle.Fixed3D;
            pbPodglad.BackColor = Color.Black;
            this.Controls.Add(pbPodglad);

            int btnX = 660;

            Label lblCam = new Label();
            lblCam.Text = "Wybierz kamerę:";
            lblCam.Location = new Point(btnX, 10);
            this.Controls.Add(lblCam);

            cmbCameras = new ComboBox();
            cmbCameras.Location = new Point(btnX, 30);
            cmbCameras.Size = new Size(150, 25);
            cmbCameras.DropDownStyle = ComboBoxStyle.DropDownList;
            this.Controls.Add(cmbCameras);

            btnConnect = CreateButton("Połącz", btnX, 60, BtnConnect_Click);
            btnPhoto = CreateButton("Zrób Zdjęcie", btnX, 110, photoTake);
            btnRecord = CreateButton("Start Video", btnX, 160, record);
            btnStopRecord = CreateButton("Stop Video", btnX, 210, BtnStopRecord_Click);

            btnSettings = CreateButton("Ustawienia Kamery", btnX, 260, BtnSettings_Click);

            lblZoomInfo = new Label();
            lblZoomInfo.Text = "Cyfrowy ZOOM (tylko podgląd):";
            lblZoomInfo.Location = new Point(btnX, 310);
            lblZoomInfo.AutoSize = true;
            this.Controls.Add(lblZoomInfo);

        }

        private void FindCameras()
        {
            cmbCameras.Items.Clear();
            StringBuilder name = new StringBuilder(100);
            StringBuilder ver = new StringBuilder(100);

            for (short i = 0; i < 5; i++)
            {
                if (capGetDriverDescriptionA(i, name, 100, ver, 100))
                {
                    cmbCameras.Items.Add($"{i}: {name}");
                }
            }

            if (cmbCameras.Items.Count > 0) cmbCameras.SelectedIndex = 0;
            else cmbCameras.Items.Add("Brak kamer");
        }

        private Button CreateButton(string text, int x, int y, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = new Point(x, y);
            btn.Size = new Size(120, 40);
            btn.Click += onClick;
            this.Controls.Add(btn);
            return btn;
        }

        private void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (hCamera != IntPtr.Zero)
            {
                SendMessage(hCamera, WM_CAP_DRIVER_DISCONNECT, 0, 0);
            }

            hCamera = capCreateCaptureWindowA("Kamera", WS_CHILD | WS_VISIBLE, 0, 0, baseW, baseHeight, pbPodglad.Handle, 0);
            

            if (hCamera != IntPtr.Zero)
            {
                int driverIndex = cmbCameras.SelectedIndex;
                if (driverIndex < 0) driverIndex = 0;

                SendMessage(hCamera, WM_CAP_DRIVER_CONNECT, driverIndex, 0);
                
                SendMessage(hCamera, WM_CAP_SET_PREVIEWRATE, 66, 0);
                SendMessage(hCamera, WM_CAP_SET_SCALE, 1, 0);
                SendMessage(hCamera, WM_CAP_SET_PREVIEW, 1, 0);
            }
        }
        private void BtnSettings_Click(object? sender, EventArgs e)
        {
            if (hCamera != IntPtr.Zero)
            {
                SendMessage(hCamera, WM_CAP_DLG_VIDEOSOURCE, 0, 0);
            }
        }

        private void record(object? sender, EventArgs e)
        {
            if (hCamera == IntPtr.Zero) return;

            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "video.avi");
            SendMessageStr(hCamera, WM_CAP_FILE_SET_CAPTURE_FILEA, 0, path);

            CAPTUREPARMS settings = new CAPTUREPARMS();
            SendMessageStruct(hCamera, WM_CAP_GET_SEQUENCE_SETUP, Marshal.SizeOf(settings), ref settings);

            settings.fYield = 1;
            settings.fMakeUserHitOKToCapture = 0;
            settings.fCaptureAudio = 0;
            settings.dwRequestMicroSecPerFrame = 66667;

            SendMessageStruct(hCamera, WM_CAP_SET_SEQUENCE_SETUP, Marshal.SizeOf(settings), ref settings);
            SendMessage(hCamera, WM_CAP_SEQUENCE, 0, 0);
        }

        private void BtnStopRecord_Click(object? sender, EventArgs e)
        {
            if (hCamera == IntPtr.Zero) return;
            SendMessage(hCamera, WM_CAP_STOP, 0, 0);
            MessageBox.Show("Zakończono nagrywanie.");
        }

        private void photoTake(object? sender, EventArgs e)
        {
            if (hCamera == IntPtr.Zero) return;
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "foto.bmp");
            SendMessageStr(hCamera, WM_CAP_SAVEDIB, 0, path);
            MessageBox.Show($"Zapisano: {path}");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (hCamera != IntPtr.Zero)
                SendMessage(hCamera, WM_CAP_DRIVER_DISCONNECT, 0, 0);
            base.OnFormClosing(e);
        }
    }
}