using System;
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
    public class JobHistoryResourceIntTest {
        public JobHistoryResourceIntTest()
        {
            _factory = new NhipsterWebApplicationFactory<TestStartup>().WithMockUser();
            _client = _factory.CreateClient();

            _applicationDatabaseContext = _factory.GetRequiredService<ApplicationDatabaseContext>();

            InitTest();
        }

        private static readonly DateTime DefaultStartDate = DateTime.UnixEpoch;
        private static readonly DateTime UpdatedStartDate = DateTime.Now;

        private static readonly DateTime DefaultEndDate = DateTime.UnixEpoch;
        private static readonly DateTime UpdatedEndDate = DateTime.Now;

        private readonly NhipsterWebApplicationFactory<TestStartup> _factory;
        private readonly HttpClient _client;

        private readonly ApplicationDatabaseContext _applicationDatabaseContext;

        private JobHistory _jobHistory;

        private JobHistory CreateEntity()
        {
            return new JobHistory {
                StartDate = DefaultStartDate,
                EndDate = DefaultEndDate
            };
        }

        private void InitTest()
        {
            _jobHistory = CreateEntity();
        }

        [Fact]
        public async Task CreateJobHistory()
        {
            var databaseSizeBeforeCreate = _applicationDatabaseContext.JobHistories.Count();

            // Create the JobHistory
            var response = await _client.PostAsync("/api/job-histories", TestUtil.ToJsonContent(_jobHistory));
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            // Validate the JobHistory in the database
            var jobHistoryList = _applicationDatabaseContext.JobHistories.ToList();
            jobHistoryList.Count().Should().Be(databaseSizeBeforeCreate + 1);
            var testJobHistory = jobHistoryList[jobHistoryList.Count - 1];
            testJobHistory.StartDate.Should().Be(DefaultStartDate);
            testJobHistory.EndDate.Should().Be(DefaultEndDate);
        }

        [Fact]
        public async Task CreateJobHistoryWithExistingId()
        {
            var databaseSizeBeforeCreate = _applicationDatabaseContext.JobHistories.Count();
            databaseSizeBeforeCreate.Should().Be(0);
            // Create the JobHistory with an existing ID
            _jobHistory.Id = 1L;

            // An entity with an existing ID cannot be created, so this API call must fail
            var response = await _client.PostAsync("/api/job-histories", TestUtil.ToJsonContent(_jobHistory));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Validate the JobHistory in the database
            var jobHistoryList = _applicationDatabaseContext.JobHistories.ToList();
            jobHistoryList.Count().Should().Be(databaseSizeBeforeCreate);
        }

        [Fact]
        public async Task GetAllJobHistories()
        {
            // Initialize the database
            _applicationDatabaseContext.JobHistories.Add(_jobHistory);
            await _applicationDatabaseContext.SaveChangesAsync();

            // Get all the jobHistoryList
            var response = await _client.GetAsync("/api/job-histories?sort=id,desc");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.SelectTokens("$.[*].id").Should().Contain(_jobHistory.Id);
            json.SelectTokens("$.[*].startDate").Should().Contain(DefaultStartDate);
            json.SelectTokens("$.[*].endDate").Should().Contain(DefaultEndDate);
        }

        [Fact]
        public async Task GetJobHistory()
        {
            // Initialize the database
            _applicationDatabaseContext.JobHistories.Add(_jobHistory);
            await _applicationDatabaseContext.SaveChangesAsync();

            // Get the jobHistory
            var response = await _client.GetAsync($"/api/job-histories/{_jobHistory.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.SelectTokens("$.id").Should().Contain(_jobHistory.Id);
            json.SelectTokens("$.startDate").Should().Contain(DefaultStartDate);
            json.SelectTokens("$.endDate").Should().Contain(DefaultEndDate);
        }

        [Fact]
        public async Task GetNonExistingJobHistory()
        {
            var response = await _client.GetAsync("/api/job-histories/" + long.MaxValue);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateJobHistory()
        {
            // Initialize the database
            _applicationDatabaseContext.JobHistories.Add(_jobHistory);
            await _applicationDatabaseContext.SaveChangesAsync();

            var databaseSizeBeforeUpdate = _applicationDatabaseContext.JobHistories.Count();

            // Update the jobHistory
            var updatedJobHistory =
                await _applicationDatabaseContext.JobHistories.SingleOrDefaultAsync(it => it.Id == _jobHistory.Id);
            // Disconnect from session so that the updates on updatedJobHistory are not directly saved in db
//TODO detach
            updatedJobHistory.StartDate = UpdatedStartDate;
            updatedJobHistory.EndDate = UpdatedEndDate;

            var response = await _client.PutAsync("/api/job-histories", TestUtil.ToJsonContent(updatedJobHistory));
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Validate the JobHistory in the database
            var jobHistoryList = _applicationDatabaseContext.JobHistories.ToList();
            jobHistoryList.Count().Should().Be(databaseSizeBeforeUpdate);
            var testJobHistory = jobHistoryList[jobHistoryList.Count - 1];
            testJobHistory.StartDate.Should().Be(UpdatedStartDate);
            testJobHistory.EndDate.Should().Be(UpdatedEndDate);
        }

        [Fact]
        public async Task UpdateNonExistingJobHistory()
        {
            var databaseSizeBeforeUpdate = _applicationDatabaseContext.JobHistories.Count();

            // If the entity doesn't have an ID, it will throw BadRequestAlertException
            var response = await _client.PutAsync("/api/job-histories", TestUtil.ToJsonContent(_jobHistory));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Validate the JobHistory in the database
            var jobHistoryList = _applicationDatabaseContext.JobHistories.ToList();
            jobHistoryList.Count().Should().Be(databaseSizeBeforeUpdate);
        }

        [Fact]
        public async Task DeleteJobHistory()
        {
            // Initialize the database
            _applicationDatabaseContext.JobHistories.Add(_jobHistory);
            await _applicationDatabaseContext.SaveChangesAsync();

            var databaseSizeBeforeDelete = _applicationDatabaseContext.JobHistories.Count();

            var response = await _client.DeleteAsync($"/api/job-histories/{_jobHistory.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Validate the database is empty
            var jobHistoryList = _applicationDatabaseContext.JobHistories.ToList();
            jobHistoryList.Count().Should().Be(databaseSizeBeforeDelete - 1);
        }

        [Fact]
        public void EqualsVerifier()
        {
            TestUtil.EqualsVerifier(typeof(JobHistory));
            var jobHistory1 = new JobHistory {
                Id = 1L
            };
            var jobHistory2 = new JobHistory {
                Id = jobHistory1.Id
            };
            jobHistory1.Should().Be(jobHistory2);
            jobHistory2.Id = 2L;
            jobHistory1.Should().NotBe(jobHistory2);
            jobHistory1.Id = 0;
            jobHistory1.Should().NotBe(jobHistory2);
        }
    }
}
