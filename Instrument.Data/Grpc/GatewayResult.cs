namespace Instrument.Data.Grpc;
using System;


/// <summary>
/// Simple result wrapper for gateway operations
/// </summary>
public  class GatewayResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }

    public static GatewayResult<T> Success(T data, TimeSpan duration)
    {
        return new GatewayResult<T>
        {
            IsSuccess = true,
            Data = data,
            Duration = duration,
        };
    }

    public static GatewayResult<T> Failure(string error, TimeSpan duration)
    {
        return new GatewayResult<T>
        {
            IsSuccess = false,
            ErrorMessage = error,
            Duration = duration,
        };
    }
}
