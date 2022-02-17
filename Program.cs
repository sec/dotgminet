global using Microsoft.AspNetCore.Connections;
global using Microsoft.AspNetCore.StaticFiles;
global using System.Runtime.InteropServices;
global using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseKestrel(o =>
{
    o.ListenAnyIP(1965, b =>
    {
        b.UseHttps(Path.Combine(AppContext.BaseDirectory, "certificate.pfx"), "", o =>
        {
            o.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
            o.AllowAnyClientCertificate();
        });
        b.UseConnectionHandler<GeminiConnectionHandler>();
    });
});

var app = builder.Build();
app.Run();