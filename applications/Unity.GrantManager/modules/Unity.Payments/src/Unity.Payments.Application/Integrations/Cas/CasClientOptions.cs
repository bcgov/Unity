﻿namespace Unity.Payments.Integrations.Cas
{
    public class CasClientOptions
    {
        public string CasBaseUrl { get; set; } = string.Empty;
        public string CasClientId { get; set; } = string.Empty;
        public string CasClientSecret { get; set; } = string.Empty;
    }
}