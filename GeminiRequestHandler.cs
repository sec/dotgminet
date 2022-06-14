using System.IO.Pipelines;

/// <summary>
/// Based on https://gemini.circumlunar.space/docs/specification.gmi
/// </summary>
public class GeminiRequestHandler : ConnectionHandler
{
    private const string GMI_EXT = ".gmi";
    private const string MIME_GMI = "text/gemini";
    private const string PUBLIC = "public";

    private static readonly FileExtensionContentTypeProvider _mime = new();

    private readonly ILogger<GeminiRequestHandler> _logger;
    private readonly StringBuilder _sb;

    public GeminiRequestHandler(ILogger<GeminiRequestHandler> logger)
    {
        _logger = logger;
        _sb = new();

        _logger.LogInformation("GeminiRequestHandler created");
    }

    public async override Task OnConnectedAsync(ConnectionContext connection)
    {
        var result = await connection.Transport.Input.ReadAsync();
        var buffer = result.Buffer;

        if (buffer.IsSingleSegment)
        {
            _logger.LogInformation("New connection handling");

            await HandleAsync(connection, new Uri(Encoding.UTF8.GetString(buffer.FirstSpan).Trim()));

            await connection.Transport.Output.CompleteAsync();
            await connection.DisposeAsync();
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    async Task HandleAsync(ConnectionContext connection, Uri _uri)
    {
        var req = _uri.Segments.Last();

        if (req == "/")
        {
            AddLine($"20 {MIME_GMI}");

            AddLine($">{RuntimeInformation.OSArchitecture}, {RuntimeInformation.OSDescription}, {RuntimeInformation.ProcessArchitecture}");
            AddLine($">{Environment.Version} / {RuntimeInformation.FrameworkDescription}");
            AddLine($">");
            AddLine($">{DateTime.Now:O}");

            await Flush(connection);
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
                    await SendFile(connection, MIME_GMI, filename);
                }
                else
                {
                    await SendFile(connection, _mime.TryGetContentType(filename, out var contentType) ? contentType : "application/octet-stream", filename);
                }
            }
            else
            {
                AddLine("51 Not found.");
                await Flush(connection);
            }
        }
    }

    async Task SendFile(ConnectionContext connection, string contentType, string filename)
    {
        var finfo = new FileInfo(filename);
        var buffer = new Memory<byte>(new byte[1024 * 16]);
        var bytesRead = 0L;

        AddLine($"20 {contentType}");
        await Flush(connection);

        using var reader = new FileStream(filename, FileMode.Open);
        while (bytesRead < finfo.Length)
        {
            var read = await reader.ReadAsync(buffer);

            await connection.Transport.Output.WriteAsync(buffer[0..read]);

            bytesRead += read;
        }
    }

    void AddLine(string s = "") => _sb.Append($"{s}\r\n");

    ValueTask<FlushResult> Flush(ConnectionContext connection) => connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes(_sb.ToString()));
}