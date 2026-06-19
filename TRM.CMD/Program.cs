using TRM.Core;

var analysis = new BulletClusterAnalysis2();
var dataPath = Path.Combine(AppContext.BaseDirectory, "Coma_Cluster_Chandra_temperature_all_profiles.dat");
var redshiftPath = Path.Combine(AppContext.BaseDirectory, "Coma_Cluster_Chandra_temperature_accept_main.tab");

if (!File.Exists(dataPath))
{
    Console.Error.WriteLine($"Datendatei nicht gefunden: {dataPath}");
    return;
}

if (!File.Exists(redshiftPath))
{
    Console.Error.WriteLine($"Redshift-Datei nicht gefunden: {redshiftPath}");
    return;
}



//////var allClusters = analysis.LoadAllClusterShells(dataPath);
//////var redshifts = analysis.LoadClusterRedshifts(redshiftPath);

//////// 3. SILENT 2D-GRID-SWEEP STARTEN
//////// Diese Methode jagt nun Druck-Trigger und Geometrie-Dämpfung (Beta) gleichzeitig durch das unbestechliche Orakel
//////analysis.FindBestPhysicalThresholdAndBeta(allClusters, redshifts);

//////// 4. AUSWERTUNG MIT DEN NEUEN OPTIMALEN PARAMETERN
//////// Sobald die Konsole dir die besten Werte ausgibt (z. B. Threshold = 6.00E-34, Beta = 0.45),
//////// tragen wir diese hier ein, um das finale, korrigierte Paper-Ergebnis zu generieren:
//////double optimizedThreshold = 6.00e-034;
//////double optimizedBeta = 0.45; // Beispielwert aus der erwarteten Dämpfung

//////analysis.EvaluatePhysicsDrivenBimodalTheory(
//////    allClusters,
//////    redshifts,
//////    C: 1.3195,
//////    alpha: -0.7589,
//////    baselineK: 0.1,
//////    pressureThreshold: optimizedThreshold,
//////    beta: optimizedBeta,
//////    resultsCsvPath: "TRM_V2.2_FinalResults.csv"
//////);

var cmbSolver = new CmbAcousticSolver();
var cmbSpectrum = cmbSolver.ComputeCmbSpectrum(maxL: 1200);

Console.WriteLine("\n--- CMB TT-SPEKTRUM GEFUNDENE PEAKS ---");
foreach (var res in cmbSpectrum.Where(r => r.MultipoleL % 100 == 0))
{
    Console.WriteLine($"Multipol l: {res.MultipoleL,4} | Leistung (TT-Schnitt): {res.AmplitudeTT:F2}");
}

//var results = analysis.FindOptimalKForAllClusters(dataPath, redshiftPath);

//analysis.FindBestPhysicalThreshold(allClusters, redshifts);

//var resultsCsvPath = Path.Combine(AppContext.BaseDirectory, "results.csv");
//analysis.EvaluatePhysicsDrivenBimodalTheory(allClusters, redshifts, resultsCsvPath: resultsCsvPath);






//analysis.DiagnosePhysicalThreshold(dataPath, redshiftPath);
//analysis.EvaluateBimodalTheoryForAllClusters(dataPath, redshiftPath);

//BulletClusterAnalysis.DeriveDynamicKzLaw(results, redshifts);

//Console.WriteLine("Cluster       | Best k | Min Error        | Points");
//Console.WriteLine("----------------------------------------------------");
//foreach (var result in results)
//{
//    Console.WriteLine($"{result.ClusterName,-12}| {result.BestK,6:F2} | {result.MinError,14:E2} | {result.PointCount,6}");
//}

//Console.WriteLine();
//Console.WriteLine("--------------- FIXED-z MODEL COMPARISON ---------------");
//Console.WriteLine($"clusterName | z fix       | k baseline  | error at baseline |  Best K | Best error | Improvement");
//foreach (var result in results)
//{
//    if (!allClusters.TryGetValue(result.ClusterName, out var shells))
//        continue;

//    if (!redshifts.TryGetValue(result.ClusterName, out var zCluster))
//        continue;

//    BulletClusterAnalysis.ComparePhysicsModels(
//        result.ClusterName,
//        shells,
//        zCluster,
//        result.BestK,
//        result.MinError,
//        0.1);
//}

//analysis.CalculateHydrostaticMass(shells);

//Console.WriteLine("Radius (kpc) | Mgrav (Reported) | M_Hydro (Calculated)");
//Console.WriteLine("-----------------------------------------------------");

//foreach (var shell in shells.Where(s => s.CalculatedMass > 0))
//{
//    Console.WriteLine($"{shell.RadiusKpc:F0} kpc | {shell.ReportedMass:E2} | {shell.CalculatedMass:E2}");
//}

//var csvPath = Path.Combine(AppContext.BaseDirectory, "Analyse.csv");
//BulletClusterAnalysis.ExportAnalysisToCsv(shells, csvPath);

