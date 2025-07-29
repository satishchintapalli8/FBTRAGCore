namespace FBTRAGCore.Models
{
    public class FBTChatRequest
    {
        public string Message { get; set; }

        public FBTChatRequest(string message)
        {
            Message = message;
        }
    }    
}
