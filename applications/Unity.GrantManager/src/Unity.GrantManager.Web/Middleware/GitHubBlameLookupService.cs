using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Integrations;

namespace Unity.GrantManager.Web.Middleware;

public class GitHubBlameLookupService : IBlameLookupService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubBlameLookupService> _logger;
    private readonly IEndpointManagementAppService? _endpointService;

    private readonly string _owner = string.Empty;
    private readonly string _repo = string.Empty;

    private string _branch;

    public GitHubBlameLookupService(
        HttpClient httpClient,
        ILogger<GitHubBlameLookupService> logger,
        IEndpointManagementAppService? endpointService = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _endpointService = endpointService;

        // Try to get repo from endpoint service if available
        string? repoUrl = null;
        if (_endpointService != null)
        {
            try
            {
                repoUrl = _endpointService.GetGitHubRepoUrlAsync().GetAwaiter().GetResult();
            }
            catch { }
        }
        if (!string.IsNullOrWhiteSpace(repoUrl))
        {
            var parts = repoUrl.TrimEnd('/').Split('/');
            if (parts.Length >= 2)
            {
                _owner = parts[^2];
                _repo = parts[^1];
            }
        }
        else
        {
            _owner = Environment.GetEnvironmentVariable("GITHUB_OWNER") ?? "";
            _repo = Environment.GetEnvironmentVariable("GITHUB_REPO") ?? "";
        }

        var env =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Production";

        _branch = env switch
        {
            "Development" => "dev",
            "Test" => "test",
            "Staging" => "main",
            "Production" => "main",
            _ => "main"
        };

        // Optional override
        var branchOverride =
            Environment.GetEnvironmentVariable("GITHUB_BRANCH");

        if (!string.IsNullOrWhiteSpace(branchOverride))
        {
            _branch = branchOverride;
        }

        // Set Authorization header if UNITY_GITHUB_PAT is present
        string pat = Environment.GetEnvironmentVariable("UNITY_GITHUB_PAT") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(pat))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", pat);
        }

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Unity-GrantManager");
    }

    /// <summary>
    /// Creates a compact blame reference.
    ///
    /// Example:
    /// main/src/MyFile.cs#L123
    /// </summary>
    public string BuildBlameReference(string path, int line)
    {
        return $"{_branch}/{path}#L{line}";
    }

    /// <summary>
    /// Converts compact reference into a full GitHub URL.
    ///
    /// Example:
    /// https://github.com/bcgov/Unity/blame/main/src/MyFile.cs#L123
    /// </summary>
    public string BuildBlameUrl(string reference)
    {
        return
            $"https://github.com/{_owner}/{_repo}/blame/{reference}";
    }

    /// <summary>
    /// Lookup blame information from a compact reference.
    ///
    /// Supported:
    /// main/src/MyFile.cs#L123
    /// src/MyFile.cs#L123
    /// </summary>
    public async Task<GitHubBlameInfo?> GetBlameFromReferenceAsync(
        string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        string branch = _branch;
        string pathWithFragment = reference;

        // Try extracting branch
        int firstSlash = reference.IndexOf('/');

        if (firstSlash > 0)
        {
            string possibleBranch = reference[..firstSlash];

            if (possibleBranch is "main" or "dev" or "test")
            {
                branch = possibleBranch;
                pathWithFragment = reference[(firstSlash + 1)..];
            }
        }

        // Parse:
        // src/MyFile.cs#L123
        string[] parts =
            pathWithFragment.Split("#L");

        string path = parts[0];

        int line = 1;

        if (parts.Length > 1 &&
            int.TryParse(parts[1], out int parsedLine))
        {
            line = parsedLine;
        }

        return await GetBlameAsync(
            _owner,
            _repo,
            branch,
            path,
            line);
    }

    /// <summary>
    /// Lookup blame using configured repo + branch.
    /// </summary>
    public async Task<GitHubBlameInfo?> GetBlameAsync(
        string repoPath,
        int line)
    {
        return await GetBlameAsync(
            _owner,
            _repo,
            _branch,
            repoPath,
            line);
    }

    /// <summary>
    /// Full blame lookup.
    /// </summary>
    public async Task<GitHubBlameInfo?> GetBlameAsync(
        string owner,
        string repo,
        string branch,
        string repoPath,
        int line)
    {
        if (string.IsNullOrWhiteSpace(owner) ||
            string.IsNullOrWhiteSpace(repo))
        {
            _logger.LogDebug(
                "GitHub blame lookup skipped — owner or repo missing");

            return null;
        }

        var query = BuildQuery(
            owner,
            repo,
            branch,
            repoPath);

        var payload = JsonSerializer.Serialize(new
        {
            query
        });

        string? githubGraphQlUrl = null;
        if (_endpointService != null)
        {
            try
            {
                githubGraphQlUrl = _endpointService.GetGitHubGraphQlUrlAsync().GetAwaiter().GetResult();
            }
            catch { }
        }
  
        if (githubGraphQlUrl == null)        {
            return null;
        }

        using var response = await _httpClient.PostAsync(
            githubGraphQlUrl,
            new StringContent(
                payload,
                Encoding.UTF8,
                "application/json"));

        response.EnsureSuccessStatusCode();

        string json =
            await response.Content.ReadAsStringAsync();

        using JsonDocument doc =
            JsonDocument.Parse(json);

        var root = doc.RootElement;

        if (root.TryGetProperty("errors", out var errors))
        {
            _logger.LogWarning(
                "GitHub GraphQL errors: {Errors}",
                errors.ToString());

            return null;
        }

        var ranges =
            root
                .GetProperty("data")
                .GetProperty("repository")
                .GetProperty("object")
                .GetProperty("blame")
                .GetProperty("ranges");

        foreach (var range in ranges.EnumerateArray())
        {
            int start =
                range.GetProperty("startingLine").GetInt32();

            int end =
                range.GetProperty("endingLine").GetInt32();

            if (line < start || line > end)
            {
                continue;
            }

            var commit =
                range.GetProperty("commit");

            string sha =
                commit.GetProperty("oid").GetString()
                ?? "";

            string message =
                commit.GetProperty("messageHeadline").GetString()
                ?? "";

            var author =
                commit.GetProperty("author");

            string authorName =
                author.GetProperty("name").GetString()
                ?? "";

            string email =
                author.GetProperty("email").GetString()
                ?? "";

            string? prUrl = null;
            int? prNumber = null;
            string? prTitle = null;

            var prs =
                commit
                    .GetProperty("associatedPullRequests")
                    .GetProperty("nodes");

            if (prs.GetArrayLength() > 0)
            {
                var pr = prs[0];
                prUrl = pr.GetProperty("url").GetString();
                prTitle = pr.GetProperty("title").GetString();
                if (pr.TryGetProperty("number", out var numberProp))
                {
                    prNumber = numberProp.GetInt32();
                }
            }

            return new GitHubBlameInfo
            {
                CommitSha = sha,
                Author = authorName,
                Email = email,
                Message = message,
                PullRequestUrl = prUrl,
                PullRequestNumber = prNumber,
                PullRequestTitle = prTitle
            };
        }

        return null;
    }

    private string BuildQuery(
        string owner,
        string repo,
        string branch,
        string path)
    {
        return $@"
query {{
  repository(owner: ""{owner}"", name: ""{repo}"") {{
    object(expression: ""{branch}"") {{
      ... on Commit {{
        blame(path: ""{path}"") {{
          ranges {{
            startingLine
            endingLine
            commit {{
              oid
              messageHeadline
              author {{
                name
                email
              }}
              associatedPullRequests(first: 1) {{
                nodes {{
                  number
                  url
                  title
                }}
              }}
            }}
          }}
        }}
      }}
    }}
  }}
}}";
    }
}