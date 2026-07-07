using Xunit;
using TRM.Core.V4_1.Analysis;
using TRM.Core.V4_1.Publication;
using TRM.Core.V4_1.Sync;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Publication")]
public class V4_1_Publication_Tests
{
    private static (PaperDocument doc, string md) GenerateSample()
    {
        var cfg = new SyncParameterScanConfig
        {
            Dimensions = [1, 2, 3],
            CouplingStrengths = [0.5, 1.0],
            FrequencySpreads = [0.0, 0.1],
            Dt = 0.05, Steps = 200, SeedsPerPoint = 2, GraphSizePerDim = 3
        };
        var results = SyncParameterScanner.Scan(cfg);
        var analysis = DimensionAnalysisEngine.Analyze(results);
        var conclusions = DimensionAnalysisEngine.GenerateConclusions(analysis);
        var doc = PaperGenerator.Generate(results, analysis, conclusions);
        var md = PaperGenerator.ToMarkdown(doc);
        return (doc, md);
    }

    [Fact]
    public void V4_1_100_Publication_SectionsGeneratedDeterministically()
    {
        var (doc1, _) = GenerateSample();
        var (doc2, _) = GenerateSample();
        Assert.Equal(doc1.Sections.Count, doc2.Sections.Count);
        for (int i = 0; i < doc1.Sections.Count; i++)
        {
            Assert.Equal(doc1.Sections[i].Title, doc2.Sections[i].Title);
            Assert.Equal(doc1.Sections[i].Content, doc2.Sections[i].Content);
        }
    }

    [Fact]
    public void V4_1_101_Publication_NoForbiddenWords()
    {
        var (_, md) = GenerateSample();
        Assert.DoesNotContain("proven", md, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("established", md, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("proof", md, StringComparison.OrdinalIgnoreCase);
        // "derived" may appear in negations ("not derived") — that is acceptable.
    }

    [Fact]
    public void V4_1_102_Publication_ClassificationLabelsPresent()
    {
        var (doc, _) = GenerateSample();
        foreach (var ce in doc.ClaimEvidence)
            Assert.True(ce.Classification is "Supported" or "Conditional" or "Inconclusive");
    }

    [Fact]
    public void V4_1_103_Publication_ReferencesFigureBundles()
    {
        var appendix = AppendixGenerator.Generate(
            DimensionAnalysisEngine.Analyze(SampleResults()),
            "Dimensions=[1,2,3], K=[0.5,1.0], spread=[0.0,0.1]",
            "See v4_1_fig_*.svg for figure bundles.");
        Assert.Contains("v4_1_fig_", appendix);
        Assert.Contains("D = 3 is HYPOTHESIS", appendix);
    }

    [Fact]
    public void V4_1_104_Publication_RepeatedGenerationIdentical()
    {
        var (_, md1) = GenerateSample();
        var (_, md2) = GenerateSample();
        Assert.Equal(md1, md2);
    }

    private static List<SyncParameterScanResult> SampleResults()
    {
        var cfg = new SyncParameterScanConfig
        {
            Dimensions = [3],
            CouplingStrengths = [0.5],
            FrequencySpreads = [0.1],
            Dt = 0.05, Steps = 200, SeedsPerPoint = 2, GraphSizePerDim = 3
        };
        return SyncParameterScanner.Scan(cfg);
    }
}
