using System;
using System.Text.Json;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ContactsFunction
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Function1))]
        public async Task Run([QueueTrigger("contacts", Connection = "AzureWebJobsStorage")] QueueMessage message)
        {
            _logger.LogInformation($"C# Queue trigger function processed: {message.MessageText}");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            string json = message.MessageText;

            Contact? weatherForecast = JsonSerializer.Deserialize<Contact>(json, options);

            Contact? contact = JsonSerializer.Deserialize<Contact>(json);

            if(contact == null) 
            {
                _logger.LogError("failed to the message");
                return;
            }

            _logger.LogInformation($"Hello {contact.FirstName}");

            // get connection string from app settings
            string? connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("SQL connection string is not set in the environment variables.");
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync(); // Note the ASYNC

                var query = "INSERT INTO Contacts(FirstName, LastName, Email) VALUES(@FirstName, @LastName, @Email)"

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FirstName", contact.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", contact.FirstName);
                    cmd.Parameters.AddWithValue("@Email", contact.Email);

                    await cmd.ExecuteNonQueryAsync();
                }
            }

            _logger.LogInformation("Contact added to database");
        }

    }
}
