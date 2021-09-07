using Serilog;

var builder = WebApplication.CreateBuilder(args);

/// <summary>
/// �������ĵ�������serilog
/// </summary>
builder.Host.UseSerilog((hostBuilderContext, loggerConfiguration) =>
{
    loggerConfiguration.ReadFrom.Configuration(hostBuilderContext.Configuration);
});

// Add services to the container.

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Demo_Serilog", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Demo_Serilog v1"));
}

/// <summary>
/// ������־��չ
/// </summary>
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("Host", httpContext.Request.Host);
        diagnosticContext.Set("Remote IP", httpContext.Connection.RemoteIpAddress);
        diagnosticContext.Set("ContentType", httpContext.Response.ContentType);
        diagnosticContext.Set("EndpointName", httpContext.GetEndpoint()?.DisplayName);
    };
});

#region api

/// <summary>
/// ������־�ȼ�
/// </summary>
app.MapGet("log_level", (Serilog.ILogger logger) =>
{
    logger.Debug("debug");
    logger.Information("information");
    logger.Warning("warning");
    logger.Error("error");
    logger.Fatal("fatal");

    return Results.Ok();
});

/// <summary>
/// �ṹ����־
/// from https://github.com/serilog/serilog/wiki/Structured-Data
/// </summary>
app.MapGet("log_structured_data", (Serilog.ILogger logger) =>
{
    var count = 456;
    logger.Information("Retrieved {Count} records", count);

    var fruit = new[] { "Apple", "Pear", "Orange" };
    logger.Information("In my bowl I have {Fruit}", fruit);

    var fruitDic = new Dictionary<string, int> { { "Apple", 1 }, { "Pear", 5 } };
    logger.Information("In my bowl I have {Fruit}", fruitDic);

    var sensorInput = new { Latitude = 25, Longitude = 134 };
    logger.Information("Processing {@SensorInput}", sensorInput);

    //ʹ�� $ ǿ�� ToString() , ��¼���� unknown.ToString()
    var unknown = new[] { 1, 2, 3 };
    logger.Information("Received {$Data}", unknown);
});

/// <summary>
/// ע������
/// </summary>
app.MapGet("log_notice", (Serilog.ILogger logger) =>
{
    //ʹ��ģ������ڴ����ļ��������� https://github.com/serilog/serilog/wiki/Writing-Log-Events#message-template-recommendations
    // Don't:
    Log.Information("The time is " + DateTime.Now);
    // Do:
    Log.Information("The time is {Now}", DateTime.Now);
}); 

#endregion

app.Run();
