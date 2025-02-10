namespace Application.Core.Game
{
    public class ScriptResFactory
    {
        public readonly static string ScriptDirName = GetScriptDir();

        public static string GetScriptFullPath(string relativePath)
        {
            if (!Path.IsPathRooted(relativePath))
                return Path.Combine(ScriptDirName, relativePath);

            return Path.Combine(ScriptDirName, relativePath);
        }

        private static string GetScriptDir()
        {
            var dir = Environment.GetEnvironmentVariable("ms-script");
            if (dir == null || !Directory.Exists(dir))
                dir = "scripts";
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);
        }

        public static List<FileInfo> LoadAllScript()
        {
            return new DirectoryInfo(ScriptDirName).GetFiles("*", SearchOption.AllDirectories).ToList();
        }
    }
}
