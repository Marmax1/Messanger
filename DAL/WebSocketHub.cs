using Server.Data;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

public static class WebSocketHub
{
	public static async Task<string> ProcessMessage(WebSocketMessage message, IServiceProvider services)
	{
		if (message == null)
			return ErrorResponse("Пустое сообщение");

		using var scope = services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var chatService = scope.ServiceProvider.GetRequiredService<ChatService>();
		var userService = scope.ServiceProvider.GetRequiredService<UserService>();

		try
		{
			switch (message.Action?.ToLower())
			{
				case "join":
					if (string.IsNullOrWhiteSpace(message.Nickname))
						return ErrorResponse("Никнейм не может быть пустым");

					var user = await userService.Join(message.Nickname);
					var history = await chatService.GetRoomHistory(message.RoomId); // Добавляем загрузку истории

					return SuccessResponse("join", new
					{
						userId = user.Id,
						history  // Отправляем историю вместе с подтверждением входа
					});

				case "send":
					if (message.UserId <= 0 || message.RoomId <= 0)
						return ErrorResponse("Некорректный ID пользователя или комнаты");

					if (string.IsNullOrWhiteSpace(message.Text))
						return ErrorResponse("Текст сообщения пуст");

					var msg = await chatService.SendMessage(message.UserId, message.RoomId, message.Text);
					return SuccessResponse("message", new { text = $"{msg.User.Nickname}: {msg.Text}" });

				case "create_room":
					if (string.IsNullOrWhiteSpace(message.RoomName))
						return ErrorResponse("Название комнаты не может быть пустым");

					var room = await chatService.CreateRoom(message.RoomName);
					return SuccessResponse("room_created", new { roomId = room.Id });

				case "load_history":
					if (message.RoomId <= 0)
						return ErrorResponse("Некорректный ID комнаты");

					var mes_history = await chatService.GetRoomHistory(
						message.RoomId,
						message.Page,
						message.PageSize
					);
					return SuccessResponse("history", mes_history);

				default:
					return ErrorResponse("Неизвестная команда");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка: {ex.Message}");
			return ErrorResponse("Внутренняя ошибка сервера");
		}
	}

	private static string SuccessResponse(string type, object data)
		=> JsonSerializer.Serialize(new { type, data });

	private static string ErrorResponse(string error)
		=> JsonSerializer.Serialize(new { type = "error", error });
}

public class WebSocketMessage
{
	public string Action { get; set; }  // "join", "send", "create_room", "load_history"
	public string Nickname { get; set; }
	public int UserId { get; set; }
	public int RoomId { get; set; }
	public string Text { get; set; }
	public string RoomName { get; set; }
	public int Page { get; set; }      // Для пагинации (начинается с 1)
	public int PageSize { get; set; }  // Дефолтное значение: 20
}