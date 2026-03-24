using System;

namespace APICore.Services.Exceptions
{
    public class CustomBaseException : Exception
    {
        public int HttpCode { get; set; }
        public int CustomCode { get; set; }
        public string CustomMessage { get; set; }

        public CustomBaseException() : base()
        {
        }

        /// <summary>
        /// Permite que <see cref="Exception.Message"/> devuelva el mismo texto que <see cref="CustomMessage"/> al registrar o hacer catch.
        /// </summary>
        public CustomBaseException(string message) : base(message)
        {
        }
    }
}