using GoodHamburger.Helpers;
using GoodHamburger.Models.Order;
using GoodHamburger.Models.Product;
using GoodHamburger.ViewModels.Order;
using Microsoft.EntityFrameworkCore;

namespace GoodHamburger
{
    public interface IOrderManager
    {
        Task<decimal> SendOrderValidator(Products order);
        Task<List<OrderViewModel>> GetOrderValidator();
        Task UpdateOrderValidator(int OrderId, Products order);
        Task DeleteOrderValidator(int OrderId);
    }

    public class OrderManager : IOrderManager
    {
        private readonly ILogger<OrderManager> _logger;
        private readonly ApplicationDbContext _context;

        public OrderManager(ApplicationDbContext context, ILogger<OrderManager> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region SendOrder
        public async Task<decimal> SendOrderValidator(Products Products)
        {
            try
            {
                ValidateProducts(Products);
                Sandwich Sandwich = await GetSandwichAsync(Products.sandwiches.First().Id);
                List<Extra> Extras = await GetExtrasAsync(Products.extras);
                decimal Total = CalculateTotal(Sandwich, Extras);
                await CreateOrderAsync(Extras, Sandwich.Id);

                return Total;
            }
            catch (CustomException ex)
            {
                throw new CustomException(ex.Message, StatusCodes.Status500InternalServerError);
            }       
        }

        private void ValidateProducts(Products Products)
        {
            if (Products.sandwiches.Count > 1)
                throw new CustomException("You should choose only one sandwich!", StatusCodes.Status400BadRequest);

            else if (Products.extras.Count > 2)
                throw new CustomException("You should choose only two extra!", StatusCodes.Status400BadRequest);

            else if (Products.extras.GroupBy(e => e.Id).Any(e => e.Count() > 1))
                throw new CustomException("You cannot select more than one soda or one fries!", StatusCodes.Status400BadRequest);
        }

        private async Task<Sandwich> GetSandwichAsync(int SandwichId)
        {
            try
            {
                Sandwich Sandwich = await _context.Sandwich.FindAsync(SandwichId);

                if (Sandwich is null)
                    throw new CustomException("Invalid sandwich", StatusCodes.Status400BadRequest);

                return Sandwich;
            }
            catch (CustomException ex)
            {
                throw new CustomException(ex.Message, StatusCodes.Status500InternalServerError);
            }           
        }

        private async Task<List<Extra>> GetExtrasAsync(List<Extra> Extras)
        {
            try
            {
                List<Extra> ExistingExtras = await _context.Extra.Where(e => Extras.Select(extra => extra.Id).Contains(e.Id)).ToListAsync();

                if (ExistingExtras.Count is 0)
                    throw new CustomException("Invalid extra", StatusCodes.Status400BadRequest);

                else if (Extras.Count > ExistingExtras.Count)
                    throw new CustomException("Your order contains an invalid extra!", StatusCodes.Status400BadRequest);

                return ExistingExtras;
            }
            catch (CustomException ex)
            {
                throw new CustomException(ex.Message, StatusCodes.Status500InternalServerError);
            }           
        }

        private decimal CalculateTotal(Sandwich Sandwich, List<Extra> Extras)
        {
            decimal SandwichPrice = Sandwich.Price;
            decimal ExtrasPrice = Extras.Sum(extra => extra.Price);

            if (Extras.Count == 2)
                return (SandwichPrice + ExtrasPrice) * 0.8M;

            else if (Extras.First().Id == 1)
                return (SandwichPrice + ExtrasPrice) * 0.9M;

            else
                return (SandwichPrice + ExtrasPrice) * 0.85M;
        }

        private async Task CreateOrderAsync(List<Extra> Extras, int SandwichId)
        {
            try
            {
                var Order = new Order();
                _context.Order.Add(Order);

                await _context.SaveChangesAsync();

                foreach (var Extra in Extras)
                {
                    var OrderItem = new OrderItem
                    {
                        OrderId = Order.Id,
                        SandwichId = SandwichId,
                        ExtraId = Extra.Id,
                    };
                    _context.OrderItem.Add(OrderItem);
                }
                await _context.SaveChangesAsync();
            }
            catch (CustomException ex)
            {
                throw new CustomException(ex.Message, StatusCodes.Status500InternalServerError);
            }           
        }

        #endregion

        #region GetOrder
        public async Task<List<OrderViewModel>> GetOrderValidator()
        {
            try
            {
                List<int> OrderIds = await _context.Order.Select(o => o.Id).ToListAsync();
                List<OrderViewModel> OrderList = new List<OrderViewModel>();

                foreach (var id in OrderIds)
                {
                    var SandwichId = _context.OrderItem.Where(oi => oi.OrderId == id).Select(oi => oi.SandwichId).First();
                    var ExtraIds = await _context.OrderItem.Where(oi => oi.OrderId == id).Select(oi => oi.ExtraId).ToListAsync();
                    var Sandwich = await _context.Sandwich.Where(s => s.Id == SandwichId).Distinct().ToListAsync();
                    var Extra = await _context.Extra.Where(e => ExtraIds.Contains(e.Id)).ToListAsync();

                    OrderViewModel Order = new OrderViewModel()
                    {
                        OrderId = id,
                        sandwich = Sandwich,
                        extra = Extra
                    };

                    OrderList.Add(Order);
                }

                return OrderList;
            }
            catch (CustomException ex)
            {
                throw new CustomException(ex.Message, StatusCodes.Status500InternalServerError);
            }
            
        }
        #endregion

        #region UpdateOrder
        public async Task UpdateOrderValidator(int OrderId, Products Products)
        {
            try
            {
                await FindOrderById(OrderId, Products);
                await RemoveOrderById(OrderId);
                await UpdateOrderAsync(OrderId, Products);
            }
            catch (CustomException ex)
            {
                new CustomException(ex.Message, ex.StatusCode);
            }       
        }

        private async Task FindOrderById(int OrderId, Products Products)
        {
            try
            {
                Order Order = await _context.Order.FindAsync(OrderId);
                if (Order is null)
                    throw new CustomException("Order not found", StatusCodes.Status400BadRequest);

                Sandwich sandwich = await _context.Sandwich.FindAsync(Products.sandwiches.First().Id);
                if (sandwich is null)
                    throw new CustomException("Invalid sandwich", StatusCodes.Status400BadRequest);

                List<Extra> extras = await _context.Extra.Where(e => Products.extras.Select(extra => extra.Id).Contains(e.Id)).ToListAsync();
                if (extras.Count is 0)
                    throw new CustomException("Invalid extra", StatusCodes.Status400BadRequest);
            }
            catch (CustomException ex)
            {
                throw new CustomException(ex.Message, StatusCodes.Status500InternalServerError);
            }
            
        }

        private async Task RemoveOrderById(int OrderId)
        {
            try
            {
                List<OrderItem> Order = await _context.OrderItem.Where(oi => oi.OrderId == OrderId).ToListAsync();
                _context.OrderItem.RemoveRange(Order);
            }
            catch (CustomException ex)
            {
                throw new CustomException(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }

        private async Task UpdateOrderAsync(int OrderId, Products Products)
        {
            try
            {
                foreach (var extra in Products.extras)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = OrderId,
                        SandwichId = Products.sandwiches.First().Id,
                        ExtraId = extra.Id,
                    };
                    _context.OrderItem.Add(orderItem);
                }

                await _context.SaveChangesAsync();
            }
            catch (CustomException ex)
            {
                throw new CustomException(ex.Message, StatusCodes.Status500InternalServerError);
            }         
        }
        #endregion

        #region DeleteOrder
        public async Task DeleteOrderValidator(int OrderId)
        {
            try
            {
                Order Order = await _context.Order.FindAsync(OrderId) ?? 
                    throw new CustomException("Order not found", StatusCodes.Status400BadRequest);

                _context.Order.Remove(Order);
                await _context.SaveChangesAsync();
            }
            catch (CustomException ex)
            {
                throw new CustomException(ex.Message, StatusCodes.Status500InternalServerError);
            }

        }
        #endregion
    }
}

