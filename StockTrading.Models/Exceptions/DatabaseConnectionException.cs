using System;

namespace StockTrading.Models.Exceptions
{
    public class DatabaseConnectionException : Exception
    {
        public DatabaseConnectionException() 
            : base("A database connection error occurred.") { }

        public DatabaseConnectionException(string message) 
            : base(message) { }

        public DatabaseConnectionException(string message, Exception innerException) 
            : base(message, innerException) { }
            
        public string? ConnectionString { get; set; }
        public string? DatabaseName { get; set; }
    }
}