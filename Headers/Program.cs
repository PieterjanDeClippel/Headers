var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHealthChecks();
builder.Services.AddAuthentication("certscheme")
    .AddCertificate("certscheme", o =>
    {
        o.AllowedCertificateTypes = Microsoft.AspNetCore.Authentication.Certificate.CertificateTypes.All;
        o.RevocationMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck;
        o.Events = new Microsoft.AspNetCore.Authentication.Certificate.CertificateAuthenticationEvents
        {
            OnCertificateValidated = (context) =>
            {
                context.Success();
                return Task.CompletedTask;
            }
        };
    });

builder.WebHost.ConfigureKestrel(k =>
{
    k.ConfigureHttpsDefaults(http =>
    {
        http.ClientCertificateValidation = (_, __, ___) => true;
        http.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.RequireCertificate;
    });
});
builder.WebHost.UseKestrelHttpsConfiguration();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHealthChecks("/healtz");
app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

app.MapGet("/", async (context) =>
{
    var head = string.Join(Environment.NewLine, context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"));
    var cert = context.Connection.ClientCertificate != null;
    var subj = context.Connection.ClientCertificate?.SubjectName != null;
    var name = context.Connection.ClientCertificate?.SubjectName?.Name != null;
    await context.Response.WriteAsJsonAsync(new { head, cert = new { cert, subj, name } });
});

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
