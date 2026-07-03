using System;
using System.IO;

namespace TRM.FormalProofs.Cli;

/// <summary>
/// Writes formatted proof execution reports to the console and to disk.
/// </summary>
public static class ReportWriter
{
    /// <summary>
    /// Writes a horizontal line to the console.
    /// </summary>
    public static void WriteLineSeparator()
    {
        Console.WriteLine(new string('=', 80));
    }

    /// <summary>
    /// Formats and prints a section header.
    /// </summary>
    public static void WriteHeader(string title)
    {
        WriteLineSeparator();
        Console.WriteLine($"[TRM EXACT RATIONAL PROOF] - {title.ToUpper()}");
        WriteLineSeparator();
    }

    /// <summary>
    /// Writes detailed proof content to the console.
    /// </summary>
    public static void WriteDetails(string details)
    {
        Console.WriteLine(details);
    }

    /// <summary>
    /// Writes a brief result summary to the console.
    /// </summary>
    public static void WriteSummary(string command, bool passed, string summaryText)
    {
        WriteLineSeparator();
        Console.WriteLine($"COMMAND EXECUTED: {command}");
        Console.ForegroundColor = passed ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"STATUS:           {(passed ? "PASSED" : "FAILED")}");
        Console.ResetColor();
        Console.WriteLine($"SUMMARY:          {summaryText}");
        WriteLineSeparator();
        Console.WriteLine();
    }

    /// <summary>
    /// Optionally saves the detailed log of the proof to a local markdown or text file.
    /// </summary>
    public static void SaveReportToFile(string filename, string details)
    {
        try
        {
            File.WriteAllText(filename, details);
            Console.WriteLine($"Saved detailed proof log to: {Path.GetFullPath(filename)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to save report to file. {ex.Message}");
        }
    }
}
