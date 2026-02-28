using Newtonsoft.Json;

namespace APICore.API.BasicResponses
{
    public class ApiOkPaginatedResponse : ApiResponse
    {
        public ApiOkPaginatedResponse(object result, object pagination)
            : base(200)
        {
            Result = result;
            Pagination = pagination;
        }

        public object Result { get; }
        public object Pagination { get; }
    }
}
