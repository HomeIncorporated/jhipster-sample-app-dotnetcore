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
    public class JobResourceIntTest {
        public JobResourceIntTest()
        {
            _factory = new NhipsterWebApplicationFactory<TestStartup>().WithMockUser();
            _client = _factory.CreateClient();

            _applicationDatabaseContext = _factory.GetRequiredService<ApplicationDatabaseContext>();

            InitTest();
        }

        private const string DefaultJobTitle = "AAAAAAAAAA";
        private const string UpdatedJobTitle = "BBBBBBBBBB";

        private static readonly long? DefaultMinSalary = 1L;
        private static readonly long? UpdatedMinSalary = 2L;

        private static readonly long? DefaultMaxSalary = 1L;
        private static readonly long? UpdatedMaxSalary = 2L;

        private readonly NhipsterWebApplicationFactory<TestStartup> _factory;
        private readonly HttpClient _client;

        private readonly ApplicationDatabaseContext _applicationDatabaseContext;

        private Job _job;

        private Job CreateEntity()
        {
            return new Job {
                JobTitle = DefaultJobTitle,
                MinSalary = DefaultMinSalary,
                MaxSalary = DefaultMaxSalary
            };
        }

        private void InitTest()
        {
            _job = CreateEntity();
        }

        [Fact]
        public async Task CreateJob()
        {
            var databaseSizeBeforeCreate = _applicationDatabaseContext.Jobs.Count();

            // Create the Job
            var response = await _client.PostAsync("/api/jobs", TestUtil.ToJsonContent(_job));
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            // Validate the Job in the database
            var jobList = _applicationDatabaseContext.Jobs.ToList();
            jobList.Count().Should().Be(databaseSizeBeforeCreate + 1);
            var testJob = jobList[jobList.Count - 1];
            testJob.JobTitle.Should().Be(DefaultJobTitle);
            testJob.MinSalary.Should().Be(DefaultMinSalary);
            testJob.MaxSalary.Should().Be(DefaultMaxSalary);
        }

        [Fact]
        public async Task CreateJobWithExistingId()
        {
            var databaseSizeBeforeCreate = _applicationDatabaseContext.Jobs.Count();
            databaseSizeBeforeCreate.Should().Be(0);
            // Create the Job with an existing ID
            _job.Id = 1L;

            // An entity with an existing ID cannot be created, so this API call must fail
            var response = await _client.PostAsync("/api/jobs", TestUtil.ToJsonContent(_job));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Validate the Job in the database
            var jobList = _applicationDatabaseContext.Jobs.ToList();
            jobList.Count().Should().Be(databaseSizeBeforeCreate);
        }

        [Fact]
        public async Task GetAllJobs()
        {
            // Initialize the database
            _applicationDatabaseContext.Jobs.Add(_job);
            await _applicationDatabaseContext.SaveChangesAsync();

            // Get all the jobList
            var response = await _client.GetAsync("/api/jobs?sort=id,desc");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.SelectTokens("$.[*].id").Should().Contain(_job.Id);
            json.SelectTokens("$.[*].jobTitle").Should().Contain(DefaultJobTitle);
            json.SelectTokens("$.[*].minSalary").Should().Contain(DefaultMinSalary);
            json.SelectTokens("$.[*].maxSalary").Should().Contain(DefaultMaxSalary);
        }

        [Fact]
        public async Task GetJob()
        {
            // Initialize the database
            _applicationDatabaseContext.Jobs.Add(_job);
            await _applicationDatabaseContext.SaveChangesAsync();

            // Get the job
            var response = await _client.GetAsync($"/api/jobs/{_job.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.SelectTokens("$.id").Should().Contain(_job.Id);
            json.SelectTokens("$.jobTitle").Should().Contain(DefaultJobTitle);
            json.SelectTokens("$.minSalary").Should().Contain(DefaultMinSalary);
            json.SelectTokens("$.maxSalary").Should().Contain(DefaultMaxSalary);
        }

        [Fact]
        public async Task GetNonExistingJob()
        {
            var response = await _client.GetAsync("/api/jobs/" + long.MaxValue);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateJob()
        {
            // Initialize the database
            _applicationDatabaseContext.Jobs.Add(_job);
            await _applicationDatabaseContext.SaveChangesAsync();

            var databaseSizeBeforeUpdate = _applicationDatabaseContext.Jobs.Count();

            // Update the job
            var updatedJob =
                await _applicationDatabaseContext.Jobs.SingleOrDefaultAsync(it => it.Id == _job.Id);
            // Disconnect from session so that the updates on updatedJob are not directly saved in db
//TODO detach
            updatedJob.JobTitle = UpdatedJobTitle;
            updatedJob.MinSalary = UpdatedMinSalary;
            updatedJob.MaxSalary = UpdatedMaxSalary;

            var response = await _client.PutAsync("/api/jobs", TestUtil.ToJsonContent(updatedJob));
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Validate the Job in the database
            var jobList = _applicationDatabaseContext.Jobs.ToList();
            jobList.Count().Should().Be(databaseSizeBeforeUpdate);
            var testJob = jobList[jobList.Count - 1];
            testJob.JobTitle.Should().Be(UpdatedJobTitle);
            testJob.MinSalary.Should().Be(UpdatedMinSalary);
            testJob.MaxSalary.Should().Be(UpdatedMaxSalary);
        }

        [Fact]
        public async Task UpdateNonExistingJob()
        {
            var databaseSizeBeforeUpdate = _applicationDatabaseContext.Jobs.Count();

            // If the entity doesn't have an ID, it will throw BadRequestAlertException
            var response = await _client.PutAsync("/api/jobs", TestUtil.ToJsonContent(_job));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Validate the Job in the database
            var jobList = _applicationDatabaseContext.Jobs.ToList();
            jobList.Count().Should().Be(databaseSizeBeforeUpdate);
        }

        [Fact]
        public async Task DeleteJob()
        {
            // Initialize the database
            _applicationDatabaseContext.Jobs.Add(_job);
            await _applicationDatabaseContext.SaveChangesAsync();

            var databaseSizeBeforeDelete = _applicationDatabaseContext.Jobs.Count();

            var response = await _client.DeleteAsync($"/api/jobs/{_job.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Validate the database is empty
            var jobList = _applicationDatabaseContext.Jobs.ToList();
            jobList.Count().Should().Be(databaseSizeBeforeDelete - 1);
        }

        [Fact]
        public void EqualsVerifier()
        {
            TestUtil.EqualsVerifier(typeof(Job));
            var job1 = new Job {
                Id = 1L
            };
            var job2 = new Job {
                Id = job1.Id
            };
            job1.Should().Be(job2);
            job2.Id = 2L;
            job1.Should().NotBe(job2);
            job1.Id = 0;
            job1.Should().NotBe(job2);
        }
    }
}
