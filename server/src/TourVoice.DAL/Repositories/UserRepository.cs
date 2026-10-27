using Microsoft.EntityFrameworkCore;
using TourVoice.DAL.Data;
using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

/// <summary>Quan ly tai khoan cho admin (SCR-A7).</summary>
public interface IUserRepository
{
    /// <summary><paramref name="search"/> da chuan hoa chu thuong; tim trong email va ho ten.</summary>
    Task<(List<User> Items, int Total)> GetPageAsync(string? search, UserRole? role, int page, int pageSize);

    // Khong AsNoTracking - admin khoa/mo roi SaveChangesAsync
    Task<User?> GetByIdAsync(int id);
    Task<int> CountActiveAdminsAsync();
    Task<bool> EmailExistsAsync(string email);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}

public class UserRepository : IUserRepository
{
    private readonly TourVoiceDbContext _db;
    public UserRepository(TourVoiceDbContext db) => _db = db;

    public async Task<(List<User> Items, int Total)> GetPageAsync(string? search, UserRole? role, int page, int pageSize)
    {
        var query = _db.Users.AsNoTracking();
        if (role is { } r) query = query.Where(u => u.Role == r);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.Email.Contains(search) || u.FullName.ToLower().Contains(search));

        var total = await query.CountAsync();
        var items = await query.OrderBy(u => u.Role == UserRole.Admin ? 0 : 1).ThenBy(u => u.Email)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public Task<User?> GetByIdAsync(int id) => _db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<int> CountActiveAdminsAsync() => _db.Users.CountAsync(u => u.Role == UserRole.Admin && u.IsActive);

    public Task<bool> EmailExistsAsync(string email) => _db.Users.AnyAsync(u => u.Email == email);

    public async Task AddAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
