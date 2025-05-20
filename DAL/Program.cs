using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Server.Data;
using Server.Models;
using Microsoft.Extensions.Configuration;

namespace Server
{
	public class Program
	{
		public static void Main(string[] args)
		{
			string projectDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

			var configuration = new ConfigurationBuilder()
				.SetBasePath(projectDirectory)
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.Build();

			var builder = WebApplication.CreateBuilder(args);

			// Подключение БД
			builder.Services.AddDbContext<AppDbContext>(options =>
				options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

			// Регистрация сервисов
			builder.Services.AddSingleton<ChatService>();
			builder.Services.AddSingleton<UserService>();

			// Добавляем политику CORS
			builder.Services.AddCors(options =>
			{
				options.AddPolicy("AllowAll", policy =>
				{
					policy.AllowAnyOrigin()
						  .AllowAnyMethod()
						  .AllowAnyHeader();
				});
			});

			var app = builder.Build();

			// Используем CORS
			app.UseCors("AllowAll");

			// Middleware для WebSocket
			app.UseWebSockets();

			// Обработчик WS-подключений
			app.Map("/ws", async context =>
			{
				if (!context.WebSockets.IsWebSocketRequest)
				{
					context.Response.StatusCode = 400;
					return;
				}

				using var ws = await context.WebSockets.AcceptWebSocketAsync();
				var buffer = new byte[1024 * 4];

				try
				{
					while (ws.State == WebSocketState.Open)
					{
						var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

						if (result.MessageType == WebSocketMessageType.Close)
							break;

						string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
						string response;

						try
						{
							var message = JsonSerializer.Deserialize<WebSocketMessage>(json);
							response = await WebSocketHub.ProcessMessage(message, context.RequestServices);
						}
						catch (JsonException)
						{
							response = JsonSerializer.Serialize(new { type = "error", error = "Неверный формат JSON" });
						}

						await ws.SendAsync(Encoding.UTF8.GetBytes(response), WebSocketMessageType.Text, true, CancellationToken.None);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Ошибка: {ex.Message}");
					await ws.CloseAsync(WebSocketCloseStatus.InternalServerError, "Server error", CancellationToken.None);
				}
			});

			app.Run("http://localhost:5000");
		}
	}
}