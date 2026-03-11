using OutfitApp.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

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

builder.Services.AddInfrastructure(blobConnectionString, blobContainerName, sqlConnectionString,
    openAiEndpoint, openAiApiKey, openAiDeploymentName);

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

app.MapControllers();

// Root'a gelince upload sayfasına yönlendir
app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run();
