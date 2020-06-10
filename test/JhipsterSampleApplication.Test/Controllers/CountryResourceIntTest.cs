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
    public class CountryResourceIntTest {
        public CountryResourceIntTest()
        {
            _factory = new NhipsterWebApplicationFactory<TestStartup>().WithMockUser();
            _client = _factory.CreateClient();

            _applicationDatabaseContext = _factory.GetRequiredService<ApplicationDatabaseContext>();

            InitTest();
        }

        private const string DefaultCountryName = "AAAAAAAAAA";
        private const string UpdatedCountryName = "BBBBBBBBBB";

        private readonly NhipsterWebApplicationFactory<TestStartup> _factory;
        private readonly HttpClient _client;

        private readonly ApplicationDatabaseContext _applicationDatabaseContext;

        private Country _country;

        private Country CreateEntity()
        {
            return new Country {
                CountryName = DefaultCountryName
            };
        }

        private void InitTest()
        {
            _country = CreateEntity();
        }

        [Fact]
        public async Task CreateCountry()
        {
            var databaseSizeBeforeCreate = _applicationDatabaseContext.Countries.Count();

            // Create the Country
            var response = await _client.PostAsync("/api/countries", TestUtil.ToJsonContent(_country));
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            // Validate the Country in the database
            var countryList = _applicationDatabaseContext.Countries.ToList();
            countryList.Count().Should().Be(databaseSizeBeforeCreate + 1);
            var testCountry = countryList[countryList.Count - 1];
            testCountry.CountryName.Should().Be(DefaultCountryName);
        }

        [Fact]
        public async Task CreateCountryWithExistingId()
        {
            var databaseSizeBeforeCreate = _applicationDatabaseContext.Countries.Count();
            databaseSizeBeforeCreate.Should().Be(0);
            // Create the Country with an existing ID
            _country.Id = 1L;

            // An entity with an existing ID cannot be created, so this API call must fail
            var response = await _client.PostAsync("/api/countries", TestUtil.ToJsonContent(_country));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Validate the Country in the database
            var countryList = _applicationDatabaseContext.Countries.ToList();
            countryList.Count().Should().Be(databaseSizeBeforeCreate);
        }

        [Fact]
        public async Task GetAllCountries()
        {
            // Initialize the database
            _applicationDatabaseContext.Countries.Add(_country);
            await _applicationDatabaseContext.SaveChangesAsync();

            // Get all the countryList
            var response = await _client.GetAsync("/api/countries?sort=id,desc");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.SelectTokens("$.[*].id").Should().Contain(_country.Id);
            json.SelectTokens("$.[*].countryName").Should().Contain(DefaultCountryName);
        }

        [Fact]
        public async Task GetCountry()
        {
            // Initialize the database
            _applicationDatabaseContext.Countries.Add(_country);
            await _applicationDatabaseContext.SaveChangesAsync();

            // Get the country
            var response = await _client.GetAsync($"/api/countries/{_country.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.SelectTokens("$.id").Should().Contain(_country.Id);
            json.SelectTokens("$.countryName").Should().Contain(DefaultCountryName);
        }

        [Fact]
        public async Task GetNonExistingCountry()
        {
            var response = await _client.GetAsync("/api/countries/" + long.MaxValue);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateCountry()
        {
            // Initialize the database
            _applicationDatabaseContext.Countries.Add(_country);
            await _applicationDatabaseContext.SaveChangesAsync();

            var databaseSizeBeforeUpdate = _applicationDatabaseContext.Countries.Count();

            // Update the country
            var updatedCountry =
                await _applicationDatabaseContext.Countries.SingleOrDefaultAsync(it => it.Id == _country.Id);
            // Disconnect from session so that the updates on updatedCountry are not directly saved in db
//TODO detach
            updatedCountry.CountryName = UpdatedCountryName;

            var response = await _client.PutAsync("/api/countries", TestUtil.ToJsonContent(updatedCountry));
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Validate the Country in the database
            var countryList = _applicationDatabaseContext.Countries.ToList();
            countryList.Count().Should().Be(databaseSizeBeforeUpdate);
            var testCountry = countryList[countryList.Count - 1];
            testCountry.CountryName.Should().Be(UpdatedCountryName);
        }

        [Fact]
        public async Task UpdateNonExistingCountry()
        {
            var databaseSizeBeforeUpdate = _applicationDatabaseContext.Countries.Count();

            // If the entity doesn't have an ID, it will throw BadRequestAlertException
            var response = await _client.PutAsync("/api/countries", TestUtil.ToJsonContent(_country));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Validate the Country in the database
            var countryList = _applicationDatabaseContext.Countries.ToList();
            countryList.Count().Should().Be(databaseSizeBeforeUpdate);
        }

        [Fact]
        public async Task DeleteCountry()
        {
            // Initialize the database
            _applicationDatabaseContext.Countries.Add(_country);
            await _applicationDatabaseContext.SaveChangesAsync();

            var databaseSizeBeforeDelete = _applicationDatabaseContext.Countries.Count();

            var response = await _client.DeleteAsync($"/api/countries/{_country.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Validate the database is empty
            var countryList = _applicationDatabaseContext.Countries.ToList();
            countryList.Count().Should().Be(databaseSizeBeforeDelete - 1);
        }

        [Fact]
        public void EqualsVerifier()
        {
            TestUtil.EqualsVerifier(typeof(Country));
            var country1 = new Country {
                Id = 1L
            };
            var country2 = new Country {
                Id = country1.Id
            };
            country1.Should().Be(country2);
            country2.Id = 2L;
            country1.Should().NotBe(country2);
            country1.Id = 0;
            country1.Should().NotBe(country2);
        }
    }
}
