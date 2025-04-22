namespace Instrument.Scheduling.Data.Interfaces;
public interface IDataContext
{
    IQueryable<T> Data<T>() where T : class;
}
