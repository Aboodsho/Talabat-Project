using Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class OrderShippingAddress:BaseEntity
    {
        public Guid OrderId { get; private set; }
        public string Street { get; private set; }=default!;
        public string City { get; private set; } = default!;
        public string? State { get; private set; }
        public string? PostalCode { get; private set; }
        public string Country { get; private set; } = default!;


        private OrderShippingAddress() { }

        internal OrderShippingAddress(Guid orderId, string street, string city, string? state, string? postalCode, string country)
        {
            OrderId = orderId;
            Update(street, city, state, postalCode, country);

        }
        internal void Update(string street, string city, string? state, string? postalCode, string country)
        {
            if(string.IsNullOrWhiteSpace(street))
                throw new DomainException("shipping_address.street_required", "Street is required.");
            if (string.IsNullOrWhiteSpace(city))
                throw new DomainException("shipping_address.city_required", "City is required.");
            
            if(string.IsNullOrWhiteSpace(country))
                throw new DomainException("shipping_address.country_required", "Country is required.");

            Street = street;
            City = city;
            State = state;
            PostalCode = postalCode;
            Country = country;
            MarkAsUpdated();
        }
    }
}
