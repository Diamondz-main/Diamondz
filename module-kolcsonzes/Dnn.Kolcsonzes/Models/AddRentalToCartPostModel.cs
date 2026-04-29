namespace DnnKolcsonzes.Models
{
    public class AddRentalToCartPostModel
    {
        public string ProductBvin { get; set; }
        public string Sku { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
}