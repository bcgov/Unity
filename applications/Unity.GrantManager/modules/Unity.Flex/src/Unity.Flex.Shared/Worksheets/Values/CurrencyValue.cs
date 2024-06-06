using System;

namespace Unity.Flex.Worksheets.Values
{
    public class CurrencyValue : CustomValueBase
    {
        public CurrencyValue() : base() { }
        public CurrencyValue(object value) : base(value) { }

        internal object Convert()
        {
            throw new NotImplementedException();
        }
    }
}
