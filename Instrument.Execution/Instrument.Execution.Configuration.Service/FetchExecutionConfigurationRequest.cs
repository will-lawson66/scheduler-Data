namespace Instrument.Execution.Grpc.Configuration;

using ProtoBuf;

/// <summary>
/// A request used to specify a request to get the current
/// configuration.
/// </summary>
/// <param name="IncludeSequences">
/// Indicates whether or not the returning configurations should
/// include sequences. This also includes referenced hardware
/// resources.
/// </param>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public record FetchExecutionConfigurationRequest(
    bool IncludeSequences = false
);