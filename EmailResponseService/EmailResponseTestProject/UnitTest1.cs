namespace EmailResponseTestProject;

public class Tests
{
    private string BaseFolderPath = @"";
    private string Filename = @"";
    
    [SetUp]
    public void Setup()
    {
        BaseFolderPath = Environment.CurrentDirectory;
        Filename = Path.Combine(BaseFolderPath, "郵件附件.eml");
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }
}