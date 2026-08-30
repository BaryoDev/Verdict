# Security Policy

## Supported versions

| Version | Supported                      |
| ------- | ------------------------------ |
| 3.x     | Yes                            |
| 2.x     | Security fixes until 3.1 ships |
| 1.x     | No                             |

## What this library is exposed to

Verdict is not an internal value type. A `Result` is a courier, and knowing what
it carries and where it ends up is most of the threat model.

A `Result` is **built from** an exception raised by a database driver or an HTTP
client, or deserialised from a request body. It is **read into** a log message
and an HTTP response body. So the library sits directly on the path between
attacker-influenced input and two sinks that leak.

Three consequences shape the design:

**An error message is untrusted text.** It is where a username, a filename or a
request value gets interpolated. Messages are neutralised of control characters
and bounded in length at construction, in the core package, so a carriage return
cannot forge a line in a plain-text log sink and an oversized request value
cannot carry its size into the log.

**An exception message was written for an operator, not a client.** Anything
holding an exception is treated as internal by `Verdict.AspNetCore`: its message
and its code are both withheld from the response unless `IncludeExceptionDetails`
is explicitly turned on, and it maps to 500 rather than 400.
`Error.FromException` uses a constant code rather than the exception's type name,
because the type name identifies the data access stack.

**Deserialisation is a trust boundary.** `Verdict.Json` rejects a success that
carries no value rather than producing one holding `default(T)`, and rejects a
failure with no error. A malformed payload throws `JsonException` rather than
producing a plausible-looking result.

## What is yours rather than ours

- **Map your error codes to status codes.** Anything unmapped is 400, or 500 if
  it carries an exception. A domain failure you want reported as 5xx needs a
  mapping, or it will be reported as a client error and stay out of your alerting.
- **Decide whether messages reach clients.** `IncludeErrorMessage` defaults to
  true, which is right for messages you wrote for a client to read and wrong for
  anything else. Errors carrying an exception are handled for you.
- **Use the `HttpContext` overloads** of `ToHttpResult` and `ToActionResult` if
  more than one application shares a process. The overloads without one read
  process-wide statics.
- **Dispose pooled error collections before reading is finished, not after.** See
  [docs/packages/extensions.md](docs/packages/extensions.md).

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

1. Email `baryodev@gmail.com` with a description, the affected versions, steps to
   reproduce, and the impact as you see it.
2. Ask for the PGP key in that first mail if you want to encrypt what follows.

You will get an acknowledgement within 48 hours, a preliminary assessment within
five business days, and a fix or a mitigation within 30 days depending on what it
turns out to be. We follow coordinated disclosure and will ask for reasonable time
before anything is public.

Advisories are published on the repository's Security tab and referenced from
`CHANGELOG.md`.

## What has been checked

- Dependency advisories fail the build. `NuGetAudit` runs in `all` mode at `low`
  level with warnings as errors, so an advisory in a transitive package breaks CI
  rather than waiting to be noticed.
- There is no regular expression anywhere in `src/`, so there is no ReDoS surface.
- Every logging template is a compile-time constant, so user data cannot become a
  format string.
- Pooled buffers are returned with `clearArray: true`, so a later renter cannot
  observe a previous caller's errors, and exception references are not retained
  in the pool.
- `tests/Verdict.AspNetCore.Tests/SecureDefaultsTests.cs` asserts what a
  default-configured pipeline does, rather than what it can be configured to do.

---

Last updated: August 2026.
