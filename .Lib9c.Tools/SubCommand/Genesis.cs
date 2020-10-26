using Cocona;
using Libplanet;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.State;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Lib9c.Tools.SubCommand
{
    public class Genesis
    {
        [Command(Description = "Convert key files to pendings")]
        public void ConvertPending(string activationKeys)
        {
            foreach (string line in File.ReadAllLines(activationKeys))
            {
                var ak = ActivationKey.Decode(line);
                var pub = ak.PrivateKey.PublicKey;
                Console.WriteLine($"{ByteUtil.Hex(pub.Format(true))}/{ByteUtil.Hex(pub.ToAddress().ToByteArray())}");
            }
        }

        [Command(Description = "Create activation keys")]
        public void CreateActivationKeys(
            [Argument(Description = "Number of keys to create")] int keyCount
        )
        {
            Utils.CreateActivationKey(
                out List<PendingActivationState> _,
                out List<ActivationKey> activationKeys,
                (uint)keyCount);
            foreach (ActivationKey keys in activationKeys)
            {
                Console.WriteLine(keys.Encode());
            }
        }

        [Command(Description = "Create a new genesis block.")]
        public void Create(
            [Option("private-key", new[]{ 'k' }, Description = "Hex encoded private key for gensis block")]
            string privateKeyHex,
            [Option('g', Description = "/path/to/nekoyume-unity/nekoyume/Assets/AddressableAssets/TableCSV")]
            string gameConfigDir,
            [Option('d', Description = "/path/to/nekoyume-unity/nekoyume/Assets/StreamingAssets/GoldDistribution.csv")]
            string goldDistributedPath,
            [Option('a', Description = "Number of activation keys to generate")]
            uint activationKeyCount,
            [Option("adminStateConfig", Description = "Config path to create AdminState")]
            string adminStateConfigPath,
            [Option("activatedAccountsList", Description = "List of accounts to be activated")]
            string activatedAccountsListPath = null,
            [Option('m', Description = "Config path to create AuthorizedMinersState")]
            string authorizedMinerConfigPath = null,
            [Option('c', Description = "Path of a plain text file containing names for credits.")]
            string creditsPath = null,
            [Option("pending-activations", new[] { 'p' }, Description = "Path of a plain text file containing pending activations")]
            string pendingActivationsPath = null
        )
        {
            List<PendingActivationState> pendingActivationStates;
            if (pendingActivationsPath is null)
            {
                Utils.CreateActivationKey(
                    out pendingActivationStates,
                    out List<ActivationKey> activationKeys,
                    activationKeyCount);
                ExportKeys(activationKeys, "keys.txt");
            }
            else
            {
                pendingActivationStates = new List<PendingActivationState>();
                foreach (string line in File.ReadAllLines(pendingActivationsPath))
                {
                    var parts = line.Split("/");
                    pendingActivationStates.Add(new PendingActivationState(
                        ByteUtil.ParseHex(parts[1]),
                        new PublicKey(ByteUtil.ParseHex(parts[0]))
                    ));
                }
            }

            Dictionary<string, string> tableSheets = Utils.ImportSheets(gameConfigDir);
            GoldDistribution[] goldDistributions = GoldDistribution
                .LoadInDescendingEndBlockOrder(goldDistributedPath);

            AdminState adminState = Utils.GetAdminState(adminStateConfigPath);

            AuthorizedMinersState authorizedMinersState = null;
            if (!(authorizedMinerConfigPath is null))
            {
                authorizedMinersState = Utils.GetAuthorizedMinersState(authorizedMinerConfigPath);
            }

            var activatedAccounts = activatedAccountsListPath is null
                ? ImmutableHashSet<Address>.Empty
                : Utils.GetActivatedAccounts(activatedAccountsListPath);

            Block<PolymorphicAction<ActionBase>> block = BlockHelper.MineGenesisBlock(
                tableSheets,
                goldDistributions,
                pendingActivationStates.ToArray(),
                adminState,
                authorizedMinersState: authorizedMinersState,
                activatedAccounts: activatedAccounts,
                isActivateAdminAddress: activationKeyCount != 0,
                credits: creditsPath is null ? null : File.ReadLines(creditsPath),
                privateKey: new PrivateKey(ByteUtil.ParseHex(privateKeyHex))
            );

            ExportBlock(block, "genesis-block");
        }

        private static void ExportBlock(Block<PolymorphicAction<ActionBase>> block, string path)
        {
            byte[] encoded = block.Serialize();
            File.WriteAllBytes(path, encoded);
        }

        private static void ExportKeys(List<ActivationKey> keys, string path)
        {
            File.WriteAllLines(path, keys.Select(v => v.Encode()));
        }
    }
}
