namespace ShuttlOps.DTOs
{
    public class ReusableModalDTO
    {
        public string ModalId { get; set; } = null!;
        public string ModalTitle { get; set; } = null!;
        public string ModalIcon { get; set; } = null!;
        public string FormId { get; set; } = null!;
        public string SubmitButtonText { get; set; } = null!;
        public string BodyPartialName { get; set; } = null!;
    }
}
