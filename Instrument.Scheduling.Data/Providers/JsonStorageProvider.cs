using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using System.Text.Json;

namespace Instrument.Scheduling.Data.Providers;
public class JsonStorageProvider<T> : IStorageProvider<T> where T : class
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;
    private List<T> _data;
    private bool _isDirty;

    public JsonStorageProvider(string filePath)
    {
        _filePath = filePath;
        _options = new JsonSerializerOptions { WriteIndented = true };
        _data = new List<T>();
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _data = JsonSerializer.Deserialize<List<T>>(json, _options) ?? new List<T>();
            }
        }
        catch (Exception ex)
        {
            throw new StorageProviderException("LoadData", ex);
        }
    }

    public Task<IEnumerable<T>> GetAllAsync()
    {
        try
        {
            return Task.FromResult<IEnumerable<T>>(_data.ToList());
        }
        catch (Exception ex)
        {
            throw new StorageProviderException("GetAll", ex);
        }
    }

    public Task<T?> GetByIdAsync(string id)
    {
        try
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null)
                throw new InvalidOperationException($"Type {typeof(T).Name} must have an Id property");

            var entity = _data.FirstOrDefault(x => 
                idProperty.GetValue(x)?.ToString() == id);
            return Task.FromResult(entity);
        }
        catch (Exception ex)
        {
            throw new StorageProviderException("GetById", ex);
        }
    }

    public Task AddAsync(T entity)
    {
        try
        {
            _data.Add(entity);
            _isDirty = true;
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new StorageProviderException("Add", ex);
        }
    }

    public Task UpdateAsync(T entity)
    {
        try
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null)
                throw new InvalidOperationException($"Type {typeof(T).Name} must have an Id property");

            var id = idProperty.GetValue(entity)?.ToString();
            var index = _data.FindIndex(x => 
                idProperty.GetValue(x)?.ToString() == id);

            if (index >= 0)
            {
                _data[index] = entity;
                _isDirty = true;
            }
            else
            {
                throw new EntityNotFoundException(typeof(T).Name, id ?? "unknown");
            }
            
            return Task.CompletedTask;
        }
        catch (EntityNotFoundException)
        {
            // Re-throw entity not found exceptions
            throw;
        }
        catch (Exception ex)
        {
            throw new StorageProviderException("Update", ex);
        }
    }

    public Task DeleteAsync(string id)
    {
        try
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null)
                throw new InvalidOperationException($"Type {typeof(T).Name} must have an Id property");

            var originalCount = _data.Count;
            _data.RemoveAll(x => idProperty.GetValue(x)?.ToString() == id);
            
            if (_data.Count == originalCount)
            {
                throw new EntityNotFoundException(typeof(T).Name, id);
            }
            
            _isDirty = true;
            return Task.CompletedTask;
        }
        catch (EntityNotFoundException)
        {
            // Re-throw entity not found exceptions
            throw;
        }
        catch (Exception ex)
        {
            throw new StorageProviderException("Delete", ex);
        }
    }

    public Task SaveChangesAsync()
    {
        try
        {
            if (_isDirty)
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonSerializer.Serialize(_data, _options);
                File.WriteAllText(_filePath, json);
                _isDirty = false;
            }
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new StorageProviderException("SaveChanges", ex);
        }
    }
}
