﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestEase.SampleWebApi.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RestEase.SampleWebApi.Infrastructure
{
	public class FileContentModelBinder : IModelBinder
	{
		public async Task BindModelAsync(ModelBindingContext bindingContext)
		{
			if (bindingContext == null)
			{
				throw new System.ArgumentNullException(nameof(bindingContext));
			}

			var request = bindingContext.HttpContext.Request;
			var downloadable = await FromForm(request) ?? await FromBody(request);
			bindingContext.Result = ModelBindingResult.Success(downloadable);
		}

		private async Task<FileContent> FromForm(HttpRequest request)
		{
			// transfer from formfile
			if (!request.HasFormContentType)
			{
				return null;
			}

			var formFile = request.Form.Files.FirstOrDefault();

			using (var stream = new MemoryStream())
			{
				await formFile.CopyToAsync(stream);
				return new FileContent
				{
					FileName = formFile.FileName,
					Content = stream.ToArray()
				};
			}
		}

		private async Task<FileContent> FromBody(HttpRequest request)
		{
			// transfer from body and headers
			var headers = request.GetTypedHeaders();
			using (var stream = new MemoryStream())
			{
				await request.Body.CopyToAsync(stream);
				var fileName = headers?.ContentDisposition?.FileName.Value;

				// Убираем кавычки из content-disposition
				if (fileName != null && fileName.Length > 2 && fileName.StartsWith("\"") && fileName.EndsWith("\""))
				{
					fileName = fileName.Substring(1, fileName.Length - 2);
				}

				return new FileContent
				{
					FileName = fileName,
					Content = stream.ToArray(),
					MimeType = headers?.ContentType?.MediaType.Value
				};
			}
		}
	}
}
