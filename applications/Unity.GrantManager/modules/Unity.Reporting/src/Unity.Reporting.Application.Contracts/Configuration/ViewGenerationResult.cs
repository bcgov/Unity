using System;

namespace Unity.Reporting.Configuration
{
    /// <summary>
    /// Represents the result of a view generation request.
    /// </summary>
    public class ViewGenerationResult
    {
        /// <summary>
        /// A human-readable message describing the status of the view generation request.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The name of the view that was queued for generation.
        /// </summary>
        public string ViewName { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the view generation has been successfully queued.
        /// </summary>
        public bool IsQueued { get; set; }

        /// <summary>
        /// The orginal view name - can be referenced if changing the view name
        /// </summary>
        public string OriginalViewName { get; set; } = string.Empty;
    }
}