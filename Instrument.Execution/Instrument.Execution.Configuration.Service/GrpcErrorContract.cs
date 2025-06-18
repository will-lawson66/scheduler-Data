namespace Instrument.Grpc;

using System.Collections.Generic;
using ProtoBuf;

/// <summary>
/// Provides gRPC messages additional details regarding errors above the
/// protocol layer. Modeled after Google's best practices when reporting
/// errors. 
/// </summary>
/// <param name="ErrorCode">
/// An error code that uniquely identifies the issue that was encountered.
/// </param>
/// <param name="Message">
/// A human readable message summarizing the issue that was encountered.
/// </param>
/// <param name="Details">
/// Any additional details provided that could help debug the issue that
/// was encountered. 
/// </param>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public sealed record GrpcErrorContract(
    int ErrorCode, string Message, IReadOnlyCollection<string> Details);