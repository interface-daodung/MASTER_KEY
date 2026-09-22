using System.Text.Json;

namespace MasterKeyVault;

/// <summary>
/// vault.enc LUÔN nằm ở Documents (OneDrive/Documents nếu có redirect).
/// Trên máy bạn = C:\Users\inter\OneDrive\Documents\vault.enc
/// Không hardcode username — dùng MyDocuments để tự resolve.
/// Data tạm thời chỉ là Dictionary key-value (biến môi trường / secret / password).
/// </summary>
public static class VaultStore
{
    public const string VaultFileName = "vault.enc";

    /// <summary>Thư mục cố định chứa vault.enc</summary>
    public static string VaultDir
    {
        get
        {
            // MyDocuments trên máy đã redirect về OneDrive\Documents
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrWhiteSpace(docs) || !Directory.Exists(docs))
            {
                string? oneDrive = Environment.GetEnvironmentVariable("OneDrive");
                if (!string.IsNullOrWhiteSpace(oneDrive))
                {
                    string odDocs = Path.Combine(oneDrive, "Documents");
                    if (Directory.Exists(odDocs))
                        return odDocs;
                }
                docs = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "OneDrive", "Documents");
            }
            return docs;
        }
    }

    public static string VaultFilePath => Path.Combine(VaultDir, VaultFileName);

    public static bool VaultExists() => File.Exists(VaultFilePath);

    public static void EnsureVaultDir()
    {
        Directory.CreateDirectory(VaultDir);
    }

    // ---------- key-value model ----------

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true
    };

    public static Dictionary<string, string> DecryptToDict(string masterKey)
    {
        string json = VaultSecurityManager.DecryptVaultFromFile(VaultFilePath, masterKey);
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOpts)
                ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "vault.enc giải mã OK nhưng không phải JSON key-value. " +
                "File có thể là vault bản cũ (text thô) hoặc đã hỏng.", ex);
        }
    }

    public static void EncryptFromDict(Dictionary<string, string> dict, string masterKey)
    {
        EnsureVaultDir();
        string json = JsonSerializer.Serialize(dict, JsonOpts);
        VaultSecurityManager.EncryptVaultToFile(json, masterKey, VaultFilePath);
    }

    /// <summary>
    /// Mở luôn nếu DPAPI đã có key (không cho tạo mới).
    /// Trả về (dict, masterKey). Ném exception nếu sai key / hỏng file.
    /// </summary>
    public static (Dictionary<string, string> Dict, string MasterKey) OpenWithDpapiOrThrow()
    {
        string? masterKey = VaultSecurityManager.LoadKeyFromDPAPI()
            ?? throw new InvalidOperationException("Máy này chưa có DPAPI — cần quét QR để khôi phục.");
        if (!VaultExists())
            throw new FileNotFoundException($"Không thấy {VaultFilePath}.");
        return (DecryptToDict(masterKey), masterKey);
    }
}
