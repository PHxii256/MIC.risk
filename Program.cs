using MIC.risk.Authorization;
using MIC.risk.Data;
using MIC.risk.Interfaces;
using MIC.risk.Middleware;
using MIC.risk.Models;
using MIC.risk.Options;
using MIC.risk.Service;
using MIC.risk.Services;
using MIC.risk.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var signingKey = builder.Configuration["JWT:SigningKey"]
    ?? Environment.GetEnvironmentVariable("JWT__SigningKey");

if (string.IsNullOrWhiteSpace(signingKey))
{
    throw new InvalidOperationException(
        "JWT signing key is not configured. Set JWT:SigningKey or environment variable JWT__SigningKey.");
}

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
});

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "Paste only the JWT token. Example: eyJhbGciOi..."
        };

        return Task.CompletedTask;
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length > 0)
        {
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDBContext>();

builder.Services.Configure<FileUploadOptions>(
    builder.Configuration.GetSection(FileUploadOptions.SectionName));

builder.Services.Configure<FormOptions>(options =>
{
    var maxUploadBytes = builder.Configuration
        .GetSection(FileUploadOptions.SectionName)
        .GetValue<long?>(nameof(FileUploadOptions.MaxFileSizeBytes))
        ?? 10 * 1024 * 1024;

    options.MultipartBodyLengthLimit = maxUploadBytes;
});

builder.Services.AddDbContext<ApplicationDBContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddIdentity<AppUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDBContext>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKey))
    };
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IRiskReportService, RiskReportService>();
builder.Services.AddScoped<IAuthorizationHandler, RiskReportOwnerHandler>();
builder.Services.AddScoped<IRiskService, RiskService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IResourceEngagementService, ResourceEngagementService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IRiskActionService, RiskActionService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EditOrViewRiskReport", policy =>
        policy.Requirements.Add(new SameOwnerRequirement()));
});

var app = builder.Build();

var webRootPath = app.Environment.WebRootPath;
if (!string.IsNullOrWhiteSpace(webRootPath))
{
    Directory.CreateDirectory(Path.Combine(webRootPath, "uploads"));
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.AddPreferredSecuritySchemes("Bearer");
    });
}
else
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
