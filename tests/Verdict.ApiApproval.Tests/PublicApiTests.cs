using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PublicApiGenerator;
using Xunit;

namespace Verdict.ApiApproval.Tests;

/// <summary>
/// Pins the public surface of every published package.
///
/// These packages are consumed by other people's code, so any change to a signature, an
/// accessibility, a base type or a default parameter is a breaking change for someone even
/// when every behavioural test still passes. Rendering the surface to text and comparing it
/// to a checked-in file turns that into a failing test and a reviewable diff.
///
/// When this test fails it is not necessarily wrong. Read the diff, decide whether the change
/// is additive or breaking, then approve it by copying the .received.txt over the .approved.txt
/// and committing both that and the version bump the change implies.
/// </summary>
public class PublicApiTests
{
    public static IEnumerable<object[]> Packages => new[]
    {
        new object[] { "Verdict" },
        new object[] { "Verdict.Async" },
        new object[] { "Verdict.AspNetCore" },
        new object[] { "Verdict.Extensions" },
        new object[] { "Verdict.Fluent" },
        new object[] { "Verdict.Json" },
        new object[] { "Verdict.Logging" },
        new object[] { "Verdict.Rich" }
    };

    [Theory]
    [MemberData(nameof(Packages))]
    public void PublicApiHasNotChanged(string package)
    {
        var assembly = Assembly.Load(package);

        var actual = assembly.GeneratePublicApi(new ApiGeneratorOptions
        {
            // Attributes the build emits rather than the author writes. They are noise in a
            // diff and they change with tooling upgrades, not with the API.
            ExcludeAttributes = new[]
            {
                "System.Diagnostics.DebuggerNonUserCodeAttribute",
                "System.Runtime.CompilerServices.CompilerGeneratedAttribute",
                "System.Runtime.CompilerServices.RefSafetyRulesAttribute",
                "System.Runtime.Versioning.TargetFrameworkAttribute",
                "System.Reflection.AssemblyMetadataAttribute",
                "System.Reflection.AssemblyCompanyAttribute",
                "System.Reflection.AssemblyConfigurationAttribute",
                "System.Reflection.AssemblyFileVersionAttribute",
                "System.Reflection.AssemblyInformationalVersionAttribute",
                "System.Reflection.AssemblyProductAttribute",
                "System.Reflection.AssemblyTitleAttribute",
                "System.Reflection.AssemblyVersionAttribute"
            }
        }).Trim().ReplaceLineEndings("\n");

        var approvedPath = ApprovedPathFor(package);
        var receivedPath = approvedPath.Replace(".approved.txt", ".received.txt");

        if (!File.Exists(approvedPath))
        {
            File.WriteAllText(receivedPath, actual);
            Assert.Fail(
                $"No approved API file for {package}. A new package needs its surface approved once.\n"
                + $"Review {receivedPath} and, if it is what you meant to publish, rename it to {Path.GetFileName(approvedPath)}.");
        }

        var approved = File.ReadAllText(approvedPath).Trim().ReplaceLineEndings("\n");

        if (approved == actual)
        {
            // Leave nothing behind from an earlier failing run.
            if (File.Exists(receivedPath))
            {
                File.Delete(receivedPath);
            }

            return;
        }

        File.WriteAllText(receivedPath, actual);

        Assert.Fail(
            $"The public API of {package} changed.\n\n"
            + $"{DescribeDiff(approved, actual)}\n"
            + $"If the change is intended, copy\n  {receivedPath}\nover\n  {approvedPath}\n"
            + "and make sure the version bump matches: additions are a minor, anything removed or "
            + "changed in place is a major.");
    }

    private static string ApprovedPathFor(string package)
    {
        // Walk up from the test binaries to the project directory so the approved files are
        // edited and committed in source rather than in bin.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Verdict.ApiApproval.Tests.csproj")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName ?? AppContext.BaseDirectory;
        var approvedDirectory = Path.Combine(root, "ApprovedApi");
        Directory.CreateDirectory(approvedDirectory);

        return Path.Combine(approvedDirectory, $"{package}.approved.txt");
    }

    private static string DescribeDiff(string approved, string actual)
    {
        var before = approved.Split('\n');
        var after = actual.Split('\n');

        var removed = before.Except(after).ToArray();
        var added = after.Except(before).ToArray();

        var lines = new List<string>();

        if (removed.Length > 0)
        {
            lines.Add($"Removed or changed ({removed.Length}):");
            lines.AddRange(removed.Take(25).Select(l => $"  - {l.Trim()}"));
            if (removed.Length > 25)
            {
                lines.Add($"  ... and {removed.Length - 25} more");
            }
        }

        if (added.Length > 0)
        {
            lines.Add($"Added ({added.Length}):");
            lines.AddRange(added.Take(25).Select(l => $"  + {l.Trim()}"));
            if (added.Length > 25)
            {
                lines.Add($"  ... and {added.Length - 25} more");
            }
        }

        return string.Join("\n", lines);
    }
}
