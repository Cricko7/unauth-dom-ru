namespace DoomRu.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? phone { get; set; }
        public bool ConsentAnalitics { get; set; }
        public bool ConsentCookie { get; set; }
        public string? ConsentVersion { get; set; }
    }
    public class Ai
    {
        public int Id { get; set; }

    }
}
