﻿namespace Pos_System.API.Middlewares;

using System.Net;

using Microsoft.AspNetCore.Mvc;

using Pos_System.API.Models.Response;

public class ExceptionHandlingMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<ExceptionHandlingMiddleware> _logger;
	public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		try
		{
			await _next(context);
		}
		catch (Exception ex)
		{
			await HandleExceptionAsync(context, ex);
		}
	}

	private async Task HandleExceptionAsync(HttpContext context, Exception exception)
	{
		context.Response.ContentType = "application/json";
		var response = context.Response;

		var errorResponse = new ErrorResponse() { TimeStamp = DateTime.UtcNow, Error = exception.Message };
		switch (errorResponse)
		{
			//add more custom exception
			//For example case AppException: do something 
			default:
				//unhandled error
				errorResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
				_logger.LogError(exception.ToString());
				break;
		}
		var result = errorResponse.ToString();
		await context.Response.WriteAsync(result);
	}
}