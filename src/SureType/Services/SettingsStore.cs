using System.IO;
using System.Text.Json;
using SureType.Models;

namespace SureType.Services;

public sealed class SettingsStore
{
    private readonly string _path;
    private bool _preserveOriginal;
    public string? LastError { get; private set; }
    public SettingsStore(string? path = null) => _path = path ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SureType", "settings.json");
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_path)) return new();
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? throw new JsonException("Empty settings");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            LastError = "无法读取设置，已使用默认设置；原文件将保留。 / Settings could not be loaded; defaults are active and the original is preserved.";
            _preserveOriginal = true;
            return new();
        }
    }
    public bool Save(AppSettings settings)
    {
        var temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            if (_preserveOriginal && File.Exists(_path))
                File.Copy(_path, _path + ".corrupt-" + Guid.NewGuid().ToString("N"));
            _preserveOriginal = false;
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, settings, new JsonSerializerOptions { WriteIndented = true });
                stream.Flush(true);
            }
            File.Move(temporary, _path, overwrite: true);
            LastError = null;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            LastError = "设置未能保存，请检查磁盘空间和文件夹权限。 / Settings could not be saved. Check disk space and folder permissions.";
            return false;
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
