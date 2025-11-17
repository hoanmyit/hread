using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;

namespace HRead
{
    public partial class frmMain : Form
    {
        private const int HOTKEY_ID_CTRL_H = 1;
        private const int HOTKEY_ID_CTRL_K = 2;
        private const int HOTKEY_ID_CTRL_O = 3;

        private HotkeyManager _hotkeyManager;
        private ImageProcessor _imageProcessor;
        private ReplacementLibrary _library;

        private FilterInfoCollection _videoDevices;
        private VideoCaptureDevice _videoSource;
        private bool _isCameraActive = false;

        public frmMain()
        {
            InitializeComponent();
            InitializeApplication();
        }

        private void InitializeApplication()
        {
            this.KeyPreview = true;

            string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            _library = new ReplacementLibrary(dataPath);
            _imageProcessor = new ImageProcessor();
            _hotkeyManager = new HotkeyManager(this.Handle);

            _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            RegisterHotkeys();
        }

        private void RegisterHotkeys()
        {
            _hotkeyManager.RegisterHotkey(HOTKEY_ID_CTRL_H, Keys.H);
            _hotkeyManager.RegisterHotkey(HOTKEY_ID_CTRL_K, Keys.K);
            _hotkeyManager.RegisterHotkey(HOTKEY_ID_CTRL_O, Keys.O);
        }

        protected override void WndProc(ref Message m)
        {
            _hotkeyManager?.ProcessMessage(m);
            base.WndProc(ref m);
        }

        private async void OnHotkeyPressed(int hotkeyId)
        {
            switch (hotkeyId)
            {
                case HOTKEY_ID_CTRL_O:
                    await ProcessClipboardImageAsync();
                    break;
                case HOTKEY_ID_CTRL_H:
                    ProcessTextReplacement();
                    break;
            }
        }

        private async Task ProcessClipboardImageAsync()
        {
            try
            {
                _hotkeyManager.SimulateCopy();
                await Task.Delay(200);

                if (Clipboard.ContainsImage())
                {
                    var image = Clipboard.GetImage();

                    // Tạo bitmap mới từ image để đảm bảo định dạng
                    using (var bitmap = new Bitmap(image))
                    {
                        picImage.Image = (Bitmap)bitmap.Clone();
                        picImage.SizeMode = PictureBoxSizeMode.AutoSize;

                        string ocrText = _imageProcessor.PerformOCR(bitmap);
                        txtRes.Text = ocrText;
                        Clipboard.SetText(ocrText);

                        RefreshControls();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing clipboard image: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ProcessTextReplacement()
        {
            SendKeys.SendWait("^+{LEFT}^c");
            System.Threading.Thread.Sleep(120);

            string text = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text)) return;

            var item = _library.Find(text);
            if (item == null) return;

            ExecuteReplacement(item);
        }

        private void ExecuteReplacement(ReplacementItem item)
        {
            SendKeys.SendWait("^{BACKSPACE}");

            switch (item.Type)
            {
                case "text":
                case "file":
                    Clipboard.SetText(item.Value);
                    SendKeys.SendWait("^v");
                    break;
                case "image":
                    TrySetImageToClipboard(item.Value);
                    SendKeys.SendWait("^v");
                    break;
            }
        }

        private void TrySetImageToClipboard(string filePath)
        {
            try
            {
                using (var img = Image.FromFile(filePath))
                {
                    Clipboard.SetImage(img);
                }
            }
            catch (Exception ex)
            {
                // Log error if needed
            }
        }

        private void RefreshControls()
        {
            txtRes.Refresh();
            picImage.Refresh();
        }

        // Event Handlers
        private void btnText_Click(object sender, EventArgs e)
        {
            if (picImage.Image is Bitmap bitmap)
            {
                txtRes.Text = _imageProcessor.PerformOCR(bitmap);
            }
        }

        private void frmMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V && Clipboard.ContainsImage())
            {
                SetImageFromClipboard();
            }
            else if (e.Control)
            {
                HandleTextProcessingShortcuts(e);
            }
        }

