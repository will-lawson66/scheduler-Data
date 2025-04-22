namespace Instrument.Scheduling.Data;
public interface IDataContext
{
    IQueryable<T> Data<T>() where T : class;
}
