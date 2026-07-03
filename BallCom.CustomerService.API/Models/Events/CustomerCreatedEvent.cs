namespace BallCom.CustomerService.API.Models.Events
{
    public class CustomerCreatedEvent
    {
        public Guid CustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CompanyName { get; set; }
        public string Address { get; set; }
    }
}
