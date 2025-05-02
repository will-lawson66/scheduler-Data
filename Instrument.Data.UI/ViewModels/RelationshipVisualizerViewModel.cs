using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Instrument.Data.Services;
using Instrument.Data.UI.Services;
using Instrument.Data.UI.Views;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Media;

namespace Instrument.Data.UI.ViewModels
{
    public partial class RelationshipVisualizerViewModel : ViewModelBase
    {
        private readonly SequenceService _sequenceService;
        private readonly ParameterService _parameterService;
        private readonly RangeService _rangeService;
        private readonly ResourceService _resourceService;
        private readonly SequenceGroupService _sequenceGroupService;

        [ObservableProperty]
        private ObservableCollection<EntityTypeItem> _entityTypes = new();

        [ObservableProperty]
        private EntityTypeItem? _selectedEntityType;

        [ObservableProperty]
        private ObservableCollection<EntityNode> _entityNodes = new();

        [ObservableProperty]
        private ObservableCollection<RelationshipLine> _relationshipLines = new();

        [ObservableProperty]
        private ObservableCollection<RelationshipLabel> _relationshipLabels = new();

        [ObservableProperty]
        private bool _isVisualizationReady = false;

        [ObservableProperty]
        private bool _isLoading = false;

        // Solid color brushes for node backgrounds
        private readonly SolidColorBrush[] _entityBrushes = new[]
        {
            new SolidColorBrush(Color.FromRgb(179, 229, 252)), // Light Blue
            new SolidColorBrush(Color.FromRgb(200, 230, 201)), // Light Green
            new SolidColorBrush(Color.FromRgb(255, 224, 178)), // Light Orange
            new SolidColorBrush(Color.FromRgb(225, 190, 231)), // Light Purple
            new SolidColorBrush(Color.FromRgb(245, 245, 245))  // Light Grey
        };

        public RelationshipVisualizerViewModel(
            NavigationService navigationService,
            DialogService dialogService,
            ILogger<RelationshipVisualizerViewModel> logger,
            SequenceService sequenceService,
            ParameterService parameterService,
            RangeService rangeService,
            ResourceService resourceService,
            SequenceGroupService sequenceGroupService)
            : base(navigationService, dialogService, logger)
        {
            Title = "Relationship Visualizer";

            _sequenceService = sequenceService;
            _parameterService = parameterService;
            _rangeService = rangeService;
            _resourceService = resourceService;
            _sequenceGroupService = sequenceGroupService;

            // Initialize entity types
            LoadEntityTypes();
        }

        private void LoadEntityTypes()
        {
            EntityTypes.Clear();
            
            // Add all entity types that we want to visualize
            EntityTypes.Add(new EntityTypeItem("Sequence", typeof(Instrument.Data.Entities.Sequence)));
            EntityTypes.Add(new EntityTypeItem("Parameter", typeof(Instrument.Data.Entities.Parameter)));
            EntityTypes.Add(new EntityTypeItem("Range", typeof(Instrument.Data.Entities.Range)));
            EntityTypes.Add(new EntityTypeItem("Resource", typeof(Instrument.Data.Entities.Resource)));
            EntityTypes.Add(new EntityTypeItem("Sequence Group", typeof(Instrument.Data.Entities.SequenceGroup)));
        }

