// Puxa todas as configs para a aplicação rodar
var builder = WebApplication.CreateBuilder(args);

// Configura a dependencia - import openApi
builder.Services.AddOpenApi();
// Monta a aplicação
var app = builder.Build();

// Executa a dependencia de doc
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// middleware
// app.UseHttpsRedirection();

//endpoints
app.MapGet("/", () =>
{
    var forecast =  "banana";
    return forecast;
})
.WithName("Home");

app.Run();
