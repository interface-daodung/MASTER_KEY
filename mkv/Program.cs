using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CredentialManagement;
using OtpNet;

// mkv "KEY_NAME" -> in VALUE ra stdout (không xuống dòng thừa) để dùng trong script.
// Ví dụ:  mkv CONTROL_PLANE_TUNNEL_ID
//         mkv '$env:CONTROL_PLANE_TUNNEL_ID'   (nháy đơn để PowerShell khỏi expand)
//         mkv --list   (liệt kê key)
// Exit code: 0=OK, 1=không thấy key, 2=vault/DPAPI lỗi, 3=giải mã lỗi.

Console.OutputEncoding = Encoding.UTF8;

if (args.Length == 0)
{
    PrintUsage();
    return 2;
}

string first = args[0].Trim();

if (first is "-h" or "--help" or "help" or "/?")
{
    PrintUsage();
    return 0;
}

if (first is "--path")
{
    Console.WriteLine(VaultFilePath());
    return 0;
}

if (first is "--selftest")
{
    return SelfTest();
}

try
{
    string? masterKey = LoadKeyFromDPAPI();
    if (masterKey is null)
    {
        Console.Error.WriteLine("ERR: máy này chưa có DPAPI key. Mở app WinForms và quét QR để khôi phục trước.");
        return 2;
    }

    string vaultPath = VaultFilePath();
    if (!File.Exists(vaultPath))
    {
        Console.Error.WriteLine($"ERR: không thấy vault.enc tại: {vaultPath}");
        return 2;
    }

    Dictionary<string, string> dict = DecryptToDict(vaultPath, masterKey);

    if (first is "-l" or "--list")
    {
        foreach (string k in dict.Keys.OrderBy(k => k, StringComparer.Ordinal))
            Console.WriteLine(k);
        return 0;
    }

    string key = NormalizeKey(first);
    if (string.IsNullOrEmpty(key))
    {
        Console.Error.WriteLine("ERR: tên key rỗng.");
        return 1;
    }

    // Tìm key: đúng case trước, rồi tới ignore-case
    if (!dict.TryGetValue(key, out string? value))
    {
        var hit = dict.FirstOrDefault(kv =>
            kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (hit.Key is null)
        {
            Console.Error.WriteLine($"ERR: không thấy key '{key}' trong vault ({dict.Count} keys). Dùng 'mkv --list' để xem.");
            return 1;
        }
        value = hit.Value;
    }

    // Chỉ in VALUE ra stdout, KHÔNG thêm newline/log để script capture sạch.
    Console.Write(value);
    return 0;
}
catch (CryptographicException ex)
{
    Console.Error.WriteLine("ERR: giải mã thất bại (sai MASTER_KEY hoặc vault hỏng): " + ex.Message);
    return 3;
}
catch (Exception ex)
{
    Console.Error.WriteLine("ERR: " + ex.Message);
    return 2;
}

// ---------------- helpers (khớp 100% thuật toán bên WinForms) ----------------

static void PrintUsage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  mkv <KEY_NAME>          in VALUE ra stdout");
    Console.Error.WriteLine("  mkv '$env:FOO'          tự strip prefix $env: (nhớ dùng nháy ĐƠN trong PowerShell)");
    Console.Error.WriteLine("  mkv --list              liệt kê các KEY");
    Console.Error.WriteLine("  mkv --path              in đường dẫn vault.enc");
    Console.Error.WriteLine("Ví dụ:");
    Console.Error.WriteLine("  mkv CONTROL_PLANE_TUNNEL_ID");
    Console.Error.WriteLine("  $t = mkv CONTROL_PLANE_TUNNEL_ID");
}

static string NormalizeKey(string raw)
{
    string s = raw.Trim();
    // Bỏ quote bao ngoài nếu user truyền kèm quote
    if (s.Length >= 2 && ((s[0] == '\'' && s[^1] == '\'') || (s[0] == '"' && s[^1] == '"')))
        s = s[1..^1].Trim();
    // PowerShell: '$env:FOO' hoặc '${env:FOO}'
    if (s.StartsWith("${env:", StringComparison.OrdinalIgnoreCase) && s.EndsWith('}'))
        return s["${env:".Length..^1].Trim();
    if (s.StartsWith("$env:", StringComparison.OrdinalIgnoreCase))
        return s["$env:".Length..].Trim();
    return s;
}

static string VaultFilePath()
{
    string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    if (string.IsNullOrWhiteSpace(docs) || !Directory.Exists(docs))
    {
        string? od = Environment.GetEnvironmentVariable("OneDrive");
        if (!string.IsNullOrWhiteSpace(od))
        {
            string odDocs = Path.Combine(od, "Documents");
            if (Directory.Exists(odDocs))
                return Path.Combine(odDocs, "vault.enc");
        }
        docs = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "OneDrive", "Documents");
    }
    return Path.Combine(docs, "vault.enc");
}

