namespace AhDung.WebChecker.Models
{
    public class User
    {
        public string Name { get; set; }

        public string Password{ get; set; }

        public bool Enabled { get; set; } = true;
    }
}
