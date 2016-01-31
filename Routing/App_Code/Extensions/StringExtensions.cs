using System.Globalization;
using System.Text;
using Umbraco.Core;


namespace Routing.Extensions
{
    public static class StringExtensions
    {
        public static bool Equals(this string stringSelf, string stringToCompare, bool caseSensitive, bool accentSensitive)
        {
            if (!accentSensitive)
            {
                stringSelf = stringSelf.RemoveDiacritics();
                stringToCompare = stringToCompare.RemoveDiacritics();
            }
            if (caseSensitive)
            {
                return stringSelf.Equals(stringToCompare);
            }
            else
            {
                return stringSelf.InvariantEquals(stringToCompare);
            }
        }

        public static string RemoveDiacritics(this string stringSelf)
        {
            var normalizedString = stringSelf.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();
            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }

}
