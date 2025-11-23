using System;
using System.Collections.Generic;

namespace WorldMapZoom
{
    public class Person
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public string FirstLastName { get; set; }
        public string SecondLastName { get; set; }
        public string NationalId { get; set; }
        public DateTime BirthDate { get; set; }
        public bool IsAlive { get; set; }
        public DateTime? DeathDate { get; set; }
        public int Age { get; set; }
        public string PhotoUrl { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; }

        // IDs de familiares relacionados
        public List<string> ParentIds { get; set; }
        public List<string> SpouseIds { get; set; }
        public List<string> ChildrenIds { get; set; }

        public Person()
        {
            Id = Guid.NewGuid().ToString();
            ParentIds = new List<string>();
            SpouseIds = new List<string>();
            ChildrenIds = new List<string>();
            IsAlive = true;
        }

        public string FullName
        {
            get
            {
                string name = FirstName;
                if (!string.IsNullOrWhiteSpace(SecondName))
                    name += " " + SecondName;
                name += " " + FirstLastName;
                if (!string.IsNullOrWhiteSpace(SecondLastName))
                    name += " " + SecondLastName;
                return name;
            }
        }

        public void CalculateAge()
        {
            DateTime referenceDate = IsAlive ? DateTime.Now : (DeathDate ?? DateTime.Now);
            int age = referenceDate.Year - BirthDate.Year;
            if (referenceDate < BirthDate.AddYears(age))
                age--;
            Age = age;
        }
    }
}