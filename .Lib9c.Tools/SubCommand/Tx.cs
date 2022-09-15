using Bencodex;
using Bencodex.Types;
using Cocona;
using CsvHelper;
using Libplanet;
using Libplanet.Assets;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.State;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Nekoyume.TableData;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Lib9c.Tools.SubCommand
{
    public class Tx
    {
        private static Codec _codec = new Codec();

        [Command(Description = "Create TransferAsset action and dump it.")]
        public void TransferAsset(
            [Argument("SENDER", Description = "An address of sender.")] string sender,
            [Argument("RECIPIENT", Description = "An address of recipient.")] string recipient,
            [Argument("AMOUNT", Description = "An amount of gold to transfer.")] int goldAmount,
            [Argument("GENESIS-BLOCK", Description = "A genesis block containing InitializeStates.")] string genesisBlock
        )
        {
            byte[] genesisBytes = File.ReadAllBytes(genesisBlock);
            var genesisDict = (Bencodex.Types.Dictionary)_codec.Decode(genesisBytes);
            IReadOnlyList<Transaction<NCAction>> genesisTxs =
                BlockMarshaler.UnmarshalBlockTransactions<NCAction>(genesisDict);
            var initStates = (InitializeStates)genesisTxs.Single().CustomActions!.Single().InnerAction;
            Currency currency = new GoldCurrencyState(initStates.GoldCurrency).Currency;

            var action = new TransferAsset(
                new Address(sender),
                new Address(recipient),
                currency * goldAmount
            );

            var bencoded = new List(
                (Text)nameof(TransferAsset),
                action.PlainValue
            );

            byte[] raw = _codec.Encode(bencoded);
            Console.Write(ByteUtil.Hex(raw));
        }

        [Command(Description = "Create new transaction with given actions and dump it.")]
        public void Sign(
            [Argument("PRIVATE-KEY", Description = "A hex-encoded private key for signing.")] string privateKey,
            [Argument("NONCE", Description = "A nonce for new transaction.")] long nonce,
            [Argument("TIMESTAMP", Description = "A datetime for new transaction.")] string timestamp = null,
            [Argument("GENESIS-HASH", Description = "A hex-encoded genesis block hash.")] string genesisHash = null,
            [Option("action", new[] { 'a' }, Description = "Hex-encoded actions or a path of the file contained it.")] string[] actions = null,
            [Option("bytes", new[] { 'b' }, Description = "Print raw bytes instead of base64.  No trailing LF appended.")] bool bytes = false
        )
        {
            List<NCAction> parsedActions = null;
            if (!(actions is null))
            {
                parsedActions = actions.Select(a =>
                {
                    if (File.Exists(a))
                    {
                        Console.WriteLine($"Read all text from {a}");
                        a = File.ReadAllText(a);
                    }

                    var bencoded = (List)_codec.Decode(ByteUtil.ParseHex(a));
                    string type = (Text)bencoded[0];
                    IValue plainValue = bencoded[1];

                    ActionBase action = null;
                    action = type switch
                    {
                        nameof(TransferAsset) => new TransferAsset(),
                        nameof(PatchTableSheet) => new PatchTableSheet(),
                        nameof(AddRedeemCode) => new AddRedeemCode(),
                        nameof(Nekoyume.Action.MigrationLegacyShop) => new MigrationLegacyShop(),
                        nameof(Nekoyume.Action.MigrationActivatedAccountsState) => new MigrationActivatedAccountsState(),
                        nameof(Nekoyume.Action.MigrationAvatarState) => new MigrationAvatarState(),
                        nameof(Nekoyume.Action.CreatePendingActivations) => new CreatePendingActivations(),
                        nameof(Nekoyume.Action.RenewAdminState) => new RenewAdminState(),
                        _ => throw new CommandExitedException($"Can't determine given action type: {type}", 128),
                    };
                    action.LoadPlainValue(plainValue);

                    return (NCAction)action;
                }).ToList();
            }
            else
            {
                parsedActions = new List<NCAction>();
            }
            Transaction<NCAction> tx = Transaction<NCAction>.Create(
                nonce: nonce,
                privateKey: new PrivateKey(ByteUtil.ParseHex(privateKey)),
                genesisHash: (genesisHash is null) ? default : BlockHash.FromString(genesisHash),
                timestamp: (timestamp is null) ? default : DateTimeOffset.Parse(timestamp),
                customActions: parsedActions
            );
            byte[] raw = tx.Serialize(true);

            if (bytes)
            {
                using Stream stdout = Console.OpenStandardOutput();
                stdout.Write(raw);
            }
            else
            {
                Console.WriteLine(Convert.ToBase64String(raw));
            }
        }

        [Command(Description = "Create PatchTable action and dump it.")]
        public void PatchTable(
            [Argument("TABLE-PATH", Description = "A table file path for patch.")]
            string tablePath
        )
        {
            var tableName = Path.GetFileName(tablePath);
            if (tableName.EndsWith(".csv"))
            {
                tableName = tableName.Split(".csv")[0];
            }
            Console.Error.Write("----------------\n");
            Console.Error.Write(tableName);
            Console.Error.Write("\n----------------\n");
            var tableCsv = File.ReadAllText(tablePath);
            Console.Error.Write(tableCsv);

            var type = typeof(ISheet).Assembly
                .GetTypes()
                .First(type => type.Namespace is { } @namespace &&
                               @namespace.StartsWith($"{nameof(Nekoyume)}.{nameof(Nekoyume.TableData)}") &&
                               !type.IsAbstract &&
                               typeof(ISheet).IsAssignableFrom(type) &&
                               type.Name == tableName);
            var sheet = (ISheet) Activator.CreateInstance(type);
            sheet!.Set(tableCsv);

            var action = new PatchTableSheet
            {
                TableName = tableName,
                TableCsv = tableCsv
            };

            var bencoded = new List(
                (Text)nameof(PatchTableSheet),
                action.PlainValue
            );

            byte[] raw = _codec.Encode(bencoded);
            Console.WriteLine(ByteUtil.Hex(raw));
        }

        [Command(Description = "Create MigrationLegacyShop action and dump it.")]
        public void MigrationLegacyShop()
        {
            var action = new MigrationLegacyShop();

            var bencoded = new List(
                (Text)nameof(Nekoyume.Action.MigrationLegacyShop),
                action.PlainValue
            );

            byte[] raw = _codec.Encode(bencoded);
            Console.WriteLine(ByteUtil.Hex(raw));
        }

        [Command(Description = "Create MigrationActivatedAccountsState action and dump it.")]
        public void MigrationActivatedAccountsState()
        {
            var action = new MigrationActivatedAccountsState();
            var bencoded = new List(
                (Text)nameof(Nekoyume.Action.MigrationActivatedAccountsState),
                action.PlainValue
            );

            byte[] raw = _codec.Encode(bencoded);
            Console.WriteLine(ByteUtil.Hex(raw));
        }

        [Command(Description = "Create MigrationAvatarState action and dump it.")]
        public void MigrationAvatarState(
        [Argument("directory-path", Description = "path of the directory contained hex-encoded avatar states.")] string directoryPath,
        [Argument("output-path", Description = "path of the output file dumped action.")] string outputPath
        )
        {
            Console.WriteLine("> directory path: " + directoryPath);
            Console.WriteLine("> output path: " + outputPath);
            var avatarStates = Directory
                .GetFiles(directoryPath, "*", SearchOption.TopDirectoryOnly)
                .Where(filePath => !filePath.ToLower().EndsWith(".ds_store"))
                .Select(filePath =>
                {
                    Console.WriteLine("> file path: " + filePath);
                    var raw = File.ReadAllText(filePath);
                    var dict = (Dictionary)_codec.Decode(ByteUtil.ParseHex(raw));
                    Console.WriteLine($">> {SerializeKeys.LegacyInventoryKey}:" +
                                      $" {(dict.ContainsKey(SerializeKeys.LegacyInventoryKey) ? "not null" : "null")}");
                    Console.WriteLine($">> {SerializeKeys.LegacyWorldInformationKey}:" +
                                      $" {(dict.ContainsKey(SerializeKeys.LegacyWorldInformationKey) ? "not null" : "null")}");
                    Console.WriteLine($">> {SerializeKeys.LegacyQuestListKey}:" +
                                      $" {(dict.ContainsKey(SerializeKeys.LegacyQuestListKey) ? "not null" : "null")}");
                    return dict;
                })
                .ToList();
            var action = new MigrationAvatarState
            {
                avatarStates = avatarStates
            };

            var encoded = new List(
                (Text)nameof(Nekoyume.Action.MigrationAvatarState),
                action.PlainValue
            );

            var bytes = _codec.Encode(encoded);
            File.WriteAllText(outputPath, ByteUtil.Hex(bytes));
        }

        [Command(Description = "Create AddRedeemCode action and dump it.")]
        public void AddRedeemCode(
            [Argument("TABLE-PATH", Description = "A table file path for RedeemCodeListSheet")] string tablePath
        )
        {
            var tableCsv = File.ReadAllText(tablePath);
            var action = new AddRedeemCode
            {
                redeemCsv = tableCsv
            };
            var encoded = new List(
                (Text)nameof(Nekoyume.Action.AddRedeemCode),
                action.PlainValue
            );
            byte[] raw = _codec.Encode(encoded);
            Console.WriteLine(ByteUtil.Hex(raw));
        }

        [Command(Description = "Create CreatePendingActivations action and dump it.")]
        public void CreatePendingActivations(
            [Argument("CSV-PATH", Description = "A csv file path for CreatePendingActivations")] string csvPath
        )
        {
            var RecordType = new
            {
                EncodedActivationKey = string.Empty,
                NonceHex = string.Empty,
            };
            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var activations =
                csv.GetRecords(RecordType)
                    .Select(r => new PendingActivationState(
                        ByteUtil.ParseHex(r.NonceHex),
                        ActivationKey.Decode(r.EncodedActivationKey).PrivateKey.PublicKey)
                    )
                    .ToList();
            var action = new CreatePendingActivations(activations);
            var encoded = new List(
               new IValue[]
               {
                    (Text) nameof(Nekoyume.Action.CreatePendingActivations),
                    action.PlainValue
               }
           );
            byte[] raw = _codec.Encode(encoded);
            Console.WriteLine(ByteUtil.Hex(raw));
        }

        [Command(Description = "Create RenewAdminState action and dump it.")]
        public void RenewAdminState(
            [Argument("NEW-VALID-UNTIL")]
            long newValidUntil
        )
        {
            RenewAdminState action = new RenewAdminState(newValidUntil);
            var encoded = new List(
                (Text) nameof(Nekoyume.Action.RenewAdminState),
                action.PlainValue
            );
            byte[] raw = _codec.Encode(encoded);
            Console.WriteLine(ByteUtil.Hex(raw));
        }

        [Command(Description = "Create ActvationKey-nonce pairs and dump them as csv")]
        public void CreateActivationKeys(
            [Argument("COUNT", Description = "An amount of pairs")] int count
        )
        {
            var rng = new Random();
            var nonce = new byte[4];
            Console.WriteLine("EncodedActivationKey,NonceHex");
            foreach (int i in Enumerable.Range(0, count))
            {
                PrivateKey key;
                while (true)
                {
                    key = new PrivateKey();
                    if (key.ToByteArray().Length == 32)
                    {
                        break;
                    }
                }

                rng.NextBytes(nonce);
                var (ak, _) = ActivationKey.Create(key, nonce);
                Console.WriteLine($"{ak.Encode()},{ByteUtil.Hex(nonce)}");
            }
        }
    }
}
