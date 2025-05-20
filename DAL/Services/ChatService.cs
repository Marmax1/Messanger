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

	public async Task<Message> SendMessage(int userId, int roomId, string text)
	{
		// Проверяем существование пользователя и комнаты
		var user = await _db.Users.FindAsync(userId);
		if (user == null)
			throw new Exception("Пользователь не найден");

		var room = await _db.ChatRooms.FindAsync(roomId);
		if (room == null)
			throw new Exception("Комната не найдена");

		if (text.Length > 1000)
			throw new Exception("Сообщение слишком длинное");

		var message = new Message
		{
			UserId = userId,
			ChatRoomId = roomId,
			Text = text.Trim(),
			SentAt = DateTime.UtcNow
		};

		_db.Messages.Add(message);
		await _db.SaveChangesAsync();

		// Явно подгружаем пользователя для ответа
		message.User = user;
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
}