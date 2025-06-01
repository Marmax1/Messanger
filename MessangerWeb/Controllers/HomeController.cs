using MessangerWeb.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace MessangerWeb.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;

		public HomeController(ILogger<HomeController> logger)
		{
			_logger = logger;
		}

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Privacy()
		{
			return View();
		}

		public IActionResult Room(int roomId)  // Изменяем тип параметра на int
		{
			ViewBag.RoomId = roomId;
			ViewBag.RoomName = GetRoomName(roomId);  // Добавляем название комнаты
			return View();
		}

		private string GetRoomName(int roomId)
		{
			// Здесь должна быть логика получения названия комнаты из БД
			// Временно возвращаем заглушку
			return $"Комната {roomId}";
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
