using System.Text.Json.Serialization;

namespace Server.WS
{
	// Базовый класс для всех входящих сообщений
	public abstract class WsRequest
	{
		[JsonPropertyName("action")]
		public abstract string Action { get; }
	}

	// Конкретные классы запросов:

	public class AuthRequest : WsRequest
	{
		public override string Action => "auth";

		[JsonPropertyName("nickname")]
		public string Nickname { get; set; }
	}

	public class CreateRoomRequest : WsRequest
	{
		public override string Action => "create_room";

		[JsonPropertyName("roomName")]
		public string RoomName { get; set; }
	}

	public class JoinRoomRequest : WsRequest
	{
		public override string Action => "join";

		[JsonPropertyName("roomId")]
		public int RoomId { get; set; }
	}

	public class SendMessageRequest : WsRequest
	{
		public override string Action => "send";

		[JsonPropertyName("roomId")]
		public int RoomId { get; set; }

		[JsonPropertyName("text")]
		public string Text { get; set; }
	}

	public class LoadHistoryRequest : WsRequest
	{
		public override string Action => "load_history";

		[JsonPropertyName("roomId")]
		public int RoomId { get; set; }

		[JsonPropertyName("page")]
		public int Page { get; set; } = 1;

		[JsonPropertyName("pageSize")]
		public int PageSize { get; set; } = 20;
	}

	public class GetRoomsRequest : WsRequest
	{
		public override string Action => "get_rooms";
	}

	public class GetUsersRequest : WsRequest
	{
		public override string Action => "get_users";

		[JsonPropertyName("roomId")]
		public int RoomId { get; set; }
	}

	public class LeaveRoomRequest : WsRequest
	{
		public override string Action => "leave";

		[JsonPropertyName("roomId")]
		public int RoomId { get; set; }
	}

	public class DeleteRoomRequest : WsRequest
	{
		public override string Action => "delete_room";

		[JsonPropertyName("roomId")]
		public int RoomId { get; set; }
	}
}
