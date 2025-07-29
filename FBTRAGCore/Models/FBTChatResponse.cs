namespace FBTRAGCore.Models
{
    public class FBTChatResponse
    {
        public string Reply { get; set; }

        public FBTChatResponse()
        {

        }

        public FBTChatResponse(string reply)
        {
            Reply = reply;
        }
    }
}
