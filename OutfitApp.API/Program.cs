using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OutfitApp.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

// Auth0 Configuration
var auth0Domain = builder.Configuration["Auth0:Domain"] ?? "dev-geqpxtl7uq7wpx6i.us.auth0.com";
var auth0ClientId = builder.Configuration["Auth0:ClientId"] ?? "mI6dJDUNZjwsIGIEZcb903RxJAAA95zS";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{auth0Domain}/";
        options.Audience = auth0ClientId; // ID token has ClientId as audience
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = $"https://{auth0Domain}/",
            ValidAudience = auth0ClientId
        };
    });

builder.Services.AddAuthorization();

// CORS – frontend'den erişim için
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Azure Blob Storage
var blobConnectionString = builder.Configuration["AzureBlobStorage:ConnectionString"]
    ?? throw new InvalidOperationException("AzureBlobStorage:ConnectionString is not configured.");
var blobContainerName = builder.Configuration["AzureBlobStorage:ContainerName"] ?? "clothing-images";

// Azure SQL Database
var sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

// Azure OpenAI
var openAiEndpoint = builder.Configuration["AzureOpenAI:Endpoint"]
    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured.");
var openAiApiKey = builder.Configuration["AzureOpenAI:ApiKey"]
    ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured.");
var openAiDeploymentName = builder.Configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";

// Azure DALL-E
var dalleEndpoint = builder.Configuration["AzureDalle:Endpoint"] ?? "";
var dalleApiKey = builder.Configuration["AzureDalle:ApiKey"] ?? "";
var dalleDeploymentName = builder.Configuration["AzureDalle:DeploymentName"] ?? "dall-e-3";

builder.Services.AddInfrastructure(blobConnectionString, blobContainerName, sqlConnectionString,
    openAiEndpoint, openAiApiKey, openAiDeploymentName,
    dalleEndpoint, dalleApiKey, dalleDeploymentName);

var app = builder.Build();

// OpenAPI + Scalar UI
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "OutfitApp API";
    options.Theme = ScalarTheme.Purple;
});

app.UseHttpsRedirection();
app.UseCors();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Root'a gelince upload sayfasına yönlendir
app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run();
