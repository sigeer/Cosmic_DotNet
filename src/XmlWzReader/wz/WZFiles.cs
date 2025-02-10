namespace XmlWzReader.wz;

public class WZFiles
{
    public static readonly WZFiles QUEST = new("Quest");
    public static readonly WZFiles ETC = new("Etc");
    public static readonly WZFiles ITEM = new("Item");
    public static readonly WZFiles CHARACTER = new("Character");
    public static readonly WZFiles STRING = new("string");
    public static readonly WZFiles LIST = new("List");
    public static readonly WZFiles MOB = new("Mob");
    public static readonly WZFiles MAP = new("Map");
    public static readonly WZFiles NPC = new("Npc");
    public static readonly WZFiles REACTOR = new("Reactor");
    public static readonly WZFiles SKILL = new("Skill");
    public static readonly WZFiles SOUND = new("Sound");
    public static readonly WZFiles UI = new("UI");

    public readonly static string DIRECTORY = getWzDirectory();

    private string fileName;

    WZFiles(string name)
    {
        this.fileName = name + ".wz";
    }

    public string getFile()
    {
        return Path.Combine(DIRECTORY, fileName);
    }

    public string getFilePath()
    {
        return getFile().ToString();
    }

    private static string getWzDirectory()
    {
        var propertyPath = Environment.GetEnvironmentVariable("ms-wz");
        if (propertyPath != null && Directory.Exists(propertyPath))
        {
            return propertyPath;
        }

        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wz");
    }
}
