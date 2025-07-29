namespace FBTRAGCore.Services
{
    public interface IPdfIngestionService
    {
        Task IngestPdfAsync(Stream pdfStream, string fileName, string category);
    }
}
