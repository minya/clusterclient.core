﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Vostok.ClusterClient.Core.Model
{
    public class HeaderValuesWithQualityCollection : IEnumerable<HeaderValueWithQuality>
    {
        private static readonly char[] ValueSeparator = {','};
        private static readonly char[] QualitySeparator = {';'};
        private static readonly char[] QualityValueSeparator = {'='};
        private static readonly Regex ValueFormat = new Regex(@"^\s*([a-z0-9\-+\*/]+\s*(;\s*q\s*=\s*([01](\.[0-9]{0,3})?)\s*)?,\s*)*([a-z0-9\-+\*/]+\s*(;\s*q\s*=\s*([01](\.[0-9]{0,3})?)\s*)?)?$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        public static HeaderValuesWithQualityCollection Parse(string headerValue)
        {
            var result = new HeaderValuesWithQualityCollection();

            if (string.IsNullOrEmpty(headerValue))
                return result;

            if (!ValueFormat.IsMatch(headerValue))
                throw new FormatException($"Invalid header value: '{headerValue}'.");

            var headerValueParts = headerValue.Split(ValueSeparator, StringSplitOptions.RemoveEmptyEntries);
            foreach (var valueWithQuality in headerValueParts)
            {
                var valueQuality = valueWithQuality.Split(QualitySeparator, StringSplitOptions.RemoveEmptyEntries);
                if (valueQuality.Length < 1 || valueQuality.Length > 2)
                    throw new FormatException($"Invalid header value format at '{valueWithQuality}'");

                var value = valueQuality.FirstOrDefault();
                if (valueQuality.Length < 2 ||
                    !decimal.TryParse(valueQuality[1].Split(QualityValueSeparator, StringSplitOptions.RemoveEmptyEntries).LastOrDefault(), NumberStyles.Number, NumberFormatInfo.InvariantInfo, out var quality))
                    quality = 1m;
                result.Add(value, quality);
            }

            return result;
        }

        private readonly List<HeaderValueWithQuality> sortedValues = new List<HeaderValueWithQuality>();

        public int Count => sortedValues.Count;

        public HeaderValueWithQuality this[int idx] => sortedValues[idx];

        public void Add(string value, decimal quality = 1m) => Add(new HeaderValueWithQuality(value, quality));

        public void Add(HeaderValueWithQuality valueWithQuality)
        {
            if (sortedValues.Count == 0)
            {
                sortedValues.Add(valueWithQuality);
                return;
            }

            var insertIndex = sortedValues.FindIndex(i => i.Quality < valueWithQuality.Quality); //todo: binary search?
            if (insertIndex < 0)
                sortedValues.Add(valueWithQuality);
            else
                sortedValues.Insert(insertIndex, valueWithQuality);
        }

        public override string ToString() => string.Join(",", sortedValues);

        public void Remove(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            sortedValues.RemoveAll(i => i.Value.Equals(value, StringComparison.InvariantCultureIgnoreCase));
        }

        public void Remove(string value, decimal quality) =>
            Remove(new HeaderValueWithQuality(value, quality));

        public void Remove(HeaderValueWithQuality valueWithQuality) =>
            sortedValues.RemoveAll(i => i.Equals(valueWithQuality));

        public void Clear() => sortedValues.Clear();

        public IEnumerator<HeaderValueWithQuality> GetEnumerator() => sortedValues.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}