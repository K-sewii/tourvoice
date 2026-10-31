using TourVoice.BLL.Auth;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Services;

public class AccountStatusService : IAccountStatusService
{
    private readonly ICustomerRepository _customers;

    public AccountStatusService(ICustomerRepository customers) => _customers = customers;

    public async Task<bool> IsActiveAsync(int userId, CancellationToken ct = default)
    {
        // Tai khoan da bi xoa cung coi nhu khong con hieu luc.
        var user = await _customers.GetUserByIdAsync(userId);
        return user is { IsActive: true };
    }
}
