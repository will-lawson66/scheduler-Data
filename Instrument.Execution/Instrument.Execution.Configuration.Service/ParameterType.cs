namespace Instrument.Execution.Parameter;

/// <summary>
/// basic types that are currently supported when creating  be passed in to gRPC request.
/// All of the types MUST be scalar types defined in
/// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types"/>
/// </summary>
public enum ParameterType
{
    BooleanType,
    StringType,
    IntegerType,
    DecimalType,
    EnumType,
    ArrayType
}
