using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Server.Data;
using Server.WS;

public class WebSocketHandler
{
	private readonly IServiceProvider _serviceProvider;
	private readonly OnlineUsersService _onlineUsers;
	private class UserConnectionInfo
	{
		public int UserId { get; set; }
		public int? RoomId { get; set; }
	}

	private readonly ConcurrentDictionary<WebSocket, UserConnectionInfo> _connections;

	public WebSocketHandler(IServiceProvider serviceProvider, OnlineUsersService onlineUsers)
	{
		_serviceProvider = serviceProvider;
		_onlineUsers = onlineUsers;
		_connections = new ConcurrentDictionary<WebSocket, UserConnectionInfo>();
	}

	public async Task HandleConnection(WebSocket webSocket, CancellationToken cancellationToken)
	{
		var buffer = new byte[1024 * 4];

		try
		{
			while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
			{
				var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

				if (result.MessageType == WebSocketMessageType.Close)
					break;

				var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
				string response;

				try
				{
					// Определяем тип запроса по полю action
					using var jsonDoc = JsonDocument.Parse(json);
					if (!jsonDoc.RootElement.TryGetProperty("action", out var actionProp))
					{
						response = JsonSerializer.Serialize(new { type = "error", error = "Неверный формат сообщения" });
					}
					else
					{
						var action = actionProp.GetString();
						var connectionInfo = _connections.GetOrAdd(webSocket, _ => new UserConnectionInfo());
						response = await ProcessRequestByAction(action, json, _serviceProvider, _onlineUsers, connectionInfo);
					}
				}
				catch (JsonException)
				{
					response = JsonSerializer.Serialize(new { type = "error", error = "Неверный JSON" });
				}

				await webSocket.SendAsync(
					new ArraySegment<byte>(Encoding.UTF8.GetBytes(response)),
					WebSocketMessageType.Text,
					true,
					cancellationToken);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"WebSocket error: {ex.Message}");
		}
		finally
		{
			if (_connections.TryRemove(webSocket, out var connectionInfo))
			{
				if (connectionInfo.RoomId.HasValue)
				{
					_onlineUsers.RemoveUserFromRoom(connectionInfo.UserId, connectionInfo.RoomId.Value);
				}
			}
			await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", cancellationToken);
		}
	}

	private async Task<string> ProcessRequestByAction(string action, string json, IServiceProvider services,
		OnlineUsersService onlineUsers, UserConnectionInfo connectionInfo)
	{
		try
		{
			using var scope = services.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			var chatService = scope.ServiceProvider.GetRequiredService<ChatService>();
			var userService = scope.ServiceProvider.GetRequiredService<UserService>();

			return action switch
			{
				"auth" => await HandleAuth(JsonSerializer.Deserialize<AuthRequest>(json), userService, connectionInfo),
				"create_room" => await HandleCreateRoom(JsonSerializer.Deserialize<CreateRoomRequest>(json), chatService),
				"join" => await HandleJoinRoom(JsonSerializer.Deserialize<JoinRoomRequest>(json), chatService, userService, onlineUsers, connectionInfo),
				"send" => await HandleSendMessage(JsonSerializer.Deserialize<SendMessageRequest>(json), chatService, connectionInfo),
				"load_history" => await HandleLoadHistory(JsonSerializer.Deserialize<LoadHistoryRequest>(json), chatService),
				"get_rooms" => await HandleGetRooms(chatService),
				"get_users" => HandleGetUsers(JsonSerializer.Deserialize<GetUsersRequest>(json), onlineUsers),
				"leave" => HandleLeaveRoom(JsonSerializer.Deserialize<LeaveRoomRequest>(json), onlineUsers, connectionInfo),
				"delete_room" => await HandleDeleteRoom(JsonSerializer.Deserialize<DeleteRoomRequest>(json), chatService),
				_ => ErrorResponse("Неизвестная команда")
			};
		}
		catch (JsonException)
		{
			return ErrorResponse("Неверный формат данных для команды");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка: {ex.Message}");
			return ErrorResponse(ex.Message);
		}
	}

	// Все Handle методы перенесены сюда из WebSocketHub и сделаны приватными
	private static async Task<string> HandleAuth(AuthRequest request, UserService userService, UserConnectionInfo connectionInfo)
	{
		if (string.IsNullOrWhiteSpace(request.Nickname))
			return ErrorResponse("Никнейм не может быть пустым");

		var user = await userService.Join(request.Nickname);
		connectionInfo.UserId = user.Id;

		return SuccessResponse("auth_success", new
		{
			userId = user.Id,
			nickname = user.Nickname
		});
	}

