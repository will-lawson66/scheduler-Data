using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Instrument.Scheduling.Data.Interfaces;

namespace Instrument.Scheduling.Data.Providers
{
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
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _data = JsonSerializer.Deserialize<List<T>>(json, _options) ?? new List<T>();
            }
        }

        public Task<IEnumerable<T>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<T>>(_data.ToList());
        }

        public Task<T?> GetByIdAsync(string id)
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null)
                throw new InvalidOperationException($"Type {typeof(T).Name} must have an Id property");

            var entity = _data.FirstOrDefault(x => 
                idProperty.GetValue(x)?.ToString() == id);
            return Task.FromResult(entity);
        }

        public Task AddAsync(T entity)
        {
            _data.Add(entity);
            _isDirty = true;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(T entity)
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
            
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null)
                throw new InvalidOperationException($"Type {typeof(T).Name} must have an Id property");

            _data.RemoveAll(x => idProperty.GetValue(x)?.ToString() == id);
            _isDirty = true;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            if (_isDirty)
            {
                var json = JsonSerializer.Serialize(_data, _options);
                File.WriteAllText(_filePath, json);
                _isDirty = false;
            }
            return Task.CompletedTask;
        }
    }
}
