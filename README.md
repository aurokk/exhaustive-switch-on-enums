# Exhaustive Switch On Enums

This is a roslyn analyzer that solves the problem of not exhaustive enums.

## Examples

```c#
public enum Status
{
    Accepted = 1,
    Cooking = 2,
    Cooked = 3,
}
```

### Not-exhaustive switch

```c#
public static void Method(Status status)
{
    switch (status)
    {
        case Status.Accepted:
            return;
        default:
            throw new ArgumentOutOfRangeException(nameof(status), status, null);
    }
}
```

```c#
public static void Method(Status status)
{
    switch (status)
    {
        case Status.Accepted:
            return;
        case Status.Cooking:
            throw new NotImplementedException();
        case Status.Cooked:
            throw new NotImplementedException();
        default:
            throw new ArgumentOutOfRangeException(nameof(status), status, null);
    }
}
```


### Not-exhaustive switch expression

```c#
public static string Method(Status status)
{
    return status switch
    {
        Status.Accepted => """",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
```

```c#
public static string Method(Status status)
{
    return status switch
    {
        Status.Accepted => """",
        Status.Cooking => throw new NotImplementedException(),
        Status.Cooked => throw new NotImplementedException(),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
```

## Treat warnings as errors

Also, you can make compiler treat warning about not-exhaustive switch as error this way.

```
<PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <WarningsAsErrors>ExhaustiveSwitchOnEnums</WarningsAsErrors>
</PropertyGroup>
```