using System.IO;
using Xunit;
using Xunit.Abstractions;
using TRM.Core;

namespace TRM.Tests.CoreTests
{
    public class BtfRelationTests
    {
        private readonly ITestOutputHelper _output;

        public BtfRelationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Test_SparcData_Matches_Theoretical_Slope_Of_Four()
        {
            string filePath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");
            Assert.True(File.Exists(filePath));

            var galaxies = SparcMrtParser.ParseFile(filePath);

            // Use the updated fitting method
            var (directSlope, inverseSlopePhysical, intercept, rmaSlope) = SparcMrtParser.FitBtrf(galaxies);

            // Diagnostic output
            _output.WriteLine($"Parsed galaxies: {galaxies.Count}");
            _output.WriteLine($"Standard OLS slope (with bias): {directSlope:F3}");
            _output.WriteLine($"Inverse-corrected slope: {inverseSlopePhysical:F3}");

            // The working model expects a fundamental slope of 4.0
            // The inverse-corrected SPARC slope is typically around 3.9 - 4.1
            double expectedSlope = 4.0;
            double tolerance = 0.3;

            Assert.InRange(inverseSlopePhysical, expectedSlope - tolerance, expectedSlope + tolerance);
        }
    }
}
