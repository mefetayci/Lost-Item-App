using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// API servislerini ekliyoruz
builder.Services.AddControllers();
// Veritabanı bağlantımızı (DbContext) sisteme tanıtıyoruz
builder.Services.AddDbContext<CampusLostAndFound.API.Data.AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Swagger servisini ekledik

var app = builder.Build();

// Eğer geliştirme aşamasındaysak Swagger arayüzünü aç
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.UseStaticFiles(); // wwwroot klasörünü dışarı açar


app.Run();