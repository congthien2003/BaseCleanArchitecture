using BaseCleanArchitecture.Application.Features.Categories.Models;
using MediatR;

namespace BaseCleanArchitecture.Application.Features.Categories.Commands
{
    public class CreateCategoryCommandRequest : IRequest<CreateCategoryResponse>
    {
        public CreateCategoryRequest CreateCategoryRequest { get; set; }

        public CreateCategoryCommandRequest(CreateCategoryRequest createCategoryRequest)
        {
            CreateCategoryRequest = createCategoryRequest;
        }
    }

    public class CreateCategoryCommandRequestHandler : IRequestHandler<CreateCategoryCommandRequest, CreateCategoryResponse>
    {
        public Task<CreateCategoryResponse> Handle(CreateCategoryCommandRequest request, CancellationToken cancellationToken)
        {
            // Here you would typically interact with your database or other services to create the category
            // For demonstration purposes, we'll return a dummy response
            var response = new CreateCategoryResponse(Guid.NewGuid(), "Sample Category");
            return Task.FromResult(response);
        }
    }
}
