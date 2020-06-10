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
    public class DepartmentResourceIntTest {
        public DepartmentResourceIntTest()
        {
            _factory = new NhipsterWebApplicationFactory<TestStartup>().WithMockUser();
            _client = _factory.CreateClient();

            _applicationDatabaseContext = _factory.GetRequiredService<ApplicationDatabaseContext>();

            InitTest();
        }

        private const string DefaultDepartmentName = "AAAAAAAAAA";
        private const string UpdatedDepartmentName = "BBBBBBBBBB";

        private readonly NhipsterWebApplicationFactory<TestStartup> _factory;
        private readonly HttpClient _client;

        private readonly ApplicationDatabaseContext _applicationDatabaseContext;

        private Department _department;

        private Department CreateEntity()
        {
            return new Department {
                DepartmentName = DefaultDepartmentName
            };
        }

        private void InitTest()
        {
            _department = CreateEntity();
        }

        [Fact]
        public async Task CreateDepartment()
        {
            var databaseSizeBeforeCreate = _applicationDatabaseContext.Departments.Count();

            // Create the Department
            var response = await _client.PostAsync("/api/departments", TestUtil.ToJsonContent(_department));
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            // Validate the Department in the database
            var departmentList = _applicationDatabaseContext.Departments.ToList();
            departmentList.Count().Should().Be(databaseSizeBeforeCreate + 1);
            var testDepartment = departmentList[departmentList.Count - 1];
            testDepartment.DepartmentName.Should().Be(DefaultDepartmentName);
        }

        [Fact]
        public async Task CreateDepartmentWithExistingId()
        {
            var databaseSizeBeforeCreate = _applicationDatabaseContext.Departments.Count();
            databaseSizeBeforeCreate.Should().Be(0);
            // Create the Department with an existing ID
            _department.Id = 1L;

            // An entity with an existing ID cannot be created, so this API call must fail
            var response = await _client.PostAsync("/api/departments", TestUtil.ToJsonContent(_department));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Validate the Department in the database
            var departmentList = _applicationDatabaseContext.Departments.ToList();
            departmentList.Count().Should().Be(databaseSizeBeforeCreate);
        }

        [Fact]
        public async Task CheckDepartmentNameIsRequired()
        {
            var databaseSizeBeforeTest = _applicationDatabaseContext.Departments.Count();

            // Set the field to null
            _department.DepartmentName = null;

            // Create the Department, which fails.
            var response = await _client.PostAsync("/api/departments", TestUtil.ToJsonContent(_department));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var departmentList = _applicationDatabaseContext.Departments.ToList();
            departmentList.Count().Should().Be(databaseSizeBeforeTest);
        }

        [Fact]
        public async Task GetAllDepartments()
        {
            // Initialize the database
            _applicationDatabaseContext.Departments.Add(_department);
            await _applicationDatabaseContext.SaveChangesAsync();

            // Get all the departmentList
            var response = await _client.GetAsync("/api/departments?sort=id,desc");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.SelectTokens("$.[*].id").Should().Contain(_department.Id);
            json.SelectTokens("$.[*].departmentName").Should().Contain(DefaultDepartmentName);
        }

        [Fact]
        public async Task GetDepartment()
        {
            // Initialize the database
            _applicationDatabaseContext.Departments.Add(_department);
            await _applicationDatabaseContext.SaveChangesAsync();

            // Get the department
            var response = await _client.GetAsync($"/api/departments/{_department.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.SelectTokens("$.id").Should().Contain(_department.Id);
            json.SelectTokens("$.departmentName").Should().Contain(DefaultDepartmentName);
        }

        [Fact]
        public async Task GetNonExistingDepartment()
        {
            var response = await _client.GetAsync("/api/departments/" + long.MaxValue);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateDepartment()
        {
            // Initialize the database
            _applicationDatabaseContext.Departments.Add(_department);
            await _applicationDatabaseContext.SaveChangesAsync();

            var databaseSizeBeforeUpdate = _applicationDatabaseContext.Departments.Count();

            // Update the department
            var updatedDepartment =
                await _applicationDatabaseContext.Departments.SingleOrDefaultAsync(it => it.Id == _department.Id);
            // Disconnect from session so that the updates on updatedDepartment are not directly saved in db
//TODO detach
            updatedDepartment.DepartmentName = UpdatedDepartmentName;

            var response = await _client.PutAsync("/api/departments", TestUtil.ToJsonContent(updatedDepartment));
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Validate the Department in the database
            var departmentList = _applicationDatabaseContext.Departments.ToList();
            departmentList.Count().Should().Be(databaseSizeBeforeUpdate);
            var testDepartment = departmentList[departmentList.Count - 1];
            testDepartment.DepartmentName.Should().Be(UpdatedDepartmentName);
        }

        [Fact]
        public async Task UpdateNonExistingDepartment()
        {
            var databaseSizeBeforeUpdate = _applicationDatabaseContext.Departments.Count();

            // If the entity doesn't have an ID, it will throw BadRequestAlertException
            var response = await _client.PutAsync("/api/departments", TestUtil.ToJsonContent(_department));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Validate the Department in the database
            var departmentList = _applicationDatabaseContext.Departments.ToList();
            departmentList.Count().Should().Be(databaseSizeBeforeUpdate);
        }

        [Fact]
        public async Task DeleteDepartment()
        {
            // Initialize the database
            _applicationDatabaseContext.Departments.Add(_department);
            await _applicationDatabaseContext.SaveChangesAsync();

            var databaseSizeBeforeDelete = _applicationDatabaseContext.Departments.Count();

            var response = await _client.DeleteAsync($"/api/departments/{_department.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Validate the database is empty
            var departmentList = _applicationDatabaseContext.Departments.ToList();
            departmentList.Count().Should().Be(databaseSizeBeforeDelete - 1);
        }

        [Fact]
        public void EqualsVerifier()
        {
            TestUtil.EqualsVerifier(typeof(Department));
            var department1 = new Department {
                Id = 1L
            };
            var department2 = new Department {
                Id = department1.Id
            };
            department1.Should().Be(department2);
            department2.Id = 2L;
            department1.Should().NotBe(department2);
            department1.Id = 0;
            department1.Should().NotBe(department2);
        }
    }
}
