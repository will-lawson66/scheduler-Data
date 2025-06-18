namespace Instrument.Execution.Grpc.Configuration;

using ProtoBuf;

/// <summary>
/// A gRPC request contract for fetching a specific sequience. 
/// </summary>
/// <param name="SequenceKey">
/// The specific sequence key.
/// </param>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public record FetchSequenceConfigurationRequest(string SequenceKey);
