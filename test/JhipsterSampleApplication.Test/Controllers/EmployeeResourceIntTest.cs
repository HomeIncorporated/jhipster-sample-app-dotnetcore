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
    public class EmployeeResourceIntTest {
        public EmployeeResourceIntTest()
        {
            _factory = new NhipsterWebApplicationFactory<TestStartup>().WithMockUser();
            _client = _factory.CreateClient();

            _applicationDatabaseContext = _factory.GetRequiredService<ApplicationDatabaseContext>();

            InitTest();
        }

        private const string DefaultFirstName = "AAAAAAAAAA";
        private const string UpdatedFirstName = "BBBBBBBBBB";

        private const string DefaultLastName = "AAAAAAAAAA";
        private const string UpdatedLastName = "BBBBBBBBBB";

        private const string DefaultEmail = "AAAAAAAAAA";
        private const string UpdatedEmail = "BBBBBBBBBB";

        private const string DefaultPhoneNumber = "AAAAAAAAAA";
        private const string UpdatedPhoneNumber = "BBBBBBBBBB";

        private static readonly DateTime DefaultHireDate = DateTime.UnixEpoch;
        private static readonly DateTime UpdatedHireDate = DateTime.Now;

        private static readonly long? DefaultSalary = 1L;
        private static readonly long? UpdatedSalary = 2L;

        private static readonly long? DefaultCommissionPct = 1L;
        private static readonly long? UpdatedCommissionPct = 2L;

        private readonly NhipsterWebApplicationFactory<TestStartup> _factory;
        private readonly HttpClient _client;

        private readonly ApplicationDatabaseContext _applicationDatabaseContext;

        private Employee _employee;

        private Employee CreateEntity()
        {
            return new Employee {
                FirstName = DefaultFirstName,
                LastName = DefaultLastName,
                Email = DefaultEmail,
                PhoneNumber = DefaultPhoneNumber,
                HireDate = DefaultHireDate,
                Salary = DefaultSalary,
                CommissionPct = DefaultCommissionPct
            };
        }

        private void InitTest()
        {
            _employee = CreateEntity();
        }

        [Fact]
        public async Task CreateEmployee()
        {
            var databaseSizeBeforeCreate = _applicationDatabaseContext.Employees.Count();

            // Create the Employee
            var response = await _client.PostAsync("/api/employees", TestUtil.ToJsonContent(_employee));
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            // Validate the Employee in the database
            var employeeList = _applicationDatabaseContext.Employees.ToList();
            employeeList.Count().Should().Be(databaseSizeBeforeCreate + 1);
            var testEmployee = employeeList[employeeList.Count - 1];
            testEmployee.FirstName.Should().Be(DefaultFirstName);
            testEmployee.LastName.Should().Be(DefaultLastName);
            testEmployee.Email.Should().Be(DefaultEmail);
            testEmployee.PhoneNumber.Should().Be(DefaultPhoneNumber);
            testEmployee.HireDate.Should().Be(DefaultHireDate);
            testEmployee.Salary.Should().Be(DefaultSalary);
            testEmployee.CommissionPct.Should().Be(DefaultCommissionPct);
        }

        [Fact]
        public async Task CreateEmployeeWithExistingId()
        {
            var databaseSizeBeforeCreate = _applicationDatabaseContext.Employees.Count();
            databaseSizeBeforeCreate.Should().Be(0);
            // Create the Employee with an existing ID
            _employee.Id = 1L;

            // An entity with an existing ID cannot be created, so this API call must fail
            var response = await _client.PostAsync("/api/employees", TestUtil.ToJsonContent(_employee));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Validate the Employee in the database
            var employeeList = _applicationDatabaseContext.Employees.ToList();
            employeeList.Count().Should().Be(databaseSizeBeforeCreate);
        }

        [Fact]
        public async Task GetAllEmployees()
        {
            // Initialize the database
            _applicationDatabaseContext.Employees.Add(_employee);
            await _applicationDatabaseContext.SaveChangesAsync();

            // Get all the employeeList
            var response = await _client.GetAsync("/api/employees?sort=id,desc");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.SelectTokens("$.[*].id").Should().Contain(_employee.Id);
            json.SelectTokens("$.[*].firstName").Should().Contain(DefaultFirstName);
            json.SelectTokens("$.[*].lastName").Should().Contain(DefaultLastName);
            json.SelectTokens("$.[*].email").Should().Contain(DefaultEmail);
            json.SelectTokens("$.[*].phoneNumber").Should().Contain(DefaultPhoneNumber);
            json.SelectTokens("$.[*].hireDate").Should().Contain(DefaultHireDate);
            json.SelectTokens("$.[*].salary").Should().Contain(DefaultSalary);
            json.SelectTokens("$.[*].commissionPct").Should().Contain(DefaultCommissionPct);
        }

        [Fact]
        public async Task GetEmployee()
        {
            // Initialize the database
            _applicationDatabaseContext.Employees.Add(_employee);
            await _applicationDatabaseContext.SaveChangesAsync();

            // Get the employee
            var response = await _client.GetAsync($"/api/employees/{_employee.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.SelectTokens("$.id").Should().Contain(_employee.Id);
            json.SelectTokens("$.firstName").Should().Contain(DefaultFirstName);
            json.SelectTokens("$.lastName").Should().Contain(DefaultLastName);
            json.SelectTokens("$.email").Should().Contain(DefaultEmail);
            json.SelectTokens("$.phoneNumber").Should().Contain(DefaultPhoneNumber);
            json.SelectTokens("$.hireDate").Should().Contain(DefaultHireDate);
            json.SelectTokens("$.salary").Should().Contain(DefaultSalary);
            json.SelectTokens("$.commissionPct").Should().Contain(DefaultCommissionPct);
        }

        [Fact]
        public async Task GetNonExistingEmployee()
        {
            var response = await _client.GetAsync("/api/employees/" + long.MaxValue);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateEmployee()
        {
            // Initialize the database
            _applicationDatabaseContext.Employees.Add(_employee);
            await _applicationDatabaseContext.SaveChangesAsync();

            var databaseSizeBeforeUpdate = _applicationDatabaseContext.Employees.Count();

            // Update the employee
            var updatedEmployee =
                await _applicationDatabaseContext.Employees.SingleOrDefaultAsync(it => it.Id == _employee.Id);
            // Disconnect from session so that the updates on updatedEmployee are not directly saved in db
//TODO detach
            updatedEmployee.FirstName = UpdatedFirstName;
            updatedEmployee.LastName = UpdatedLastName;
            updatedEmployee.Email = UpdatedEmail;
            updatedEmployee.PhoneNumber = UpdatedPhoneNumber;
            updatedEmployee.HireDate = UpdatedHireDate;
            updatedEmployee.Salary = UpdatedSalary;
            updatedEmployee.CommissionPct = UpdatedCommissionPct;

            var response = await _client.PutAsync("/api/employees", TestUtil.ToJsonContent(updatedEmployee));
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Validate the Employee in the database
            var employeeList = _applicationDatabaseContext.Employees.ToList();
            employeeList.Count().Should().Be(databaseSizeBeforeUpdate);
            var testEmployee = employeeList[employeeList.Count - 1];
            testEmployee.FirstName.Should().Be(UpdatedFirstName);
            testEmployee.LastName.Should().Be(UpdatedLastName);
            testEmployee.Email.Should().Be(UpdatedEmail);
            testEmployee.PhoneNumber.Should().Be(UpdatedPhoneNumber);
            testEmployee.HireDate.Should().Be(UpdatedHireDate);
            testEmployee.Salary.Should().Be(UpdatedSalary);
            testEmployee.CommissionPct.Should().Be(UpdatedCommissionPct);
        }

        [Fact]
        public async Task UpdateNonExistingEmployee()
        {
            var databaseSizeBeforeUpdate = _applicationDatabaseContext.Employees.Count();

            // If the entity doesn't have an ID, it will throw BadRequestAlertException
            var response = await _client.PutAsync("/api/employees", TestUtil.ToJsonContent(_employee));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Validate the Employee in the database
            var employeeList = _applicationDatabaseContext.Employees.ToList();
            employeeList.Count().Should().Be(databaseSizeBeforeUpdate);
        }

        [Fact]
        public async Task DeleteEmployee()
        {
            // Initialize the database
            _applicationDatabaseContext.Employees.Add(_employee);
            await _applicationDatabaseContext.SaveChangesAsync();

            var databaseSizeBeforeDelete = _applicationDatabaseContext.Employees.Count();

            var response = await _client.DeleteAsync($"/api/employees/{_employee.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Validate the database is empty
            var employeeList = _applicationDatabaseContext.Employees.ToList();
            employeeList.Count().Should().Be(databaseSizeBeforeDelete - 1);
        }

        [Fact]
        public void EqualsVerifier()
        {
            TestUtil.EqualsVerifier(typeof(Employee));
            var employee1 = new Employee {
                Id = 1L
            };
            var employee2 = new Employee {
                Id = employee1.Id
            };
            employee1.Should().Be(employee2);
            employee2.Id = 2L;
            employee1.Should().NotBe(employee2);
            employee1.Id = 0;
            employee1.Should().NotBe(employee2);
        }
    }
}
