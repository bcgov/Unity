using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.Emails
{
    public interface IEmailsService
    {
        Task<EmailDto> CreateEmailAsync(Guid id, CreateEmailDto dto);
        Task<IReadOnlyList<EmailDto>> GetEmailsAsync(Guid id);
        Task<EmailDto> UpdateEmailAsync(Guid id, UpdateEmailDto dto);
        Task<EmailDto> GetEmailAsync(Guid id, Guid commentId);
    }
}
