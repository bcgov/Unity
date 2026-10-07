using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Unity.Modules.Shared.Utils;

namespace Unity.GrantManager.GrantsPortal.Notifications;

public static class ApplicantUpdateEmailRenderer
{
    public static string Render(string baseUrl, Guid tenantId, Guid applicantId, string displayId,
        string applicantName, DateTime updatedAtUtc, ApplicantUpdateDetails details)
    {
        var link = QueryHelpers.AddQueryString($"{baseUrl.TrimEnd('/')}/GrantApplicants/Details",
            new Dictionary<string, string?>
            {
                ["ApplicantId"] = applicantId.ToString(),
                ["TenantId"] = tenantId.ToString()
            });
        var timestamp = DateTimeExtensions.FormatPacificTime(updatedAtUtc);

        var body = new StringBuilder("<!DOCTYPE html><html lang=\"en\"><body><h2>Applicant update</h2>");
        body.Append($"<p><strong>Applicant name:</strong> {Encode(applicantName)}</p>");
        body.Append($"<p><strong>Applicant ID:</strong> <a href=\"{Encode(link)}\">{Encode(displayId)}</a></p>");
        body.Append($"<p><strong>Date and time of update:</strong> {Encode(timestamp)}</p>");
        body.Append($"<p><strong>Type of update:</strong> {Encode(details.UpdateType)}</p>");
        body.Append("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\"><thead><tr><th scope=\"col\">Field name</th>");
        body.Append(details.IsComparison
            ? "<th scope=\"col\">Previous value</th><th scope=\"col\">New value</th>"
            : "<th scope=\"col\">Value</th>");
        body.Append("</tr></thead><tbody>");
        foreach (var field in details.Fields)
        {
            body.Append($"<tr><th scope=\"row\">{Encode(field.Name)}</th>");
            if (details.IsComparison)
            {
                body.Append($"<td>{EncodeValue(field.PreviousValue)}</td>");
            }
            body.Append($"<td>{EncodeValue(field.Value)}</td></tr>");
        }
        return body.Append("</tbody></table></body></html>").ToString();
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
    private static string EncodeValue(string? value) => Encode(string.IsNullOrWhiteSpace(value) ? "Not provided" : value);
}
