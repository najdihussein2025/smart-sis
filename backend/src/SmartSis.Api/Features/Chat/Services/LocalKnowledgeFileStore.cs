using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SmartSis.Api.Features.Chat.Interfaces;

namespace SmartSis.Api.Features.Chat.Services;

/// <summary>Settings for uploaded knowledge-base files (section <c>KnowledgeBase</c>).</summary>
public sealed class KnowledgeBaseOptions
{
    public const string Section = "KnowledgeBase";

    /// <summary>Folder for uploaded files. Relative paths are under the API's content root.</summary>
    public string StoragePath { get; set; } = "App_Data/knowledge";

    /// <summary>Largest accepted upload, in bytes.</summary>
    public long MaxFileBytes { get; set; } = 1_000_000;
}

/// <summary>Keeps each uploaded document's text as a UTF-8 file named by a random id. Keys are file names only.</summary>
public sealed partial class LocalKnowledgeFileStore(IOptions<KnowledgeBaseOptions> options, IHostEnvironment env) : IKnowledgeFileStore
{
    [GeneratedRegex(@"^[a-f0-9]{32}\.(txt|md)$")]
    private static partial Regex KeyPattern();

    private string Folder => Path.GetFullPath(Path.Combine(env.ContentRootPath, options.Value.StoragePath));

    public async Task<string> SaveAsync(string content, string extension, CancellationToken ct)
    {
        var key = $"{Guid.NewGuid():N}{(extension == ".md" ? ".md" : ".txt")}";
        Directory.CreateDirectory(Folder);
        await File.WriteAllTextAsync(Path.Combine(Folder, key), content, new UTF8Encoding(false), ct);
        return key;
    }

    public async Task<string?> ReadAsync(string key, CancellationToken ct)
    {
        var path = PathFor(key);
        return path is not null && File.Exists(path) ? await File.ReadAllTextAsync(path, ct) : null;
    }

    public void Delete(string key)
    {
        var path = PathFor(key);
        if (path is not null && File.Exists(path)) File.Delete(path);
    }

    /// <summary>Only keys this store created are accepted (no path traversal).</summary>
    private string? PathFor(string key) => KeyPattern().IsMatch(key) ? Path.Combine(Folder, key) : null;
}
