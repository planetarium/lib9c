using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Mail
{
    [Serializable]
    public class MailBox : IEnumerable<Mail>, IState
    {
        private List<Mail> _mails = new List<Mail>();

        public int Count => _mails.Count;

        public Mail this[int idx] => _mails[idx];

        public MailBox()
        {
        }

        public MailBox(List serialized) : this()
        {
            _mails = serialized.Select(
                d => Mail.Deserialize((Dictionary)d)
            ).ToList();
        }

        public IEnumerator<Mail> GetEnumerator()
        {
            return _mails.OrderByDescending(i => i.blockIndex).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(Mail mail)
        {
            _mails.Add(mail);
        }

        [Obsolete("Use CleanUp")]
        public void CleanUpV1()
        {
            if (_mails.Count > 30)
            {
                _mails = _mails.OrderByDescending(m => m.blockIndex).Take(30).ToList();
            }
        }

        [Obsolete("Use CleanUp")]
        public void CleanUpV2()
        {
            if (_mails.Count > 30)
            {
                _mails = _mails
                    .OrderByDescending(m => m.blockIndex)
                    .ThenBy(m => m.id)
                    .Take(30)
                    .ToList();
            }
        }

        [Obsolete("No longer in use.")]
        public void CleanUpTemp(long blockIndex)
        {
            _mails = _mails
                .Where(m => m.requiredBlockIndex >= blockIndex)
                .ToList();
        }

        public void Remove(Mail mail)
        {
            _mails.Remove(mail);
        }

        public IValue Serialize() => new List(_mails
            .OrderBy(i => i.id)
            .Select(m => m.Serialize()));
    }
}
