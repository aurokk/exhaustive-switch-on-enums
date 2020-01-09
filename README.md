# Exhaustive Switch On Enums

[![Nuget](https://img.shields.io/nuget/v/ExhaustiveSwitchOnEnums?style=flat-square)](https://www.nuget.org/packages/ExhaustiveSwitchOnEnums/1.0.1)

This roslyn analyzer solves the problem of non-exhaustive switches 
on enums. It generates a warning when a switch is non-exhaustive.

## Examples

```c#
public enum Status
{
    Accepted = 1,
    Cooking = 2,
    Cooked = 3,
}
```

### Non-exhaustive switch

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


### Non-exhaustive switch expression

```c#
public static string Method(Status status)
{
    return status switch
    {
        Status.Accepted => "",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
```

```c#
public static string Method(Status status)
{
    return status switch
    {
        Status.Accepted => "",
        Status.Cooking => throw new NotImplementedException(),
        Status.Cooked => throw new NotImplementedException(),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
```

## Treat warnings as errors

Also, you can make a compiler treat the warning as an error, like this:

```
<PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <WarningsAsErrors>ExhaustiveSwitchOnEnums</WarningsAsErrors>
</PropertyGroup>
```

## Issues

Feel free to write me about any problems.
