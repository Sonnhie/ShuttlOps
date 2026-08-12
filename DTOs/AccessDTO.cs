namespace ShuttlOps.DTOs
{
    public class AccessDTO
    {
        public int Permission_id { get; set; }
        public int Role_id { get; set; }
        public int Module_id { get; set; }
        public string Role_name { get; set; } = null!;
        public string Module_name { get; set; } = null!;
        public bool Can_view { get; set; }
        public bool Can_edit { get; set; }
        public bool Can_delete { get; set; }
        public bool Can_approve { get; set; }
        public bool Can_create { get; set; }
        public bool SelectAll { get; set; }
    }

    public class AccessPayloadDTO
    {
        public int id { get; set; }
        public string action { get; set; } = null!;
        public bool isActive { get; set; }
    }

}
