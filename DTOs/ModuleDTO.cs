namespace ShuttlOps.DTOs
{
    public class ModuleDTO
    {
        public int Id { get; set; }
        public string ModuleName { get; set; } = null!;
        public string ModuleDescription { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
