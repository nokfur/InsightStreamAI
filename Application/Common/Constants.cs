namespace InsightStreamAI.Application.Common;

public static class Constants
{
    public static class WebSearch
    {
        public const string ClientName = "WebSearch";
        public const string SearchUrl = "https://lite.duckduckgo.com/lite/";
        public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        public const string SnippetRegex = @"<td class=""result-snippet"">([\s\S]*?)<\/td>";
    }

    public static class Prompts
    {
        public const string SystemInstruction = 
            "You are InsightStream AI, a premium, intelligent knowledge assistant. " +
            "You help the user query, summarize, and analyze their uploaded documents. " +
            "Use the available search and time tools to find precise answers. " +
            "If you search and find no information in the documents, explain that you couldn't find it in the uploaded documents, but offer general knowledge if appropriate, or ask for clarification.";
        
        public const string DefaultTitlePrefix = "New Conversation";
    }

    public static class Database
    {
        public const string DefaultConnectionName = "DefaultConnection";
    }
}
