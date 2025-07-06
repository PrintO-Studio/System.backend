using Dumpify;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using PrintO;
using PrintO.Intergrations;
using PrintO.Models;
using System.Text;
using System.Text.Json.Serialization;
using Zorro;
using Zorro.Middlewares;
using Zorro.Modules.Infisical;
using Zorro.Services;
using static Zorro.Secrets.GetSecretValueUtility;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

InfisicalSettings infisicalSettings = new InfisicalSettings()
{
    clientId = configuration["InfisicalSettings:clientId"]
        ?? Environment.GetEnvironmentVariable("INFISICAL_CLIENT_ID")!,
    clientSecret = configuration["InfisicalSettings:clientSecret"]
        ?? Environment.GetEnvironmentVariable("INFISICAL_CLIENT_SECRET")!,
    projectId = configuration["InfisicalSettings:projectId"]
        ?? Environment.GetEnvironmentVariable("INFISICAL_PROJECT_ID")!,
    URL = configuration["InfisicalSettings:URL"]
        ?? Environment.GetEnvironmentVariable("INFISICAL_URL")!
};

MySQLService.ConnectionStringMaster = new MySQLService.ConnectionStringBuilder(builder =>
{
    builder.AllowPublicKeyRetrieval = false;
    builder.SslMode = MySqlConnector.MySqlSslMode.Disabled;
    builder.Server = GetSecretValue("/MYSQL", "SERVER");
    builder.Port = GetSecretValue("/MYSQL", "PORT", uint.Parse);
    builder.Database = GetSecretValue("/MYSQL", "DATABASE");
    builder.UserID = GetSecretValue("/MYSQL", "USER");
    builder.Password = GetSecretValue("/MYSQL", "PASSWORD");

    return builder;
});

MinIOService.SettingsMaster = new MinIOService.MinIOSettingsBuilder(settings =>
{
    settings.secure = true;
    settings.defaultBucket = GetSecretValue("/MINIO", "BUCKET");
    settings.endpoint = GetSecretValue("/MINIO", "ENDPOINT");
    settings.accessKey = GetSecretValue("/MINIO", "ACCESS_KEY");
    settings.secretKey = GetSecretValue("/MINIO", "SECRET_KEY");

    return settings;
});

JwtBearerService.TokenValidationMaster = new JwtBearerService.TokenValidationBuilder(validation =>
{
    var keyBytes = Encoding.UTF8.GetBytes(GetSecretValue("/BEARER", "KEY"));
    validation.IssuerSigningKey = new SymmetricSecurityKey(keyBytes);
    validation.ValidateIssuerSigningKey = true;

    validation.ValidAudience = GetSecretValue("/BEARER", "AUDIENCE");
    validation.ValidateAudience = true;

    validation.ValidIssuer = GetSecretValue("/BEARER", "ISSUER");
    validation.ValidateIssuer = true;

    validation.ValidateLifetime = true;

    return validation;
});


ZorroDI
    .InitRaw(args)
    .AddInfisical(infisicalSettings)
    .AddDatabase<DataContext>(MySQLService.UseMySQL)
    .AddAuthAndIdentity<User, DataContext, UserRole, int>(JwtBearerService.UseJwtBearer)
    .AddMinIO()
    .AddSwaggerGen(s =>
    {
        s.CustomSchemaIds(t => t.FullName!.Replace('+', '.'));
    })
    .AddCors(options =>
    {
        options.AddDefaultPolicy(builder =>
        {
            List<string> origins = ["https://dashboard.printo.studio"];
            if (ZorroDI.environment == Zorro.Enums.Environment.Development || ZorroDI.environment == Zorro.Enums.Environment.Staging)
            {
                origins.Add("http://localhost:5173");
            }

            builder
                .WithOrigins(origins.ToArray())
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();

            if (ZorroDI.environment == Zorro.Enums.Environment.Development)
                builder.SetIsOriginAllowed(hostName => true);
        });
    })
    .Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024; // 2Gb
    })
    .AddScoped<OzonIntegration>()
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        //options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
    });

var app = ZorroDI.Build();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseCors();

ZorroDI.application.UseSwagger().UseSwaggerUI();

ZorroDI.Run(port: 5000);