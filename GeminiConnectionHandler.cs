public class GeminiConnectionHandler : ConnectionHandler
{
    private readonly ILogger<GeminiConnectionHandler> _logger;

    public GeminiConnectionHandler(ILogger<GeminiConnectionHandler> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        var result = await connection.Transport.Input.ReadAsync();
        var buffer = result.Buffer;

        if (buffer.IsSingleSegment)
        {
            var uri = new Uri(Encoding.UTF8.GetString(buffer.FirstSpan).Trim());

            await using var handler = new GeminiRequestHandler(_logger, connection, uri);
            await handler.HandleAsync();
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}