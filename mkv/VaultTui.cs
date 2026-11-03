using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using CredentialManagement;
using OtpNet;
using QRCoder;

/// <summary>Interactive, keyboard-driven replacement for the old WinForms vault UI.</summary>
internal static partial class VaultTui
{
    private const string CredentialTarget = "MyPersonalVault_MasterKey";
    private const string BrowserPasswordsStorageKey = "__MKV_BROWSER_PASSWORDS_V1";
    private static readonly string VaultPath = ResolveVaultPath();
    private static string? _masterKey;
    private static Dictionary<string, string> _values = new(StringComparer.Ordinal);
    private static string _notice = "";

    public static async Task<int> RunAsync()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;
        try
        {
            _masterKey = File.Exists(VaultPath) ? LoadKeyFromDpapi() : null;
            if (_masterKey is not null)
            {
                try { _values = DecryptToDict(VaultPath, _masterKey); }
                catch (Exception ex) { _masterKey = null; _notice = "Không mở được vault: " + ex.Message; }
            }
            return await LoopAsync();
        }
        finally
        {
            _masterKey = null;
            Console.ResetColor();
            Console.CursorVisible = true;
        }
    }

    public static Task<int> RunImportAsync(string path)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;
        try
        {
            _masterKey = File.Exists(VaultPath) ? LoadKeyFromDpapi() : null;
            if (_masterKey is null)
            {
                Console.Error.WriteLine("Vault đang khóa. Chạy `mkv --tui` để khôi phục trước khi nhập.");
                return Task.FromResult(2);
            }
            _values = DecryptToDict(VaultPath, _masterKey);
            int result = ImportBrowserPasswords(path);
            if (!string.IsNullOrWhiteSpace(_notice)) Console.WriteLine("\n" + _notice);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERR: " + ex.Message);
            return Task.FromResult(2);
        }
        finally
        {
            _masterKey = null;
            Console.ResetColor();
            Console.CursorVisible = true;
        }
    }

    public static Task<int> RunExportAsync(string path)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;
        Console.WriteLine("Cảnh báo: CSV xuất ra sẽ chứa password dạng plaintext.");
        try
        {
            _masterKey = File.Exists(VaultPath) ? LoadKeyFromDpapi() : null;
            if (_masterKey is null)
            {
                Console.Error.WriteLine("Vault đang khóa. Chạy `mkv --tui` để khôi phục trước khi xuất.");
                return Task.FromResult(2);
            }
            _values = DecryptToDict(VaultPath, _masterKey);
            int result = ExportBrowserPasswords(path, confirmOverwrite: true);
            if (!string.IsNullOrWhiteSpace(_notice)) Console.WriteLine("\n" + _notice);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERR: " + ex.Message);
            return Task.FromResult(2);
        }
        finally
        {
            _masterKey = null;
            Console.ResetColor();
            Console.CursorVisible = true;
        }
    }

    private static async Task<int> LoopAsync()
    {
        int selected = 0;
        while (true)
        {
            Draw(selected);
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.UpArrow) selected = Math.Max(0, selected - 1);
            else if (key.Key == ConsoleKey.DownArrow) selected = Math.Min(9, selected + 1);
            else if (key.Key is ConsoleKey.Escape or ConsoleKey.Q) return 0;
            else if (key.Key == ConsoleKey.Enter || key.Key is >= ConsoleKey.D1 and <= ConsoleKey.D9 or ConsoleKey.D0)
            {
                int action = key.Key == ConsoleKey.Enter ? selected : key.Key == ConsoleKey.D0 ? 9 : key.Key - ConsoleKey.D1;
                switch (action)
                {
                    case 0: ListKeys(); break;
                    case 1: AddOrEdit(); break;
                    case 2: DeleteKey(); break;
                    case 3: RevealValue(); break;
                    case 4: BrowserPasswordsTab(); break;
                    case 5: await UnlockByPhoneAsync(); break;
                    case 6: RestoreManually(); break;
                    case 7: CreateVault(); break;
                    case 8: ShowVaultInfo(); break;
                    case 9: return 0;
                }
            }
        }
    }

    private static void Draw(int selected)
    {
        Console.Clear();
        int width = Math.Clamp(Console.WindowWidth - 2, 54, 100);
        string line = new('═', width - 2);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔" + line + "╗");
        Console.WriteLine("║" + Center("M K V   /   MASTER KEY VAULT", width - 2) + "║");
        Console.WriteLine("╚" + line + "╝");
        Console.ResetColor();
        Console.WriteLine("  File  " + VaultPath);
        Console.WriteLine($"  State {(_masterKey is null ? "LOCKED" : "UNLOCKED")}  ·  {UserKeyCount()} keys  ·  DPAPI {(LoadKeyFromDpapi() is null ? "not installed" : "ready")}");
        Console.WriteLine();
        string[] labels = { "Browse keys", "Add / edit key", "Delete key", "Reveal value", "Browser passwords", "Unlock with WinHello QR", "Restore with master key", "Create vault", "Vault details", "Exit" };
        for (int i = 0; i < labels.Length; i++)
        {
            if (i == selected) { Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Cyan; }
            string shortcut = i == 9 ? "0" : (i + 1).ToString();
            Console.WriteLine($"  {(i == selected ? "❯" : " ")}  {shortcut}. {labels[i],-34} {(i < 5 && _masterKey is null ? "[locked]" : "")}");
            Console.ResetColor();
        }
        Console.WriteLine();
        if (!string.IsNullOrWhiteSpace(_notice))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  " + _notice);
            Console.ResetColor();
        }
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ↑/↓ chọn   Enter mở   1–9 nhanh   0 thoát");
        Console.ResetColor();
    }

    private static string Center(string value, int width) => value.Length >= width ? value[..width] : value.PadLeft((width + value.Length) / 2).PadRight(width);

    private static int UserKeyCount() => _values.Keys.Count(k => !k.StartsWith("__MKV_", StringComparison.Ordinal));

    private static void ListKeys()
    {
        if (!RequireUnlocked()) return;
        Console.Clear(); Console.WriteLine("KEYS\n");
        var visible = _values.Where(x => !x.Key.StartsWith("__MKV_", StringComparison.Ordinal)).OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToList();
        if (visible.Count == 0) Console.WriteLine("  Vault chưa có key.");
        foreach (var pair in visible)
            Console.WriteLine($"  {pair.Key,-38} {Mask(pair.Value)}");
        Pause();
    }

    private static void AddOrEdit()
    {
        if (!RequireUnlocked()) return;
        Console.Clear(); Console.WriteLine("ADD / EDIT KEY\n");
        string name = Prompt("Tên key").Trim();
        if (string.IsNullOrWhiteSpace(name) || name.StartsWith("__MKV_", StringComparison.OrdinalIgnoreCase)) { _notice = "Tên key trống hoặc dành riêng cho dữ liệu nội bộ."; return; }
        string value = PromptSecret("Giá trị (nhập xong nhấn Enter)");
        _values[name] = value;
        SaveVault();
        _notice = $"Đã lưu {name}.";
    }

    private static void DeleteKey()
    {
        if (!RequireUnlocked()) return;
        Console.Clear(); Console.WriteLine("DELETE KEY\n");
        string name = Prompt("Tên key cần xóa").Trim();
        if (!_values.ContainsKey(name)) { _notice = "Không tìm thấy key."; return; }
        if (Prompt($"Gõ DELETE để xóa {name}") == "DELETE") { _values.Remove(name); SaveVault(); _notice = "Đã xóa key."; }
        else _notice = "Đã hủy.";
    }

    private static void RevealValue()
    {
        if (!RequireUnlocked()) return;
        Console.Clear();
        string name = Prompt("Tên key cần xem").Trim();
        if (!_values.TryGetValue(name, out var value)) { _notice = "Không tìm thấy key."; return; }
        Console.WriteLine($"\n{name} = {value}");
        _ = Console.ReadKey(true);
    }

    private static async Task UnlockByPhoneAsync()
    {
        if (!File.Exists(VaultPath)) { _notice = "Chưa có vault.enc. Hãy chọn Create vault."; return; }
        if (_masterKey is not null) { _notice = "Vault đã mở khóa."; return; }
        TcpListener? listener = null;
        try
        {
            var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            using var cert = CreatePairingCertificate();
            listener = new TcpListener(IPAddress.Any, 0);
            listener.Start(4);
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            string ip = GetLanAddress();
            string pin = Convert.ToHexString(SHA256.HashData(cert.RawData));
            var qrText = $"winhello://vault-unlock?endpoint={Uri.EscapeDataString($"https://{ip}:{port}/v1/unlock")}&pin={pin}&token={secret}";
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("WINHELLO VAULT UNLOCK  ·  QR DÙNG MỘT LẦN  ·  HẾT HẠN SAU 120 GIÂY\n");
            using (var generator = new QRCodeGenerator())
            using (var data = generator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q))
                Console.WriteLine(new AsciiQRCode(data).GetGraphicSmall(true, false));
            Console.WriteLine($"TLS pin: {pin}\nToken:   {secret}\n");
            Console.WriteLine("Quét bằng WinHello trên cùng mạng. Esc hủy; đang chờ kết nối…");
            Console.ResetColor();

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            using var cancel = new CancellationTokenSource();
            var accept = AcceptSecretAsync(listener, cert, secret, timeout.Token);
            while (!accept.IsCompleted)
            {
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape) cancel.Cancel();
                if (cancel.IsCancellationRequested) { listener.Stop(); _notice = "Đã hủy phiên QR."; return; }
                await Task.WhenAny(accept, Task.Delay(150));
            }
            string received = await accept;
            var dict = DecryptToDict(VaultPath, received);
            SaveKeyToDpapi(received);
            _masterKey = received;
            _values = dict;
            _notice = $"WinHello đã gửi key hợp lệ; mở vault thành công ({dict.Count} keys).";
        }
        catch (OperationCanceledException) { _notice = "QR đã hết hạn, hãy tạo phiên mới."; }
        catch (Exception ex) { _notice = "Ghép đôi thất bại: " + ex.Message; }
        finally { listener?.Stop(); }
    }

    private static async Task<string> AcceptSecretAsync(TcpListener listener, X509Certificate2 certificate, string token, CancellationToken ct)
    {
        if (!listener.Server.IsBound) listener.Start();
        int failed = 0;
        while (failed < 5)
        {
            using var client = await listener.AcceptTcpClientAsync(ct);
            using var tls = new SslStream(client.GetStream(), false);
            await tls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
            {
                ServerCertificate = certificate,
                ClientCertificateRequired = false,
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
            }, ct);
            byte[] headerBytes = await ReadHeadersAsync(tls, ct);
            string headers = Encoding.ASCII.GetString(headerBytes);
            var lines = headers.Split("\r\n", StringSplitOptions.None);
            string request = lines[0];
            int length = HeaderValue(lines, "Content-Length") is string n && int.TryParse(n, out var parsed) ? parsed : 0;
            string presentedToken = HeaderValue(lines, "X-MKV-Token") ?? "";
            if (!request.StartsWith("POST /v1/unlock ", StringComparison.Ordinal) || length is <= 0 or > 4096 || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presentedToken), Encoding.UTF8.GetBytes(token)))
            {
                await RespondAsync(tls, 401, "{\"error\":\"invalid_request\"}"); failed++; continue;
            }
            byte[] body = new byte[length];
            await ReadExactAsync(tls, body, ct);
            using var doc = JsonDocument.Parse(body);
            string? key = doc.RootElement.TryGetProperty("masterKey", out var value) ? value.GetString() : null;
            if (string.IsNullOrWhiteSpace(key)) { await RespondAsync(tls, 400, "{\"error\":\"missing_master_key\"}"); failed++; continue; }
            try { _ = DecryptToDict(VaultPath, key); }
            catch { await RespondAsync(tls, 401, "{\"error\":\"key_did_not_unlock_vault\"}"); failed++; continue; }
            await RespondAsync(tls, 200, "{\"unlocked\":true}");
            return key;
        }
        throw new InvalidOperationException("Quá nhiều lần gửi key không hợp lệ.");
    }

    private static async Task<byte[]> ReadHeadersAsync(Stream stream, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        var one = new byte[1];
        while (ms.Length < 16384)
        {
            int read = await stream.ReadAsync(one, ct);
            if (read == 0) throw new IOException("Kết nối đóng trước HTTP headers.");
            ms.WriteByte(one[0]);
            if (ms.Length >= 4)
            {
                var b = ms.GetBuffer(); int n = (int)ms.Length;
                if (b[n - 4] == 13 && b[n - 3] == 10 && b[n - 2] == 13 && b[n - 1] == 10) return ms.ToArray()[..^4];
            }
        }
        throw new InvalidDataException("HTTP headers quá dài.");
    }

    private static async Task ReadExactAsync(Stream stream, byte[] bytes, CancellationToken ct)
    {
        int offset = 0;
        while (offset < bytes.Length) { int n = await stream.ReadAsync(bytes.AsMemory(offset), ct); if (n == 0) throw new EndOfStreamException(); offset += n; }
    }

    private static string? HeaderValue(string[] lines, string name) => lines.Skip(1).Select(x => x.Split(':', 2)).Where(x => x.Length == 2 && x[0].Equals(name, StringComparison.OrdinalIgnoreCase)).Select(x => x[1].Trim()).FirstOrDefault();

    private static async Task RespondAsync(Stream stream, int status, string body)
    {
        byte[] data = Encoding.UTF8.GetBytes(body);
        byte[] head = Encoding.ASCII.GetBytes($"HTTP/1.1 {status} {(status == 200 ? "OK" : "Error")}\r\nContent-Type: application/json\r\nContent-Length: {data.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(head); await stream.WriteAsync(data); await stream.FlushAsync();
    }

    private static X509Certificate2 CreatePairingCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=mkv-one-time-pairing", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder();
        san.AddIpAddress(IPAddress.Parse(GetLanAddress()));
        request.CertificateExtensions.Add(san.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(3));
    }

    private static string GetLanAddress() => NetworkInterface.GetAllNetworkInterfaces()
        .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
        .SelectMany(n => n.GetIPProperties().UnicastAddresses)
        .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address))
        .Select(a => a.Address.ToString()).FirstOrDefault() ?? throw new InvalidOperationException("No IPv4 LAN address; connect this PC and phone to the same network.");

    private static void RestoreManually()
    {
        if (!File.Exists(VaultPath)) { _notice = "Chưa có vault.enc."; return; }
        string key = Prompt("Nhập MASTER_KEY (Base32)").Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "");
        try
        {
            var dict = DecryptToDict(VaultPath, key);
            SaveKeyToDpapi(key); _masterKey = key; _values = dict;
            _notice = $"Đã khôi phục DPAPI và mở vault ({dict.Count} keys).";
        }
        catch (Exception ex) { _notice = "Khôi phục thất bại: " + ex.Message; }
    }

    private static void CreateVault()
    {
        if (File.Exists(VaultPath)) { _notice = "vault.enc đã tồn tại. Dùng restore để mở."; return; }
        Console.Clear(); Console.WriteLine("CREATE VAULT\n");
        byte[] raw = KeyGeneration.GenerateRandomKey(20);
        string key = Base32Encoding.ToString(raw); CryptographicOperations.ZeroMemory(raw);
        Console.WriteLine("Quét QR bằng app authenticator, sau đó nhập mã TOTP 6 số để xác nhận.\n");
        using (var gen = new QRCodeGenerator())
        using (var data = gen.CreateQrCode($"otpauth://totp/MASTER_KEY:{Uri.EscapeDataString(Environment.MachineName)}?secret={key}&issuer=MASTER_KEY&digits=6&period=30", QRCodeGenerator.ECCLevel.Q))
            Console.WriteLine(new AsciiQRCode(data).GetGraphicSmall(true, false));
        Console.WriteLine("MASTER_KEY: " + key);
        string code = Prompt("Mã TOTP xác nhận").Trim();
        byte[] b = Base32Encoding.ToBytes(key);
        bool valid;
        try { valid = new Totp(b).VerifyTotp(code, out _, new VerificationWindow(1, 1)); }
        finally { CryptographicOperations.ZeroMemory(b); }
        if (!valid) { _notice = "Mã TOTP không hợp lệ; vault chưa tạo."; return; }
        _values = new(StringComparer.Ordinal);
        EncryptFromDict(VaultPath, _values, key);
        SaveKeyToDpapi(key);
        _masterKey = key;
        _notice = "Đã tạo vault.enc và lưu DPAPI.";
    }

    private static void ShowVaultInfo()
    {
        Console.Clear();
        Console.WriteLine("VAULT DETAILS\n");
        Console.WriteLine($"File:   {VaultPath}");
        Console.WriteLine($"Exists: {File.Exists(VaultPath)}");
        Console.WriteLine($"Size:   {(File.Exists(VaultPath) ? new FileInfo(VaultPath).Length : 0):N0} bytes");
        Console.WriteLine($"DPAPI:  {(LoadKeyFromDpapi() is null ? "not present" : "present")}");
        Console.WriteLine($"Keys:   {(_masterKey is null ? "locked" : _values.Count)}");
        Pause();
    }

    private static bool RequireUnlocked() { if (_masterKey is not null) return true; _notice = "Vault đang khóa. Dùng WinHello QR hoặc khôi phục bằng MASTER_KEY."; return false; }
    private static void SaveVault() { if (_masterKey is null) throw new InvalidOperationException("Vault đang khóa."); EncryptFromDict(VaultPath, _values, _masterKey); }
    private static string Prompt(string label) { Console.Write("  " + label + ": "); Console.CursorVisible = true; string value = Console.ReadLine() ?? ""; Console.CursorVisible = false; return value; }
    private static string PromptSecret(string label)
    {
        Console.Write("  " + label + ": ");
        var chars = new StringBuilder();
        Console.CursorVisible = false;
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return chars.ToString(); }
            if (key.Key == ConsoleKey.Backspace && chars.Length > 0) { chars.Length--; Console.Write("\b \b"); }
            else if (!char.IsControl(key.KeyChar)) { chars.Append(key.KeyChar); Console.Write('•'); }
        }
    }
    private static void Pause() { Console.WriteLine("\n  Nhấn phím bất kỳ để về menu…"); Console.ReadKey(true); }
    private static string Mask(string value) => string.IsNullOrEmpty(value) ? "(empty)" : new string('•', Math.Min(24, value.Length));

    private static string ResolveVaultPath()
    {
        string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(docs) || !Directory.Exists(docs))
        {
            string? od = Environment.GetEnvironmentVariable("OneDrive");
            if (!string.IsNullOrWhiteSpace(od) && Directory.Exists(Path.Combine(od, "Documents"))) docs = Path.Combine(od, "Documents");
            else docs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", "Documents");
        }
        return Path.Combine(docs, "vault.enc");
    }

    private static string? LoadKeyFromDpapi()
    {
        using var cred = new Credential { Target = CredentialTarget };
        if (!cred.Load()) return null;
        byte[] encrypted = Convert.FromBase64String(cred.Password);
        byte[] plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        try { return Encoding.UTF8.GetString(plain); }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }

    private static void SaveKeyToDpapi(string key)
    {
        byte[] plain = Encoding.UTF8.GetBytes(key);
        try
        {
            byte[] encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            using var cred = new Credential { Target = CredentialTarget, Type = CredentialType.Generic, PersistanceType = PersistanceType.LocalComputer, Password = Convert.ToBase64String(encrypted), Username = Environment.UserName };
            if (!cred.Save()) throw new InvalidOperationException("Không lưu được Credential Manager.");
        }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }

    private static Dictionary<string, string> DecryptToDict(string path, string base32)
    {
        byte[] cipher = File.ReadAllBytes(path);
        if (cipher.Length <= 16) throw new InvalidDataException("vault.enc không hợp lệ.");
        byte[] raw = Base32Encoding.ToBytes(base32); byte[] key = SHA256.HashData(raw); CryptographicOperations.ZeroMemory(raw);
        byte[] iv = cipher[..16];
        try
        {
            using var aes = Aes.Create(); aes.Key = key; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
            using var ms = new MemoryStream(cipher, 16, cipher.Length - 16); using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read); using var sr = new StreamReader(cs, Encoding.UTF8);
            string json = sr.ReadToEnd(); return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new(StringComparer.Ordinal);
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(iv); }
    }

    private static void EncryptFromDict(string path, Dictionary<string, string> values, string base32)
    {
        byte[] raw = Base32Encoding.ToBytes(base32); byte[] key = SHA256.HashData(raw); CryptographicOperations.ZeroMemory(raw);
        byte[] iv = RandomNumberGenerator.GetBytes(16);
        try
        {
            using var aes = Aes.Create(); aes.Key = key; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
            using var ms = new MemoryStream(); ms.Write(iv);
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write, true))
            using (var sw = new StreamWriter(cs, new UTF8Encoding(false))) sw.Write(JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = true }));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temp = path + ".tmp"; File.WriteAllBytes(temp, ms.ToArray()); File.Move(temp, path, true);
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(iv); }
    }
}
