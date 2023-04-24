using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Arguments;
using Lib9c.Parser.Arguments;

namespace Lib9c.Parser.Tests.Arguments
{
    public class ItemArgsParserTest
    {
        private static IEnumerable<object[]> GetMemberData()
        {
            yield return new object[]
            {
                "",
                SourceFormat.Csv,
                0,
                Array.Empty<(int? sheetId, int? count, int? level, bool? tradable)>(),
            };

            yield return new object[]
            {
                @"sheet_id
1
2
3",
                SourceFormat.Csv,
                3,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, null, null, null),
                    (2, null, null, null),
                    (3, null, null, null),
                },
            };

            yield return new object[]
            {
                @"sheet_id,count
1,1
2,2
3,3",
                SourceFormat.Csv,
                3,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, 1, null, null),
                    (2, 2, null, null),
                    (3, 3, null, null),
                },
            };

            yield return new object[]
            {
                @"sheet_id,level
1,0
2,1
3,2",
                SourceFormat.Csv,
                3,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, null, 0, null),
                    (2, null, 1, null),
                    (3, null, 2, null),
                },
            };

            yield return new object[]
            {
                @"sheet_id,tradable
1,false
2,true
3,false",
                SourceFormat.Csv,
                3,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, null, null, false),
                    (2, null, null, true),
                    (3, null, null, false),
                },
            };

            yield return new object[]
            {
                @"sheet_id,count,level
1,1,0
2,2,1
3,3,2",
                SourceFormat.Csv,
                3,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, 1, 0, null),
                    (2, 2, 1, null),
                    (3, 3, 2, null),
                },
            };

            yield return new object[]
            {
                @"sheet_id,count,tradable
1,1,false
2,2,true
3,3,false",
                SourceFormat.Csv,
                3,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, 1, null, false),
                    (2, 2, null, true),
                    (3, 3, null, false),
                },
            };

            yield return new object[]
            {
                @"sheet_id,level,tradable
1,0,false
2,1,true
3,2,false",
                SourceFormat.Csv,
                3,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, null, 0, false),
                    (2, null, 1, true),
                    (3, null, 2, false),
                },
            };

            yield return new object[]
            {
                @"sheet_id,count,level,tradable
1,1,0,false
2,2,1,true
3,3,2,false",
                SourceFormat.Csv,
                3,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, 1, 0, false),
                    (2, 2, 1, true),
                    (3, 3, 2, false),
                },
            };

            yield return new object[]
            {
                "",
                SourceFormat.Json,
                0,
                Array.Empty<(int? sheetId, int? count, int? level, bool? tradable)>(),
            };

            yield return new object[]
            {
                "[]",
                SourceFormat.Json,
                0,
                Array.Empty<(int? sheetId, int? count, int? level, bool? tradable)>(),
            };

            yield return new object[]
            {
                "{\"SheetId\": 1}",
                SourceFormat.Json,
                1,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, null, null, null),
                },
            };

            yield return new object[]
            {
                "{\"sheetId\": 1, \"count\": 1}",
                SourceFormat.Json,
                1,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, 1, null, null),
                },
            };

            yield return new object[]
            {
                "{\"sheetId\": 1, \"level\": 0}",
                SourceFormat.Json,
                1,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, null, 0, null),
                },
            };

            yield return new object[]
            {
                "{\"sheetId\": 1, \"tradable\": true}",
                SourceFormat.Json,
                1,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, null, null, true),
                },
            };

            yield return new object[]
            {
                "{\"sheetId\": 1, \"count\": 1, \"level\": 0}",
                SourceFormat.Json,
                1,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, 1, 0, null),
                },
            };

            yield return new object[]
            {
                "{\"sheetId\": 1, \"count\": 1, \"tradable\": true}",
                SourceFormat.Json,
                1,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, 1, null, true),
                },
            };

            yield return new object[]
            {
                "{\"sheetId\": 1, \"level\": 0, \"tradable\": true}",
                SourceFormat.Json,
                1,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, null, 0, true),
                },
            };

            yield return new object[]
            {
                "{\"sheetId\": 1, \"count\": 1, \"level\": 0, \"tradable\": true}",
                SourceFormat.Json,
                1,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, 1, 0, true),
                },
            };

            yield return new object[]
            {
                @"[
{""sheetId"": 1},
{""sheetId"": 2}
]",
                SourceFormat.Json,
                2,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, null, null, null),
                    (2, null, null, null),
                },
            };

            yield return new object[]
            {
                @"[
{""sheetId"": 1, ""count"": 1},
{""sheetId"": 2, ""count"": 2}
]",
                SourceFormat.Json,
                2,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, 1, null, null),
                    (2, 2, null, null),
                },
            };

            yield return new object[]
            {
                @"[
{""sheetId"": 1, ""level"": 0},
{""sheetId"": 2, ""level"": 1}
]",
                SourceFormat.Json,
                2,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, null, 0, null),
                    (2, null, 1, null),
                },
            };

            yield return new object[]
            {
                @"[
{""sheetId"": 1, ""tradable"": false},
{""sheetId"": 2, ""tradable"": true}
]",
                SourceFormat.Json,
                2,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, null, null, false),
                    (2, null, null, true),
                },
            };

            yield return new object[]
            {
                @"[
{""sheetId"": 1, ""count"": 1, ""level"": 0},
{""sheetId"": 2, ""count"": 2, ""level"": 1}
]",
                SourceFormat.Json,
                2,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, 1, 0, null),
                    (2, 2, 1, null),
                },
            };

            yield return new object[]
            {
                @"[
{""sheetId"": 1, ""count"": 1, ""tradable"": false},
{""sheetId"": 2, ""count"": 2, ""tradable"": true}
]",
                SourceFormat.Json,
                2,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, 1, null, false),
                    (2, 2, null, true),
                },
            };

            yield return new object[]
            {
                @"[
{""sheetId"": 1, ""level"": 0, ""tradable"": false},
{""sheetId"": 2, ""level"": 1, ""tradable"": true}
]",
                SourceFormat.Json,
                2,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, null, 0, false),
                    (2, null, 1, true),
                },
            };

            yield return new object[]
            {
                @"[
{""sheetId"": 1, ""count"": 1, ""level"": 0, ""tradable"": false},
{""sheetId"": 2, ""count"": 2, ""level"": 1, ""tradable"": true}
]",
                SourceFormat.Json,
                2,
                new (int? sheetId, int? count, int? level, bool? tradable)[]
                {
                    (1, 1, 0, false),
                    (2, 2, 1, true),
                },
            };
        }

        [Theory]
        [MemberData(nameof(GetMemberData))]
        public void CsvToItem_Via_ItemArgsParser(
            string source,
            SourceFormat sourceFormat,
            int expectedItemsCount,
            (int? sheetId, int? count, int? level, bool? tradable)[] expectedItems)
        {
            var enumerable = ItemArgsParser.Parse(source, new ParseOptions
            {
                SourceFormat = sourceFormat,
            });
            var arr = enumerable as ItemArgs[] ?? enumerable.ToArray();
            Assert.Equal(expectedItemsCount, arr.Length);
            for (var i = 0; i < arr.Length; i++)
            {
                var itemArgs = arr[i];
                var (sheetId, count, level, tradable) = expectedItems[i];
                ValidateItem(
                    itemArgs,
                    sheetId,
                    count,
                    level,
                    tradable);
            }
        }

        private static void ValidateItem(
            ItemArgs itemArgs,
            int? sheetId,
            int? count,
            int? level,
            bool? tradable)
        {
            Assert.Equal(sheetId, itemArgs.SheetId);
            Assert.Equal(count, itemArgs.Count);
            Assert.Equal(level, itemArgs.Level);
            Assert.Equal(tradable, itemArgs.Tradable);
        }
    }
}
