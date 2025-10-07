using System;

namespace Lib9c.Model.Skill
{
    public class UnexpectedOperationException : Exception
    {
        public UnexpectedOperationException(string message) : base(message)
        {
        }
    }
}
