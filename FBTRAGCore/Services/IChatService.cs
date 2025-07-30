namespace FBTRAGCore.Services
{
    public interface IChatService
    {
        Task<string> GetChatResponseAsync(string userQuery, string userId = "satish");
    }
}
