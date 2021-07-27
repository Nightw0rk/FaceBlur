using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FaceBlurShared.Models
{
    public enum FaceBlurItemStatusEnum
    {
        Idly,
        Downloading,
        Recognizing,
        Bluring,
        Error,
        Done
    }
    public class FaceBlurItem
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public FaceBlurItemStatusEnum status { get; set; }
        public string originUrl { get; set; }
        public long processTime { get; set; }
        public string publicUrl { get; set; }
        public string errorMessage { get; set; }

        public string hash { get; set; }

    }
}
