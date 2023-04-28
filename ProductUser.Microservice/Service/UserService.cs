using Microsoft.EntityFrameworkCore;
using ProductUser.Microservice.Data;
using ProductUser.Microservice.Model;
using System.IO.Compression;
using System.Text;

namespace ProductUser.Microservice.Service
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ProductOfferDetail> AddProductAsync(ProductOfferDetail product)
        {
            var result = _context.ProductOffers.Add(product);
            await _context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<ProductOfferDetail> GetProductByIdAsync(int id)
        {
            return await _context.ProductOffers.FindAsync(id);
        }

        public async Task<IEnumerable<ProductOfferDetail>> GetProductListAsync()
        {
            return await _context.ProductOffers.ToListAsync();
        }
    }
}
