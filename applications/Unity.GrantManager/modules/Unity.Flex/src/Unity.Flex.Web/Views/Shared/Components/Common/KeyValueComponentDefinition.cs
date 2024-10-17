using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.Common
{
    public partial class KeyValueComponentDefinition : AbpViewComponent
    {
        const string validInputPattern = @"^[ə̀ə̩ə̥ɛæə̌ə̂ə̧ə̕ə̓ᵒə̄ə̱·ʷəŧⱦʸʋɨⱡɫʔʕⱥɬθᶿɣɔɩłə̈ʼə̲ᶻꭓȼƛλŋƚə̨ə̣ə́ `1234567890-=qwertyuiop[]asdfghjkl;_'_\\zxcvbnm,.~!@#$%^&*()_+QWERTYUIOP{}ASDFGHJKL:""||ZXCVBNM<>?]+$";

        [GeneratedRegex(validInputPattern)]
        protected static partial Regex InputRegex();


        public virtual async Task<IViewComponentResult> InvokeAsync(string? definition)
        {
            await Task.CompletedTask;
            return View();
        }

        protected enum KeyValueType
        {
            Values = 0,
            Labels = 1
        };

        protected static void ValidateKeysFormat(StringValues keys)
        {
            foreach (var key in keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new UserFriendlyException("There are empty Keys captured which are required");
                }

                if (!IsValidInput(key))
                {
                    throw new UserFriendlyException("The following characters are allowed for Keys: " + validInputPattern);
                }
            }
        }

        protected static void ValidateKeysUnique(StringValues keys)
        {
            if (keys.Distinct().Count() != keys.Count)
            {
                throw new UserFriendlyException("Provided Keys must be unique");
            }
        }

        protected static bool IsValidInput(string input)
        {
            Regex regex = InputRegex();
            return regex.IsMatch(input);
        }

        protected static void ValidateInputCounts(StringValues keys, StringValues labels, KeyValueType type)
        {
            if (keys.Count == 0 || labels.Count == 0)
            {
                throw new UserFriendlyException($"Both Keys and {type} are required");
            }
        }

        protected static void ValidateValuesFormat(StringValues values, KeyValueType type)
        {
            foreach (var key in values)
            {
                if (!IsValidInput(key ?? string.Empty))
                {
                    throw new UserFriendlyException($"The following characters are allowed for {type}: " + validInputPattern);
                }
            }
        }

        protected static void ValidateInput(StringValues keys, StringValues labels, KeyValueType type)
        {
            ValidateInputCounts(keys, labels, type);
            ValidateKeysUnique(keys);
            ValidateKeysFormat(keys);
            ValidateValuesFormat(labels, type);
        }
    }
}
