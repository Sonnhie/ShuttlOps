namespace ShuttlOps.DTOs
{
    public class ReusableTableDTO
    {
        public string TableId { get; set; } = null!;
        public List<string> TableHeaders { get; set; } = new();
    }
}