        private void SetImageFromClipboard()
        {
            var image = Clipboard.GetImage();
            picImage.Image = image;
            picImage.SizeMode = PictureBoxSizeMode.AutoSize;
        }

        private void HandleTextProcessingShortcuts(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.R)
            {
                txtRes.Text = StringProcessor.ConvertToSqlString(txtRes.Text);
            }
            else if (e.KeyCode == Keys.B)
            {
                txtRes.Text = StringProcessor.EncodeBase64(txtRes.Text);
            }
            else if (e.KeyCode == Keys.N)
            {
                txtRes.Text = StringProcessor.DecodeBase64(txtRes.Text);
            }
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.png;*.gif;*.bmp;*.jpeg";
                openFileDialog.Title = "Select Image File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadImageFromFile(openFileDialog.FileName);
                }
            }
        }

        private void LoadImageFromFile(string filePath)
        {
            txtPath.Text = filePath;
            picImage.Image = Image.FromFile(filePath);
            picImage.SizeMode = PictureBoxSizeMode.AutoSize;
        }

        private void btnQr_Click(object sender, EventArgs e)
        {
            if (picImage.Image is Bitmap bitmap)
            {
                txtRes.Text = _imageProcessor.ReadQRCode(bitmap);
            }
        }

        private void btnMakeQr_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtRes.Text)) return;

            var qrCode = _imageProcessor.GenerateQRCode(txtRes.Text, picImage.Size);
            picImage.Image = qrCode;
        }

        private void btnMakeBar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtRes.Text)) return;

            var barcode = _imageProcessor.GenerateBarcode(txtRes.Text, picImage.Size);
            picImage.Image = barcode;
        }

        private void btnWc_Click(object sender, EventArgs e)
        {
            ToggleWebcam();
        }

        private void ToggleWebcam()
        {
            if (_isCameraActive)
            {
                StopWebcam();
                btnWc.Text = "Webcam ON";
            }
            else
            {
                StartWebcam();
                btnWc.Text = "Webcam OFF";
            }
            _isCameraActive = !_isCameraActive;
        }

        private void StartWebcam()
        {
            _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (_videoDevices.Count == 0)
            {
                MessageBox.Show("No webcam devices found.");
                return;
            }

            _videoSource = new VideoCaptureDevice(_videoDevices[0].MonikerString);
            _videoSource.NewFrame += VideoSource_NewFrame;
            _videoSource.Start();
        }

        private void StopWebcam()
        {
            _videoSource?.SignalToStop();
            _videoSource?.WaitForStop();
            _videoSource = null;
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            var frame = (Bitmap)eventArgs.Frame.Clone();
            picImage.Invoke((MethodInvoker)delegate
            {
                picImage.Image = frame;
            });
        }

        private void btnIco_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Icon File|*.ico";
                saveFileDialog.Title = "Save Icon File";

                if (saveFileDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(saveFileDialog.FileName))
                {
                    SaveAsIcon(saveFileDialog.FileName);
                }
            }
        }

        private void SaveAsIcon(string filePath)
        {
            if (picImage.Image == null)
            {
                MessageBox.Show("No image to save as icon.");
                return;
            }

            try
            {
                using (var bitmap = new Bitmap(picImage.Image, new Size(128, 128)))
                using (var icon = Icon.FromHandle(bitmap.GetHicon()))
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    icon.Save(stream);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving icon: {ex.Message}");
            }
        }

        private void hd_Click(object sender, EventArgs e)
        {
            ShowHelp();
        }

        private void ShowHelp()
        {
            string libraryText = _library.ExportAsText();
            string helpText = $@"
Keyboard Shortcuts:
Ctrl + R: Convert to multi-line SQL string ('var1','var2')
Ctrl + B: Encode to Base64
Ctrl + N: Decode from Base64
Ctrl + H: Text replacement

Library Contents:
{libraryText}
";
            MessageBox.Show(helpText, "Application Help");
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            CleanupResources();
        }

        private void CleanupResources()
        {
            StopWebcam();
            _hotkeyManager?.Dispose();
            _imageProcessor?.Dispose();
        }
    }
}