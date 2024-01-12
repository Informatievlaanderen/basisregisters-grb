namespace Grb.Building.Api.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using Query;

    public interface IPagedUriGenerator
    {
        public Uri FirstPage(string path);
        public Uri? NextPage<T>(IEnumerable<T> query, Pagination pagination, string path);
    }

    public class PagedUriGenerator : IPagedUriGenerator
    {
        private readonly Uri _baseUri;

        public PagedUriGenerator(IConfiguration configuration)
        {
            _baseUri = new Uri(configuration.GetValue<string>("BaseUrl"));
        }

        public Uri? NextPage<T>(IEnumerable<T> query, Pagination pagination, string path)
        {
            var hasNextPage = query
                .Skip(pagination.NextPageOffset)
                .Any();

            path = path.TrimEnd('/');

            return hasNextPage
                ? new Uri(_baseUri, $"{path}?offset={pagination.NextPageOffset}")
                : null;
        }

        public Uri FirstPage(string path)
        {
            return new Uri(_baseUri, $"{path.TrimEnd('/')}?offset=0");
        }
    }
}
