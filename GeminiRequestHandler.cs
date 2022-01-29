/// <summary>
/// Based on https://gemini.circumlunar.space/docs/specification.gmi
/// </summary>
public class GeminiRequestHandler : IAsyncDisposable
{
    private static readonly FileExtensionContentTypeProvider _mime = new();

    private readonly ILogger<GeminiConnectionHandler> _logger;
    private readonly ConnectionContext _connection;
    private readonly Uri _uri;
    private readonly StringBuilder _sb;

    private const string GMI_EXT = ".gmi";
    private const string MIME_GMI = "text/gemini";
    private const string PUBLIC = "public";

    public GeminiRequestHandler(ILogger<GeminiConnectionHandler> logger, ConnectionContext connection, Uri uri)
    {
        _logger = logger;
        _connection = connection;
        _uri = uri;
        _sb = new();
    }

    internal async Task HandleAsync()
    {
        var req = _uri.Segments.Last();

        if (req == "/")
        {
            AddLine($"20 {MIME_GMI}");

            AddLine($">{RuntimeInformation.OSArchitecture}, {RuntimeInformation.OSDescription}, {RuntimeInformation.ProcessArchitecture}");
            AddLine($">{Environment.Version} / {RuntimeInformation.FrameworkDescription}");
            AddLine($">");
            AddLine($">{DateTime.Now:O}");

            await Flush();
        }
        else
        {
            var filename = Path.Combine(AppContext.BaseDirectory, PUBLIC, req);

            if (File.Exists(filename + GMI_EXT))
            {
                filename += GMI_EXT;
            }

            if (File.Exists(filename))
            {
                if (Path.GetExtension(filename) == GMI_EXT)
                {
                    await SendFile(MIME_GMI, filename);
                }
                else
                {
                    await SendFile(_mime.TryGetContentType(filename, out var contentType) ? contentType : "application/octet-stream", filename);
                }
            }
            else
            {
                AddLine("51 Not found.");
                await Flush();
            }
        }
    }

    private async Task SendFile(string contentType, string filename)
    {
        var finfo = new FileInfo(filename);
        var buffer = new Memory<byte>(new byte[1024 * 16]);
        var bytesRead = 0L;

        AddLine($"20 {contentType}");
        await Flush();

        using var reader = new FileStream(filename, FileMode.Open);
        while (bytesRead < finfo.Length)
        {
            var read = await reader.ReadAsync(buffer);

            await _connection.Transport.Output.WriteAsync(buffer[0..read]);

            bytesRead += read;
        }
    }

    protected void AddLine(string s = "")
    {
        _sb.Append(s);
        _sb.Append("\r\n");
    }

    protected async Task Flush()
    {
        await _connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes(_sb.ToString()));
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.Transport.Output.CompleteAsync();
        await _connection.DisposeAsync();
    }
}