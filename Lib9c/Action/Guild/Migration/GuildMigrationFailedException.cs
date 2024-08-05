using System;

namespace Nekoyume.Action.Guild.Migration
{
    public class GuildMigrationFailedException : InvalidOperationException
    {
        public GuildMigrationFailedException(string message) : base(message)
        {
        }
    }
}
