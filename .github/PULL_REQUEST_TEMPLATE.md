## What this changes

<!-- One or two sentences. The diff says what; say why. -->

## Related issue

<!-- Fixes #123, or "none, this is a typo fix". -->

## Checklist

- [ ] A test fails without this change
- [ ] `dotnet test Verdict.sln` passes locally
- [ ] `CHANGELOG.md` updated under `## [Unreleased]`

## If the public API changed

- [ ] The `.approved.txt` files are updated and committed
- [ ] Additions only, so this is a minor bump
- [ ] Something was removed or changed in place, so this is a major, and the changelog has a migration note

## If this could affect performance

Benchmarks do not run on pull requests, so paste before and after numbers from
`dotnet run -c Release --project benchmarks/Verdict.Benchmarks` if the change touches a hot path.

<!-- before:
     after:  -->
