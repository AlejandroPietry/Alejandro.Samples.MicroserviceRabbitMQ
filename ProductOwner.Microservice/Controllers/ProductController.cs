using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductOwner.Microservice.Model;
using ProductOwner.Microservice.Services;

namespace ProductOwner.Microservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet("list")]
        public Task<IEnumerable<ProductDetails>> ProductListAsync()
        {
            var productList = _productService.GetProductListAsync();
            return productList;

        }
        [HttpGet("filterlist")]
        public Task<ProductDetails> GetProductByIdAsync(int Id)
        {
            return _productService.GetProductByIdAsync(Id);
        }

        [HttpPost("addproduct")]
        public Task<ProductDetails> AddProductAsync(ProductDetails product)
        {
            var productData = _productService.AddProductAsync(product);
            return productData;
        }

        [HttpPost("sendoffer")]
        public bool SendProductOfferAsync(ProductOfferDetail productOfferDetails)
        {
            bool isSent = false;
            if (productOfferDetails != null)
            {
                isSent = _productService.SendProductOffer(productOfferDetails);

                return isSent;
            }
            return isSent;
        }
    }
}
