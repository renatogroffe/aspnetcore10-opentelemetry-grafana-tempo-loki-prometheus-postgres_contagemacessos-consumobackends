using APIContagem;
using APIContagem.Clients;
using APIContagem.Data;
using APIContagem.Models;
using APIContagem.Tracing;
using Grafana.OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<APIExternaClient>();

builder.Services.AddDbContext<ContagemPostgresContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("BaseContagemPostgres"),
        o => o.UseNodaTime());
});

var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: OpenTelemetryExtensions.ServiceName,
        serviceVersion: OpenTelemetryExtensions.ServiceVersion);
builder.Services.AddOpenTelemetry()
    .WithTracing((traceBuilder) =>
    {
        traceBuilder
            .AddSource(OpenTelemetryExtensions.ServiceName)
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .UseGrafana();
    });
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(resourceBuilder);
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.AttachLogsToActivityEvent();
    options.UseGrafana();
});
builder.Services.AddOpenTelemetry()
    .WithMetrics((metricBuilder) =>
    {
        metricBuilder.AddView(
            "http.server.request.duration",
            new ExplicitBucketHistogramConfiguration()
            {
                Boundaries = [0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
            }
        );
        metricBuilder.AddMeter(
            "System.Diagnostics.Metrics",
            "Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Server.Kestrel",
            "System.Net.Http");
        metricBuilder
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter()
            .AddPrometheusExporter(options =>
            {
                options.ScrapeResponseCacheDurationMilliseconds = 0;
            })
            .UseGrafana();
    });

builder.Services.AddOpenApi();

builder.Services.AddScoped<ContagemRepository>();
builder.Services.AddSingleton<Contador>();

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapOpenApi();

Lock ContagemLock = new();

app.MapGet("/contador", async (ContagemRepository repository, Contador contador, APIExternaClient apiExternaClient) =>
{
    using var activity1 = OpenTelemetryExtensions.ActivitySource
        .StartActivity("GerarValorContagem")!;
            
    int valorAtualContador;
    using (ContagemLock.EnterScope())
    {
        contador.Incrementar();
        valorAtualContador = contador.ValorAtual;
    }
    activity1.SetTag("valorAtual", valorAtualContador);
    app.Logger.LogInformation($"Contador - Valor atual: {valorAtualContador}");

    
    using var activity1ConsumoJava = OpenTelemetryExtensions.ActivitySource
        .StartActivity("RegistrarConsumoApiJava")!;
    var resultadoJava = await apiExternaClient.ConsumirApiExternaAsync(true);
    activity1ConsumoJava.Stop();

    using var activity1ConsumoNodeJs = OpenTelemetryExtensions.ActivitySource
        .StartActivity("RegistrarConsumoApiNodeJs")!;
    var resultadoNodeJs = await apiExternaClient.ConsumirApiExternaAsync(false);
    activity1ConsumoNodeJs.Stop();

    var resultadoContador = new ResultadoContador()
    {
        ValorAtual = contador.ValorAtual,
        Local = contador.Local,
        Kernel = contador.Kernel,
        Framework = contador.Framework,
        Mensagem = app.Configuration["Saudacao"],
        ResultadoAPIExternaJava = resultadoJava,
        ResultadoAPIExternaNodeJs = resultadoNodeJs
    };
    activity1.Stop();

    using var activity2 = OpenTelemetryExtensions.ActivitySource
        .StartActivity("RegistrarRetornarValorContagem")!;

    repository.Insert(resultadoContador);
    app.Logger.LogInformation($"Registro inserido com sucesso! Valor: {valorAtualContador}");

    activity2.SetTag("valorAtual", valorAtualContador);
    activity2.SetTag("horario", $"{DateTime.UtcNow.AddHours(-3):HH:mm:ss}");

    return Results.Ok(resultadoContador);
})
.Produces<ResultadoContador>();

app.MapGet("/badrequest", () =>
{
    using var activity = OpenTelemetryExtensions.ActivitySource
        .StartActivity("SimularBadRequest")!;

    activity.SetTag("erro", "Simulação de Bad Request");
    app.Logger.LogWarning("Simulação de Bad Request realizada.");

    return Results.BadRequest(new { Erro = "Este é um Bad Request simulado." });
});

app.MapGet("/error", () =>
{
    using var activity = OpenTelemetryExtensions.ActivitySource
        .StartActivity("SimularErroInterno")!;

    activity.SetTag("erro", "Simulação de erro interno");
    app.Logger.LogError("Simulação de erro interno (500).");

    throw new InvalidOperationException("Erro simulado para teste de métricas");
});

app.Run();