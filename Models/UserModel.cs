namespace lost_and_found.Models;

public class UserModels
{
    public required int userId { get; set; }
    public required string name { get; set; }
    public required string email { get; set; }
    public required string role { get; set; }

    public required string token { get; set; }

    public override string ToString()
    {
        return $"{userId}-{name}-{email}-{role}";
    }
}
