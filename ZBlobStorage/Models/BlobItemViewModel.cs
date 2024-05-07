using Azure.Storage.Blobs.Models;

namespace ZBlobStorage.Models
{
    public class BlobItemViewModel
    {
        public string Name { get; set; }

        public string Folderpath { get; set; }
        public bool IsFolder { get; set; }

        /// <summary>
        /// Deleted.
        /// </summary>
        public bool Deleted { get; internal set; }

        /// <summary>
        /// Snapshot.
        /// </summary>
        public string Snapshot { get; internal set; }

        /// <summary>
        /// VersionId.
        /// </summary>
        public string VersionId { get; internal set; }

        /// <summary>
        /// IsCurrentVersion.
        /// </summary>
        public bool? IsLatestVersion { get; internal set; }

        /// <summary>
        /// Properties of a blob.
        /// </summary>
        public BlobItemProperties Properties { get; internal set; }

        /// <summary>
        /// Metadata.
        /// </summary>
        public IDictionary<string, string> Metadata { get; internal set; }

        /// <summary>
        /// Tags.
        /// </summary>
        public IDictionary<string, string> Tags { get; internal set; }

        /// <summary>
        /// Object Replication Metadata (OrMetadata)
        /// </summary>
        public IList<ObjectReplicationPolicy> ObjectReplicationSourceProperties { get; internal set; }

        /// <summary>
        /// Indicates that this root blob has been deleted, but it has versions that are active.
        /// </summary>
        public bool? HasVersionsOnly { get; internal set; }
    }
}
