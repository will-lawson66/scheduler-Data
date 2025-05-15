using Instrument.Data;
using Instrument.Data.Configuration;
using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Instrument.Data.Initialization;
using Instrument.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Set up configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

// Configure services
var services = new ServiceCollection();

// Add logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Warning);
});

// Get storage configuration
var storageConfig = new StorageConfiguration();
configuration.GetSection("DataStorage").Bind(storageConfig);

// Add data services with initialization
services.AddSchedulerDataWithInitialization(storageConfig);
services.AddCleanupServices();

// Build service provider
using var serviceProvider = services.BuildServiceProvider();

// Get required services for demos
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("SchedulerDemo");
var dbContext = serviceProvider.GetRequiredService<SchedulerDbContext>();

try
{
    // Create data initializer
    var factory = serviceProvider.GetRequiredService<DataInitializerFactory>();
    var initializer = factory.CreateInitializer(storageConfig);

    // Check if storage exists
    bool exists = await initializer.ExistsAsync();
    Console.WriteLine($"Storage exists: {exists}");

    if (!exists)
    {
        Console.WriteLine("Initializing storage...");
        await initializer.InitializeAsync();
        Console.WriteLine("Storage initialized successfully");

        // Apply migrations for database storage
        bool migrationsApplied = await initializer.MigrateAsync();
        if (migrationsApplied)
        {
            Console.WriteLine("Migrations applied successfully");
        }
    }

    // Get storage status
    string status = await initializer.GetStatusMessageAsync();
    Console.WriteLine("Storage status:");
    Console.WriteLine(status);

    bool exit = false;
    while (!exit)
    {
        // Show menu
        Console.WriteLine("\n===== Scheduler Data Layer Demo =====");
        Console.WriteLine("5. Demonstrate Full Workflow");
        Console.WriteLine("6. Clear All Data");
        Console.WriteLine("7. Exit");
        Console.Write("\nEnter your choice (1-7): ");

        var key = Console.ReadKey();
        Console.WriteLine();

        switch (key.KeyChar)
        {
            case '5':
                await DemoFullWorkflowAsync(serviceProvider);
                break;
            case '6':
                await ClearAllDataAsync(serviceProvider);
                break;
            case '7':
                exit = true;
                break;
            default:
                Console.WriteLine("Invalid choice. Please try again.");
                break;
        }

        if (!exit)
        {
            Console.WriteLine("\nPress any key to return to menu...");
            Console.ReadKey();
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

async Task DemoFullWorkflowAsync(ServiceProvider provider)
{
    Console.WriteLine("\n===== Full Workflow Demo =====");

    try
    {
        var parameterService = provider.GetRequiredService<IParameterService>();
        var sequenceService = provider.GetRequiredService<ISequenceService>();
        var sequenceGroupService = provider.GetRequiredService<ISequenceGroupService>();
        var sequenceGroupCollectionService = provider.GetRequiredService<ISequenceGroupCollectionService<Technology>>();

        // Step 1: Create sequences with parameters
        Console.WriteLine("\nStep 1: Creating sequences with parameters...");

        // Create a parameter for reagent volume
        var reagentVolumeParam = new Parameter
        {
            Name = "Reagent Volume",
            Type = ParameterType.IntegerType,
            DefaultValue = "50",
            Min = "10",
            Max = "100",
            Format = "N0"
        };

        await parameterService.CreateParameterAsync(reagentVolumeParam);

        // Create a parameter for temperature
        var temperatureParam = new Parameter
        {
            Name = "Temperature",
            Type = ParameterType.DecimalType,
            DefaultValue = "37.0",
            Min = "20.0",
            Max = "50.0",
            Format = "N1"
        };

        await parameterService.CreateParameterAsync(temperatureParam);

        // Create a parameter for time
        var timeParam = new Parameter
        {
            Name = "Incubation Time",
            Type = ParameterType.IntegerType,
            DefaultValue = "60",
            Min = "30",
            Max = "300",
            Format = "N0"
        };

        await parameterService.CreateParameterAsync(timeParam);

        // Create sequences
        var prepSequence = new Sequence
        {
            Name = "Preparation Sequence",
            Description = "Prepares the samples for processing",
            WorstCaseTime = TimeSpan.FromSeconds(30),
            CanBeParallel = false
        };

        var processSequence = new Sequence
        {
            Name = "Processing Sequence",
            Description = "Processes the samples",
            WorstCaseTime = TimeSpan.FromSeconds(120),
            CanBeParallel = true
        };

        var washSequence = new Sequence
        {
            Name = "Wash Sequence",
            Description = "Washes the samples",
            WorstCaseTime = TimeSpan.FromSeconds(45),
            CanBeParallel = true
        };

        var analyzeSequence = new Sequence
        {
            Name = "Analyze Sequence",
            Description = "Analyzes the samples",
            WorstCaseTime = TimeSpan.FromSeconds(60),
            CanBeParallel = false
        };

        await sequenceService.CreateSequenceAsync(prepSequence);
        await sequenceService.CreateSequenceAsync(processSequence);
        await sequenceService.CreateSequenceAsync(washSequence);
        await sequenceService.CreateSequenceAsync(analyzeSequence);

        // Link parameters to sequences
        await sequenceService.AddParameterToSequenceAsync(reagentVolumeParam.Id, washSequence.Id);
        await sequenceService.AddParameterToSequenceAsync(temperatureParam.Id, prepSequence.Id);
        await sequenceService.AddParameterToSequenceAsync(timeParam.Id, processSequence.Id);
        await sequenceService.AddParameterToSequenceAsync(timeParam.Id, analyzeSequence.Id);

        Console.WriteLine("Created sequences and parameters successfully");

        // Step 2: Create a sequence group template
        Console.WriteLine("\nStep 2: Creating a sequence group template...");

        var templateGroup = await sequenceGroupService.CreateSequenceGroupAsync(
            "Demo Sample Processing Template",
            "Template for processing samples"
        );

        // Add sequences to the template in order
        await sequenceGroupService.AddSequenceToSequenceGroupAsync(templateGroup.Id, prepSequence.Id, 1);
        await sequenceGroupService.AddSequenceToSequenceGroupAsync(templateGroup.Id, processSequence.Id, 2);
        await sequenceGroupService.AddSequenceToSequenceGroupAsync(templateGroup.Id, washSequence.Id, 3);
        await sequenceGroupService.AddSequenceToSequenceGroupAsync(templateGroup.Id, analyzeSequence.Id, 4);

        Console.WriteLine("Created sequence group template successfully");

        // Step 3: Display the template's ordered sequences
        var templateOrderedSequences = await sequenceGroupService.GetOrderedSequencesAsync(templateGroup.Id);
        Console.WriteLine("\nTemplate sequences in order:");
        foreach (var (order, sequence) in templateOrderedSequences)
        {
            Console.WriteLine($"  * {order}: {sequence.Name}");

            // Get parameters for this sequence
            var sequenceWithParams = await dbContext.Sequences
                .Include(s => s.SequenceParameters)
                .ThenInclude(sp => sp.Parameter)
                .FirstOrDefaultAsync(s => s.Id == sequence.Id);

            if (sequenceWithParams?.SequenceParameters?.Any() == true)
            {
                Console.WriteLine("    Parameters:");
                foreach (var sp in sequenceWithParams.SequenceParameters.OrderBy(sp => sp.OrderNumber))
                {
                    Console.WriteLine($"      - {sp.Parameter.Name}: {sp.Parameter.DefaultValue} {sp.Parameter.Format}");
                }
            }
        }

        // Step 3: Create a "real" sequence group based on the template
        Console.WriteLine("\nStep 3: Creating a 'real' sequence group based on the template...");

        var realGroup = await sequenceGroupService.CreateSequenceGroupAsync(
            "Sample Batch #12345",
            "Processing configuration for sample batch #12345"
        );

        // Copy the sequences from the template to the real group
        foreach (var (order, sequence) in templateOrderedSequences)
        {
            await sequenceGroupService.AddSequenceToSequenceGroupAsync(realGroup.Id, sequence.Id, order);
        }

        Console.WriteLine("Created real sequence group successfully");

        // Step 4: Validate the real group
        Console.WriteLine("\nStep 4: Validating the real sequence group...");
        var isValid = await sequenceGroupService.ValidateSequenceGroupAsync(realGroup.Id);
        Console.WriteLine($"Sequence group is valid: {isValid}");

        // Show the current structure
        Console.WriteLine("\nFinal structure:");
        Console.WriteLine("Template Group:");
        var templateSequences = await sequenceGroupService.GetOrderedSequencesAsync(templateGroup.Id);
        foreach (var (order, sequence) in templateSequences)
        {
            Console.WriteLine($"  * {order}: {sequence.Name}");
        }

        Console.WriteLine("\nReal Group:");
        var realSequences = await sequenceGroupService.GetOrderedSequencesAsync(realGroup.Id);
        foreach (var (order, sequence) in realSequences)
        {
            Console.WriteLine($"  * {order}: {sequence.Name}");
        }

        // Step 5: Add the sequence groups to a sequence group collection
        Console.WriteLine("\nStep 5: Creating a 'real' sequence group based on the template...");
        var sequenceGroupCollection = new SequenceGroupCollection<Technology>
        {
            Name = "Technology sequence group 1",
            Description = "Add two sequence groups to a collection",
            //CategoryTypeName = "Technology",
           // CategoryName = "ImmunoCap",
            Category = Technology.ImmunoCap
        };
        await sequenceGroupCollectionService.CreateSequenceGroupCollectionAsync(sequenceGroupCollection.Category,
            sequenceGroupCollection.Name,
            sequenceGroupCollection.Description);

        // Add the sequence groups to the collection in order
        await sequenceGroupCollectionService.AddSequenceGroupToSequenceGroupCollectionAsync(sequenceGroupCollection.Id,
            templateGroup.Id, 
            1);
        await sequenceGroupCollectionService.AddSequenceGroupToSequenceGroupCollectionAsync(sequenceGroupCollection.Id,
            realGroup.Id,
            2);
        
        // Show the collection structure
        Console.WriteLine("\nSequenceGroupCollection structure:");
        var collectionGroups = await sequenceGroupCollectionService.GetOrderedSequenceGroupsAsync(sequenceGroupCollection.Id);
        foreach (var (order, sequenceGroup) in collectionGroups)
        {
            Console.WriteLine($"  * {order}: {sequenceGroup.Name}");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in full workflow demo");
        Console.WriteLine($"Error: {ex.Message}");
    }
}

async Task ClearAllDataAsync(ServiceProvider provider)
{
    Console.WriteLine("Clearing all data...");
    try
    {
        var dBCleanupService = provider.GetRequiredService<DatabaseCleanupService>();
        await dBCleanupService.ClearAllDataAsync();
        Console.WriteLine("All data cleared successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error clearing data: {ex.Message}");
    }
}
