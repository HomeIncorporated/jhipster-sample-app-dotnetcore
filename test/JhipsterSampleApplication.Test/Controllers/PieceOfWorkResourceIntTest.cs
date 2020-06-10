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
    public class PieceOfWorkResourceIntTest {
        public PieceOfWorkResourceIntTest()
        {
            _factory = new NhipsterWebApplicationFactory<TestStartup>().WithMockUser();
            _client = _factory.CreateClient();

            _applicationDatabaseContext = _factory.GetRequiredService<ApplicationDatabaseContext>();

            InitTest();
        }

        private const string DefaultTitle = "AAAAAAAAAA";
        private const string UpdatedTitle = "BBBBBBBBBB";

        private const string DefaultDescription = "AAAAAAAAAA";
        private const string UpdatedDescription = "BBBBBBBBBB";

        private readonly NhipsterWebApplicationFactory<TestStartup> _factory;
        private readonly HttpClient _client;

        private readonly ApplicationDatabaseContext _applicationDatabaseContext;

        private PieceOfWork _pieceOfWork;

        private PieceOfWork CreateEntity()
        {
            return new PieceOfWork {
                Title = DefaultTitle,
                Description = DefaultDescription
            };
        }

        private void InitTest()
        {
            _pieceOfWork = CreateEntity();
        }

        [Fact]
        public async Task CreatePieceOfWork()
        {
            var databaseSizeBeforeCreate = _applicationDatabaseContext.PieceOfWorks.Count();

            // Create the PieceOfWork
            var response = await _client.PostAsync("/api/piece-of-works", TestUtil.ToJsonContent(_pieceOfWork));
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            // Validate the PieceOfWork in the database
            var pieceOfWorkList = _applicationDatabaseContext.PieceOfWorks.ToList();
            pieceOfWorkList.Count().Should().Be(databaseSizeBeforeCreate + 1);
            var testPieceOfWork = pieceOfWorkList[pieceOfWorkList.Count - 1];
            testPieceOfWork.Title.Should().Be(DefaultTitle);
            testPieceOfWork.Description.Should().Be(DefaultDescription);
        }

        [Fact]
        public async Task CreatePieceOfWorkWithExistingId()
        {
            var databaseSizeBeforeCreate = _applicationDatabaseContext.PieceOfWorks.Count();
            databaseSizeBeforeCreate.Should().Be(0);
            // Create the PieceOfWork with an existing ID
            _pieceOfWork.Id = 1L;

            // An entity with an existing ID cannot be created, so this API call must fail
            var response = await _client.PostAsync("/api/piece-of-works", TestUtil.ToJsonContent(_pieceOfWork));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Validate the PieceOfWork in the database
            var pieceOfWorkList = _applicationDatabaseContext.PieceOfWorks.ToList();
            pieceOfWorkList.Count().Should().Be(databaseSizeBeforeCreate);
        }

        [Fact]
        public async Task GetAllPieceOfWorks()
        {
            // Initialize the database
            _applicationDatabaseContext.PieceOfWorks.Add(_pieceOfWork);
            await _applicationDatabaseContext.SaveChangesAsync();

            // Get all the pieceOfWorkList
            var response = await _client.GetAsync("/api/piece-of-works?sort=id,desc");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.SelectTokens("$.[*].id").Should().Contain(_pieceOfWork.Id);
            json.SelectTokens("$.[*].title").Should().Contain(DefaultTitle);
            json.SelectTokens("$.[*].description").Should().Contain(DefaultDescription);
        }

        [Fact]
        public async Task GetPieceOfWork()
        {
            // Initialize the database
            _applicationDatabaseContext.PieceOfWorks.Add(_pieceOfWork);
            await _applicationDatabaseContext.SaveChangesAsync();

            // Get the pieceOfWork
            var response = await _client.GetAsync($"/api/piece-of-works/{_pieceOfWork.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.SelectTokens("$.id").Should().Contain(_pieceOfWork.Id);
            json.SelectTokens("$.title").Should().Contain(DefaultTitle);
            json.SelectTokens("$.description").Should().Contain(DefaultDescription);
        }

        [Fact]
        public async Task GetNonExistingPieceOfWork()
        {
            var response = await _client.GetAsync("/api/piece-of-works/" + long.MaxValue);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdatePieceOfWork()
        {
            // Initialize the database
            _applicationDatabaseContext.PieceOfWorks.Add(_pieceOfWork);
            await _applicationDatabaseContext.SaveChangesAsync();

            var databaseSizeBeforeUpdate = _applicationDatabaseContext.PieceOfWorks.Count();

            // Update the pieceOfWork
            var updatedPieceOfWork =
                await _applicationDatabaseContext.PieceOfWorks.SingleOrDefaultAsync(it => it.Id == _pieceOfWork.Id);
            // Disconnect from session so that the updates on updatedPieceOfWork are not directly saved in db
//TODO detach
            updatedPieceOfWork.Title = UpdatedTitle;
            updatedPieceOfWork.Description = UpdatedDescription;

            var response = await _client.PutAsync("/api/piece-of-works", TestUtil.ToJsonContent(updatedPieceOfWork));
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Validate the PieceOfWork in the database
            var pieceOfWorkList = _applicationDatabaseContext.PieceOfWorks.ToList();
            pieceOfWorkList.Count().Should().Be(databaseSizeBeforeUpdate);
            var testPieceOfWork = pieceOfWorkList[pieceOfWorkList.Count - 1];
            testPieceOfWork.Title.Should().Be(UpdatedTitle);
            testPieceOfWork.Description.Should().Be(UpdatedDescription);
        }

        [Fact]
        public async Task UpdateNonExistingPieceOfWork()
        {
            var databaseSizeBeforeUpdate = _applicationDatabaseContext.PieceOfWorks.Count();

            // If the entity doesn't have an ID, it will throw BadRequestAlertException
            var response = await _client.PutAsync("/api/piece-of-works", TestUtil.ToJsonContent(_pieceOfWork));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Validate the PieceOfWork in the database
            var pieceOfWorkList = _applicationDatabaseContext.PieceOfWorks.ToList();
            pieceOfWorkList.Count().Should().Be(databaseSizeBeforeUpdate);
        }

        [Fact]
        public async Task DeletePieceOfWork()
        {
            // Initialize the database
            _applicationDatabaseContext.PieceOfWorks.Add(_pieceOfWork);
            await _applicationDatabaseContext.SaveChangesAsync();

            var databaseSizeBeforeDelete = _applicationDatabaseContext.PieceOfWorks.Count();

            var response = await _client.DeleteAsync($"/api/piece-of-works/{_pieceOfWork.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Validate the database is empty
            var pieceOfWorkList = _applicationDatabaseContext.PieceOfWorks.ToList();
            pieceOfWorkList.Count().Should().Be(databaseSizeBeforeDelete - 1);
        }

        [Fact]
        public void EqualsVerifier()
        {
            TestUtil.EqualsVerifier(typeof(PieceOfWork));
            var pieceOfWork1 = new PieceOfWork {
                Id = 1L
            };
            var pieceOfWork2 = new PieceOfWork {
                Id = pieceOfWork1.Id
            };
            pieceOfWork1.Should().Be(pieceOfWork2);
            pieceOfWork2.Id = 2L;
            pieceOfWork1.Should().NotBe(pieceOfWork2);
            pieceOfWork1.Id = 0;
            pieceOfWork1.Should().NotBe(pieceOfWork2);
        }
    }
}
