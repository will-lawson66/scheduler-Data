using Instrument.Scheduling.Data.Interfaces;

namespace Instrument.Scheduling.Data.Domain.DataContext;
public class JsonSequenceDataContext(string filePath) : IDataContext
{
    private async Task<List<T>> LoadDataAsync<T>()
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Could not find sequence definitions at " + filePath);
        }

        var json = await File.ReadAllTextAsync(filePath);
        return System.Text.Json.JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
    }

    public IQueryable<T> Data<T>() where T : class
    {
        return LoadDataAsync<T>().Result.AsQueryable();
    }
}
