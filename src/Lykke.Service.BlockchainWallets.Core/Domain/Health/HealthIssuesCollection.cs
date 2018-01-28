using System.Collections;
using System.Collections.Generic;

namespace Lykke.Service.BlockchainWallets.Core.Domain.Health
{
    public class HealthIssuesCollection : IReadOnlyCollection<HealthIssue>
    {
        private readonly List<HealthIssue> _list;

        public HealthIssuesCollection()
        {
            _list = new List<HealthIssue>();
        }

        public void Add(string type, string value)
        {
            _list.Add(HealthIssue.Create(type, value));
        }

        public int Count => _list.Count;

        public IEnumerator<HealthIssue> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
