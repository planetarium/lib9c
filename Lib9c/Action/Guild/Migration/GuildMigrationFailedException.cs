using System;

namespace Nekoyume.Action.Guild.Migration
{
    /// <summary>
    /// An exception to be thrown when guild migration failed.
    /// </summary>
    public class GuildMigrationFailedException : InvalidOperationException
    {
        public GuildMigrationFailedException(string message) : base(message)
        {
        }
    }
}
