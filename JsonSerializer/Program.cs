using Serializer.Attributes;

namespace Serializer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // --------------- serialization ----------------
            Employee employee1 = new Employee("Abdullah", 20_000.43f, null, "Dammam");
            Employee employee2 = new Employee("Ali", 40_000.50f, "Manager", "Riyadh");
            Employee employee3 = new Employee("Mohammed", 14_500.75f, "Developer", "Jeddah");
            Employee? employee4 = null;

            Employee?[] employeesToSerialize = { employee1, employee2, employee3, employee4 };
            await JsonSerializer.SerializeAsync(employeesToSerialize, "employees.json");

            // --------------- deserialization ----------------
            List<Employee>? deserializedEmployees = await JsonSerializer.DeserializeListAsync<Employee>("employees.json");
            foreach (Employee emp in deserializedEmployees!)
            {
                Console.Write(emp + "\n\n-----------------------------\n\n");
            }
        }
    }

    public class Employee
    {
        // default: private fields and properties are not serialized, public fields and properties are serialized
        public string? EmployeeName { get; set; }
        public AddressNested? EmployeeAddress { get; set; }
        public string? Position { get; set; }
        private string? _carModel = "Elantra";
        public string? Nationality = "Saudi";

        [JsonPropertyName("Wage")] // will override the property name 
        public float Salary { get; set; }

        [JsonInclude] // will include the private property, even though it is ignored by default
        private bool IsActive { get; set; } 

        [JsonIgnore] // will ignore the public property, even though it is included by default
        public int Experience { get; set; } = 1; 



        public Employee(string? name, float salary, string? position, string? City)
        {
            EmployeeName = name;
            Salary = salary;
            Position = position;
            EmployeeAddress = new AddressNested(City, "st, 64", 12345);
        }

        public void Promote(string newPosition)
        {
        }

        public override string ToString()
        {
            Position = Position == null ? "null" : Position;
            return $"--(Employee)--\nEmployeeName: {EmployeeName}\nSalary: {Salary}\nPosition: {Position}\nIsActive: {IsActive}\nNationality: {Nationality}\nCarModel: {_carModel}\n" +
                $"\n--(Nested Employee Address)--{EmployeeAddress}" +
                $"\n\n--(Nested City Info)--\n{EmployeeAddress.CityInfo}";
        }
    }

    public class AddressNested
    {
        public string? City { get; set; }
        public string? Street { get; set; }
        public int ZipCode { get; set; }

        public CityInfoNested? CityInfo { get; set; }
        public AddressNested(string? city, string? street, int zipCode)
        {
            City = city;
            Street = street;
            ZipCode = zipCode;
            CityInfo = new CityInfoNested();
        }

        public override string ToString()
        {
            return $"\nCity: {City}\nStreet: {Street}\nZipCode: {ZipCode}";
        }
    }


    public class CityInfoNested
    {
        public string? CityDescription { get; set; }

        public CityInfoNested()
        {
            CityDescription = "a city, in Saudi Arabia";
        }
        public override string ToString()
        {
            return $"CityDescription: {CityDescription}";
        }
    }
}