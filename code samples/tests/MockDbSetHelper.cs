using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using System.Linq.Expressions;

namespace Instrument.Data.UT
{
    /// <summary>
    /// Helper class for mocking Entity Framework DbSet and DbContext functionality
    /// </summary>
    public static class MockDbSetHelper
    {
        /// <summary>
        /// Creates a mock DbSet from a list of entities that can be used in repository tests
        /// </summary>
        public static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
        {
            var queryableData = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();
            
            // Setup IQueryable interface
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryableData.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryableData.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryableData.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() 
                => queryableData.GetEnumerator());
            
            // Setup Async Queryable extension methods
            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
                
            mockSet.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(queryableData.Provider));
            
            // Setup Find method
            mockSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns<object[]>(ids => 
            {
                var id = ids[0];
                return data.FirstOrDefault(d => GetEntityId(d) == id);
            });
            
            mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>())).ReturnsAsync<object[], DbSet<T>, T>(ids => 
            {
                var id = ids[0];
                return data.FirstOrDefault(d => GetEntityId(d) == id);
            });
                
            // Setup collection methods
            mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(entity => 
            {
                data.Add(entity);
            }).Returns((EntityEntry<T>)null);
            
            mockSet.Setup(m => m.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Callback<T, CancellationToken>((entity, _) => data.Add(entity))
                .ReturnsAsync((EntityEntry<T>)null);
                
            mockSet.Setup(m => m.Remove(It.IsAny<T>())).Callback<T>(entity => 
            {
                var item = data.FirstOrDefault(d => GetEntityId(d).ToString() == GetEntityId(entity).ToString());
                if (item != null)
                {
                    data.Remove(item);
                }
            }).Returns((EntityEntry<T>)null);

            return mockSet;
        }
        
        /// <summary>
        /// Creates a mock DbContext with configured entity sets for testing repositories
        /// </summary>
        public static Mock<Instrument.Data.DataContext.SchedulerDbContext> CreateMockDbContext()
        {
            var mockContext = new Mock<Instrument.Data.DataContext.SchedulerDbContext>(new DbContextOptions<Instrument.Data.DataContext.SchedulerDbContext>());
            mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            
            return mockContext;
        }
        
        /// <summary>
        /// Gets the ID value from an entity using reflection
        /// </summary>
        private static object GetEntityId<T>(T entity) where T : class
        {
            return entity.GetType().GetProperty("Id").GetValue(entity);
        }
    }
    
    // Helper classes for async querying
    internal class TestAsyncQueryProvider<TEntity> : Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        {
            return new TestAsyncEnumerable<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var resultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethod(
                    name: nameof(IQueryProvider.Execute),
                    genericParameterCount: 1,
                    types: new[] { typeof(Expression) })
                .MakeGenericMethod(resultType)
                .Invoke(this, new[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                .MakeGenericMethod(resultType)
                .Invoke(null, new[] { executionResult });
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(base.Provider);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public T Current => _inner.Current;

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return new ValueTask();
        }
    }
}
