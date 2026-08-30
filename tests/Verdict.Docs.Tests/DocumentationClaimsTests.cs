using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Verdict.Docs.Tests;

/// <summary>
/// Checks the claims the documentation makes, because several of them drifted the
/// same way: the code changed and the prose did not.
/// </summary>
/// <remarks>
/// What this caught when it was written: the README said 525 tests when the suite
/// was 601, its coverage badge linked to a file that had moved into docs/internal,
/// and docs/README.md linked eight package guides under a directory that did not
/// exist. None of those needed a human to notice, and none of them will again.
/// </remarks>
public class DocumentationClaimsTests
{
    private static readonly string Root = FindRepositoryRoot();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Verdict.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory!.FullName;
    }

    public static TheoryData<string> MarkdownFiles() => new()
    {
        "README.md",
        "CONTRIBUTING.md",
        "SECURITY.md",
        Path.Combine("docs", "README.md"),
    };

    [Theory]
    [MemberData(nameof(MarkdownFiles))]
    public void EveryRelativeLinkResolves(string relativePath)
    {
        var path = Path.Combine(Root, relativePath);
        Assert.True(File.Exists(path), $"{relativePath} does not exist.");

        var text = File.ReadAllText(path);
        var directory = Path.GetDirectoryName(path)!;
        var broken = new List<string>();

        foreach (Match match in Regex.Matches(text, @"\]\(([^)\s]+)\)"))
        {
            var target = match.Groups[1].Value;

            // Absolute URLs and in-page anchors are somebody else's problem.
            if (target.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                || target.StartsWith("#", StringComparison.Ordinal)
                || target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var withoutAnchor = target.Split('#')[0];
            if (withoutAnchor.Length == 0)
            {
                continue;
            }

            var resolved = Path.GetFullPath(Path.Combine(directory, withoutAnchor));
            if (!File.Exists(resolved) && !Directory.Exists(resolved))
            {
                broken.Add(target);
            }
        }

        Assert.True(
            broken.Count == 0,
            $"{relativePath} links to {broken.Count} path(s) that do not exist: "
            + string.Join(", ", broken));
    }

    [Fact]
    public void TheReadmeDoesNotQuoteATestCount()
    {
        // A hard-coded count is drift waiting to happen: it was 525 while the
        // suite was 601, and nothing could have noticed. The suite size is CI's
        // to report, not the README's to promise.
        var text = File.ReadAllText(Path.Combine(Root, "README.md"));

        var matches = Regex.Matches(text, @"\b\d{3,}\s+tests\b", RegexOptions.IgnoreCase);

        Assert.True(
            matches.Count == 0,
            "README.md states an exact test count: "
            + string.Join(", ", matches.Select(m => m.Value))
            + ". Link to the CI run instead, so it cannot go stale.");
    }

    [Fact]
    public void TheUpgradeSectionMatchesTheShippingVersion()
    {
        // "Upgrading to 2.5.0" sat in the README for three minor versions.
        var version = Regex.Match(
            File.ReadAllText(Path.Combine(Root, "Directory.Build.props")),
            @"<VersionPrefix>([^<]+)</VersionPrefix>").Groups[1].Value;

        Assert.False(string.IsNullOrWhiteSpace(version), "VersionPrefix not found.");
        var major = version.Split('.')[0];

        var readme = File.ReadAllText(Path.Combine(Root, "README.md"));
        foreach (Match match in Regex.Matches(readme, @"##\s+Upgrading to ([0-9]+)\.[0-9]+"))
        {
            Assert.True(
                match.Groups[1].Value == major,
                $"README.md has an '{match.Value.Trim()}' section while the shipping version is "
                + $"{version}. Either the section is stale or the version is.");
        }
    }

    [Fact]
    public void SecurityPolicyCoversTheShippingVersion()
    {
        // SECURITY.md listed 1.0.x as the supported version while 2.8.0 shipped,
        // which reads as "the current release gets no security updates".
        var version = Regex.Match(
            File.ReadAllText(Path.Combine(Root, "Directory.Build.props")),
            @"<VersionPrefix>([^<]+)</VersionPrefix>").Groups[1].Value;
        var major = version.Split('.')[0];

        var policy = File.ReadAllText(Path.Combine(Root, "SECURITY.md"));

        Assert.True(
            policy.Contains($"{major}.", StringComparison.Ordinal),
            $"SECURITY.md does not mention the {major}.x line while {version} is what ships.");
    }
}
