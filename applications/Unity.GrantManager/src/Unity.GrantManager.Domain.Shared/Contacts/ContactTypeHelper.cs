using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GrantManager.Contacts
{
    public static class ContactTypeHelper
    {
        public class ContactTypeDto
        {
            public string Value { get; set; } = default!;
            public string Display { get; set; } = default!;
        }

        public static List<ContactTypeDto> GetApplicantContactTypes()
        {
            return [.. Enum.GetValues<ContactTypes.ApplicantContactTypes>()
                .Select(x => new ContactTypeDto
                {
                    Value = x.ToString(),
                    Display = x.GetDisplayName()
                })];
        }
    }
}
