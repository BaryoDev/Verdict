# Migrating from 2.4 to 2.5

One breaking change, which affects you only if you use `Verdict.Extensions` and
`MultiResult` / `ErrorCollection`. Everything else is additive.

```bash
dotnet add package Verdict --version 2.5.0
```

## Breaking: reading a disposed ErrorCollection now throws

`ErrorCollection` rents its buffer from `ArrayPool<Error>.Shared`. Until 2.5,
`Dispose()` returned the buffer to the pool but the struct kept the reference,
so every accessor carried on working and returned whatever the next renter of
that buffer had written. In a server that means one request reading another
request's errors.

Accessors now throw `ObjectDisposedException`: `Count`, `HasErrors`, `AsSpan()`,
`this[int]`, `First()` and `ToArray()`.

If your code read after disposal it was already returning wrong values. It now
fails loudly instead.

```csharp
// Before: returned stale or foreign data, silently.
// Now:    throws ObjectDisposedException.
var errors = ErrorCollection.Create(list);
errors.Dispose();
var first = errors[0];
```

### Fixing it

Finish reading inside the scope:

```csharp
using (var errors = ErrorCollection.Create(list))
{
    foreach (var e in errors.AsSpan()) { Report(e); }
}
```

Or copy out what you need before disposing:

```csharp
Error[] snapshot;
using (var errors = ErrorCollection.Create(list))
{
    snapshot = errors.ToArray();
}
Report(snapshot);
```

### Disposing a copy disposes them all

`ErrorCollection` is a **struct**, so copies share the same pooled buffer. If you
pass one to a method that disposes it, your copy is disposed too:

```csharp
void Handle(ErrorCollection errors) => errors.Dispose();   // disposes the caller's too

var errors = ErrorCollection.Create(list);
Handle(errors);
var count = errors.Count;      // now throws, correctly
```

Decide who owns disposal, usually the code that created it. Check `IsDisposed`
when ownership is unclear.

### Not affected

`ErrorCollection.Create(Error)` and `Create(params Error[])` allocate their own
array and never use the pool, so they are unaffected by disposal.

## Additive: equality no longer allocates

`Result<T>` and `Result` now implement `IEquatable<>` with `==`, `!=` and
`GetHashCode`. Before 2.5 they fell through to reflection-based
`ValueType.Equals`, allocating about 320 bytes per comparison.

```csharp
Result<int>.Success(1) == Result<int>.Success(1);        // now compiles
new HashSet<Result<int>> { a, b };                        // no longer allocates per comparison
```

If you relied on reference-style behaviour from `ValueType.Equals`, note that
successes now compare **by value** and failures **by error**.

## Additive: trimming and Native AOT

Every package except `Verdict.Json` is now `IsTrimmable` and `IsAotCompatible`.
`Verdict.Json` works under AOT if you register concrete converters with
`AddVerdictConverter<T>()` rather than the reflection-based factory. See the
[Verdict.Json guide](packages/json.md#trimming-and-native-aot).

## Additive: XML documentation

Packages now ship XML docs, so IntelliSense works for the first time. No action
needed.

## Corrected documentation

Two claims were wrong before 2.5 and are worth knowing if you designed against
them:

- **Thread safety.** `Result<T>` was described as thread-safe. It is immutable
  and safe to read concurrently, but it is 32-48 bytes, so reassigning a shared
  field from multiple threads can tear. See
  [Thread safety](../README.md#thread-safety).
- **GC pressure.** The README claimed 25 GB/sec saved at 100k req/sec. The
  correct figure from the same inputs is 18-38 MB/sec.
