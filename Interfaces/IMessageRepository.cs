using BachelorRoomFinding.Entities;

namespace BachelorRoomFinding.Interfaces
{
    public interface IMessageRepository : IRepository<Message>
    {
        Task<IEnumerable<Message>> GetConversationAsync(int currentUserId, int otherUserId);
    }
}
