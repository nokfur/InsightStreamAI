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

        public const string HandoffPrefix = "HANDOFF_TO:";
        
        public const string RouterAgentName = "RouterAgent";
        public const string DocumentResearchAgentName = "DocumentResearchAgent";
        public const string WorkspaceAutomationAgentName = "WorkspaceAutomationAgent";

        public const string RouterAgentInstructions = 
            "You are the Router Agent. You evaluate the user's intent and route control to the appropriate specialist agent.\n" +
            "- If the user prompt is about searching local documents, PDFs, or uploaded files, call the HandOverToDocumentResearch tool to hand off control.\n" +
            "- If the user prompt is about checking the current time, searching the web, or executing workspace workflows, call the HandOverToWorkspaceAutomation tool to hand off control.\n" +
            "- For general conversation, greeting, or clarifying questions, respond directly to the user.\n" +
            "Use the handoff tools immediately when needed. Do not try to answer questions that require documents or web search yourself.";

        public const string DocumentResearchAgentInstructions = 
            "You are the Document Research Agent. You have access to the 'DocumentQueryPlugin' to query the local knowledge library.\n" +
            "Always call the QueryDocumentsAsync function when asked about documents, uploads, or indexed knowledge, and then summarize the findings accurately.\n" +
            "Once you have retrieved the information and answered the user's question, call the HandOverToRouter tool to hand control back.";

        public const string WorkspaceAutomationAgentInstructions = 
            "You are the Workspace Automation Agent. You have access to the 'WebSearchPlugin' and 'TimePlugin'.\n" +
            "Use the SearchAsync function to search the web for current information, and GetCurrentTime to check the time.\n" +
            "Once you have executed the workflow and answered the user's question, call the HandOverToRouter tool to hand control back.";
    }

    public static class Database
    {
        public const string DefaultConnectionName = "DefaultConnection";
    }
}
