# Contributing to Verdict

Contributions are welcome, including small ones. Fixing a typo in a doc comment is a real
contribution and does not need a discussion first.

## Before you start

**For a bug, open an issue with a reproduction.** A failing test, a small console program, or
the exact calls that produce the wrong result. A reproduction is worth more than a careful
description, because it turns a discussion into something that can be measured.

**For a new feature, open an issue first.** Not to gatekeep, but because Verdict has a strict
constraint that decides most design questions before they are argued (see below), and it is
better to find out early that an idea belongs in an extension package rather than the core.

## The one constraint that shapes everything

**The core `Verdict` package allocates nothing on the success path, and that is enforced by a
test.** It is the reason the library exists. Everything else is negotiable.

In practice:

- Core types are `readonly struct`. Prefer that for anything new.
- New features belong in an extension package, not in core, unless they cost nothing.
- Watch for boxing, captured variables in lambdas, `params` arrays, interface returns and LINQ
  in hot paths. Each of those is a heap allocation.

## What CI will check

Every one of these has been deliberately broken and confirmed to fail, so if one trips it is
telling you something real rather than being flaky.

| Gate | What trips it |
|---|---|
| **Tests** | 598 across nine projects |
| **Allocation** | `AllocationTests` measures `GC.GetAllocatedBytesForCurrentThread` over a thousand iterations. Any allocation on the success path, the failure path or the accessors fails |
| **Public API** | `Verdict.ApiApproval.Tests` renders each package's public surface and compares it to a checked-in file. Any signature, accessibility or default-parameter change fails |
| **Trim and AOT** | The net8.0 targets run the trim analyzer, and warnings are errors. Reflection over a generic parameter, for instance, fails as `IL2090` |
| **XML docs** | Every public member needs a `///` comment. A missing one is `CS1591`, and warnings are errors |

### When the API approval test fails

That is usually correct rather than a nuisance. Read the diff it prints, then decide:

- **Something added?** That is a minor version bump.
- **Anything removed, renamed or changed in place?** That is a major, and it needs a note in
  `CHANGELOG.md` with a migration example.

To approve the new surface, copy the generated `.received.txt` over the matching
`.approved.txt` in `tests/Verdict.ApiApproval.Tests/ApprovedApi/` and commit it with your change.

### When the allocation test fails

Do not raise the threshold. The number is zero on purpose. Find the allocation.

## Running things locally

```bash
dotnet build Verdict.sln
dotnet test Verdict.sln

# just the gates
dotnet test tests/Verdict.Tests/Verdict.Tests.csproj --filter "FullyQualifiedName~AllocationTests"
dotnet test tests/Verdict.ApiApproval.Tests/Verdict.ApiApproval.Tests.csproj

# benchmarks, Release only or the numbers are meaningless
dotnet run -c Release --project benchmarks/Verdict.Benchmarks
```

## Benchmarks are not a pull request gate, deliberately

BenchmarkDotNet on a shared CI runner has enough run-to-run variance to fail an innocent pull
request, and a check that cries wolf is one people learn to ignore. Timing therefore runs on a
schedule and on demand, not on every push.

What does gate a pull request is the allocation count, because that is deterministic. If your
change could plausibly affect throughput, run the benchmarks locally and put the before and
after numbers in the pull request.

## Pull requests

- One change per pull request. A fix and a refactor in the same diff are hard to review and
  harder to revert.
- Add a test that fails without your change. If you cannot write one, say so in the description
  and explain why.
- Update `CHANGELOG.md` under an `## [Unreleased]` heading. Say what broke and how to migrate,
  not just what changed.
- Commit messages: explain why, not what. The diff already says what.

## Code of conduct

Be decent. Assume the other person is trying to help. Disagreement about a technical decision
is fine and useful; disparaging the person making it is not.

## Licence

Verdict is MPL-2.0. Contributions are accepted under the same licence.
