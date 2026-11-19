using Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Review:BaseEntity
    {

        public Guid ProductId { get; private set; }
        public Guid CustomerId { get; private set; }
        public int Rating { get; private set; }
        public string? Comment { get; private set; }

        private Review() { } 

        public Review(Guid productId, Guid customerId, int rating, string? comment = null)
        {
            ProductId = productId;
            CustomerId = customerId;
            SetRating(rating);
            Comment = comment;
        }

        public void SetRating(int rating)
        {
            if (rating is < 1 or > 5)
                throw new DomainException("review.invalid_rating", "Rating must be between 1 and 5.");

            Rating = rating;
            MarkAsUpdated();
        }

        public void UpdateComment(string? comment)
        {
            Comment = comment;
            MarkAsUpdated();
        }
    }
}
