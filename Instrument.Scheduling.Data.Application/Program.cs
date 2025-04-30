using Instrument.Scheduling.Data;
using Instrument.Scheduling.Data.Configuration;
using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Initialization;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Providers;
using Instrument.Scheduling.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Range = Instrument.Scheduling.Data.Entities.Range;

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
    builder.SetMinimumLevel(LogLevel.Information);
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
var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
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

        // Seed default data
        Console.WriteLine("Seeding default data...");
        bool seeded = await initializer.SeedDefaultDataAsync();
        if (seeded)
        {
            Console.WriteLine("Default data seeded successfully");
        }
        else
        {
            Console.WriteLine("Data already exists, no seeding needed");
        }
    }

    // Get storage status
    string status = await initializer.GetStatusMessageAsync();
    Console.WriteLine("Storage status:");
    Console.WriteLine(status);
    
    // Create some additional test data
    await SetupAdditionalTestDataAsync(unitOfWork, dbContext, logger);

    bool exit = false;
    while (!exit)
    {
        // Show menu
        Console.WriteLine("\n===== Scheduler Data Layer Demo =====");
        Console.WriteLine("1. Sequence Operations");
        Console.WriteLine("2. Parameter Operations");
        Console.WriteLine("3. Range Operations");
        Console.WriteLine("4. SequenceGroup Operations");
        Console.WriteLine("5. Demonstrate Full Workflow");
        Console.WriteLine("6. Clear All Data");
        Console.WriteLine("7. Exit");
        Console.Write("\nEnter your choice (1-7): ");

        var key = Console.ReadKey();
        Console.WriteLine();

        switch (key.KeyChar)
        {
            case '1':
                await DemoSequenceOperationsAsync(unitOfWork, logger);
                break;
            case '2':
                await DemoParameterOperationsAsync(unitOfWork, logger);
                break;
            case '3':
                await DemoRangeOperationsAsync(unitOfWork, logger);
                break;
            case '4':
                await DemoSequenceGroupOperationsAsync(unitOfWork, logger);
                break;
            case '5':
                await DemoFullWorkflowAsync(unitOfWork, dbContext, logger);
                break;
            case '6':
                await ClearAllDataAsync(serviceProvider, storageConfig);
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

/// <summary>
/// Demonstrates operations with Sequences
/// </summary>
async Task DemoSequenceOperationsAsync(IUnitOfWork unitOfWork, ILogger logger)
{
    Console.WriteLine("\n===== Sequence Operations =====");
    
    try
    {
        // Get all sequences
        var sequences = await unitOfWork.SequenceDefinitions.GetAllAsync();
        Console.WriteLine($"Found {sequences.Count()} sequences:");
        
        foreach (var seq in sequences)
        {
            Console.WriteLine($"  - ID: {seq.Id}, Name: {seq.Name}, Worst Case Time: {seq.WorstCaseTime}");
        }
        
        // Create a new sequence
        Console.WriteLine("\nCreating a new sequence...");
        var newSequence = new Sequence
        {
            Id = Guid.NewGuid().ToString().Substring(0, 8),
            Name = $"Demo Sequence {DateTime.Now:HHmmss}",
            Description = "Created during demo",
            WorstCaseTime = TimeSpan.FromSeconds(20),
            CanBeParallel = true
        };
        
        await unitOfWork.SequenceDefinitions.AddAsync(newSequence);
        await unitOfWork.SaveChangesAsync();
        
        Console.WriteLine($"Created sequence with ID: {newSequence.Id}");
        
        // Get sequence by ID
        var retrievedSequence = await unitOfWork.SequenceDefinitions.GetByIdAsync(newSequence.Id);
        if (retrievedSequence != null)
        {
            Console.WriteLine($"Retrieved sequence: {retrievedSequence.Name}");
            
            // Update sequence
            Console.WriteLine("\nUpdating sequence...");
            retrievedSequence = new Sequence
            {
                Id = retrievedSequence.Id,
                Name = retrievedSequence.Name + " (Updated)",
                Description = retrievedSequence.Description + " and updated",
                WorstCaseTime = retrievedSequence.WorstCaseTime.Add(TimeSpan.FromSeconds(10)),
                CanBeParallel = retrievedSequence.CanBeParallel
            };
            
            await unitOfWork.SequenceDefinitions.UpdateAsync(retrievedSequence);
            await unitOfWork.SaveChangesAsync();
            
            Console.WriteLine("Sequence updated successfully");
            
            // Delete sequence
            Console.WriteLine("\nDeleting sequence...");
            await unitOfWork.SequenceDefinitions.DeleteAsync(retrievedSequence.Id);
            await unitOfWork.SaveChangesAsync();
            
            Console.WriteLine("Sequence deleted successfully");
        }
        else
        {
            Console.WriteLine("Failed to retrieve the created sequence");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in sequence operations demo");
        Console.WriteLine($"Error: {ex.Message}");
    }
}

/// <summary>
/// Demonstrates operations with Parameters
/// </summary>
async Task DemoParameterOperationsAsync(IUnitOfWork unitOfWork, ILogger logger)
{
    Console.WriteLine("\n===== Parameter Operations =====");
    
    try
    {
        // Get all parameters
        var parameters = await unitOfWork.Parameters.GetAllAsync();
        Console.WriteLine($"Found {parameters.Count()} parameters:");
        
        foreach (var param in parameters)
        {
            Console.WriteLine($"  - ID: {param.Id}, Name: {param.Name}, Type: {param.Type}, Default: {param.DefaultValue}");
        }
        
        // Create a new parameter
        Console.WriteLine("\nCreating a new parameter...");
        var newParameter = new Parameter
        {
            Id = Guid.NewGuid().ToString().Substring(0, 8),
            Name = $"Demo Parameter {DateTime.Now:HHmmss}",
            Type = "integer",
            DefaultValue = "42",
            Min = "0",
            Max = "100",
            Format = "N0"
        };
        
        await unitOfWork.Parameters.AddAsync(newParameter);
        await unitOfWork.SaveChangesAsync();
        
        Console.WriteLine($"Created parameter with ID: {newParameter.Id}");
        
        // Get parameter by ID
        var retrievedParameter = await unitOfWork.Parameters.GetByIdAsync(newParameter.Id);
        if (retrievedParameter != null)
        {
            Console.WriteLine($"Retrieved parameter: {retrievedParameter.Name}");
            
            // Update parameter
            Console.WriteLine("\nUpdating parameter...");
            retrievedParameter = new Parameter
            {
                Id = retrievedParameter.Id,
                Name = retrievedParameter.Name + " (Updated)",
                Type = retrievedParameter.Type,
                DefaultValue = "50",
                Min = retrievedParameter.Min,
                Max = retrievedParameter.Max,
                Format = retrievedParameter.Format
            };
            
            await unitOfWork.Parameters.UpdateAsync(retrievedParameter);
            await unitOfWork.SaveChangesAsync();
            
            Console.WriteLine("Parameter updated successfully");
            
            // Delete parameter
            Console.WriteLine("\nDeleting parameter...");
            await unitOfWork.Parameters.DeleteAsync(retrievedParameter.Id);
            await unitOfWork.SaveChangesAsync();
            
            Console.WriteLine("Parameter deleted successfully");
        }
        else
        {
            Console.WriteLine("Failed to retrieve the created parameter");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in parameter operations demo");
        Console.WriteLine($"Error: {ex.Message}");
    }
}

/// <summary>
/// Demonstrates operations with Ranges
/// </summary>
async Task DemoRangeOperationsAsync(IUnitOfWork unitOfWork, ILogger logger)
{
    Console.WriteLine("\n===== Range Operations =====");
    
    try
    {
        // Get all ranges
        var ranges = await unitOfWork.Ranges.GetAllAsync();
        Console.WriteLine($"Found {ranges.Count()} ranges:");
        
        foreach (var range in ranges)
        {
            Console.WriteLine($"  - ID: {range.Id}, Name: {range.Name}");
            var rangeValues = await unitOfWork.RangeValues.GetValuesForRangeAsync(range.Id);
            Console.WriteLine($"    Values ({rangeValues.Count()}):");
            foreach (var value in rangeValues)
            {
                Console.WriteLine($"      * {value.Name}: {value.Value}");
            }
        }
        
        // Create a new range
        Console.WriteLine("\nCreating a new range...");
        var newRange = new Range
        {
            Id = Guid.NewGuid().ToString().Substring(0, 8),
            Name = $"Demo Range {DateTime.Now:HHmmss}",
            Description = "Created during demo"
        };
        
        await unitOfWork.Ranges.AddAsync(newRange);
        await unitOfWork.SaveChangesAsync();
        
        Console.WriteLine($"Created range with ID: {newRange.Id}");
        
        // Add range values
        Console.WriteLine("\nAdding range values...");
        var rangeValue1 = new RangeValue
        {
            Id = Guid.NewGuid().ToString().Substring(0, 8),
            RangeId = newRange.Id,
            Name = "First Value",
            Value = "Value1"
        };
        
        var rangeValue2 = new RangeValue
        {
            Id = Guid.NewGuid().ToString().Substring(0, 8),
            RangeId = newRange.Id,
            Name = "Second Value",
            Value = "Value2"
        };
        
        await unitOfWork.RangeValues.AddAsync(rangeValue1);
        await unitOfWork.RangeValues.AddAsync(rangeValue2);
        await unitOfWork.SaveChangesAsync();
        
        Console.WriteLine("Range values added successfully");
        
        // Get range with values
        var retrievedRange = await unitOfWork.Ranges.GetByIdAsync(newRange.Id);
        if (retrievedRange != null)
        {
            Console.WriteLine($"Retrieved range: {retrievedRange.Name}");
            
            var values = await unitOfWork.RangeValues.GetValuesForRangeAsync(retrievedRange.Id);
            Console.WriteLine($"  Values ({values.Count()}):");
            foreach (var value in values)
            {
                Console.WriteLine($"    * {value.Name}: {value.Value}");
            }
            
            // Delete range (will cascade delete range values)
            Console.WriteLine("\nDeleting range...");
            await unitOfWork.Ranges.DeleteAsync(retrievedRange.Id);
            await unitOfWork.SaveChangesAsync();
            
            Console.WriteLine("Range deleted successfully");
        }
        else
        {
            Console.WriteLine("Failed to retrieve the created range");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in range operations demo");
        Console.WriteLine($"Error: {ex.Message}");
    }
}

/// <summary>
/// Demonstrates operations with SequenceGroups
/// </summary>
async Task DemoSequenceGroupOperationsAsync(IUnitOfWork unitOfWork, ILogger logger)
{
    Console.WriteLine("\n===== SequenceGroup Operations =====");
    
    try
    {
        var sequenceGroupService = unitOfWork.SequenceGroupService;
        
        // Get all sequence groups
        var sequenceGroups = await unitOfWork.SequenceGroups.GetAllAsync();
        Console.WriteLine($"Found {sequenceGroups.Count()} sequence groups:");
        
        foreach (var group in sequenceGroups)
        {
            Console.WriteLine($"  - ID: {group.Id}, Name: {group.Name}");
            Console.WriteLine($"    Sequences ({(await sequenceGroupService.GetOrderedSequencesAsync(group.Id)).Count}):");
            foreach ((int order, Sequence sequence) in await sequenceGroupService.GetOrderedSequencesAsync(group.Id) as SortedList<int, Sequence>)
            {
                Console.WriteLine($"      * {order}: {sequence.Name}");
            }
        }
        
        // Create a new sequence group
        Console.WriteLine("\nCreating a new sequence group...");
        var groupId = Guid.NewGuid().ToString().Substring(0, 8);
        var newGroup = await sequenceGroupService.CreateSequenceGroupAsync(
            groupId,
            $"Demo Group {DateTime.Now:HHmmss}",
            "Created during demo"
        );
        
        Console.WriteLine($"Created sequence group with ID: {newGroup.Id}");
        
        // Add sequences to the group
        Console.WriteLine("\nAdding sequences to the group...");
        var sequences = (await unitOfWork.SequenceDefinitions.GetAllAsync()).Take(3).ToList();
        if (sequences.Count < 3)
        {
            Console.WriteLine("Not enough sequences found for demo. Make sure seed data exists.");
            return;
        }
        
        await sequenceGroupService.AddSequenceToGroupAsync(newGroup.Id, sequences[0].Id, 1);
        await sequenceGroupService.AddSequenceToGroupAsync(newGroup.Id, sequences[1].Id, 2);
        await sequenceGroupService.AddSequenceToGroupAsync(newGroup.Id, sequences[2].Id, 3);
        
        Console.WriteLine("Sequences added to group successfully");
        
        // Get ordered sequences
        var orderedSequences = await sequenceGroupService.GetOrderedSequencesAsync(newGroup.Id);
        Console.WriteLine("\nSequences in order:");
        foreach (var (order, sequence) in orderedSequences)
        {
            Console.WriteLine($"  * {order}: {sequence.Name}");
        }
        
        // Reorder a sequence
        Console.WriteLine("\nReordering a sequence...");
        await sequenceGroupService.ReorderSequenceInGroupAsync(newGroup.Id, sequences[0].Id, 3);
        
        var reorderedSequences = await sequenceGroupService.GetOrderedSequencesAsync(newGroup.Id);
        Console.WriteLine("Sequences after reordering:");
        foreach (var (order, sequence) in reorderedSequences)
        {
            Console.WriteLine($"  * {order}: {sequence.Name}");
        }
        
        // Remove a sequence
        Console.WriteLine("\nRemoving a sequence from the group...");
        await sequenceGroupService.RemoveSequenceFromGroupAsync(newGroup.Id, sequences[1].Id);
        
        var remainingSequences = await sequenceGroupService.GetOrderedSequencesAsync(newGroup.Id);
        Console.WriteLine("Sequences after removal:");
        foreach (var (order, sequence) in remainingSequences)
        {
            Console.WriteLine($"  * {order}: {sequence.Name}");
        }
        
        // Validate the group
        Console.WriteLine("\nValidating the sequence group...");
        var isValid = await sequenceGroupService.ValidateSequenceGroupAsync(newGroup.Id);
        Console.WriteLine($"Sequence group is valid: {isValid}");
        
        // Delete the group
        Console.WriteLine("\nDeleting the sequence group...");
        await sequenceGroupService.DeleteSequenceGroupAsync(newGroup.Id);
        
        Console.WriteLine("Sequence group deleted successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in sequence group operations demo");
        Console.WriteLine($"Error: {ex.Message}");
    }
}

/// <summary>
/// Demonstrates a full workflow combining all data layer features
/// </summary>
async Task DemoFullWorkflowAsync(IUnitOfWork unitOfWork, SchedulerDbContext dbContext, ILogger logger)
{
    Console.WriteLine("\n===== Full Workflow Demo =====");
    
    try
    {
        var sequenceGroupService = unitOfWork.SequenceGroupService;
        
        // Step 1: Create sequences with parameters
        Console.WriteLine("\nStep 1: Creating sequences with parameters...");
        
        // Create a parameter for reagent volume
        var reagentVolumeParam = new Parameter
        {
            Id = "demo_reagent_volume",
            Name = "Reagent Volume",
            Type = "integer",
            DefaultValue = "50",
            Min = "10",
            Max = "100",
            Format = "N0"
        };
        
        await unitOfWork.Parameters.AddAsync(reagentVolumeParam);
        
        // Create a parameter for temperature
        var temperatureParam = new Parameter
        {
            Id = "demo_temperature",
            Name = "Temperature",
            Type = "float",
            DefaultValue = "37.0",
            Min = "20.0",
            Max = "50.0",
            Format = "N1"
        };
        
        await unitOfWork.Parameters.AddAsync(temperatureParam);
        
        // Create a parameter for time
        var timeParam = new Parameter
        {
            Id = "demo_time",
            Name = "Incubation Time",
            Type = "integer",
            DefaultValue = "60",
            Min = "30",
            Max = "300",
            Format = "N0"
        };
        
        await unitOfWork.Parameters.AddAsync(timeParam);
        
        await unitOfWork.SaveChangesAsync();
        
        // Create sequences
        var prepSequence = new Sequence
        {
            Id = "demo_prep",
            Name = "Preparation Sequence",
            Description = "Prepares the samples for processing",
            WorstCaseTime = TimeSpan.FromSeconds(30),
            CanBeParallel = false
        };
        
        var processSequence = new Sequence
        {
            Id = "demo_process",
            Name = "Processing Sequence",
            Description = "Processes the samples",
            WorstCaseTime = TimeSpan.FromSeconds(120),
            CanBeParallel = true
        };
        
        var washSequence = new Sequence
        {
            Id = "demo_wash",
            Name = "Wash Sequence",
            Description = "Washes the samples",
            WorstCaseTime = TimeSpan.FromSeconds(45),
            CanBeParallel = true
        };
        
        var analyzeSequence = new Sequence
        {
            Id = "demo_analyze",
            Name = "Analyze Sequence",
            Description = "Analyzes the samples",
            WorstCaseTime = TimeSpan.FromSeconds(60),
            CanBeParallel = false
        };
        
        await unitOfWork.SequenceDefinitions.AddAsync(prepSequence);
        await unitOfWork.SequenceDefinitions.AddAsync(processSequence);
        await unitOfWork.SequenceDefinitions.AddAsync(washSequence);
        await unitOfWork.SequenceDefinitions.AddAsync(analyzeSequence);
        
        await unitOfWork.SaveChangesAsync();
        
        // Link parameters to sequences
        await dbContext.SequenceParameters.AddAsync(new SequenceParameter
        {
            SequenceId = prepSequence.Id,
            ParameterId = reagentVolumeParam.Id,
            OrderNumber = 1
        });
        
        await dbContext.SequenceParameters.AddAsync(new SequenceParameter
        {
            SequenceId = processSequence.Id,
            ParameterId = temperatureParam.Id,
            OrderNumber = 1
        });
        
        await dbContext.SequenceParameters.AddAsync(new SequenceParameter
        {
            SequenceId = processSequence.Id,
            ParameterId = timeParam.Id,
            OrderNumber = 2
        });
        
        await dbContext.SaveChangesAsync();
        
        Console.WriteLine("Created sequences and parameters successfully");
        
        // Step 2: Create a sequence group template
        Console.WriteLine("\nStep 2: Creating a sequence group template...");
        
        var templateGroup = await sequenceGroupService.CreateSequenceGroupAsync(
            "demo_template",
            "Demo Sample Processing Template",
            "Template for processing samples"
        );
        
        // Add sequences to the template in order
        await sequenceGroupService.AddSequenceToGroupAsync(templateGroup.Id, prepSequence.Id, 1);
        await sequenceGroupService.AddSequenceToGroupAsync(templateGroup.Id, processSequence.Id, 2);
        await sequenceGroupService.AddSequenceToGroupAsync(templateGroup.Id, washSequence.Id, 3);
        await sequenceGroupService.AddSequenceToGroupAsync(templateGroup.Id, analyzeSequence.Id, 4);
        
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
        
        // Step 4: Create a "real" sequence group based on the template
        Console.WriteLine("\nStep 4: Creating a 'real' sequence group based on the template...");
        
        var realGroup = await sequenceGroupService.CreateSequenceGroupAsync(
            "demo_real_group",
            "Sample Batch #12345",
            "Processing configuration for sample batch #12345"
        );
        
        // Copy the sequences from the template to the real group
        foreach (var (order, sequence) in templateOrderedSequences)
        {
            await sequenceGroupService.AddSequenceToGroupAsync(realGroup.Id, sequence.Id, order);
        }
        
        Console.WriteLine("Created real sequence group successfully");
        
        // Step 5: Validate the real group
        Console.WriteLine("\nStep 5: Validating the real sequence group...");
        var isValid = await sequenceGroupService.ValidateSequenceGroupAsync(realGroup.Id);
        Console.WriteLine($"Sequence group is valid: {isValid}");
        
        // Step 6: Show the final structure
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
        
        // Step 7: Clean up (optional - comment out to keep the demo data)
        Console.WriteLine("\nStep 7: Cleaning up demo data...");
        
        // Delete the groups first
        await sequenceGroupService.DeleteSequenceGroupAsync(realGroup.Id);
        await sequenceGroupService.DeleteSequenceGroupAsync(templateGroup.Id);
        
        // Delete the sequences
        await unitOfWork.SequenceDefinitions.DeleteAsync(prepSequence.Id);
        await unitOfWork.SequenceDefinitions.DeleteAsync(processSequence.Id);
        await unitOfWork.SequenceDefinitions.DeleteAsync(washSequence.Id);
        await unitOfWork.SequenceDefinitions.DeleteAsync(analyzeSequence.Id);
        
        // Delete the parameters
        await unitOfWork.Parameters.DeleteAsync(reagentVolumeParam.Id);
        await unitOfWork.Parameters.DeleteAsync(temperatureParam.Id);
        await unitOfWork.Parameters.DeleteAsync(timeParam.Id);
        
        await unitOfWork.SaveChangesAsync();
        
        Console.WriteLine("Demo data cleaned up successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in full workflow demo");
        Console.WriteLine($"Error: {ex.Message}");
    }
}

/// <summary>
/// Sets up additional test data for demos
/// </summary>
async Task SetupAdditionalTestDataAsync(IUnitOfWork unitOfWork, SchedulerDbContext dbContext, ILogger logger)
{
    try
    {
        // Check if we already have enough test data
        var sequences = await unitOfWork.SequenceDefinitions.GetAllAsync();
        if (sequences.Count() >= 4)
        {
            return; // We already have enough data
        }
        
        logger.LogInformation("Setting up additional test data");
        
        // Create additional sequences if needed
        if (sequences.Count() < 4)
        {
            var sequencesToAdd = 4 - sequences.Count();
            for (int i = 0; i < sequencesToAdd; i++)
            {
                var sequence = new Sequence
                {
                    Id = $"test_seq_{i + 1}",
                    Name = $"Test Sequence {i + 1}",
                    Description = $"Test sequence for demo purposes #{i + 1}",
                    WorstCaseTime = TimeSpan.FromSeconds(30 + (i * 10)),
                    CanBeParallel = i % 2 == 0
                };
                
                await unitOfWork.SequenceDefinitions.AddAsync(sequence);
            }
            
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("Added {Count} additional test sequences", sequencesToAdd);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error setting up additional test data");
    }
}

/// <summary>
/// Clears all data from the storage
/// </summary>
async Task ClearAllDataAsync(ServiceProvider serviceProvider, StorageConfiguration storageConfig)
{
    Console.WriteLine("Clearing all data...");
    
    try
    {
        switch (storageConfig.Provider)
        {
            case StorageProviderType.Json:
                var jsonCleanupService = serviceProvider.GetRequiredService<JsonDataCleanupService>();
                jsonCleanupService.ClearAllData();
                break;
                
            default:
                var dBCleanupService = serviceProvider.GetRequiredService<DatabaseCleanupService>();
                await dBCleanupService.ClearAllDataAsync();
                break;
        }
        
        Console.WriteLine("All data cleared successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error clearing data: {ex.Message}");
    }
}