        [RelayCommand]
        private async Task VisualizeAsync()
        {
            if (SelectedEntityType == null)
            {
                DialogService.ShowWarning("Visualization", "Please select an entity type to visualize.");
                return;
            }

            try
            {
                IsLoading = true;
                IsVisualizationReady = false;

                // Clear current visualization
                EntityNodes.Clear();
                RelationshipLines.Clear();
                RelationshipLabels.Clear();

                // Simulate a slight delay for effect
                await Task.Delay(500);

                // Generate the visualization based on the selected entity type
                await GenerateVisualizationAsync(SelectedEntityType);

                IsVisualizationReady = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error visualizing relationships");
                DialogService.ShowError("Visualization Error", 
                    $"An error occurred while generating the visualization: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GenerateVisualizationAsync(EntityTypeItem entityType)
        {
            // This implementation is a simulation - in a real application, 
            // you would retrieve actual entity relationships from the database
            
            // Generate nodes based on entity type
            var random = new Random();
            int nodeCount = random.Next(3, 8); // Random number of entities for demo
            
            // Create main entity nodes
            for (int i = 0; i < nodeCount; i++)
            {
                EntityNodes.Add(new EntityNode
                {
                    Name = $"{entityType.Name} {i+1}",
                    Type = entityType.Name,
                    X = 200 + random.Next(-100, 100),
                    Y = 150 + random.Next(-50, 150),
                    Background = _entityBrushes[0],
                    PropertiesCount = random.Next(3, 12)
                });
            }
            
            // Create related entity nodes
            var relatedEntityTypes = GetRelatedEntityTypes(entityType.Type);
            
            foreach (var relatedType in relatedEntityTypes)
            {
                int typeIndex = EntityTypes.IndexOf(
                    EntityTypes.FirstOrDefault(e => e.Type == relatedType) ?? 
                    new EntityTypeItem(relatedType.Name, relatedType));
                
                int relatedNodeCount = random.Next(1, 4);
                for (int i = 0; i < relatedNodeCount; i++)
                {
                    EntityNodes.Add(new EntityNode
                    {
                        Name = $"{relatedType.Name} {i+1}",
                        Type = relatedType.Name,
                        X = 500 + random.Next(-100, 100) + (typeIndex * 50),
                        Y = 150 + random.Next(-50, 200) + (typeIndex * 30),
                        Background = _entityBrushes[Math.Min(typeIndex + 1, _entityBrushes.Length - 1)],
                        PropertiesCount = random.Next(3, 8)
                    });
                }
                
                // Add relationship lines between main entity and related entity
                var mainNodes = EntityNodes.Where(n => n.Type == entityType.Name).ToList();
                var relatedNodes = EntityNodes.Where(n => n.Type == relatedType.Name).ToList();
                
                foreach (var mainNode in mainNodes)
                {
                    foreach (var relatedNode in relatedNodes.Take(random.Next(1, relatedNodes.Count + 1)))
                    {
                        var relationship = GetRandomRelationship(entityType.Type, relatedType);
                        
                        // Create a relationship line
                        RelationshipLines.Add(new RelationshipLine
                        {
                            X1 = mainNode.X + 90,  // Center of card
                            Y1 = mainNode.Y + 50,  // Center of card
                            X2 = relatedNode.X + 90,  // Center of card
                            Y2 = relatedNode.Y + 50,  // Center of card
                            Relationship = relationship,
                            DashPattern = relationship.Contains("many") ? new DoubleCollection(new double[] { 4, 2 }) : null
                        });
                        
                        // Add a label in the middle of the line
                        RelationshipLabels.Add(new RelationshipLabel
                        {
                            Text = relationship,
                            X = (mainNode.X + relatedNode.X) / 2 + 60,
                            Y = (mainNode.Y + relatedNode.Y) / 2 + 45
                        });
                    }
                }
            }
        }
        
        private List<Type> GetRelatedEntityTypes(Type entityType)
        {
            // This is a simplified implementation
            var relatedTypes = new List<Type>();
            
            if (entityType == typeof(Instrument.Data.Entities.Sequence))
            {
                relatedTypes.Add(typeof(Instrument.Data.Entities.Parameter));
                relatedTypes.Add(typeof(Instrument.Data.Entities.SequenceGroup));
            }
            else if (entityType == typeof(Instrument.Data.Entities.Parameter))
            {
                relatedTypes.Add(typeof(Instrument.Data.Entities.Sequence));
                relatedTypes.Add(typeof(Instrument.Data.Entities.Range));
                relatedTypes.Add(typeof(Instrument.Data.Entities.Resource));
            }
            else if (entityType == typeof(Instrument.Data.Entities.Range))
            {
                relatedTypes.Add(typeof(Instrument.Data.Entities.Parameter));
            }
            else if (entityType == typeof(Instrument.Data.Entities.Resource))
            {
                relatedTypes.Add(typeof(Instrument.Data.Entities.Parameter));
            }
            else if (entityType == typeof(Instrument.Data.Entities.SequenceGroup))
            {
                relatedTypes.Add(typeof(Instrument.Data.Entities.Sequence));
            }
            
            return relatedTypes;
        }
        
        private string GetRandomRelationship(Type sourceType, Type targetType)
        {
            // Simulate relationship types based on entity types
            if (sourceType == typeof(Instrument.Data.Entities.Sequence) && 
                targetType == typeof(Instrument.Data.Entities.Parameter))
            {
                return "has many";
            }
            else if (sourceType == typeof(Instrument.Data.Entities.Sequence) && 
                     targetType == typeof(Instrument.Data.Entities.SequenceGroup))
            {
                return "belongs to many";
            }
            else if (sourceType == typeof(Instrument.Data.Entities.Parameter) && 
                     targetType == typeof(Instrument.Data.Entities.Sequence))
            {
                return "belongs to many";
            }
            else if (sourceType == typeof(Instrument.Data.Entities.Parameter) && 
                     targetType == typeof(Instrument.Data.Entities.Range))
            {
                return "has one";
            }
            else if (sourceType == typeof(Instrument.Data.Entities.Parameter) && 
                     targetType == typeof(Instrument.Data.Entities.Resource))
            {
                return "references";
            }
            else if (sourceType == typeof(Instrument.Data.Entities.Range) && 
                     targetType == typeof(Instrument.Data.Entities.Parameter))
            {
                return "used by many";
            }
            else if (sourceType == typeof(Instrument.Data.Entities.Resource) && 
                     targetType == typeof(Instrument.Data.Entities.Parameter))
            {
                return "referenced by many";
            }
            else if (sourceType == typeof(Instrument.Data.Entities.SequenceGroup) && 
                     targetType == typeof(Instrument.Data.Entities.Sequence))
            {
                return "contains many";
            }
            else
            {
                return "related to";
            }
        }
    }

    public class EntityTypeItem
    {
        public string Name { get; }
        public Type Type { get; }

        public EntityTypeItem(string name, Type type)
        {
            Name = name;
            Type = type;
        }
    }

    public class EntityNode
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public SolidColorBrush Background { get; set; } = new SolidColorBrush(Colors.LightBlue);
        public int PropertiesCount { get; set; }
    }

    public class RelationshipLine
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public string Relationship { get; set; } = string.Empty;
        public DoubleCollection? DashPattern { get; set; }
    }

    public class RelationshipLabel
    {
        public string Text { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
    }
}
