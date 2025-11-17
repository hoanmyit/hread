using System;
using System.Drawing;
using System.IO;
using System.Text;
using Tesseract;
using ZXing;
using ZXing.Common;

namespace HRead
{
    public class ImageProcessor
    {
        private readonly TesseractEngine _ocrEngine;

        public ImageProcessor(string tessDataPath = "tessdata", string language = "vie")
        {
            _ocrEngine = new TesseractEngine(tessDataPath, language, EngineMode.Default);
        }

        public string PerformOCR(Bitmap image)
        {
            if (image == null) return string.Empty;

            try
            {
                // Tạo bitmap mới để đảm bảo định dạng phù hợp
                using (var compatibleBitmap = new Bitmap(image))
                using (var ms = new MemoryStream())
                {
                    // Lưu với định dạng PNG để đảm bảo tương thích
                    compatibleBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;

                    using (var pixImage = Pix.LoadFromMemory(ms.ToArray()))
                    using (var page = _ocrEngine.Process(pixImage, PageSegMode.AutoOnly))
                    {
                        return page.GetText()?.Trim() ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"OCR processing failed: {ex.Message}", ex);
            }
        }

        public string ReadQRCode(Bitmap image)
        {
            if (image == null) return string.Empty;

            var reader = new BarcodeReader();
            var result = reader.Decode(image);
            return result?.Text ?? string.Empty;
        }

        public Bitmap GenerateQRCode(string text, Size size)
        {
            if (string.IsNullOrEmpty(text)) return null;

            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Width = size.Width,
                    Height = size.Height,
                    Margin = 1
                }
            };

            return writer.Write(text);
        }

        public Bitmap GenerateBarcode(string text, Size size)
        {
            if (string.IsNullOrEmpty(text)) return null;

            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = size.Width,
                    Height = size.Height,
                    Margin = 1
                }
            };

            return writer.Write(text);
        }

        public static byte[] ImageToByte(Image image)
        {
            if (image == null) return Array.Empty<byte>();

            using (var ms = new MemoryStream())
            {
                // Sử dụng định dạng mặc định nếu RawFormat là null
                var format = image.RawFormat ?? System.Drawing.Imaging.ImageFormat.Png;
                image.Save(ms, format);
                return ms.ToArray();
            }
        }

        public void Dispose()
        {
            _ocrEngine?.Dispose();
        }
    }
}