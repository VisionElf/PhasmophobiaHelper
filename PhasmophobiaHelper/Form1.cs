using System;
using System.Windows.Forms;

namespace PhasmophobiaHelper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private Phasmophobia _phasmophobia;

        private void Form1_Load(object sender, EventArgs e)
        {
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;

            _phasmophobia = new Phasmophobia();
            _phasmophobia.OnStateChanged += UpdateVisual;

            label5.Text = _phasmophobia.GetShortcuts();
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            label1.Invoke((MethodInvoker) delegate
            {
                label1.Text = _phasmophobia.GetText1();
            });
            label2.Invoke((MethodInvoker) delegate
            {
                label2.Text = _phasmophobia.GetText2();
            });
            label3.Invoke((MethodInvoker) delegate
            {
                label3.Text = _phasmophobia.GetText3();
            });
            label4.Invoke((MethodInvoker) delegate
            {
                label4.Text = _phasmophobia.GetText4();
            });
            label6.Invoke((MethodInvoker) delegate
            {
                label6.Text = _phasmophobia.GetNotEvidences();
            });
        }

        private void Form1_Closing(object sender, FormClosingEventArgs e)
        {
            _phasmophobia.Destroy();
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void Form1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            _phasmophobia.ClearAllEvidences();
        }
    }
}
