using FBTRAGCore.Models;
using FBTRAGCore.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using System.Text.RegularExpressions;

namespace FBTRAGCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        public ChatController(Kernel kernel,IChatService chatService)
        {
            _kernel = kernel;
            ChatService = chatService;
        }

        protected Kernel _kernel { get; }
        public IChatService ChatService { get; }

        [HttpPost]
        [Route("send")]
        public async Task<IActionResult> SendAsync([FromBody] FBTChatRequest req)
        {
            var response = await ChatService.GetChatResponseAsync(req.Message);
            return Ok(new FBTChatResponse() { Reply = response.ToString() });
        }
    }
}
