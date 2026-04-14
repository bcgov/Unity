namespace Unity.GrantManager.Attachments;

public interface IS3PresignedUrlService
{
    string GetPresignedUrl(string s3ObjectKey, int expiryMinutes = 10);
}
