namespace Instrument.Data.UT;

[CollectionDefinition("Database Tests", DisableParallelization = true)]
public class DatabaseTestCollection : ICollectionFixture<TestFixture>
{
    // This class doesn't need any code
    // It's just a marker for the test collection
}

public class TestFixture : IDisposable
{
    public TestFixture()
    {
        // Any shared setup could go here
    }

    public void Dispose()
    {
        // Any shared cleanup could go here
    }
}
