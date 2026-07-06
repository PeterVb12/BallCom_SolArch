using BallCom.CustomerService.API.Data;
using BallCom.CustomerService.API.Models.Events;
using BallCom.CustomerService.API.Models;

namespace BallCom.CustomerService.API.Services
{
    public class CustomerImportService
    {
        private readonly CustomerServiceDbContext _context;
        private readonly IEventPublisher _bus;

        public CustomerImportService(CustomerServiceDbContext context, IEventPublisher bus)
        {
            _context = context;
            _bus = bus;
        }

        public void SyncExternalCustomers()
        {
            string filePath = "fake_customer_data_export.csv";

            if (!File.Exists(filePath)) return;

            var lines = File.ReadAllLines(filePath);

            var csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            foreach (var line in lines.Skip(1)) 
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] columns = csvParser.Split(line);

                var companyName = columns[0].Trim('"');
                var firstName = columns[1].Trim('"');
                var lastName = columns[2].Trim('"');
                var phone = columns[3].Trim('"');
                var address = columns[4].Trim('"');

                if (_context.Customers.Any(c => c.FirstName == firstName && c.LastName == lastName))
                {
                    continue;
                }

                var newCustomer = new Customer
                {
                    Id = Guid.NewGuid(),
                    CompanyName = companyName,
                    FirstName = firstName,
                    LastName = lastName,
                    PhoneNumber = phone,
                    Address = address
                };

                _context.Customers.Add(newCustomer);
                _context.SaveChanges();

                _bus.Publish(new CustomerCreatedEvent
                {
                    CustomerId = newCustomer.Id,
                    FirstName = newCustomer.FirstName,
                    LastName = newCustomer.LastName,
                    CompanyName = newCustomer.CompanyName,
                    Address = newCustomer.Address
                });
            }
        }
    }
}
