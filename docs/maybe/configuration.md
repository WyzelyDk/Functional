# Configuration

[Back to the Maybe overview](../../src/Maybe/README.md)

Everything that can reasonably differ between applications is a setting with a sensible default, so nothing has to be configured.
Change settings once, at start-up:

```csharp
using Wyzely.Functional;

MaybeConfiguration.Configure(options =>
{
    options.SomeFormat = "Just({0})";                  // how a maybe with a value is shown by ToString()
    options.NoneText = "Nothing";                      // how a maybe without a value is shown
    options.NoValueMessage = "There is nothing to read here.";   // the message of GetValue() on none
});

Maybe.Some(5).ToString();                              // "Just(5)"
Maybe.None<int>().ToString();                          // "Nothing"
```

| Setting | Default | Affects |
|---|---|---|
| `SomeFormat` | `Some({0})` | `ToString()` of a maybe with a value; `{0}` is the value (or the redacted text). It must contain `{0}` and be a valid format string |
| `NoneText` | `None` | `ToString()` of a maybe without a value |
| `NoValueMessage` | `The maybe has no value. Check IsSome first, or use TryGetValue.` | the message of the `InvalidOperationException` that `GetValue()` throws on none |
| `ShowValueInToString` | `true` | whether `ToString()` shows the value; set it to `false` when maybes may hold data that must not reach logs |
| `RedactedValue` | `***` | the text shown instead of the value when `ShowValueInToString` is `false` |
| `ContinueOnCapturedContext` | `false` | whether the library's awaits continue on the captured synchronization context |

```csharp
using Wyzely.Functional;

MaybeConfiguration.Configure(options => options.ShowValueInToString = false);

Maybe.Some("secret").ToString();                       // "Some(***)"
Maybe.None<string>().ToString();                       // "None": there is nothing to hide

MaybeConfiguration.Reset();                            // back to the defaults
```

The configuration is process-wide, because maybes are created everywhere and passing an options object to every call would defeat a
lightweight type. A change is applied atomically, and `Configure` hands your action a copy, so a reference that you keep cannot change the
active settings later; if your action throws, nothing is changed. Settings are read each time a maybe is shown or read, so they take effect at
once, but configure them at start-up so that the output of the whole process is consistent. `MaybeConfiguration.Reset()` restores the
defaults (useful in tests that change them). A setting rejects `null`, and `SomeFormat` and `NoValueMessage` also reject blank text.

`samples/Maybe.Samples/ConfigurationExample.cs` runs every setting on this page, including a counting synchronization context that shows what
`ContinueOnCapturedContext` changes; the awaits of the library follow the setting (`MaybeConfigurationTests`).

`ContinueOnCapturedContext` is the setting of the `Maybe` package only. In the umbrella package the methods between `Maybe` and `Result`
follow the settings of both; see [Interop](../functional/interop.md#async-and-configureawait).
