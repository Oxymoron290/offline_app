namespace BlazorPWA.Shared;

public static class Constants
{
    public const string MediaQueueName = "media-processing";
    public const string MediaContainerName = "media";
    public const string PhotosFolder = "photos";
    public const string VideosFolder = "videos";
    public const string DocumentsFolder = "documents";
    public const int MaxRetryCount = 3;
    public const int SyncBatchSize = 50;
    public const int MaxPhotoSizeMb = 10;
    public const int MaxVideoSizeMb = 100;
    public const int MaxDocumentSizeMb = 25;

    public static class ApiRoutes
    {
        public const string SyncOperations = "/api/sync";
        public const string Reports = "/api/reports";
        public const string Media = "/api/media";
        public const string Conflicts = "/api/conflicts";
        public const string Health = "/api/health";
    }

    public static class IndexedDbStores
    {
        public const string Entities = "entities";
        public const string SyncQueue = "syncQueue";
        public const string MediaBlobs = "mediaBlobs";
        public const string Config = "config";
    }
}
