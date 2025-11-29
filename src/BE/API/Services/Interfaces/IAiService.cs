namespace API.Services.Interfaces;

public interface IAiService
{
    Task<string> GetCompletionAsync(string systemPrompt, string userPrompt);
}
