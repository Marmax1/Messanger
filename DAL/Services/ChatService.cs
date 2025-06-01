using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.DTO;
public class ChatService
{
	private readonly AppDbContext _db;

	public ChatService(AppDbContext db)
	{
		_db = db;
	}

	public async Task<User> GetUserById(int userId)
	{
		return await _db.Users.FindAsync(userId);
	}

	public async Task<Message> SendMessage(int userId, int roomId, string text)
	{
		if (!await _db.Users.AnyAsync(u => u.Id == userId))
			throw new Exception("Пользователь не найден");

		if (!await _db.ChatRooms.AnyAsync(r => r.Id == roomId))
			throw new Exception("Комната не найдена");

		if (string.IsNullOrWhiteSpace(text) || text.Length > 1000)
			throw new Exception("Сообщение должно быть от 1 до 1000 символов");

		var message = new Message
		{
			UserId = userId,
			ChatRoomId = roomId,
			Text = text.Trim(),
			SentAt = DateTime.UtcNow
		};

		_db.Messages.Add(message);
		await _db.SaveChangesAsync();

		message.User = await _db.Users.FindAsync(userId);
		return message;
	}

	public async Task<PaginatedMessages> GetRoomHistory(int roomId, int page = 1, int pageSize = 20)
	{
		if (page < 1) page = 1;
		if (pageSize < 1 || pageSize > 100) pageSize = 20;

		var query = _db.Messages
			.Where(m => m.ChatRoomId == roomId)
			.OrderByDescending(m => m.SentAt)
			.Include(m => m.User);

		var totalItems = await query.CountAsync();
		var messages = await query
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(m => new
			{
				Text = m.Text,
				Sender = m.User.Nickname,
				SentAt = m.SentAt
			})
			.ToListAsync();

		return new PaginatedMessages
		{
			Messages = messages,
			CurrentPage = page,
			TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
		};
	}

	public async Task<ChatRoom> CreateRoom(string name)
	{
		if (await _db.ChatRooms.AnyAsync(r => r.Name == name))
			throw new Exception("Комната с таким именем уже существует");

		var room = new ChatRoom
		{
			Name = name.Trim(),
			CreatedAt = DateTime.UtcNow
		};

		_db.ChatRooms.Add(room);
		await _db.SaveChangesAsync();

		return room;  // Важно вернуть созданную комнату
	}

	// Удалить комнату
	public async Task DeleteRoom(int roomId)
	{
		var room = await _db.ChatRooms.FindAsync(roomId);
		if (room == null) return;

		_db.ChatRooms.Remove(room);
		await _db.SaveChangesAsync();
	}

	public async Task<ChatRoom> GetRoomById(int roomId)
	{
		return await _db.ChatRooms.FirstOrDefaultAsync(r => r.Id == roomId);
	}

	// Список всех комнат
	public async Task<List<ChatRoom>> GetAllRooms()
		=> await _db.ChatRooms.ToListAsync();
}