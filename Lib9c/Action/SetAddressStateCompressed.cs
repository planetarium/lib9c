using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action
{
    /// <summary>
    /// Compressed version of SetAddressState to reduce transaction size for large state data.
    /// Uses GZip compression to minimize the size of state data in transactions.
    ///
    /// This action is particularly useful when setting large state data that would exceed
    /// transaction policy size limits. The compression can reduce state data size by 60-80%
    /// depending on the content structure and redundancy.
    ///
    /// Example usage:
    /// <code>
    /// var states = new List&lt;(Address, Address, IValue)&gt;
    /// {
    ///     (accountAddress, targetAddress, largeStateValue),
    /// };
    /// var compressedAction = new SetAddressStateCompressed(states);
    /// </code>
    /// </summary>
    [ActionType(TypeId)]
    public class SetAddressStateCompressed : ActionBase
    {
        /// <summary>
        /// The type identifier of the action.
        /// </summary>
        public const string TypeId = "set_address_state_compressed";

        /// <summary>
        /// The operator address that has special permission to execute this action.
        /// When the action is signed by this operator, permission check is skipped.
        /// </summary>
        private static readonly Address Operator = PatchTableSheet.Operator;

        /// <summary>
        /// List of tuples (account address, target address, compressed state data) to set state for.
        /// The state data is compressed using GZip compression.
        /// </summary>
        public IReadOnlyList<(Address accountAddress, Address targetAddress, byte[] compressedState)> CompressedStates { get; private set; }


        /// <summary>
        /// Default constructor.
        /// </summary>
        public SetAddressStateCompressed()
        {
        }

        /// <summary>
        /// Constructor that initializes with a list of states to set.
        /// The states will be automatically compressed using GZip compression.
        /// </summary>
        /// <param name="states">List of tuples to set state for</param>
        /// <exception cref="ArgumentNullException">Thrown if states is null or any state value is null</exception>
        /// <exception cref="ArgumentException">Thrown if states list is empty</exception>
        public SetAddressStateCompressed(IReadOnlyList<(Address accountAddress, Address targetAddress, IValue state)> states)
        {
            if (states == null)
            {
                throw new ArgumentNullException(nameof(states), "States list cannot be null.");
            }

            if (states.Count == 0)
            {
                throw new ArgumentException("States list cannot be empty.", nameof(states));
            }

            if (states.Any(s => s.state == null))
            {
                throw new ArgumentNullException(nameof(states), "State values cannot be null.");
            }

            try
            {
                CompressedStates = states.Select(s => (s.accountAddress, s.targetAddress, CompressState(s.state))).ToList();
            }
            catch (Exception ex) when (!(ex is ArgumentNullException))
            {
                throw new InvalidOperationException("Failed to compress state data.", ex);
            }
        }

        /// <summary>
        /// Constructor that initializes with already compressed state data.
        /// </summary>
        /// <param name="compressedStates">List of tuples with compressed state data</param>
        /// <exception cref="ArgumentNullException">Thrown if compressedStates is null or any compressed state is null</exception>
        /// <exception cref="ArgumentException">Thrown if compressedStates list is empty</exception>
        public SetAddressStateCompressed(IReadOnlyList<(Address accountAddress, Address targetAddress, byte[] compressedState)> compressedStates)
        {
            if (compressedStates == null)
            {
                throw new ArgumentNullException(nameof(compressedStates), "Compressed states list cannot be null.");
            }

            if (compressedStates.Count == 0)
            {
                throw new ArgumentException("Compressed states list cannot be empty.", nameof(compressedStates));
            }

            if (compressedStates.Any(s => s.compressedState == null))
            {
                throw new ArgumentNullException(nameof(compressedStates), "Compressed state data cannot be null.");
            }

            if (compressedStates.Any(s => s.compressedState.Length == 0))
            {
                throw new ArgumentException("Compressed state data cannot be empty.", nameof(compressedStates));
            }

            CompressedStates = compressedStates;
        }

        /// <inheritdoc />
        public override IValue PlainValue =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                { (Text)"type_id", (Text)TypeId },
                {
                    (Text)"values", Dictionary.Empty
                    .Add("s", new List(CompressedStates.Select(s =>
                        List.Empty
                            .Add(s.accountAddress.Serialize())
                            .Add(s.targetAddress.Serialize())
                            .Add(new Binary(s.compressedState)))))
                }
            });

        /// <inheritdoc />
        public override void LoadPlainValue(IValue plainValue)
        {
            if (plainValue == null)
            {
                throw new ArgumentNullException(nameof(plainValue), "Plain value cannot be null.");
            }

            try
            {
                var dictionary = (Dictionary)plainValue;
                if (!dictionary.ContainsKey("values"))
                {
                    throw new ArgumentException("Invalid plain value format: missing 'values' key.", nameof(plainValue));
                }

                var values = (Dictionary)dictionary["values"];
                if (!values.ContainsKey("s"))
                {
                    throw new ArgumentException("Invalid plain value format: missing 's' key in values.", nameof(plainValue));
                }

                var statesList = (List)values["s"];
                if (statesList.Count == 0)
                {
                    throw new ArgumentException("States list cannot be empty.", nameof(plainValue));
                }

                var states = statesList
                    .Select(s =>
                    {
                        if (!(s is List stateList) || stateList.Count != 3)
                        {
                            throw new ArgumentException("Invalid state format: expected list with 3 elements.", nameof(plainValue));
                        }

                        var compressedState = ((Binary)stateList[2]).ByteArray.ToArray();
                        if (compressedState == null || compressedState.Length == 0)
                        {
                            throw new ArgumentException("Compressed state data cannot be null or empty.", nameof(plainValue));
                        }

                        return (stateList[0].ToAddress(), stateList[1].ToAddress(), compressedState);
                    })
                    .ToList();

                CompressedStates = states;
            }
            catch (Exception ex) when (!(ex is ArgumentNullException || ex is ArgumentException))
            {
                throw new InvalidOperationException("Failed to load plain value.", ex);
            }
        }

        /// <summary>
        /// Executes the action to set state for the specified addresses.
        /// Throws an exception if the state already exists for the target address.
        /// </summary>
        /// <param name="context">Action context</param>
        /// <returns>The updated world state</returns>
        /// <exception cref="ArgumentNullException">Thrown if context is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if the state already exists for the target address</exception>
        /// <exception cref="InvalidDataException">Thrown when the compressed data is corrupted or invalid</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the signer does not have permission to execute this action</exception>
        public override IWorld Execute(IActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), "Action context cannot be null.");
            }

            if (CompressedStates == null || CompressedStates.Count == 0)
            {
                throw new InvalidOperationException("No compressed states to execute.");
            }

            GasTracer.UseGas(1);
            var states = context.PreviousState;

            try
            {
#if !LIB9C_DEV_EXTENSIONS && !UNITY_EDITOR
                if (context.Signer == Operator)
                {
                    Log.Information(
                        "Skip CheckPermission since {TxId} had been signed by the operator({Operator}).",
                        context.TxId,
                        Operator
                    );
                }
                else
                {
                    CheckPermission(context);
                }
#else
                CheckPermission(context);
#endif
            }
            catch (Exception ex) when (!(ex is ArgumentNullException))
            {
                throw new UnauthorizedAccessException("Permission denied to execute this action.", ex);
            }

            foreach (var (accountAddress, targetAddress, compressedState) in CompressedStates)
            {
                try
                {
                    var account = states.GetAccount(accountAddress);
                    var existingState = account.GetState(targetAddress);
                    if (existingState is not null)
                    {
                        throw new InvalidOperationException($"State already exists at address {targetAddress}");
                    }

                    // Decompress the state data
                    var state = DecompressState(compressedState);
                    account = account.SetState(targetAddress, state);
                    states = states.SetAccount(accountAddress, account);
                }
                catch (InvalidDataException)
                {
                    throw; // Re-throw decompression errors as-is
                }
                catch (Exception ex) when (!(ex is InvalidOperationException))
                {
                    throw new InvalidOperationException($"Failed to set state for address {targetAddress}", ex);
                }
            }

            return states;
        }

        /// <summary>
        /// Compresses state data using GZip compression.
        ///
        /// This method is used to prepare state data for the CompressedStates property.
        /// GZip compression typically reduces state data size by 60-80% depending on content.
        ///
        /// Example:
        /// <code>
        /// var state = (Text)"large state data";
        /// var compressedData = SetAddressStateCompressed.CompressState(state);
        /// // compressedData can now be used in CompressedStates property
        /// </code>
        /// </summary>
        /// <param name="state">The state data to compress. Must not be null.</param>
        /// <returns>Compressed byte array using GZip compression.</returns>
        /// <exception cref="ArgumentNullException">Thrown when state is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when compression fails.</exception>
        public static byte[] CompressState(IValue state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state), "State cannot be null.");

            try
            {
                // Serialize the state to bytes using Bencodex
                var stateBytes = new Codec().Encode(state);
                if (stateBytes == null || stateBytes.Length == 0)
                {
                    throw new InvalidOperationException("Failed to encode state to bytes.");
                }

                using var memoryStream = new MemoryStream();
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    gzipStream.Write(stateBytes, 0, stateBytes.Length);
                }

                var compressedData = memoryStream.ToArray();
                if (compressedData == null || compressedData.Length == 0)
                {
                    throw new InvalidOperationException("Compression resulted in empty data.");
                }

                return compressedData;
            }
            catch (Exception ex) when (!(ex is ArgumentNullException || ex is InvalidOperationException))
            {
                throw new InvalidOperationException("Failed to compress state data.", ex);
            }
        }

        /// <summary>
        /// Decompresses state data using GZip compression.
        ///
        /// This method is used internally to restore the original state data
        /// from the compressed byte array stored in CompressedStates.
        ///
        /// Example:
        /// <code>
        /// var originalState = SetAddressStateCompressed.DecompressState(compressedData);
        /// // originalState now contains the decompressed IValue
        /// </code>
        /// </summary>
        /// <param name="compressedData">The compressed data to decompress. Must not be null.</param>
        /// <returns>Decompressed IValue state data.</returns>
        /// <exception cref="ArgumentNullException">Thrown when compressedData is null.</exception>
        /// <exception cref="ArgumentException">Thrown when compressedData is empty.</exception>
        /// <exception cref="InvalidDataException">Thrown when the compressed data is corrupted or not valid GZip format.</exception>
        /// <exception cref="InvalidOperationException">Thrown when decompression fails.</exception>
        public static IValue DecompressState(byte[] compressedData)
        {
            if (compressedData == null)
                throw new ArgumentNullException(nameof(compressedData), "Compressed data cannot be null.");

            if (compressedData.Length == 0)
                throw new ArgumentException("Compressed data cannot be empty.", nameof(compressedData));

            try
            {
                using var memoryStream = new MemoryStream(compressedData);
                using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
                using var decompressedStream = new MemoryStream();
                gzipStream.CopyTo(decompressedStream);
                var decompressedBytes = decompressedStream.ToArray();

                if (decompressedBytes == null || decompressedBytes.Length == 0)
                {
                    throw new InvalidOperationException("Decompression resulted in empty data.");
                }

                // Deserialize back to IValue using Bencodex
                var result = new Codec().Decode(decompressedBytes);
                if (result == null)
                {
                    throw new InvalidOperationException("Failed to decode decompressed data to IValue.");
                }

                return result;
            }
            catch (InvalidDataException)
            {
                throw; // Re-throw GZip decompression errors as-is
            }
            catch (Exception ex) when (!(ex is ArgumentNullException || ex is ArgumentException || ex is InvalidOperationException))
            {
                throw new InvalidOperationException("Failed to decompress state data.", ex);
            }
        }
    }
}
