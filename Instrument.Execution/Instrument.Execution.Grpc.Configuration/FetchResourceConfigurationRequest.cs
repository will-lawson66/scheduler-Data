namespace Instrument.Execution.Grpc.Configuration;

using ProtoBuf;

/// <summary>
/// A gRPC request contract for fetching a specific resource. 
/// </summary>
/// <param name="ResourceKey">
/// The specific resource key.
/// </param>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public record FetchResourceConfigurationRequest(string ResourceKey);
