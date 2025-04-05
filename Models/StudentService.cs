using Microsoft.Azure.Cosmos;
using Secure_Student_Management_System.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Secure_Student_Management_System.Models
{
    public class StudentService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;

        public StudentService(CosmosClient cosmosClient, IConfiguration configuration)
        {
            _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
            // Read container name from configuration:
            _container = _cosmosClient.GetContainer(
                configuration["CosmosDb:Database"],
                configuration["CosmosDb:Container"] // Matches appsettings.json
            );
        }

        public async Task<Student?> GetStudentByIdAsync(string id)
        {
            try
            {
                ItemResponse<Student> response = await _container.ReadItemAsync<Student>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex)
            {
                Console.WriteLine($"Error fetching student with id {id}: {ex.Message}");
                return null;
            }
        }

        public async Task CreateStudentAsync(Student student)
        {
            try
            {
                await _container.CreateItemAsync(student, new PartitionKey(student.Id));
            }
            catch (CosmosException ex)
            {
                Console.WriteLine($"Error creating student {student.Id}: {ex.Message}");
            }
        }

        public async Task UpdateStudentAsync(Student student)
        {
            try
            {
                await _container.UpsertItemAsync(student, new PartitionKey(student.Id));
            }
            catch (CosmosException ex)
            {
                Console.WriteLine($"Error updating student {student.Id}: {ex.Message}");
            }
        }

        public async Task DeleteStudentAsync(string id)
        {
            try
            {
                await _container.DeleteItemAsync<Student>(id, new PartitionKey(id));
            }
            catch (CosmosException ex)
            {
                Console.WriteLine($"Error deleting student with id {id}: {ex.Message}");
            }
        }

        // New method for searching students with pagination
        public async Task<List<Student>> SearchStudentsAsync(string searchQuery, int pageSize = 10, int pageNumber = 1)
        {
            var query = $"SELECT * FROM c WHERE CONTAINS(LOWER(c.FirstName), @searchQuery) OR CONTAINS(LOWER(c.LastName), @searchQuery) OR CONTAINS(LOWER(c.Email), @searchQuery)";
            var queryDefinition = new QueryDefinition(query).WithParameter("@searchQuery", searchQuery.ToLower());

            // Set the request options with page size
            var requestOptions = new QueryRequestOptions { MaxItemCount = pageSize };

            var iterator = _container.GetItemQueryIterator<Student>(queryDefinition, requestOptions: requestOptions);
            var students = new List<Student>();

            // Pagination setup
            int skipCount = (pageNumber - 1) * pageSize;

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                students.AddRange(response);
            }

            return students.Skip(skipCount).Take(pageSize).ToList();
        }

    }
}
