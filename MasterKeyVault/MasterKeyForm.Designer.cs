namespace MasterKeyVault;

partial class MasterKeyForm
{
    private System.ComponentModel.IContainer components = null;

    private Label lblVaultFile;
    private Button btnOpenFolder;
    private Label lblStatus;
    private GroupBox grpSetup;
    private Label lblLabel;
    private TextBox txtLabel;
    private Button btnSetup;
    private PictureBox pictureBoxQR;
    private Label lblMasterKey;
    private TextBox txtMasterKey;
    private Button btnCopyKey;
    private Label lblTotp;
    private TextBox txtTotp;
    private Button btnConfirmSetup;
    private Label lblHint;
    private GroupBox grpRestore;
    private Label lblRestoreInfo;
    private Button btnScanCamera;
    private Button btnRestoreFromQR;
    private Label lblManual;
    private TextBox txtManualKey;
    private CheckBox chkShowManualKey;
    private Button btnManualUnlock;
    private GroupBox grpVault;
    private DataGridView dgvEnv;
    private DataGridViewTextBoxColumn colKey;
    private DataGridViewTextBoxColumn colValue;
    private Button btnAdd;
    private Button btnDelete;
    private CheckBox chkShowValue;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        lblVaultFile = new Label();
        btnOpenFolder = new Button();
        lblStatus = new Label();
        grpSetup = new GroupBox();
        lblLabel = new Label();
        txtLabel = new TextBox();
        btnSetup = new Button();
        pictureBoxQR = new PictureBox();
        lblMasterKey = new Label();
        txtMasterKey = new TextBox();
        btnCopyKey = new Button();
        lblTotp = new Label();
        txtTotp = new TextBox();
        btnConfirmSetup = new Button();
        lblHint = new Label();
        grpRestore = new GroupBox();
        lblRestoreInfo = new Label();
        btnScanCamera = new Button();
        btnRestoreFromQR = new Button();
        lblManual = new Label();
        txtManualKey = new TextBox();
        chkShowManualKey = new CheckBox();
        btnManualUnlock = new Button();
        grpVault = new GroupBox();
        dgvEnv = new DataGridView();
        colKey = new DataGridViewTextBoxColumn();
        colValue = new DataGridViewTextBoxColumn();
        btnAdd = new Button();
        btnDelete = new Button();
        chkShowValue = new CheckBox();
        grpSetup.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pictureBoxQR).BeginInit();
        grpRestore.SuspendLayout();
        grpVault.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvEnv).BeginInit();
        SuspendLayout();
        //
        // lblVaultFile
        //
        lblVaultFile.AutoSize = true;
        lblVaultFile.Location = new Point(12, 9);
        lblVaultFile.MaximumSize = new Size(880, 20);
        lblVaultFile.Name = "lblVaultFile";
        lblVaultFile.Size = new Size(120, 20);
        lblVaultFile.TabIndex = 0;
        lblVaultFile.Text = "vault.enc: ...";
        //
        // btnOpenFolder
        //
        btnOpenFolder.Location = new Point(918, 6);
        btnOpenFolder.Name = "btnOpenFolder";
        btnOpenFolder.Size = new Size(110, 29);
        btnOpenFolder.TabIndex = 1;
        btnOpenFolder.Text = "Mở thư mục";
        btnOpenFolder.UseVisualStyleBackColor = true;
        btnOpenFolder.Click += btnOpenFolder_Click;
        //
        // lblStatus
        //
        lblStatus.AutoSize = true;
        lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        lblStatus.ForeColor = Color.DarkSlateGray;
        lblStatus.Location = new Point(12, 34);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(128, 20);
        lblStatus.TabIndex = 2;
        lblStatus.Text = "Trạng thái: ...";
        //
        // grpSetup
        //
        grpSetup.Controls.Add(lblHint);
        grpSetup.Controls.Add(btnConfirmSetup);
        grpSetup.Controls.Add(txtTotp);
        grpSetup.Controls.Add(lblTotp);
        grpSetup.Controls.Add(btnCopyKey);
        grpSetup.Controls.Add(txtMasterKey);
        grpSetup.Controls.Add(lblMasterKey);
        grpSetup.Controls.Add(pictureBoxQR);
        grpSetup.Controls.Add(btnSetup);
        grpSetup.Controls.Add(txtLabel);
        grpSetup.Controls.Add(lblLabel);
        grpSetup.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        grpSetup.Location = new Point(352, 58);
        grpSetup.Name = "grpSetup";
        grpSetup.Size = new Size(336, 556);
        grpSetup.TabIndex = 3;
        grpSetup.TabStop = false;
        grpSetup.Text = "① Tạo mới && chốt QR";
        //
        // lblLabel
        //
        lblLabel.AutoSize = true;
        lblLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        lblLabel.Location = new Point(10, 28);
        lblLabel.Name = "lblLabel";
        lblLabel.Size = new Size(129, 20);
        lblLabel.TabIndex = 0;
        lblLabel.Text = "Tên nhãn (máy):";
        //
        // txtLabel
        //
        txtLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        txtLabel.Location = new Point(145, 25);
        txtLabel.Name = "txtLabel";
        txtLabel.Size = new Size(181, 27);
        txtLabel.TabIndex = 1;
        //
        // btnSetup
        //
        btnSetup.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        btnSetup.Location = new Point(10, 58);
        btnSetup.Name = "btnSetup";
        btnSetup.Size = new Size(316, 32);
        btnSetup.TabIndex = 2;
        btnSetup.Text = "1a. Tạo mã mới (xem trước)";
        btnSetup.UseVisualStyleBackColor = true;
        btnSetup.Click += btnSetup_Click;
        //
        // pictureBoxQR
        //
        pictureBoxQR.BorderStyle = BorderStyle.FixedSingle;
        pictureBoxQR.Location = new Point(10, 96);
        pictureBoxQR.Name = "pictureBoxQR";
        pictureBoxQR.Size = new Size(316, 220);
        pictureBoxQR.SizeMode = PictureBoxSizeMode.Zoom;
        pictureBoxQR.TabIndex = 3;
        pictureBoxQR.TabStop = false;
        //
        // lblMasterKey
        //
        lblMasterKey.AutoSize = true;
        lblMasterKey.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        lblMasterKey.Location = new Point(10, 322);
        lblMasterKey.Name = "lblMasterKey";
        lblMasterKey.Size = new Size(298, 20);
        lblMasterKey.TabIndex = 4;
        lblMasterKey.Text = "MASTER-KEY plaintext (đối chiếu rồi cất kỹ):";
        //
        // txtMasterKey
        //
        txtMasterKey.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
        txtMasterKey.Location = new Point(10, 345);
        txtMasterKey.Name = "txtMasterKey";
        txtMasterKey.ReadOnly = true;
        txtMasterKey.Size = new Size(228, 25);
        txtMasterKey.TabIndex = 5;
        //
        // btnCopyKey
        //
        btnCopyKey.Enabled = false;
        btnCopyKey.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        btnCopyKey.Location = new Point(244, 344);
        btnCopyKey.Name = "btnCopyKey";
        btnCopyKey.Size = new Size(82, 27);
        btnCopyKey.TabIndex = 6;
        btnCopyKey.Text = "Copy";
        btnCopyKey.UseVisualStyleBackColor = true;
        btnCopyKey.Click += btnCopyKey_Click;
        //
        // lblTotp
        //
        lblTotp.AutoSize = true;
        lblTotp.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        lblTotp.Location = new Point(10, 378);
        lblTotp.Name = "lblTotp";
        lblTotp.Size = new Size(186, 20);
        lblTotp.TabIndex = 7;
        lblTotp.Text = "Mã 6 số trên điện thoại:";
        //
        // txtTotp
        //
        txtTotp.Enabled = false;
        txtTotp.Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point);
        txtTotp.Location = new Point(202, 375);
        txtTotp.MaxLength = 6;
        txtTotp.Name = "txtTotp";
        txtTotp.Size = new Size(124, 27);
        txtTotp.TabIndex = 8;
        //
        // btnConfirmSetup
        //
        btnConfirmSetup.Enabled = false;
        btnConfirmSetup.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        btnConfirmSetup.Location = new Point(10, 408);
        btnConfirmSetup.Name = "btnConfirmSetup";
        btnConfirmSetup.Size = new Size(316, 32);
        btnConfirmSetup.TabIndex = 9;
        btnConfirmSetup.Text = "1b. Chốt QR: verify mã 6 số → lưu DPAPI";
        btnConfirmSetup.UseVisualStyleBackColor = true;
        btnConfirmSetup.Click += btnConfirmSetup_Click;
        //
        // lblHint
        //
        lblHint.Font = new Font("Segoe UI", 9F, FontStyle.Italic, GraphicsUnit.Point);
        lblHint.ForeColor = Color.DimGray;
        lblHint.Location = new Point(10, 446);
        lblHint.Name = "lblHint";
        lblHint.Size = new Size(316, 100);
        lblHint.TabIndex = 10;
        lblHint.Text = "Hint...";
        //
        // grpRestore
        //
        grpRestore.Controls.Add(btnManualUnlock);
        grpRestore.Controls.Add(chkShowManualKey);
        grpRestore.Controls.Add(txtManualKey);
        grpRestore.Controls.Add(lblManual);
        grpRestore.Controls.Add(btnRestoreFromQR);
        grpRestore.Controls.Add(btnScanCamera);
        grpRestore.Controls.Add(lblRestoreInfo);
        grpRestore.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        grpRestore.Location = new Point(352, 58);
        grpRestore.Name = "grpRestore";
        grpRestore.Size = new Size(336, 556);
        grpRestore.TabIndex = 4;
        grpRestore.TabStop = false;
        grpRestore.Text = "② Khôi phục (chưa có DPAPI)";
        //
        // lblRestoreInfo
        //
        lblRestoreInfo.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        lblRestoreInfo.ForeColor = Color.DimGray;
        lblRestoreInfo.Location = new Point(10, 28);
        lblRestoreInfo.Name = "lblRestoreInfo";
        lblRestoreInfo.Size = new Size(316, 76);
        lblRestoreInfo.TabIndex = 0;
        lblRestoreInfo.Text = "Đã có vault.enc nhưng máy này chưa có key DPAPI.\r\nChọn 1 trong 3 cách bên dưới để mở khóa.";
        //
        // btnScanCamera
        //
        btnScanCamera.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        btnScanCamera.Location = new Point(10, 110);
        btnScanCamera.Name = "btnScanCamera";
        btnScanCamera.Size = new Size(316, 34);
        btnScanCamera.TabIndex = 1;
        btnScanCamera.Text = "Quét QR bằng webcam...";
        btnScanCamera.UseVisualStyleBackColor = true;
        btnScanCamera.Click += btnScanCamera_Click;
        //
        // btnRestoreFromQR
        //
        btnRestoreFromQR.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        btnRestoreFromQR.Location = new Point(10, 150);
        btnRestoreFromQR.Name = "btnRestoreFromQR";
        btnRestoreFromQR.Size = new Size(316, 34);
        btnRestoreFromQR.TabIndex = 2;
        btnRestoreFromQR.Text = "Tải ảnh QR lên...";
        btnRestoreFromQR.UseVisualStyleBackColor = true;
        btnRestoreFromQR.Click += btnRestoreFromQR_Click;
        //
        // lblManual
        //
        lblManual.AutoSize = true;
        lblManual.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        lblManual.Location = new Point(10, 196);
        lblManual.Name = "lblManual";
        lblManual.Size = new Size(228, 20);
        lblManual.TabIndex = 3;
        lblManual.Text = "Hoặc nhập MASTER-KEY thủ công:";
        //
        // txtManualKey
        //
        txtManualKey.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
        txtManualKey.Location = new Point(10, 220);
        txtManualKey.Name = "txtManualKey";
        txtManualKey.Size = new Size(316, 25);
        txtManualKey.TabIndex = 4;
        txtManualKey.UseSystemPasswordChar = true;
        //
        // chkShowManualKey
        //
        chkShowManualKey.AutoSize = true;
        chkShowManualKey.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        chkShowManualKey.Location = new Point(10, 251);
        chkShowManualKey.Name = "chkShowManualKey";
        chkShowManualKey.Size = new Size(92, 24);
        chkShowManualKey.TabIndex = 5;
        chkShowManualKey.Text = "Hiện key";
        chkShowManualKey.UseVisualStyleBackColor = true;
        chkShowManualKey.CheckedChanged += chkShowManualKey_CheckedChanged;
        //
        // btnManualUnlock
        //
        btnManualUnlock.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        btnManualUnlock.Location = new Point(10, 282);
        btnManualUnlock.Name = "btnManualUnlock";
        btnManualUnlock.Size = new Size(316, 36);
        btnManualUnlock.TabIndex = 6;
        btnManualUnlock.Text = "Mở khóa bằng key tay";
        btnManualUnlock.UseVisualStyleBackColor = true;
        btnManualUnlock.Click += btnManualUnlock_Click;
        //
        // grpVault
        //
        grpVault.Controls.Add(chkShowValue);
        grpVault.Controls.Add(btnDelete);
        grpVault.Controls.Add(btnAdd);
        grpVault.Controls.Add(dgvEnv);
        grpVault.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        grpVault.Location = new Point(354, 136);
        grpVault.Name = "grpVault";
        grpVault.Size = new Size(674, 478);
        grpVault.TabIndex = 5;
        grpVault.TabStop = false;
        grpVault.Text = "③ Biến môi trường (KEY / VALUE)";
        //
        // dgvEnv
        //
        dgvEnv.AllowUserToAddRows = true;
        dgvEnv.AllowUserToDeleteRows = true;
        dgvEnv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvEnv.BackgroundColor = SystemColors.Window;
        dgvEnv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvEnv.Columns.AddRange(new DataGridViewColumn[] { colKey, colValue });
        dgvEnv.Location = new Point(10, 28);
        dgvEnv.Name = "dgvEnv";
        dgvEnv.RowHeadersWidth = 40;
        dgvEnv.Size = new Size(654, 400);
        dgvEnv.TabIndex = 0;
        //
        // colKey
        //
        colKey.FillWeight = 35F;
        colKey.HeaderText = "KEY (biến môi trường)";
        colKey.Name = "colKey";
        //
        // colValue
        //
        colValue.FillWeight = 65F;
        colValue.HeaderText = "VALUE (secret / password)";
        colValue.Name = "colValue";
        //
        // btnAdd
        //
        btnAdd.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        btnAdd.Location = new Point(10, 434);
        btnAdd.Name = "btnAdd";
        btnAdd.Size = new Size(100, 32);
        btnAdd.TabIndex = 1;
        btnAdd.Text = "Thêm dòng";
        btnAdd.UseVisualStyleBackColor = true;
        btnAdd.Click += btnAdd_Click;
        //
        // btnDelete
        //
        btnDelete.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        btnDelete.Location = new Point(116, 434);
        btnDelete.Name = "btnDelete";
        btnDelete.Size = new Size(100, 32);
        btnDelete.TabIndex = 2;
        btnDelete.Text = "Xóa dòng";
        btnDelete.UseVisualStyleBackColor = true;
        btnDelete.Click += btnDelete_Click;
        //
        // chkShowValue
        //
        chkShowValue.AutoSize = true;
        chkShowValue.Checked = true;
        chkShowValue.CheckState = CheckState.Checked;
        chkShowValue.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        chkShowValue.Location = new Point(550, 438);
        chkShowValue.Name = "chkShowValue";
        chkShowValue.Size = new Size(114, 24);
        chkShowValue.TabIndex = 6;
        chkShowValue.Text = "Hiện VALUE";
        chkShowValue.UseVisualStyleBackColor = true;
        chkShowValue.CheckedChanged += chkShowValue_CheckedChanged;
        //
        // MasterKeyForm
        //
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1040, 626);
        Controls.Add(grpVault);
        Controls.Add(grpRestore);
        Controls.Add(grpSetup);
        Controls.Add(lblStatus);
        Controls.Add(btnOpenFolder);
        Controls.Add(lblVaultFile);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "MasterKeyForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "MASTER_KEY Vault — QR + DPAPI + AES-256";
        Load += MasterKeyForm_Load;
        grpSetup.ResumeLayout(false);
        grpSetup.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)pictureBoxQR).EndInit();
        grpRestore.ResumeLayout(false);
        grpRestore.PerformLayout();
        grpVault.ResumeLayout(false);
        grpVault.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)dgvEnv).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}
