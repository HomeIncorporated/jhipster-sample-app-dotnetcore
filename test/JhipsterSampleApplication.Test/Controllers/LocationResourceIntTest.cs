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
    public class LocationResourceIntTest {
        public LocationResourceIntTest()
        {
            _factory = new NhipsterWebApplicationFactory<TestStartup>().WithMockUser();
            _client = _factory.CreateClient();

            _applicationDatabaseContext = _factory.GetRequiredService<ApplicationDatabaseContext>();

            InitTest();
        }

        private const string DefaultStreetAddress = "AAAAAAAAAA";
        private const string UpdatedStreetAddress = "BBBBBBBBBB";

        private const string DefaultPostalCode = "AAAAAAAAAA";
        private const string UpdatedPostalCode = "BBBBBBBBBB";

        private const string DefaultCity = "AAAAAAAAAA";
        private const string UpdatedCity = "BBBBBBBBBB";

        private const string DefaultStateProvince = "AAAAAAAAAA";
        private const string UpdatedStateProvince = "BBBBBBBBBB";

        private readonly NhipsterWebApplicationFactory<TestStartup> _factory;
        private readonly HttpClient _client;

        private readonly ApplicationDatabaseContext _applicationDatabaseContext;

        private Location _location;

        private Location CreateEntity()
        {
            return new Location {
                StreetAddress = DefaultStreetAddress,
                PostalCode = DefaultPostalCode,
                City = DefaultCity,
                StateProvince = DefaultStateProvince
            };
        }

        private void InitTest()
        {
            _location = CreateEntity();
        }

        [Fact]
        public async Task CreateLocation()
        {
            var databaseSizeBeforeCreate = _applicationDatabaseContext.Locations.Count();

            // Create the Location
            var response = await _client.PostAsync("/api/locations", TestUtil.ToJsonContent(_location));
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            // Validate the Location in the database
            var locationList = _applicationDatabaseContext.Locations.ToList();
            locationList.Count().Should().Be(databaseSizeBeforeCreate + 1);
            var testLocation = locationList[locationList.Count - 1];
            testLocation.StreetAddress.Should().Be(DefaultStreetAddress);
            testLocation.PostalCode.Should().Be(DefaultPostalCode);
            testLocation.City.Should().Be(DefaultCity);
            testLocation.StateProvince.Should().Be(DefaultStateProvince);
        }

        [Fact]
        public async Task CreateLocationWithExistingId()
        {
            var databaseSizeBeforeCreate = _applicationDatabaseContext.Locations.Count();
            databaseSizeBeforeCreate.Should().Be(0);
            // Create the Location with an existing ID
            _location.Id = 1L;

            // An entity with an existing ID cannot be created, so this API call must fail
            var response = await _client.PostAsync("/api/locations", TestUtil.ToJsonContent(_location));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Validate the Location in the database
            var locationList = _applicationDatabaseContext.Locations.ToList();
            locationList.Count().Should().Be(databaseSizeBeforeCreate);
        }

        [Fact]
        public async Task GetAllLocations()
        {
            // Initialize the database
            _applicationDatabaseContext.Locations.Add(_location);
            await _applicationDatabaseContext.SaveChangesAsync();

            // Get all the locationList
            var response = await _client.GetAsync("/api/locations?sort=id,desc");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.SelectTokens("$.[*].id").Should().Contain(_location.Id);
            json.SelectTokens("$.[*].streetAddress").Should().Contain(DefaultStreetAddress);
            json.SelectTokens("$.[*].postalCode").Should().Contain(DefaultPostalCode);
            json.SelectTokens("$.[*].city").Should().Contain(DefaultCity);
            json.SelectTokens("$.[*].stateProvince").Should().Contain(DefaultStateProvince);
        }

        [Fact]
        public async Task GetLocation()
        {
            // Initialize the database
            _applicationDatabaseContext.Locations.Add(_location);
            await _applicationDatabaseContext.SaveChangesAsync();

            // Get the location
            var response = await _client.GetAsync($"/api/locations/{_location.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.SelectTokens("$.id").Should().Contain(_location.Id);
            json.SelectTokens("$.streetAddress").Should().Contain(DefaultStreetAddress);
            json.SelectTokens("$.postalCode").Should().Contain(DefaultPostalCode);
            json.SelectTokens("$.city").Should().Contain(DefaultCity);
            json.SelectTokens("$.stateProvince").Should().Contain(DefaultStateProvince);
        }

        [Fact]
        public async Task GetNonExistingLocation()
        {
            var response = await _client.GetAsync("/api/locations/" + long.MaxValue);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateLocation()
        {
            // Initialize the database
            _applicationDatabaseContext.Locations.Add(_location);
            await _applicationDatabaseContext.SaveChangesAsync();

            var databaseSizeBeforeUpdate = _applicationDatabaseContext.Locations.Count();

            // Update the location
            var updatedLocation =
                await _applicationDatabaseContext.Locations.SingleOrDefaultAsync(it => it.Id == _location.Id);
            // Disconnect from session so that the updates on updatedLocation are not directly saved in db
//TODO detach
            updatedLocation.StreetAddress = UpdatedStreetAddress;
            updatedLocation.PostalCode = UpdatedPostalCode;
            updatedLocation.City = UpdatedCity;
            updatedLocation.StateProvince = UpdatedStateProvince;

            var response = await _client.PutAsync("/api/locations", TestUtil.ToJsonContent(updatedLocation));
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Validate the Location in the database
            var locationList = _applicationDatabaseContext.Locations.ToList();
            locationList.Count().Should().Be(databaseSizeBeforeUpdate);
            var testLocation = locationList[locationList.Count - 1];
            testLocation.StreetAddress.Should().Be(UpdatedStreetAddress);
            testLocation.PostalCode.Should().Be(UpdatedPostalCode);
            testLocation.City.Should().Be(UpdatedCity);
            testLocation.StateProvince.Should().Be(UpdatedStateProvince);
        }

        [Fact]
        public async Task UpdateNonExistingLocation()
        {
            var databaseSizeBeforeUpdate = _applicationDatabaseContext.Locations.Count();

            // If the entity doesn't have an ID, it will throw BadRequestAlertException
            var response = await _client.PutAsync("/api/locations", TestUtil.ToJsonContent(_location));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Validate the Location in the database
            var locationList = _applicationDatabaseContext.Locations.ToList();
            locationList.Count().Should().Be(databaseSizeBeforeUpdate);
        }

        [Fact]
        public async Task DeleteLocation()
        {
            // Initialize the database
            _applicationDatabaseContext.Locations.Add(_location);
            await _applicationDatabaseContext.SaveChangesAsync();

            var databaseSizeBeforeDelete = _applicationDatabaseContext.Locations.Count();

            var response = await _client.DeleteAsync($"/api/locations/{_location.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Validate the database is empty
            var locationList = _applicationDatabaseContext.Locations.ToList();
            locationList.Count().Should().Be(databaseSizeBeforeDelete - 1);
        }

        [Fact]
        public void EqualsVerifier()
        {
            TestUtil.EqualsVerifier(typeof(Location));
            var location1 = new Location {
                Id = 1L
            };
            var location2 = new Location {
                Id = location1.Id
            };
            location1.Should().Be(location2);
            location2.Id = 2L;
            location1.Should().NotBe(location2);
            location1.Id = 0;
            location1.Should().NotBe(location2);
        }
    }
}
