using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using MyCompany.Data;
using MyCompany.Models;
using MyCompany.Test.Setup;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Xunit;

namespace MyCompany.Test.Controllers {
    public class RegionResourceIntTest {
        public RegionResourceIntTest()
        {
            _factory = new NhipsterWebApplicationFactory<TestStartup>().WithMockUser();
            _client = _factory.CreateClient();

            _applicationDatabaseContext = _factory.GetRequiredService<ApplicationDatabaseContext>();

            InitTest();
        }

        private const string DefaultRegionName = "AAAAAAAAAA";
        private const string UpdatedRegionName = "BBBBBBBBBB";

        private readonly NhipsterWebApplicationFactory<TestStartup> _factory;
        private readonly HttpClient _client;

        private readonly ApplicationDatabaseContext _applicationDatabaseContext;

        private Region _region;

        private Region CreateEntity()
        {
            return new Region {
                RegionName = DefaultRegionName
            };
        }

        private void InitTest()
        {
            _region = CreateEntity();
        }

        [Fact]
        public async Task CreateRegion()
        {
            var databaseSizeBeforeCreate = _applicationDatabaseContext.Regions.Count();

            // Create the Region
            var response = await _client.PostAsync("/api/regions", TestUtil.ToJsonContent(_region));
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            // Validate the Region in the database
            var regionList = _applicationDatabaseContext.Regions.ToList();
            regionList.Count().Should().Be(databaseSizeBeforeCreate + 1);
            var testRegion = regionList[regionList.Count - 1];
            testRegion.RegionName.Should().Be(DefaultRegionName);
        }

        [Fact]
        public async Task CreateRegionWithExistingId()
        {
            var databaseSizeBeforeCreate = _applicationDatabaseContext.Regions.Count();
            databaseSizeBeforeCreate.Should().Be(0);
            // Create the Region with an existing ID
            _region.Id = 1L;

            // An entity with an existing ID cannot be created, so this API call must fail
            var response = await _client.PostAsync("/api/regions", TestUtil.ToJsonContent(_region));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Validate the Region in the database
            var regionList = _applicationDatabaseContext.Regions.ToList();
            regionList.Count().Should().Be(databaseSizeBeforeCreate);
        }

        [Fact]
        public async Task GetAllRegions()
        {
            // Initialize the database
            _applicationDatabaseContext.Regions.Add(_region);
            await _applicationDatabaseContext.SaveChangesAsync();

            // Get all the regionList
            var response = await _client.GetAsync("/api/regions?sort=id,desc");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.SelectTokens("$.[*].id").Should().Contain(_region.Id);
            json.SelectTokens("$.[*].regionName").Should().Contain(DefaultRegionName);
        }

        [Fact]
        public async Task GetRegion()
        {
            // Initialize the database
            _applicationDatabaseContext.Regions.Add(_region);
            await _applicationDatabaseContext.SaveChangesAsync();

            // Get the region
            var response = await _client.GetAsync($"/api/regions/{_region.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.SelectTokens("$.id").Should().Contain(_region.Id);
            json.SelectTokens("$.regionName").Should().Contain(DefaultRegionName);
        }

        [Fact]
        public async Task GetNonExistingRegion()
        {
            var response = await _client.GetAsync("/api/regions/" + long.MaxValue);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateRegion()
        {
            // Initialize the database
            _applicationDatabaseContext.Regions.Add(_region);
            await _applicationDatabaseContext.SaveChangesAsync();

            var databaseSizeBeforeUpdate = _applicationDatabaseContext.Regions.Count();

            // Update the region
            var updatedRegion =
                await _applicationDatabaseContext.Regions.SingleOrDefaultAsync(it => it.Id == _region.Id);
            // Disconnect from session so that the updates on updatedRegion are not directly saved in db
//TODO detach
            updatedRegion.RegionName = UpdatedRegionName;

            var response = await _client.PutAsync("/api/regions", TestUtil.ToJsonContent(updatedRegion));
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Validate the Region in the database
            var regionList = _applicationDatabaseContext.Regions.ToList();
            regionList.Count().Should().Be(databaseSizeBeforeUpdate);
            var testRegion = regionList[regionList.Count - 1];
            testRegion.RegionName.Should().Be(UpdatedRegionName);
        }

        [Fact]
        public async Task UpdateNonExistingRegion()
        {
            var databaseSizeBeforeUpdate = _applicationDatabaseContext.Regions.Count();

            // If the entity doesn't have an ID, it will throw BadRequestAlertException
            var response = await _client.PutAsync("/api/regions", TestUtil.ToJsonContent(_region));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Validate the Region in the database
            var regionList = _applicationDatabaseContext.Regions.ToList();
            regionList.Count().Should().Be(databaseSizeBeforeUpdate);
        }

        [Fact]
        public async Task DeleteRegion()
        {
            // Initialize the database
            _applicationDatabaseContext.Regions.Add(_region);
            await _applicationDatabaseContext.SaveChangesAsync();

            var databaseSizeBeforeDelete = _applicationDatabaseContext.Regions.Count();

            var response = await _client.DeleteAsync($"/api/regions/{_region.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Validate the database is empty
            var regionList = _applicationDatabaseContext.Regions.ToList();
            regionList.Count().Should().Be(databaseSizeBeforeDelete - 1);
        }

        [Fact]
        public void EqualsVerifier()
        {
            TestUtil.EqualsVerifier(typeof(Region));
            var region1 = new Region {
                Id = 1L
            };
            var region2 = new Region {
                Id = region1.Id
            };
            region1.Should().Be(region2);
            region2.Id = 2L;
            region1.Should().NotBe(region2);
            region1.Id = 0;
            region1.Should().NotBe(region2);
        }
    }
}
