using Server.Models;
using System.Collections.Concurrent;

public class OnlineUsersService
{
	private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, byte>> _roomUsers = new();
	private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, byte>> _userRooms = new();

	public void AddUserToRoom(int userId, int roomId)
	{
		// Добавляем пользователя в комнату
		_roomUsers.GetOrAdd(roomId, _ => new ConcurrentDictionary<int, byte>())
				 .TryAdd(userId, 0);

		// Добавляем комнату к пользователю
		_userRooms.GetOrAdd(userId, _ => new ConcurrentDictionary<int, byte>())
				 .TryAdd(roomId, 0);
	}



	public void RemoveUserFromRoom(int userId, int roomId)
	{
		// Удаляем пользователя из комнаты
		if (_roomUsers.TryGetValue(roomId, out var users))
		{
			users.TryRemove(userId, out _);
			if (users.IsEmpty)
			{
				_roomUsers.TryRemove(roomId, out _);
			}
		}

		// Удаляем комнату у пользователя
		if (_userRooms.TryGetValue(userId, out var rooms))
		{
			rooms.TryRemove(roomId, out _);
			if (rooms.IsEmpty)
			{
				_userRooms.TryRemove(userId, out _);
			}
		}
	}

	public void RemoveUserFromAllRooms(int userId)
	{
		if (_userRooms.TryRemove(userId, out var rooms))
		{
			foreach (var roomId in rooms.Keys)
			{
				if (_roomUsers.TryGetValue(roomId, out var users))
				{
					users.TryRemove(userId, out _);
					if (users.IsEmpty)
					{
						_roomUsers.TryRemove(roomId, out _);
					}
				}
			}
		}
	}

	public IReadOnlyCollection<int> GetRoomUsers(int roomId)
		=> _roomUsers.TryGetValue(roomId, out var users)
			? users.Keys.ToList()
			: (IReadOnlyCollection<int>)Array.Empty<int>();

	public IReadOnlyCollection<int> GetUserRooms(int userId)
		=> _userRooms.TryGetValue(userId, out var rooms)
			? rooms.Keys.ToList()
			: (IReadOnlyCollection<int>)Array.Empty<int>();

}