using BallCom.Logistics.API.Models;

namespace BallCom.Logistics.API.Services
{
    public class CarrierStatusProvider
    {
        public string FetchDeliveryStatus(Shipment shipment)
        {
            var age = DateTime.UtcNow - shipment.CreatedAt;

            if (age.TotalMinutes < 1)
            {
                return ShipmentStatus.Created;
            }

            if (age.TotalMinutes < 5)
            {
                return ShipmentStatus.InTransit;
            }

            return ShipmentStatus.Delivered;
        }
    }
}
