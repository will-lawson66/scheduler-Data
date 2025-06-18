namespace Instrument.Execution.Grpc;
/// <summary>
/// A gRPC contract specifying the data for the
/// execution engine.
/// </summary>
/// <param name="Key">
/// A human readable key that uniquely identifies
/// the execution resource.
/// </param>
/// <param name="HasScriptingInterface">
/// Indicates if this resource be injected into scripts as a library
/// component.
/// </param>
/// <param name="ScriptingInterface">
/// The class, if any, that is the extended interface for the resource.
/// </param>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public record ExecutionResourceContract(
    string Key,
    bool HasScriptingInterface,
    string ScriptingInterface
);