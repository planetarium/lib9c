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
        public const int MaxCount = 30;

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
            if (_mails.Count > MaxCount)
            {
                _mails = _mails.OrderByDescending(m => m.blockIndex).Take(MaxCount).ToList();
            }
        }

        [Obsolete("Use CleanUp")]
        public void CleanUpV2()
        {
            if (_mails.Count > MaxCount)
            {
                _mails = _mails
                    .OrderByDescending(m => m.blockIndex)
                    .ThenBy(m => m.id)
                    .Take(MaxCount)
                    .ToList();
            }
        }

        /// <param name="mailIdsThatShouldRemain"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void CleanUp(params Guid[] mailIdsThatShouldRemain)
        {
            if (_mails.Count <= MaxCount)
            {
                return;
            }

            var shouldRemainCount = mailIdsThatShouldRemain.Length;
            if (shouldRemainCount > MaxCount)
            {
                var message = $"{nameof(mailIdsThatShouldRemain)} count should less or equal to {MaxCount}";
                throw new ArgumentOutOfRangeException(
                    nameof(mailIdsThatShouldRemain),
                    shouldRemainCount,
                    message);
            }

            var mails = new List<Mail>();
            foreach (var mail in _mails
                .OrderByDescending(e => e.blockIndex)
                .ThenBy(e => e.id))
            {
                if (shouldRemainCount > 0 &&
                    mailIdsThatShouldRemain.Contains(mail.id))
                {
                    mails.Add(mail);
                    if (mails.Count == MaxCount)
                    {
                        break;
                    }

                    shouldRemainCount--;
                    continue;
                }

                if (mails.Count == MaxCount - shouldRemainCount)
                {
                    continue;
                }

                mails.Add(mail);
                if (mails.Count == MaxCount)
                {
                    break;
                }
            }

            _mails = mails;
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
