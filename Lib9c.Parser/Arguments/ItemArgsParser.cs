using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using CsvHelper;
using Lib9c.Arguments;

namespace Lib9c.Parser.Arguments
{
    public static class ItemArgsParser
    {
        public static IEnumerable<ItemArgs> Parse(string source, ParseOptions options)
        {
            return options.SourceFormat switch
            {
                SourceFormat.Csv => ParseFromCsv(source),
                SourceFormat.Json => ParseFromJson(source),
                _ => throw new ArgumentOutOfRangeException(nameof(options)),
            };
        }

        public static IEnumerable<ItemArgs> ParseFromCsv(string csv)
        {
            if (string.IsNullOrEmpty(csv))
            {
                Console.WriteLine("Empty CSV.");
                return Array.Empty<ItemArgs>();
            }

            var itemArgsList = new List<ItemArgs>();
            using var reader = new StringReader(csv);
            using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
            if (!csvReader.Read())
            {
                Console.WriteLine("Empty CSV.");
                return Array.Empty<ItemArgs>();
            }

            try
            {
                if (!csvReader.ReadHeader())
                {
                    Console.WriteLine("Empty CSV.");
                    return Array.Empty<ItemArgs>();
                }
            }
            catch (ReaderException e)
            {
                Console.WriteLine(e.Message + "\n" + e.StackTrace);
                return Array.Empty<ItemArgs>();
            }

            while (csvReader.Read())
            {
                if (!csvReader.TryGetField<int?>("sheet_id", out var sheetId))
                {
                    sheetId = null;
                }

                if (!csvReader.TryGetField<int?>("count", out var count))
                {
                    count = null;
                }

                if (!csvReader.TryGetField<int?>("level", out var level))
                {
                    level = null;
                }

                if (!csvReader.TryGetField<bool?>("tradable", out var tradable))
                {
                    tradable = null;
                }

                itemArgsList.Add(new ItemArgs(sheetId, count, level, tradable));
            }

            return itemArgsList;
        }

        public static IEnumerable<ItemArgs> ParseFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Console.WriteLine("Empty JSON.");
                return Array.Empty<ItemArgs>();
            }

            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            using var jsonDoc = JsonDocument.Parse(json);
            var rootElement = jsonDoc.RootElement;
            switch (rootElement.ValueKind)
            {
                case JsonValueKind.Object:
                    var itemArgs = rootElement.Deserialize<ItemArgs>(options);
                    return new[] { itemArgs };
                case JsonValueKind.Array:
                    return rootElement.EnumerateArray()
                        .Select(jsonElement => jsonElement.Deserialize<ItemArgs>(options))
                        .ToArray();
                case JsonValueKind.Undefined:
                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
