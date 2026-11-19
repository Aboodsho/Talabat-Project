using Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Category:BaseEntity
    {
        public string Name { get; private set; }=default!;
        public string? Description { get; private set; }
        public DateTime? DeletedAt { get; private set; }
        public bool IsDeleted => DeletedAt is not null; // return true if DeletedAt has value , otherwise false

        //----------------------------------------------------------------

        private Category() { }
        public Category(string name, string? description = null)
        {
            Name = name;
            Description = description;
        }

        //----------------------------------------------------------------

        public void SetName(string name)
        {
            if(string.IsNullOrEmpty(name))
                throw new DomainException("Category.Name_Required","Category Name Is Required.");
            Name = name.Trim();
            MarkAsUpdated();
        }

        public void updateDescription(string? description)
        {
            Description = description;
            MarkAsUpdated();
        }
         public void Delete()
        {
            if (IsDeleted) return;
            DeletedAt = DateTime.UtcNow;

        }

        public void Restore()
        {
            DeletedAt = null;
            MarkAsUpdated();
        }
    }
}
