using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.Rune;
using Lib9c.Module;

namespace Lib9c.Model.State
{
    /// <summary>
    /// This is new version of rune state. This state stores all rune states of an avatar.
    /// AllRuneState has all RuneStates as dictionary and has methods to get/set/update each RuneState.
    /// Use this with <see cref="RuneStateModule"/>.
    /// </summary>
    public class AllRuneState : IState
    {
        public Dictionary<int, RuneState> Runes { get; }

        public AllRuneState()
        {
            Runes = new Dictionary<int, RuneState>();
        }

        public AllRuneState(int runeId, int level = 0)
        {
            Runes = new Dictionary<int, RuneState>
            {
                { runeId, new RuneState(runeId, level) }
            };
        }

        public AllRuneState(List serialized)
        {
            Runes = new Dictionary<int, RuneState>();
            foreach (var item in serialized.OfType<List>())
            {
                var runeState = new RuneState(item);
                Runes.Add(runeState.RuneId, runeState);
            }
        }

        public bool TryGetRuneState(int runeId, out RuneState runeState)
        {
            runeState = Runes.TryGetValue(runeId, out var rs) ? rs : null;
            return runeState is not null;
        }

        public RuneState GetRuneState(int runeId)
        {
            return Runes.TryGetValue(runeId, out var runeState)
                ? runeState
                : throw new RuneNotFoundException($"Rune {runeId} not found in AllRuneState");
        }


        public void AddRuneState(int runeId, int level = 0)
        {
            if (Runes.ContainsKey(runeId))
            {
                throw new DuplicatedRuneIdException($"Rune ID {runeId} already exists");
            }

            Runes[runeId] = new RuneState(runeId, level);
        }

        public void AddRuneState(RuneState runeState)
        {
            if (Runes.ContainsKey(runeState.RuneId))
            {
                throw new DuplicatedRuneIdException($"Rune ID {runeState.RuneId} already exists");
            }

            Runes[runeState.RuneId] = runeState;
        }

        public void SetRuneState(int runeId, int level)
        {
            if (!Runes.ContainsKey(runeId))
            {
                throw new RuneNotFoundException($"Rune ID {runeId} not exists.");
            }

            var rune = Runes[runeId];
            rune.LevelUp(level - rune.Level);
        }

        public void SetRuneState(RuneState runeState)
        {
            if (!Runes.ContainsKey(runeState.RuneId))
            {
                throw new RuneNotFoundException($"Rune ID {runeState.RuneId} not exists.");
            }

            Runes[runeState.RuneId] = runeState;
        }

        public IValue Serialize()
        {
            return Runes.OrderBy(r => r.Key).Aggregate(
                List.Empty,
                (current, rune) => current.Add(rune.Value.Serialize())
            );
        }
    }
}
