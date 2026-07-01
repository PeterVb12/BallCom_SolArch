using Microsoft.EntityFrameworkCore;

namespace BallCom.Ordering.API.Data
{
    public static class OrderingDbInitializer
    {
        public static async Task InitializeAsync(OrderingDbContext context)
        {
            await context.Database.EnsureCreatedAsync();

            // EnsureCreated werkt niet op bestaande volumes; voeg ontbrekende kolommen toe (F05).
            await context.Database.ExecuteSqlRawAsync("""
                ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "CustomerEmail" text NOT NULL DEFAULT '';
                ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "CustomerName" text NOT NULL DEFAULT '';
                ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "Street" text NOT NULL DEFAULT '';
                ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "City" text NOT NULL DEFAULT '';
                ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "PostalCode" text NOT NULL DEFAULT '';
                ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "Country" text NOT NULL DEFAULT '';
                """);
        }
    }
}
