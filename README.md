# Elmah.Shaolinq

A persistence provider for [Elmah](https://code.google.com/p/elmah/) using the [Shaolinq](https://github.com/tumtumtum/Shaolinq) library.

## Usage

Install the `Elmah.Shaolinq` package from [Nuget](https://www.nuget.org/packages/Elmah.Shaolinq/).

Adjust your `DataAccessModel` class to implement `IElmahDataAccessModel`:

```csharp
[DataAccessModel]
public abstract class MyDataAccessModel
    : DataAccessModel, IElmahDataAccessModel
{
    // ...other fields
    [DataAccessObjects]
    public abstract DataAccessObjects<DbElmahError> ElmahErrors { get; }
}
```

In your `web.config`, configure the Elmah error logger:
```xml
<elmah>
  <errorLog type="Elmah.Shaolinq.ShaolinqErrorLog, Elmah.Shaolinq"
    dataAccessModelType="MyAssembly.MyDataAccessModel, MyAssembly"
    dataAccessModelConfigSection="MyDataAccessModel" />
</elmah>
```
The `dataAccessModelConfigSection` attribute is optional, defaulting to the name of the `DataAccessModel` type if not specified.
