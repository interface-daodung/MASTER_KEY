using System.Text;
using System.Text.Json;

internal static partial class VaultTui
{
    private sealed class BrowserPasswordEntry
    {
        public string Name { get; set; } = "";
        public string Note { get; set; } = "";
        public string Password { get; set; } = "";
        public string Url { get; set; } = "";
        public string Username { get; set; } = "";
    }

    private enum ConflictChoice { KeepExisting, Replace, KeepBoth, Skip }

    private static List<BrowserPasswordEntry> ReadBrowserPasswords()
    {
        if (!_values.TryGetValue(BrowserPasswordsStorageKey, out string? json) || string.IsNullOrWhiteSpace(json))
            return new List<BrowserPasswordEntry>();
        return JsonSerializer.Deserialize<List<BrowserPasswordEntry>>(json) ?? new List<BrowserPasswordEntry>();
    }

    private static void WriteBrowserPasswords(List<BrowserPasswordEntry> entries)
    {
        if (entries.Count == 0) _values.Remove(BrowserPasswordsStorageKey);
        else _values[BrowserPasswordsStorageKey] = JsonSerializer.Serialize(entries);
        SaveVault();
    }

    private static void BrowserPasswordsTab()
    {
        if (!RequireUnlocked()) return;
        int selected = 0;
        string[] options = { "List saved logins", "Import CSV file", "Export CSV file", "Add login", "Delete login", "Reveal password", "Back" };
        while (true)
        {
            Console.Clear();
            var entries = ReadBrowserPasswords();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("BROWSER PASSWORDS  ·  STORED INSIDE ENCRYPTED VAULT\n");
            Console.ResetColor();
            Console.WriteLine($"  {entries.Count} saved logins\n");
            for (int i = 0; i < options.Length; i++)
            {
                if (i == selected) { Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Cyan; }
                Console.WriteLine($"  {(i == selected ? "❯" : " ")}  {i + 1}. {options[i]}");
                Console.ResetColor();
            }
            Console.WriteLine("\n  ↑/↓ chọn   Enter mở   1–6 nhanh   Esc quay lại");
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.UpArrow) selected = Math.Max(0, selected - 1);
            else if (key.Key == ConsoleKey.DownArrow) selected = Math.Min(options.Length - 1, selected + 1);
            else if (key.Key is ConsoleKey.Escape or ConsoleKey.Q) return;
            else if (key.Key == ConsoleKey.Enter || key.Key is >= ConsoleKey.D1 and <= ConsoleKey.D6)
            {
                int action = key.Key == ConsoleKey.Enter ? selected : key.Key - ConsoleKey.D1;
                switch (action)
                {
                    case 0: ListBrowserPasswords(); break;
                    case 1: ImportBrowserPasswordsFromPrompt(); break;
                    case 2: ExportBrowserPasswordsFromPrompt(); break;
                    case 3: AddBrowserPassword(); break;
                    case 4: DeleteBrowserPassword(); break;
                    case 5: RevealBrowserPassword(); break;
                    case 6: return;
                }
            }
        }
    }

    private static void ListBrowserPasswords()
    {
        var entries = ReadBrowserPasswords();
        Console.Clear();
        Console.WriteLine("SAVED BROWSER LOGINS\n");
        if (entries.Count == 0) Console.WriteLine("  Chưa có thông tin đăng nhập.");
        for (int i = 0; i < entries.Count; i++)
        {
            var item = entries[i];
            Console.WriteLine($"  {i + 1,3}. {Display(item.Name, 24),-24} {Display(Website(item.Url), 30),-30} {Display(item.Username, 28),-28} {Mask(item.Password)}");
        }
        Pause();
    }

    private static void ImportBrowserPasswordsFromPrompt()
    {
        Console.Clear();
        Console.WriteLine("IMPORT BROWSER PASSWORDS\n");
        string path = Prompt("Đường dẫn CSV").Trim().Trim('"');
        ImportBrowserPasswords(path);
    }

    private static void ExportBrowserPasswordsFromPrompt()
    {
        Console.Clear();
        Console.WriteLine("EXPORT BROWSER PASSWORDS\n");
        Console.WriteLine("CSV chứa password dạng plaintext. Hãy lưu ở nơi an toàn.\n");
        string path = Prompt("Đường dẫn CSV").Trim().Trim('"');
        ExportBrowserPasswords(path, confirmOverwrite: true);
    }

    private static int ExportBrowserPasswords(string path, bool confirmOverwrite)
    {
        if (string.IsNullOrWhiteSpace(path)) { _notice = "Đường dẫn xuất không được để trống."; return 2; }
        string? temp = null;
        try
        {
            string fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath) && confirmOverwrite && Prompt($"File đã tồn tại. Gõ OVERWRITE để ghi đè {fullPath}") != "OVERWRITE")
            {
                _notice = "Đã hủy xuất; file hiện có được giữ nguyên.";
                return 1;
            }
            string? directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("Không xác định được thư mục đích.");
            Directory.CreateDirectory(directory);
            var builder = new StringBuilder();
            builder.AppendLine("name,note,password,url,username");
            foreach (var entry in ReadBrowserPasswords())
            {
                builder.Append(CsvField(entry.Name)).Append(',')
                    .Append(CsvField(entry.Note)).Append(',')
                    .Append(CsvField(entry.Password)).Append(',')
                    .Append(CsvField(entry.Url)).Append(',')
                    .Append(CsvField(entry.Username)).AppendLine();
            }
            temp = fullPath + ".tmp";
            File.WriteAllText(temp, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            File.Move(temp, fullPath, overwrite: true);
            temp = null;
            _notice = $"Đã xuất {ReadBrowserPasswords().Count} login ra CSV: {fullPath}";
            return 0;
        }
        catch (Exception ex)
        {
            _notice = "Không xuất được CSV: " + ex.Message;
            return 2;
        }
        finally
        {
            if (temp is not null)
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            }
        }
    }

    private static string CsvField(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";

    private static int ImportBrowserPasswords(string path)
    {
        if (!File.Exists(path)) { _notice = "Không tìm thấy file CSV."; return 2; }
        try
        {
            var csvRows = ReadCsv(File.ReadAllText(path, Encoding.UTF8));
            if (csvRows.Count < 2) throw new InvalidDataException("CSV không có dòng dữ liệu.");
            var header = csvRows[0].Select((name, index) => (name: NormalizeHeader(name), index))
                .ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);
            int urlCol = FindColumn(header, "url", "website", "site", "origin", "login_uri", "uri");
            int userCol = FindColumn(header, "username", "user", "login", "email", "account");
            int passwordCol = FindColumn(header, "password", "passwd", "pass");
            int nameCol = FindColumn(header, "name", "title", "label");
            int noteCol = FindColumn(header, "note", "notes", "comment");
            if (urlCol < 0 || userCol < 0 || passwordCol < 0)
                throw new InvalidDataException("CSV cần có cột website/url, username/login và password.");

            var entries = ReadBrowserPasswords();
            var working = entries.Select(CloneEntry).ToList();
            int added = 0, replaced = 0, kept = 0, skipped = 0, invalid = 0;
            for (int rowIndex = 1; rowIndex < csvRows.Count; rowIndex++)
            {
                var row = csvRows[rowIndex];
                if (row.All(string.IsNullOrWhiteSpace)) continue;
                string url = Cell(row, urlCol).Trim();
                string username = Cell(row, userCol).Trim();
                string password = Cell(row, passwordCol);
                if (!TryNormalizeWebsite(url, out _)) { invalid++; continue; }
                var incoming = new BrowserPasswordEntry
                {
                    Name = nameCol >= 0 ? Cell(row, nameCol).Trim() : "",
                    Note = noteCol >= 0 ? Cell(row, noteCol).Trim() : "",
                    Password = password,
                    Url = url,
                    Username = username
                };
                if (string.IsNullOrWhiteSpace(incoming.Name)) incoming.Name = Website(url);

                int duplicate = working.FindIndex(x => SameAccountAndWebsite(x, incoming));
                if (duplicate < 0) { working.Add(incoming); added++; continue; }

                var choice = ResolveBrowserConflict(working[duplicate], incoming, rowIndex, csvRows.Count - 1);
                if (choice is null)
                {
                    _notice = "Đã hủy nhập; không lưu các thay đổi trong lần nhập này.";
                    return 1;
                }
                switch (choice.Value)
                {
                    case ConflictChoice.KeepExisting: kept++; break;
                    case ConflictChoice.Replace: working[duplicate] = incoming; replaced++; break;
                    case ConflictChoice.KeepBoth: working.Add(incoming); added++; break;
                    case ConflictChoice.Skip: skipped++; break;
                }
            }

            if (added + replaced > 0) WriteBrowserPasswords(working);
            _notice = $"Nhập xong: thêm {added}, thay {replaced}, giữ bản cũ {kept}, bỏ qua {skipped}, dòng lỗi {invalid}.";
            return 0;
        }
        catch (Exception ex)
        {
            _notice = "Không nhập được CSV: " + ex.Message;
            return 2;
        }
    }

    private static ConflictChoice? ResolveBrowserConflict(BrowserPasswordEntry current, BrowserPasswordEntry incoming, int row, int total)
    {
        while (true)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"DUPLICATE LOGIN  ·  CSV ROW {row}/{total}\n");
            Console.ResetColor();
            Console.WriteLine($"Website: {Website(incoming.Url)}");
            Console.WriteLine($"Account: {Display(incoming.Username, 80)}");
            Console.WriteLine($"Vault entry: {Display(current.Name, 64)}");
            Console.WriteLine($"CSV entry:   {Display(incoming.Name, 64)}");
            Console.WriteLine("Password fields are hidden.\n");
            Console.WriteLine("  K  Giữ bản đang có trong vault");
            Console.WriteLine("  R  Thay bằng bản trong CSV");
            Console.WriteLine("  B  Giữ cả hai bản");
            Console.WriteLine("  S  Bỏ qua dòng CSV này");
            Console.WriteLine("  Esc  Hủy toàn bộ lần nhập");
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.K) return ConflictChoice.KeepExisting;
            if (key == ConsoleKey.R) return ConflictChoice.Replace;
            if (key == ConsoleKey.B) return ConflictChoice.KeepBoth;
            if (key == ConsoleKey.S) return ConflictChoice.Skip;
            if (key == ConsoleKey.Escape) return null;
        }
    }

    private static void AddBrowserPassword()
    {
        Console.Clear(); Console.WriteLine("ADD BROWSER LOGIN\n");
        string name = Prompt("Tên").Trim();
        string url = Prompt("Website URL").Trim();
        string username = Prompt("Username").Trim();
        string password = PromptSecret("Password");
        string note = Prompt("Ghi chú");
        if (!TryNormalizeWebsite(url, out _)) { _notice = "Website URL phải là địa chỉ http/https hợp lệ."; return; }
        var entry = new BrowserPasswordEntry { Name = string.IsNullOrWhiteSpace(name) ? Website(url) : name, Url = url, Username = username, Password = password, Note = note };
        var entries = ReadBrowserPasswords();
        int duplicate = entries.FindIndex(x => SameAccountAndWebsite(x, entry));
        if (duplicate >= 0)
        {
            var choice = ResolveBrowserConflict(entries[duplicate], entry, 1, 1);
            if (choice is null) { _notice = "Đã hủy."; return; }
            if (choice == ConflictChoice.Replace) entries[duplicate] = entry;
            else if (choice == ConflictChoice.KeepBoth) entries.Add(entry);
            else { _notice = choice == ConflictChoice.KeepExisting ? "Đã giữ bản cũ." : "Đã bỏ qua."; return; }
        }
        else entries.Add(entry);
        WriteBrowserPasswords(entries);
        _notice = "Đã lưu thông tin đăng nhập vào vault.";
    }

    private static void DeleteBrowserPassword()
    {
        var entries = ReadBrowserPasswords();
        if (entries.Count == 0) { _notice = "Chưa có login để xóa."; return; }
        ListBrowserPasswords();
        if (!int.TryParse(Prompt("Số login cần xóa"), out int index) || index < 1 || index > entries.Count) { _notice = "Số thứ tự không hợp lệ."; return; }
        var selected = entries[index - 1];
        if (Prompt($"Gõ DELETE để xóa {selected.Name} / {Website(selected.Url)}") != "DELETE") { _notice = "Đã hủy."; return; }
        entries.RemoveAt(index - 1);
        WriteBrowserPasswords(entries);
        _notice = "Đã xóa login.";
    }

    private static void RevealBrowserPassword()
    {
        var entries = ReadBrowserPasswords();
        if (entries.Count == 0) { _notice = "Chưa có login để hiện."; return; }
        ListBrowserPasswords();
        if (!int.TryParse(Prompt("Số login cần hiện password"), out int index) || index < 1 || index > entries.Count) { _notice = "Số thứ tự không hợp lệ."; return; }
        Console.Clear();
        Console.WriteLine($"Website: {entries[index - 1].Url}\nAccount: {entries[index - 1].Username}\nPassword: {entries[index - 1].Password}\nNote: {entries[index - 1].Note}");
        Pause();
    }

    private static bool SameAccountAndWebsite(BrowserPasswordEntry left, BrowserPasswordEntry right) =>
        TryNormalizeWebsite(left.Url, out string leftSite) &&
        TryNormalizeWebsite(right.Url, out string rightSite) &&
        string.Equals(leftSite, rightSite, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.Username.Trim(), right.Username.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool TryNormalizeWebsite(string value, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(value)) return false;
        string candidate = value.Trim();
        if (!candidate.Contains("://", StringComparison.Ordinal)) candidate = "https://" + candidate;
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) || string.IsNullOrWhiteSpace(uri.Host)) return false;
        string host = uri.IdnHost.TrimEnd('.').ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal)) host = host[4..];
        normalized = uri.IsDefaultPort ? host : $"{host}:{uri.Port}";
        return true;
    }

    private static string Website(string value) => TryNormalizeWebsite(value, out string normalized) ? normalized : value;
    private static string Display(string value, int max) => string.IsNullOrWhiteSpace(value) ? "(empty)" : value.Length <= max ? value : value[..Math.Max(0, max - 1)] + "…";
    private static BrowserPasswordEntry CloneEntry(BrowserPasswordEntry item) => new() { Name = item.Name, Note = item.Note, Password = item.Password, Url = item.Url, Username = item.Username };
    private static string Cell(List<string> row, int index) => index >= 0 && index < row.Count ? row[index] : "";
    private static int FindColumn(Dictionary<string, int> headers, params string[] candidates) => candidates.Select(candidate => headers.TryGetValue(candidate, out int index) ? index : -1).FirstOrDefault(index => index >= 0, -1);
    private static string NormalizeHeader(string value) => value.Trim().TrimStart('\uFEFF').ToLowerInvariant().Replace(" ", "_").Replace("-", "_");

    private static List<List<string>> ReadCsv(string text)
    {
        char delimiter = DetectDelimiter(text);
        var records = new List<List<string>>();
        var fields = new List<string>();
        var field = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else field.Append(c);
            }
            else if (c == '"' && field.Length == 0) quoted = true;
            else if (c == delimiter) { fields.Add(field.ToString()); field.Clear(); }
            else if (c is '\r' or '\n')
            {
                fields.Add(field.ToString()); field.Clear();
                records.Add(fields); fields = new List<string>();
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
            }
            else field.Append(c);
        }
        if (quoted) throw new InvalidDataException("CSV có ô được quote nhưng chưa đóng.");
        if (field.Length > 0 || fields.Count > 0) { fields.Add(field.ToString()); records.Add(fields); }
        while (records.Count > 0 && records[^1].All(string.IsNullOrWhiteSpace)) records.RemoveAt(records.Count - 1);
        return records;
    }

    private static char DetectDelimiter(string text)
    {
        int[] counts = new int[3];
        bool quoted = false;
        for (int i = 0; i < text.Length && text[i] is not '\r' and not '\n'; i++)
        {
            if (text[i] == '"' && i + 1 < text.Length && text[i + 1] == '"') { i++; continue; }
            if (text[i] == '"') quoted = !quoted;
            else if (!quoted)
            {
                if (text[i] == ',') counts[0]++;
                else if (text[i] == ';') counts[1]++;
                else if (text[i] == '\t') counts[2]++;
            }
        }
        int best = Array.IndexOf(counts, counts.Max());
        if (counts[best] == 0) throw new InvalidDataException("Không nhận diện được dấu phân cách CSV.");
        return best switch { 1 => ';', 2 => '\t', _ => ',' };
    }
}
