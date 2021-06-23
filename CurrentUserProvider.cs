using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

namespace PoC_client
{
    public interface ICurrentUserProvider
    {
        event EventHandler RootFolderPathChanged;

        string RootFolderPath { get; set; }

        LoginServiceModel LoggedUser { get; }

        void SetLoggedUser(LoginServiceModel model);

        void UpdateLoggedUserGroups(List<GroupServiceModel> groups);

        IBucketName TryExtractBucket(string fileFullPath);

        string ExtractPrefix(string fileFullPath);

        IBucketName GetBucketNameByDirectoryPath(string directoryPath);

        IList<string> ProvideBucketDirectoryPathes();

        IList<string> GetServerBuckets();
    }

    public interface IPathFiltrator
    {
        IList<string> FoldersToIgnore { get; }

        bool IsPathPertinent(string fullPath);

        void ReadFromSettings();

        void UpdateSubFoldersToIgnore(IList<string> pathes);

        void UpdateCurrentBuckets(List<string> pathes);
    }

    public class CurrentUserProvider : ICurrentUserProvider
    {
        [Import(typeof(IPathFiltrator))]
        public IPathFiltrator PathFiltrator { get; set; }

        public LoginServiceModel LoggedUser { get; private set; }

        private string rootFolderPath;

        public string RootFolderPath
        {
            get
            {
                return rootFolderPath;
            }
            set
            {
                rootFolderPath = value;
                TryCreateLocalBuckets();
                RootFolderPathChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler RootFolderPathChanged;

        public IBucketName TryExtractBucket(string fileFullPath)
        {
            if (string.IsNullOrEmpty(fileFullPath))
            {
                throw new ArgumentNullException(nameof(fileFullPath));
            }

            string localBucketName;

            var afterRootPath = fileFullPath.Substring(RootFolderPath.Length + 1, fileFullPath.Length - RootFolderPath.Length - 1);
            var dashIndex = afterRootPath.IndexOf('\\');

            localBucketName = dashIndex == -1 ? afterRootPath : afterRootPath.Substring(0, dashIndex);

            var group = LoggedUser.Groups.SingleOrDefault(x => x.Name.ToLowerInvariant() == localBucketName.ToLowerInvariant());

            if (group == null)
            {
                var currentGroups = string.Join(";", LoggedUser.Groups.Select(g => "id=" + g.Id + "name=" + g.Name));
                return new BucketName($"No group by name '{localBucketName}'. Local file path is '{fileFullPath}'. Current groups: {currentGroups}");
            }

            IBucketName result = new BucketName(GenerateBucketName(LoggedUser.TenantId, group.Id), localBucketName);

            return result;
        }

        // TODO 1.0 Unit tests
        public IBucketName GetBucketNameByDirectoryPath(string directoryPath)
        {
            var dashIndex = directoryPath.LastIndexOf('\\');

            var localBucketName = directoryPath.Substring(dashIndex + 1, directoryPath.Length - dashIndex - 1);

            var appropriateGroup = LoggedUser.Groups.SingleOrDefault(x => x.Name.ToLowerInvariant() == localBucketName.ToLowerInvariant());

            if (appropriateGroup == null)
            {
                return new BucketName($"Can't get group for local bucket {localBucketName}.");
            }

            var serverBucketName = GenerateBucketName(LoggedUser.TenantId, appropriateGroup.Id);

            return new BucketName(serverBucketName, localBucketName);
        }

        public IList<string> GetServerBuckets()
        {
            var result = new List<string>();

            foreach (var group in LoggedUser.Groups)
            {
                result.Add(GenerateBucketName(LoggedUser.TenantId, group.Id));
            }

            return result;
        }

        public string ExtractPrefix(string fileFullPath)
        {
            var bucket = TryExtractBucket(fileFullPath);

            if (!bucket.IsSuccess)
            {
                return null; // TODO 1.0
            }

            var bucketName = bucket.LocalName;

            var afterRootPath = fileFullPath.Substring(RootFolderPath.Length + 1, fileFullPath.Length - RootFolderPath.Length - 1);

            var relativeDirectoryPath = Path.GetDirectoryName(afterRootPath);

            if (bucketName == relativeDirectoryPath || bucketName == afterRootPath)
            {
                return string.Empty;
            }
            else
            {
                var prefix = relativeDirectoryPath.Substring(bucketName.Length + 1, relativeDirectoryPath.Length - bucketName.Length - 1);

                if (prefix == string.Empty)
                {
                    return string.Empty;
                }
                else
                {
                    var result = prefix.ToHexPrefix();
                    return result;
                }
            }
        }

        private string GenerateBucketName(string tenantId, string groupId)
        {
            var result = $"the-{tenantId}-{groupId}-res";

            return result;
        }

        public void SetLoggedUser(LoginServiceModel model)
        {
            LoggedUser = model;
            TryCreateLocalBuckets();
        }

        public void UpdateLoggedUserGroups(List<GroupServiceModel> groups)
        {
            if (LoggedUser == null)
            {
                throw new ArgumentNullException();
            }

            LoggedUser.Groups = groups;
            TryCreateLocalBuckets();
        }

        private bool TryCreateLocalBuckets()
        {
            PathFiltrator pathFiltrator = new PathFiltrator();
            if (string.IsNullOrEmpty(RootFolderPath))
            {
                return false;
            }

            if (LoggedUser == null)
            {
                return false;
            }

            var bucketDirectoryPathes = ProvideBucketDirectoryPathes();

            var existedBuckets = new List<string>();

            foreach (var bucket in bucketDirectoryPathes)
            {
                if (Directory.Exists(bucket))
                {
                    existedBuckets.Add(bucket);
                }
                else
                {
                    try
                    {
                        Directory.CreateDirectory(bucket);
                        existedBuckets.Add(bucket);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine("Can not create directory {0}. Please check access to parent folder.", bucket, "Attention");
                        return false;
                    }

                    Console.WriteLine($"Bucket '{bucket}' was created.");
                }
            }

            if (pathFiltrator != null) pathFiltrator.UpdateCurrentBuckets(existedBuckets);
            return true;
        }

        // TODO 1.0 dublicated.
        public IList<string> ProvideBucketDirectoryPathes()
        {
            var result = new List<string>();

            foreach (var group in LoggedUser.Groups)
            {
                var currentBucketDirectoryPath = Path.Combine(RootFolderPath, group.Name);
                result.Add(currentBucketDirectoryPath);
            }

            return result;
        }
    }
}