﻿namespace Unity.GrantManager.Integrations.Mail
{
    public class ChesClientOptions
    {
        public string ChesUrl { get; set; } = string.Empty;
        public string ChesTokenUrl { get; set; } = string.Empty;
        public string ChesClientId { get; set; } = string.Empty;
        public string ChesClientSecret { get; set; } = string.Empty;
        public string ChesFromEmail { get; set; } = string.Empty;
    }
}