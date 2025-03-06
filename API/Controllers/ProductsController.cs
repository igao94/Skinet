using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IGenericRepository<Product> repo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetProducts(string? brand,
        string? type,
        string? sort)
    {
        var spec = new ProductSpecification(brand, type, sort);

        return Ok(await repo.ListAllWithSpecAsync(spec));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await repo.GetByIdAsync(id);

        if (product is null) return NotFound();

        return product;
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        repo.Add(product);

        var result = await repo.SaveAllAsync();

        return result
            ? CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product)
            : BadRequest("Failed to create product.");
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateProduct(int id, Product product)
    {
        if (product.Id != id || repo.Exists(id))
            return BadRequest("Can't update this product.");

        repo.Update(product);

        var result = await repo.SaveAllAsync();

        return result
            ? NoContent()
            : BadRequest("Failed to update product.");
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        var product = await repo.GetByIdAsync(id);

        if (product is null) return NotFound();

        repo.Remove(product);

        var result = await repo.SaveAllAsync();

        return result
            ? NoContent()
            : BadRequest("Failed to delete product.");
    }

    [HttpGet("brands")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetBrands()
    {
        var spec = new BrandListSpecification();

        return Ok(await repo.ListAllWithSpecAsync(spec));
    }

    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetTypes()
    {
        var spec = new TypeListSpecification();

        return Ok(await repo.ListAllWithSpecAsync(spec));
    }
}
