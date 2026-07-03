namespace BallCom.CustomerService.API.Models
{
    public class Customer
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Email { get; set; } 
    }
}
