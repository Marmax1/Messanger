using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

public class UserService
{
	private readonly AppDbContext _db;

	public UserService(AppDbContext db)
	{
		_db = db;
	}

	public async Task<User> Join(string nickname)
	{
		nickname = nickname?.Trim();
		if (string.IsNullOrWhiteSpace(nickname) || nickname.Length < 3 || nickname.Length > 20)
			throw new Exception("Никнейм должен быть от 3 до 20 символов");

		if (nickname.Any(c => !char.IsLetterOrDigit(c)))
			throw new Exception("Никнейм может содержать только буквы и цифры");

		var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Nickname == nickname);
		if (existingUser != null)
			return existingUser;

		var user = new User { Nickname = nickname, JoinedAt = DateTime.UtcNow };
		_db.Users.Add(user);
		await _db.SaveChangesAsync();
		return user;
	}

	public async Task<User> GetUser(string nickname)
	{
		nickname = nickname?.Trim();

		if (string.IsNullOrWhiteSpace(nickname) || nickname.Length < 3 || nickname.Length > 20)
			throw new Exception("Никнейм должен быть от 3 до 20 символов");

		if (nickname.Any(c => !char.IsLetterOrDigit(c)))
			throw new Exception("Никнейм может содержать только буквы и цифры");

		var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Nickname == nickname);
		if (existingUser != null)
			return existingUser;
		else
			throw new Exception("Такого пользователя не существует");
	}
}