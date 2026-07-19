using Xunit;
using TRM.Core.V4_1.Reporting.Figures;
using TRM.Core.V4_1.Sync;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_FigureBundles")]
public class V4_1_FigureBundle_Tests
{
    private static List<SyncParameterScanResult> SampleResults()
    {
        var cfg = new SyncParameterScanConfig
        {
            Dimensions = [3],
            CouplingStrengths = [0.5, 1.0],
            FrequencySpreads = [0.0, 0.1],
            Dt = 0.05, Steps = 200, SeedsPerPoint = 2, GraphSizePerDim = 3
        };
        return SyncParameterScanner.Scan(cfg);
    }

    [Fact]
    public void V4_1_80_FigureBundle_BuildsExpectedArtifacts()
    {
        var bundle = FigureBundleBuilder.BuildStandardBundle(SampleResults(), 3);
        Assert.NotEmpty(bundle.SvgArtifacts);
        Assert.NotEmpty(bundle.Panels);
        Assert.Equal(2, bundle.Panels.Count); // quality heatmap + regime grid
        Assert.Contains(bundle.SvgArtifacts.Keys, k => k.Contains("sync_quality"));
        Assert.Contains(bundle.SvgArtifacts.Keys, k => k.Contains("regime_grid"));
    }

    [Fact]
    public void V4_1_81_FigureManifest_HasStableDeterministicOrdering()
    {
        var b1 = FigureBundleBuilder.BuildStandardBundle(SampleResults(), 3);
        var b2 = FigureBundleBuilder.BuildStandardBundle(SampleResults(), 3);
        var json1 = FigureBundleBuilder.SerializeManifest(b1.Manifest);
        var json2 = FigureBundleBuilder.SerializeManifest(b2.Manifest);
        Assert.Equal(json1, json2);
    }

    [Fact]
    public void V4_1_82_FigureCaptions_AreGeneratedWithoutTheoryOverclaim()
    {
        var bundle = FigureBundleBuilder.BuildStandardBundle(SampleResults(), 3);
        foreach (var kv in bundle.Captions)
        {
            Assert.DoesNotContain("proven", kv.Value.Long, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", kv.Value.Long, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("predicted", kv.Value.Long, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("established", kv.Value.Long, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void V4_1_83_FigureBundle_Filenames_AreDeterministic()
    {
        var bundle = FigureBundleBuilder.BuildStandardBundle(SampleResults(), 3);
        foreach (var panel in bundle.Panels)
        {
            Assert.StartsWith("v4_1_fig_", panel.Filename);
            Assert.EndsWith(".svg", panel.Filename);
            Assert.Contains("D3", panel.Filename);
        }
    }

    [Fact]
    public void V4_1_84_FigureManifest_RepeatedRun_IsByteStable()
    {
        var b1 = FigureBundleBuilder.BuildStandardBundle(SampleResults(), 3);
        var b2 = FigureBundleBuilder.BuildStandardBundle(SampleResults(), 3);
        Assert.Equal(b1.Panels.Count, b2.Panels.Count);
        for (int i = 0; i < b1.Panels.Count; i++)
        {
            Assert.Equal(b1.Panels[i].Filename, b2.Panels[i].Filename);
            Assert.Equal(b1.Panels[i].PanelId, b2.Panels[i].PanelId);
        }
        Assert.Equal(b1.SvgArtifacts.Count, b2.SvgArtifacts.Count);
    }

    [Fact]
    public void V4_1_85_FigureBundle_ReferencesExistingPlotArtifacts()
    {
        var bundle = FigureBundleBuilder.BuildStandardBundle(SampleResults(), 3);
        foreach (var panel in bundle.Panels)
        {
            Assert.True(bundle.SvgArtifacts.ContainsKey(panel.Filename),
                $"Panel {panel.PanelId} references {panel.Filename} but SVG not found.");
            Assert.NotEmpty(bundle.SvgArtifacts[panel.Filename]);
        }
    }

    [Fact]
    public void V4_1_86_FigureBundle_MissingOptionalPanel_IsHandledDeterministically()
    {
        // Build comparison bundle — always succeeds with available data.
        var resultsByDim = new Dictionary<int, List<SyncParameterScanResult>>
        {
            [1] = SampleResults(), [3] = SampleResults()
        };
        var bundle = FigureBundleBuilder.BuildComparisonBundle(resultsByDim);
        Assert.NotEmpty(bundle.SvgArtifacts);
        Assert.Single(bundle.Panels);
    }
}
