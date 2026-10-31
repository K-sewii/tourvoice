using Moq;
using TourVoice.BLL.Services;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;
using Xunit;

namespace TourVoice.BLL.Tests;

public class AccountStatusServiceTests
{
    private readonly Mock<ICustomerRepository> _repository = new();
    private readonly AccountStatusService _service;

    public AccountStatusServiceTests() => _service = new AccountStatusService(_repository.Object);

    [Fact]
    public async Task ActiveUser_IsAllowed()
    {
        _repository.Setup(r => r.GetUserByIdAsync(7)).ReturnsAsync(new User { Id = 7, IsActive = true });

        Assert.True(await _service.IsActiveAsync(7));
    }

    [Fact]
    public async Task LockedUser_IsRejected()
    {
        _repository.Setup(r => r.GetUserByIdAsync(7)).ReturnsAsync(new User { Id = 7, IsActive = false });

        Assert.False(await _service.IsActiveAsync(7));
    }

    [Fact]
    public async Task MissingUser_IsRejected()
    {
        _repository.Setup(r => r.GetUserByIdAsync(7)).ReturnsAsync((User?)null);

        Assert.False(await _service.IsActiveAsync(7));
    }
}
