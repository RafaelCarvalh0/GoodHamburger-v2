using GoodHamburger.Helpers;
using GoodHamburger.Models.Product;
using GoodHamburger.ViewModels.Order;
using Microsoft.AspNetCore.Mvc;

namespace GoodHamburger.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly ILogger<OrderController> _logger;
        private readonly IOrderManager _repo;

        public OrderController(ILogger<OrderController> logger, IOrderManager repo)
        {
            _logger = logger;
            _repo = repo;
        }

        // POST: api/Order/SendOrder
        /// <summary>
        /// Creates an order.
        /// </summary>
        /// <param></param>
        /// <returns>Return the amount that will be charged to the customer</returns>
        /// <response code="200">Ok</response>
        /// <response code="400">Bad Request</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> SendOrder(Products products)
        {
            try
            {
                decimal total = await _repo.SendOrderValidator(products);
                return Ok(total);
            }
            catch (CustomException ex)
            {
                _logger.LogError($"Error while creating the order: {ex.Message}");
                return StatusCode(ex.StatusCode, ex.Message);
            }
        }

        // GET: api/Order/GetOrder
        /// <summary>
        /// Retrieves all orders.
        /// </summary>
        /// <param></param>
        /// <returns>Returns a list of orders.</returns>
        /// <response code="200">Ok</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult> GetOrder()
        {
            try
            {
                List<OrderViewModel> orderList = await _repo.GetOrderValidator();
                return Ok(orderList);
            }
            catch (CustomException ex)
            {
                _logger.LogError($"Error while fetching orders: {ex.Message}");
                return StatusCode(ex.StatusCode, ex.Message);
            }
        }

        // PUT: api/Order/UpdateOrder/OrderId
        /// <summary>
        /// Updates an order.
        /// </summary>
        /// <param name="OrderId"></param>
        /// <returns>Returns true if the order was successfully updated; otherwise, false.</returns>
        /// <response code="200">Ok</response>
        /// <response code="400">Bad Request</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPut]
        [Route("[action]/{OrderId}")]
        public async Task<ActionResult> UpdateOrder(int OrderId, Products products)
        {
            try
            {
                await _repo.UpdateOrderValidator(OrderId, products);
                return Ok("Order updated successfully");             
            }
            catch (CustomException ex)
            {
                _logger.LogError($"Error while updating the order: {ex.Message}");
                return StatusCode(ex.StatusCode, ex.Message);
            }
        }

        // DELETE: api/Order/RemoveOrder/OrderId
        /// <summary>
        /// Removes an order.
        /// </summary>
        /// <param name="OrderId">The ID of the order to be removed.</param>
        /// <returns>Returns true if the order was successfully removed; otherwise, false.</returns>
        /// <response code="200">Ok</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [HttpDelete]
        [Route("[action]/{OrderId}")]
        public async Task<ActionResult> RemoveOrder(int OrderId)
        {
            try
            {
                await _repo.DeleteOrderValidator(OrderId);
                return Ok("Order removed successfully");
            }
            catch (CustomException ex)
            {
                _logger.LogError($"Error while removing the order: {ex.Message}");
                return StatusCode(ex.StatusCode, ex.Message);
            }
        }
    }
}
