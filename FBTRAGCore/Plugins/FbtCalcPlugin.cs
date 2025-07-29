using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace FBTRAGCore.Plugin
{
    public class FbtCalcPlugin
    {
        [KernelFunction("CalculateCarFBT")]
        [Description("Calculates FBT on car benefit")]
        public string CalculateCarFBT(double carValue, int daysAvailable)
        {
            const double statutoryRate = 0.20;
            const double grossUpRate = 2.0802;
            const double fbtRate = 0.47;

            double baseValue = carValue * statutoryRate * daysAvailable / 365;
            double grossedUp = baseValue * grossUpRate;
            double fbt = grossedUp * fbtRate;

            return $"Estimated FBT: ${fbt:F2}";
        }
    }
}
