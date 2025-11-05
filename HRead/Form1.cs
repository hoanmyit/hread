using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;
using ZXing;
using AForge.Video;
using AForge.Video.DirectShow;
using System.IO;
using System.Runtime.InteropServices;

namespace HRead
{
    public partial class frmMain : Form
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID_CTRL_H = 1;
        private const int HOTKEY_ID_CTRL_K = 2;
        private const int HOTKEY_ID_CTRL_O = 3;
        // Modifier keys
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_C = 0x43;

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;

            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_ID_CTRL_O)
                {
                    keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
                    keybd_event(VK_C, 0, 0, UIntPtr.Zero);
                    keybd_event(VK_C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                    Task.Delay(200); // chờ 200ms cho clipboard cập nhật

                    if (Clipboard.ContainsImage())
                    {
                        // Lấy hình ảnh từ Clipboard
                        Image image = Clipboard.GetImage();

                        // Gán hình ảnh vào PictureBox
                        picImage.Image = image;

                        // Thay đổi kích thước PictureBox để hiển thị hình ảnh đầy đủ
                        picImage.SizeMode = PictureBoxSizeMode.AutoSize;

                        string otext = OCR((Bitmap)Clipboard.GetImage());
                        txtRes.Text = otext;
                        Clipboard.SetText(otext);
                        txtRes.Refresh();
                        picImage.Refresh();
                    }
                }
                if (id == HOTKEY_ID_CTRL_H)
                {
                    SendKeys.SendWait("^+{LEFT}");
                    SendKeys.SendWait("^c");
                    System.Threading.Thread.Sleep(120);
                    string text = Clipboard.GetText();
                    if (string.IsNullOrWhiteSpace(text)) return;
                    text = new string(text.Where(ch => ch >= 32).ToArray());
                    OnHotkeyTriggered(text);
                }
            }
            base.WndProc(ref m);
        }

        private void OnHotkeyTriggered(string key)
        {
            var item = library.Find(key);
            if (item == null) return;

            if (item.Type == "text" || item.Type == "file")
            {
                string text = item.Value;
                // Gõ text vào cửa sổ hiện tại
                SendKeys.SendWait("^{BACKSPACE}");
                //SendKeys.SendWait(text);
                Clipboard.SetText(text);
                SendKeys.SendWait("^v");
            }
            else if (item.Type == "image")
            {
                try
                {
                    var img = Image.FromFile(item.Value);
                    Clipboard.SetImage(img);
                }
                catch (Exception ex)
                {
                }
            }
        }
        ReplacementLibrary library;
        public frmMain()
        {
            InitializeComponent();
            string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            library = new ReplacementLibrary(dataPath);
        }
        private void RegisterGlobalHotkeys()
        {
            RegisterHotKey(this.Handle, HOTKEY_ID_CTRL_H, MOD_CONTROL, (uint)Keys.H);
            RegisterHotKey(this.Handle, HOTKEY_ID_CTRL_K, MOD_CONTROL, (uint)Keys.K);
            RegisterHotKey(this.Handle, HOTKEY_ID_CTRL_O, MOD_CONTROL, (uint)Keys.O);
        }
        private void frmMain_Load(object sender, EventArgs e)
        {
            InitializeComponent();
            RegisterGlobalHotkeys();
        }

        private string OCR(Bitmap b)
        {
            string res = "";

            using (var engine = new TesseractEngine(@"tessdata", "vie", EngineMode.Default))
            {
                var pixImage = Pix.LoadFromMemory(ImageToByte(b));
                using (var page = engine.Process(pixImage, PageSegMode.AutoOnly))
                    res = page.GetText();
            }
            return res;
        }

        public static byte[] ImageToByte(Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }

        private void btnText_Click(object sender, EventArgs e)
        {
            txtRes.Text = OCR((Bitmap)picImage.Image);
        }

        private void frmMain_KeyDown(object sender, KeyEventArgs e)
        {
            // Kiểm tra Ctrl và V đã được nhấn
            if (e.Control && e.KeyCode == Keys.V)
            {
                // Kiểm tra xem dữ liệu trong Clipboard có là hình ảnh hay không
                if (Clipboard.ContainsImage())
                {
                    // Lấy hình ảnh từ Clipboard
                    Image image = Clipboard.GetImage();

                    // Gán hình ảnh vào PictureBox
                    picImage.Image = image;

                    // Thay đổi kích thước PictureBox để hiển thị hình ảnh đầy đủ
                    picImage.SizeMode = PictureBoxSizeMode.AutoSize;
                }
            }
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Chỉ cho phép chọn file ảnh
            openFileDialog.Filter = "File ảnh|*.jpg;*.png;*.gif;*.bmp;*.jpeg";
            openFileDialog.Title = "Chọn file ảnh";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Lấy đường dẫn file ảnh được chọn
                string filePath = openFileDialog.FileName;

                // Gán đường dẫn vào TextBox
                txtPath.Text = filePath;

                // Gán hình ảnh từ file vào PictureBox
                picImage.Image = Image.FromFile(filePath);

                // Thay đổi kích thước PictureBox để hiển thị hình ảnh đầy đủ
                picImage.SizeMode = PictureBoxSizeMode.AutoSize;
            }
        }

        private void btnQr_Click(object sender, EventArgs e)
        {
            // Kiểm tra có hình ảnh trong PictureBox hay không
            if (picImage.Image == null) return;

            // Tạo đối tượng BarcodeReader
            BarcodeReader reader = new BarcodeReader();

            // Đọc ảnh từ PictureBox
            Bitmap image = new Bitmap(picImage.Image);

            // Đọc mã QR từ ảnh
            Result result = reader.Decode(image);

            // Kiểm tra xem mã QR có thành công hay không
            if (result != null)
            {
                txtRes.Text = result.Text;
            }
        }

        private void btnMakeQr_Click(object sender, EventArgs e)
        {
            if(txtRes.Text == "") return;
            // Tạo đối tượng BarcodeWriter
            BarcodeWriter writer = new BarcodeWriter();

            // Cấu hình BarcodeWriter để tạo mã QR
            writer.Format = BarcodeFormat.QR_CODE;
            writer.Options = new ZXing.Common.EncodingOptions
            {
                Width = picImage.Width, // Chiều rộng của mã QR
                Height = picImage.Height, // Chiều cao của mã QR
            };

            // Chuyển đổi văn bản thành ảnh mã QR
            Bitmap qrCodeImage = writer.Write(txtRes.Text);

            // Hiển thị ảnh mã QR trên PictureBox
            picImage.Image = qrCodeImage;
        }

        bool status = false;
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private void btnWc_Click(object sender, EventArgs e)
        {
            if (!status)
            {
                StartWc();
                btnWc.Text = "Webcam OFF";
            }
            else
            {
                StopWc();
                btnWc.Text = "Webcam ON";
            }

            status = !status;
        }

        private void StartWc()
        {

            // Tìm kiếm và lấy danh sách thiết bị webcam
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            // Kiểm tra xem có thiết bị webcam nào được tìm thấy hay không
            if (videoDevices.Count == 0)
            {
                MessageBox.Show("Không tìm thấy thiết bị webcam.");
                return;
            }

            // Khởi tạo đối tượng VideoCaptureDevice với thiết bị webcam đầu tiên
            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);

            // Thiết lập sự kiện NewFrame để nhận hình ảnh mới từ webcam
            videoSource.NewFrame += VideoSource_NewFrame;

            // Bắt đầu streaming hình ảnh từ webcam
            videoSource.Start();
        }

        private void StopWc()
        {
            // Dừng streaming hình ảnh từ webcam khi đóng form
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
                videoSource = null;
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // Lấy hình ảnh mới từ webcam
            var frame = (System.Drawing.Bitmap)eventArgs.Frame.Clone();

            // Hiển thị hình ảnh lên control PictureBox từ luồng chính
            picImage.Invoke((MethodInvoker)delegate
            {
                picImage.Image = frame;
            });
        }

        private void btnIco_Click(object sender, EventArgs e)
        {
            // Tạo đối tượng OpenFileDialog để người dùng chọn vị trí lưu
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Icon File|*.ico";
            saveFileDialog.Title = "Chọn vị trí lưu tệp ICO";
            saveFileDialog.ShowDialog();

            string icoFilePath = saveFileDialog.FileName; // Lấy đường dẫn đã được chọn để lưu tệp ICO

            if (!string.IsNullOrEmpty(icoFilePath))
            {
                // Lấy hình ảnh từ PictureBox
                Bitmap bitmap = new Bitmap(picImage.Image, new Size(128, 128));

                // Tạo đối tượng Icon từ đối tượng Bitmap
                using (Icon icon = Icon.FromHandle(bitmap.GetHicon()))
                {
                    // Tạo một luồng để ghi tệp ICO
                    using (FileStream stream = new FileStream(icoFilePath, FileMode.OpenOrCreate))
                    {
                        // Lưu đối tượng Icon thành tệp ICO
                        icon.Save(stream);
                    }
                }
            }
            else
            {
                MessageBox.Show("Đường dẫn tệp không hợp lệ.");
            }
        }

        private void btnMakeBar_Click(object sender, EventArgs e)
        {
            if (txtRes.Text == "") return;
            // Tạo đối tượng BarcodeWriter
            BarcodeWriter writer = new BarcodeWriter();

            // Cấu hình BarcodeWriter để tạo mã QR
            writer.Format = BarcodeFormat.CODE_128;
            writer.Options = new ZXing.Common.EncodingOptions
            {
                Width = picImage.Width, // Chiều rộng của mã QR
                Height = picImage.Height, // Chiều cao của mã QR
            };

            // Chuyển đổi văn bản thành ảnh mã QR
            Bitmap qrCodeImage = writer.Write(txtRes.Text);

            // Hiển thị ảnh mã QR trên PictureBox
            picImage.Image = qrCodeImage;
        }

        public static string chuyenChuoiSQL(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Tách từng dòng
            var lines = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Xử lý từng dòng
            var resultLines = lines.Select(line =>
            {
                // Tách theo tab, bỏ giá trị trống
                var values = line.Split('\t')
                                 .Select(v => v.Trim())
                                 .Where(v => !string.IsNullOrEmpty(v))
                                 .Select(v => $"'{v.Replace("'", "''")}'"); // thêm dấu nháy đơn

                // Ghép lại: ('1','2','3')
                return $"({string.Join(",", values)})";
            });

            // Ghép các dòng lại, ngăn cách bằng dấu phẩy + xuống dòng
            return string.Join("," + Environment.NewLine, resultLines);
        }

        private void repTXT_Click(object sender, EventArgs e)
        {
            if (txtRes.Text == null) return;

            string input = txtRes.Text;
            if (string.IsNullOrWhiteSpace(input))
                return;

            // Tách từng dòng
            var lines = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Xử lý từng dòng
            var resultLines = lines.Select(line =>
            {
                // Tách theo tab, bỏ giá trị trống
                var values = line.Split('\t')
                                 .Select(v => v.Trim())
                                 .Where(v => !string.IsNullOrEmpty(v))
                                 .Select(v => $"'{v.Replace("'", "''")}'"); // thêm dấu nháy đơn

                // Ghép lại: ('1','2','3')
                return $"({string.Join(",", values)})";
            });

            // Ghép các dòng lại, ngăn cách bằng dấu phẩy + xuống dòng
            string res = string.Join("," + Environment.NewLine, resultLines);

            txtRes.Text = res;
        }

        private void txtRes_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.R)
            {
                txtRes.Text = chuyenChuoiSQL(txtRes.Text);
            }
            if (e.Control && e.KeyCode == Keys.B)
            {
                txtRes.Text = EncodeBase64(txtRes.Text);
            }
            if (e.Control && e.KeyCode == Keys.N)
            {
                txtRes.Text = DecodeBase64(txtRes.Text);
            }
            
        }

        public string EncodeBase64(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            var bytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(bytes);
        }
        public string DecodeBase64(string base64Text)
        {
            if (string.IsNullOrEmpty(base64Text))
                return string.Empty;

            try
            {
                var bytes = Convert.FromBase64String(base64Text);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                // Trường hợp chuỗi không hợp lệ
                return "[Invalid Base64]";
            }
        }

        private void hd_Click(object sender, EventArgs e)
        {
            string ch = library.ExportAsText();
            string hd = $@"
Ctrl + R: chuyển thành dạng chuỗi nhiều dòng ('var1','var2')
Ctrl + B: mã hóa Base64
Ctrl + N: giải mã Base64
Ctrl + H: 
${ch}
";
            MessageBox.Show(hd);
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterAllHotkeys();
        }

        private void UnregisterAllHotkeys()
        {
            try
            {
                if (this.IsHandleCreated)
                {
                    UnregisterHotKey(this.Handle, HOTKEY_ID_CTRL_H);
                    UnregisterHotKey(this.Handle, HOTKEY_ID_CTRL_K);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi huỷ hotkey: " + ex.Message);
            }
        }
    }
}
