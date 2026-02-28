using System;
using System.Collections.Generic;
using System.Linq;

namespace SieMarketStore
{
    public class Customer
    {
        public int CustomerId { get; private set; }
        public string Name { get; private set; }

        public Customer(int customerId, string name)
        {
            if (customerId <= 0)
                throw new ArgumentOutOfRangeException(nameof(customerId), "Customer ID must be greater than zero!");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Customer name cannot be empty!", nameof(name));

            this.CustomerId = customerId;
            this.Name = name;
        }
    }

    public class OrderItem
    {
        public string ProductName { get; private set; }
        public int Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }

        public OrderItem(string productName, int quantity, decimal unitPrice)
        {
            if (string.IsNullOrWhiteSpace(productName))
                throw new ArgumentException("Product name is required!", nameof(productName));

            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than 0!");

            if (unitPrice < 0)
                throw new ArgumentOutOfRangeException(nameof(unitPrice), "Price cannot be negative!");

            ProductName = productName.Trim();
            Quantity = quantity;
            UnitPrice = unitPrice;
        }

        public decimal TotalPrice
        {
            get
            {
                return Quantity * UnitPrice;
            }
        }
    }

    public class Order
    {
        public int OrderId { get; private set; }
        public Customer Customer { get; private set; }
        private const decimal DiscountThreshold = 500m;
        private const decimal DiscountRate = 0.10m;

        private readonly List<OrderItem> _items;

        public IReadOnlyList<OrderItem> Items => _items;

        public Order(int orderId, Customer customer)
        {
            if (orderId <= 0)
                throw new ArgumentOutOfRangeException(nameof(orderId), "Order ID must be greater than zero!");

            if (customer == null)
                throw new ArgumentNullException(nameof(customer), "Order must have a valid customer!");

            OrderId = orderId;
            Customer = customer;

            _items = new List<OrderItem>();
        }

        public void AddItem(OrderItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item), "Cannot add a null item to the order!");

            _items.Add(item);
        }

        public decimal CalculateFinalPrice()
        {
            decimal totalValue = _items.Sum(item => item.TotalPrice);
            if (totalValue > DiscountThreshold)
            {
                totalValue -= totalValue * DiscountRate;
            }

            return Math.Round(totalValue, 2);
        }
    }

    public class MarketSystem
    {
        public List<Order> Orders { get; private set; }

        public MarketSystem()
        {
            Orders = new List<Order>();
        }

        public void AddOrder(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order), "Order cannot be null!");

            Orders.Add(order);
        }

        public string? GetTopSpenderName()
        {
            var topCustomer = Orders

                .GroupBy(o => o.Customer.CustomerId)
                .Select(group => new
                {

                    CustomerName = group.First().Customer.Name,
                    TotalSpent = group.Sum(o => o.CalculateFinalPrice())
                })
                .OrderByDescending(c => c.TotalSpent)
                .FirstOrDefault();

            return topCustomer?.CustomerName;
        }

        public string? GetTopSpenderNameImperative()
        {
            Dictionary<int, decimal> customerTotals = new Dictionary<int, decimal>();
            Dictionary<int, Customer> customerInfo = new Dictionary<int, Customer>();

            foreach (Order order in Orders)
            {
                int currentId = order.Customer.CustomerId;
                decimal orderTotal = order.CalculateFinalPrice();

                if (customerTotals.ContainsKey(currentId))
                {
                    customerTotals[currentId] += orderTotal;
                }
                else
                {
                    customerTotals.Add(currentId, orderTotal);
                    customerInfo.Add(currentId, order.Customer);
                }
            }

            int topCustomerId = -1;
            decimal maxSpent = -1m;


            foreach (var kvp in customerTotals)
            {
                if (kvp.Value > maxSpent)
                {
                    maxSpent = kvp.Value;
                    topCustomerId = kvp.Key;
                }
            }

            return topCustomerId != -1 ? customerInfo[topCustomerId].Name : null;
        }
        public IEnumerable<KeyValuePair<string, int>> GetPopularProducts()
        {
            return Orders
                .SelectMany(o => o.Items)
                .GroupBy(item => item.ProductName)
                .Select(group => new KeyValuePair<string, int>(group.Key, group.Sum(item => item.Quantity)))
                .OrderByDescending(kvp => kvp.Value);
        }

        public IEnumerable<KeyValuePair<string, int>> GetPopularProductsImperative()
        {
            Dictionary<string, int> productQuantities = new Dictionary<string, int>();

            foreach (var order in Orders)
            {
                foreach (var item in order.Items)
                {
                    if (productQuantities.ContainsKey(item.ProductName))
                    {
                        productQuantities[item.ProductName] += item.Quantity;
                    }
                    else
                    {
                        productQuantities.Add(item.ProductName, item.Quantity);
                    }
                }
            }

            List<KeyValuePair<string, int>> sortedList = new List<KeyValuePair<string, int>>(productQuantities);

            sortedList.Sort((x, y) => y.Value.CompareTo(x.Value));

            return sortedList;
        }
    }

        public class Program
        {
            public static void Main(string[] args)
            {


                try
                {
                    var system = new MarketSystem();

                    var customer1 = new Customer(1, "Alice Popescu");
                    var customer2 = new Customer(2, "Bob Ionescu");

                    var order1 = new Order(101, customer1);
                    order1.AddItem(new OrderItem("Mouse USB", 2, 50m));
                    system.AddOrder(order1);

                    var order2 = new Order(102, customer2);
                    order2.AddItem(new OrderItem("Laptop", 1, 1000m));
                    order2.AddItem(new OrderItem("Mouse USB", 1, 50m));
                    system.AddOrder(order2);

                    // linia urmatoare activeaza o exceptie
                    // order2.AddItem(new OrderItem("Hacked Keyboard", -5, 100m)); 

                    Console.WriteLine($"Order {order1.OrderId} ({order1.Customer.Name}): {order1.CalculateFinalPrice()} EUR");
                    Console.WriteLine($"Order {order2.OrderId} ({order2.Customer.Name}): {order2.CalculateFinalPrice()} EUR (10% discount applied)\n");

                    Console.WriteLine($"Top Spender LINQ: {system.GetTopSpenderName() ?? "not found" }");
                    Console.WriteLine($"Top Spender Imperative: {system.GetTopSpenderNameImperative() ?? "not found" }\n");

                    Console.WriteLine("Popular Products LINQ:");
                    foreach (var product in system.GetPopularProducts())
                    {
                        Console.WriteLine($" - {product.Key}: {product.Value} units sold");
                    }
                    Console.WriteLine("si imperativ:");
                    foreach (var product in system.GetPopularProductsImperative())
                    {
                        Console.WriteLine($" - {product.Key}: {product.Value} units sold");
                    }
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Console.WriteLine($"\n[DATA ERROR]: You entered an invalid numeric value. Details: {ex.Message}");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"\n[ARGUMENT ERROR]: A required piece of information is missing. Details: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[SYSTEM ERROR]: An unexpected problem occurred: {ex.Message}");
                }
                finally
                {
                    Console.WriteLine("\nPress Enter to close the application...");
                    Console.ReadLine();
                }
            }
        }
    }
