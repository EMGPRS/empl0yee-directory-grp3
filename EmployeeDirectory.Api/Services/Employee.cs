namespace EmployeeDirectory.Api.Services
{
    public enum Gender
    {
        Male = 1,
        Female = 2,
    }
    public class Employee
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public Gender Gender { get; set; }
    }
}
