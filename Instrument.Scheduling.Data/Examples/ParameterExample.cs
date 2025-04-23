using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data;
using Instrument.Scheduling.Data.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Instrument.Scheduling.Data.Examples;

public class ParameterExample
{
    public static async Task RunExample()
    {
        // Setup dependency injection
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Configure for SQLite
                var config = new StorageConfiguration
                {
                    Provider = StorageProviderType.SQLite,
                    ConnectionString = "Data Source=scheduler.db"
                };

                services.AddSchedulerDataLayer(config);
                services.AddTransient<SequenceService>();
                services.AddTransient<ParameterService>();
            })
            .Build();

        // Get the services
        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var parameterService = serviceProvider.GetRequiredService<ParameterService>();
        var sequenceService = serviceProvider.GetRequiredService<SequenceService>();
        
        // Create sample parameters
        var tempParameter = new Parameter
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Temperature",
            ParameterType = "number",
            DefaultValue = "37.0",
            MinValue = "20.0",
            MaxValue = "60.0",
            Required = true,
            Description = "Operating temperature in degrees Celsius"
        };
        
        var timeParameter = new Parameter
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Duration",
            ParameterType = "number",
            DefaultValue = "60",
            MinValue = "1",
            MaxValue = "300",
            Required = true,
            Description = "Operation duration in seconds"
        };
        
        var modeParameter = new Parameter
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Mode",
            ParameterType = "string",
            DefaultValue = "Standard",
            Required = true,
            Description = "Operation mode"
        };
        
        // Save the parameters
        await parameterService.CreateParameterAsync(tempParameter);
        await parameterService.CreateParameterAsync(timeParameter);
        await parameterService.CreateParameterAsync(modeParameter);
        
        Console.WriteLine("Created parameters:");
        Console.WriteLine($"- {tempParameter.Name} (ID: {tempParameter.Id})");
        Console.WriteLine($"- {timeParameter.Name} (ID: {timeParameter.Id})");
        Console.WriteLine($"- {modeParameter.Name} (ID: {modeParameter.Id})");
        
        // Create a sequence
        var sequence = new Sequence
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Sample Processing",
            Description = "Basic sample processing sequence",
            WorstCaseTime = TimeSpan.FromSeconds(120)
        };
        
        await sequenceService.CreateSequenceAsync(sequence);
        Console.WriteLine($"\nCreated sequence: {sequence.Name} (ID: {sequence.Id})");
        
        // Associate parameters with the sequence
        await parameterService.AddParameterToSequenceAsync(sequence.Id, tempParameter.Id, "42.5");
        await parameterService.AddParameterToSequenceAsync(sequence.Id, timeParameter.Id, "120");
        await parameterService.AddParameterToSequenceAsync(sequence.Id, modeParameter.Id, "Advanced");
        
        Console.WriteLine("\nAssociated parameters with the sequence");
        
        // Retrieve and show the parameters for the sequence
        var sequenceParameters = await parameterService.GetParametersForSequenceAsync(sequence.Id);
        
        Console.WriteLine("\nParameters for sequence:");
        foreach (var param in sequenceParameters)
        {
            Console.WriteLine($"- {param.Name} (Type: {param.ParameterType}, Default: {param.DefaultValue})");
        }
        
        // Validate a parameter value
        var isValidTemp = parameterService.ValidateParameterValue(tempParameter, "45.0");
        var isInvalidTemp = parameterService.ValidateParameterValue(tempParameter, "75.0"); // Exceeds max value
        
        Console.WriteLine($"\nValidation results:");
        Console.WriteLine($"- Temperature 45.0°C is valid: {isValidTemp}");
        Console.WriteLine($"- Temperature 75.0°C is valid: {isInvalidTemp}");
        
        Console.WriteLine("\nExample completed successfully!");
    }
}
