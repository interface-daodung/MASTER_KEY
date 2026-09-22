using System.Security.Cryptography;
using System.Text;
using CredentialManagement;
using OtpNet;
using QRCoder;
using ZXing.Windows.Compatibility;

namespace MasterKeyVault;

/// <summary>
/// Quản lý MASTER_KEY + mã hóa vault.enc
/// Luồng 1: Setup -> sinh key -> QR (Authenticator) + DPAPI (Credential Manager)
/// Luồng 2: Restore -> đọc ảnh QR -> secret -> nạp lại DPAPI cho Win mới
/// </summary>
public static class VaultSecurityManager
{
    private const string CRED_TARGET = "MyPersonalVault_MasterKey";

    // ==========================================
    // 1. SINH MASTER_KEY MỚI (PREVIEW — CHƯA LƯU DPAPI)
    // Chỉ hiện QR + plaintext để user đối chiếu/quét.
    // DPAPI chỉ được lưu ở bước "Chốt QR" sau khi verify mã TOTP 6 số.
    // ==========================================
    // URI chuẩn: otpauth://totp/MASTER_KEY:<label>?secret=...&issuer=MASTER_KEY (type totp)
    // label = hostname máy (không dùng tên user, không dùng email).
    public static (string Base32Key, Bitmap QrImage) GenerateNewVaultPreview(string label, string issuer = "MASTER_KEY")
    {
        if (string.IsNullOrWhiteSpace(label))
            label = Environment.MachineName;
        if (string.IsNullOrWhiteSpace(label))
            throw new InvalidOperationException("Tên nhãn trống — hãy nhập tên máy vào ô Tên nhãn.");
        if (string.IsNullOrWhiteSpace(issuer))
            issuer = "MASTER_KEY";

        // Sinh MASTER_KEY 20 bytes ngẫu nhiên chuẩn bảo mật
        byte[] rawKey = KeyGeneration.GenerateRandomKey(20);
        string base32Key = Base32Encoding.ToString(rawKey);
        CryptographicOperations.ZeroMemory(rawKey);

        // Tạo URI và vẽ mã QR để quét vào Google Authenticator
        string otpauthUri = BuildOtpauthUri(base32Key, label, issuer);

        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(otpauthUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new QRCode(qrCodeData);
        Bitmap qrBitmap = qrCode.GetGraphic(20);

        return (base32Key, qrBitmap);
    }

    /// <summary>
    /// Flow cũ gộp (giữ lại cho tương thích): sinh key + lưu DPAPI luôn.
    /// UI mới KHÔNG dùng hàm này nữa — dùng GenerateNewVaultPreview + SaveKeyToDPAPI sau khi user chốt QR.
    /// </summary>
    public static (string Base32Key, Bitmap QrImage) SetupNewVault(string label, string issuer = "MASTER_KEY")
    {
        var (base32Key, qrImage) = GenerateNewVaultPreview(label, issuer);

        // Lưu bản mã hóa DPAPI vào Windows Credential Manager
        SaveKeyToDPAPI(base32Key);

        return (base32Key, qrImage);
    }

    public static string BuildOtpauthUri(string base32Key, string label, string issuer = "MASTER_KEY")
    {
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(label)}" +
               $"?secret={base32Key}&issuer={Uri.EscapeDataString(issuer)}&digits=6&period=30";
    }

