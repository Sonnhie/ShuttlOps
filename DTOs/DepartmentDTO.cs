namespace ShuttlOps.DTOs
{
    public class DepartmentDTO
    {
        public int Id { get; set; }
        public string DepartmentName { get; set; } = null!;
        public int? ManagerId { get; set; } = 0!;
        public int MembersCount { get; set;} = 0!;
    }
}
