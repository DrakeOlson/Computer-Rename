namespace ComputerName
{
    public class ComputerNameRequest
    {
        public required string BlobContainerName { get; set; }
        public required string BlobURL { get; set; }
        public required string CSV { get; set; }
        public required string SerialNumber { get; set; }
    }
}
