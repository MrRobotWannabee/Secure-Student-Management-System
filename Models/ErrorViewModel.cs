namespace Secure_Student_Management_System.Models
{
    public class ErrorViewModel
    {
        // Unique identifier for the request
        public string? RequestId { get; set; }

        // Indicates whether the RequestId should be shown
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        // Detailed error message (for debugging or logging)
        public string? ErrorMessage { get; set; }

        // Stack trace for more detailed error debugging (for developers)
        public string? StackTrace { get; set; }

        // A friendly error message displayed to users (to avoid technical jargon)
        public string UserFriendlyMessage { get; set; } = "An unexpected error occurred. Please try again later.";

        // Error type or status code (e.g., 404, 500, etc.)
        public int StatusCode { get; set; }

        // A flag to indicate if the error is critical (for logging or monitoring purposes)
        public bool IsCritical { get; set; }

        // Date and time of the error occurrence
        public DateTime ErrorDateTime { get; set; } = DateTime.Now;

        // Additional contextual information (like endpoint, controller name, etc.)
        public string? ErrorContext { get; set; }

        // Method to format the error for easier readability or debugging
        public string GetFormattedError()
        {
            return $"Error at {ErrorDateTime} (Status Code: {StatusCode}): {ErrorMessage}\nStackTrace: {StackTrace}";
        }
    }
}