	private static async Task<string> HandleCreateRoom(CreateRoomRequest request, ChatService chatService)
	{
		if (string.IsNullOrWhiteSpace(request.RoomName))
			return ErrorResponse("Название комнаты не может быть пустым");

		var createdRoom = await chatService.CreateRoom(request.RoomName);
		return SuccessResponse("room_created", new { createdRoom.Id, createdRoom.Name });
	}

	private static async Task<string> HandleJoinRoom(JoinRoomRequest request, ChatService chatService,
		UserService userService, OnlineUsersService onlineUsers, UserConnectionInfo connectionInfo)
	{
		if (connectionInfo.UserId <= 0)
			return ErrorResponse("Требуется аутентификация");

		if (request.RoomId <= 0)
			return ErrorResponse("Некорректный ID комнаты");

		var existingRoom = await chatService.GetRoomById(request.RoomId);
		if (existingRoom == null)
			return ErrorResponse("Комната не найдена");

		// Добавляем в онлайн-список
		onlineUsers.AddUserToRoom(connectionInfo.UserId, request.RoomId);
		connectionInfo.RoomId = request.RoomId;

		return SuccessResponse("join_success", new
		{
			roomId = request.RoomId
		});
	}

	private static async Task<string> HandleDeleteRoom(DeleteRoomRequest request, ChatService chatService)
	{
		if (request.RoomId <= 0)
			return ErrorResponse("Некорректный ID комнаты");

		await chatService.DeleteRoom(request.RoomId);
		return SuccessResponse("room_deleted", new { roomId = request.RoomId });
	}

	private static async Task<string> HandleSendMessage(SendMessageRequest request,
		ChatService chatService, UserConnectionInfo connectionInfo)
	{
		if (connectionInfo.UserId <= 0)
			return ErrorResponse("Требуется аутентификация");

		if (request.RoomId <= 0)
			return ErrorResponse("Некорректный ID комнаты");

		if (string.IsNullOrWhiteSpace(request.Text))
			return ErrorResponse("Текст сообщения пуст");

		var msg = await chatService.SendMessage(connectionInfo.UserId, request.RoomId, request.Text);
		return SuccessResponse("message", new
		{
			sender = msg.User.Nickname,
			text = msg.Text,
			timestamp = msg.SentAt
		});
	}

	private static async Task<string> HandleLoadHistory(LoadHistoryRequest request, ChatService chatService)
	{
		if (request.RoomId <= 0)
			return ErrorResponse("Некорректный ID комнаты");

		var page = request.Page > 0 ? request.Page : 1;
		var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 20;

		var history = await chatService.GetRoomHistory(request.RoomId, page, pageSize);
		return SuccessResponse("history", history);
	}

	private static async Task<string> HandleGetRooms(ChatService chatService)
	{
		var rooms = await chatService.GetAllRooms();
		return SuccessResponse("rooms_list", rooms);
	}

	private static string HandleGetUsers(GetUsersRequest request, OnlineUsersService onlineUsers)
	{
		if (request.RoomId <= 0)
			return ErrorResponse("Некорректный ID комнаты");

		var users = onlineUsers.GetRoomUsers(request.RoomId);
		return SuccessResponse("users_list", users);
	}

	private static string HandleLeaveRoom(LeaveRoomRequest request,
		OnlineUsersService onlineUsers, UserConnectionInfo connectionInfo)
	{
		if (connectionInfo.UserId <= 0)
			return ErrorResponse("Требуется аутентификация");

		if (request.RoomId <= 0)
			return ErrorResponse("Некорректный ID комнаты");

		onlineUsers.RemoveUserFromRoom(connectionInfo.UserId, request.RoomId);

		// Сбрасываем roomId в connectionInfo, если это текущая комната
		if (connectionInfo.RoomId == request.RoomId)
		{
			connectionInfo.RoomId = null;
		}

		return SuccessResponse("left_room", new { roomId = request.RoomId });
	}

	private static string SuccessResponse(string type, object data)
		=> JsonSerializer.Serialize(new { type, data });

	private static string ErrorResponse(string error)
		=> JsonSerializer.Serialize(new { type = "error", error });
}