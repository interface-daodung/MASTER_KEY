namespace MasterKeyVault;

public partial class MasterKeyForm : Form
{
    private string? _masterKey; // giữ trong RAM khi đang mở khóa
    private string? _pendingKey; // key preview BƯỚC 1a: đã sinh + hiện QR/plaintext, CHƯA lưu DPAPI
    private bool _suppressAutosave; // true khi đang nạp grid từ đĩa -> không tự lưu
    private bool _saving; // chống re-entrancy khi autosave

    public MasterKeyForm()
    {
        InitializeComponent();
        KeyPreview = true; // để bắt Ctrl+S ở cấp form
        dgvEnv.CellEndEdit += (_, _) => AutoSaveVault();
        dgvEnv.CellValueChanged += (_, _) => AutoSaveVault();
        dgvEnv.RowsRemoved += (_, _) => AutoSaveVault();
        dgvEnv.RowValidated += (_, _) => AutoSaveVault();
        dgvEnv.Leave += (_, _) => AutoSaveVault();
        dgvEnv.CellFormatting += dgvEnv_CellFormatting;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.S))
        {
            SaveVaultNow(manual: true);
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void dgvEnv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        // Ẩn VALUE khi bỏ tick "Hiện VALUE" — chỉ mask hiển thị, data gốc giữ nguyên
        if (e.ColumnIndex == 1 && !chkShowValue.Checked && e.Value is string s && s.Length > 0)
        {
            e.Value = new string('•', Math.Min(s.Length, 24));
            e.FormattingApplied = true;
        }
    }

    private void MasterKeyForm_Load(object? sender, EventArgs e)
    {
        lblVaultFile.Text = "vault.enc: " + VaultStore.VaultFilePath;
        // Mặc định nhãn QR = hostname máy; chỉ khi không lấy được mới bắt nhập tay.
        if (string.IsNullOrWhiteSpace(txtLabel.Text))
            txtLabel.Text = Environment.MachineName;
        RefreshVaultState(autoOpen: true);
    }

    // ---------- trạng thái trung tâm: 3 view loại trừ nhau ----------
    // - chưa có vault.enc            -> VIEW TẠO MÃ (grpSetup)
    // - có file nhưng chưa có DPAPI  -> VIEW KHÔI PHỤC (grpRestore: webcam / ảnh QR / key tay)
    // - có file + DPAPI (hoặc đã unlock trong RAM) -> VIEW EDIT (grpVault)
    // View không dùng tới thì ẩn đi. Nhập/tạo thành công -> tự chuyển qua VIEW EDIT.
    private enum VaultView { Create, Restore, Edit }

    private void RefreshVaultState(bool autoOpen, string? msg = null)
    {
        bool exists = VaultStore.VaultExists();
        bool hasDpapi = VaultSecurityManager.HasDpapiKey();
        bool unlocked = !string.IsNullOrEmpty(_masterKey);

        // Cụm chốt QR chỉ bật khi đang ở view tạo mã + có key preview chờ chốt
        bool canConfirm = !exists && _pendingKey != null;
        btnConfirmSetup.Enabled = canConfirm;
        txtTotp.Enabled = canConfirm;
        btnCopyKey.Enabled = _pendingKey != null;
        btnSetup.Enabled = !exists;

        lblStatus.Text = $"Trạng thái: {(msg ?? "sẵn sàng")} | " +
            $"File: {(exists ? "đã có vault.enc" : "chưa có vault.enc")} | " +
            $"DPAPI: {(hasDpapi ? "đã có key" : "chưa có key")}" +
            (_pendingKey != null ? " | *key chờ chốt (chưa lưu DPAPI)" : "");

        if (!exists)
        {
            lblHint.Text = hasDpapi
                ? "Chưa có vault.enc nhưng DPAPI còn key cũ.\r\nBấm '1a. Tạo mã mới' để xem QR + plaintext trước. DPAPI chỉ ghi đè khi bạn 'Chốt QR'."
                : "Chưa có vault.enc — Tên nhãn mặc định là hostname máy.\r\nBấm '1a. Tạo mã mới' để xem QR + plaintext, rồi 'Chốt QR' bằng mã 6 số.";
            ApplyView(VaultView.Create);
        }
        else if (hasDpapi || unlocked)
        {
            // Mặc định = đã mở khóa. Thử mở ngay khi khởi động.
            ApplyView(VaultView.Edit);
            if (autoOpen && !unlocked)
            {
                bool ok = TryOpenWithDpapi(silent: true);
                if (!ok)
                {
                    // Không mở được bằng DPAPI -> qua màn hình Khôi phục quét QR/nhập key tay.
                    lblRestoreInfo.Text = $"Đã có vault.enc tại:\r\n{VaultStore.VaultFilePath}\r\nKhông mở được bằng DPAPI trên máy này — quét QR, tải ảnh QR, hoặc nhập MASTER-KEY thủ công.";
                    ApplyView(VaultView.Restore);
                }
            }
            else
            {
                lblHint.Text = "Đã mở khóa — mọi thay đổi tự lưu vào vault.enc (Ctrl+S để lưu chủ động).";
            }
        }
        else
        {
            lblRestoreInfo.Text = $"Đã có vault.enc tại:\r\n{VaultStore.VaultFilePath}\r\nMáy này chưa có key DPAPI — quét QR, tải ảnh QR, hoặc nhập MASTER-KEY thủ công.";
            ApplyView(VaultView.Restore);
        }
    }

    private void ApplyView(VaultView view)
    {
        switch (view)
        {
            case VaultView.Create:
                grpSetup.Visible = true;
                grpSetup.Location = new Point((ClientSize.Width - grpSetup.Width) / 2, 58);
                grpRestore.Visible = false;
                grpVault.Visible = false;
                break;
            case VaultView.Restore:
                grpSetup.Visible = false;
                grpRestore.Visible = true;
                grpRestore.Location = new Point((ClientSize.Width - grpRestore.Width) / 2, 58);
                grpVault.Visible = false;
                break;
            case VaultView.Edit:
            default:
                grpSetup.Visible = false;
                grpRestore.Visible = false;
                grpVault.Visible = true;
                // Dàn full chiều ngang khi chỉ còn 1 panel edit
                grpVault.Location = new Point(12, 58);
                grpVault.Size = new Size(1016, 556);
                dgvEnv.Location = new Point(10, 28);
                dgvEnv.Size = new Size(996, 462);
                btnAdd.Location = new Point(10, 498);
                btnDelete.Location = new Point(116, 498);
                chkShowValue.Location = new Point(892, 502);
                break;
        }
    }

    private bool TryOpenWithDpapi(bool silent)
    {
        try
        {
            var (dict, masterKey) = VaultStore.OpenWithDpapiOrThrow();
            _masterKey = masterKey;
            LoadDictToGrid(dict);
            lblHint.Text = "Đã mở khóa — mọi thay đổi tự lưu vào vault.enc (Ctrl+S để lưu chủ động).";
            RefreshVaultState(autoOpen: false, msg: "đã mở khóa");
            return true;
        }
        catch (Exception ex)
        {
            if (!silent)
                MessageBox.Show("Mở vault thất bại: " + ex.Message +
                    "\nNếu vừa cài lại Win, hãy dùng 'Quét QR webcam' / 'Chọn ảnh QR' / nhập key tay.",
                    "Open failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    // ---------- grid <-> dict ----------

    private void LoadDictToGrid(Dictionary<string, string> dict)
    {
        _suppressAutosave = true;
        try
        {
            dgvEnv.Rows.Clear();
            foreach (var kv in dict.OrderBy(k => k.Key, StringComparer.Ordinal))
                dgvEnv.Rows.Add(kv.Key, kv.Value);
        }
        finally
        {
            _suppressAutosave = false;
        }
        RefreshVaultState(autoOpen: false);
    }

    private Dictionary<string, string> ReadGridToDict()
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (DataGridViewRow row in dgvEnv.Rows)
        {
            if (row.IsNewRow) continue;
            string key = (row.Cells[0].Value?.ToString() ?? "").Trim();
            string val = (row.Cells[1].Value?.ToString() ?? "");
            if (string.IsNullOrEmpty(key)) continue; // bỏ dòng trống
            if (dict.ContainsKey(key))
                throw new InvalidOperationException($"Trùng KEY: '{key}'. Mỗi biến môi trường chỉ 1 dòng.");
            dict[key] = val;
        }
        return dict;
    }

    private bool EnsureUnlocked(bool silent = false)
    {
        if (!string.IsNullOrEmpty(_masterKey))
            return true;
        string? k = VaultSecurityManager.LoadKeyFromDPAPI();
        if (k == null)
        {
            if (!silent)
                MessageBox.Show("Chưa mở khóa! Hãy khôi phục bằng QR hoặc key tay.",
                    "Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        _masterKey = k;
        return true;
    }

    // ---------- 1a. tạo mã mới: CHỈ xem trước (KHÔNG lưu DPAPI, KHÔNG tạo vault) ----------

    private void btnSetup_Click(object sender, EventArgs e)
    {
        if (VaultStore.VaultExists())
        {
            MessageBox.Show("Đã tồn tại vault.enc tại:\n" + VaultStore.VaultFilePath +
                "\nKhông được tạo mới nữa.",
                "Đã có vault", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshVaultState(autoOpen: false);
            return;
        }

        // Tên nhãn (label) hiển thị trong Authenticator dạng MASTER_KEY:<hostname>.
        // Mặc định = hostname máy. Ô này KHÔNG được để trống:
        // nếu máy không lấy được hostname thì bắt buộc nhập tay.
        string label = txtLabel.Text.Trim();
        if (string.IsNullOrWhiteSpace(label))
            label = Environment.MachineName;
        if (string.IsNullOrWhiteSpace(label))
        {
            MessageBox.Show("Ô 'Tên nhãn' không được để trống — hãy nhập tên máy.",
                "Thiếu tên nhãn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtLabel.Focus();
            return;
        }

        try
        {
            // PREVIEW: sinh key + vẽ QR, KHÔNG đụng tới DPAPI.
            var (base32Key, qrImage) = VaultSecurityManager.GenerateNewVaultPreview(label);
            _pendingKey = base32Key;
            _masterKey = null;

            pictureBoxQR.Image?.Dispose();
            pictureBoxQR.Image = qrImage;
            txtMasterKey.Text = base32Key; // plaintext lần đầu để đối chiếu/kiểm tra
            txtTotp.Clear();

            MessageBox.Show(
                "Đã sinh MASTER-KEY mới (xem plaintext + QR bên trái).\n\n" +
                "DPAPI CHƯA bị ghi đè. Hãy:\n" +
                "1. Đối chiếu chuỗi plaintext MASTER-KEY;\n" +
                "2. Quét QR vào Google Authenticator;\n" +
                "3. Nhập mã 6 số trên điện thoại rồi bấm '1b. Chốt QR'.",
                "Xem trước (chưa lưu)", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshVaultState(autoOpen: false, msg: "key chờ chốt");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khởi tạo: " + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ---------- 1b. chốt QR: verify mã 6 số từ điện thoại rồi MỚI lưu DPAPI ----------

    private void btnConfirmSetup_Click(object sender, EventArgs e)
    {
        if (VaultStore.VaultExists())
        {
            MessageBox.Show("Đã tồn tại vault.enc — không được chốt key mới nữa.",
                "Đã có vault", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshVaultState(autoOpen: false);
            return;
        }
        if (string.IsNullOrEmpty(_pendingKey))
        {
            MessageBox.Show("Chưa có key chờ chốt. Hãy bấm '1a. Tạo mã mới' trước.",
                "Chưa có key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string code = txtTotp.Text.Trim();
        if (code.Length != 6 || !code.All(char.IsDigit))
        {
            MessageBox.Show("Nhập đúng mã 6 số đang hiện trên Authenticator.",
                "Sai định dạng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Mã 6 số đúng = chứng minh điện thoại đã quét đúng QR này → mới "chốt".
        if (!VaultSecurityManager.VerifyTotp(_pendingKey, code))
        {
            MessageBox.Show("Mã 6 số SAI (hoặc đã hết hạn 30s — nhập mã mới).\n" +
                "DPAPI CHƯA bị ghi đè. Kiểm tra lại QR trên điện thoại.",
                "Verify thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            VaultSecurityManager.SaveKeyToDPAPI(_pendingKey);
            _masterKey = _pendingKey;
            _pendingKey = null;

            // Tạo vault từ grid hiện tại (thường là rỗng ở lần đầu)
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            try { dict = ReadGridToDict(); } catch { /* giữ rỗng nếu grid lỗi */ }
            VaultStore.EncryptFromDict(dict, _masterKey);

            // Xóa plaintext khỏi màn hình sau khi đã chốt để khỏi lộ
            txtMasterKey.Clear();
            txtTotp.Clear();

            MessageBox.Show("Chốt QR thành công!\nĐã lưu DPAPI + tạo vault tại:\n" + VaultStore.VaultFilePath,
                "Chốt OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            // Tự chuyển qua giao diện edit sau khi tạo xong
            LoadDictToGrid(dict);
            RefreshVaultState(autoOpen: false, msg: "chốt QR OK");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi chốt: " + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnCopyKey_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtMasterKey.Text))
            return;
        try
        {
            Clipboard.SetText(txtMasterKey.Text);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Copy thất bại: " + ex.Message);
        }
    }

    // ---------- 2. unlock bằng QR / ảnh / key tay (view Khôi phục) ----------

    /// <summary>
    /// Dùng chung cho cả 3 cách khôi phục: verify key giải được vault.enc
    /// rồi mới lưu DPAPI, nạp RAM và tự chuyển qua giao diện edit.
    /// </summary>
    private bool UnlockWithSecret(string rawInput, string sourceName)
    {
        string secret;
        try
        {
            secret = VaultSecurityManager.ExtractSecretFromOtpauthUri(rawInput.Trim());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Key từ {sourceName} không hợp lệ: " + ex.Message, "Key sai",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        Dictionary<string, string> dict;
        try
        {
            dict = VaultStore.DecryptToDict(secret);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Key từ {sourceName} không giải được vault.enc:\n" + ex.Message +
                "\nHãy kiểm tra lại QR / key (đúng máy, đúng file vault).",
                "Sai key", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        try
        {
            VaultSecurityManager.SaveKeyToDPAPI(secret);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lưu DPAPI thất bại: " + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        _masterKey = secret;
        txtManualKey.Clear();
        LoadDictToGrid(dict);

        string totp = VaultSecurityManager.GetCurrentTotp(secret);
        MessageBox.Show($"Mở khóa OK ({sourceName}), đã nạp lại DPAPI.\nTOTP hiện tại: {totp}",
            "Unlock OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        RefreshVaultState(autoOpen: false, msg: $"mở khóa OK ({sourceName})");
        return true;
    }

    private void btnScanCamera_Click(object sender, EventArgs e)
    {
        using var f = new CameraScanForm();
        if (f.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(f.RestoredSecret))
            UnlockWithSecret(f.RestoredSecret, "webcam");
    }

    private void btnRestoreFromQR_Click(object sender, EventArgs e)
    {
        using var open = new OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp",
            Title = "Chọn ảnh QR export từ Google Authenticator"
        };
        if (open.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            string raw;
            using (var bmp = new Bitmap(open.FileName))
            {
                var reader = new ZXing.Windows.Compatibility.BarcodeReader();
                var result = reader.Decode(bmp);
                if (result == null || string.IsNullOrWhiteSpace(result.Text))
                    throw new InvalidOperationException("Không đọc được mã QR từ hình ảnh này!");
                raw = result.Text.Trim();
            }
            UnlockWithSecret(raw, "ảnh QR");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Khôi phục thất bại: " + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnManualUnlock_Click(object? sender, EventArgs e)
    {
        string raw = txtManualKey.Text.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            MessageBox.Show("Hãy nhập MASTER-KEY (chuỗi Base32) hoặc dán nguyên URI otpauth://...",
                "Thiếu key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtManualKey.Focus();
            return;
        }
        UnlockWithSecret(raw, "key tay");
    }

    private void chkShowManualKey_CheckedChanged(object? sender, EventArgs e)
    {
        txtManualKey.UseSystemPasswordChar = !chkShowManualKey.Checked;
    }

    // ---------- tự lưu liên tục (Enter / out focus) + Ctrl+S ----------

    private void CommitGridEdits()
    {
        try
        {
            if (dgvEnv.IsCurrentCellInEditMode)
                dgvEnv.EndEdit();
        }
        catch { /* bỏ qua */ }
    }

    private void AutoSaveVault()
    {
        if (_suppressAutosave || _saving)
            return;
        if (!grpVault.Visible)
            return; // chỉ tự lưu khi đang ở view Edit
        SaveVaultNow(manual: false);
    }

    private void SaveVaultNow(bool manual)
    {
        if (_suppressAutosave || _saving)
            return;
        // Không có gì để lưu khi chưa có file (đang ở view Tạo mã) hoặc chưa unlock
        if (!VaultStore.VaultExists())
            return;
        if (!EnsureUnlocked(silent: !manual))
            return;
        try
        {
            _saving = true;
            CommitGridEdits();
            var dict = ReadGridToDict();
            VaultStore.EncryptFromDict(dict, _masterKey!);
            RefreshVaultState(autoOpen: false, msg: manual ? "lưu OK (Ctrl+S)"
                : $"tự lưu {DateTime.Now:HH:mm:ss}");
            if (manual)
                MessageBox.Show("Đã mã hóa & lưu vào:\n" + VaultStore.VaultFilePath,
                    "Save OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            string err = "Lưu thất bại: " + ex.Message;
            RefreshVaultState(autoOpen: false, msg: err);
            if (manual)
                MessageBox.Show(err, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            // Autosave lỗi (vd trùng KEY): hiện 1 lần để user sửa, không crash
            else if (ex is InvalidOperationException)
                MessageBox.Show(err + "\nHãy sửa lại KEY trùng rồi Enter để tự lưu lại.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _saving = false;
        }
    }

    // ---------- editor thêm / xóa ----------

    private void btnAdd_Click(object sender, EventArgs e)
    {
        CommitGridEdits();
        // Sinh KEY không trùng để autosave sau đó không báo lỗi trùng
        var existing = new HashSet<string>(StringComparer.Ordinal);
        try { foreach (var k in ReadGridToDict().Keys) existing.Add(k); } catch { }
        string name = "NEW_KEY";
        for (int n = 2; existing.Contains(name); n++)
            name = $"NEW_KEY_{n}";
        int idx = dgvEnv.Rows.Add(name, "");
        dgvEnv.CurrentCell = dgvEnv.Rows[idx].Cells[0];
        dgvEnv.BeginEdit(true);
        SaveVaultNow(manual: false);
    }

    private void btnDelete_Click(object sender, EventArgs e)
    {
        _suppressAutosave = true;
        try
        {
            if (dgvEnv.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow r in dgvEnv.SelectedRows)
                    if (!r.IsNewRow) dgvEnv.Rows.Remove(r);
            }
            else if (dgvEnv.CurrentRow != null && !dgvEnv.CurrentRow.IsNewRow)
            {
                dgvEnv.Rows.Remove(dgvEnv.CurrentRow);
            }
        }
        finally
        {
            _suppressAutosave = false;
        }
        SaveVaultNow(manual: false);
    }

    private void chkShowValue_CheckedChanged(object sender, EventArgs e)
    {
        dgvEnv.Invalidate(); // vẽ lại để mask/unmask VALUE
    }

    private void btnOpenFolder_Click(object sender, EventArgs e)
    {
        try
        {
            VaultStore.EnsureVaultDir();
            System.Diagnostics.Process.Start("explorer.exe", VaultStore.VaultDir);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không mở được thư mục: " + ex.Message);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Đã tự lưu liên tục nên thoát không hỏi. Cố lưu nốt lần cuối nếu còn edit dở.
        try
        {
            if (grpVault.Visible && !string.IsNullOrEmpty(_masterKey))
                SaveVaultNow(manual: false);
        }
        catch { /* bỏ qua lỗi lúc thoát */ }
        _masterKey = null;
        _pendingKey = null;
        base.OnFormClosing(e);
    }
}
