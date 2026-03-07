namespace Unity.GrantManager.AI
{
    internal static class PromptHeader
    {
        public static string Build(string role, string task)
        {
            return $@"ROLE
{role}

TASK
{task}";
        }
    }
}
