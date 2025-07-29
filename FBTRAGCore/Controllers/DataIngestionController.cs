using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using OllamaSharp.Models.Chat;
using System.Text.RegularExpressions;
using FBTRAGCore.Models;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Http;
using FBTRAGCore.Services;

namespace FBTRAGCore.Controllers
{
    [Route("api/[controller]")]
    public class DataIngestionController : Controller
    {

        public DataIngestionController(Kernel kernel, IPdfIngestionService pdfIngestionService)
        {
            _kernel = kernel;
            PdfIngestionService = pdfIngestionService;
        }

        public Kernel _kernel { get; }
        public IPdfIngestionService PdfIngestionService { get; }

        [HttpPost]
        [Route("upload")]
        public async Task<IActionResult> UploadAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            var category = "FBT";
            PdfIngestionService.IngestPdfAsync(stream, file.FileName, category);
            return Ok("PDF processed successfully.");
        }
    }
}
