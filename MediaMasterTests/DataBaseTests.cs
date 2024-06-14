using System.Diagnostics;
using MediaMaster.DataBase;

namespace MediaMasterTests;

[TestClass]
public class DataBaseTests
{
    [TestMethod]
    public void TestMethod1()
    {
        using (MediaDbContext dataBase = new())
        {
            dataBase.InitializeAsync().GetAwaiter().GetResult();
        }
        Debug.WriteLine("DataBaseTests.TestMethod1");
        MediaService.AddMediaAsync("C:\\").GetAwaiter().GetResult();
    }
}
