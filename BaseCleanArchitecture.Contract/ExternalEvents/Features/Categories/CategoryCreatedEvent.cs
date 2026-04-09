using BaseCleanArchitecture.Contract.ExternalEvents.Abstractions;

namespace BaseCleanArchitecture.Contract.ExternalEvents.Features.Categories
{
    public class CategoryCreatedEvent : BaseExternalEvent
    {
        public string CategoryName { get; set; }

        public string CategoryDescription { get; set; }

        public CategoryCreatedEvent(string categoryName, string categoryDescription) : base()
        {
            CategoryName = categoryName;
            CategoryDescription = categoryDescription;
        }
    }
}
