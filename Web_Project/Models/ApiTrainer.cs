using System.Text.Json.Serialization;

namespace Web_Project.Models
{
    public class ApiTrainer
    {
        [JsonPropertyName("trainerID")]
        public int TrainerID { get; set; }

        [JsonPropertyName("personID")]
        public int PersonID { get; set; }

        [JsonPropertyName("person")]
        public ApiPerson? Person { get; set; }

        [JsonPropertyName("expertiseAreas")]
        public string? ExpertiseAreas { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("skills")]
        public List<ApiTrainerSkill>? Skills { get; set; }
    }

    public class ApiPerson
    {
        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    // --- UPDATED CLASS ---
    public class ApiTrainerSkill
    {
        // API sends "id" (from your TrainerSkill.cs 'Id' property)
        [JsonPropertyName("id")]
        public int TrainerSkillID { get; set; }

        // API sends "serviceId" (from your TrainerSkill.cs 'ServiceId' property)
        [JsonPropertyName("serviceId")]
        public int ServiceID { get; set; }

        // API sends "service" (lowercase 's' from your virtual property)
        [JsonPropertyName("service")]
        public ServiceDTO? Service { get; set; }
    }
}