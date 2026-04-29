using System.Collections.Generic;

namespace DnnKolcsonzes.Models
{
    public class RentalProductViewModel
    {
        public RentalProductViewModel()
        {
            UnavailableRanges = new List<UnavailableDateRange>();
            Highlights = new List<RentalHighlightItem>();
        }

        public string ProductBvin { get; set; }
        public string Sku { get; set; }

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string DescriptionHtml { get; set; }
        public string ImageUrl { get; set; }

        public decimal DailyPrice { get; set; }
        public decimal DepositAmount { get; set; }

        public int MinRentalDays { get; set; }
        public int? MaxRentalDays { get; set; }
        public int PreparationDays { get; set; }

        public bool PickupAllowed { get; set; }
        public bool ShippingAllowed { get; set; }

        public string RentalNote { get; set; }

        public List<UnavailableDateRange> UnavailableRanges { get; set; }
        public List<RentalHighlightItem> Highlights { get; set; }
    }

    public class UnavailableDateRange
    {
        public string Start { get; set; }
        public string End { get; set; }
    }

    public class RentalHighlightItem
    {
        public string Title { get; set; }
        public string Text { get; set; }
    }
}