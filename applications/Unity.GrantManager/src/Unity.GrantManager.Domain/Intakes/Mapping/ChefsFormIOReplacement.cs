using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Intakes.Mapping
{
    public static class ChefsFormIOReplacement
    {
        private static ILogger logger = NullLogger.Instance;

        // Method to initialize the logger (if needed)
        public static void InitializeLogger(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger("ChefsFormIOReplacement");
        }
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMinutes(1);

        // Map of regex => replacement
        private static readonly Dictionary<Regex, string> SubPatterns = new()
        {
            // Advanced components
            { new Regex(@"""type""\s*:\s*""orgbook""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"select\"" },
            { new Regex(@"""type""\s*:\s*""simpleaddressadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"address\"" },
            { new Regex(@"""type""\s*:\s*""simplebuttonadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"button\"" },
            { new Regex(@"""type""\s*:\s*""simplecheckboxadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"checkbox\"" },
            { new Regex(@"""type""\s*:\s*""simplecurrencyadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"currency\"" },
            { new Regex(@"""type""\s*:\s*""simpledatetimeadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"datetime\"" },
            { new Regex(@"""type""\s*:\s*""simpledayadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"day\"" },
            { new Regex(@"""type""\s*:\s*""simpleemailadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"email\"" },
            { new Regex(@"""type""\s*:\s*""simplenumberadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"number\"" },
            { new Regex(@"""type""\s*:\s*""simplepasswordadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"password\"" },
            { new Regex(@"""type""\s*:\s*""simplephonenumberadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"phoneNumber\"" },
            { new Regex(@"""type""\s*:\s*""simpleradioadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"radio\"" },
            { new Regex(@"""type""\s*:\s*""simpleselectadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"select\"" },
            { new Regex(@"""type""\s*:\s*""simpleselectboxesadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"selectboxes\"" },
            { new Regex(@"""type""\s*:\s*""simplesignatureadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"signature\"" },
            { new Regex(@"""type""\s*:\s*""simplesurveyadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"survey\"" },
            { new Regex(@"""type""\s*:\s*""simpletagsadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"tags\"" },
            { new Regex(@"""type""\s*:\s*""simpletextareaadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"textarea\"" },
            { new Regex(@"""type""\s*:\s*""simpletextfieldadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"textfield\"" },
            { new Regex(@"""type""\s*:\s*""simpletimeadvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"time\"" },
            { new Regex(@"""type""\s*:\s*""simpleurladvanced""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"url\"" },

            // Regular components
            { new Regex(@"""type""\s*:\s*""simplebcaddress""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"address\"" },
            { new Regex(@"""type""\s*:\s*""bcaddress""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"address\"" },
            { new Regex(@"""type""\s*:\s*""simplebtnreset""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"button\"" },
            { new Regex(@"""type""\s*:\s*""simplebtnsubmit""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"button\"" },
            { new Regex(@"""type""\s*:\s*""simplecheckboxes""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"selectboxes\"" },
            { new Regex(@"""type""\s*:\s*""simplecheckbox""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"checkbox\"" },
            { new Regex(@"""type""\s*:\s*""simplecols2""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"columns\"" },
            { new Regex(@"""type""\s*:\s*""simplecols3""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"columns\"" },
            { new Regex(@"""type""\s*:\s*""simplecols4""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"columns\"" },
            { new Regex(@"""type""\s*:\s*""simplecontent""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"content\"" },
            { new Regex(@"""type""\s*:\s*""simpledatetime""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"datetime\"" },
            { new Regex(@"""type""\s*:\s*""simpleday""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"day\"" },
            { new Regex(@"""type""\s*:\s*""simpleemail""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"email\"" },
            { new Regex(@"""type""\s*:\s*""simplefile""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"file\"" },
            { new Regex(@"""type""\s*:\s*""simpleheading""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"header\"" },
            { new Regex(@"""type""\s*:\s*""simplefieldset""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"fieldset\"" },
            { new Regex(@"""type""\s*:\s*""simplenumber""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"number\"" },
            { new Regex(@"""type""\s*:\s*""simplepanel""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"panel\"" },
            { new Regex(@"""type""\s*:\s*""simpleparagraph""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"textarea\"" },
            { new Regex(@"""type""\s*:\s*""simplephonenumber""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"phoneNumber\"" },
            { new Regex(@"""type""\s*:\s*""simpleradios""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"radio\"" },
            { new Regex(@"""type""\s*:\s*""simpleselect""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"select\"" },
            { new Regex(@"""type""\s*:\s*""simpletabs""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"tabs\"" },
            { new Regex(@"""type""\s*:\s*""simpletextarea""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"textarea\"" },
            { new Regex(@"""type""\s*:\s*""simpletextfield""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"textfield\"" },
            { new Regex(@"""type""\s*:\s*""simpletime""", RegexOptions.Compiled, RegexTimeout), "\"type\": \"time\"" }
        };

        public static string ReplaceAdvancedFormIoControls(dynamic formSubmission)
        {
            string? formSubmissionStr = formSubmission?.ToString();
            if (string.IsNullOrEmpty(formSubmissionStr))
                return string.Empty;

            string replacedString = formSubmissionStr;

            try
            {
                foreach (var kv in SubPatterns)
                {
                    replacedString = kv.Key.Replace(replacedString, kv.Value);
                }
            }
            catch (RegexMatchTimeoutException ex)
            {
                logger.LogWarning(ex, "ReplaceAdvancedFormIoControls RegEx Timeout: {Message}", ex.Message);
            }

            return replacedString;
        }
    }
}