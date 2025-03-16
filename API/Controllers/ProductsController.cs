using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class ProductsController(IUnitOfWork unitOfWork) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetProducts([FromQuery] ProductSpecParams specParams)
    {
        var spec = new ProductSpecification(specParams);

        return await CreatePagedResultAsync(unitOfWork.Repository<Product>(),
            spec,
            specParams.PageIndex,
            specParams.PageSize);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await unitOfWork.Repository<Product>().GetByIdAsync(id);

        if (product is null) return NotFound();

        return product;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        unitOfWork.Repository<Product>().Add(product);

        var result = await unitOfWork.CompleteAsync();

        return result
            ? CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product)
            : BadRequest("Failed to create product.");
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateProduct(int id, Product product)
    {
        if (product.Id != id || !unitOfWork.Repository<Product>().Exists(id))
            return BadRequest("Can't update this product.");

        unitOfWork.Repository<Product>().Update(product);

        var result = await unitOfWork.CompleteAsync();

        return result
            ? NoContent()
            : BadRequest("Failed to update product.");
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        var product = await unitOfWork.Repository<Product>().GetByIdAsync(id);

        if (product is null) return NotFound();

        unitOfWork.Repository<Product>().Remove(product);

        var result = await unitOfWork.CompleteAsync();

        return result
            ? NoContent()
            : BadRequest("Failed to delete product.");
    }

    [HttpGet("brands")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetBrands()
    {
        var spec = new BrandListSpecification();

        return Ok(await unitOfWork.Repository<Product>().ListAllWithSpecAsync(spec));
    }

    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetTypes()
    {
        var spec = new TypeListSpecification();

        return Ok(await unitOfWork.Repository<Product>().ListAllWithSpecAsync(spec));
    }
}
