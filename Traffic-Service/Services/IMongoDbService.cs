namespace Traffic_Service.Services
{
    public interface IMongoDbService
    {
        Task InsertTrafficRecordAsync(object record);
        Task<List<T>> GetTrafficHistoryAsync<T>(string sensorId, DateTime from, DateTime to);
    }
}
