namespace BaseCleanArchitecture.Application.Features.Categories.Models
{
    public class CreateCategoryResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public CreateCategoryResponse(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
