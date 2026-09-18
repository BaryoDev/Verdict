# Verdict.Json

`System.Text.Json` converters for `Result<T>`, `Result` and `Error`.

```csharp
var options = VerdictJsonExtensions.CreateVerdictJsonOptions();
var json = JsonSerializer.Serialize(Result<int>.Success(42), options);
// {"isSuccess":true,"value":42}
```

## Reading untrusted input

This is the one package that reads bytes your process did not write, so it is
strict about what it accepts.

| Payload | Result |
|---|---|
| `{"isSuccess":true,"value":42}` | success carrying 42 |
| `{"isSuccess":true,"value":null}` | success carrying null, because the payload said so |
| `{"isSuccess":true}` | **`JsonException`** |
| `{"isSuccess":false}` | `JsonException`, no error supplied |
| `{"value":42}` | `JsonException`, no `isSuccess` |
| truncated or wrong-typed | `JsonException` |

A success with no `value` property used to become a success carrying
`default(T)`. For `Result<Uri>` that is a success carrying null, produced from a
truncated request body, and the null then travelled instead of being rejected at
the edge. The whole contract of the type is that a success has a value.

`Error.Exception` is never serialised. Only the code and the message cross the
wire.

## Native AOT

The convenience factory uses `MakeGenericType`, so it is annotated
`[RequiresDynamicCode]`. Under AOT, register the closed generic and serialise
through a `JsonTypeInfo` rather than through the options:

```csharp
[JsonSourceGenerationOptions(Converters = new[] { typeof(ResultJsonConverter<int>) })]
[JsonSerializable(typeof(Result<int>))]
internal partial class AppJsonContext : JsonSerializerContext { }

var json = JsonSerializer.Serialize(result, AppJsonContext.Default.ResultInt32);
```

The `JsonSerializer.Serialize(value, options)` overload carries
`RequiresDynamicCode` whatever converters are registered on the options, so it
warns under AOT even when everything it needs is source-generated. Use the
`JsonTypeInfo` overload and it does not.

`tests/Verdict.Aot.Smoke` is exactly this, published and run by CI.

**Known limitation.** `Error.Exception` is a public property, so a consumer's
source-generated context emits an `Exception` serializer, and that touches
`Exception.TargetSite`. Two IL2026 warnings follow. Suppress them narrowly, the
way that project does.
