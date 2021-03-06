using System;
using System.Threading;
using System.Threading.Tasks;
using Samples.HelloCart.V2;
using Stl.Async;
using Stl.Fusion;
using Stl.Fusion.EntityFramework;

namespace Samples.HelloCart.V3
{
    public class DbProductService2 : DbServiceBase<AppDbContext>, IProductService
    {
        private readonly DbEntityResolver<AppDbContext, string, DbProduct> _productResolver;

        public DbProductService2(
            IServiceProvider services,
            DbEntityResolver<AppDbContext, string, DbProduct> productResolver)
            : base(services)
            => _productResolver = productResolver;

        public virtual async Task EditAsync(EditCommand<Product> command, CancellationToken cancellationToken = default)
        {
            var (productId, product) = command;
            if (string.IsNullOrEmpty(productId))
                throw new ArgumentOutOfRangeException(nameof(command));
            if (Computed.IsInvalidating()) {
                FindAsync(productId, default).Ignore();
                return;
            }

            await using var dbContext = await CreateCommandDbContextAsync(cancellationToken);
            var dbProduct = await dbContext.Products.FindAsync(ComposeKey(productId), cancellationToken);
            if (product == null) {
                if (dbProduct != null)
                    dbContext.Remove(dbProduct);
            }
            else {
                if (dbProduct != null)
                    dbProduct.Price = product.Price;
                else
                    dbContext.Add(new DbProduct { Id = productId, Price = product.Price });
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task<Product?> FindAsync(string id, CancellationToken cancellationToken = default)
        {
            var dbProduct = await _productResolver.TryGetAsync(id, cancellationToken);
            if (dbProduct == null)
                return null;
            return new Product() { Id = dbProduct.Id, Price = dbProduct.Price };
        }
    }
}
