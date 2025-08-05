namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Text;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class PatchTableSheetCompressedTest
    {
        private readonly IActionContext _context;
        private readonly IWorld _previousState;
        private readonly Dictionary<string, string> _sheet;

        public PatchTableSheetCompressedTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
            _previousState = new World(MockWorldState.CreateModern());
            _context = new ActionContext
            {
                PreviousState = _previousState,
                RandomSeed = 0,
            };
            _sheet = TableSheetsImporter.ImportSheets();
        }

        [Fact]
        public void CompressCsv_WithValidData_ShouldCompress()
        {
            // Arrange
            var originalCsv = _sheet["RuneOptionSheet"];

            // Act
            var compressedData = PatchTableSheetCompressed.CompressCsv(originalCsv);

            // Assert
            Assert.True(compressedData.Length > 0);
            Assert.True(compressedData.Length < Encoding.UTF8.GetByteCount(originalCsv));
        }

        [Fact]
        public void DecompressCsv_WithValidData_ShouldRestoreOriginal()
        {
            // Arrange
            var originalCsv = _sheet["RuneOptionSheet"];
            var compressedData = PatchTableSheetCompressed.CompressCsv(originalCsv);

            // Act
            var decompressedCsv = PatchTableSheetCompressed.DecompressCsv(compressedData);

            // Assert
            Assert.Equal(originalCsv, decompressedCsv);
        }

        [Fact]
        public void Execute_WithValidData_ShouldSucceed()
        {
            // Arrange
            var originalCsv = _sheet["RuneOptionSheet"];
            var compressedData = PatchTableSheetCompressed.CompressCsv(originalCsv);

            var action = new PatchTableSheetCompressed
            {
                TableName = nameof(RuneOptionSheet),
                CompressedTableCsv = compressedData,
            };

            // Act
            var nextState = action.Execute(_context);

            // Assert
            var csvData = (Text)nextState.GetLegacyState(Addresses.GetSheetAddress<RuneOptionSheet>());
            Assert.Equal(originalCsv, csvData);
        }

        [Fact]
        public void CompareTransactionSizes_WithRuneOptionSheet_ShouldShowCompressionBenefit()
        {
            // Arrange
            var runeSheetCsv = _sheet["RuneOptionSheet"];

            // Act - Get serialized data sizes
            var uncompressedSize = Encoding.UTF8.GetByteCount(runeSheetCsv);
            var compressedSize = PatchTableSheetCompressed.CompressCsv(runeSheetCsv).Length;

            // Calculate compression ratio
            var compressionRatio = (double)compressedSize / uncompressedSize;
            var sizeReduction = uncompressedSize - compressedSize;
            var reductionPercentage = (double)sizeReduction / uncompressedSize * 100;

            // Assert
            Assert.True(
                compressedSize < uncompressedSize,
                $"Compressed size ({compressedSize} bytes) should be smaller than uncompressed size ({uncompressedSize} bytes)");

            // Log results
            Log.Information($"=== Transaction Size Comparison (RuneSheet) ===");
            Log.Information($"Original CSV size: {uncompressedSize} bytes");
            Log.Information($"Compressed size: {compressedSize} bytes");
            Log.Information($"Size reduction: {sizeReduction} bytes ({reductionPercentage:F1}%)");
            Log.Information($"Compression ratio: {compressionRatio:P}");
            Log.Information("==============================================");

            // Assert that compression is significant
            Assert.True(
                reductionPercentage > 50,
                $"Should have significant compression: {reductionPercentage:F1}% reduction");
        }
    }
}
