using Cache.WebAPI.Context;
using Cache.WebAPI.Models;
using EntityFrameworkCorePagination.Nuget.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseInMemoryDatabase("MyDb");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

var scoped = builder.Services.BuildServiceProvider();
ApplicationDbContext context = scoped.GetRequiredService<ApplicationDbContext>();
IMemoryCache memoryCache = scoped.GetRequiredService<IMemoryCache>();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("SeedData", () =>
{
    List<Product> products = new List<Product>();
    for (int i = 0; i < 100000; i++)
    {
        Product product = new()
        {
            Name = "Product " + i
        };

        products.Add(product); 
    }

    context.Products.AddRange(products);
    context.SaveChanges();

    return new { Message = "Product SeedData is successful" };
});
app.MapGet("GetAllProducts", async (CancellationToken cancellationToken) =>
{
    List<Product>? products;

    memoryCache.TryGetValue("products", out products);

    if(products is null)
    {
        products = await context.Products.ToListAsync(cancellationToken);

        memoryCache.Set("products", products, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
        });
    }
     
    return products.Count();
});

app.MapGet("GetAllProductsWithPagination", async (int pageNumber, int pageSize, CancellationToken cancellationToken = default) => 
{
    //List<Product> products = await context.Products.Skip(pageSize * pageNumber).Take(pageSize).ToListAsync(cancellationToken);
    //decimal count  = await context.Products.CountAsync(cancellationToken);
    //decimal totalPageNumbers = Math.Ceiling(Convert.ToDecimal(count / pageSize));
    //bool isFirstPage = pageNumber == 1 ? true : false;
    //bool isLastPage = pageNumber == totalPageNumbers ? true : false;


    ////Pagination yapýsý
    //var reponse = new
    //{
    //    Data = products,
    //    Count = count,
    //    TotalPageNumbers = totalPageNumbers,
    //    IsFirstPage = isFirstPage,
    //    IsLastPage = isLastPage,
    //    PageNumber = pageNumber,
    //    PageSize = pageSize
    //};
    PaginationResult<Product> pageProducts = await context.Products.ToPagedListAsync(pageNumber, pageSize, cancellationToken);

    return pageProducts;
});

app.Run();

