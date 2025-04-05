using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<RecordServiceDbContext>(options =>
  options.UseInMemoryDatabase("InMemoryDb"));

var app = builder.Build();

app.UseMiddleware<DbLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/records", (List<Dictionary<int, string>> values, RecordServiceDbContext dbContext) =>
  {
    List<Record> records;
    try
    {
      records = values
        .SelectMany(v => v)
        .Select(v => new Record(v.Key, v.Value))
        .OrderBy(r => r.Code)
        .ToList();
    }
    catch (Exception)
    {
      throw new BadHttpRequestException("Incorrect input provided.");
    }

    dbContext.Records.RemoveRange(dbContext.Records.ToList());
    dbContext.Records.AddRange(records);
    dbContext.SaveChanges();

    return dbContext.Records.ToList();
  })
  .WithName("CreateRecords")
  .WithOpenApi();

app.MapGet("/records", (int? code, string? valueIncludes, int? takeCount, int? skipCount, RecordServiceDbContext dbContext) =>
  {
    var records = dbContext.Records.AsNoTracking();
    if (code.HasValue)
    {
      records = records.Where(r => r.Code == code.Value);
    }

    if (!string.IsNullOrEmpty(valueIncludes))
    {
      records = records.Where(r => r.Value.Contains(valueIncludes));
    }

    if (skipCount.HasValue)
    {
      records = records.Skip(skipCount.Value);
    }

    if (takeCount.HasValue)
    {
      records = records.Take(takeCount.Value);
    }

    return records
      .ToList();
  })
  .WithName("GetRecords")
  .WithOpenApi();

app.MapGet("/logs", (RecordServiceDbContext dbContext) => dbContext.Logs.ToListAsync())
  .WithName("GetLogs")
  .WithOpenApi();

app.Run();

[PrimaryKey(nameof(Id))]
public class Record(int code, string value)
{
  public int Id { get; init; }
  public int Code { get; set; } = code;
  public string Value { get; set; } = value;
}

[PrimaryKey(nameof(Id))]
public class HttpLog
{
  public int Id { get; set; }
  public DateTime Timestamp { get; set; } = DateTime.UtcNow;

  public string Method { get; set; } = default!;
  public string Path { get; set; } = default!;
  public string? QueryString { get; set; }
  public string? RequestBody { get; set; }
  public int StatusCode { get; set; }
  public string? ResponseBody { get; set; }
}

public class RecordServiceDbContext(DbContextOptions<RecordServiceDbContext> options) : DbContext(options)
{
  public DbSet<Record> Records => Set<Record>();
  public DbSet<HttpLog> Logs => Set<HttpLog>();
}

public class DbLoggingMiddleware(RequestDelegate next)
{
  public async Task Invoke(HttpContext context, RecordServiceDbContext db)
  {
    if (!context.Request.Path.StartsWithSegments("/records", StringComparison.OrdinalIgnoreCase))
    {
      await next(context);
      return;
    }

    context.Request.EnableBuffering();
    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
    context.Request.Body.Position = 0;
    var originalBodyStream = context.Response.Body;
    using var responseBodyStream = new MemoryStream();
    context.Response.Body = responseBodyStream;

    await next(context);

    context.Response.Body.Seek(0, SeekOrigin.Begin);
    var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
    context.Response.Body.Seek(0, SeekOrigin.Begin);

    db.Logs.Add(new HttpLog
    {
      Method = context.Request.Method,
      Path = context.Request.Path,
      QueryString = context.Request.QueryString.ToString(),
      RequestBody = requestBody,
      StatusCode = context.Response.StatusCode,
      ResponseBody = responseBody
    });

    await db.SaveChangesAsync();

    await responseBodyStream.CopyToAsync(originalBodyStream);
  }
}
