namespace DbProxy.Client.Providers;

public static class CommandArgsValueProvider
{
    public static string GetValue(string[] args, string option, string? fallback = null)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == option && i + 1 < args.Length)
                return args[i + 1];
        }

        return fallback ?? throw new ArgumentException($"You must specify a value for this option. {option}");
    }
}