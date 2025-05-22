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
using Server.WS;
using System.Collections.Specialized;

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
			builder.Services.AddSingleton<OnlineUsersService>();
			builder.Services.AddSingleton<WebSocketHandler>();

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

			// Обработчик WebSocket
			app.Map("/ws", async context =>
			{
				if (!context.WebSockets.IsWebSocketRequest)
				{
					context.Response.StatusCode = 400;
					return;
				}

				using var ws = await context.WebSockets.AcceptWebSocketAsync();
				var handler = context.RequestServices.GetRequiredService<WebSocketHandler>();
				await handler.HandleConnection(ws, context.RequestAborted);
			});

			// HTTP endpoint для получения списка комнат
			app.MapGet("/api/rooms", async (ChatService chatService) =>
				Results.Json(await chatService.GetAllRooms()));

			app.Run("http://localhost:5000");
		}
	}
}