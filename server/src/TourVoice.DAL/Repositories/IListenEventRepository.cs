using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public interface IListenEventRepository
{
    Task AddAsync(ListenEvent listenEvent);
}