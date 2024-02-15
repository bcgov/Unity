using System.IO;
namespace Unity.GrantManager.GrantApplications;

public static class PublishEventMessageCard
{
    public static string GetMessageCard()
    {
        string currentPath = Directory.GetCurrentDirectory();
        string directoryPath = @$"{currentPath}\wwwroot\teams";
        string path = @$"{directoryPath}\MessageCard.json";
        string jsonResult = File.ReadAllText(path);
        return jsonResult;
    }

    public static string GetPublishEventMessageCard()
    {
        string currentPath = Directory.GetCurrentDirectory();
        string directoryPath = @$"{currentPath}\wwwroot\teams";
        string path = @$"{directoryPath}\PublishEventMessageCard.json";
        string jsonResult = File.ReadAllText(path);
        return jsonResult;
    }
}
