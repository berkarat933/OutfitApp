using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OutfitApp.Core.Interfaces;
using OutfitApp.Infrastructure.Data;
using OutfitApp.Infrastructure.Services;

namespace OutfitApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string blobConnectionString,
        string blobContainerName,
        string sqlConnectionString,
        string openAiEndpoint,
        string openAiApiKey,
        string openAiDeploymentName,
        string dalleEndpoint,
        string dalleApiKey,
        string dalleDeploymentName = "dall-e-3")
    {
        // Azure Blob Storage
        services.AddSingleton<IBlobStorageService>(
            new AzureBlobStorageService(blobConnectionString, blobContainerName));

        // Azure OpenAI (GPT-4o Vision)
        services.AddSingleton<IClothingAnalysisService>(
            new AzureOpenAIClothingAnalysisService(openAiEndpoint, openAiApiKey, openAiDeploymentName));

        // Azure DALL-E 3 for outfit image generation
        services.AddSingleton<IOutfitImageService>(
            new AzureDalleService(dalleEndpoint, dalleApiKey, dalleDeploymentName));

        // Background Removal (currently disabled - Azure deprecated the API)
        services.AddSingleton<IBackgroundRemovalService>(
            new AzureComputerVisionService("", ""));

        // EF Core + Azure SQL with retry on transient failures
        services.AddDbContext<OutfitAppDbContext>(options =>
            options.UseSqlServer(sqlConnectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            }));

        return services;
    }
}
