using System.IO;
using Xunit;
using Xunit.Abstractions;
using TRM.Core;

namespace TRM.Tests
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

            // Nutze die neue Methode
            var (directSlope, inverseSlopePhysical, intercept, rmaSlope) = SparcMrtParser.FitBtrf(galaxies);

            // Ausgaben zur Überprüfung
            _output.WriteLine($"Geparste Galaxien: {galaxies.Count}");
            _output.WriteLine($"Standard OLS Steigung (mit Bias): {directSlope:F3}");
            _output.WriteLine($"Invers bereinigte Steigung: {inverseSlopePhysical:F3}");

            // Dein Arbeitsmodell fordert eine fundamentale Steigung von 4.0
            // Der invers bereinigte Wert der SPARC-Daten liegt typischerweise bei ~3.9 - 4.1
            double expectedSlope = 4.0;
            double tolerance = 0.3;

            Assert.InRange(inverseSlopePhysical, expectedSlope - tolerance, expectedSlope + tolerance);
        }
    }
}
