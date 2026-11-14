using Microsoft.Extensions.Options;
using SmartSis.Api.Features.Chat.Interfaces;
using SmartSis.Api.Features.Chat.Repositories;
using SmartSis.Api.Features.Chat.Services;

namespace SmartSis.Api.Features.Chat;

public static class ChatModule
{
    public static IServiceCollection AddChatFeature(this IServiceCollection services)
    {
        services.AddOptions<AiServiceOptions>().BindConfiguration(AiServiceOptions.Section);
        services.AddOptions<KnowledgeBaseOptions>().BindConfiguration(KnowledgeBaseOptions.Section);

        services.AddHttpClient<IAiServiceClient, AiServiceClient>((sp, http) =>
        {
            var o = sp.GetRequiredService<IOptions<AiServiceOptions>>().Value;
            http.BaseAddress = new Uri(o.BaseUrl.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(o.TimeoutSeconds);
        });
        services.AddSingleton<IKnowledgeFileStore, LocalKnowledgeFileStore>();
        services.AddScoped<IBearerTokenAccessor, BearerTokenAccessor>();

        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IKnowledgeService, KnowledgeService>();
        return services;
    }
}
