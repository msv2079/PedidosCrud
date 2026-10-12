using PedidosCrud.Model;
using PedidosCrud.Repository;
using PedidosCrud.Service;

namespace PedidosCrud.Tests;

[TestClass]
public sealed class OrderServiceTests
{
    private static OrderService CreateService()
    {
        var repository = new InMemoryOrderRepository(new OrderSeedGenerator(TimeProvider.System));
        return new OrderService(repository, TimeProvider.System);
    }

    private static OrderInput ValidInput()
    {
        return new OrderInput { CustomerName = "Cliente de teste", Description = "Pedido de teste", TotalAmount = 1234.56m, Status = OrderStatus.Open };
    }

    [TestMethod]
    public void InitialLoadContainsTenThousandUniqueOrders()
    {
        var result = CreateService().Search(new OrderQuery { PageSize = 100 });
        Assert.AreEqual(10_000, result.TotalCount);
        Assert.AreEqual(100, result.Items.Count);
        Assert.AreEqual(100, result.Items.Select(order => order.Id).Distinct().Count());
        Assert.IsTrue(result.Items.All(order => order.Version == 1 && order.TotalAmount > 0));
    }

    [TestMethod]
    public void CreateUpdateAndDeletePreserveIdentityAndCreationTime()
    {
        var service = CreateService();
        var input = ValidInput();
        input.CustomerName = "  Cliente de teste  ";
        var created = service.Create(input);
        input.Version = created.Version;
        input.Status = OrderStatus.Paid;
        var updated = service.Update(created.Id, input);

        Assert.AreEqual(10_001, created.Id);
        Assert.AreEqual("Cliente de teste", created.CustomerName);
        Assert.AreEqual(created.CreatedAt, updated.CreatedAt);
        Assert.AreEqual(OrderStatus.Paid, service.GetById(created.Id).Status);
        Assert.AreEqual(2L, updated.Version);
        service.Delete(updated.Id, updated.Version);
        Assert.ThrowsExactly<OrderNotFoundException>(() => service.GetById(updated.Id));
        Assert.AreEqual(10_000, service.Search(new OrderQuery()).TotalCount);
    }

    [TestMethod]
    public void ServiceRejectsInvalidFieldsWithoutControllerValidation()
    {
        var input = new OrderInput { CustomerName = " ", Description = "", TotalAmount = -5, Status = (OrderStatus)99 };
        var exception = Assert.ThrowsExactly<InputValidationException>(() => CreateService().Create(input));
        CollectionAssert.AreEquivalent(new[] { "customerName", "description", "totalAmount", "status" }, exception.Errors.Keys.ToArray());
    }

    [TestMethod]
    public void ServiceRejectsMoreThanTwoDecimalPlaces()
    {
        var input = ValidInput();
        input.TotalAmount = 1.001m;
        var exception = Assert.ThrowsExactly<InputValidationException>(() => CreateService().Create(input));
        Assert.IsTrue(exception.Errors.ContainsKey("totalAmount"));
    }

    [TestMethod]
    [DataRow("999999.99", true)]
    [DataRow("1000000.00", true)]
    [DataRow("1000000.01", false)]
    public void TotalAmountCannotExceedOneMillion(string amount, bool isValid)
    {
        var service = CreateService();
        var input = ValidInput();
        input.TotalAmount = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        if (isValid)
        {
            Assert.AreEqual(input.TotalAmount, service.Create(input).TotalAmount);
        }
        else
        {
            var exception = Assert.ThrowsExactly<InputValidationException>(() => service.Create(input));
            Assert.IsTrue(exception.Errors.ContainsKey("totalAmount"));
        }
    }

    [TestMethod]
    public void SearchIsCaseInsensitiveAndCountsAllMatchesBeforePagination()
    {
        var service = CreateService();

        for (var index = 0; index < 13; index++)
        {
            var input = ValidInput();
            input.CustomerName = "Pesquisa Exclusiva";
            service.Create(input);
        }

        var firstPage = service.Search(new OrderQuery { Search = "pesquisa exclusiva", PageSize = 10 });
        var secondPage = service.Search(new OrderQuery { Search = "PESQUISA EXCLUSIVA", Page = 2, PageSize = 10 });
        Assert.AreEqual(13, firstPage.FilteredCount);
        Assert.AreEqual(10_013, firstPage.TotalCount);
        Assert.AreEqual(10, firstPage.Items.Count);
        Assert.AreEqual(3, secondPage.Items.Count);
        Assert.AreEqual(0, firstPage.Items.Select(order => order.Id).Intersect(secondPage.Items.Select(order => order.Id)).Count());
    }

    [TestMethod]
    public void ServiceRejectsInvalidPaginationAndSorting()
    {
        var query = new OrderQuery { Page = 0, PageSize = 101, SortBy = "unknown", Search = new string('a', 201) };
        var exception = Assert.ThrowsExactly<InputValidationException>(() => CreateService().Search(query));
        Assert.AreEqual(4, exception.Errors.Count);
    }

    [TestMethod]
    public void UpdatingWithOldVersionReturnsConflictWithoutOverwriting()
    {
        var service = CreateService();
        var created = service.Create(ValidInput());
        var input = ValidInput();
        input.Version = created.Version;
        input.Description = "Primeira alteração";
        service.Update(created.Id, input);
        input.Description = "Alteração desatualizada";

        Assert.ThrowsExactly<OrderConflictException>(() => service.Update(created.Id, input));
        Assert.ThrowsExactly<OrderConflictException>(() => service.Delete(created.Id, created.Version));
        Assert.AreEqual("Primeira alteração", service.GetById(created.Id).Description);
    }

    [TestMethod]
    public void UpdateAndDeleteRequirePositiveVersion()
    {
        var service = CreateService();
        Assert.ThrowsExactly<InputValidationException>(() => service.Update(1, ValidInput()));
        Assert.ThrowsExactly<InputValidationException>(() => service.Delete(1, 0));
    }

    [TestMethod]
    public async Task ConcurrentCreateAndReadKeepUniqueIdsAndFinalCount()
    {
        var service = CreateService();
        var writes = Enumerable.Range(0, 200).Select(_ => Task.Run(() => service.Create(ValidInput()))).ToArray();
        var reads = Enumerable.Range(0, 30).Select(_ => Task.Run(() => service.Search(new OrderQuery { PageSize = 100 }))).ToArray();
        await Task.WhenAll(writes.Cast<Task>().Concat(reads));

        Assert.AreEqual(200, writes.Select(task => task.Result.Id).Distinct().Count());
        Assert.AreEqual(10_200, service.Search(new OrderQuery()).TotalCount);
        Assert.IsTrue(reads.All(task => task.Result.Items.Count == 100 && task.Result.FilteredCount == task.Result.TotalCount));
    }

    [TestMethod]
    public async Task ConcurrentUpdatesWithSameVersionAllowExactlyOneSuccess()
    {
        var service = CreateService();
        var order = service.Create(ValidInput());
        var operations = Enumerable.Range(0, 30).Select(index => Task.Run(() =>
        {
            var input = ValidInput();
            input.Version = order.Version;
            input.Description = $"Alteração {index}";

            try
            {
                service.Update(order.Id, input);
                return true;
            }
            catch (OrderConflictException)
            {
                return false;
            }
        }));

        var results = await Task.WhenAll(operations);
        Assert.AreEqual(1, results.Count(success => success));
        Assert.AreEqual(2L, service.GetById(order.Id).Version);
    }
}
