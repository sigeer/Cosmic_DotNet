namespace Application.Core.Game.Commands
{
    public abstract class CommandBase
    {
        protected ILogger log;
        public CommandBase(int level, params string[] syntax)
        {
            var commandType = GetType();
            log = LogFactory.GetLogger($"Command/{commandType.Name}");

            Rank = level;
            Syntax = syntax;
        }
        public string[] Syntax { get; set; }
        public int Rank { get; set; }
        public string? Description { get; set; }

        public abstract void Execute(IClient client, string[] values);
        protected string joinStringFrom(string[] arr, int start)
        {
            return string.Join(' ', arr, start, arr.Length - start);
        }
    }
}
