using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace TRM.Tests.QuantumTests;

public class PlanckConsistencyTests
{
    private readonly ITestOutputHelper _output;
    public PlanckConsistencyTests(ITestOutputHelper output)
    {
        _output = output;
    }
}
