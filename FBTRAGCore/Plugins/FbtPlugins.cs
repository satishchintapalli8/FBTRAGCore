using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace FBTRAGCore.Plugin
{
    public class FbtPlugins
    {
        [KernelFunction("get_summarize")]
        [Description("Summarizes the user's FBT query in 2–3 lines.")]
        public string Summarize([Description("User's full question or FBT detail.")] string input)
        {
            return $"Summarizing FBT info: {input}";
        }

        [KernelFunction("get_licensed_periods")]
        [Description("Licensed periods information")]
        public string GetLicensedPeriods()
        {
            return string.Join(", ", new[] { "2022-23", "2023-24", "2024-25" });
        }
    }
}
