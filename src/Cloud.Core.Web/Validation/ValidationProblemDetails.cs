﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using Cloud.Core.Web.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Cloud.Core.Web.Validation
{
    /// <summary>
    /// Validation problem information returned from Api.
    /// </summary>
    public class ValidationProblemDetails : Microsoft.AspNetCore.Mvc.ValidationProblemDetails
    {
        /// <summary>
        /// Constructor, build using status code.
        /// </summary>
        /// <param name="context">Calling controller context.</param>
        /// <param name="statusCode">Http status code of response.</param>
        public ValidationProblemDetails(ControllerContext context, HttpStatusCode statusCode) : base(context.ModelState)
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
            Title = "One or more model validation errors occurred.";
            Status = (int)statusCode;
            Detail = "See the errors property for details";
            Instance = context.HttpContext.Request.Path;
        }

        /// <summary>
        /// Contructor, built using ModelState.
        /// </summary>
        /// <param name="modelState">Model state used to build instance.</param>
        public ValidationProblemDetails(ModelStateDictionary modelState) : base(modelState) { }

        /// <summary>
        /// Contructor, built using error list.
        /// </summary>
        /// <param name="errors">Error list used to build instance.</param>
        public ValidationProblemDetails(IDictionary<string, string[]> errors) : base(errors) { }

        /// <summary>
        /// Constructor, build problem details using an exception.
        /// </summary>
        /// <param name="message">Top level error message.</param>
        /// <param name="ex">Exception to parse.</param>
        public ValidationProblemDetails(string message, Exception ex = null) : base() 
        {
            var errors = new List<string>();
            var currentEx = ex;

            if (currentEx != null)
            {
                do
                {
                    errors.Add(currentEx.Message);
                    currentEx = currentEx.InnerException;
                } while (currentEx != null);
            }
            base.Errors.Add(message, errors.ToArray());
        }

        /// <summary>
        /// Gets the validation errors associated with this instance of <see cref="ValidationProblemDetails"/>.
        /// </summary>
        [JsonIgnore]
        public new IDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>(StringComparer.Ordinal);
        
        /// <summary>
        /// Gets the validation errors associated with this instance of <see cref="ValidationProblemDetails"/>.
        /// </summary>
        [JsonPropertyName("errors")]
        [JsonPropertyOrder(1)]
        public IDictionary<string, ErrorItem[]> ErrorItems => GetErrors();

        private IDictionary<string, ErrorItem[]> GetErrors()
        {
            var returnItems = new Dictionary<string, ErrorItem[]>();
            foreach (var err in base.Errors)
            {
                returnItems.Add(err.Key, err.Value.Select(e => ErrorItem.Parse(err.Key, e)).ToArray());
            }

            return returnItems;
        }

    }

    /// <summary>
    /// Error item class.
    /// </summary>
    public class ErrorItem
    {
        /// <summary>Error code string.</summary>
        public string Code { get; }
        
        /// <summary>Error message string.</summary>
        public string Message { get; }

        /// <summary>
        /// Constructor, build using error code and message.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public ErrorItem(string code, string message)
        {
            Code = code;
            Message = message;
        }

        internal static ErrorItem Parse(string key, string parseMessage)
        {
            var parts = parseMessage.Split('|');
            if (int.TryParse(parts[0], out _))
            {
                var message = parts.Length > 1 ? string.Join('|', parts.Skip(1)) : $"{key} error occurred";
                return new ErrorItem(parts[0], message);
            }

            return new ErrorItem("0000", parseMessage);
        }
    }
}
