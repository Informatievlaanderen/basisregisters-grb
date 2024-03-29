namespace Grb.Building.Api.IntegrationTests
{
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.Auth.AcmIdm;
    using Xunit;

    public class IntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;

        public IntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory]
        [InlineData("/v2/uploads/jobs", Scopes.DvGrIngemetengebouwBeheer)]
        public async Task Post_ReturnsSuccess(string endpoint, string requiredScopes)
        {
            var client = _fixture.TestServer.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await _fixture.GetAccessToken(requiredScopes));

            var response = await client.PostAsync(endpoint,
                new StringContent("{}", Encoding.UTF8, "application/json"), CancellationToken.None);
            Assert.NotNull(response);
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [InlineData("/v2/uploads/jobs")]
        public async Task Post_ReturnsUnauthorized(string endpoint)
        {
            var client = _fixture.TestServer.CreateClient();

            var response = await client.PostAsync(endpoint,
                new StringContent("{}", Encoding.UTF8, "application/json"), CancellationToken.None);
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("/v2/uploads/jobs")]
        [InlineData("/v2/uploads/jobs", "dv_gr_geschetstgebouw_beheer")]
        public async Task Post_ReturnsForbidden(string endpoint, string scope = "")
        {
            var client = _fixture.TestServer.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await _fixture.GetAccessToken(scope));

            var response = await client.PostAsync(endpoint,
                new StringContent("{}", Encoding.UTF8, "application/json"), CancellationToken.None);
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [InlineData("/v2/uploads/jobs", Scopes.DvGrIngemetengebouwBeheer)]
        [InlineData("/v2/uploads/jobs/00000000-0000-0000-0000-000000000000", Scopes.DvGrIngemetengebouwBeheer)]
        [InlineData("/v2/uploads/jobs/00000000-0000-0000-0000-000000000000/results", Scopes.DvGrIngemetengebouwBeheer)]
        public async Task Get_ReturnsSuccess(string endpoint, string requiredScopes)
        {
            var client = _fixture.TestServer.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await _fixture.GetAccessToken(requiredScopes));

            var response = await client.GetAsync(endpoint, CancellationToken.None);
            Assert.NotNull(response);
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("/v2/uploads/jobs")]
        [InlineData("/v2/uploads/jobs/00000000-0000-0000-0000-000000000000")]
        [InlineData("/v2/uploads/jobs/00000000-0000-0000-0000-000000000000/results")]
        public async Task Get_ReturnsUnauthorized(string endpoint)
        {
            var client = _fixture.TestServer.CreateClient();

            var response = await client.GetAsync(endpoint, CancellationToken.None);
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("/v2/uploads/jobs")]
        [InlineData("/v2/uploads/jobs", "dv_gr_geschetstgebouw_beheer")]
        [InlineData("/v2/uploads/jobs/00000000-0000-0000-0000-000000000000")]
        [InlineData("/v2/uploads/jobs/00000000-0000-0000-0000-000000000000", "dv_gr_geschetstgebouw_beheer")]
        [InlineData("/v2/uploads/jobs/00000000-0000-0000-0000-000000000000/results")]
        [InlineData("/v2/uploads/jobs/00000000-0000-0000-0000-000000000000/results", "dv_gr_geschetstgebouw_beheer")]
        public async Task Get_ReturnsForbidden(string endpoint, string scope = "")
        {
            var client = _fixture.TestServer.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await _fixture.GetAccessToken(scope));

            var response = await client.GetAsync(endpoint, CancellationToken.None);
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [InlineData("/v2/uploads/jobs/00000000-0000-0000-0000-000000000000", Scopes.DvGrIngemetengebouwBeheer)]
        [InlineData("/v2/uploads/jobs/00000000-0000-0000-0000-000000000000/jobrecords/123", Scopes.DvGrIngemetengebouwUitzonderingen)]
        public async Task Delete_ReturnsSuccess(string endpoint, string requiredScopes)
        {
            var client = _fixture.TestServer.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await _fixture.GetAccessToken(requiredScopes));

            var response = await client.DeleteAsync(endpoint, CancellationToken.None);
            Assert.NotNull(response);
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [InlineData("/v2/uploads/jobs/00000000-0000-0000-0000-000000000000")]
        [InlineData("/v2/uploads/jobs/00000000-0000-0000-0000-000000000000/jobrecords/123")]
        public async Task Delete_ReturnsUnauthorized(string endpoint)
        {
            var client = _fixture.TestServer.CreateClient();

            var response = await client.DeleteAsync(endpoint, CancellationToken.None);
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("/v2/uploads/jobs/00000000-0000-0000-0000-000000000000")]
        [InlineData("/v2/uploads/jobs/00000000-0000-0000-0000-000000000000", "dv_gr_geschetstgebouw_beheer")]
        [InlineData("/v2/uploads/jobs/00000000-0000-0000-0000-000000000000/jobrecords/123")]
        [InlineData("/v2/uploads/jobs/00000000-0000-0000-0000-000000000000/jobrecords/123", "ingemeten-gebouw-grb-bijwerker")]
        [InlineData("/v2/uploads/jobs/00000000-0000-0000-0000-000000000000/jobrecords/123", "dv_gr_geschetstgebouw_beheer")]
        public async Task Delete_ReturnsForbidden(string endpoint, string scope = "")
        {
            var client = _fixture.TestServer.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await _fixture.GetAccessToken(scope));

            var response = await client.DeleteAsync(endpoint, CancellationToken.None);
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
