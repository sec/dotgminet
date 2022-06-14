global using Microsoft.AspNetCore.Connections;
global using Microsoft.AspNetCore.StaticFiles;
global using System.Runtime.InteropServices;
global using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost
    .ConfigureServices(o =>
    {
        o.AddTransient<GeminiRequestHandler>();
    })
    .UseKestrel(o =>
    {
        o.ListenAnyIP(1965, b =>
        {
            b.UseHttps(Path.Combine(AppContext.BaseDirectory, "certificate.pfx"), "", o =>
            {
                o.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
                o.AllowAnyClientCertificate();
            });

            b.UseConnectionHandler(() => b.ApplicationServices.GetRequiredService<GeminiRequestHandler>());
        });
    });

var app = builder.Build();
app.Run();

public static class Ext
{
    public static IConnectionBuilder UseConnectionHandler<T>(this IConnectionBuilder connectionBuilder, Func<T> factory)
        where T : ConnectionHandler => connectionBuilder.Run(connection
            => factory().OnConnectedAsync(connection));
}