static string? LoadKeyFromDPAPI()
{
    using var cred = new Credential { Target = "MyPersonalVault_MasterKey" };
    if (!cred.Load())
        return null;
    byte[] enc = Convert.FromBase64String(cred.Password);
    byte[] dec = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
    try
    {
        return Encoding.UTF8.GetString(dec);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(dec);
    }
}

static Dictionary<string, string> DecryptToDict(string vaultPath, string base32Key)
{
    byte[] cipher = File.ReadAllBytes(vaultPath);
    if (cipher.Length <= 16)
        throw new InvalidOperationException("vault.enc không hợp lệ (quá ngắn).");

    byte[] raw = Base32Encoding.ToBytes(base32Key);
    byte[] aesKey;
    try { aesKey = SHA256.HashData(raw); }
    finally { CryptographicOperations.ZeroMemory(raw); }

    byte[] iv = new byte[16];
    Buffer.BlockCopy(cipher, 0, iv, 0, 16);
    string json;
    try
    {
        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var ms = new MemoryStream(cipher, 16, cipher.Length - 16);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);
        json = sr.ReadToEnd();
    }
    finally
    {
        CryptographicOperations.ZeroMemory(aesKey);
        CryptographicOperations.ZeroMemory(iv);
    }

    if (string.IsNullOrWhiteSpace(json))
        return new Dictionary<string, string>(StringComparer.Ordinal);
    return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
        ?? new Dictionary<string, string>(StringComparer.Ordinal);
}

static int SelfTest()
{
    // Roundtrip in-memory + file tạm, KHÔNG đụng DPAPI/vault thật.
    // Kiểm tra: Base32 -> AES-CBC IV-prefix -> JSON -> lookup + NormalizeKey.
    try
    {
        byte[] rawKey = KeyGeneration.GenerateRandomKey(20);
        string b32 = Base32Encoding.ToString(rawKey);
        CryptographicOperations.ZeroMemory(rawKey);

        var src = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CONTROL_PLANE_TUNNEL_ID"] = "tunnel_6ab09dc933333333333333361d3807bf7f",
            ["EMPTY_OK"] = ""
        };
        string json = JsonSerializer.Serialize(src);

        // encrypt (copy thuật toán DecryptToDict chiều ngược)
        byte[] raw = Base32Encoding.ToBytes(b32);
        byte[] aesKey;
        try { aesKey = SHA256.HashData(raw); }
        finally { CryptographicOperations.ZeroMemory(raw); }
        byte[] iv = RandomNumberGenerator.GetBytes(16);
        byte[] cipher;
        using (var aes = Aes.Create())
        {
            aes.Key = aesKey;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using var ms = new MemoryStream();
            ms.Write(iv, 0, iv.Length);
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
                sw.Write(json);
            cipher = ms.ToArray();
        }
        CryptographicOperations.ZeroMemory(aesKey);
        CryptographicOperations.ZeroMemory(iv);

        string tmp = Path.Combine(Path.GetTempPath(), $"mkv_selftest_{Guid.NewGuid():N}.enc");
        File.WriteAllBytes(tmp, cipher);
        Dictionary<string, string> back = DecryptToDict(tmp, b32);
        File.Delete(tmp);

        bool ok = back.TryGetValue("CONTROL_PLANE_TUNNEL_ID", out string? v)
            && v == "tunnel_6ab09dc933333333333333361d3807bf7f"
            && NormalizeKey("$env:CONTROL_PLANE_TUNNEL_ID") == "CONTROL_PLANE_TUNNEL_ID"
            && NormalizeKey("${env:CONTROL_PLANE_TUNNEL_ID}") == "CONTROL_PLANE_TUNNEL_ID"
            && NormalizeKey("  CONTROL_PLANE_TUNNEL_ID  ") == "CONTROL_PLANE_TUNNEL_ID";

        // Verify TOTP 6 số đúng logic nút "1b. Chốt QR" bên WinForms:
        // mã hiện tại phải pass, mã sai phải fail.
        byte[] totpKey = Base32Encoding.ToBytes(b32);
        try
        {
            var totp = new OtpNet.Totp(totpKey);
            string current = totp.ComputeTotp();
            ok = ok && totp.VerifyTotp(current, out _,
                new OtpNet.VerificationWindow(previous: 1, future: 1));
            string wrong = current[0] == '0' ? "1" + current[1..] : "0" + current[1..];
            ok = ok && !totp.VerifyTotp(wrong, out _,
                new OtpNet.VerificationWindow(previous: 0, future: 0));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(totpKey);
        }

        Console.Error.WriteLine(ok ? "SELFTEST: PASS" : "SELFTEST: FAIL");
        return ok ? 0 : 1;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("SELFTEST: FAIL: " + ex.Message);
        return 1;
    }
}
