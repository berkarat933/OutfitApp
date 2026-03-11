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
        string openAiDeploymentName)
    {
        // Azure Blob Storage
        services.AddSingleton<IBlobStorageService>(
            new AzureBlobStorageService(blobConnectionString, blobContainerName));

        // Azure OpenAI (GPT-4o Vision)
        services.AddSingleton<IClothingAnalysisService>(
            new AzureOpenAIClothingAnalysisService(openAiEndpoint, openAiApiKey, openAiDeploymentName));

        // EF Core + Azure SQL
        services.AddDbContext<OutfitAppDbContext>(options =>
            options.UseSqlServer(sqlConnectionString));

        return services;
    }
}
