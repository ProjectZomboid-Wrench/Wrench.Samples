using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wrench.Host.Security;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Wrench.Samples.Tests;

[TestClass]
public class SampleVettingTest
{
    [TestMethod]
    [DataRow("Wrench.Samples.ReallyCDDAy", "pzmod/WrenchReallyCDDAy/42/dotnet/Wrench.Samples.ReallyCDDAy.dll")]
    [DataRow("Wrench.Samples.GourmetDishes", "pzmod/WrenchGourmetDishes/42/dotnet/Wrench.Samples.GourmetDishes.dll")]
    [DataRow("Wrench.Samples.Tutorial", "Wrench.Samples.Tutorial.dll")]
    [DataRow("Wrench.Samples.RoomLights", "pzmod/WrenchRoomLights/42/dotnet/Wrench.Samples.RoomLights.dll")]
    [DataRow("Wrench.Samples.GatherAll", "pzmod/WrenchGatherAll/42/dotnet/Wrench.Samples.GatherAll.dll")]
    [DataRow("Wrench.Samples.NeedsAlerts", "pzmod/WrenchNeedsAlerts/42/dotnet/Wrench.Samples.NeedsAlerts.dll")]
    [DataRow("Wrench.Samples.StatusHud", "pzmod/WrenchStatusHud/42/dotnet/Wrench.Samples.StatusHud.dll")]
    [DataRow("Wrench.Samples.VisualBasic", "pzmod/WrenchWeighIt/42/dotnet/Wrench.Samples.VisualBasic.dll")]
    public void Sample_Mods_Pass(string project, string assembly)
    {
        var configuration = AppContext.BaseDirectory.Contains("/Release/") ? "Release" : "Debug";
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            project, "bin", configuration, "net11.0", assembly));

        var violations = ModVetting.Vet(path);

        Assert.IsEmpty(violations, string.Join(" | ", violations));
    }
}
