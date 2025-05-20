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
		if (await _db.Users.AnyAsync(u => u.Nickname == nickname))
			throw new Exception("Никнейм уже занят");

		if (nickname.Length < 3 || nickname.Length > 20)
			throw new Exception("Никнейм должен быть от 3 до 20 символов");

		var user = new User { Nickname = nickname.Trim(), JoinedAt = DateTime.UtcNow };
		_db.Users.Add(user);
		await _db.SaveChangesAsync();
		return user;
	}
}