using BallCom.Logistics.API.Models;

namespace BallCom.Logistics.API.Services
{
    public class CarrierSelectionService
    {
        public IReadOnlyList<CarrierQuote> GetAllowedCarrierQuotes()
        {
            return new List<CarrierQuote>
            {
                new("PostNL", 6.95m),
                new("DHL", 8.50m),
                new("DPD", 5.49m)
            };
        }

        public CarrierQuote SelectLowestCostCarrier()
        {
            return GetAllowedCarrierQuotes().OrderBy(q => q.Price).First();
        }
    }
}
