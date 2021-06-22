using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace PoC_client
{
    public class PathFiltrator
    {
        private object lockObject = new object();
        private List<string> currentBuckets = new List<string>();

        public bool IsPathPertinent(string folderPath)
        {
            lock (lockObject)
            {
                if (FoldersToIgnore.Any(f => folderPath.Contains(f)))
                {
                    return false;
                }

                if (currentBuckets.Any(b => folderPath.Contains(b)))
                {
                    return true;
                }

                return false;
            }
        }

        public IList<string> FoldersToIgnore { get; private set; } = new List<string>();

        public void UpdateCurrentBuckets(List<string> buckets)
        {
            lock (lockObject)
            {
                currentBuckets = buckets;
            }
        }
    }
}
