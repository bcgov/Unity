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

    private readonly string _owner;
    private readonly string _repo;
    private readonly string _branch;

    public GitHubBlameLookupService(
        HttpClient httpClient,
        ILogger<GitHubBlameLookupService> logger,
        IEndpointManagementAppService? endpointService = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _endpointService = endpointService;

        string? repoUrl = null;

        if (_endpointService != null)
        {
            try
            {
                repoUrl = _endpointService.GetGitHubRepoUrlAsync()
                    .GetAwaiter().GetResult();
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(repoUrl))
        {
            var parts = repoUrl.TrimEnd('/').Split('/');
            _owner = parts.Length >= 2 ? parts[^2] : "";
            _repo = parts.Length >= 1 ? parts[^1] : "";
        }
        else
        {
            _owner = Environment.GetEnvironmentVariable("GITHUB_OWNER") ?? "";
            _repo = Environment.GetEnvironmentVariable("GITHUB_REPO") ?? "";
        }

        var env =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Production";

        _branch = Environment.GetEnvironmentVariable("GITHUB_BRANCH")
            ?? env switch
            {
                "Development" => "dev",
                "Test" => "test",
                _ => "main"
            };

        string pat = Environment.GetEnvironmentVariable("UNITY_GITHUB_PAT") ?? "";

        if (!string.IsNullOrWhiteSpace(pat))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", pat);
        }

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Unity-GrantManager");
    }

    public string BuildBlameReference(string path, int line)
        => $"{_branch}/{path}#L{line}";

    public string BuildBlameUrl(string reference)
        => $"https://github.com/{_owner}/{_repo}/blame/{reference}";

    public Task<GitHubBlameInfo?> GetBlameFromReferenceAsync(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return Task.FromResult<GitHubBlameInfo?>(null);

        string branch = _branch;
        string pathWithFragment = reference;

        int firstSlash = reference.IndexOf('/');

        if (firstSlash > 0)
        {
            var possibleBranch = reference[..firstSlash];

            if (possibleBranch is "main" or "dev" or "test")
            {
                branch = possibleBranch;
                pathWithFragment = reference[(firstSlash + 1)..];
            }
        }

        var parts = pathWithFragment.Split("#L");
        var path = parts[0];
        var line = (parts.Length > 1 && int.TryParse(parts[1], out var l)) ? l : 1;

        return GetBlameAsync(_owner, _repo, branch, path, line);
    }

    public Task<GitHubBlameInfo?> GetBlameAsync(string repoPath, int line)
        => GetBlameAsync(_owner, _repo, _branch, repoPath, line);

    public async Task<GitHubBlameInfo?> GetBlameAsync(
        string owner,
        string repo,
        string branch,
        string repoPath,
        int line)
    {
        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
            return null;

        // 🔥 FIX: resolve ACTUAL HEAD commit safely (not ambiguous ref target)
        var sha = await ResolveBranchHeadCommitShaAsync(owner, repo, branch);

        if (string.IsNullOrWhiteSpace(sha))
            return null;

        var query = BuildBlameQuery(owner, repo, sha, repoPath);
        var payload = JsonSerializer.Serialize(new { query });

        var url = await GetGraphQlUrlAsync();
        if (url == null)
            return null;

        using var response = await _httpClient.PostAsync(
            url,
            new StringContent(payload, Encoding.UTF8, "application/json"));

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[BlameLookup] GitHub error: {Json}", json);
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("errors", out var errors))
        {
            _logger.LogWarning("[BlameLookup] GraphQL errors: {Errors}", errors);
            return null;
        }

        var ranges = root
            .GetProperty("data")
            .GetProperty("repository")
            .GetProperty("object")
            .GetProperty("blame")
            .GetProperty("ranges");

        foreach (var range in ranges.EnumerateArray())
        {
            int start = range.GetProperty("startingLine").GetInt32();
            int end = range.GetProperty("endingLine").GetInt32();

            if (line < start || line > end)
                continue;

            var commit = range.GetProperty("commit");
            var author = commit.GetProperty("author");

            var prs = commit
                .GetProperty("associatedPullRequests")
                .GetProperty("nodes");

            string? prUrl = null;
            string? prTitle = null;
            int? prNumber = null;

            if (prs.GetArrayLength() > 0)
            {
                var pr = prs[0];
                prUrl = pr.GetProperty("url").GetString();
                prTitle = pr.GetProperty("title").GetString();

                if (pr.TryGetProperty("number", out var n))
                    prNumber = n.GetInt32();
            }

            return new GitHubBlameInfo
            {
                CommitSha = commit.GetProperty("oid").GetString() ?? "",
                Author = author.GetProperty("name").GetString() ?? "",
                Email = author.GetProperty("email").GetString() ?? "",
                Message = commit.GetProperty("messageHeadline").GetString() ?? "",
                PullRequestUrl = prUrl,
                PullRequestNumber = prNumber,
                PullRequestTitle = prTitle
            };
        }

        return null;
    }

    // 🔥 FIXED: reliable HEAD commit resolution (prevents wrong object types)
    private async Task<string?> ResolveBranchHeadCommitShaAsync(string owner, string repo, string branch)
    {
        var query = $@"
query {{
  repository(owner: ""{owner}"", name: ""{repo}"") {{
    ref(qualifiedName: ""refs/heads/{branch}"") {{
      target {{
        ... on Commit {{
          history(first: 1) {{
            nodes {{
              oid
            }}
          }}
        }}
      }}
    }}
  }}
}}";

        var payload = JsonSerializer.Serialize(new { query });

        var url = await GetGraphQlUrlAsync();
        if (url == null)
            return null;

        using var response = await _httpClient.PostAsync(
            url,
            new StringContent(payload, Encoding.UTF8, "application/json"));

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        try
        {
            return doc.RootElement
                .GetProperty("data")
                .GetProperty("repository")
                .GetProperty("ref")
                .GetProperty("target")
                .GetProperty("history")
                .GetProperty("nodes")[0]
                .GetProperty("oid")
                .GetString();
        }
        catch
        {
            _logger.LogWarning("[BlameLookup] Failed to resolve HEAD commit SHA");
            return null;
        }
    }

    private async Task<string?> GetGraphQlUrlAsync()
    {
        try
        {
            return _endpointService != null
                ? await _endpointService.GetGitHubGraphQlUrlAsync()
                : "https://api.github.com/graphql";
        }
        catch
        {
            return "https://api.github.com/graphql";
        }
    }

    private static string BuildBlameQuery(string owner, string repo, string sha, string path)
    {
        return $@"
query {{
  repository(owner: ""{owner}"", name: ""{repo}"") {{
    object(oid: ""{sha}"") {{
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