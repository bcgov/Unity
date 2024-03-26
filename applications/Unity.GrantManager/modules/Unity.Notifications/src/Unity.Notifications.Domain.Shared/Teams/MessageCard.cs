using System.IO;
namespace Unity.Notifications.TeamsNotifications;

public static class MessageCard
{
    public static string GetMessageCard()
    {
        string currentPath = Directory.GetCurrentDirectory();
        string directoryPath = @$"{currentPath}\wwwroot\teams";
        string path = @$"{directoryPath}\MessageCard.json";
        string jsonResult = File.ReadAllText(path);
        return jsonResult;
    }
}
