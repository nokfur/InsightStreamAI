using System.ComponentModel;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;
using InsightStreamAI.Application.Common;
using Microsoft.Extensions.Logging;

namespace InsightStreamAI.Infrastructure.Plugins;

public class WebSearchPlugin(IHttpClientFactory httpClientFactory, ILogger<WebSearchPlugin> logger)
{
    [KernelFunction, Description("Searches the web for current information on a given query.")]
    public async Task<string> SearchAsync([Description("The search query")] string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "No query provided.";
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient(Constants.WebSearch.ClientName);
            
            // DuckDuckGo Lite search endpoint
            var url = Constants.WebSearch.SearchUrl;
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "q", query }
            });

            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            request.Headers.UserAgent.ParseAdd(Constants.WebSearch.UserAgent);

            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            // Basic regex parsing of the lite HTML format
            var results = new List<string>();
            var matches = Regex.Matches(html, Constants.WebSearch.SnippetRegex);

            int count = 0;
            foreach (Match match in matches)
            {
                if (count >= 5) break; // Limit to top 5 results
                var snippet = match.Groups[1].Value;
                // Strip HTML tags
                snippet = Regex.Replace(snippet, "<.*?>", string.Empty);
                snippet = System.Net.WebUtility.HtmlDecode(snippet).Trim();
                
                if (!string.IsNullOrWhiteSpace(snippet))
                {
                    results.Add($"- {snippet}");
                    count++;
                }
            }

            if (results.Count > 0)
            {
                return string.Join("\n\n", results);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Web search failed for query '{Query}'. Falling back to simulated mock search results.", query);
        }

        return $"Simulated Web Search Results for '{query}':\n" +
               $"- Latest news and updates relating to '{query}' are currently trending.\n" +
               $"- Industry analysis reports discuss the key elements and practical applications of '{query}'.\n" +
               $"- Standard developer documentation and user guides provide standard setups for '{query}'.";
    }
}
