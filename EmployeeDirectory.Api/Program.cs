using EmployeeDirectory.Api.Data;
using EmployeeDirectory.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

//add swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString= builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<EmployeeDBContext>(options =>
    options.UseSqlServer(connectionString));

//inject employee interface
builder.Services.AddScoped<IEmployeeService, EmployeeService>();


var app = builder.Build();

//code first db migration and update db
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<EmployeeDBContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.

//enable swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
