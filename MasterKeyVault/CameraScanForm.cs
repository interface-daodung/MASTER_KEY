using OpenCvSharp;
using ZXing.Windows.Compatibility;

namespace MasterKeyVault;

/// <summary>
/// Quét mã QR Authenticator bằng webcam để mở khóa vault.enc.
/// Decode liên tục, khi ra secret hợp lệ thì tự nạp DPAPI và đóng form.
/// </summary>
public class CameraScanForm : Form
{
    private readonly PictureBox _view;
    private readonly Label _lblHint;
    private readonly Button _btnCancel;
    private readonly System.Windows.Forms.Timer _timer;

    private VideoCapture? _capture;
    private readonly BarcodeReader _reader = new()
    {
        AutoRotate = true,
        Options = new ZXing.Common.DecodingOptions
        {
            TryHarder = false,
            PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.QR_CODE }
        }
    };
    private bool _done;
    private bool _busy;

    /// <summary>Secret Base32 khôi phục được (đã nạp DPAPI).</summary>
    public string? RestoredSecret { get; private set; }

    public CameraScanForm()
    {
        Text = "Quét mã QR bằng webcam";
        Width = 680;
        Height = 560;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _view = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle
        };
        _lblHint = new Label
        {
            Dock = DockStyle.Top,
            Height = 36,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "Đưa mã QR (Export từ Google Authenticator) ra trước webcam..."
        };
        _btnCancel = new Button
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            Text = "Hủy"
        };
        _btnCancel.Click += (_, _) => Close();

        Controls.Add(_view);
        Controls.Add(_lblHint);
        Controls.Add(_btnCancel);

        _timer = new System.Windows.Forms.Timer { Interval = 250 };
        _timer.Tick += OnTick;

        DoubleBuffered = true;

        Load += (_, _) => StartCamera();
        FormClosed += (_, _) => StopCamera();
    }

    private void StartCamera()
    {
        try
        {
            _capture = new VideoCapture(0);
            if (!_capture.IsOpened())
            {
                MessageBox.Show("Không mở được webcam (index 0). Kiểm tra camera / quyền truy cập.",
                    "No camera", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Close();
                return;
            }
            _timer.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi webcam: " + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }
    }

    private void StopCamera()
    {
        _timer.Stop();
        _busy = false;
        try { _capture?.Release(); } catch { /* ignore */ }
        try { _capture?.Dispose(); } catch { /* ignore */ }
        _capture = null;
        // QUAN TRỌNG: phải gỡ Image khỏi PictureBox TRƯỚC khi Dispose,
        // nếu không lần Paint kế tiếp sẽ DrawImage lên object đã Dispose
        // và ném ExternalException "A generic error occurred in GDI+".
        var img = _view.Image;
        _view.Image = null;
        try { img?.Dispose(); } catch { /* ignore */ }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_done || _busy || _capture == null || !_capture.IsOpened())
            return;
        if (IsDisposed || Disposing || _view.IsDisposed)
            return;

        _busy = true;
        Bitmap? frame = null;
        try
        {
            using var mat = new Mat();
            if (!_capture.Read(mat) || mat.Empty())
                return;

            // Mat -> Bitmap. BẮT BUỘC tách khỏi MemoryStream:
            // "new Bitmap(stream)" vẫn giữ tham chiếu tới stream,
            // dispose stream ngay sẽ làm Bitmap hỏng -> PictureBox.OnPaint
            // ném "A generic error occurred in GDI+".
            try
            {
                byte[] bytes = mat.ToBytes(".bmp");
                using var ms = new MemoryStream(bytes);
                using var tmp = new Bitmap(ms);
                frame = new Bitmap(tmp); // bản copy độc lập, sống sau khi ms/tmp dispose
            }
            catch
            {
                return;
            }

            // Thử decode QR trên frame (trước khi đưa lên UI)
            string? foundSecret = null;
            try
            {
                var result = _reader.Decode(frame);
                if (result != null && !string.IsNullOrWhiteSpace(result.Text))
                    foundSecret = VaultSecurityManager.ExtractSecretFromOtpauthUri(result.Text.Trim());
            }
            catch
            {
                // Chưa phải QR secret hợp lệ → vẫn hiện preview, quét tiếp
                foundSecret = null;
            }

            // Đưa frame lên PictureBox an toàn: gán trước, dispose ảnh cũ sau.
            // Không bao giờ Dispose object đang còn gán trong Image.
            var old = _view.Image;
            _view.Image = frame;
            frame = null; // quyền sở hữu đã chuyển cho PictureBox
            try { old?.Dispose(); } catch { /* ignore */ }

            if (foundSecret != null)
            {
                VaultSecurityManager.SaveKeyToDPAPI(foundSecret);
                _done = true;
                RestoredSecret = foundSecret;
                DialogResult = DialogResult.OK;
                Close();
            }
        }
        finally
        {
            // frame chỉ còn non-null khi chưa gán lên UI (đọc/decode lỗi) → dispose ở đây
            try { frame?.Dispose(); } catch { /* ignore */ }
            _busy = false;
        }
    }
}
