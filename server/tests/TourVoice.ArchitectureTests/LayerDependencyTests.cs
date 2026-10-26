using System.Reflection;
using NetArchTest.Rules;

namespace TourVoice.ArchitectureTests;

public class LayerDependencyTests
{
    private const string ApiNs = "TourVoice.API";
    private const string BllNs = "TourVoice.BLL";
    private const string DalNs = "TourVoice.DAL";

    private static readonly Assembly Api = Assembly.Load(ApiNs);
    private static readonly Assembly Bll = Assembly.Load(BllNs);
    private static readonly Assembly Dal = Assembly.Load(DalNs);

    [Fact]
    public void Api_KhongDuoc_PhuThuocTrucTiep_Dal()
    {
        var result = Types.InAssembly(Api)
            .ShouldNot().HaveDependencyOn(DalNs)
            .GetResult();

        Assert.True(result.IsSuccessful, Loi(result));
    }

    [Fact]
    public void Bll_KhongDuoc_PhuThuoc_Api()
    {
        var result = Types.InAssembly(Bll)
            .ShouldNot().HaveDependencyOn(ApiNs)
            .GetResult();

        Assert.True(result.IsSuccessful, Loi(result));
    }

    [Fact]
    public void Dal_KhongDuoc_PhuThuoc_Bll_Hay_Api()
    {
        var result = Types.InAssembly(Dal)
            .ShouldNot().HaveDependencyOnAny(BllNs, ApiNs)
            .GetResult();

        Assert.True(result.IsSuccessful, Loi(result));
    }

    private static string Loi(TestResult r) =>
        "Vi phạm kiến trúc 3 lớp tại: " +
        string.Join(", ", r.FailingTypeNames ?? Enumerable.Empty<string>());
}