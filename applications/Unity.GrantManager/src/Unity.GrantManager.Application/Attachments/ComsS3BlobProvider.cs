using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Attachments;

public class ComsS3BlobProvider : BlobProviderBase, ITransientDependency
{
    public override Task<bool> DeleteAsync(BlobProviderDeleteArgs args)
    {
        throw new NotImplementedException();
    }

    public override Task<bool> ExistsAsync(BlobProviderExistsArgs args)
    {
        throw new NotImplementedException();
    }

    public override Task<Stream> GetOrNullAsync(BlobProviderGetArgs args)
    {
        throw new NotImplementedException();
    }

    public override async Task SaveAsync(BlobProviderSaveArgs args)
    {
        var config = args.Configuration.GetComsS3BlobProviderConfiguration();
        var bucketId = config.BucketId;
        var baseUri = config.BaseUri;
        var username = config.Username;
        var password = config.Password;
        if(!baseUri.EndsWith("/"))        
        {
            baseUri += "/";
        }
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Put, baseUri+"object?bucketId="+bucketId);
        request.Content = new StreamContent(args.BlobStream);
        request.Content.Headers.Remove("Content-Disposition");
        var contentDisposition = "attachment; filename=" + Uri.EscapeDataString(args.BlobName) + "; filename*=UTF-8''" + Uri.EscapeDataString(args.BlobName);
        request.Content.Headers.Add("Content-Disposition", contentDisposition);
        var authenticationString = $"{username}:{password}";
        var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(authenticationString));

        request.Headers.Add("Authorization", "Basic "+ base64EncodedAuthenticationString);
        
        var response = await client.SendAsync(request);
        if((int)response.StatusCode==409)
        {
            throw new Exception("File Already Existing.");
        } else if ((int)response.StatusCode == 200)
        {
            response.EnsureSuccessStatusCode();
            var responseStr = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseStr);
        }
        
        
    }
}
