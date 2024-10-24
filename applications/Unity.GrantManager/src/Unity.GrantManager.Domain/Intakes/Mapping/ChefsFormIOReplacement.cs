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

        private static int OneMinuteMilliseconds = 60000;
        public static string ReplaceAdvancedFormIoControls(dynamic formSubmission)
        {
            string formSubmissionStr = formSubmission.ToString();
            if (!string.IsNullOrEmpty(formSubmissionStr))
            {
                Dictionary<string, string> subPatterns = new Dictionary<string, string>
                {
                    { @"\borgbook\b", "select" },
                    { @"\bsimpleaddressadvanced\b", "address" },
                    { @"\bsimplebuttonadvanced\b", "button" },
                    { @"\bsimplecheckboxadvanced\b", "checkbox" },
                    { @"\bsimplecurrencyadvanced\b", "currency" },
                    { @"\bsimpledatetimeadvanced\b", "datetime" },
                    { @"\bsimpledayadvanced\b", "day" },
                    { @"\bsimpleemailadvanced\b", "email" },
                    { @"\bsimplenumberadvanced\b", "number" },
                    { @"\bsimplepasswordadvanced\b", "password" },
                    { @"\bsimplephonenumberadvanced\b", "phoneNumber" },
                    { @"\bsimpleradioadvanced\b", "radio" },
                    { @"\bsimpleselectadvanced\b", "select" },
                    { @"\bsimpleselectboxesadvanced\b", "selectboxes" },
                    { @"\bsimplesignatureadvanced\b", "signature" },
                    { @"\bsimplesurveyadvanced\b", "survey" },
                    { @"\bsimpletagsadvanced\b", "tags" },
                    { @"\bsimpletextareaadvanced\b", "textarea" },
                    { @"\bsimpletextfieldadvanced\b", "textfield" },
                    { @"\bsimpletimeadvanced\b", "time" },
                    { @"\bsimpleurladvanced\b", "url" },

                    // Regular components
                    { @"\bsimplebcaddress\b", "address" },
                    { @"\bbcaddress\b", "address" },
                    { @"\bsimplebtnreset\b", "button" },
                    { @"\bsimplebtnsubmit\b", "button" },
                    { @"\bsimplecheckboxes\b", "selectboxes" },
                    { @"\bsimplecheckbox\b", "checkbox" },
                    { @"\bsimplecols2\b", "columns" },
                    { @"\bsimplecols3\b", "columns" },
                    { @"\bsimplecols4\b", "columns" },
                    { @"\bsimplecontent\b", "content" },
                    { @"\bsimpledatetime\b", "datetime" },
                    { @"\bsimpleday\b", "day" },
                    { @"\bsimpleemail\b", "email" },
                    { @"\bsimplefile\b", "file" },
                    { @"\bsimpleheading\b", "header" },
                    { @"\bsimplefieldset\b", "fieldset" },
                    { @"\bsimplenumber\b", "number" },
                    { @"\bsimplepanel", "panel" },
                    { @"\bsimpleparagraph\b", "textarea" },
                    { @"\bsimplephonenumber\b", "phoneNumber" },
                    { @"\bsimpleradios\b", "radio" },
                    { @"\bsimpleselect\b", "select" },
                    { @"\bsimpletabs\b", "tabs" },
                    { @"\bsimpletextarea\b", "textarea" },
                    { @"\bsimpletextfield\b", "textfield" },
                    { @"\bsimpletime\b", "time" }
                };
                string replacedString = formSubmissionStr;

                //find the replacement
                foreach (var subPattern in subPatterns)
                {
                    string patternKey = subPattern.Key;
                    string replace = subPattern.Value;
                    // Allow one minute timeout
                    try
                    {
                        replacedString = Regex.Replace(replacedString,
                            patternKey,
                            replace,
                            RegexOptions.None,
                            TimeSpan.FromMilliseconds(OneMinuteMilliseconds));
                    }
                    catch (RegexMatchTimeoutException ex)
                    {
                        string ExceptionMessage = ex.Message;
                        logger.LogWarning(ex, "ReplaceAdvancedFormIoControls RegEx Exception {ExceptionMessage}", ExceptionMessage);
                    }
                }

                formSubmissionStr = replacedString;
            }
            return formSubmissionStr;
        }
    }
}