    // ==========================================
    // 2. TẠO HOẶC KHÔI PHỤC DPAPI TỪ MASTER_KEY
    // ==========================================
    public static void SaveKeyToDPAPI(string base32Key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(base32Key);
        try
        {
            byte[] encryptedBytes = ProtectedData.Protect(
                keyBytes,
                optionalEntropy: null,
                scope: DataProtectionScope.CurrentUser);

            using var cred = new Credential
            {
                Target = CRED_TARGET,
                Type = CredentialType.Generic,
                PersistanceType = PersistanceType.LocalComputer,
                Password = Convert.ToBase64String(encryptedBytes),
                Username = Environment.UserName
            };
            if (!cred.Save())
                throw new InvalidOperationException("Không lưu được Credential Manager.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBytes);
        }
    }

    /// <summary>Đọc MASTER_KEY từ DPAPI khi mở app bình thường. Trả về null nếu chưa cài.</summary>
    public static string? LoadKeyFromDPAPI()
    {
        using var cred = new Credential { Target = CRED_TARGET };
        if (!cred.Load())
            return null;

        byte[] encryptedBytes = Convert.FromBase64String(cred.Password);
        byte[] decryptedBytes = ProtectedData.Unprotect(
            encryptedBytes,
            optionalEntropy: null,
            scope: DataProtectionScope.CurrentUser);
        try
        {
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(decryptedBytes);
        }
    }

    public static bool HasDpapiKey() => LoadKeyFromDPAPI() is not null;

    public static void DeleteDpapiKey()
    {
        using var cred = new Credential { Target = CRED_TARGET };
        cred.Delete();
    }

    // ==========================================
    // 3. KHÔI PHỤC MASTER_KEY BẰNG CÁCH ĐỌC ẢNH QR
    // ==========================================
    public static string RestoreMasterKeyFromQRImage(Bitmap qrCodeImage)
    {
        string otpauthUri = DecodeQrText(qrCodeImage);
        string secret = ExtractSecretFromOtpauthUri(otpauthUri);

        // Khôi phục xong -> tự động cài lại DPAPI cho máy mới luôn
        SaveKeyToDPAPI(secret);
        return secret;
    }

    public static string RestoreMasterKeyFromQRFile(string imagePath)
    {
        using var bmp = new Bitmap(imagePath);
        return RestoreMasterKeyFromQRImage(bmp);
    }

    private static string DecodeQrText(Bitmap qrCodeImage)
    {
        var reader = new BarcodeReader();
        var result = reader.Decode(qrCodeImage);

        if (result == null || string.IsNullOrWhiteSpace(result.Text))
            throw new InvalidOperationException("Không đọc được mã QR từ hình ảnh này!");

        return result.Text.Trim();
    }

    /// <summary>
    /// Parse URI otpauth://totp/...?secret=XXX... Không dùng System.Web để tránh thêm dependency.
    /// Nếu QR chỉ chứa mỗi secret thô thì trả về luôn.
    /// </summary>
    public static string ExtractSecretFromOtpauthUri(string otpauthUriOrSecret)
    {
        string input = otpauthUriOrSecret.Trim();

        // Trường hợp ảnh QR export của Google Authenticator có thể chỉ là text secret
        if (!input.Contains("://", StringComparison.Ordinal))
        {
            ValidateBase32Secret(input);
            return input.ToUpperInvariant().Replace(" ", "").Replace("-", "");
        }

        var uri = new Uri(input);
        string query = uri.Query.TrimStart('?');

        foreach (string part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int eq = part.IndexOf('=');
            if (eq < 0) continue;
            string name = Uri.UnescapeDataString(part[..eq]);
            string value = Uri.UnescapeDataString(part[(eq + 1)..]);
            if (name.Equals("secret", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException("Mã QR không chứa secret hợp lệ!");
                string secret = value.ToUpperInvariant().Replace(" ", "").Replace("-", "");
                ValidateBase32Secret(secret);
                return secret;
            }
        }

        throw new InvalidOperationException("Mã QR không chứa thông tin secret của Authenticator!");
    }

    private static void ValidateBase32Secret(string secret)
    {
        try
        {
            byte[] raw = Base32Encoding.ToBytes(secret);
            CryptographicOperations.ZeroMemory(raw);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Secret trong QR không phải Base32 hợp lệ!", ex);
        }
    }

    // ==========================================
    // 4. TOTP verify (Otp.NET) — dùng để kiểm tra key sau khi restore
    // ==========================================
    public static bool VerifyTotp(string base32Key, string sixDigitCode, int windowSteps = 1)
    {
        byte[] keyBytes = Base32Encoding.ToBytes(base32Key);
        try
        {
            var totp = new Totp(keyBytes, step: 30, totpSize: 6);
            return totp.VerifyTotp(sixDigitCode, out _, new VerificationWindow(previous: windowSteps, future: windowSteps));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBytes);
        }
    }

    public static string GetCurrentTotp(string base32Key)
    {
        byte[] keyBytes = Base32Encoding.ToBytes(base32Key);
        try
        {
            return new Totp(keyBytes).ComputeTotp();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBytes);
        }
    }

    // ==========================================
    // 5. MÃ HÓA / GIẢI MÃ VAULT.ENC (AES-256-CBC, IV prefix 16 bytes)
    // Key AES = SHA256(raw 20 bytes giải mã từ Base32)
    // ==========================================
    private static byte[] DeriveAesKey(string base32Key)
    {
        byte[] raw = Base32Encoding.ToBytes(base32Key);
        try
        {
            return SHA256.HashData(raw);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(raw);
        }
    }

    public static byte[] EncryptVaultData(string plainTextData, string base32Key)
    {
        byte[] key = DeriveAesKey(base32Key);
        byte[] iv = RandomNumberGenerator.GetBytes(16);
        try
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var ms = new MemoryStream();
            ms.Write(iv, 0, iv.Length); // IV ở 16 bytes đầu file
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainTextData);
            }
            return ms.ToArray();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(iv);
        }
    }

    public static string DecryptVaultData(byte[] cipherData, string base32Key)
    {
        if (cipherData == null || cipherData.Length <= 16)
            throw new ArgumentException("File vault.enc không hợp lệ (quá ngắn).");

        byte[] key = DeriveAesKey(base32Key);
        byte[] iv = new byte[16];
        Buffer.BlockCopy(cipherData, 0, iv, 0, 16);
        try
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var ms = new MemoryStream(cipherData, 16, cipherData.Length - 16);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            return sr.ReadToEnd();
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException("Giải mã thất bại: sai MASTER_KEY hoặc file vault.enc đã hỏng.", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(iv);
        }
    }

    public static void EncryptVaultToFile(string plainTextData, string base32Key, string filePath)
        => File.WriteAllBytes(filePath, EncryptVaultData(plainTextData, base32Key));

    public static string DecryptVaultFromFile(string filePath, string base32Key)
        => DecryptVaultData(File.ReadAllBytes(filePath), base32Key);
}
